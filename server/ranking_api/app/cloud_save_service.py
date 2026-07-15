import hashlib
import json
from datetime import datetime
from typing import Any, Dict, Optional

from sqlalchemy.orm import Session

from app import models
from app.player_service import PlayerProfileError, get_profile_by_id
from app.schemas import (
    CloudSaveDownloadResponse,
    CloudSaveStatusResponse,
    CloudSaveUploadRequest,
    CloudSaveUploadResponse,
)


class CloudSaveError(ValueError):
    def __init__(self, status_code: int, reason: str, message: str):
        super().__init__(message)
        self.status_code = status_code
        self.reason = reason
        self.message = message


def get_cloud_save_status(db: Session, profile_id: str) -> CloudSaveStatusResponse:
    profile = _get_profile_or_error(db, profile_id)
    row = _get_cloud_save(db, profile.profile_id)
    return CloudSaveStatusResponse(
        profileId=profile.profile_id,
        exists=row is not None,
        saveVersion=row.save_version if row is not None else 0,
        serverUpdatedAt=_format_utc(row.server_updated_at) if row is not None else None,
        payloadHash=row.payload_hash if row is not None else None,
        googlePlayLinked=bool(profile.google_play_player_id),
        googleLinked=bool(profile.google_account_id),
    )


def upload_cloud_save(
    db: Session,
    profile_id: str,
    payload: CloudSaveUploadRequest,
) -> CloudSaveUploadResponse:
    profile = _get_profile_or_error(db, profile_id)
    if not profile.google_play_player_id and not profile.google_account_id:
        raise CloudSaveError(
            403,
            "account_not_linked",
            "Link Google Play Games or Google Account to enable cloud sync.",
        )

    payload_json = _dump_payload(payload.payload)
    payload_hash = hashlib.sha256(payload_json.encode("utf-8")).hexdigest()
    now = datetime.utcnow()
    row = _get_cloud_save(db, profile.profile_id)
    overwritten = row is not None
    if row is None:
        row = models.PlayerCloudSave(profile_id=profile.profile_id)
        db.add(row)

    row.save_version = payload.saveVersion
    row.payload_json = payload_json
    row.payload_hash = payload_hash
    row.client_updated_at = _parse_utc(payload.clientUpdatedAtUtc)
    row.server_updated_at = now
    row.device_id_hash = payload.deviceIdHash
    row.app_version = payload.appVersion
    db.commit()
    db.refresh(row)
    return CloudSaveUploadResponse(
        success=True,
        profileId=row.profile_id,
        serverUpdatedAt=_format_utc(row.server_updated_at),
        saveVersion=row.save_version,
        payloadHash=row.payload_hash,
        overwritten=overwritten,
    )


def download_cloud_save(db: Session, profile_id: str) -> CloudSaveDownloadResponse:
    profile = _get_profile_or_error(db, profile_id)
    row = _get_cloud_save(db, profile.profile_id)
    if row is None:
        raise CloudSaveError(404, "not_found", "Cloud save was not found.")

    return CloudSaveDownloadResponse(
        profileId=row.profile_id,
        saveVersion=row.save_version,
        payload=_load_payload(row.payload_json),
        serverUpdatedAt=_format_utc(row.server_updated_at),
        payloadHash=row.payload_hash,
    )


def _get_profile_or_error(db: Session, profile_id: str) -> models.PlayerProfile:
    profile = get_profile_by_id(db, profile_id)
    if profile is None:
        raise PlayerProfileError(404, "not_found", "Player profile was not found.")
    return profile


def _get_cloud_save(db: Session, profile_id: str) -> Optional[models.PlayerCloudSave]:
    return (
        db.query(models.PlayerCloudSave)
        .filter(models.PlayerCloudSave.profile_id == profile_id)
        .first()
    )


def _dump_payload(payload: Dict[str, Any]) -> str:
    return json.dumps(payload or {}, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def _load_payload(payload_json: str) -> Dict[str, Any]:
    try:
        value = json.loads(payload_json or "{}")
        return value if isinstance(value, dict) else {}
    except json.JSONDecodeError:
        return {}


def _parse_utc(value: Optional[str]) -> Optional[datetime]:
    if not value:
        return None
    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00")).replace(tzinfo=None)
    except ValueError:
        return None


def _format_utc(value: Optional[datetime]) -> str:
    if value is None:
        return ""
    return value.isoformat() + "Z"
