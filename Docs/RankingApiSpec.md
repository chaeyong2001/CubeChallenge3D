# Ranking API Spec

This document prepares the future Railway + FastAPI + PostgreSQL integration.
No server code or Unity network calls are implemented in this step.

## Client Policy

- Gameplay never waits for the server.
- Daily Challenge scramble is generated locally from the UTC date seed.
- Results are shown immediately after the cube is solved.
- Submit failures should create pending submissions for retry.
- Ranking fetch should show cached records first, then refresh when a server response arrives.
- Quick Play and Ranking Challenge must remain playable without a network connection.

## GET /health

Response:

```json
{
  "ok": true,
  "service": "cube-ranking-api"
}
```

Failure response:

```json
{
  "ok": false,
  "message": "service unavailable"
}
```

## GET /challenge/today

The server may later become the authority for challenge date boundaries.
The client currently generates the same values locally from UTC date.

Response:

```json
{
  "challengeId": "daily_2026_06_06",
  "dateUtc": "2026-06-06",
  "seed": 123456789,
  "scrambleLength": 20
}
```

## POST /ranking/submit

Request:

```json
{
  "submissionId": "guid",
  "challengeId": "daily_2026_06_06",
  "playerId": "local-guid-or-account-id",
  "playerName": "Player",
  "elapsedSeconds": 35.24,
  "moveCount": 48,
  "scrambleNotation": "R U F2 ...",
  "moveLogNotation": "U R' ...",
  "controlMode": "Drag",
  "completedAtUtc": "2026-06-06T00:00:00Z",
  "clientVersion": "local"
}
```

Success response:

```json
{
  "success": true,
  "submissionId": "guid",
  "isVerified": true,
  "rank": 12
}
```

Rejected response:

```json
{
  "success": false,
  "isRejected": true,
  "message": "Move log does not solve the scramble."
}
```

## GET /ranking/top?challengeId=daily_2026_06_06&limit=10

Response:

```json
{
  "success": true,
  "challengeId": "daily_2026_06_06",
  "records": [
    {
      "playerName": "Player",
      "elapsedSeconds": 35.24,
      "moveCount": 48,
      "completedAtUtc": "2026-06-06T00:00:00Z"
    }
  ]
}
```

## GET /ranking/my-records?playerId=local-guid-or-account-id

Response:

```json
{
  "success": true,
  "records": []
}
```

## Future Server Notes

- FastAPI should verify submissions server-side by replaying:
  solved cube + scramble moves + user moves => solved.
- PostgreSQL should store submissions with unique `submissionId`.
- The server should reject duplicates and invalid move logs.
- Unity should map server timeout/failure to `Pending`, not block play.
