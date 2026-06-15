using System;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Assist
{
    [Serializable]
    public sealed class StageAssistState
    {
        public int freeUndoRemaining;
        public int paidUndoUsed;
        public int adContinueCount;
        public int moveItemUseCount;
        public int assistUseCount;
        public int bonusMovesAdded;
        public int hintCount;
        public bool usedContinue;
        public bool usedMoveItem;
        public bool usedHint;
        public int originalMoveLimit;
        public int currentMoveLimit;

        public static StageAssistState Create(StageData stage)
        {
            int moveLimit = stage != null ? stage.moveLimit : 0;
            return new StageAssistState
            {
                freeUndoRemaining = GetFreeUndoCount(stage != null ? stage.difficulty : StageDifficulty.Easy),
                originalMoveLimit = moveLimit,
                currentMoveLimit = moveLimit
            };
        }

        public static int GetFreeUndoCount(StageDifficulty difficulty)
        {
            switch (difficulty)
            {
                case StageDifficulty.Easy:
                    return 3;
                case StageDifficulty.Normal:
                    return 2;
                case StageDifficulty.Hard:
                case StageDifficulty.Expert:
                    return 1;
                default:
                    return 1;
            }
        }
    }
}
