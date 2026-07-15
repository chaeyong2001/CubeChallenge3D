import re
from dataclasses import dataclass


MAX_NICKNAME_LENGTH = 15
ALLOWED_NICKNAME_PATTERN = re.compile(r"^[A-Za-z0-9\uAC00-\uD7A3]+$")

RESERVED_NAMES = {
    "admin",
    "administrator",
    "moderator",
    "system",
    "server",
    "gm",
    "player",
    "guest",
    "null",
    "\uC6B4\uC601\uC790",
    "\uAD00\uB9AC\uC790",
    "\uC2DC\uC2A4\uD15C",
}

BANNED_WORDS = {
    "fuck",
    "shit",
    "bitch",
    "bastard",
    "asshole",
    "dick",
    "pussy",
    "cunt",
    "sex",
    "porn",
    "nude",
    "nazi",
    "\uC2DC\uBC1C",
    "\uC2F8\uBC1C",
    "\uBCD1\uC2E0",
    "\uC9C0\uB784",
    "\uC880\uC880",
    "\uB2C8\uBBF8",
    "\uC139\uC2A4",
    "\uC57C\uB3D9",
}


@dataclass(frozen=True)
class NicknameValidationResult:
    valid: bool
    normalized: str
    error_code: str | None
    message: str


def normalize_nickname(nickname: str | None) -> str:
    return "" if nickname is None else nickname.strip()


def nickname_key(nickname: str | None) -> str:
    return normalize_nickname(nickname).casefold()


def validate_nickname(nickname: str | None) -> NicknameValidationResult:
    normalized = normalize_nickname(nickname)
    if not normalized:
        return _invalid(normalized, "empty", "Enter a nickname.")

    if len(normalized) > MAX_NICKNAME_LENGTH:
        return _invalid(normalized, "too_long", "Nickname must be 15 characters or less.")

    if not ALLOWED_NICKNAME_PATTERN.fullmatch(normalized):
        return _invalid(
            normalized,
            "invalid_characters",
            "Only English, Korean, and numbers are allowed.",
        )

    comparable = normalized.casefold()
    if comparable in RESERVED_NAMES:
        return _invalid(normalized, "reserved", "This nickname is reserved.")

    for banned_word in BANNED_WORDS:
        if banned_word and banned_word.casefold() in comparable:
            return _invalid(normalized, "banned_word", "Choose a different nickname.")

    return NicknameValidationResult(True, normalized, None, "")


def _invalid(normalized: str, error_code: str, message: str) -> NicknameValidationResult:
    return NicknameValidationResult(False, normalized, error_code, message)
