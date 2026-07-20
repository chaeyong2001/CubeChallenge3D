using System;

namespace CubeChallenge3D.Ads
{
    public sealed class MockInterstitialAdProvider : IInterstitialAdProvider
    {
        public bool IsAvailable(InterstitialPlacement placement)
        {
            return true;
        }

        public bool IsReady(InterstitialPlacement placement)
        {
            return true;
        }

        public void ShowInterstitial(
            InterstitialPlacement placement,
            Action onClosed,
            Action onFailed)
        {
            onClosed?.Invoke();
        }
    }
}
