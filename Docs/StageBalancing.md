# Stage Balancing

Stage data is designed to scale to at least 100 Solve stages and 100 Reverse Target stages.

Rules:
- Do not use 1-4 move stages as real playable stages.
- Use `minimumMoves` as the current balancing estimate. It may be replaced by solver-verified true minimum moves later.
- Keep `moveLimit = minimumMoves + extraMoves`.
- Star limits should usually be:
  - 3 stars: `minimumMoves`
  - 2 stars: `minimumMoves + 2`
  - 1 star: `moveLimit`

Suggested progression:
- Stage 1-10: `minimumMoves` 5-8, `extraMoves` +4 to +5
- Stage 11-30: `minimumMoves` 9-14, `extraMoves` +3 to +4
- Stage 31-60: `minimumMoves` 15-22, `extraMoves` +2 to +3
- Stage 61-85: `minimumMoves` 23-32, `extraMoves` +1 to +2
- Stage 86-100: `minimumMoves` 33+, `extraMoves` +0 to +1

Apply the same progression shape to Reverse Target stages. Generated stages should keep `generatedSeed`, `generationGroup`, `scrambleNotation`, and `solutionNotation` so the generator and later solver verification can reproduce and audit them.
