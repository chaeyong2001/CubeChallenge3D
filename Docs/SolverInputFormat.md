# Solver Input Format

Manual solver input uses 54 facelets in `URFDLB` order.

Face names:

```text
U = Up
R = Right
F = Front
D = Down
L = Left
B = Back
```

Each face has 9 cells indexed:

```text
0 1 2
3 4 5
6 7 8
```

Global index is `faceIndex * 9 + cellIndex`.

Face order:

```text
U: indexes 0-8
R: indexes 9-17
F: indexes 18-26
D: indexes 27-35
L: indexes 36-44
B: indexes 45-53
```

The solver facelet string is generated from center colors. For example, every facelet with the same color as the U center becomes `U`. Full cube legality validation is intentionally deferred until the solver engine integration step.
