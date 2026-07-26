from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from app.database import get_db
from app.iap_service import IapError, get_entitlements, spend_paid_gems, sync_voided_purchases, verify_google_purchase
from app.player_service import PlayerProfileError
from app.schemas import (
    IapGoogleRestoreRequest,
    IapGoogleVerifyRequest,
    IapGoogleVerifyResponse,
    IapGemSpendRequest,
    IapGemSpendResponse,
    IapProfileStateResponse,
    IapVoidedPurchasesSyncRequest,
    IapVoidedPurchasesSyncResponse,
)


router = APIRouter(prefix="/iap", tags=["iap"])


@router.post("/google/verify", response_model=IapGoogleVerifyResponse)
def verify_google_iap(payload: IapGoogleVerifyRequest, db: Session = Depends(get_db)):
    try:
        return verify_google_purchase(db, payload)
    except IapError as exc:
        return IapGoogleVerifyResponse(
            success=False,
            errorCode=exc.error_code,
            message=exc.message,
        )


@router.post("/google/restore", response_model=IapGoogleVerifyResponse)
def restore_google_iap(payload: IapGoogleRestoreRequest, db: Session = Depends(get_db)):
    last_response: IapGoogleVerifyResponse | None = None
    for purchase in payload.purchases:
        if purchase.productId != "remove_ads":
            continue
        request = IapGoogleVerifyRequest(
            profileId=payload.profileId,
            productId=purchase.productId,
            purchaseToken=purchase.purchaseToken,
            orderId=purchase.orderId,
        )
        try:
            last_response = verify_google_purchase(db, request)
        except IapError as exc:
            last_response = IapGoogleVerifyResponse(
                success=False,
                errorCode=exc.error_code,
                message=exc.message,
            )

    if last_response is not None:
        return last_response

    try:
        profile = get_entitlements(db, payload.profileId)
    except PlayerProfileError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc

    return IapGoogleVerifyResponse(
        success=True,
        alreadyGranted=True,
        profile=profile,
        message="No restorable non-consumable purchases were found.",
    )


@router.post("/google/sync-voided-purchases", response_model=IapVoidedPurchasesSyncResponse)
def sync_google_voided_purchases(
    payload: IapVoidedPurchasesSyncRequest,
    db: Session = Depends(get_db),
):
    try:
        return sync_voided_purchases(db, payload.adminSecret, None)
    except IapError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc


@router.post("/gems/spend", response_model=IapGemSpendResponse)
def spend_iap_gems(payload: IapGemSpendRequest, db: Session = Depends(get_db)):
    try:
        return spend_paid_gems(db, payload.profileId, payload.amount, payload.reason)
    except (IapError, PlayerProfileError) as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc


@router.get("/entitlements", response_model=IapProfileStateResponse)
def read_iap_entitlements(
    profileId: str = Query(..., min_length=1),
    db: Session = Depends(get_db),
):
    try:
        return get_entitlements(db, profileId)
    except PlayerProfileError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc
