using System;
using CubeChallenge3D.Save;

namespace CubeChallenge3D.Ads
{
    [Serializable]
    public sealed class AdsRewardLimitData
    {
        public int saveVersion;
        public int dailyCoinsAdCount;
        public int dailyShopCoinAdCount;
        public int dailySolverBonusAdCount;
        public string dailyHeartAdDate;
        public int dailyHeartAdTotalCount;
        public int heartAdBatchCount;
        public string heartAdBatchCooldownEndTime;
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

        public int DailyShopCoinAdCount
        {
            get
            {
                EnsureCurrentDate();
                return data.dailyShopCoinAdCount;
            }
        }

        public int DailyHeartAdTotalCount
        {
            get
            {
                EnsureCurrentDate();
                return data.dailyHeartAdTotalCount;
            }
        }

        public int HeartAdBatchCount
        {
            get
            {
                EnsureCurrentDate();
                return data.heartAdBatchCount;
            }
        }

        public DateTime HeartAdBatchCooldownEndTimeUtc
        {
            get
            {
                EnsureCurrentDate();
                return TryParseUtc(data.heartAdBatchCooldownEndTime, out DateTime parsed) ? parsed : DateTime.MinValue;
            }
        }

        public void RecordReward(RewardedAdPlacement placement)
        {
            EnsureCurrentDate();
            if (placement == RewardedAdPlacement.DailyCoins)
            {
                data.dailyCoinsAdCount++;
            }
            else if (placement == RewardedAdPlacement.ShopCoinReward)
            {
                data.dailyShopCoinAdCount++;
            }
            else if (placement == RewardedAdPlacement.SolverBonusTicket)
            {
                data.dailySolverBonusAdCount++;
            }

            SaveService.SaveJson(FileName, data);
        }

        public void RecordHeartAdReward(DateTime utcNow, int batchLimit, TimeSpan batchCooldown)
        {
            EnsureCurrentDate();
            data.dailyHeartAdTotalCount++;
            data.heartAdBatchCount++;
            if (data.heartAdBatchCount >= batchLimit)
            {
                data.heartAdBatchCooldownEndTime = utcNow.Add(batchCooldown).ToString("o");
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
            bool changed = SaveDataValidator.Normalize(data, DateTime.Today);
            changed |= RefreshHeartBatchCooldown(DateTime.UtcNow);
            if (!changed)
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

        private bool RefreshHeartBatchCooldown(DateTime utcNow)
        {
            if (data == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.heartAdBatchCooldownEndTime)
                || !TryParseUtc(data.heartAdBatchCooldownEndTime, out DateTime cooldownEnd)
                || utcNow < cooldownEnd)
            {
                return false;
            }

            data.heartAdBatchCount = 0;
            data.heartAdBatchCooldownEndTime = string.Empty;
            return true;
        }

        private static bool TryParseUtc(string value, out DateTime parsedUtc)
        {
            if (DateTime.TryParse(value, out DateTime parsed))
            {
                parsedUtc = parsed.ToUniversalTime();
                return true;
            }

            parsedUtc = DateTime.MinValue;
            return false;
        }
    }
}
