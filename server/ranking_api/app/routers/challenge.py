from datetime import datetime, timezone
from hashlib import sha256

from fastapi import APIRouter

from app.config import settings
from app.schemas import ChallengeTodayResponse


router = APIRouter(prefix="/challenge", tags=["challenge"])


def seed_for_date(date_text: str) -> int:
    digest = sha256(date_text.encode("utf-8")).digest()
    return int.from_bytes(digest[:4], "big") & 0x7FFFFFFF


@router.get("/today", response_model=ChallengeTodayResponse)
def today():
    today_utc = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    challenge_id = f"daily_{today_utc.replace('-', '_')}"
    return ChallengeTodayResponse(
        challengeId=challenge_id,
        dateUtc=today_utc,
        seed=seed_for_date(today_utc),
        scrambleLength=settings.scramble_length,
    )
