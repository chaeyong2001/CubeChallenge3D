# CubeChallenge3D Ranking API

FastAPI ranking prototype for CubeChallenge3D. This service handles ranking submit/fetch only. Gameplay and daily scramble generation remain local-first in Unity.

## Local Run

```bash
cd server/ranking_api
python -m pip install --upgrade pip
pip install -r requirements.txt
uvicorn app.main:app --reload
```

If `DATABASE_URL` is missing, the app uses a local SQLite file for development only. Railway should use PostgreSQL through `DATABASE_URL`.

PostgreSQL uses psycopg v3. Railway may provide a URL like:

```text
postgresql://user:password@host:5432/database
```

The app converts it internally to SQLAlchemy's psycopg v3 dialect:

```text
postgresql+psycopg://user:password@host:5432/database
```

This avoids the Windows `pg_config executable not found` issue from psycopg2 source builds.

## Railway Setup

1. Create a new Railway project for CubeChallenge3D.
2. Add a PostgreSQL database.
3. Confirm `DATABASE_URL` is available to the web service.
4. Deploy this `server/ranking_api` folder.
5. Use this start command:

```bash
uvicorn app.main:app --host 0.0.0.0 --port $PORT
```

## Endpoints

- `GET /health`
- `GET /challenge/today`
- `POST /ranking/submit`
- `GET /ranking/top?challengeId=daily_2026_06_06&limit=10`
- `GET /ranking/my-records?playerId=...`

## Submit Example

```json
{
  "submissionId": "guid",
  "challengeId": "daily_2026_06_06",
  "playerId": "local-guid",
  "playerName": "Player",
  "elapsedSeconds": 35.24,
  "moveCount": 48,
  "scrambleNotation": "R U F2",
  "moveLogNotation": "F2 U' R'",
  "controlMode": "Drag",
  "completedAtUtc": "2026-06-06T00:00:00Z",
  "clientVersion": "local",
  "deviceIdHash": "optional"
}
```

## Verification

Current server verification is intentionally basic:

- non-empty challenge id
- elapsed time at least 1 second
- move count greater than zero
- valid move notation tokens
- move count matches move log token count
- control mode is `Drag` or `Keypad`

Full cube-state replay verification should be added before production ranking. The stored scramble and move log are designed for that upgrade.

## Production Notes

Before public release, add:

- rate limiting
- authentication or device signing
- full server-side cube verification
- duplicate submission hardening
- Alembic migrations
- restricted CORS origins
