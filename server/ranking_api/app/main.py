from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.config import settings
from app.database import init_db
from app.routers import challenge, health, iap, players, ranking, stage_records


app = FastAPI(title="CubeChallenge3D Ranking API", version="0.1.0")

origins = ["*"] if settings.cors_origins == "*" else settings.cors_origins.split(",")
app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.on_event("startup")
def startup() -> None:
    init_db()


app.include_router(health.router)
app.include_router(challenge.router)
app.include_router(ranking.router)
app.include_router(stage_records.router)
app.include_router(players.router)
app.include_router(iap.router)
