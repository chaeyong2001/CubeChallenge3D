using System.Collections.Generic;
using System.Threading.Tasks;

namespace CubeChallenge3D.Ranking
{
    public interface IRankingService
    {
        Task<RankingSubmitResult> SubmitAsync(RankingSubmission submission);
        Task<RankingFetchResult> GetTopAsync(string challengeId, int maxCount);
        RankingFetchResult GetCachedTop(string challengeId, int maxCount);
        Task RetryPendingAsync();
    }
}
