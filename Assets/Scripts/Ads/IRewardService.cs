using System;

namespace CubeChallenge3D.Ads
{
    public interface IRewardService
    {
        bool IsRewardAvailable(RewardType type);
        void ShowReward(RewardType type, Action<bool> onCompleted);
    }
}
