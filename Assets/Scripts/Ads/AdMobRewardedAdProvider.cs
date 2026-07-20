using System;
using System.Collections.Generic;
using UnityEngine;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace CubeChallenge3D.Ads
{
    // Add GOOGLE_MOBILE_ADS to Player Scripting Define Symbols only after
    // importing the Google Mobile Ads Unity SDK.
    public sealed class AdMobRewardedAdProvider : IRewardedAdProvider
    {
        private readonly AdsConfig config;

#if GOOGLE_MOBILE_ADS
        private readonly Dictionary<RewardedAdPlacement, RewardedAd> loadedAds = new();
        private readonly HashSet<RewardedAdPlacement> loadingPlacements = new();
#endif

        public AdMobRewardedAdProvider(AdsConfig adsConfig)
        {
            config = adsConfig ?? AdsConfig.Default;
            AdMobInitializer.Initialize(config, PreloadAll);
        }

        public bool IsAvailable(RewardedAdPlacement placement)
        {
            if (!config.adsEnabled || string.IsNullOrWhiteSpace(config.GetRewardedAdUnitId(placement)))
            {
                return false;
            }

#if GOOGLE_MOBILE_ADS
            return !string.IsNullOrWhiteSpace(config.GetAdMobAppId());
#else
            return false;
#endif
        }

        public bool IsReady(RewardedAdPlacement placement)
        {
#if GOOGLE_MOBILE_ADS
            return IsAvailable(placement)
                && loadedAds.TryGetValue(placement, out RewardedAd ad)
                && ad != null
                && ad.CanShowAd();
#else
            return false;
#endif
        }

        public void ShowRewardedAd(
            RewardedAdPlacement placement,
            Action onRewarded,
            Action onFailed,
            Action onClosed)
        {
#if GOOGLE_MOBILE_ADS
            if (!IsReady(placement))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdShow] placement={placement}, type=rewarded, ready=false, result=not_ready");
#endif
                Load(placement);
                onFailed?.Invoke();
                return;
            }

            RewardedAd ad = loadedAds[placement];
            loadedAds.Remove(placement);
            bool completed = false;

            ad.OnAdFullScreenContentFailed += _ =>
            {
                if (completed)
                {
                    return;
                }

                completed = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdShow] placement={placement}, type=rewarded, ready=true, result=failed");
#endif
                ad.Destroy();
                onFailed?.Invoke();
                Load(placement);
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                if (!completed)
                {
                    completed = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[AdShow] placement={placement}, type=rewarded, ready=true, result=closed");
#endif
                    onClosed?.Invoke();
                }

                ad.Destroy();
                Load(placement);
            };

            ad.Show(_ =>
            {
                if (completed)
                {
                    return;
                }

                completed = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdRewardEarned] placement={placement}, rewardType=rewarded, rewardAmount=1");
#endif
                onRewarded?.Invoke();
            });
#else
            onFailed?.Invoke();
#endif
        }

        private void PreloadAll()
        {
            foreach (RewardedAdPlacement placement in Enum.GetValues(typeof(RewardedAdPlacement)))
            {
                Load(placement);
            }
        }

        private void Load(RewardedAdPlacement placement)
        {
#if GOOGLE_MOBILE_ADS
            if (!AdMobInitializer.IsInitialized
                || !IsAvailable(placement)
                || loadingPlacements.Contains(placement))
            {
                return;
            }

            string adUnitId = config.GetRewardedAdUnitId(placement);
            loadingPlacements.Add(placement);
            RewardedAd.Load(adUnitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
            {
                loadingPlacements.Remove(placement);
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdLoad] type=rewarded, placement={placement}, unitId={adUnitId}, success=False, error={error}");
                    return;
                }

                if (loadedAds.TryGetValue(placement, out RewardedAd previousAd))
                {
                    previousAd?.Destroy();
                }

                loadedAds[placement] = ad;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdLoad] type=rewarded, placement={placement}, unitId={adUnitId}, success=True, error=");
#endif
            });
#endif
        }
    }
}
