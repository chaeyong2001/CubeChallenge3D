using System;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class RankingSubmission
    {
        public string submissionId;
        public string challengeId;
        public string playerId;
        public string playerName;
        public float elapsedSeconds;
        public int moveCount;
        public string scrambleNotation;
        public string moveLogNotation;
        public string controlMode;
        public string completedAtUtc;
        public bool completed;
        public bool isVerified;
        public bool isSynced;
        public string syncStatus;
        public string clientVersion;
        public string deviceIdHash;

        public static RankingSubmission Create(
            string challengeId,
            string playerId,
            string playerName,
            float elapsedSeconds,
            int moveCount,
            string scrambleNotation,
            string moveLogNotation,
            string controlMode)
        {
            return new RankingSubmission
            {
                submissionId = Guid.NewGuid().ToString(),
                challengeId = challengeId,
                playerId = string.IsNullOrWhiteSpace(playerId) ? "local" : playerId,
                playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName,
                elapsedSeconds = elapsedSeconds,
                moveCount = moveCount,
                scrambleNotation = scrambleNotation,
                moveLogNotation = moveLogNotation,
                controlMode = controlMode,
                completedAtUtc = DateTime.UtcNow.ToString("o"),
                completed = true,
                isVerified = false,
                isSynced = false,
                syncStatus = RankingSyncStatus.LocalOnly,
                clientVersion = "local",
                deviceIdHash = string.IsNullOrWhiteSpace(playerId) ? string.Empty : playerId
            };
        }
    }
}
