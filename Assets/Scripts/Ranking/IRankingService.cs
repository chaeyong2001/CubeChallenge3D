using System.Collections.Generic;
using System.Threading.Tasks;

namespace CubeChallenge3D.Ranking
{
    public interface IRankingService
    {
        Task<RankingSubmitResult> SubmitAsync(RankingSubmission submission);
        Task<RankingFetchResult> GetTopAsync(string challengeId, int maxCount);
        Task<RankingFetchResult> GetMyRecordsAsync(string playerId, int maxCount);
        Task<RankingRankResult> GetRankAsync(string challengeId, string playerId, string submissionId);
        RankingFetchResult GetCachedTop(string challengeId, int maxCount);
        Task RetryPendingAsync();
    }
}
