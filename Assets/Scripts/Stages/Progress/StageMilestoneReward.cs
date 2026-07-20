using System;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Progress
{
    [Serializable]
    public sealed class StageMilestoneReward
    {
        public StageType stageType = StageType.SolveStage;
        public int blockIndex;
        public int startStageNumber;
        public int endStageNumber;
        public int requiredStars = 30;
        public int rewardGems;
        public bool isClaimed;
        public bool claimedByUser;
        public string claimedAtUtc;
    }
}
