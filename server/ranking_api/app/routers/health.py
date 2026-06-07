from fastapi import APIRouter

from app.config import settings
from app.schemas import HealthResponse


router = APIRouter()


@router.get("/health", response_model=HealthResponse)
def health():
    return HealthResponse(ok=True, service=settings.app_name)
