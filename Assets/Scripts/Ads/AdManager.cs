using System;
using UnityEngine;

namespace CubeChallenge3D.Ads
{
    public sealed class AdManager
    {
        private static AdManager instance;

        private readonly RewardedAdService rewardedService;
        private readonly IInterstitialAdProvider interstitialProvider;
        private bool isShowingInterstitial;

        public AdsConfig Config { get; }
        public bool RemoveAdsPurchased { get; private set; }
        public static AdManager Instance => instance ??= CreateDefault();

        public AdManager(
            AdsConfig config = null,
            RewardedAdService rewards = null,
            IInterstitialAdProvider interstitials = null)
        {
            Config = config ?? AdsConfig.Load();
            rewardedService = rewards ?? RewardedAdService.CreateDefault();
            interstitialProvider = interstitials ?? CreateInterstitialProvider(Config);
        }

        public static AdManager CreateDefault()
        {
            return new AdManager();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAtStartup()
        {
            Instance.InitializeAds();
        }

        public void InitializeAds()
        {
            bool useMockProvider = false;
#if UNITY_EDITOR
            useMockProvider = Config.useMockAdsInEditor;
#elif DEVELOPMENT_BUILD
            useMockProvider = Config.useMockAdsInDevelopmentBuild;
#endif
            if (!useMockProvider)
            {
                AdMobInitializer.Initialize(Config);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AdManagerInit] testMode={Config.useTestAdUnitIds}, appPackage={Application.identifier}, mock={useMockProvider}");
#endif
        }

        public bool CanShowRewardedAd(RewardedAdPlacement placement)
        {
            return rewardedService.CanShow(placement);
        }

        public void ShowRewardedAd(
            RewardedAdPlacement placement,
            Action onRewardEarned,
            Action<RewardedAdResult> onCompleted = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AdShow] placement={placement}, type=rewarded, ready={rewardedService.IsReady(placement)}, result=requested");
#endif
            rewardedService.Show(placement, onRewardEarned, result =>
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdShow] placement={placement}, type=rewarded, ready={rewardedService.IsReady(placement)}, result={result}");
#endif
                onCompleted?.Invoke(result);
            });
        }

        public bool CanShowInterstitial(InterstitialPlacement placement)
        {
            return Config.adsEnabled
                && !RemoveAdsPurchased
                && !isShowingInterstitial
                && interstitialProvider != null
                && interstitialProvider.IsReady(placement);
        }

        public bool TryShowInterstitial(
            InterstitialPlacement placement,
            Action onCompleted = null)
        {
            bool canShow = CanShowInterstitial(placement);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[InterstitialPolicy] placement={placement}, removeAds={RemoveAdsPurchased}, canShow={canShow}");
#endif
            if (!canShow)
            {
                onCompleted?.Invoke();
                return false;
            }

            isShowingInterstitial = true;
            interstitialProvider.ShowInterstitial(
                placement,
                () =>
                {
                    isShowingInterstitial = false;
                    onCompleted?.Invoke();
                },
                () =>
                {
                    isShowingInterstitial = false;
                    onCompleted?.Invoke();
                });
            return true;
        }

        public void SetRemoveAdsPurchased(bool purchased)
        {
            RemoveAdsPurchased = purchased;
        }

        public void SetTestMode(bool enabled)
        {
            Config.useTestAdUnitIds = enabled;
        }

        private static IInterstitialAdProvider CreateInterstitialProvider(AdsConfig config)
        {
            if (!config.adsEnabled)
            {
                return new NoOpInterstitialAdProvider();
            }
#if UNITY_EDITOR
            if (config.useMockAdsInEditor)
            {
                return new MockInterstitialAdProvider();
            }
#elif DEVELOPMENT_BUILD
            if (config.useMockAdsInDevelopmentBuild)
            {
                return new MockInterstitialAdProvider();
            }
#endif
            return new AdMobInterstitialAdProvider(config);
        }
    }
}
