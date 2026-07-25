import os


def normalize_database_url(value: str) -> str:
    # Railway commonly provides postgresql:// URLs. Use SQLAlchemy's explicit
    # psycopg v3 dialect so Windows installs do not need psycopg2/pg_config.
    if value.startswith("postgresql+psycopg://"):
        return value
    if value.startswith("postgresql://"):
        return value.replace("postgresql://", "postgresql+psycopg://", 1)
    if value.startswith("postgres://"):
        return value.replace("postgres://", "postgresql+psycopg://", 1)
    return value


class Settings:
    app_name = "cube-ranking-api"
    scramble_length = 20
    cors_origins = os.getenv("CORS_ORIGINS", "*")
    database_url = os.getenv("DATABASE_URL", "sqlite:///./ranking_dev.db")
    google_play_package_name = os.getenv("GOOGLE_PLAY_PACKAGE_NAME", "com.FAMLEE.CubeChallenge3D")
    google_play_service_account_json = os.getenv("GOOGLE_PLAY_SERVICE_ACCOUNT_JSON", "")
    google_play_service_account_file = os.getenv("GOOGLE_PLAY_SERVICE_ACCOUNT_FILE", "")
    iap_admin_secret = os.getenv("IAP_ADMIN_SECRET", "")
    google_play_voided_sync_enabled = os.getenv("GOOGLE_PLAY_VOIDED_SYNC_ENABLED", "false").lower() == "true"

    @property
    def sqlalchemy_database_url(self) -> str:
        # The sqlite fallback is development-only and should not be used for production.
        return normalize_database_url(self.database_url)


settings = Settings()
