import hashlib
import json
from datetime import datetime
from typing import Any

from sqlalchemy.orm import Session

from app import models
from app.config import settings
from app.google_play_api import GooglePlayApiError, GooglePlayDeveloperApi
from app.iap_products import IapProductConfig, get_product
from app.player_service import PlayerProfileError, get_profile_by_id
from app.schemas import (
    IapGemSpendResponse,
    IapGoogleVerifyRequest,
    IapGoogleVerifyResponse,
    IapProfileStateResponse,
    IapPurchaseResponse,
    IapVoidedPurchasesSyncResponse,
)


class IapError(ValueError):
    def __init__(self, status_code: int, error_code: str, message: str):
        super().__init__(message)
        self.status_code = status_code
        self.error_code = error_code
        self.message = message


def verify_google_purchase(
    db: Session,
    payload: IapGoogleVerifyRequest,
    google_api: GooglePlayDeveloperApi | None = None,
) -> IapGoogleVerifyResponse:
    profile = get_profile_by_id(db, payload.profileId)
    if profile is None:
        raise IapError(404, "PROFILE_NOT_FOUND", "Player profile was not found.")

    product = get_product(payload.productId)
    if product is None:
        raise IapError(400, "UNKNOWN_PRODUCT_ID", "Unknown productId.")

    expected_package = settings.google_play_package_name
    if payload.packageName != expected_package:
        raise IapError(400, "PACKAGE_NAME_MISMATCH", "Package name does not match server config.")

    token_hash = hash_purchase_token(payload.purchaseToken)
    existing = get_purchase_by_token_hash(db, token_hash)
    if existing is not None and existing.status in {"voided", "revoked"}:
        raise IapError(409, "PURCHASE_REVOKED", "Purchase was already voided or revoked.")
    if existing is not None and existing.status in {"granted", "consumed", "acknowledged"}:
        return _build_verify_response(db, existing, already_granted=True, message="Purchase already granted.")

    api = google_api or GooglePlayDeveloperApi()
    try:
        google_purchase = api.get_product_purchase(payload.packageName, payload.productId, payload.purchaseToken)
    except GooglePlayApiError as exc:
        raise IapError(503, exc.error_code, exc.message) from exc

    if google_purchase.purchase_state not in (0, None):
        raise IapError(400, "PURCHASE_NOT_PURCHASED", "Purchase is not in purchased state.")

    now = datetime.utcnow()
    purchase = existing or models.IapPurchase(
        profile_id=profile.profile_id,
        google_play_player_id=profile.google_play_player_id,
        package_name=payload.packageName,
        product_id=payload.productId,
        purchase_token=payload.purchaseToken,
        purchase_token_hash=token_hash,
        order_id=payload.orderId or google_purchase.order_id,
        product_type=product.product_type,
        quantity=1,
        status="pending",
        created_at=now,
    )
    purchase.google_play_player_id = profile.google_play_player_id
    purchase.purchase_state = google_purchase.purchase_state
    purchase.acknowledgement_state = google_purchase.acknowledgement_state
    purchase.consumption_state = google_purchase.consumption_state
    purchase.order_id = payload.orderId or google_purchase.order_id or purchase.order_id
    purchase.granted_currency_type = product.grant_currency_type
    purchase.granted_amount = product.grant_amount
    if product.grant_currency_type == "gems":
        purchase.granted_gems = purchase.granted_gems or product.grant_amount
        purchase.remaining_gems = purchase.remaining_gems or product.grant_amount
        purchase.used_gems = purchase.used_gems or 0
        purchase.refundable_status = calculate_refundable_status(purchase)
    purchase.entitlement_key = product.entitlement_key
    purchase.raw_google_response = json.dumps(google_purchase.raw, ensure_ascii=False)
    purchase.updated_at = now
    purchase.status = "verified"
    db.add(purchase)
    db.flush()

    entitlement = get_or_create_entitlement(db, profile.profile_id)
    grant_product(entitlement, purchase, product, now)

    try:
        if product.product_type == "consumable":
            api.consume_product_purchase(payload.packageName, payload.productId, payload.purchaseToken)
            purchase.status = "consumed"
            purchase.consumed_at = now
        else:
            api.acknowledge_product_purchase(payload.packageName, payload.productId, payload.purchaseToken)
            purchase.status = "acknowledged"
            purchase.acknowledged_at = now
    except GooglePlayApiError as exc:
        # Grant remains recorded; Google acknowledgement/consume can be retried by sending the same token again.
        purchase.status = "granted"
        purchase.raw_google_response = json.dumps(
            {"purchase": google_purchase.raw, "finishTransactionError": exc.error_code},
            ensure_ascii=False,
        )

    purchase.granted_at = purchase.granted_at or now
    purchase.updated_at = now
    entitlement.updated_at = now
    db.commit()
    db.refresh(purchase)
    return _build_verify_response(db, purchase, already_granted=False, message="Purchase verified and granted.")


