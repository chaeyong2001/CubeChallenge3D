using System;

namespace CubeChallenge3D.Ads
{
    public interface IInterstitialAdProvider
    {
        bool IsAvailable(InterstitialPlacement placement);
        bool IsReady(InterstitialPlacement placement);
        void ShowInterstitial(
            InterstitialPlacement placement,
            Action onClosed,
            Action onFailed);
    }
}
