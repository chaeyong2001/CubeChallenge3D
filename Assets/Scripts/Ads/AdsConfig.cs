using System;
using UnityEngine;

namespace CubeChallenge3D.Ads
{
    [Serializable]
    public sealed class AdsConfig
    {
        public bool adsEnabled = true;
        public bool useMockAdsInEditor = true;
        public bool useMockAdsInDevelopmentBuild = true;
        public bool useTestAdUnitIds = true;

        public string androidAdMobAppId = string.Empty;
        public string iosAdMobAppId = string.Empty;

        public string androidRewardedAdUnitIdStageContinue = string.Empty;
        public string androidRewardedAdUnitIdDailyCoins = string.Empty;
        public string androidRewardedAdUnitIdSolverBonusTicket = string.Empty;
        public string iosRewardedAdUnitIdStageContinue = string.Empty;
        public string iosRewardedAdUnitIdDailyCoins = string.Empty;
        public string iosRewardedAdUnitIdSolverBonusTicket = string.Empty;

        // Google-provided rewarded test ad unit IDs. Keep production IDs above.
        public string androidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        public string iosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

        public int stageContinueMaxPerRun = 2;
        public int stageContinueMovesReward = 2;
        public int dailyCoinAdsMax = 5;
        public int dailyCoinRewardAmount = 100;
        public int dailySolverTicketAdsMax = 1;
        public int solverTicketRewardAmount = 1;

        public static AdsConfig Default => new AdsConfig();

        public static AdsConfig Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("AdsConfig");
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                return Default;
            }

            try
            {
                return JsonUtility.FromJson<AdsConfig>(asset.text) ?? Default;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"AdsConfig could not be loaded. Defaults will be used. {exception.Message}");
                return Default;
            }
        }

        public string GetAdMobAppId()
        {
#if UNITY_IOS
            return iosAdMobAppId;
#else
            return androidAdMobAppId;
#endif
        }

        public string GetRewardedAdUnitId(RewardedAdPlacement placement)
        {
            if (useTestAdUnitIds)
            {
#if UNITY_IOS
                return iosTestRewardedAdUnitId;
#else
                return androidTestRewardedAdUnitId;
#endif
            }

#if UNITY_IOS
            switch (placement)
            {
                case RewardedAdPlacement.StageContinue:
                    return iosRewardedAdUnitIdStageContinue;
                case RewardedAdPlacement.DailyCoins:
                    return iosRewardedAdUnitIdDailyCoins;
                case RewardedAdPlacement.SolverBonusTicket:
                    return iosRewardedAdUnitIdSolverBonusTicket;
                default:
                    return string.Empty;
            }
#else
            switch (placement)
            {
                case RewardedAdPlacement.StageContinue:
                    return androidRewardedAdUnitIdStageContinue;
                case RewardedAdPlacement.DailyCoins:
                    return androidRewardedAdUnitIdDailyCoins;
                case RewardedAdPlacement.SolverBonusTicket:
                    return androidRewardedAdUnitIdSolverBonusTicket;
                default:
                    return string.Empty;
            }
#endif
        }
    }
}
