import logging
from datetime import datetime, time, timedelta, timezone
from typing import Optional

from fastapi import APIRouter, Depends, Query
from fastapi.responses import JSONResponse
from sqlalchemy.orm import Session

from app import models
from app.database import get_db
from app.ranking_verification import RankingValidationError
from app.ranking_service import create_submission, get_rank_for_submission, get_top, to_response
from app.schemas import (
    RankingRankResponse,
    RankingSubmissionCreate,
    RankingSubmitResponse,
    RankingTopResponse,
    WeeklyRankingRewardClaimRequest,
    WeeklyRankingRewardClaimResponse,
    WeeklyRankingRewardInfoResponse,
    WeeklyRankingRewardResponse,
)


router = APIRouter(prefix="/ranking", tags=["ranking"])
logger = logging.getLogger(__name__)
KST = timezone(timedelta(hours=9))
WEEKLY_REWARD_RULES = {
    1: ("gem", 15),
    2: ("gem", 10),
    3: ("coin", 100),
}


@router.post("/submit", response_model=RankingSubmitResponse)
def submit(payload: RankingSubmissionCreate, db: Session = Depends(get_db)):
    try:
        row, duplicate = create_submission(db, payload)
    except RankingValidationError as exc:
        logger.warning(
            "Rejected ranking submission: submissionId=%s challengeId=%s reason=%s message=%s",
            payload.submissionId,
            payload.challengeId,
            exc.reason,
            exc.message,
        )
        return JSONResponse(
            status_code=422,
            content={
                "success": False,
                "reason": exc.reason,
                "message": exc.message,
            },
        )

    return RankingSubmitResponse(
        success=True,
        isVerified=row.is_verified,
        message="duplicate" if duplicate else "saved",
        duplicate=duplicate,
        submission=to_response(row, db),
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
        records=[to_response(row, db) for row in rows],
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
        .filter(models.RankingSubmission.is_verified.is_(True))
        .order_by(
            models.RankingSubmission.elapsed_seconds.asc(),
            models.RankingSubmission.move_count.asc(),
            models.RankingSubmission.created_at.asc(),
        )
        .limit(limit)
        .all()
    )
    return RankingTopResponse(
        success=True,
        challengeId="my-records",
        records=[to_response(row, db) for row in rows],
    )


@router.get("/my-rank", response_model=RankingRankResponse)
def my_rank(
    challengeId: str = Query(..., min_length=1),
    playerId: str = Query("", min_length=0),
    submissionId: str = Query(..., min_length=1),
    db: Session = Depends(get_db),
):
    rank, row = get_rank_for_submission(db, challengeId, submissionId)
    if row is None or (playerId and row.player_id != playerId):
        return RankingRankResponse(
            success=False,
            message="record_not_found",
            rank=0,
            record=None,
        )

    return RankingRankResponse(
        success=True,
        message="ok",
        rank=rank,
        record=to_response(row, db),
    )


@router.get("/weekly-rewards/info", response_model=WeeklyRankingRewardInfoResponse)
def weekly_rewards_info(db: Session = Depends(get_db)):
    week_start, week_end = get_previous_week_window_kst()
    ensure_weekly_rewards(db, week_start, week_end)
    return WeeklyRankingRewardInfoResponse(
        success=True,
        weekStartKst=format_kst(week_start),
        weekEndKst=format_kst(week_end),
        description="Weekly rankings run from Monday to Sunday.\nRewards are distributed every Monday at 00:00 KST.",
        rewards=[
            "1st Place: 15 Gems",
            "2nd Place: 10 Gems",
            "3rd Place: 100 Coins",
        ],
    )


@router.get("/weekly-rewards/claimable", response_model=WeeklyRankingRewardResponse)
def weekly_rewards_claimable(playerId: str = Query(..., min_length=1), db: Session = Depends(get_db)):
    week_start, week_end = get_previous_week_window_kst()
    ensure_weekly_rewards(db, week_start, week_end)
    reward = (
        db.query(models.WeeklyRankingReward)
        .filter_by(week_start_kst=format_kst(week_start), player_id=playerId)
        .first()
    )
    if reward is None:
        rank_response = weekly_player_rank_to_response(db, week_start, week_end, playerId)
        if rank_response is not None:
            return rank_response
        return WeeklyRankingRewardResponse(exists=False, message="No weekly ranking reward.")

    return weekly_reward_to_response(reward)


