# Solver Engine Plan

Step 12 created the manual input screen and basic color-count validation.

Step 13-A added the solver adapter boundary:

- `ISolverEngine`
- `SolverRequest`
- `SolverValidationResult`
- `SolverResult`
- `SolverSolution`
- `PlaceholderSolverEngine`

Manual input produces a 54-character facelet string in `URFDLB` order. The placeholder engine performs basic validation only: length, allowed characters, count of 9 per face character, and centers at indexes `4,13,22,31,40,49`.

Full cubie legality validation is deferred to 13-B, where the real compatible two-phase solver integration should detect impossible states such as invalid edge/corner pieces, flip errors, twist errors, and parity errors.

Step 13-B adds an internal solver engine:

- `RealSolverEngine`
- cubie piece-set validation for corners and edges
- corner/edge permutation parity validation
- bounded IDDFS search using the existing `CubeState.ApplyMove` model
- timeout and depth-limit failure results
- recent `SolverSolution` save for later playback

Current limits:

- This is not an optimal two-phase solver.
- Search is capped to a small depth to avoid freezing Unity.
- Deep random cubes may return `SolutionNotFound` or `Timeout`.
- Twist/flip validation error codes are prepared, but full orientation legality should be strengthened with the future full cubie/two-phase model.

Step 14 will use `SolverSolution` for 3D playback.

Step 13-I added a replacement-ready engine selection layer:

- `SolverEngineProvider`
- fallback order: high-performance adapter, `RealSolverEngine`, then `PlaceholderSolverEngine`
- valid states beyond the fallback search depth return `CurrentSolverLimitation`, not `Invalid`
- debug reporting includes the active engine, fallback reason, elapsed time, depth, timeout, and searched nodes
- the latest debug facelet case is saved locally for repeatable self-checks
- high-performance source code was deferred until license review completed

License notes:

- Do not directly include GPL solver code unless the project license strategy explicitly accepts it.
- Prefer MIT/Apache/permissive compatible code or a clean internal implementation.
- If external solver code is used later, keep it isolated behind `ISolverEngine` and include required license notices.
- See `Docs/SolverEngineLicenseReview.md` for candidate review notes.

Step 13-J connects the production two-phase engine:

- Current active preferred engine: `TwoPhaseSolverEngine`
- Source: `tremwil/TwoPhaseSolver`, imported commit
  `0f6a2662693cdd4605a8418758a2c542b2afdbed`
- License: MIT
- Runtime source: `Assets/ThirdParty/Solver/TwoPhaseSolver`
- Precomputed tables: `Assets/Resources/TwoPhaseSolverTables`
- Adapter: `Assets/Scripts/Solver/Engine/HighPerformance/TwoPhaseSolverEngine.cs`
- Facelet conversion: app `URFDLB` 54 facelets to the solver's non-center
  `U,R,F,L,B,D` 48 facelets
- Returned moves are normalized and reapplied to the source `CubeState`; an
  unverified solution is rejected.
- If table initialization fails, the provider keeps the existing internal
  fallback chain.
