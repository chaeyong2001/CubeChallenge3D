using System;

namespace CubeChallenge3D.Stages.Progress
{
    [Serializable]
    public sealed class StageProgress
    {
        public string stageId;
        public bool isUnlocked;
        public bool isCleared;
        public int bestMoves = -1;
        public float bestTimeSeconds = -1f;
        public int stars;
        public int clearCount;
        public string lastClearedAtUtc;
    }
}
