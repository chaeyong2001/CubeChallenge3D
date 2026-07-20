using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CubeChallenge3D.Ranking
{
    public sealed class LocalRankingService : IRankingService
    {
        private readonly LocalRankingStore store;
        private readonly CachedRankingStore cacheStore;

        public LocalRankingService(
            LocalRankingStore rankingStore,
            CachedRankingStore cachedStore = null,
            PendingRankingSubmissionStore pendingSubmissionStore = null)
        {
            store = rankingStore;
            cacheStore = cachedStore ?? new CachedRankingStore();
        }

        public Task<RankingSubmitResult> SubmitAsync(RankingSubmission submission)
        {
            if (submission == null || !submission.isVerified || submission.isDebugClear)
            {
                if (submission != null)
                {
                    submission.syncStatus = submission.isDebugClear
                        ? RankingSyncStatus.LocalOnly
                        : RankingSyncStatus.Rejected;
                }

                string message = submission != null && submission.isDebugClear
                    ? "DEV clear - not submitted"
                    : "Rejected by local verification.";
                return Task.FromResult(RankingSubmitResult.Rejected(submission, message));
            }

            submission.syncStatus = RankingSyncStatus.LocalOnly;
            submission.isSynced = false;
            store.AddSubmission(submission);
            return Task.FromResult(RankingSubmitResult.Success(submission, "Saved locally. Server: Not connected."));
        }

        public Task<RankingFetchResult> GetTopAsync(string challengeId, int maxCount)
        {
            List<RankingSubmission> records = store.GetTopByTime(challengeId, maxCount).ToList();
            cacheStore.SaveCache(challengeId, records);
            return Task.FromResult(new RankingFetchResult
            {
                success = true,
                fromCache = false,
                message = "Local Ranking. Server: Not connected.",
                records = records
            });
        }

        public Task<RankingFetchResult> GetMyRecordsAsync(string playerId, int maxCount)
        {
            List<RankingSubmission> records = store.GetPlayerRecords(playerId, maxCount).ToList();
            return Task.FromResult(new RankingFetchResult
            {
                success = true,
                fromCache = false,
                message = string.IsNullOrWhiteSpace(playerId)
                    ? "Create a profile first."
                    : "Local Records. Server: Not connected.",
                records = records
            });
        }

        public Task<RankingRankResult> GetRankAsync(string challengeId, string playerId, string submissionId)
        {
            return Task.FromResult(store.GetRank(challengeId, submissionId));
        }

        public RankingFetchResult GetCachedTop(string challengeId, int maxCount)
        {
            return new RankingFetchResult
            {
                success = true,
                fromCache = true,
                message = "Cached ranking.",
                records = cacheStore.GetCache(challengeId).Take(maxCount).ToList()
            };
        }

        public Task RetryPendingAsync()
        {
            // Local mode does not mark pending records as failed.
            return Task.CompletedTask;
        }
    }
}
