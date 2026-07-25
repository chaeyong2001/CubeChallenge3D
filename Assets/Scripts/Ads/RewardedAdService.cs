using System;
using UnityEngine;

namespace CubeChallenge3D.Ads
{
    public sealed class RewardedAdService : IRewardService
    {
        private readonly IRewardedAdProvider provider;
        private readonly AdsRewardLimitStore limitStore;
        private bool isShowingAd;

        public AdsConfig Config { get; }
        public bool IsShowingAd => isShowingAd;
        public bool AdsEnabled => Config.adsEnabled;
        public int DailyCoinsUsed => limitStore.DailyCoinsAdCount;
        public int DailyShopCoinAdsUsed => limitStore.DailyShopCoinAdCount;
        public int DailySolverTicketsUsed => limitStore.DailySolverBonusAdCount;

        public RewardedAdService(
            IRewardedAdProvider rewardedAdProvider,
            AdsRewardLimitStore rewardLimitStore = null,
            AdsConfig config = null)
        {
            provider = rewardedAdProvider;
            limitStore = rewardLimitStore ?? new AdsRewardLimitStore();
            Config = config ?? AdsConfig.Default;
        }

        public static RewardedAdService CreateDefault()
        {
            AdsConfig config = AdsConfig.Load();
            IRewardedAdProvider selectedProvider;
            if (!config.adsEnabled)
            {
                selectedProvider = new NoOpRewardedAdProvider();
            }
#if UNITY_EDITOR
            else if (config.useMockAdsInEditor)
            {
                selectedProvider = new MockRewardedAdProvider();
            }
#elif DEVELOPMENT_BUILD
            else if (config.useMockAdsInDevelopmentBuild)
            {
                selectedProvider = new MockRewardedAdProvider();
            }
#endif
            else
            {
                selectedProvider = new AdMobRewardedAdProvider(config);
            }

            return new RewardedAdService(selectedProvider, config: config);
        }

        public bool IsPlacementAvailable(RewardedAdPlacement placement)
        {
            return Config.adsEnabled
                && provider != null
                && provider.IsAvailable(placement);
        }

        public bool IsReady(RewardedAdPlacement placement)
        {
            return IsPlacementAvailable(placement)
                && provider.IsReady(placement);
        }

        public string GetUnavailableMessage(RewardedAdPlacement placement)
        {
            if (!Config.adsEnabled)
            {
                return "Ads are not available yet.";
            }
            if (IsLimitReached(placement))
            {
                return placement == RewardedAdPlacement.StageContinue
                    || placement == RewardedAdPlacement.OutOfMovesPlus2
                    ? "Stage continue limit reached."
                    : "Daily ad limit reached.";
            }
            if (!IsPlacementAvailable(placement))
            {
                return "Ads are not available yet.";
            }
            if (!provider.IsReady(placement))
            {
                return "Ad is not ready. Try again later.";
            }

            return string.Empty;
        }

        public bool CanShow(RewardedAdPlacement placement)
        {
            return IsPlacementAvailable(placement)
                && !isShowingAd
                && !IsLimitReached(placement)
                && provider.IsReady(placement);
        }

        public bool CanRequest(RewardedAdPlacement placement)
        {
            return IsPlacementAvailable(placement)
                && !isShowingAd
                && !IsLimitReached(placement);
        }

        public void EnsureLoaded(RewardedAdPlacement placement)
        {
            if (!IsPlacementAvailable(placement) || IsLimitReached(placement) || provider.IsReady(placement))
            {
                return;
            }

            provider.LoadRewardedAd(placement);
        }

        public int GetRemaining(RewardedAdPlacement placement)
        {
            if (placement == RewardedAdPlacement.DailyCoins)
            {
                return Mathf.Max(0, Config.dailyCoinAdsMax - DailyCoinsUsed);
            }

            if (placement == RewardedAdPlacement.ShopCoinReward)
            {
                return Mathf.Max(0, Config.dailyCoinAdsMax - DailyShopCoinAdsUsed);
            }

            if (placement == RewardedAdPlacement.SolverBonusTicket)
            {
                return Mathf.Max(0, Config.dailySolverTicketAdsMax - DailySolverTicketsUsed);
            }

            return Config.stageContinueMaxPerRun;
        }

        public void Show(
            RewardedAdPlacement placement,
            Action onRewarded,
            Action<RewardedAdResult> onCompleted = null)
        {
            if (isShowingAd)
            {
                onCompleted?.Invoke(RewardedAdResult.Busy);
                return;
            }
            if (!IsPlacementAvailable(placement))
            {
                onCompleted?.Invoke(RewardedAdResult.Unavailable);
                return;
            }
            if (IsLimitReached(placement))
            {
                onCompleted?.Invoke(RewardedAdResult.LimitReached);
                return;
            }
            if (!provider.IsReady(placement))
            {
                provider.LoadRewardedAd(placement);
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                return;
            }

            isShowingAd = true;
            bool finished = false;
            provider.ShowRewardedAd(
                placement,
                () =>
                {
                    if (finished)
                    {
                        return;
                    }

                    finished = true;
                    isShowingAd = false;
                    limitStore.RecordReward(placement);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[AdRewardApplied] placement={placement}, result=rewarded");
#endif
                    onRewarded?.Invoke();
                    onCompleted?.Invoke(RewardedAdResult.Rewarded);
                },
                () =>
                {
                    if (finished)
                    {
                        return;
                    }

                    finished = true;
                    isShowingAd = false;
                    onCompleted?.Invoke(RewardedAdResult.Failed);
                },
                () =>
                {
                    if (finished)
                    {
                        return;
                    }

                    finished = true;
                    isShowingAd = false;
                    onCompleted?.Invoke(RewardedAdResult.Closed);
                });
        }

        public bool IsRewardAvailable(RewardType type)
        {
            return CanShow(ToPlacement(type));
        }

        public void ShowReward(RewardType type, Action<bool> onCompleted)
        {
            Show(ToPlacement(type), () => onCompleted?.Invoke(true), result =>
            {
                if (result != RewardedAdResult.Rewarded)
                {
                    onCompleted?.Invoke(false);
                }
            });
        }

        public void ResetLimitsForDebug()
        {
            limitStore.ResetForDebug();
        }

        private bool IsLimitReached(RewardedAdPlacement placement)
        {
            if (placement == RewardedAdPlacement.DailyCoins)
            {
                return DailyCoinsUsed >= Config.dailyCoinAdsMax;
            }

            if (placement == RewardedAdPlacement.ShopCoinReward)
            {
                return DailyShopCoinAdsUsed >= Config.dailyCoinAdsMax;
            }

            if (placement == RewardedAdPlacement.SolverBonusTicket)
            {
                return DailySolverTicketsUsed >= Config.dailySolverTicketAdsMax;
            }

            return false;
        }

        private static RewardedAdPlacement ToPlacement(RewardType type)
        {
            switch (type)
            {
                case RewardType.ContinueStage:
                    return RewardedAdPlacement.OutOfMovesPlus2;
                case RewardType.SolverExtraUse:
                    return RewardedAdPlacement.SolverBonusTicket;
                default:
                    return RewardedAdPlacement.ShopCoinReward;
            }
        }
    }
}
