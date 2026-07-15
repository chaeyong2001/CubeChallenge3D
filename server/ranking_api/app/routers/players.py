from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from app.cloud_save_service import (
    CloudSaveError,
    download_cloud_save,
    get_cloud_save_status,
    upload_cloud_save,
)
from app.database import get_db
from app.nickname_validator import nickname_key, validate_nickname
from app.player_service import (
    PlayerProfileError,
    create_profile,
    get_profile_by_id,
    get_profile_by_nickname_key,
    link_google,
    link_google_play,
    to_player_profile_response,
    update_avatar,
)
from app.schemas import (
    AccountLinksResponse,
    CloudSaveDownloadResponse,
    CloudSaveStatusResponse,
    CloudSaveUploadRequest,
    CloudSaveUploadResponse,
    GoogleLinkRequest,
    GooglePlayLinkRequest,
    NicknameCheckResponse,
    PlayerAvatarUpdateRequest,
    PlayerProfileCreateRequest,
    PlayerProfileResponse,
)


router = APIRouter(prefix="/players", tags=["players"])


@router.get("/check-nickname", response_model=NicknameCheckResponse)
def check_nickname(
    nickname: str = Query(..., min_length=1),
    db: Session = Depends(get_db),
):
    validation = validate_nickname(nickname)
    if not validation.valid:
        return NicknameCheckResponse(
            available=False,
            valid=False,
            reason=validation.error_code,
            message=validation.message,
        )

    duplicate = get_profile_by_nickname_key(db, nickname_key(validation.normalized))
    if duplicate is not None:
        return NicknameCheckResponse(
            available=False,
            valid=True,
            reason="duplicate",
            message="Nickname is already taken.",
        )

    return NicknameCheckResponse(
        available=True,
        valid=True,
        reason=None,
        message="Nickname is available.",
    )


@router.post("/create", response_model=PlayerProfileResponse)
def create_player_profile(
    payload: PlayerProfileCreateRequest,
    db: Session = Depends(get_db),
):
    try:
        row = create_profile(db, payload)
    except PlayerProfileError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc
    return to_player_profile_response(row)


@router.get("/{profile_id}", response_model=PlayerProfileResponse)
def get_player_profile(profile_id: str, db: Session = Depends(get_db)):
    row = get_profile_by_id(db, profile_id)
    if row is None:
        raise HTTPException(status_code=404, detail="Player profile was not found.")
    return to_player_profile_response(row)


@router.patch("/{profile_id}/avatar", response_model=PlayerProfileResponse)
def update_player_avatar(
    profile_id: str,
    payload: PlayerAvatarUpdateRequest,
    db: Session = Depends(get_db),
):
    try:
        row = update_avatar(db, profile_id, payload.avatarId)
    except PlayerProfileError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc
    return to_player_profile_response(row)


@router.get("/{profile_id}/account-links", response_model=AccountLinksResponse)
def get_account_links(profile_id: str, db: Session = Depends(get_db)):
    row = get_profile_by_id(db, profile_id)
    if row is None:
        raise HTTPException(status_code=404, detail="Player profile was not found.")
    return AccountLinksResponse(
        profileId=row.profile_id,
        googlePlayLinked=bool(row.google_play_player_id),
        googleLinked=bool(row.google_account_id),
    )


@router.post("/{profile_id}/link-google-play", response_model=AccountLinksResponse)
def link_player_google_play(
    profile_id: str,
    payload: GooglePlayLinkRequest,
    db: Session = Depends(get_db),
):
    try:
        row = link_google_play(db, profile_id, payload.googlePlayPlayerId)
    except PlayerProfileError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc
    return AccountLinksResponse(
        profileId=row.profile_id,
        googlePlayLinked=bool(row.google_play_player_id),
        googleLinked=bool(row.google_account_id),
    )


@router.post("/{profile_id}/link-google", response_model=AccountLinksResponse)
def link_player_google(
    profile_id: str,
    payload: GoogleLinkRequest,
    db: Session = Depends(get_db),
):
    try:
        row = link_google(db, profile_id, payload.googleAccountId, payload.googleEmailHash)
    except PlayerProfileError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc
    return AccountLinksResponse(
        profileId=row.profile_id,
        googlePlayLinked=bool(row.google_play_player_id),
        googleLinked=bool(row.google_account_id),
    )


@router.get("/{profile_id}/cloud-save/status", response_model=CloudSaveStatusResponse)
def get_player_cloud_save_status(profile_id: str, db: Session = Depends(get_db)):
    try:
        return get_cloud_save_status(db, profile_id)
    except PlayerProfileError as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc


@router.put("/{profile_id}/cloud-save", response_model=CloudSaveUploadResponse)
def upload_player_cloud_save(
    profile_id: str,
    payload: CloudSaveUploadRequest,
    db: Session = Depends(get_db),
):
    try:
        return upload_cloud_save(db, profile_id, payload)
    except (PlayerProfileError, CloudSaveError) as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc


@router.get("/{profile_id}/cloud-save", response_model=CloudSaveDownloadResponse)
def download_player_cloud_save(profile_id: str, db: Session = Depends(get_db)):
    try:
        return download_cloud_save(db, profile_id)
    except (PlayerProfileError, CloudSaveError) as exc:
        raise HTTPException(status_code=exc.status_code, detail=exc.message) from exc
