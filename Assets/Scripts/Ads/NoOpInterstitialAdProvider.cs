using System;

namespace CubeChallenge3D.Ads
{
    public sealed class NoOpInterstitialAdProvider : IInterstitialAdProvider
    {
        public bool IsAvailable(InterstitialPlacement placement)
        {
            return false;
        }

        public bool IsReady(InterstitialPlacement placement)
        {
            return false;
        }

        public void ShowInterstitial(
            InterstitialPlacement placement,
            Action onClosed,
            Action onFailed)
        {
            onFailed?.Invoke();
        }
    }
}
