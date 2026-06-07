using System;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class RankingChallengeConfig
    {
        public string challengeId;
        public int seed;
        public int scrambleLength = 20;
        public string dateUtc;

        public static RankingChallengeConfig CreateToday(int scrambleLength = 20)
        {
            DateTime utcNow = DateTime.UtcNow;
            return new RankingChallengeConfig
            {
                challengeId = DailyChallengeSeed.GetChallengeIdForDate(utcNow),
                seed = DailyChallengeSeed.GetSeedForDate(utcNow),
                scrambleLength = scrambleLength,
                dateUtc = utcNow.ToString("yyyy-MM-dd")
            };
        }
    }
}
