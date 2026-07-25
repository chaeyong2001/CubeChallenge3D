using System;

namespace CubeChallenge3D.Ads
{
    public sealed class MockRewardedAdProvider : IRewardedAdProvider
    {
        public RewardedAdResult NextResult { get; set; } = RewardedAdResult.Rewarded;

        public bool IsAvailable(RewardedAdPlacement placement)
        {
            return true;
        }

        public bool IsReady(RewardedAdPlacement placement)
        {
            return true;
        }

        public void LoadRewardedAd(RewardedAdPlacement placement)
        {
        }

        public void ShowRewardedAd(
            RewardedAdPlacement placement,
            Action onRewarded,
            Action onFailed,
            Action onClosed)
        {
            RewardedAdResult result = NextResult;
            NextResult = RewardedAdResult.Rewarded;
            if (result == RewardedAdResult.Rewarded)
            {
                onRewarded?.Invoke();
                onClosed?.Invoke();
                return;
            }

            if (result == RewardedAdResult.Closed)
            {
                onClosed?.Invoke();
                return;
            }

            onFailed?.Invoke();
        }
    }
}
