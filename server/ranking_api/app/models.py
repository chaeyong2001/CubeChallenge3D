from datetime import datetime

from sqlalchemy import Boolean, Column, DateTime, Float, Index, Integer, String, Text

from app.database import Base


class RankingSubmission(Base):
    __tablename__ = "ranking_submissions"

    id = Column(Integer, primary_key=True, index=True)
    submission_id = Column(String(80), unique=True, nullable=False, index=True)
    challenge_id = Column(String(80), nullable=False, index=True)
    player_id = Column(String(120), nullable=True, index=True)
    player_name = Column(String(80), nullable=False, default="Player")
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


Index(
    "ix_ranking_challenge_time_moves",
    RankingSubmission.challenge_id,
    RankingSubmission.elapsed_seconds,
    RankingSubmission.move_count,
)
