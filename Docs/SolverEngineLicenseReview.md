# Solver Engine License Review

Date: 2026-06-11

## Included solver

- Name: tremwil/TwoPhaseSolver
- Source: https://github.com/tremwil/TwoPhaseSolver
- Imported commit: `0f6a2662693cdd4605a8418758a2c542b2afdbed`
- Language: C#
- License: MIT
- Copyright: Copyright (c) 2019 William Tremblay
- Commercial use: permitted by the MIT license
- Notice requirement: retain the copyright and MIT permission notice
- Source location: `Assets/ThirdParty/Solver/TwoPhaseSolver`
- Table location: `Assets/Resources/TwoPhaseSolverTables`

The repository `LICENSE` file was checked before inclusion. The complete license is included as
`Assets/ThirdParty/Solver/TwoPhaseSolver/LICENSE.txt`.

## Unity adaptation

- Runtime C# files and precomputed solver tables are included.
- Console test projects and Visual Studio project files are excluded.
- `BinLoad` was changed to load table data through Unity `Resources`.
- The application adapter remains separate under
  `Assets/Scripts/Solver/Engine/HighPerformance`.

## Runtime policy

1. Use `TwoPhaseSolverEngine` when its tables initialize successfully.
2. Fall back to the internal `RealSolverEngine` if initialization fails.
3. Use `PlaceholderSolverEngine` only if both runtime engines are unavailable.

No GPL or license-unclear solver source is included.
