using System;
using UnityEngine;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace CubeChallenge3D.Ads
{
    public static class AdMobInitializer
    {
        // Release checklist: configure the AdMob App ID/manifest, then add UMP
        // consent, privacy policy, and personalization handling before launch.
        private static bool initializationStarted;
        private static bool isInitialized;
        private static Action pendingCallbacks;

        public static bool IsInitialized => isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeAtStartup()
        {
            AdsConfig config = AdsConfig.Load();
            if (!config.adsEnabled)
            {
                return;
            }

#if UNITY_EDITOR
            if (config.useMockAdsInEditor)
            {
                return;
            }
#elif DEVELOPMENT_BUILD
            if (config.useMockAdsInDevelopmentBuild)
            {
                return;
            }
#endif
            Initialize(config);
        }

        public static void Initialize(AdsConfig config, Action onInitialized = null)
        {
            if (isInitialized)
            {
                onInitialized?.Invoke();
                return;
            }

            if (onInitialized != null)
            {
                pendingCallbacks += onInitialized;
            }

            if (initializationStarted || config == null || !config.adsEnabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(config.GetAdMobAppId()))
            {
                Debug.LogWarning("AdMob initialization skipped: platform App ID is empty.");
                return;
            }

            initializationStarted = true;
#if GOOGLE_MOBILE_ADS
            MobileAds.Initialize(_ =>
            {
                isInitialized = true;
                Action callbacks = pendingCallbacks;
                pendingCallbacks = null;
                callbacks?.Invoke();
            });
#else
            Debug.Log("AdMob SDK is not installed. Rewarded ads remain unavailable.");
#endif
        }
    }
}
