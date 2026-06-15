using System;

namespace CubeChallenge3D.Ads
{
    public sealed class NoOpRewardedAdProvider : IRewardedAdProvider
    {
        public bool IsAvailable(RewardedAdPlacement placement)
        {
            return false;
        }

        public bool IsReady(RewardedAdPlacement placement)
        {
            return false;
        }

        public void ShowRewardedAd(
            RewardedAdPlacement placement,
            Action onRewarded,
            Action onFailed,
            Action onClosed)
        {
            onFailed?.Invoke();
        }
    }
}
