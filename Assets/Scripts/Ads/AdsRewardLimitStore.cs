using System;
using CubeChallenge3D.Save;

namespace CubeChallenge3D.Ads
{
    [Serializable]
    public sealed class AdsRewardLimitData
    {
        public int saveVersion;
        public int dailyCoinsAdCount;
        public int dailySolverBonusAdCount;
        public string lastResetDate;
    }

    public sealed class AdsRewardLimitStore
    {
        private const string FileName = "ads_reward_limits.json";
        private AdsRewardLimitData data;

        public int DailyCoinsAdCount
        {
            get
            {
                EnsureCurrentDate();
                return data.dailyCoinsAdCount;
            }
        }

        public int DailySolverBonusAdCount
        {
            get
            {
                EnsureCurrentDate();
                return data.dailySolverBonusAdCount;
            }
        }

        public void RecordReward(RewardedAdPlacement placement)
        {
            EnsureCurrentDate();
            if (placement == RewardedAdPlacement.DailyCoins)
            {
                data.dailyCoinsAdCount++;
            }
            else if (placement == RewardedAdPlacement.SolverBonusTicket)
            {
                data.dailySolverBonusAdCount++;
            }

            SaveService.SaveJson(FileName, data);
        }

        public void ResetForDebug()
        {
            data = CreateDefault();
            SaveService.SaveJson(FileName, data);
        }

        private void EnsureCurrentDate()
        {
            data ??= SaveService.LoadJson(FileName, CreateDefault());
            if (!SaveDataValidator.Normalize(data, DateTime.Today))
            {
                return;
            }

            SaveService.SaveJson(FileName, data);
        }

        private static AdsRewardLimitData CreateDefault()
        {
            return new AdsRewardLimitData
            {
                lastResetDate = DateTime.Today.ToString("yyyy-MM-dd")
            };
        }
    }
}
