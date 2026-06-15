using System;

namespace CubeChallenge3D.Ads
{
    public sealed class MockRewardService : IRewardService
    {
        public bool IsRewardAvailable(RewardType type)
        {
            return true;
        }

        public void ShowReward(RewardType type, Action<bool> onCompleted)
        {
            // Step 15 can replace this mock with a real AdMob rewarded-ad implementation.
            onCompleted?.Invoke(true);
        }
    }
}
