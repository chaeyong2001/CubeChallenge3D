using System;
using System.Collections.Generic;
using UnityEngine;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace CubeChallenge3D.Ads
{
    public sealed class AdMobInterstitialAdProvider : IInterstitialAdProvider
    {
        private readonly AdsConfig config;

#if GOOGLE_MOBILE_ADS
        private readonly Dictionary<InterstitialPlacement, InterstitialAd> loadedAds = new();
        private readonly HashSet<InterstitialPlacement> loadingPlacements = new();
#endif

        public AdMobInterstitialAdProvider(AdsConfig adsConfig)
        {
            config = adsConfig ?? AdsConfig.Default;
            AdMobInitializer.Initialize(config, PreloadAll);
        }

        public bool IsAvailable(InterstitialPlacement placement)
        {
            if (!config.adsEnabled || string.IsNullOrWhiteSpace(config.GetInterstitialAdUnitId(placement)))
            {
                return false;
            }

#if GOOGLE_MOBILE_ADS
            return !string.IsNullOrWhiteSpace(config.GetAdMobAppId());
#else
            return false;
#endif
        }

        public bool IsReady(InterstitialPlacement placement)
        {
#if GOOGLE_MOBILE_ADS
            return IsAvailable(placement)
                && loadedAds.TryGetValue(placement, out InterstitialAd ad)
                && ad != null
                && ad.CanShowAd();
#else
            return false;
#endif
        }

        public void ShowInterstitial(
            InterstitialPlacement placement,
            Action onClosed,
            Action onFailed)
        {
#if GOOGLE_MOBILE_ADS
            if (!IsReady(placement))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdShow] placement={placement}, type=interstitial, ready=false, result=not_ready");
#endif
                Load(placement);
                onFailed?.Invoke();
                return;
            }

            InterstitialAd ad = loadedAds[placement];
            loadedAds.Remove(placement);
            bool finished = false;

            ad.OnAdFullScreenContentFailed += _ =>
            {
                if (finished)
                {
                    return;
                }

                finished = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdShow] placement={placement}, type=interstitial, ready=true, result=failed");
#endif
                ad.Destroy();
                onFailed?.Invoke();
                Load(placement);
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                if (!finished)
                {
                    finished = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[AdShow] placement={placement}, type=interstitial, ready=true, result=closed");
#endif
                    onClosed?.Invoke();
                }

                ad.Destroy();
                Load(placement);
            };

            ad.Show();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AdShow] placement={placement}, type=interstitial, ready=true, result=started");
#endif
#else
            onFailed?.Invoke();
#endif
        }

        private void PreloadAll()
        {
            foreach (InterstitialPlacement placement in Enum.GetValues(typeof(InterstitialPlacement)))
            {
                Load(placement);
            }
        }

        private void Load(InterstitialPlacement placement)
        {
#if GOOGLE_MOBILE_ADS
            if (!AdMobInitializer.IsInitialized
                || !IsAvailable(placement)
                || loadingPlacements.Contains(placement))
            {
                return;
            }

            string adUnitId = config.GetInterstitialAdUnitId(placement);
            loadingPlacements.Add(placement);
            InterstitialAd.Load(adUnitId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
            {
                loadingPlacements.Remove(placement);
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdLoad] type=interstitial, placement={placement}, unitId={adUnitId}, success=False, error={error}");
                    return;
                }

                if (loadedAds.TryGetValue(placement, out InterstitialAd previousAd))
                {
                    previousAd?.Destroy();
                }

                loadedAds[placement] = ad;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[AdLoad] type=interstitial, placement={placement}, unitId={adUnitId}, success=True, error=");
#endif
            });
#endif
        }
    }
}
