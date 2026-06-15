using System;

namespace CubeChallenge3D.Stages.Progress
{
    [Serializable]
    public sealed class StageMilestoneReward
    {
        public int blockIndex;
        public int startStageNumber;
        public int endStageNumber;
        public int requiredStars = 30;
        public int rewardGems;
        public bool isClaimed;
        public string claimedAtUtc;
    }
}
