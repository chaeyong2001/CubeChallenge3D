using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Services
{
    public static class StageDataValidator
    {
        public static StageValidationResult ValidateStage(StageData stage)
        {
            var result = new StageValidationResult();
            if (stage == null)
            {
                result.AddError("Stage is null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(stage.stageId))
            {
                result.AddError("stageId is empty.");
            }

            if (stage.stageNumber <= 0)
            {
                result.AddError($"{stage.stageId}: stageNumber must be greater than 0.");
            }

            if (!Enum.IsDefined(typeof(StageType), stage.stageType))
            {
                result.AddError($"{stage.stageId}: invalid stageType.");
            }

            if (stage.moveLimit <= 0)
            {
                result.AddError($"{stage.stageId}: moveLimit must be greater than 0.");
            }

            int minimumMoves = GetMinimumMoves(stage);
            if (minimumMoves < 0)
            {
                result.AddError($"{stage.stageId}: minimumMoves must be 0 or greater.");
            }

            if (minimumMoves < 5)
            {
                result.AddError($"{stage.stageId}: playable stages should use minimumMoves of 5 or greater.");
            }

            if (stage.moveLimit < minimumMoves)
            {
                result.AddError($"{stage.stageId}: moveLimit must be greater than or equal to minimumMoves.");
            }

            if (stage.starMoveLimit3 < minimumMoves
                || stage.starMoveLimit2 < stage.starMoveLimit3
                || stage.starMoveLimit1 < stage.starMoveLimit2
                || stage.moveLimit < stage.starMoveLimit1)
            {
                result.AddError($"{stage.stageId}: star move limits must satisfy minimum <= 3-star <= 2-star <= 1-star <= moveLimit.");
            }

            ValidateNotation(stage.stageId, "scrambleNotation", stage.scrambleNotation, result);
            ValidateNotation(stage.stageId, "solutionNotation", stage.solutionNotation, result);
            return result;
        }

        public static StageValidationResult ValidateAll(IReadOnlyList<StageData> stages)
        {
            var result = new StageValidationResult();
            if (stages == null)
            {
                result.AddError("Stage list is null.");
                return result;
            }

            var ids = new HashSet<string>();
            var stageNumbers = new HashSet<int>();
            foreach (StageData stage in stages)
            {
                StageValidationResult stageResult = ValidateStage(stage);
                if (!stageResult.isValid)
                {
                    foreach (string message in stageResult.messages)
                    {
                        result.AddError(message);
                    }
                }

                if (stage == null || string.IsNullOrWhiteSpace(stage.stageId))
                {
                    continue;
                }

                if (!ids.Add(stage.stageId))
                {
                    result.AddError($"Duplicate stageId: {stage.stageId}");
                }

                if (!stageNumbers.Add(stage.stageNumber))
                {
                    result.AddError($"Duplicate stageNumber: {stage.stageNumber}");
                }
            }

            foreach (StageData stage in stages.Where(item => item != null && !string.IsNullOrWhiteSpace(item.unlockAfterStageId)))
            {
                if (!ids.Contains(stage.unlockAfterStageId))
                {
                    result.AddError($"{stage.stageId}: unlockAfterStageId not found: {stage.unlockAfterStageId}");
                }
            }

            return result;
        }

        private static void ValidateNotation(string stageId, string fieldName, string notation, StageValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(notation))
            {
                return;
            }

            try
            {
                MoveUtility.ParseSequence(notation);
            }
            catch (Exception exception)
            {
                result.AddError($"{stageId}: {fieldName} parse failed. {exception.Message}");
            }
        }

        private static int GetMinimumMoves(StageData stage)
        {
            return stage.minimumMoves > 0 ? stage.minimumMoves : stage.minMoveCount;
        }
    }
}
