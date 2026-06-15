# Stage Generation Plan

## Pack

- Generator: `StagePackGenerator`
- Solve seed: `15001`
- Reverse Target seed: `15101`
- Output: `Assets/Resources/Stages/stages_generated.json`
- Content: 100 Solve stages and 100 Reverse Target stages
- Stable IDs: `solve_001` to `solve_100`, `target_001` to `target_100`
- Stage numbers: Solve `1-100`, Target `101-200`

The same seeds produce the same move sequences and serialized cube states.

## Move rules

- Allowed faces: `U D R L F B`
- Allowed turns: clockwise, counter-clockwise, and 180 degrees
- No middle-layer moves
- The same face is not generated consecutively
- Consecutive moves on the same axis are strongly discouraged
- Duplicate notation and duplicate generated states are rejected
- States solvable in four moves or fewer are rejected

## Difficulty curve

- Stages 1-10: 5-7 moves, onboarding
- Stages 11-30: 7-10 moves
- Stages 31-60: 10-14 moves
- Stages 61-100: 14-20 moves
- Reverse Target uses a slightly gentler upper range

`minimumMoves` currently represents the generated reference solution length.
The generator additionally proves that each generated state requires at least
five moves. Exact optimal lengths can be recalculated later during balance
tuning if an optimal-search pass is added.

## Limits and stars

- `moveLimit = minimumMoves + extraMoves`
- Early stages receive the largest margin
- Later stages receive a four-to-six move margin
- Three-star, two-star, and one-star thresholds are generated in ascending
  order and never exceed `moveLimit`
- Existing assist-use star restrictions remain in the stage runtime

## Stage types

- Solve starts from `startStateFacelets` and targets solved state.
- Reverse Target starts solved and targets `targetStateFacelets`.
- Solve stores the inverse scramble as `solutionNotation`.
- Reverse Target stores the target-building sequence as `solutionNotation`.
- Full solution notation remains data for validation, hints, and development;
  normal stage UI does not expose it as the answer.

## Milestones

Stage numbers are contiguous from 1 through 200. Existing milestone blocks
therefore contain exactly ten stages:

- Solve blocks: `1-10` through `91-100`
- Target blocks: `101-110` through `191-200`
- Each block supports 30 total stars and the existing one-time gem claim

## Validation

- Unique ID and stage number
- Minimum five moves
- Valid move and star limits
- Parseable notation
- Unique serialized state within each stage type
- Stored solution recreates or solves the serialized state
- Exactly ten stages per milestone block in the generated pack

Balance values are deterministic defaults and can be regenerated after
playtesting without changing stable stage IDs.
