from datetime import datetime

from sqlalchemy import Boolean, Column, DateTime, Float, ForeignKey, Index, Integer, String, Text, UniqueConstraint

from app.database import Base


class RankingSubmission(Base):
    __tablename__ = "ranking_submissions"

    id = Column(Integer, primary_key=True, index=True)
    submission_id = Column(String(80), unique=True, nullable=False, index=True)
    challenge_id = Column(String(80), nullable=False, index=True)
    player_id = Column(String(120), nullable=True, index=True)
    player_name = Column(String(80), nullable=False, default="Player")
    avatar_id = Column(Integer, nullable=True)
    elapsed_seconds = Column(Float, nullable=False)
    move_count = Column(Integer, nullable=False)
    scramble_notation = Column(Text, nullable=False)
    move_log_notation = Column(Text, nullable=False)
    control_mode = Column(String(32), nullable=False)
    completed_at_utc = Column(String(64), nullable=False)
    client_version = Column(String(64), nullable=True)
    device_id_hash = Column(String(160), nullable=True)
    is_verified = Column(Boolean, nullable=False, default=False)
    verify_reason = Column(String(240), nullable=True)
    created_at = Column(DateTime, nullable=False, default=datetime.utcnow)


class PlayerProfile(Base):
    __tablename__ = "player_profiles"

    id = Column(Integer, primary_key=True, index=True)
    profile_id = Column(String(120), unique=True, nullable=False, index=True)
    nickname = Column(String(40), unique=True, nullable=False, index=True)
    nickname_normalized = Column(String(40), unique=True, nullable=False, index=True)
    avatar_id = Column(Integer, nullable=False, default=0)
    created_at = Column(DateTime, nullable=False, default=datetime.utcnow)
    updated_at = Column(DateTime, nullable=False, default=datetime.utcnow)
    last_seen_at = Column(DateTime, nullable=True)
    google_play_player_id = Column(String(160), unique=True, nullable=True, index=True)
    google_account_id = Column(String(160), unique=True, nullable=True, index=True)
    google_email_hash = Column(String(160), nullable=True)
    is_banned = Column(Boolean, nullable=False, default=False)
    nickname_change_tickets = Column(Integer, nullable=False, default=0)
    last_nickname_change_at = Column(DateTime, nullable=True)


class PlayerCloudSave(Base):
    __tablename__ = "player_cloud_saves"

    id = Column(Integer, primary_key=True, index=True)
    profile_id = Column(String(120), ForeignKey("player_profiles.profile_id"), unique=True, nullable=False, index=True)
    save_version = Column(Integer, nullable=False, default=1)
    payload_json = Column(Text, nullable=False)
    payload_hash = Column(String(160), nullable=True)
    client_updated_at = Column(DateTime, nullable=True)
    server_updated_at = Column(DateTime, nullable=False, default=datetime.utcnow)
    device_id_hash = Column(String(160), nullable=True)
    app_version = Column(String(64), nullable=True)


class StageProgressRecord(Base):
    __tablename__ = "stage_progress_records"

    id = Column(Integer, primary_key=True, index=True)
    player_id = Column(String(120), ForeignKey("player_profiles.profile_id"), nullable=False, index=True)
    nickname = Column(String(40), nullable=False, index=True)
    profile_image_id = Column(Integer, nullable=False, default=0)
    mode = Column(String(32), nullable=False, index=True)
    cleared_stage = Column(Integer, nullable=False, default=0)
    total_stars = Column(Integer, nullable=False, default=0)
    client_updated_at_utc = Column(String(64), nullable=True)
    created_at = Column(DateTime, nullable=False, default=datetime.utcnow)
    updated_at = Column(DateTime, nullable=False, default=datetime.utcnow)

    __table_args__ = (
        UniqueConstraint("player_id", "mode", name="uq_stage_progress_player_mode"),
    )


Index(
    "ix_ranking_challenge_time_moves",
    RankingSubmission.challenge_id,
    RankingSubmission.elapsed_seconds,
    RankingSubmission.move_count,
)

Index(
    "ix_stage_progress_mode_stage_stars",
    StageProgressRecord.mode,
    StageProgressRecord.cleared_stage.desc(),
    StageProgressRecord.total_stars.desc(),
)


class WeeklyRankingReward(Base):
    __tablename__ = "weekly_ranking_rewards"

    id = Column(Integer, primary_key=True, index=True)
    week_start_kst = Column(String(32), nullable=False, index=True)
    week_end_kst = Column(String(32), nullable=False, index=True)
    player_id = Column(String(120), nullable=False, index=True)
    nickname = Column(String(80), nullable=False, default="Player")
    rank = Column(Integer, nullable=False)
    reward_type = Column(String(16), nullable=False)
    reward_amount = Column(Integer, nullable=False)
    claimed = Column(Boolean, nullable=False, default=False)
    claimed_at = Column(DateTime, nullable=True)
    created_at = Column(DateTime, nullable=False, default=datetime.utcnow)

    __table_args__ = (
        UniqueConstraint("week_start_kst", "player_id", name="uq_weekly_ranking_reward_player_week"),
    )
