using System;

namespace CubeChallenge3D.Ads
{
    public interface IRewardedAdProvider
    {
        bool IsAvailable(RewardedAdPlacement placement);
        bool IsReady(RewardedAdPlacement placement);
        void ShowRewardedAd(
            RewardedAdPlacement placement,
            Action onRewarded,
            Action onFailed,
            Action onClosed);
    }
}
