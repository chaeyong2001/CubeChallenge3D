import argparse
import sqlite3
from pathlib import Path
from typing import Optional


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DB = REPO_ROOT / "server" / "ranking_api" / "ranking_dev.db"
TEST_PREFIX = "test_world_rank_"


def clear_records(db_path: Path, challenge_id: Optional[str]) -> None:
    if not db_path.exists():
        raise FileNotFoundError(f"Database not found: {db_path}")

    with sqlite3.connect(db_path) as connection:
        cursor = connection.cursor()
        if challenge_id:
            cursor.execute(
                """
                DELETE FROM ranking_submissions
                WHERE challenge_id = ?
                  AND (
                    submission_id LIKE ?
                    OR player_id LIKE ?
                    OR device_id_hash LIKE ?
                  )
                """,
                (challenge_id, f"{TEST_PREFIX}%", f"{TEST_PREFIX}%", f"{TEST_PREFIX}%"),
            )
        else:
            cursor.execute(
                """
                DELETE FROM ranking_submissions
                WHERE submission_id LIKE ?
                   OR player_id LIKE ?
                   OR device_id_hash LIKE ?
                """,
                (f"{TEST_PREFIX}%", f"{TEST_PREFIX}%", f"{TEST_PREFIX}%"),
            )
        deleted = cursor.rowcount
        connection.commit()

    print(f"db={db_path}")
    print(f"deleted={deleted}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Remove seeded world ranking test records.")
    parser.add_argument("--db", default=str(DEFAULT_DB), help="Path to ranking_dev.db")
    parser.add_argument("--challenge-id", default=None, help="Optional challenge ID to clear")
    args = parser.parse_args()

    clear_records(Path(args.db).resolve(), args.challenge_id)


if __name__ == "__main__":
    main()
