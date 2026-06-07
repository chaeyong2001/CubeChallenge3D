using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class RankingFetchResult
    {
        public bool success;
        public bool fromCache;
        public string message;
        public List<RankingSubmission> records = new List<RankingSubmission>();
    }
}