@router.post("/weekly-rewards/claim", response_model=WeeklyRankingRewardClaimResponse)
def weekly_rewards_claim(payload: WeeklyRankingRewardClaimRequest, db: Session = Depends(get_db)):
    week_start, week_end = get_previous_week_window_kst()
    ensure_weekly_rewards(db, week_start, week_end)
    requested_week = payload.weekStartKst.strip() if payload.weekStartKst else format_kst(week_start)
    reward = (
        db.query(models.WeeklyRankingReward)
        .filter_by(week_start_kst=requested_week, player_id=payload.playerId)
        .first()
    )
    if reward is None:
        return WeeklyRankingRewardClaimResponse(success=False, claimed=False, message="No weekly ranking reward.")
    if reward.claimed:
        return WeeklyRankingRewardClaimResponse(
            success=False,
            claimed=True,
            message="Weekly ranking reward already claimed.",
            reward=weekly_reward_to_response(reward),
        )

    reward.claimed = True
    reward.claimed_at = datetime.utcnow()
    db.commit()
    db.refresh(reward)
    return WeeklyRankingRewardClaimResponse(
        success=True,
        claimed=True,
        message="claimed",
        reward=weekly_reward_to_response(reward),
    )


def ensure_weekly_rewards(db: Session, week_start_kst: datetime, week_end_kst: datetime) -> None:
    week_start_text = format_kst(week_start_kst)
    existing = (
        db.query(models.WeeklyRankingReward)
        .filter_by(week_start_kst=week_start_text)
        .first()
    )
    if existing is not None:
        return

    start_utc = week_start_kst.astimezone(timezone.utc).replace(tzinfo=None)
    end_utc = week_end_kst.astimezone(timezone.utc).replace(tzinfo=None)
    rows = (
        db.query(models.RankingSubmission)
        .filter(models.RankingSubmission.is_verified.is_(True))
        .filter(models.RankingSubmission.player_id.isnot(None))
        .filter(models.RankingSubmission.created_at >= start_utc)
        .filter(models.RankingSubmission.created_at < end_utc)
        .order_by(
            models.RankingSubmission.elapsed_seconds.asc(),
            models.RankingSubmission.move_count.asc(),
            models.RankingSubmission.created_at.asc(),
        )
        .all()
    )

    winners = []
    seen_players = set()
    for row in rows:
        if not row.player_id or row.player_id in seen_players:
            continue
        seen_players.add(row.player_id)
        winners.append(row)
        if len(winners) >= 3:
            break

    for index, row in enumerate(winners, start=1):
        reward_type, amount = WEEKLY_REWARD_RULES[index]
        db.add(models.WeeklyRankingReward(
            week_start_kst=week_start_text,
            week_end_kst=format_kst(week_end_kst),
            player_id=row.player_id,
            nickname=row.player_name or "Player",
            rank=index,
            reward_type=reward_type,
            reward_amount=amount,
            claimed=False,
        ))

    db.commit()


def weekly_player_rank_to_response(
    db: Session,
    week_start_kst: datetime,
    week_end_kst: datetime,
    player_id: str,
) -> Optional[WeeklyRankingRewardResponse]:
    start_utc = week_start_kst.astimezone(timezone.utc).replace(tzinfo=None)
    end_utc = week_end_kst.astimezone(timezone.utc).replace(tzinfo=None)
    rows = (
        db.query(models.RankingSubmission)
        .filter(models.RankingSubmission.is_verified.is_(True))
        .filter(models.RankingSubmission.player_id.isnot(None))
        .filter(models.RankingSubmission.created_at >= start_utc)
        .filter(models.RankingSubmission.created_at < end_utc)
        .order_by(
            models.RankingSubmission.elapsed_seconds.asc(),
            models.RankingSubmission.move_count.asc(),
            models.RankingSubmission.created_at.asc(),
        )
        .all()
    )

    seen_players = set()
    rank = 0
    for row in rows:
        if not row.player_id or row.player_id in seen_players:
            continue
        seen_players.add(row.player_id)
        rank += 1
        if row.player_id == player_id:
            return WeeklyRankingRewardResponse(
                exists=True,
                claimed=False,
                weekStartKst=format_kst(week_start_kst),
                weekEndKst=format_kst(week_end_kst),
                playerId=row.player_id,
                nickname=row.player_name or "Player",
                rank=rank,
                rewardType="",
                rewardAmount=0,
                message="ranked_no_reward",
            )

    return None


def get_previous_week_window_kst() -> tuple[datetime, datetime]:
    now_kst = datetime.now(KST)
    current_week_start = datetime.combine(
        (now_kst - timedelta(days=now_kst.weekday())).date(),
        time.min,
        tzinfo=KST,
    )
    previous_week_start = current_week_start - timedelta(days=7)
    return previous_week_start, current_week_start


def format_kst(value: datetime) -> str:
    return value.astimezone(KST).strftime("%Y-%m-%dT%H:%M:%S%z")


def weekly_reward_to_response(reward: models.WeeklyRankingReward) -> WeeklyRankingRewardResponse:
    return WeeklyRankingRewardResponse(
        exists=True,
        claimed=reward.claimed,
        weekStartKst=reward.week_start_kst,
        weekEndKst=reward.week_end_kst,
        playerId=reward.player_id,
        nickname=reward.nickname,
        rank=reward.rank,
        rewardType=reward.reward_type,
        rewardAmount=reward.reward_amount,
        message="claimed" if reward.claimed else "claimable",
    )
