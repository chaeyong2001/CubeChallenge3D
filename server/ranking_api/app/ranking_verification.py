import logging
import re
from dataclasses import dataclass
from typing import List

from app.schemas import RankingSubmissionCreate


MIN_ELAPSED_SECONDS = 3.0
MAX_ELAPSED_SECONDS = 3600.0
MIN_MOVE_COUNT = 1
MAX_MOVE_COUNT = 1000
MAX_NOTATION_LENGTH = 20000
MAX_PLAYER_NAME_LENGTH = 32
MAX_SUBMISSION_ID_LENGTH = 80
MAX_CHALLENGE_ID_LENGTH = 80
DAILY_CHALLENGE_PATTERN = re.compile(r"^daily_\d{4}_\d{2}_\d{2}$")

VALID_MOVES = {
    "R", "R'", "R2",
    "L", "L'", "L2",
    "U", "U'", "U2",
    "D", "D'", "D2",
    "F", "F'", "F2",
    "B", "B'", "B2",
}

logger = logging.getLogger(__name__)


class RankingValidationError(ValueError):
    def __init__(self, reason: str, message: str):
        super().__init__(message)
        self.reason = reason
        self.message = message


@dataclass
class VerificationResult:
    is_verified: bool
    reason: str
    scramble_count: int = 0
    move_count: int = 0
    solved_checked: bool = False


def parse_notation(value: str) -> List[str]:
    if not value or not value.strip():
        return []
    tokens = value.split()
    invalid = [token for token in tokens if token not in VALID_MOVES]
    if invalid:
        raise RankingValidationError(
            "invalid_move_token",
            f"Invalid move token: {invalid[0]}",
        )
    return tokens


def validate_move_tokens(value: str, field_name: str) -> List[str]:
    if value is None or not value.strip():
        raise RankingValidationError(
            f"{field_name}_required",
            f"{field_name} is required.",
        )
    if len(value) > MAX_NOTATION_LENGTH:
        raise RankingValidationError(
            f"{field_name}_too_long",
            f"{field_name} is too long.",
        )
    return parse_notation(value)


def count_moves(value: str) -> int:
    return len(parse_notation(value))


def verify_submission_basic(payload: RankingSubmissionCreate) -> VerificationResult:
    submission_id = (payload.submissionId or "").strip()
    if not submission_id:
        raise RankingValidationError("missing_submission_id", "submissionId is required.")
    if len(submission_id) > MAX_SUBMISSION_ID_LENGTH:
        raise RankingValidationError("submission_id_too_long", "submissionId is too long.")

    challenge_id = (payload.challengeId or "").strip()
    if not challenge_id:
        raise RankingValidationError("missing_challenge_id", "challengeId is required.")
    if len(challenge_id) > MAX_CHALLENGE_ID_LENGTH:
        raise RankingValidationError("challenge_id_too_long", "challengeId is too long.")
    if challenge_id.startswith("daily_") and not DAILY_CHALLENGE_PATTERN.match(challenge_id):
        raise RankingValidationError("invalid_challenge", "Daily challenge id format is invalid.")

    player_name = (payload.playerName or "").strip()
    if not player_name:
        raise RankingValidationError("missing_player_name", "playerName is required.")
    if len(player_name) > MAX_PLAYER_NAME_LENGTH:
        raise RankingValidationError("player_name_too_long", "playerName is too long.")

    if payload.elapsedSeconds < MIN_ELAPSED_SECONDS:
        raise RankingValidationError(
            "elapsed_too_low",
            "Elapsed time is too low for a valid ranking submission.",
        )
    if payload.elapsedSeconds > MAX_ELAPSED_SECONDS:
        raise RankingValidationError(
            "elapsed_too_high",
            "Elapsed time is too high for a valid ranking submission.",
        )

    if payload.moveCount < MIN_MOVE_COUNT:
        raise RankingValidationError("move_count_invalid", "moveCount must be at least 1.")
    if payload.moveCount > MAX_MOVE_COUNT:
        raise RankingValidationError("move_count_too_high", "moveCount is too high.")

    if payload.controlMode not in {"Drag", "Keypad"}:
        raise RankingValidationError("invalid_control_mode", "controlMode must be Drag or Keypad.")

    scramble = validate_move_tokens(payload.scrambleNotation, "scrambleNotation")
    moves = validate_move_tokens(payload.moveLogNotation, "moveLogNotation")
    if not scramble:
        raise RankingValidationError("missing_scramble", "scrambleNotation is required.")
    if not moves:
        raise RankingValidationError("missing_move_log", "moveLogNotation is required.")
    if payload.moveCount != len(moves):
        raise RankingValidationError(
            "move_count_mismatch",
            "moveCount does not match moveLogNotation count.",
        )

    return VerificationResult(True, "basic verification passed", len(scramble), len(moves))


def verify_solved_if_possible(payload: RankingSubmissionCreate) -> VerificationResult:
    # TODO: Add full cube-state replay verification after the server cube model is ported.
    return verify_submission_basic(payload)


def verify_submission(payload: RankingSubmissionCreate) -> VerificationResult:
    result = verify_solved_if_possible(payload)
    logger.debug(
        "Ranking submission verified: submissionId=%s challengeId=%s scramble=%s moves=%s solvedChecked=%s",
        payload.submissionId,
        payload.challengeId,
        result.scramble_count,
        result.move_count,
        result.solved_checked,
    )
    return result
