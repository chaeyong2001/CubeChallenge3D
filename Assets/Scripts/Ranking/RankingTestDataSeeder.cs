using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CubeChallenge3D.Ranking
{
    public static class RankingTestDataSeeder
    {
        private const int TestRecordCount = 80;
        private const string TestPlayerIdPrefix = "ranking_test_world_";
        private const string TestPlayerNamePrefix = "WorldTest";

        public static void EnsureWorldRankingTestData(
            RankingChallengeConfig config,
            LocalRankingStore localStore,
            CachedRankingStore cacheStore)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.challengeId))
            {
                return;
            }

            bool hasLocalTestData = localStore != null
                && localStore.GetTopByTime(config.challengeId, TestRecordCount)
                    .Count(record => IsTestRecord(record)) >= TestRecordCount;
            bool hasCachedTestData = cacheStore != null
                && cacheStore.GetCache(config.challengeId)
                    .Count(record => IsTestRecord(record)) >= TestRecordCount;

            if (hasLocalTestData && hasCachedTestData)
            {
                Debug.Log($"[RankingTestData] Existing {TestRecordCount} dummy ranking records found for {config.challengeId}. Seed skipped.");
                return;
            }

            List<RankingSubmission> records = CreateTestRecords(config.challengeId);

            if (localStore != null && !hasLocalTestData)
            {
                foreach (RankingSubmission record in records)
                {
                    localStore.AddSubmission(record);
                }
            }

            if (cacheStore != null && !hasCachedTestData)
            {
                cacheStore.SaveCache(config.challengeId, records);
            }

            Debug.Log($"[RankingTestData] Seeded {TestRecordCount} dummy world ranking records for {config.challengeId}.");
        }

        private static List<RankingSubmission> CreateTestRecords(string challengeId)
        {
            var records = new List<RankingSubmission>(TestRecordCount);
            DateTime baseTime = DateTime.UtcNow.AddMinutes(-TestRecordCount);

            for (int index = 1; index <= TestRecordCount; index++)
            {
                records.Add(new RankingSubmission
                {
                    submissionId = $"ranking-test-{challengeId}-{index:000}",
                    challengeId = challengeId,
                    playerId = $"{TestPlayerIdPrefix}{index:000}",
                    playerName = $"{TestPlayerNamePrefix}{index:000}",
                    avatarId = (index - 1) % 4,
                    elapsedSeconds = 28.75f + (index * 1.43f),
                    moveCount = 18 + index,
                    scrambleNotation = "R U R' U' F2 D L2 B'",
                    moveLogNotation = "R U R' U' F2 D L2 B'",
                    controlMode = "Touch",
                    completedAtUtc = baseTime.AddMinutes(index).ToString("o"),
                    completed = true,
                    isVerified = true,
                    isSynced = true,
                    isDebugClear = false,
                    clearSource = "RankingTestData",
                    syncStatus = RankingSyncStatus.Synced,
                    clientVersion = "test-data",
                    deviceIdHash = $"{TestPlayerIdPrefix}{index:000}"
                });
            }

            return records;
        }

        private static bool IsTestRecord(RankingSubmission record)
        {
            return record != null
                && !string.IsNullOrWhiteSpace(record.playerId)
                && record.playerId.StartsWith(TestPlayerIdPrefix, StringComparison.Ordinal);
        }
    }
}
