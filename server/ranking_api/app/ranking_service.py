from datetime import datetime
from typing import List, Optional

from sqlalchemy.orm import Session

from app import models
from app.schemas import RankingSubmissionCreate, RankingSubmissionResponse
from app.verification import verify_submission


def to_response(row: models.RankingSubmission) -> RankingSubmissionResponse:
    return RankingSubmissionResponse(
        submissionId=row.submission_id,
        challengeId=row.challenge_id,
        playerId=row.player_id,
        playerName=row.player_name,
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


def create_submission(db: Session, payload: RankingSubmissionCreate) -> models.RankingSubmission:
    existing = get_submission_by_id(db, payload.submissionId)
    if existing is not None:
        return existing

    verification = verify_submission(payload)
    row = models.RankingSubmission(
        submission_id=payload.submissionId,
        challenge_id=payload.challengeId,
        player_id=payload.playerId,
        player_name=payload.playerName or "Player",
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
    return row


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
