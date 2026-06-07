from typing import List, Optional

from pydantic import BaseModel, Field


class HealthResponse(BaseModel):
    ok: bool
    service: str


class ChallengeTodayResponse(BaseModel):
    challengeId: str
    dateUtc: str
    seed: int
    scrambleLength: int


class RankingSubmissionCreate(BaseModel):
    submissionId: str = Field(..., min_length=1)
    challengeId: str = Field(..., min_length=1)
    playerId: Optional[str] = None
    playerName: str = "Player"
    elapsedSeconds: float
    moveCount: int
    scrambleNotation: str = Field(..., min_length=1)
    moveLogNotation: str = Field(..., min_length=1)
    controlMode: str
    completedAtUtc: str
    clientVersion: Optional[str] = None
    deviceIdHash: Optional[str] = None


class RankingSubmissionResponse(BaseModel):
    submissionId: str
    challengeId: str
    playerId: Optional[str]
    playerName: str
    elapsedSeconds: float
    moveCount: int
    scrambleNotation: str
    moveLogNotation: str
    controlMode: str
    completedAtUtc: str
    clientVersion: Optional[str]
    deviceIdHash: Optional[str]
    isVerified: bool
    verifyReason: Optional[str]
    createdAt: str


class RankingSubmitResponse(BaseModel):
    success: bool
    isVerified: bool
    message: str
    submission: RankingSubmissionResponse


class RankingTopResponse(BaseModel):
    success: bool
    challengeId: str
    records: List[RankingSubmissionResponse]


class ApiMessageResponse(BaseModel):
    success: bool = False
    message: str
