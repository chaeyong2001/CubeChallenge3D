from dataclasses import dataclass
from typing import List

from app.schemas import RankingSubmissionCreate


VALID_MOVES = {
    "R", "R'", "R2",
    "L", "L'", "L2",
    "U", "U'", "U2",
    "D", "D'", "D2",
    "F", "F'", "F2",
    "B", "B'", "B2",
}


@dataclass
class VerificationResult:
    is_verified: bool
    reason: str
    scramble_count: int = 0
    move_count: int = 0


def parse_notation(value: str) -> List[str]:
    if not value or not value.strip():
        return []
    tokens = value.split()
    invalid = [token for token in tokens if token not in VALID_MOVES]
    if invalid:
        raise ValueError(f"Invalid move token: {invalid[0]}")
    return tokens


def verify_submission(payload: RankingSubmissionCreate) -> VerificationResult:
    if not payload.challengeId.strip():
        return VerificationResult(False, "challengeId is required")
    if payload.elapsedSeconds < 1.0:
        return VerificationResult(False, "elapsedSeconds must be at least 1 second")
    if payload.moveCount <= 0:
        return VerificationResult(False, "moveCount must be greater than zero")
    if payload.controlMode not in {"Drag", "Keypad"}:
        return VerificationResult(False, "controlMode must be Drag or Keypad")

    try:
        scramble = parse_notation(payload.scrambleNotation)
        moves = parse_notation(payload.moveLogNotation)
    except ValueError as exc:
        return VerificationResult(False, str(exc))

    if not scramble:
        return VerificationResult(False, "scrambleNotation is required")
    if not moves:
        return VerificationResult(False, "moveLogNotation is required", len(scramble), 0)
    if payload.moveCount != len(moves):
        return VerificationResult(
            False,
            "moveCount does not match moveLogNotation count",
            len(scramble),
            len(moves),
        )

    # Full cube-state replay verification is intentionally deferred. The Unity client
    # already records scramble + move log so this can be upgraded server-side later.
    return VerificationResult(True, "basic verification passed", len(scramble), len(moves))
