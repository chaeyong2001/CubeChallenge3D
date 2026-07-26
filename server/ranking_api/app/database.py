from sqlalchemy import create_engine, inspect, text
from sqlalchemy.orm import declarative_base, sessionmaker

from app.config import settings


database_url = settings.sqlalchemy_database_url

engine = create_engine(
    database_url,
    pool_pre_ping=True,
    connect_args={"check_same_thread": False}
    if database_url.startswith("sqlite")
    else {},
)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def init_db() -> None:
    # Good enough for the first Railway prototype. Introduce Alembic before production.
    Base.metadata.create_all(bind=engine)
    ensure_runtime_columns()


def ensure_runtime_columns() -> None:
    inspector = inspect(engine)
    table_names = inspector.get_table_names()
    with engine.begin() as connection:
        if "ranking_submissions" in table_names:
            ranking_columns = {column["name"] for column in inspector.get_columns("ranking_submissions")}
            if "avatar_id" not in ranking_columns:
                connection.execute(text("ALTER TABLE ranking_submissions ADD COLUMN avatar_id INTEGER"))

        if "player_profiles" in table_names:
            profile_columns = {column["name"] for column in inspector.get_columns("player_profiles")}
            if "google_play_player_id" not in profile_columns:
                connection.execute(text("ALTER TABLE player_profiles ADD COLUMN google_play_player_id VARCHAR(160)"))
            if "google_account_id" not in profile_columns:
                connection.execute(text("ALTER TABLE player_profiles ADD COLUMN google_account_id VARCHAR(160)"))
            if "google_email_hash" not in profile_columns:
                connection.execute(text("ALTER TABLE player_profiles ADD COLUMN google_email_hash VARCHAR(160)"))

        if "iap_purchases" in table_names:
            iap_columns = {column["name"] for column in inspector.get_columns("iap_purchases")}
            if "granted_gems" not in iap_columns:
                connection.execute(text("ALTER TABLE iap_purchases ADD COLUMN granted_gems INTEGER NOT NULL DEFAULT 0"))
            if "remaining_gems" not in iap_columns:
                connection.execute(text("ALTER TABLE iap_purchases ADD COLUMN remaining_gems INTEGER NOT NULL DEFAULT 0"))
            if "used_gems" not in iap_columns:
                connection.execute(text("ALTER TABLE iap_purchases ADD COLUMN used_gems INTEGER NOT NULL DEFAULT 0"))
            if "refundable_status" not in iap_columns:
                connection.execute(text("ALTER TABLE iap_purchases ADD COLUMN refundable_status VARCHAR(32) NOT NULL DEFAULT 'unused'"))
