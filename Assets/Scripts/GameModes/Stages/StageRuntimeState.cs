using System;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.GameModes.Stages
{
    [Serializable]
    public sealed class StageRuntimeState
    {
        public StageData stage;
        public float elapsedSeconds;
        public int currentMoves;
        public int moveLimit;
        public int remainingMoves;
        public bool isPlaying;
        public bool isCompleted;
        public bool isFailed;
        public DateTime startedAt;
        public string targetSummary;
    }
}
