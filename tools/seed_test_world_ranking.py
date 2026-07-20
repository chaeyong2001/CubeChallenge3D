import argparse
import shutil
import sqlite3
from datetime import UTC, datetime, timedelta
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DB = REPO_ROOT / "server" / "ranking_api" / "ranking_dev.db"
TEST_PREFIX = "test_world_rank_"


def default_challenge_id() -> str:
    return f"daily_{datetime.now(UTC):%Y_%m_%d}"


def backup_database(db_path: Path) -> None:
    backup_path = db_path.with_name(f"{db_path.stem}.before_test_world_ranking_seed.bak")
    if backup_path.exists():
        return
    shutil.copy2(db_path, backup_path)
    print(f"backup={backup_path}")


def seed_records(db_path: Path, challenge_id: str, count: int) -> None:
    if not db_path.exists():
        raise FileNotFoundError(f"Database not found: {db_path}")

    backup_database(db_path)
    now = datetime.now(UTC).replace(tzinfo=None)
    scramble = "R U R' U' F2 L D2 B' R2 U2 L' F R U' B2 D L2 U F' R'"
    move_log = "R U R' U' F2"

    with sqlite3.connect(db_path) as connection:
        cursor = connection.cursor()
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

        rows = []
        for index in range(1, count + 1):
            elapsed_seconds = round(28.75 + (index * 1.43), 3)
            move_count = 18 + (index % 37)
            completed_at = now - timedelta(minutes=count - index)
            rows.append(
                (
                    f"{TEST_PREFIX}{challenge_id}_{index:03d}",
                    challenge_id,
                    f"{TEST_PREFIX}player_{index:03d}",
                    f"WorldTest{index:03d}",
                    index % 4,
                    elapsed_seconds,
                    move_count,
                    scramble,
                    move_log,
                    "Touch",
                    completed_at.isoformat(timespec="seconds") + "Z",
                    "test-seed",
                    f"{TEST_PREFIX}device_{index:03d}",
                    1,
                    "test_seed",
                    completed_at.isoformat(timespec="seconds"),
                )
            )

        cursor.executemany(
            """
            INSERT INTO ranking_submissions (
                submission_id,
                challenge_id,
                player_id,
                player_name,
                avatar_id,
                elapsed_seconds,
                move_count,
                scramble_notation,
                move_log_notation,
                control_mode,
                completed_at_utc,
                client_version,
                device_id_hash,
                is_verified,
                verify_reason,
                created_at
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            rows,
        )
        connection.commit()

        cursor.execute(
            """
            SELECT COUNT(*)
            FROM ranking_submissions
            WHERE challenge_id = ?
              AND submission_id LIKE ?
            """,
            (challenge_id, f"{TEST_PREFIX}%"),
        )
        seeded_count = cursor.fetchone()[0]

    print(f"db={db_path}")
    print(f"challengeId={challenge_id}")
    print(f"seeded={seeded_count}")
    print(f"deleteCommand=python tools/clear_test_world_ranking.py --challenge-id {challenge_id}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Seed 80 removable world ranking test records.")
    parser.add_argument("--db", default=str(DEFAULT_DB), help="Path to ranking_dev.db")
    parser.add_argument("--challenge-id", default=default_challenge_id(), help="Challenge ID to seed")
    parser.add_argument("--count", type=int, default=80, help="Number of records to seed")
    args = parser.parse_args()

    seed_records(Path(args.db).resolve(), args.challenge_id, max(1, args.count))


if __name__ == "__main__":
    main()