def get_entitlements(db: Session, profile_id: str) -> IapProfileStateResponse:
    profile = get_profile_by_id(db, profile_id)
    if profile is None:
        raise PlayerProfileError(404, "not_found", "Player profile was not found.")

    entitlement = get_or_create_entitlement(db, profile.profile_id)
    db.commit()
    return to_profile_state(entitlement)


def spend_paid_gems(db: Session, profile_id: str, amount: int, reason: str = "") -> IapGemSpendResponse:
    profile = get_profile_by_id(db, profile_id)
    if profile is None:
        raise PlayerProfileError(404, "not_found", "Player profile was not found.")

    if amount <= 0:
        raise IapError(400, "INVALID_GEM_SPEND", "amount must be greater than zero.")

    entitlement = get_or_create_entitlement(db, profile.profile_id)
    remaining_to_spend = amount
    paid_used = 0
    grants = (
        db.query(models.IapPurchase)
        .filter(
            models.IapPurchase.profile_id == profile.profile_id,
            models.IapPurchase.granted_currency_type == "gems",
            models.IapPurchase.remaining_gems > 0,
            models.IapPurchase.status != "revoked",
        )
        .order_by(models.IapPurchase.granted_at.asc(), models.IapPurchase.created_at.asc(), models.IapPurchase.id.asc())
        .all()
    )

    now = datetime.utcnow()
    for grant in grants:
        if remaining_to_spend <= 0:
            break

        take = min(remaining_to_spend, max(0, grant.remaining_gems))
        if take <= 0:
            continue

        grant.remaining_gems -= take
        grant.used_gems += take
        grant.refundable_status = calculate_refundable_status(grant)
        grant.updated_at = now
        paid_used += take
        remaining_to_spend -= take

    if paid_used > 0:
        entitlement.gems = max(0, entitlement.gems - paid_used)
        entitlement.updated_at = now

    db.commit()
    return IapGemSpendResponse(
        success=True,
        profileId=profile.profile_id,
        requestedAmount=amount,
        paidGemsUsed=paid_used,
        untrackedGemsUsed=remaining_to_spend,
        remainingPaidGems=entitlement.gems,
        refundDebtGems=entitlement.refund_debt_gems,
        message="Paid gem ledger updated." if paid_used > 0 else "No paid gems were available in the ledger.",
    )


def sync_voided_purchases(db: Session, admin_secret: str, google_api: GooglePlayDeveloperApi | None = None) -> IapVoidedPurchasesSyncResponse:
    if not settings.iap_admin_secret or admin_secret != settings.iap_admin_secret:
        raise IapError(403, "FORBIDDEN", "Invalid admin secret.")

    if not settings.google_play_voided_sync_enabled:
        return IapVoidedPurchasesSyncResponse(success=True, scanned=0, revoked=0, skipped=0, message="Voided sync is disabled.")

    api = google_api or GooglePlayDeveloperApi()
    try:
        voided = api.list_voided_purchases(settings.google_play_package_name)
    except GooglePlayApiError as exc:
        raise IapError(503, exc.error_code, exc.message) from exc

    revoked = 0
    skipped = 0
    for entry in voided:
        token = entry.get("purchaseToken") or ""
        order_id = entry.get("orderId") or ""
        purchase = None
        if token:
            purchase = get_purchase_by_token_hash(db, hash_purchase_token(token))
        if purchase is None and order_id:
            purchase = db.query(models.IapPurchase).filter(models.IapPurchase.order_id == order_id).first()
        if purchase is None or purchase.status == "revoked":
            skipped += 1
            continue
        apply_revocation(db, purchase, str(entry.get("voidedReason") or "voided"))
        revoked += 1

    db.commit()
    return IapVoidedPurchasesSyncResponse(success=True, scanned=len(voided), revoked=revoked, skipped=skipped, message="Voided purchases synced.")


def get_or_create_entitlement(db: Session, profile_id: str) -> models.IapPlayerEntitlement:
    entitlement = (
        db.query(models.IapPlayerEntitlement)
        .filter(models.IapPlayerEntitlement.profile_id == profile_id)
        .first()
    )
    if entitlement is not None:
        return entitlement

    now = datetime.utcnow()
    entitlement = models.IapPlayerEntitlement(profile_id=profile_id, created_at=now, updated_at=now)
    db.add(entitlement)
    db.flush()
    return entitlement


