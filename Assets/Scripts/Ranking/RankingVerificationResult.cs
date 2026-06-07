using System;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class RankingVerificationResult
    {
        public bool isValid;
        public string reason;
        public int parsedScrambleCount;
        public int parsedMoveCount;

        public static RankingVerificationResult Valid(int scrambleCount, int moveCount)
        {
            return new RankingVerificationResult
            {
                isValid = true,
                reason = "Verified",
                parsedScrambleCount = scrambleCount,
                parsedMoveCount = moveCount
            };
        }

        public static RankingVerificationResult Invalid(string reason, int scrambleCount = 0, int moveCount = 0)
        {
            return new RankingVerificationResult
            {
                isValid = false,
                reason = reason,
                parsedScrambleCount = scrambleCount,
                parsedMoveCount = moveCount
            };
        }
    }
}
