using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.Save.Records;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using UnityEngine;

namespace CubeChallenge3D.Records
{
    public sealed class RecordsSummaryService
    {
        private readonly PlayerProfileStore profileStore;
        private readonly LocalRankingStore rankingStore;
        private readonly PendingRankingSubmissionStore pendingStore;
        private readonly QuickPlayRecordStore quickPlayStore;
        private readonly StageProgressStore stageProgressStore;
        private readonly StageDataLoader stageLoader;
        private readonly WalletStore walletStore;
        private readonly InventoryStore inventoryStore;

        public RecordsSummaryService()
            : this(
                new PlayerProfileStore(),
                new LocalRankingStore(),
                new PendingRankingSubmissionStore(),
                new QuickPlayRecordStore(),
                new StageProgressStore(),
                new StageDataLoader(),
                new WalletStore(),
                new InventoryStore())
        {
        }

        public RecordsSummaryService(
            PlayerProfileStore profileStore,
            LocalRankingStore rankingStore,
            PendingRankingSubmissionStore pendingStore,
            QuickPlayRecordStore quickPlayStore,
            StageProgressStore stageProgressStore,
            StageDataLoader stageLoader,
            WalletStore walletStore,
            InventoryStore inventoryStore)
        {
            this.profileStore = profileStore;
            this.rankingStore = rankingStore;
            this.pendingStore = pendingStore;
            this.quickPlayStore = quickPlayStore;
            this.stageProgressStore = stageProgressStore;
            this.stageLoader = stageLoader;
            this.walletStore = walletStore;
            this.inventoryStore = inventoryStore;
        }

        public RecordsSummary BuildSummary()
        {
            RecordsSummary summary = new RecordsSummary();
            FillProfile(summary.profile);
            FillRanking(summary.rankingChallenge, summary.profile);
            FillQuickPlay(summary.quickPlay);
            FillStageProgress(summary.stageProgress);
            FillInventory(summary.inventory);
            Debug.Log(BuildLog(summary));
            return summary;
        }

        private void FillProfile(PlayerProfileSummary summary)
        {
            PlayerProfile profile = profileStore?.Current;
            summary.hasProfile = profile != null && !string.IsNullOrWhiteSpace(profile.profileId);
            if (!summary.hasProfile)
            {
                return;
            }

            summary.profileId = profile.profileId ?? string.Empty;
            summary.nickname = profile.nickname ?? string.Empty;
            summary.avatarId = profile.avatarId;
            summary.isServerSynced = profile.isServerSynced;
            summary.serverSyncPending = profile.serverSyncPending;
        }

        private void FillRanking(RankingChallengeSummary summary, PlayerProfileSummary profile)
        {
            string playerId = profile != null && profile.hasProfile ? profile.profileId : string.Empty;
            List<RankingSubmission> records = rankingStore?.GetPlayerRecords(playerId, int.MaxValue).ToList()
                ?? new List<RankingSubmission>();
            summary.totalPlays = records.Count;
            summary.hasAnyRecord = records.Count > 0;
            summary.pendingSubmissionCount = pendingStore?.GetPending().Count ?? 0;
            if (records.Count == 0)
            {
                return;
            }

            RankingSubmission best = records
                .OrderBy(record => record.elapsedSeconds)
                .ThenBy(record => record.moveCount)
                .ThenBy(record => ParseUtc(record.completedAtUtc))
                .FirstOrDefault();
            RankingSubmission latest = records
                .OrderByDescending(record => ParseUtc(record.completedAtUtc))
                .FirstOrDefault();

            if (best != null)
            {
                summary.bestTimeSeconds = best.elapsedSeconds;
                summary.bestMoveCount = best.moveCount;
                summary.bestChallengeId = best.challengeId ?? string.Empty;
            }

            if (latest != null)
            {
                summary.latestTimeSeconds = latest.elapsedSeconds;
                summary.latestMoveCount = latest.moveCount;
                summary.latestChallengeId = latest.challengeId ?? string.Empty;
            }
        }

        private void FillQuickPlay(QuickPlaySummary summary)
        {
            QuickPlayResult bestTime = quickPlayStore?.GetBestByTime();
            QuickPlayResult bestMoves = quickPlayStore?.GetBestByMoves();
            summary.totalRecords = quickPlayStore?.Count ?? 0;
            summary.hasAnyRecord = bestTime != null;
            if (bestTime != null)
            {
                summary.bestTimeSeconds = bestTime.elapsedSeconds;
            }

            if (bestMoves != null)
            {
                summary.bestMoveCount = bestMoves.moveCount;
            }
        }

        private void FillStageProgress(StageProgressSummary summary)
        {
            IReadOnlyList<StageData> stages = stageLoader?.LoadAllStages() ?? new List<StageData>().AsReadOnly();
            summary.totalStages = stages.Count;
            summary.maxStars = stages.Count * 3;
            summary.currentWorldOrModeName = stages.Count > 0 ? "Stages" : string.Empty;
            foreach (StageData stage in stages.Where(stage => stage != null))
            {
                StageProgress progress = stageProgressStore?.GetProgress(stage.stageId);
                if (progress == null)
                {
                    continue;
                }

                if (progress.isUnlocked || stage.isUnlockedByDefault)
                {
                    summary.unlockedStages++;
                }

                if (!progress.isCleared)
                {
                    continue;
                }

                summary.clearedStages++;
                summary.totalStars += Math.Max(0, Math.Min(3, progress.stars));
                if (stage.stageType == StageType.SolveStage)
                {
                    summary.solveStageClearedCount++;
                }
                else if (stage.stageType == StageType.ReverseTargetStage)
                {
                    summary.reverseStageClearedCount++;
                }
            }
        }

        private void FillInventory(InventorySummary summary)
        {
            EconomyWallet wallet = walletStore?.Data;
            InventoryData inventory = inventoryStore?.Data;
            summary.coins = wallet != null ? wallet.coins : 0;
            summary.gems = wallet != null ? wallet.gems : 0;
            summary.hintItemCount = 0;
            summary.plusOneItemCount = inventory != null ? inventory.movePlus1Items : 0;
            summary.plusTwoItemCount = inventory != null ? inventory.movePlus2Items : 0;
            summary.plusThreeItemCount = inventory != null ? inventory.movePlus3Items : 0;
            summary.undoItemCount = inventory != null ? inventory.undoItems : 0;
            summary.solverTicketCount = inventory != null ? inventory.solverTickets : 0;
            summary.ownedSkinCount = inventory?.ownedSkinIds != null ? inventory.ownedSkinIds.Count : 0;
            summary.equippedSkinId = inventory?.selectedSkinId ?? string.Empty;
        }

        private static DateTime ParseUtc(string value)
        {
            return DateTime.TryParse(value, out DateTime parsed)
                ? parsed.ToUniversalTime()
                : DateTime.MinValue;
        }

        private static string BuildLog(RecordsSummary summary)
        {
            return "[RecordsSummary] "
                + $"profile={summary.profile.hasProfile}:{summary.profile.nickname} "
                + $"ranking={summary.rankingChallenge.totalPlays}/pending={summary.rankingChallenge.pendingSubmissionCount} "
                + $"quick={summary.quickPlay.totalRecords} "
                + $"stages={summary.stageProgress.clearedStages}/{summary.stageProgress.totalStages} "
                + $"stars={summary.stageProgress.totalStars}/{summary.stageProgress.maxStars} "
                + $"wallet={summary.inventory.coins}c/{summary.inventory.gems}g "
                + $"skins={summary.inventory.ownedSkinCount}:{summary.inventory.equippedSkinId}";
        }
    }
}
