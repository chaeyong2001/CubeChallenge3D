using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Learn.Model;
using CubeChallenge3D.Stages.Progress;

namespace CubeChallenge3D.Save
{
    public static class SaveDataValidator
    {
        public const int CurrentSaveVersion = 2;

        public static bool Normalize(EconomyWallet data)
        {
            if (data == null)
            {
                return false;
            }

            bool changed = false;
            if (data.saveVersion < 2)
            {
                if (string.IsNullOrWhiteSpace(data.lastHeartRegenTimeUtc))
                {
                    data.lastHeartRegenTimeUtc = DateTime.UtcNow.ToString("o");
                }
                changed = true;
            }

            changed |= UpgradeVersion(ref data.saveVersion);
            changed |= ClampNonNegative(ref data.coins);
            changed |= ClampNonNegative(ref data.gems);
            changed |= ClampNonNegative(ref data.hearts);
            if (string.IsNullOrWhiteSpace(data.lastHeartRegenTimeUtc)
                || !DateTime.TryParse(data.lastHeartRegenTimeUtc, out _))
            {
                data.lastHeartRegenTimeUtc = DateTime.UtcNow.ToString("o");
                changed = true;
            }
            return changed;
        }

        public static bool Normalize(InventoryData data)
        {
            if (data == null)
            {
                return false;
            }

            bool changed = UpgradeVersion(ref data.saveVersion);
            changed |= ClampNonNegative(ref data.undoItems);
            changed |= ClampNonNegative(ref data.movePlus1Items);
            changed |= ClampNonNegative(ref data.movePlus2Items);
            changed |= ClampNonNegative(ref data.movePlus3Items);
            changed |= ClampNonNegative(ref data.solverTickets);
            changed |= NormalizeIds(ref data.ownedSkinIds);
            changed |= NormalizeIds(ref data.ownedThemeIds);
            changed |= EnsureId(data.ownedSkinIds, "classic");
            changed |= EnsureId(data.ownedThemeIds, "default");

            if (string.IsNullOrWhiteSpace(data.selectedSkinId)
                || !data.ownedSkinIds.Contains(data.selectedSkinId)
                || !VisualCustomizationCatalog.HasSkin(data.selectedSkinId))
            {
                data.selectedSkinId = "classic";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(data.selectedThemeId)
                || !data.ownedThemeIds.Contains(data.selectedThemeId)
                || !VisualCustomizationCatalog.HasTheme(data.selectedThemeId))
            {
                data.selectedThemeId = "default";
                changed = true;
            }

            return changed;
        }

        public static bool Normalize(LearnLessonProgress data)
        {
            if (data == null)
            {
                return false;
            }

            bool changed = UpgradeVersion(ref data.saveVersion);
            changed |= NormalizeIds(ref data.completedLessonIds);
            if (data.lastOpenedCategory == null)
            {
                data.lastOpenedCategory = string.Empty;
                changed = true;
            }
            return changed;
        }

        public static bool Normalize(StageProgressData data)
        {
            if (data == null)
            {
                return false;
            }

            bool changed = UpgradeVersion(ref data.saveVersion);
            List<StageProgress> source = data.stages ?? new List<StageProgress>();
            List<StageProgress> merged = new List<StageProgress>();
            foreach (IGrouping<string, StageProgress> group in source
                         .Where(item => item != null && !string.IsNullOrWhiteSpace(item.stageId))
                         .GroupBy(item => item.stageId))
            {
                StageProgress best = group.First();
                foreach (StageProgress candidate in group.Skip(1))
                {
                    best.isUnlocked |= candidate.isUnlocked;
                    best.isCleared |= candidate.isCleared;
                    best.stars = Math.Max(best.stars, candidate.stars);
                    best.clearCount = Math.Max(best.clearCount, candidate.clearCount);
                    best.bestMoves = BestNonNegative(best.bestMoves, candidate.bestMoves);
                    best.bestTimeSeconds = BestNonNegative(best.bestTimeSeconds, candidate.bestTimeSeconds);
                    if (string.CompareOrdinal(candidate.lastClearedAtUtc, best.lastClearedAtUtc) > 0)
                    {
                        best.lastClearedAtUtc = candidate.lastClearedAtUtc;
                    }
                }

                int normalizedStars = Math.Max(0, Math.Min(3, best.stars));
                int normalizedClearCount = Math.Max(0, best.clearCount);
                int normalizedMoves = best.bestMoves < -1 ? -1 : best.bestMoves;
                float normalizedTime = best.bestTimeSeconds < -1f ? -1f : best.bestTimeSeconds;
                changed |= normalizedStars != best.stars
                    || normalizedClearCount != best.clearCount
                    || normalizedMoves != best.bestMoves
                    || Math.Abs(normalizedTime - best.bestTimeSeconds) > 0.0001f;
                best.stars = normalizedStars;
                best.clearCount = normalizedClearCount;
                best.bestMoves = normalizedMoves;
                best.bestTimeSeconds = normalizedTime;
                merged.Add(best);
            }

            if (data.stages == null || merged.Count != source.Count)
            {
                changed = true;
            }

            data.stages = merged;
            return changed;
        }

