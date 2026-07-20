using System;

namespace CubeChallenge3D.Records
{
    [Serializable]
    public sealed class RecordsSummary
    {
        public PlayerProfileSummary profile = new PlayerProfileSummary();
        public RankingChallengeSummary rankingChallenge = new RankingChallengeSummary();
        public QuickPlaySummary quickPlay = new QuickPlaySummary();
        public StageProgressSummary stageProgress = new StageProgressSummary();
        public InventorySummary inventory = new InventorySummary();
    }

    [Serializable]
    public sealed class PlayerProfileSummary
    {
        public bool hasProfile;
        public string profileId = string.Empty;
        public string nickname = string.Empty;
        public int avatarId = -1;
        public bool isServerSynced;
        public bool serverSyncPending;
    }

    [Serializable]
    public sealed class RankingChallengeSummary
    {
        public bool hasAnyRecord;
        public int totalPlays;
        public float bestTimeSeconds;
        public int bestMoveCount;
        public string bestChallengeId = string.Empty;
        public float latestTimeSeconds;
        public int latestMoveCount;
        public string latestChallengeId = string.Empty;
        public int pendingSubmissionCount;
    }

    [Serializable]
    public sealed class QuickPlaySummary
    {
        public bool hasAnyRecord;
        public float bestTimeSeconds;
        public int bestMoveCount;
        public int totalRecords;
    }

    [Serializable]
    public sealed class StageProgressSummary
    {
        public int totalStages;
        public int unlockedStages;
        public int clearedStages;
        public int totalStars;
        public int maxStars;
        public string currentWorldOrModeName = string.Empty;
        public int solveStageClearedCount;
        public int reverseStageClearedCount;
    }

    [Serializable]
    public sealed class InventorySummary
    {
        public int coins;
        public int gems;
        public int hintItemCount;
        public int plusOneItemCount;
        public int plusTwoItemCount;
        public int plusThreeItemCount;
        public int undoItemCount;
        public int solverTicketCount;
        public int ownedSkinCount;
        public string equippedSkinId = string.Empty;
    }
}
