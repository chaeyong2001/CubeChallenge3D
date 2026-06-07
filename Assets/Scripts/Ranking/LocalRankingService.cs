using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CubeChallenge3D.Ranking
{
    public sealed class LocalRankingService : IRankingService
    {
        private readonly LocalRankingStore store;
        private readonly CachedRankingStore cacheStore;
        private readonly PendingRankingSubmissionStore pendingStore;

        public LocalRankingService(
            LocalRankingStore rankingStore,
            CachedRankingStore cachedStore = null,
            PendingRankingSubmissionStore pendingSubmissionStore = null)
        {
            store = rankingStore;
            cacheStore = cachedStore ?? new CachedRankingStore();
            pendingStore = pendingSubmissionStore ?? new PendingRankingSubmissionStore();
        }

        public Task<RankingSubmitResult> SubmitAsync(RankingSubmission submission)
        {
            if (submission == null || !submission.isVerified)
            {
                if (submission != null)
                {
                    submission.syncStatus = RankingSyncStatus.Rejected;
                }

                return Task.FromResult(RankingSubmitResult.Rejected(submission, "Rejected by local verification."));
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
            // Network submission is intentionally not implemented in 7-B.
            foreach (RankingSubmission submission in pendingStore.GetPending())
            {
                pendingStore.MarkFailed(submission.submissionId, "Server service is not connected.");
            }

            return Task.CompletedTask;
        }
    }
}
