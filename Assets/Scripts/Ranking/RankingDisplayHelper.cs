using System;
using System.Collections.Generic;
using System.Linq;

namespace CubeChallenge3D.Ranking
{
    public static class RankingDisplayHelper
    {
        public static IEnumerable<RankingSubmission> FilterVerifiedOnly(IEnumerable<RankingSubmission> records)
        {
            return Safe(records).Where(record => record.completed && record.isVerified);
        }

        public static IEnumerable<RankingSubmission> ExcludeDebugClear(IEnumerable<RankingSubmission> records)
        {
            return Safe(records).Where(record => !record.isDebugClear);
        }

        public static IEnumerable<RankingSubmission> BestPerPlayer(IEnumerable<RankingSubmission> records)
        {
            return SortByTimeThenMoves(records)
                .GroupBy(GetPlayerKey)
                .Select(group => group.First());
        }

        public static IEnumerable<RankingSubmission> SortByTimeThenMoves(IEnumerable<RankingSubmission> records)
        {
            return Safe(records)
                .OrderBy(record => record.elapsedSeconds)
                .ThenBy(record => record.moveCount)
                .ThenBy(record => ParseCompletedAt(record.completedAtUtc));
        }

        public static List<RankingSubmission> TakeTop(IEnumerable<RankingSubmission> records, int maxCount)
        {
            return BestPerPlayer(ExcludeDebugClear(FilterVerifiedOnly(records)))
                .Take(Math.Max(0, maxCount))
                .ToList();
        }

        private static IEnumerable<RankingSubmission> Safe(IEnumerable<RankingSubmission> records)
        {
            return records?.Where(record => record != null) ?? Enumerable.Empty<RankingSubmission>();
        }

        private static string GetPlayerKey(RankingSubmission record)
        {
            if (!string.IsNullOrWhiteSpace(record.playerId))
            {
                return record.playerId;
            }

            if (!string.IsNullOrWhiteSpace(record.playerName))
            {
                return record.playerName;
            }

            return record.submissionId ?? string.Empty;
        }

        private static DateTime ParseCompletedAt(string value)
        {
            return DateTime.TryParse(value, out DateTime parsed) ? parsed : DateTime.MaxValue;
        }
    }
}
