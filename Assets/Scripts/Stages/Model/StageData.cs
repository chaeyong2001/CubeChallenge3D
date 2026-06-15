using System;

namespace CubeChallenge3D.Stages.Model
{
    [Serializable]
    public sealed class StageData
    {
        public string stageId;
        public int stageNumber;
        public StageType stageType;
        public StageDifficulty difficulty;
        public string title;
        public string description;
        public string startStateFacelets;
        public string targetStateFacelets;
        public string scrambleNotation;
        public string solutionNotation;
        public int generatedSeed;
        public string generationGroup;
        public int minimumMoves;
        public int minMoveCount;
        public int moveLimit;
        public int starMoveLimit1;
        public int starMoveLimit2;
        public int starMoveLimit3;
        public bool isUnlockedByDefault;
        public string unlockAfterStageId;
        public int rewardCoins;
    }
}