        public static bool Normalize(StageMilestoneRewardData data)
        {
            if (data == null)
            {
                return false;
            }

            bool changed = UpgradeVersion(ref data.saveVersion);
            List<StageMilestoneReward> source = data.rewards ?? new List<StageMilestoneReward>();
            data.rewards = source
                .Where(item => item != null && item.blockIndex >= 0)
                .GroupBy(item => new { item.stageType, item.blockIndex })
                .Select(group =>
                {
                    StageMilestoneReward merged = group.First();
                    merged.isClaimed = group.Any(item => item.isClaimed);
                    merged.claimedByUser = group.Any(item => item.claimedByUser);
                    int requiredStars = merged.requiredStars <= 0 ? 30 : merged.requiredStars;
                    int rewardGems = Math.Max(0, merged.rewardGems);
                    changed |= requiredStars != merged.requiredStars || rewardGems != merged.rewardGems;
                    merged.requiredStars = requiredStars;
                    merged.rewardGems = rewardGems;
                    return merged;
                })
                .OrderBy(item => item.stageType)
                .ThenBy(item => item.blockIndex)
                .ToList();
            return changed || data.rewards.Count != source.Count;
        }

        public static bool Normalize(AdsRewardLimitData data, DateTime today)
        {
            if (data == null)
            {
                return false;
            }

            bool changed = UpgradeVersion(ref data.saveVersion);
            changed |= ClampNonNegative(ref data.dailyCoinsAdCount);
            changed |= ClampNonNegative(ref data.dailyShopCoinAdCount);
            changed |= ClampNonNegative(ref data.dailySolverBonusAdCount);
            changed |= ClampNonNegative(ref data.dailyHeartAdTotalCount);
            changed |= ClampNonNegative(ref data.heartAdBatchCount);
            string currentDate = today.ToString("yyyy-MM-dd");
            if (!DateTime.TryParseExact(
                    data.lastResetDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime savedDate)
                || savedDate.Date != today.Date)
            {
                data.dailyCoinsAdCount = 0;
                data.dailyShopCoinAdCount = 0;
                data.dailySolverBonusAdCount = 0;
                data.lastResetDate = currentDate;
                changed = true;
            }

            if (!DateTime.TryParseExact(
                    data.dailyHeartAdDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime savedHeartAdDate)
                || savedHeartAdDate.Date != today.Date)
            {
                data.dailyHeartAdDate = currentDate;
                data.dailyHeartAdTotalCount = 0;
                data.heartAdBatchCount = 0;
                data.heartAdBatchCooldownEndTime = string.Empty;
                changed = true;
            }

            return changed;
        }

        private static bool UpgradeVersion(ref int version)
        {
            if (version >= CurrentSaveVersion)
            {
                return false;
            }

            version = CurrentSaveVersion;
            return true;
        }

        private static bool ClampNonNegative(ref int value)
        {
            if (value >= 0)
            {
                return false;
            }

            value = 0;
            return true;
        }

        private static bool NormalizeIds(ref List<string> ids)
        {
            List<string> normalized = (ids ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            bool changed = ids == null || normalized.Count != ids.Count;
            ids = normalized;
            return changed;
        }

        private static bool EnsureId(List<string> ids, string id)
        {
            if (ids.Contains(id))
            {
                return false;
            }

            ids.Add(id);
            return true;
        }

        private static int BestNonNegative(int left, int right)
        {
            if (left < 0) return right;
            if (right < 0) return left;
            return Math.Min(left, right);
        }

        private static float BestNonNegative(float left, float right)
        {
            if (left < 0f) return right;
            if (right < 0f) return left;
            return Math.Min(left, right);
        }
    }
}
