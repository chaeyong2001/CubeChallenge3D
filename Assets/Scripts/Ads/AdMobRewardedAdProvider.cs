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
                ad.Destroy();
                onFailed?.Invoke();
                Load(placement);
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                if (!completed)
                {
                    completed = true;
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
                    Debug.LogWarning($"Rewarded ad load failed for {placement}: {error}");
                    return;
                }

                if (loadedAds.TryGetValue(placement, out RewardedAd previousAd))
                {
                    previousAd?.Destroy();
                }

                loadedAds[placement] = ad;
            });
#endif
        }
    }
}
