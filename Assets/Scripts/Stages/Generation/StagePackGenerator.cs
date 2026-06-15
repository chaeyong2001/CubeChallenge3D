using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Generation
{
    public sealed class StagePackGenerator
    {
        public const int DefaultSolveSeed = 15001;
        public const int DefaultTargetSeed = 15101;
        private const int MaxCandidateRetries = 500;
        private static readonly CubeFace[] Faces =
        {
            CubeFace.Up, CubeFace.Down, CubeFace.Front,
            CubeFace.Back, CubeFace.Right, CubeFace.Left
        };
        private static readonly int[] Turns = { 1, -1, 2 };
        private static readonly HashSet<string> SolvedWithinTwoMoves = BuildSolvedWithinTwoMoves();

        public List<StageData> GenerateSolveStages(int count, int seed)
        {
            return Generate(count, seed, StageType.SolveStage, 1, "solve_pack_v1");
        }

        public List<StageData> GenerateReverseTargetStages(int count, int seed)
        {
            return Generate(count, seed, StageType.ReverseTargetStage, 101, "target_pack_v1");
        }

        public StageDataCollection GenerateFullStagePack()
        {
            var collection = new StageDataCollection();
            collection.stages.AddRange(GenerateSolveStages(100, DefaultSolveSeed));
            collection.stages.AddRange(GenerateReverseTargetStages(100, DefaultTargetSeed));
            return collection;
        }

        private static List<StageData> Generate(
            int count,
            int seed,
            StageType type,
            int firstStageNumber,
            string generationGroup)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var random = new Random(seed);
            var stages = new List<StageData>(count);
            var sequenceKeys = new HashSet<string>(StringComparer.Ordinal);
            var stateKeys = new HashSet<string>(StringComparer.Ordinal);
            int previousLength = 5;

            for (int index = 0; index < count; index++)
            {
                int localNumber = index + 1;
                int targetLength = Math.Max(previousLength, GetTargetLength(localNumber, type, random));
                StageData stage = null;

                for (int retry = 0; retry < MaxCandidateRetries; retry++)
                {
                    List<CubeMove> moves = GenerateMoves(targetLength, random);
                    string notation = MoveUtility.ToNotationSequence(moves);
                    CubeState generatedState = CubeState.CreateSolved();
                    generatedState.ApplyMoves(moves);
                    string facelets = CubeStateSerializer.ToFaceletString(generatedState);

                    if (sequenceKeys.Contains(notation) || stateKeys.Contains(facelets))
                    {
                        continue;
                    }

                    if (CanSolveWithin(generatedState, 4))
                    {
                        continue;
                    }

                    sequenceKeys.Add(notation);
                    stateKeys.Add(facelets);
                    stage = BuildStage(
                        type,
                        firstStageNumber + index,
                        localNumber,
                        seed + index,
                        generationGroup,
                        moves,
                        facelets);
                    break;
                }

                if (stage == null)
                {
                    throw new InvalidOperationException($"Failed to generate unique stage {type} {localNumber}.");
                }

                stages.Add(stage);
                previousLength = targetLength;
            }

            return stages;
        }

        private static StageData BuildStage(
            StageType type,
            int stageNumber,
            int localNumber,
            int generatedSeed,
            string generationGroup,
            IReadOnlyList<CubeMove> moves,
            string generatedFacelets)
        {
            int minimumMoves = moves.Count;
            StageDifficulty difficulty = GetDifficulty(localNumber);
            int extraMoves = GetExtraMoves(localNumber, minimumMoves);
            int moveLimit = minimumMoves + extraMoves;
            string idPrefix = type == StageType.SolveStage ? "solve" : "target";
            string previousId = localNumber > 1 ? $"{idPrefix}_{localNumber - 1:000}" : string.Empty;
            string notation = MoveUtility.ToNotationSequence(moves);

            return new StageData
            {
                stageId = $"{idPrefix}_{localNumber:000}",
                stageNumber = stageNumber,
                stageType = type,
                difficulty = difficulty,
                title = type == StageType.SolveStage ? $"Solve {localNumber:000}" : $"Target {localNumber:000}",
                description = type == StageType.SolveStage
                    ? "Solve the scrambled cube within the move limit."
                    : "Create the target cube pattern within the move limit.",
                startStateFacelets = type == StageType.SolveStage
                    ? generatedFacelets
                    : CubeStateSerializer.ToFaceletString(CubeState.CreateSolved()),
                targetStateFacelets = type == StageType.SolveStage
                    ? CubeStateSerializer.ToFaceletString(CubeState.CreateSolved())
                    : generatedFacelets,
                scrambleNotation = type == StageType.SolveStage ? notation : string.Empty,
                solutionNotation = type == StageType.SolveStage
                    ? MoveUtility.ToNotationSequence(MoveUtility.InverseSequence(moves))
                    : notation,
                generatedSeed = generatedSeed,
                generationGroup = generationGroup,
                minimumMoves = minimumMoves,
                minMoveCount = minimumMoves,
                moveLimit = moveLimit,
                starMoveLimit3 = minimumMoves + Math.Max(1, extraMoves / 4),
                starMoveLimit2 = minimumMoves + Math.Max(2, extraMoves / 2),
                starMoveLimit1 = moveLimit,
                isUnlockedByDefault = localNumber == 1,
                unlockAfterStageId = previousId,
                rewardCoins = GetRewardCoins(difficulty, localNumber)
            };
        }

        private static List<CubeMove> GenerateMoves(int length, Random random)
        {
            var moves = new List<CubeMove>(length);
            CubeFace? previousFace = null;
            CubeAxis? previousAxis = null;

            while (moves.Count < length)
            {
                CubeFace face = Faces[random.Next(Faces.Length)];
                CubeAxis axis = GetAxis(face);
                if (previousFace.HasValue && face == previousFace.Value)
                {
                    continue;
                }

                if (previousAxis.HasValue && axis == previousAxis.Value && random.NextDouble() < 0.8)
                {
                    continue;
                }

                moves.Add(new CubeMove(face, Turns[random.Next(Turns.Length)]));
                previousFace = face;
                previousAxis = axis;
            }

            return moves;
        }

        private static int GetTargetLength(int stageNumber, StageType type, Random random)
        {
            int min;
            int max;
            if (stageNumber <= 10)
            {
                min = 5;
                max = type == StageType.SolveStage ? 7 : 6;
            }
            else if (stageNumber <= 30)
            {
                min = 7;
                max = type == StageType.SolveStage ? 10 : 9;
            }
            else if (stageNumber <= 60)
            {
                min = 10;
                max = type == StageType.SolveStage ? 14 : 13;
            }
            else
            {
                min = 14;
                max = type == StageType.SolveStage ? 20 : 19;
            }

            double progress = (stageNumber - 1) / 99.0;
            int preferred = min + (int)Math.Round((max - min) * progress);
            return Math.Max(min, Math.Min(max, preferred + random.Next(-1, 2)));
        }

        private static int GetExtraMoves(int stageNumber, int minimumMoves)
        {
            if (stageNumber <= 10)
            {
                return Math.Max(7, 14 - minimumMoves);
            }

            if (stageNumber <= 30)
            {
                return 6;
            }

            if (stageNumber <= 60)
            {
                return 5;
            }

            return 4;
        }

        private static StageDifficulty GetDifficulty(int stageNumber)
        {
            if (stageNumber <= 20) return StageDifficulty.Easy;
            if (stageNumber <= 50) return StageDifficulty.Normal;
            if (stageNumber <= 80) return StageDifficulty.Hard;
            return StageDifficulty.Expert;
        }

        private static int GetRewardCoins(StageDifficulty difficulty, int stageNumber)
        {
            switch (difficulty)
            {
                case StageDifficulty.Easy: return 30 + ((stageNumber - 1) % 4) * 10;
                case StageDifficulty.Normal: return 50 + ((stageNumber - 1) % 5) * 10;
                case StageDifficulty.Hard: return 80 + ((stageNumber - 1) % 6) * 10;
                default: return 120 + ((stageNumber - 1) % 7) * 10;
            }
        }

        private static bool CanSolveWithin(CubeState state, int maxDepth)
        {
            if (maxDepth != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDepth), "Stage generation currently checks a four-move minimum.");
            }

            return ReachesSolvedRadius(state, 2, null);
        }

        private static bool ReachesSolvedRadius(CubeState state, int remainingDepth, CubeFace? previousFace)
        {
            if (SolvedWithinTwoMoves.Contains(CubeStateSerializer.ToFaceletString(state)))
            {
                return true;
            }

            if (remainingDepth == 0)
            {
                return false;
            }

            foreach (CubeFace face in Faces)
            {
                if (previousFace.HasValue && previousFace.Value == face)
                {
                    continue;
                }

                foreach (int turns in Turns)
                {
                    CubeState next = state.Clone();
                    next.ApplyMove(new CubeMove(face, turns));
                    if (ReachesSolvedRadius(next, remainingDepth - 1, face))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static HashSet<string> BuildSolvedWithinTwoMoves()
        {
            var states = new HashSet<string>(StringComparer.Ordinal);
            AddStates(CubeState.CreateSolved(), 2, null, states);
            return states;
        }

        private static void AddStates(
            CubeState state,
            int remainingDepth,
            CubeFace? previousFace,
            HashSet<string> states)
        {
            states.Add(CubeStateSerializer.ToFaceletString(state));
            if (remainingDepth == 0)
            {
                return;
            }

            foreach (CubeFace face in Faces)
            {
                if (previousFace.HasValue && previousFace.Value == face)
                {
                    continue;
                }

                foreach (int turns in Turns)
                {
                    CubeState next = state.Clone();
                    next.ApplyMove(new CubeMove(face, turns));
                    AddStates(next, remainingDepth - 1, face, states);
                }
            }
        }

        private static CubeAxis GetAxis(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Right:
                case CubeFace.Left:
                    return CubeAxis.X;
                case CubeFace.Up:
                case CubeFace.Down:
                    return CubeAxis.Y;
                default:
                    return CubeAxis.Z;
            }
        }
    }
}
