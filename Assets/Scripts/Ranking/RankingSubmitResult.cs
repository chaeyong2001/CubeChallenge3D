using System;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class RankingSubmitResult
    {
        public bool success;
        public bool isPending;
        public bool isRejected;
        public string message;
        public RankingSubmission submission;

        public static RankingSubmitResult Success(RankingSubmission submission, string message)
        {
            return new RankingSubmitResult { success = true, message = message, submission = submission };
        }

        public static RankingSubmitResult Rejected(RankingSubmission submission, string message)
        {
            return new RankingSubmitResult { isRejected = true, message = message, submission = submission };
        }
    }
}
