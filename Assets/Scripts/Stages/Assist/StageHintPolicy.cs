using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Assist
{
    public static class StageHintPolicy
    {
        public static StageHintResult BuildHint(StageData stage, IReadOnlyList<CubeMove> userMoves, int remainingMoves)
        {
            var result = new StageHintResult
            {
                remainingMoves = remainingMoves,
                nextMoveNotation = "-",
                message = "No hint available yet."
            };

            if (stage == null || string.IsNullOrWhiteSpace(stage.solutionNotation))
            {
                return result;
            }

            IReadOnlyList<CubeMove> solutionMoves;
            try
            {
                solutionMoves = MoveUtility.ParseSequence(stage.solutionNotation);
            }
            catch
            {
                return result;
            }

            int usedCount = userMoves != null ? userMoves.Count : 0;
            for (int i = 0; i < usedCount; i++)
            {
                if (i >= solutionMoves.Count || !userMoves[i].Equals(solutionMoves[i]))
                {
                    result.hasHint = true;
                    result.message = "Off path. Undo or Retry may help.";
                    result.estimatedMovesToGoal = solutionMoves.Count - i;
                    result.canClearWithinRemainingMoves = result.estimatedMovesToGoal <= remainingMoves;
                    result.shortageMoves = result.canClearWithinRemainingMoves ? 0 : result.estimatedMovesToGoal - remainingMoves;
                    return result;
                }
            }

            if (usedCount < solutionMoves.Count)
            {
                result.hasHint = true;
                result.nextMoveNotation = solutionMoves[usedCount].ToString();
                result.estimatedMovesToGoal = solutionMoves.Count - usedCount;
                result.canClearWithinRemainingMoves = result.estimatedMovesToGoal <= remainingMoves;
                result.shortageMoves = result.canClearWithinRemainingMoves ? 0 : result.estimatedMovesToGoal - remainingMoves;
                result.message = $"Try next move: {result.nextMoveNotation}";
                return result;
            }

            result.hasHint = true;
            result.message = "You are at the end of the known path.";
            result.canClearWithinRemainingMoves = true;
            return result;
        }
    }
}
