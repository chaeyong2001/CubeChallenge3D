from typing import Any, Dict, List, Optional

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
    submissionId: str = ""
    challengeId: str = ""
    playerId: Optional[str] = None
    playerName: str = "Player"
    avatarId: Optional[int] = Field(None, ge=-1, le=3)
    elapsedSeconds: float
    moveCount: int
    scrambleNotation: str = ""
    moveLogNotation: str = ""
    controlMode: str
    completedAtUtc: str = ""
    clientVersion: Optional[str] = None
    deviceIdHash: Optional[str] = None


class RankingSubmissionResponse(BaseModel):
    submissionId: str
    challengeId: str
    playerId: Optional[str]
    playerName: str
    avatarId: Optional[int] = None
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
    duplicate: bool = False
    submission: RankingSubmissionResponse


class RankingTopResponse(BaseModel):
    success: bool
    challengeId: str
    records: List[RankingSubmissionResponse]


class RankingRankResponse(BaseModel):
    success: bool
    message: str
    rank: int = 0
    record: Optional[RankingSubmissionResponse] = None


class WeeklyRankingRewardResponse(BaseModel):
    exists: bool = False
    claimed: bool = False
    weekStartKst: str = ""
    weekEndKst: str = ""
    playerId: str = ""
    nickname: str = ""
    rank: int = 0
    rewardType: str = ""
    rewardAmount: int = 0
    message: str = ""


class WeeklyRankingRewardClaimRequest(BaseModel):
    playerId: str = Field(..., min_length=1, max_length=120)
    weekStartKst: str = ""


class WeeklyRankingRewardClaimResponse(BaseModel):
    success: bool = False
    claimed: bool = False
    message: str = ""
    reward: Optional[WeeklyRankingRewardResponse] = None


class WeeklyRankingRewardInfoResponse(BaseModel):
    success: bool = True
    weekStartKst: str = ""
    weekEndKst: str = ""
    description: str
    rewards: List[str]


class ApiMessageResponse(BaseModel):
    success: bool = False
    message: str


class NicknameCheckResponse(BaseModel):
    available: bool
    valid: bool
    reason: Optional[str] = None
    message: str


class PlayerProfileCreateRequest(BaseModel):
    profileId: str = Field(..., min_length=1, max_length=120)
    nickname: str = Field(..., min_length=1, max_length=40)
    avatarId: int = Field(..., ge=0, le=3)
    googlePlayPlayerId: Optional[str] = None
    googleAccountId: Optional[str] = None
    googleEmailHash: Optional[str] = None


class PlayerAvatarUpdateRequest(BaseModel):
    avatarId: int = Field(..., ge=0, le=3)


class GooglePlayLinkRequest(BaseModel):
    googlePlayPlayerId: str = Field(..., min_length=1, max_length=160)
    displayName: Optional[str] = None


class GoogleLinkRequest(BaseModel):
    googleAccountId: str = Field(..., min_length=1, max_length=160)
    googleEmailHash: Optional[str] = Field(None, max_length=160)


class AccountLinksResponse(BaseModel):
    profileId: str
    googlePlayLinked: bool
    googleLinked: bool


class CloudSaveStatusResponse(BaseModel):
    profileId: str
    exists: bool
    saveVersion: int = 0
    serverUpdatedAt: Optional[str] = None
    payloadHash: Optional[str] = None
    googlePlayLinked: bool = False
    googleLinked: bool = False


class CloudSaveUploadRequest(BaseModel):
    saveVersion: int = Field(1, ge=1)
    payload: Dict[str, Any]
    clientUpdatedAtUtc: Optional[str] = None
    deviceIdHash: Optional[str] = Field(None, max_length=160)
    appVersion: Optional[str] = Field(None, max_length=64)


class CloudSaveUploadResponse(BaseModel):
    success: bool
    profileId: str
    serverUpdatedAt: str
    saveVersion: int
    payloadHash: Optional[str] = None
    overwritten: bool = False


class CloudSaveDownloadResponse(BaseModel):
    profileId: str
    saveVersion: int
    payload: Dict[str, Any]
    serverUpdatedAt: str
    payloadHash: Optional[str] = None


class PlayerProfileResponse(BaseModel):
    profileId: str
    nickname: str
    avatarId: int
    createdAt: str
    updatedAt: str
    linkedGooglePlay: bool
    linkedGoogle: bool


class StageProgressSubmitRequest(BaseModel):
    playerId: str = Field(..., min_length=1, max_length=120)
    nickname: str = Field(..., min_length=1, max_length=40)
    profileImageId: int = Field(0, ge=0, le=99)
    mode: str = Field(..., min_length=1, max_length=32)
    clearedStage: int = Field(..., ge=0)
    totalStars: int = Field(..., ge=0)
    clientUpdatedAtUtc: Optional[str] = None


class StageProgressRecordResponse(BaseModel):
    rank: int
    tied: bool = False
    playerId: str
    nickname: str
    profileImageId: int
    mode: str
    clearedStage: int
    totalStars: int
    updatedAt: str


class StageProgressSubmitResponse(BaseModel):
    success: bool
    message: str
    record: StageProgressRecordResponse


class StageProgressLeaderboardResponse(BaseModel):
    success: bool
    mode: str
    records: List[StageProgressRecordResponse]


class StageProgressMyRankResponse(BaseModel):
    success: bool
    message: str
    record: Optional[StageProgressRecordResponse] = None
