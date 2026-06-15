# Stage UI, Progress, and Rewards

## Navigation

- Stage Mode opens on Solve Stages.
- Solve and Target tabs are independent.
- Each mode contains ten blocks.
- Opening a block creates only its ten stage tiles.
- Stage 1 of each mode is unlocked by default.
- Clearing a stage unlocks the next stage.
- A new block opens after all preceding stages have been cleared through the
  normal sequential unlock flow.

## Progress

- Progress is stored by stable `stageId`.
- Each mode displays cleared stages and total stars.
- Tiles display local stage number, best stars, difficulty, move limit, and
  lock/clear state.
- Saved best stars use the highest result across all clears.

## Milestones

- Every ten-stage block has a 30-star milestone.
- Thirty stars are a reward goal, not a progression lock.
- A completed milestone awards five gems once.
- Claimed state remains protected by `StageMilestoneRewardStore`.

## Clear rewards

- Three stars award the full `StageData.rewardCoins`.
- Two stars award 75 percent.
- One star awards 50 percent.
- One or two counted assists cap the result at two stars.
- Three or more counted assists cap the result at one star.
- Undo does not count toward the assist star cap.

## Balance validation

Use `Tools/CubeChallenge3D/Validate Stage Balance`.

The validator checks onboarding move ranges and margins, all move/star limit
ordering, reward ranges by difficulty, ten stages per block, and a
non-decreasing average difficulty curve.
