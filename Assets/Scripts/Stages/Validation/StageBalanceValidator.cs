using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Stages.Generation;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Validation
{
    public static class StageBalanceValidator
    {
        public static StageValidationResult Validate(IReadOnlyList<StageData> stages)
        {
            var result = new StageValidationResult();
            if (stages == null || stages.Count == 0)
            {
                result.AddError("Stage balance validation requires stage data.");
                return result;
            }

            foreach (StageData stage in stages.Where(stage => stage != null))
            {
                int localNumber = GetLocalNumber(stage);
                int minimum = stage.minimumMoves > 0 ? stage.minimumMoves : stage.minMoveCount;

                if (minimum < 5)
                {
                    result.AddError($"{stage.stageId}: minimumMoves is below 5.");
                }

                if (stage.stageType != StageType.InfinityStage && localNumber >= 1 && localNumber <= 10)
                {
                    if (minimum < 5 || minimum > 7)
                    {
                        result.AddError($"{stage.stageId}: onboarding minimumMoves must be 5-7.");
                    }

                    if (stage.moveLimit - minimum < 6)
                    {
                        result.AddError($"{stage.stageId}: onboarding move margin should be at least 6.");
                    }
                }

                if (stage.moveLimit < minimum
                    || stage.starMoveLimit3 > stage.starMoveLimit2
                    || stage.starMoveLimit2 > stage.starMoveLimit1
                    || stage.starMoveLimit1 > stage.moveLimit)
                {
                    result.AddError($"{stage.stageId}: invalid move or star threshold order.");
                }

                int minReward;
                int maxReward;
                GetRewardRange(stage.difficulty, out minReward, out maxReward);
                if (stage.rewardCoins < minReward || stage.rewardCoins > maxReward)
                {
                    result.AddError($"{stage.stageId}: rewardCoins {stage.rewardCoins} is outside {stage.difficulty} range {minReward}-{maxReward}.");
                }
            }

            ValidateBlockCurve(stages, StageType.SolveStage, result);
            ValidateBlockCurve(stages, StageType.ReverseTargetStage, result);
            ValidateBlockCurve(stages, StageType.InfinityStage, result);
            ValidateBlockCurve(stages, StageType.TutorialStage, result);
            return result;
        }

        private static void ValidateBlockCurve(
            IReadOnlyList<StageData> stages,
            StageType type,
            StageValidationResult result)
        {
            double previousAverage = 0d;
            int stageCount = stages.Count(stage => stage != null && stage.stageType == type);
            int blockCount = (stageCount + 9) / 10;
            for (int block = 0; block < blockCount; block++)
            {
                int localStart = block * 10 + 1;
                List<StageData> blockStages = stages.Where(stage =>
                        stage != null
                        && stage.stageType == type
                        && GetLocalNumber(stage) >= localStart
                        && GetLocalNumber(stage) < localStart + 10)
                    .ToList();
                if (blockStages.Count != 10)
                {
                    result.AddError($"{type} block {block + 1}: expected 10 stages, found {blockStages.Count}.");
                    continue;
                }

                double average = blockStages.Average(stage =>
                    stage.minimumMoves > 0 ? stage.minimumMoves : stage.minMoveCount);
                if (block > 0 && average < previousAverage)
                {
                    result.AddError($"{type} block {block + 1}: average minimumMoves decreased.");
                }

                previousAverage = average;
            }
        }

        private static int GetLocalNumber(StageData stage)
        {
            string id = stage.stageId ?? string.Empty;
            int separator = id.LastIndexOf('_');
            if (separator >= 0
                && separator < id.Length - 1
                && int.TryParse(id.Substring(separator + 1), out int parsed)
                && parsed > 0)
            {
                return parsed;
            }

            if (stage.stageType == StageType.ReverseTargetStage)
            {
                return stage.stageNumber - StagePackGenerator.NormalStageCount;
            }

            if (stage.stageType == StageType.InfinityStage)
            {
                return stage.stageNumber - (StagePackGenerator.NormalStageCount + StagePackGenerator.HardStageCount);
            }

            if (stage.stageType == StageType.TutorialStage)
            {
                return stage.stageNumber - StagePackGenerator.TutorialFirstStageNumber + 1;
            }

            return stage.stageNumber;
        }

        private static void GetRewardRange(StageDifficulty difficulty, out int minimum, out int maximum)
        {
            switch (difficulty)
            {
                case StageDifficulty.Easy:
                    minimum = 30;
                    maximum = 60;
                    break;
                case StageDifficulty.Normal:
                    minimum = 50;
                    maximum = 90;
                    break;
                case StageDifficulty.Hard:
                    minimum = 80;
                    maximum = 130;
                    break;
                default:
                    minimum = 120;
                    maximum = 180;
                    break;
            }
        }
    }
}
