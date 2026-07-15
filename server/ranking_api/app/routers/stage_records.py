from datetime import datetime
from typing import List

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from app import models
from app.database import get_db
from app.schemas import (
    StageProgressLeaderboardResponse,
    StageProgressMyRankResponse,
    StageProgressRecordResponse,
    StageProgressSubmitRequest,
    StageProgressSubmitResponse,
)


router = APIRouter(prefix="/stage-records", tags=["stage-records"])


def _normalize_mode(mode: str) -> str:
    value = (mode or "").strip().lower()
    if value not in {"normal", "hard", "infinity"}:
        return "normal"
    return value


def _ordered_records(db: Session, mode: str) -> List[models.StageProgressRecord]:
    return (
        db.query(models.StageProgressRecord)
        .filter(models.StageProgressRecord.mode == mode)
        .order_by(
            models.StageProgressRecord.cleared_stage.desc(),
            models.StageProgressRecord.total_stars.desc(),
            models.StageProgressRecord.updated_at.asc(),
        )
        .all()
    )


def _ranked_response(rows: List[models.StageProgressRecord]) -> List[StageProgressRecordResponse]:
    responses: List[StageProgressRecordResponse] = []
    previous_stage = None
    previous_stars = None
    current_rank = 0
    for index, row in enumerate(rows):
        tied = (
            index > 0
            and row.cleared_stage == previous_stage
            and row.total_stars == previous_stars
        )
        if not tied:
            current_rank = index + 1

        responses.append(
            StageProgressRecordResponse(
                rank=current_rank,
                tied=tied,
                playerId=row.player_id,
                nickname=row.nickname,
                profileImageId=row.profile_image_id,
                mode=row.mode,
                clearedStage=row.cleared_stage,
                totalStars=row.total_stars,
                updatedAt=row.updated_at.isoformat(),
            )
        )
        previous_stage = row.cleared_stage
        previous_stars = row.total_stars

    return responses


@router.post("/submit", response_model=StageProgressSubmitResponse)
def submit(payload: StageProgressSubmitRequest, db: Session = Depends(get_db)):
    mode = _normalize_mode(payload.mode)
    now = datetime.utcnow()
    row = (
        db.query(models.StageProgressRecord)
        .filter_by(player_id=payload.playerId, mode=mode)
        .one_or_none()
    )
    created = row is None
    if row is None:
        row = models.StageProgressRecord(
            player_id=payload.playerId,
            mode=mode,
            created_at=now,
        )
        db.add(row)

    row.nickname = payload.nickname
    row.profile_image_id = payload.profileImageId
    row.cleared_stage = max(row.cleared_stage or 0, payload.clearedStage)
    row.total_stars = max(row.total_stars or 0, payload.totalStars)
    row.client_updated_at_utc = payload.clientUpdatedAtUtc
    row.updated_at = now
    db.commit()
    db.refresh(row)

    records = _ranked_response(_ordered_records(db, mode))
    response_record = next(record for record in records if record.playerId == row.player_id)
    return StageProgressSubmitResponse(
        success=True,
        message="created" if created else "updated",
        record=response_record,
    )


@router.get("/leaderboard", response_model=StageProgressLeaderboardResponse)
def leaderboard(
    mode: str = Query("normal", min_length=1),
    limit: int = Query(50, ge=1, le=100),
    db: Session = Depends(get_db),
):
    normalized = _normalize_mode(mode)
    records = _ranked_response(_ordered_records(db, normalized))[:limit]
    return StageProgressLeaderboardResponse(
        success=True,
        mode=normalized,
        records=records,
    )


@router.get("/my-rank", response_model=StageProgressMyRankResponse)
def my_rank(
    playerId: str = Query(..., min_length=1),
    mode: str = Query("normal", min_length=1),
    db: Session = Depends(get_db),
):
    normalized = _normalize_mode(mode)
    for record in _ranked_response(_ordered_records(db, normalized)):
        if record.playerId == playerId:
            return StageProgressMyRankResponse(
                success=True,
                message="ok",
                record=record,
            )

    return StageProgressMyRankResponse(
        success=False,
        message="record_not_found",
        record=None,
    )
