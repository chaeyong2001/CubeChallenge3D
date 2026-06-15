using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Stages.Progress
{
    [Serializable]
    public sealed class StageMilestoneRewardData
    {
        public int saveVersion;
        public List<StageMilestoneReward> rewards = new List<StageMilestoneReward>();
    }
}
