using System;
using CubeChallenge3D.Save;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Stages.Assist;
using UnityEngine;

namespace CubeChallenge3D.Economy
{
    public enum DailyRewardType
    {
        Coins,
        Gems,
        Hearts,
        Item
    }

    public sealed class DailyRewardDefinition
    {
        public DailyRewardType type;
        public int amount;
        public StageAssistItemType itemType;
    }

    [Serializable]
    public sealed class DailyRewardData
    {
        public int saveVersion;
        public int currentDayIndex;
        public string lastClaimDateUtc;
        public int streakCount;
    }

    public sealed class DailyRewardStore
    {
        private const string FileName = "daily_rewards.json";
        private static readonly DailyRewardDefinition[] Rewards =
        {
            new DailyRewardDefinition { type = DailyRewardType.Coins, amount = 50 },
            new DailyRewardDefinition { type = DailyRewardType.Hearts, amount = 3 },
            new DailyRewardDefinition { type = DailyRewardType.Coins, amount = 80 },
            new DailyRewardDefinition { type = DailyRewardType.Hearts, amount = 5 },
            new DailyRewardDefinition { type = DailyRewardType.Gems, amount = 2 },
            new DailyRewardDefinition { type = DailyRewardType.Hearts, amount = 5 },
            new DailyRewardDefinition { type = DailyRewardType.Gems, amount = 5 }
        };
        private DailyRewardData data;

        public DailyRewardData Data => data ?? (data = Load());
        public int CurrentDayNumber => (((Data.currentDayIndex % 7) + 7) % 7) + 1;

        public int GetRewardCoinsForDay(int dayIndex)
        {
            int normalized = ((dayIndex % 7) + 7) % 7;
            return 30 + (normalized * 10);
        }

        public bool CanClaim(DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(Data.lastClaimDateUtc))
            {
                return true;
            }

            if (!DateTime.TryParse(Data.lastClaimDateUtc, out DateTime lastClaim))
            {
                return true;
            }

            return lastClaim.ToUniversalTime().Date < utcNow.Date;
        }

        public string GetRewardDescription()
        {
            DailyRewardDefinition reward = GetCurrentReward();
            switch (reward.type)
            {
                case DailyRewardType.Coins:
                    return $"Day {CurrentDayNumber}: {reward.amount} coins + {EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                case DailyRewardType.Gems:
                    return $"Day {CurrentDayNumber}: {reward.amount} gems + {EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                case DailyRewardType.Hearts:
                    return $"Day {CurrentDayNumber}: \u2665 +{reward.amount} Hearts + {EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                case DailyRewardType.Item:
                    return $"Day {CurrentDayNumber}: {reward.itemType} x{reward.amount} + {EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                default:
                    return "Daily Reward";
            }
        }

        public DailyRewardDefinition GetRewardForDay(int dayNumber)
        {
            int normalized = Mathf.Clamp(dayNumber, 1, Rewards.Length) - 1;
            return Rewards[normalized];
        }

        public bool TryClaim(DateTime utcNow, WalletStore walletStore, InventoryStore inventoryStore)
        {
            if (!CanClaim(utcNow) || walletStore == null || inventoryStore == null)
            {
                return false;
            }

            DailyRewardDefinition reward = GetCurrentReward();
            switch (reward.type)
            {
                case DailyRewardType.Coins:
                    walletStore.AddCoins(reward.amount);
                    break;
                case DailyRewardType.Gems:
                    walletStore.AddGems(reward.amount);
                    break;
                case DailyRewardType.Hearts:
                    walletStore.AddHearts(reward.amount);
                    break;
                case DailyRewardType.Item:
                    inventoryStore.Add(reward.itemType, reward.amount);
                    break;
            }
            inventoryStore.Add(StageAssistItemType.SolverTicket, EconomyBalanceConfig.DailySolverTicketBonus);

            MarkClaimed(utcNow);
            return true;
        }

        private DailyRewardDefinition GetCurrentReward()
        {
            return Rewards[CurrentDayNumber - 1];
        }

        public void MarkClaimed(DateTime utcNow)
        {
            Data.lastClaimDateUtc = utcNow.ToString("o");
            Data.currentDayIndex = (Data.currentDayIndex + 1) % 7;
            Data.streakCount = Data.streakCount == int.MaxValue ? int.MaxValue : Data.streakCount + 1;
            SaveService.SaveJson(FileName, Data);
        }

        public void ResetForDebug()
        {
            data = new DailyRewardData();
            SaveService.SaveJson(FileName, data);
        }

        public void DebugMakeCurrentRewardClaimable(DateTime utcNow)
        {
            Data.lastClaimDateUtc = utcNow.AddDays(-1).ToString("o");
            SaveService.SaveJson(FileName, Data);
        }

        private static DailyRewardData Load()
        {
            DailyRewardData loaded = SaveService.LoadJson(FileName, new DailyRewardData());
            bool changed = false;
            if (loaded.saveVersion < SaveDataValidator.CurrentSaveVersion)
            {
                loaded.saveVersion = SaveDataValidator.CurrentSaveVersion;
                changed = true;
            }
            if (loaded.currentDayIndex < 0 || loaded.currentDayIndex > 6)
            {
                loaded.currentDayIndex = 0;
                changed = true;
            }
            if (loaded.streakCount < 0)
            {
                loaded.streakCount = 0;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(loaded.lastClaimDateUtc)
                && !DateTime.TryParse(loaded.lastClaimDateUtc, out _))
            {
                loaded.lastClaimDateUtc = string.Empty;
                changed = true;
            }
            if (changed)
            {
                SaveService.SaveJson(FileName, loaded);
            }

            return loaded;
        }
    }
}
