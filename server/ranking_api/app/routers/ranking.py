from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from app import models
from app.database import get_db
from app.ranking_service import create_submission, get_top, to_response
from app.schemas import RankingSubmissionCreate, RankingSubmitResponse, RankingTopResponse


router = APIRouter(prefix="/ranking", tags=["ranking"])


@router.post("/submit", response_model=RankingSubmitResponse)
def submit(payload: RankingSubmissionCreate, db: Session = Depends(get_db)):
    row = create_submission(db, payload)
    return RankingSubmitResponse(
        success=True,
        isVerified=row.is_verified,
        message="saved" if row.is_verified else f"saved_unverified: {row.verify_reason}",
        submission=to_response(row),
    )


@router.get("/top", response_model=RankingTopResponse)
def top(
    challengeId: str = Query(..., min_length=1),
    limit: int = Query(10, ge=1, le=100),
    db: Session = Depends(get_db),
):
    rows = get_top(db, challengeId, limit)
    return RankingTopResponse(
        success=True,
        challengeId=challengeId,
        records=[to_response(row) for row in rows],
    )


@router.get("/my-records", response_model=RankingTopResponse)
def my_records(
    playerId: str = Query(..., min_length=1),
    limit: int = Query(20, ge=1, le=100),
    db: Session = Depends(get_db),
):
    rows = (
        db.query(models.RankingSubmission)
        .filter_by(player_id=playerId)
        .order_by(models.RankingSubmission.created_at.desc())
        .limit(limit)
        .all()
    )
    return RankingTopResponse(
        success=True,
        challengeId="my-records",
        records=[to_response(row) for row in rows],
    )
