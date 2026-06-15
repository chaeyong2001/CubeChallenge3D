using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Services;

namespace CubeChallenge3D.Stages.Generation
{
    public static class StagePackValidator
    {
        public static StageValidationResult Validate(StageDataCollection collection)
        {
            StageValidationResult result = StageDataValidator.ValidateAll(collection?.stages);
            if (collection?.stages == null)
            {
                return result;
            }

            var stageNumbers = new HashSet<int>();
            var solveStates = new HashSet<string>(StringComparer.Ordinal);
            var targetStates = new HashSet<string>(StringComparer.Ordinal);
            var solveNotations = new HashSet<string>(StringComparer.Ordinal);
            var targetNotations = new HashSet<string>(StringComparer.Ordinal);
            foreach (StageData stage in collection.stages)
            {
                if (stage == null)
                {
                    continue;
                }

                if (!stageNumbers.Add(stage.stageNumber))
                {
                    result.AddError($"Duplicate stageNumber: {stage.stageNumber}");
                }

                if (stage.starMoveLimit3 < stage.minimumMoves
                    || stage.starMoveLimit2 < stage.starMoveLimit3
                    || stage.starMoveLimit1 < stage.starMoveLimit2
                    || stage.moveLimit < stage.starMoveLimit1)
                {
                    result.AddError($"{stage.stageId}: invalid star move limits.");
                }

                try
                {
                    ValidateGeneratedState(
                        stage,
                        solveStates,
                        targetStates,
                        solveNotations,
                        targetNotations,
                        result);
                }
                catch (Exception exception)
                {
                    result.AddError($"{stage.stageId}: state validation failed. {exception.Message}");
                }
            }

            int solveCount = collection.stages.Count(stage => stage != null && stage.stageType == StageType.SolveStage);
            int targetCount = collection.stages.Count(stage => stage != null && stage.stageType == StageType.ReverseTargetStage);
            if (solveCount != 100 || targetCount != 100)
            {
                result.AddError($"Expected 100 Solve and 100 Reverse Target stages, found {solveCount} and {targetCount}.");
            }

            for (int start = 1; start <= 200; start += 10)
            {
                int blockCount = collection.stages.Count(
                    stage => stage != null && stage.stageNumber >= start && stage.stageNumber < start + 10);
                if (blockCount != 10)
                {
                    result.AddError($"Milestone block {start}-{start + 9} contains {blockCount} stages.");
                }
            }

            return result;
        }

        private static void ValidateGeneratedState(
            StageData stage,
            HashSet<string> solveStates,
            HashSet<string> targetStates,
            HashSet<string> solveNotations,
            HashSet<string> targetNotations,
            StageValidationResult result)
        {
            IReadOnlyList<CubeMove> solution = MoveUtility.ParseSequence(stage.solutionNotation);
            string normalizedNotation = MoveUtility.ToNotationSequence(solution);
            if (solution.Count < 5)
            {
                result.AddError($"{stage.stageId}: solution contains fewer than 5 moves.");
            }

            if (stage.stageType == StageType.SolveStage)
            {
                CubeState start = CubeStateSerializer.FromFaceletString(stage.startStateFacelets);
                CubeState check = start.Clone();
                check.ApplyMoves(solution);
                if (!check.IsSolved())
                {
                    result.AddError($"{stage.stageId}: solution does not solve startStateFacelets.");
                }

                if (!solveStates.Add(stage.startStateFacelets))
                {
                    result.AddError($"{stage.stageId}: duplicate Solve stage state.");
                }

                if (!solveNotations.Add(normalizedNotation))
                {
                    result.AddError($"{stage.stageId}: duplicate normalized Solve notation.");
                }
            }
            else
            {
                CubeState target = CubeStateSerializer.FromFaceletString(stage.targetStateFacelets);
                CubeState check = CubeState.CreateSolved();
                check.ApplyMoves(solution);
                if (!check.Equals(target))
                {
                    result.AddError($"{stage.stageId}: solution does not create targetStateFacelets.");
                }

                if (!targetStates.Add(stage.targetStateFacelets))
                {
                    result.AddError($"{stage.stageId}: duplicate Reverse Target state.");
                }

                if (!targetNotations.Add(normalizedNotation))
                {
                    result.AddError($"{stage.stageId}: duplicate normalized Reverse Target notation.");
                }
            }
        }
    }
}