def get_purchase_by_token_hash(db: Session, token_hash: str) -> models.IapPurchase | None:
    return (
        db.query(models.IapPurchase)
        .filter(models.IapPurchase.purchase_token_hash == token_hash)
        .first()
    )


def grant_product(
    entitlement: models.IapPlayerEntitlement,
    purchase: models.IapPurchase,
    product: IapProductConfig,
    now: datetime,
) -> None:
    if purchase.granted_at is not None:
        return

    if product.grant_currency_type == "gems":
        entitlement.gems += product.grant_amount
        purchase.granted_gems = product.grant_amount
        purchase.remaining_gems = product.grant_amount
        purchase.used_gems = 0
        purchase.refundable_status = "unused"
    elif product.entitlement_key == "remove_ads":
        entitlement.remove_ads_purchased = True

    purchase.status = "granted"
    purchase.granted_at = now


def apply_revocation(db: Session, purchase: models.IapPurchase, reason: str) -> None:
    entitlement = get_or_create_entitlement(db, purchase.profile_id)
    before = entitlement.gems
    action_type = "no_action"
    amount = 0

    if purchase.granted_currency_type == "gems" and purchase.granted_amount > 0:
        action_type = "subtract_currency"
        reclaimable = max(0, purchase.remaining_gems)
        used = max(0, purchase.used_gems)
        amount = reclaimable
        missing_reclaimable = max(0, reclaimable - before)
        entitlement.gems = max(0, entitlement.gems - reclaimable)
        if used > 0 or missing_reclaimable > 0:
            entitlement.refund_debt_gems += used + missing_reclaimable
        purchase.remaining_gems = 0
        purchase.used_gems = max(purchase.used_gems, max(0, purchase.granted_gems))
        purchase.refundable_status = "revoked"
    elif purchase.entitlement_key == "remove_ads":
        action_type = "revoke_entitlement"
        entitlement.remove_ads_purchased = False

    now = datetime.utcnow()
    purchase.status = "revoked"
    purchase.voided_reason = reason
    purchase.voided_at = purchase.voided_at or now
    purchase.revoked_at = now
    purchase.updated_at = now
    entitlement.updated_at = now
    db.add(
        models.IapRevocationLog(
            purchase_id=purchase.id,
            profile_id=purchase.profile_id,
            action_type=action_type,
            amount=amount,
            before_balance=before,
            after_balance=entitlement.gems,
            note=reason,
            created_at=now,
        )
    )


def to_profile_state(entitlement: models.IapPlayerEntitlement) -> IapProfileStateResponse:
    return IapProfileStateResponse(
        profileId=entitlement.profile_id,
        gems=entitlement.gems,
        coins=entitlement.coins,
        removeAdsPurchased=entitlement.remove_ads_purchased,
        refundDebtGems=entitlement.refund_debt_gems,
    )


def hash_purchase_token(purchase_token: str) -> str:
    return hashlib.sha256((purchase_token or "").encode("utf-8")).hexdigest()


def mask_token(value: str) -> str:
    value = (value or "").strip()
    if len(value) <= 8:
        return "***"
    return f"{value[:4]}***{value[-4:]}"


def calculate_refundable_status(purchase: models.IapPurchase) -> str:
    if purchase.status == "revoked":
        return "revoked"
    if purchase.status == "voided":
        return "voided"
    if purchase.used_gems <= 0:
        return "unused"
    if purchase.remaining_gems <= 0:
        return "fully_used"
    return "partially_used"


def _build_verify_response(
    db: Session,
    purchase: models.IapPurchase,
    already_granted: bool,
    message: str,
) -> IapGoogleVerifyResponse:
    entitlement = get_or_create_entitlement(db, purchase.profile_id)
    return IapGoogleVerifyResponse(
        success=True,
        alreadyGranted=already_granted,
        profile=to_profile_state(entitlement),
        purchase=IapPurchaseResponse(
            productId=purchase.product_id,
            status=purchase.status,
            productType=purchase.product_type,
            grantedCurrencyType=purchase.granted_currency_type,
            grantedAmount=purchase.granted_amount,
            grantedGems=purchase.granted_gems,
            remainingGems=purchase.remaining_gems,
            usedGems=purchase.used_gems,
            refundableStatus=purchase.refundable_status,
        ),
        message=message,
    )
