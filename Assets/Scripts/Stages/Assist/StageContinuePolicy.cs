using CubeChallenge3D.Ads;

namespace CubeChallenge3D.Stages.Assist
{
    public static class StageContinuePolicy
    {
        public const int MaxAdContinuesPerStage = 2;
        public const int MovesPerAdContinue = 2;

        public static bool CanContinue(StageAssistState assistState, IRewardService rewardService)
        {
            return assistState != null
                && assistState.adContinueCount < MaxAdContinuesPerStage
                && rewardService != null
                && rewardService.IsRewardAvailable(RewardType.ContinueStage);
        }

        public static void ApplyAdContinue(StageAssistState assistState, int movesReward = MovesPerAdContinue)
        {
            if (assistState == null)
            {
                return;
            }

            assistState.adContinueCount++;
            assistState.assistUseCount++;
            int moves = movesReward > 0 ? movesReward : MovesPerAdContinue;
            assistState.bonusMovesAdded += moves;
            assistState.currentMoveLimit += moves;
            assistState.usedContinue = true;
        }

        public static int ApplyAssistStarCap(int baseStars, int assistUseCount)
        {
            if (assistUseCount >= 3)
            {
                return baseStars > 0 ? 1 : 0;
            }

            if (assistUseCount >= 1)
            {
                return baseStars > 2 ? 2 : baseStars;
            }

            return baseStars;
        }
    }
}
