from datetime import datetime
from typing import List, Optional

from sqlalchemy.orm import Session

from app import models
from app.ranking_verification import verify_submission
from app.schemas import RankingSubmissionCreate, RankingSubmissionResponse


def to_response(row: models.RankingSubmission, db: Session = None) -> RankingSubmissionResponse:
    player_name = row.player_name
    avatar_id = row.avatar_id
    profile = _get_player_profile(db, row.player_id)
    if profile is not None and profile.nickname == row.player_name:
        avatar_id = profile.avatar_id

    return RankingSubmissionResponse(
        submissionId=row.submission_id,
        challengeId=row.challenge_id,
        playerId=row.player_id,
        playerName=player_name,
        avatarId=avatar_id,
        elapsedSeconds=row.elapsed_seconds,
        moveCount=row.move_count,
        scrambleNotation=row.scramble_notation,
        moveLogNotation=row.move_log_notation,
        controlMode=row.control_mode,
        completedAtUtc=row.completed_at_utc,
        clientVersion=row.client_version,
        deviceIdHash=row.device_id_hash,
        isVerified=row.is_verified,
        verifyReason=row.verify_reason,
        createdAt=row.created_at.isoformat() + "Z",
    )


def get_submission_by_id(db: Session, submission_id: str) -> Optional[models.RankingSubmission]:
    return (
        db.query(models.RankingSubmission)
        .filter(models.RankingSubmission.submission_id == submission_id)
        .first()
    )


def create_submission(db: Session, payload: RankingSubmissionCreate) -> tuple[models.RankingSubmission, bool]:
    existing = get_submission_by_id(db, payload.submissionId)
    if existing is not None:
        return existing, True

    verification = verify_submission(payload)
    player_name = payload.playerName or "Player"
    avatar_id = payload.avatarId if payload.avatarId is not None and payload.avatarId >= 0 else None
    profile = _get_player_profile(db, payload.playerId)
    if profile is not None and profile.nickname == player_name:
        avatar_id = profile.avatar_id

    row = models.RankingSubmission(
        submission_id=payload.submissionId,
        challenge_id=payload.challengeId,
        player_id=payload.playerId,
        player_name=player_name,
        avatar_id=avatar_id,
        elapsed_seconds=payload.elapsedSeconds,
        move_count=payload.moveCount,
        scramble_notation=payload.scrambleNotation,
        move_log_notation=payload.moveLogNotation,
        control_mode=payload.controlMode,
        completed_at_utc=payload.completedAtUtc,
        client_version=payload.clientVersion,
        device_id_hash=payload.deviceIdHash,
        is_verified=verification.is_verified,
        verify_reason=verification.reason,
        created_at=datetime.utcnow(),
    )
    db.add(row)
    db.commit()
    db.refresh(row)
    return row, False


def _get_player_profile(db: Session, player_id: str) -> Optional[models.PlayerProfile]:
    if db is None or not player_id:
        return None
    return (
        db.query(models.PlayerProfile)
        .filter(models.PlayerProfile.profile_id == player_id)
        .first()
    )


def get_top(db: Session, challenge_id: str, limit: int) -> List[models.RankingSubmission]:
    safe_limit = max(1, min(limit, 100))
    return (
        db.query(models.RankingSubmission)
        .filter(
            models.RankingSubmission.challenge_id == challenge_id,
            models.RankingSubmission.is_verified.is_(True),
        )
        .order_by(
            models.RankingSubmission.elapsed_seconds.asc(),
            models.RankingSubmission.move_count.asc(),
            models.RankingSubmission.created_at.asc(),
        )
        .limit(safe_limit)
        .all()
    )


def get_rank_for_submission(
    db: Session,
    challenge_id: str,
    submission_id: str,
) -> tuple[int, Optional[models.RankingSubmission]]:
    if not submission_id:
        return 0, None

    rows = (
        db.query(models.RankingSubmission)
        .filter(
            models.RankingSubmission.challenge_id == challenge_id,
            models.RankingSubmission.is_verified.is_(True),
        )
        .order_by(
            models.RankingSubmission.elapsed_seconds.asc(),
            models.RankingSubmission.move_count.asc(),
            models.RankingSubmission.created_at.asc(),
        )
        .all()
    )
    for index, row in enumerate(rows, start=1):
        if row.submission_id == submission_id:
            return index, row
    return 0, None
