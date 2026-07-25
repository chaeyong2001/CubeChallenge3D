from datetime import datetime
from typing import Optional

from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app import models
from app.nickname_validator import nickname_key, validate_nickname
from app.schemas import PlayerProfileCreateRequest, PlayerProfileResponse


MIN_AVATAR_ID = 0
MAX_AVATAR_ID = 3


class PlayerProfileError(ValueError):
    def __init__(self, status_code: int, reason: str, message: str):
        super().__init__(message)
        self.status_code = status_code
        self.reason = reason
        self.message = message


def to_player_profile_response(row: models.PlayerProfile) -> PlayerProfileResponse:
    return PlayerProfileResponse(
        profileId=row.profile_id,
        nickname=row.nickname,
        avatarId=row.avatar_id,
        createdAt=_format_utc(row.created_at),
        updatedAt=_format_utc(row.updated_at),
        linkedGooglePlay=bool(row.google_play_player_id),
        linkedGoogle=bool(row.google_account_id),
        googlePlayPlayerId=row.google_play_player_id,
        googlePlayGamesPlayerId=row.google_play_player_id,
    )


def get_profile_by_id(db: Session, profile_id: str) -> Optional[models.PlayerProfile]:
    return (
        db.query(models.PlayerProfile)
        .filter(models.PlayerProfile.profile_id == profile_id.strip())
        .first()
    )


def get_profile_by_nickname_key(db: Session, normalized: str) -> Optional[models.PlayerProfile]:
    return (
        db.query(models.PlayerProfile)
        .filter(models.PlayerProfile.nickname_normalized == normalized)
        .first()
    )


def get_profile_by_google_play_id(db: Session, google_play_player_id: str) -> Optional[models.PlayerProfile]:
    google_play_player_id = google_play_player_id.strip() if google_play_player_id else ""
    if not google_play_player_id:
        return None

    return (
        db.query(models.PlayerProfile)
        .filter(models.PlayerProfile.google_play_player_id == google_play_player_id)
        .first()
    )


def is_avatar_id_valid(avatar_id: int) -> bool:
    return MIN_AVATAR_ID <= avatar_id <= MAX_AVATAR_ID


def create_profile(db: Session, payload: PlayerProfileCreateRequest) -> models.PlayerProfile:
    profile_id = payload.profileId.strip() if payload.profileId else ""
    if not profile_id:
        raise PlayerProfileError(400, "invalid_profile_id", "profileId is required.")

    existing = get_profile_by_id(db, profile_id)
    if existing is not None:
        return existing

    raw_google_play_player_id = payload.googlePlayGamesPlayerId or payload.googlePlayPlayerId
    google_play_player_id = raw_google_play_player_id.strip() if raw_google_play_player_id else ""
    if google_play_player_id:
        existing_google = get_profile_by_google_play_id(db, google_play_player_id)
        if existing_google is not None:
            return existing_google

    validation = validate_nickname(payload.nickname)
    if not validation.valid:
        raise PlayerProfileError(400, validation.error_code or "invalid", validation.message)

    if not is_avatar_id_valid(payload.avatarId):
        raise PlayerProfileError(
            400,
            "invalid_avatar",
            f"avatarId must be between {MIN_AVATAR_ID} and {MAX_AVATAR_ID}.",
        )

    normalized_key = nickname_key(validation.normalized)
    if get_profile_by_nickname_key(db, normalized_key) is not None:
        raise PlayerProfileError(409, "duplicate", "Nickname is already taken.")

    now = datetime.utcnow()
    row = models.PlayerProfile(
        profile_id=profile_id,
        nickname=validation.normalized,
        nickname_normalized=normalized_key,
        avatar_id=payload.avatarId,
        created_at=now,
        updated_at=now,
        last_seen_at=now,
        google_play_player_id=google_play_player_id or None,
        google_account_id=payload.googleAccountId,
        google_email_hash=payload.googleEmailHash,
    )

    db.add(row)
    try:
        db.commit()
    except IntegrityError as exc:
        db.rollback()
        raise PlayerProfileError(409, "duplicate", "Profile or nickname already exists.") from exc

    db.refresh(row)
    return row


def update_avatar(db: Session, profile_id: str, avatar_id: int) -> models.PlayerProfile:
    row = get_profile_by_id(db, profile_id)
    if row is None:
        raise PlayerProfileError(404, "not_found", "Player profile was not found.")

    if not is_avatar_id_valid(avatar_id):
        raise PlayerProfileError(
            400,
            "invalid_avatar",
            f"avatarId must be between {MIN_AVATAR_ID} and {MAX_AVATAR_ID}.",
        )

    row.avatar_id = avatar_id
    row.updated_at = datetime.utcnow()
    (
        db.query(models.RankingSubmission)
        .filter(
            models.RankingSubmission.player_id == row.profile_id,
            models.RankingSubmission.player_name == row.nickname,
        )
        .update({models.RankingSubmission.avatar_id: avatar_id}, synchronize_session=False)
    )
    (
        db.query(models.StageProgressRecord)
        .filter(
            models.StageProgressRecord.player_id == row.profile_id,
            models.StageProgressRecord.nickname == row.nickname,
        )
        .update({models.StageProgressRecord.profile_image_id: avatar_id}, synchronize_session=False)
    )
    db.commit()
    db.refresh(row)
    return row


def link_google_play(db: Session, profile_id: str, google_play_player_id: str) -> models.PlayerProfile:
    row = get_profile_by_id(db, profile_id)
    if row is None:
        raise PlayerProfileError(404, "not_found", "Player profile was not found.")

    google_play_player_id = google_play_player_id.strip() if google_play_player_id else ""
    if not google_play_player_id:
        raise PlayerProfileError(400, "invalid_google_play", "googlePlayPlayerId is required.")

    existing = get_profile_by_google_play_id(db, google_play_player_id)
    if existing is not None and existing.profile_id != row.profile_id:
        raise PlayerProfileError(409, "google_play_conflict", "Google Play Games account is already linked.")

    row.google_play_player_id = google_play_player_id
    row.updated_at = datetime.utcnow()
    db.commit()
    db.refresh(row)
    return row


def link_google(db: Session, profile_id: str, google_account_id: str, google_email_hash: str | None) -> models.PlayerProfile:
    row = get_profile_by_id(db, profile_id)
    if row is None:
        raise PlayerProfileError(404, "not_found", "Player profile was not found.")

    google_account_id = google_account_id.strip() if google_account_id else ""
    if not google_account_id:
        raise PlayerProfileError(400, "invalid_google", "googleAccountId is required.")

    existing = (
        db.query(models.PlayerProfile)
        .filter(models.PlayerProfile.google_account_id == google_account_id)
        .first()
    )
    if existing is not None and existing.profile_id != row.profile_id:
        raise PlayerProfileError(409, "google_conflict", "Google account is already linked.")

    row.google_account_id = google_account_id
    row.google_email_hash = google_email_hash.strip() if google_email_hash else None
    row.updated_at = datetime.utcnow()
    db.commit()
    db.refresh(row)
    return row


def _format_utc(value: datetime | None) -> str:
    if value is None:
        return ""
    return value.isoformat() + "Z"
