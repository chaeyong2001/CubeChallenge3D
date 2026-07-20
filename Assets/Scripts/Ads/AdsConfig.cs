using System;
using UnityEngine;

namespace CubeChallenge3D.Ads
{
    [Serializable]
    public sealed class AdsConfig
    {
        public bool adsEnabled = true;
        public bool useMockAdsInEditor = true;
        public bool useMockAdsInDevelopmentBuild = false;
        public bool useTestAdUnitIds = false;

        public string androidAdMobAppId = "ca-app-pub-7592667377259848~1463223074";
        public string iosAdMobAppId = "ca-app-pub-3940256099942544~1458002511";

        public string androidRewardedAdUnitIdStageContinue = "ca-app-pub-7592667377259848/6919297959";
        public string androidRewardedAdUnitIdHeartPlus1 = "ca-app-pub-7592667377259848/6919297959";
        public string androidRewardedAdUnitIdDailyCoins = string.Empty;
        public string androidRewardedAdUnitIdShopCoinReward = "ca-app-pub-7592667377259848/6919297959";
        public string androidRewardedAdUnitIdSolverBonusTicket = "ca-app-pub-7592667377259848/6919297959";
        public string androidInterstitialAdUnitIdStageClearTransition = "ca-app-pub-7592667377259848/4954765581";
        public string androidInterstitialAdUnitIdLongSessionTransition = string.Empty;
        public string androidInterstitialAdUnitIdModeTransition = string.Empty;
        public string androidInterstitialAdUnitIdRankingChallengeEnd = string.Empty;
        public string iosRewardedAdUnitIdStageContinue = string.Empty;
        public string iosRewardedAdUnitIdHeartPlus1 = string.Empty;
        public string iosRewardedAdUnitIdDailyCoins = string.Empty;
        public string iosRewardedAdUnitIdShopCoinReward = string.Empty;
        public string iosRewardedAdUnitIdSolverBonusTicket = string.Empty;
        public string iosInterstitialAdUnitIdStageClearTransition = string.Empty;
        public string iosInterstitialAdUnitIdLongSessionTransition = string.Empty;
        public string iosInterstitialAdUnitIdModeTransition = string.Empty;
        public string iosInterstitialAdUnitIdRankingChallengeEnd = string.Empty;

        // Google-provided rewarded test ad unit IDs. Keep production IDs above.
        public string androidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        public string iosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
        public string androidTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        public string iosTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";

        public int stageContinueMaxPerRun = 2;
        public int stageContinueMovesReward = 2;
        public int dailyCoinAdsMax = 3;
        public int dailyCoinRewardAmount = 50;
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
                case RewardedAdPlacement.OutOfMovesPlus2:
                    return iosRewardedAdUnitIdStageContinue;
                case RewardedAdPlacement.HeartPlus1:
                    return iosRewardedAdUnitIdHeartPlus1;
                case RewardedAdPlacement.DailyCoins:
                    return iosRewardedAdUnitIdDailyCoins;
                case RewardedAdPlacement.ShopCoinReward:
                    return iosRewardedAdUnitIdShopCoinReward;
                case RewardedAdPlacement.SolverBonusTicket:
                    return iosRewardedAdUnitIdSolverBonusTicket;
                default:
                    return string.Empty;
            }
#else
            switch (placement)
            {
                case RewardedAdPlacement.StageContinue:
                case RewardedAdPlacement.OutOfMovesPlus2:
                    return androidRewardedAdUnitIdStageContinue;
                case RewardedAdPlacement.HeartPlus1:
                    return androidRewardedAdUnitIdHeartPlus1;
                case RewardedAdPlacement.DailyCoins:
                    return androidRewardedAdUnitIdDailyCoins;
                case RewardedAdPlacement.ShopCoinReward:
                    return androidRewardedAdUnitIdShopCoinReward;
                case RewardedAdPlacement.SolverBonusTicket:
                    return androidRewardedAdUnitIdSolverBonusTicket;
                default:
                    return string.Empty;
            }
#endif
        }

        public string GetInterstitialAdUnitId(InterstitialPlacement placement)
        {
            if (useTestAdUnitIds)
            {
#if UNITY_IOS
                return iosTestInterstitialAdUnitId;
#else
                return androidTestInterstitialAdUnitId;
#endif
            }

#if UNITY_IOS
            switch (placement)
            {
                case InterstitialPlacement.StageClearTransition:
                    return iosInterstitialAdUnitIdStageClearTransition;
                case InterstitialPlacement.LongSessionTransition:
                    return iosInterstitialAdUnitIdLongSessionTransition;
                case InterstitialPlacement.ModeTransition:
                    return iosInterstitialAdUnitIdModeTransition;
                case InterstitialPlacement.RankingChallengeEnd:
                    return iosInterstitialAdUnitIdRankingChallengeEnd;
                default:
                    return string.Empty;
            }
#else
            switch (placement)
            {
                case InterstitialPlacement.StageClearTransition:
                    return androidInterstitialAdUnitIdStageClearTransition;
                case InterstitialPlacement.LongSessionTransition:
                    return androidInterstitialAdUnitIdLongSessionTransition;
                case InterstitialPlacement.ModeTransition:
                    return androidInterstitialAdUnitIdModeTransition;
                case InterstitialPlacement.RankingChallengeEnd:
                    return androidInterstitialAdUnitIdRankingChallengeEnd;
                default:
                    return string.Empty;
            }
#endif
        }
    }
}
