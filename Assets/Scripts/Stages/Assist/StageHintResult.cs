using System;

namespace CubeChallenge3D.Stages.Assist
{
    [Serializable]
    public sealed class StageHintResult
    {
        public bool hasHint;
        public bool canClearWithinRemainingMoves;
        public int estimatedMovesToGoal;
        public int remainingMoves;
        public int shortageMoves;
        public string nextMoveNotation;
        public string message;
    }
}
