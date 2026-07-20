using System;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class RankingRankResult
    {
        public bool success;
        public string message;
        public int rank;
        public RankingSubmission record;
    }
}
