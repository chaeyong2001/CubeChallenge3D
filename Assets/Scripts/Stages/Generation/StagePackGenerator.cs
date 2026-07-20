using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Generation
{
    public sealed class StagePackGenerator
    {
        public const int NormalStageCount = 300;
        public const int HardStageCount = 100;
        public const int InfinityStageCount = 500;
        public const int TutorialStageCount = 10;
        public const int HardFirstStageNumber = NormalStageCount + 1;
        public const int InfinityFirstStageNumber = NormalStageCount + HardStageCount + 1;
        public const int TutorialFirstStageNumber = NormalStageCount + HardStageCount + InfinityStageCount + 1;
        public const int DefaultTutorialSeed = 14901;
        public const int DefaultSolveSeed = 15001;
        public const int DefaultTargetSeed = 15101;
        public const int DefaultInfinitySeed = 15201;
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

        public List<StageData> GenerateTutorialStages(int count, int seed)
        {
            return Generate(count, seed, StageType.TutorialStage, TutorialFirstStageNumber, "tutorial_pack_v1");
        }

        public List<StageData> GenerateReverseTargetStages(int count, int seed)
        {
            return Generate(count, seed, StageType.ReverseTargetStage, HardFirstStageNumber, "target_pack_v1");
        }

        public List<StageData> GenerateInfinityStages(int count, int seed)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var random = new Random(seed);
            var stages = new List<StageData>(count);
            var sequenceKeys = new HashSet<string>(StringComparer.Ordinal);
            var stateKeys = new HashSet<string>(StringComparer.Ordinal);
            List<bool> targetPatternSlots = BuildInfinityPatternSlots(count, random);
            int previousLength = 5;

            for (int index = 0; index < count; index++)
            {
                int localNumber = index + 1;
                bool targetPattern = targetPatternSlots[index];
                int scaledLocalNumber = 21 + ((localNumber - 1) % 280);
                int targetLength = Math.Max(previousLength, GetTargetLength(scaledLocalNumber, targetPattern ? StageType.ReverseTargetStage : StageType.SolveStage, random));
                StageData stage = null;

                for (int retry = 0; retry < MaxCandidateRetries; retry++)
                {
                    List<CubeMove> moves = GenerateMoves(targetLength, random);
                    string notation = MoveUtility.ToNotationSequence(moves);
                    CubeState generatedState = CubeState.CreateSolved();
                    generatedState.ApplyMoves(moves);
                    string facelets = CubeStateSerializer.ToFaceletString(generatedState);
                    string uniqueKey = $"{(targetPattern ? "target" : "solve")}:{notation}";

                    if (sequenceKeys.Contains(uniqueKey) || stateKeys.Contains(facelets))
                    {
                        continue;
                    }

                    if (CanSolveWithin(generatedState, 4))
                    {
                        continue;
                    }

                    sequenceKeys.Add(uniqueKey);
                    stateKeys.Add(facelets);
                    stage = BuildInfinityStage(
                        localNumber,
                        InfinityFirstStageNumber + index,
                        seed + index,
                        targetPattern,
                        moves,
                        facelets);
                    break;
                }

                if (stage == null)
                {
                    throw new InvalidOperationException($"Failed to generate unique infinity stage {localNumber}.");
                }

                stages.Add(stage);
                previousLength = targetLength;
            }

            return stages;
        }

        private static List<bool> BuildInfinityPatternSlots(int count, Random random)
        {
            int targetCount = count / 4;
            var slots = new List<bool>(count);
            for (int i = 0; i < count; i++)
            {
                slots.Add(i < targetCount);
            }

            for (int i = slots.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                bool value = slots[i];
                slots[i] = slots[swapIndex];
                slots[swapIndex] = value;
            }

            return slots;
        }

        public StageDataCollection GenerateFullStagePack()
        {
            var collection = new StageDataCollection();
            collection.stages.AddRange(GenerateTutorialStages(TutorialStageCount, DefaultTutorialSeed));
            collection.stages.AddRange(GenerateSolveStages(NormalStageCount, DefaultSolveSeed));
            collection.stages.AddRange(GenerateReverseTargetStages(HardStageCount, DefaultTargetSeed));
            collection.stages.AddRange(GenerateInfinityStages(InfinityStageCount, DefaultInfinitySeed));
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
            int minimumMoves = MoveUtility.CountPlayerTurns(moves);
            StageDifficulty difficulty = GetDifficulty(localNumber);
            string idPrefix = GetStageIdPrefix(type);
            string previousId = localNumber > 1 ? $"{idPrefix}_{localNumber - 1:000}" : string.Empty;
            string notation = MoveUtility.ToNotationSequence(moves);
            bool solvePattern = type == StageType.SolveStage || type == StageType.TutorialStage;
            bool tutorial = type == StageType.TutorialStage;
            GetStarMoveOffsets(type, localNumber, out int threeStarOffset, out int twoStarOffset, out int oneStarOffset);
            int starMoveLimit3 = minimumMoves + threeStarOffset;
            int starMoveLimit2 = minimumMoves + twoStarOffset;
            int moveLimit = minimumMoves + oneStarOffset;

            return new StageData
            {
                stageId = $"{idPrefix}_{localNumber:000}",
                stageNumber = stageNumber,
                stageType = type,
                difficulty = difficulty,
                title = tutorial ? $"Tutorial {localNumber:000}" : type == StageType.SolveStage ? $"Solve {localNumber:000}" : $"Target {localNumber:000}",
                description = solvePattern
                    ? "Solve the scrambled cube within the move limit."
                    : "Create the target cube pattern within the move limit.",
                startStateFacelets = solvePattern
                    ? generatedFacelets
                    : CubeStateSerializer.ToFaceletString(CubeState.CreateSolved()),
                targetStateFacelets = solvePattern
                    ? CubeStateSerializer.ToFaceletString(CubeState.CreateSolved())
                    : generatedFacelets,
                scrambleNotation = solvePattern ? notation : string.Empty,
                solutionNotation = solvePattern
                    ? MoveUtility.ToNotationSequence(MoveUtility.InverseSequence(moves))
                    : notation,
                generatedSeed = generatedSeed,
                generationGroup = generationGroup,
                minimumMoves = minimumMoves,
                minMoveCount = minimumMoves,
                moveLimit = moveLimit,
                starMoveLimit3 = starMoveLimit3,
                starMoveLimit2 = starMoveLimit2,
                starMoveLimit1 = moveLimit,
                isUnlockedByDefault = localNumber == 1,
                unlockAfterStageId = previousId,
                rewardCoins = GetRewardCoins(difficulty, localNumber)
            };
        }

        private static StageData BuildInfinityStage(
            int localNumber,
            int stageNumber,
            int generatedSeed,
            bool targetPattern,
            IReadOnlyList<CubeMove> moves,
            string generatedFacelets)
        {
            int minimumMoves = MoveUtility.CountPlayerTurns(moves);
            int scaledDifficultyStage = 21 + ((localNumber - 1) % 280);
            StageDifficulty difficulty = GetDifficulty(scaledDifficultyStage);
            string notation = MoveUtility.ToNotationSequence(moves);
            GetStarMoveOffsets(StageType.InfinityStage, localNumber, out int threeStarOffset, out int twoStarOffset, out int oneStarOffset);
            int starMoveLimit3 = minimumMoves + threeStarOffset;
            int starMoveLimit2 = minimumMoves + twoStarOffset;
            int moveLimit = minimumMoves + oneStarOffset;

            return new StageData
            {
                stageId = $"infinity_{localNumber:000}",
                stageNumber = stageNumber,
                stageType = StageType.InfinityStage,
                difficulty = difficulty,
                title = targetPattern ? $"Target {localNumber:000}" : $"Solve {localNumber:000}",
                description = targetPattern
                    ? "Create the target cube pattern within the move limit."
                    : "Solve the scrambled cube within the move limit.",
                startStateFacelets = targetPattern
                    ? CubeStateSerializer.ToFaceletString(CubeState.CreateSolved())
                    : generatedFacelets,
                targetStateFacelets = targetPattern
                    ? generatedFacelets
                    : CubeStateSerializer.ToFaceletString(CubeState.CreateSolved()),
                scrambleNotation = targetPattern ? string.Empty : notation,
                solutionNotation = targetPattern
                    ? notation
                    : MoveUtility.ToNotationSequence(MoveUtility.InverseSequence(moves)),
                generatedSeed = generatedSeed,
                generationGroup = targetPattern ? "infinity_target_pack_v1" : "infinity_solve_pack_v1",
                minimumMoves = minimumMoves,
                minMoveCount = minimumMoves,
                moveLimit = moveLimit,
                starMoveLimit3 = starMoveLimit3,
                starMoveLimit2 = starMoveLimit2,
                starMoveLimit1 = moveLimit,
                isUnlockedByDefault = localNumber == 1,
                unlockAfterStageId = localNumber > 1 ? $"infinity_{localNumber - 1:000}" : string.Empty,
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
            int localStage = Math.Max(1, stageNumber);
            if (type == StageType.TutorialStage)
            {
                min = 5;
                max = 6;
            }
            else if (type == StageType.SolveStage && localStage > 100)
            {
                min = 20 + ((localStage - 101) / 50) * 2;
                max = 24 + ((localStage - 101) / 50) * 2;
            }
            else if (localStage <= 10)
            {
                min = 5;
                max = 7;
            }
            else if (localStage <= 30)
            {
                min = 7;
                max = 10;
            }
            else if (localStage <= 60)
            {
                min = 10;
                max = 14;
            }
            else
            {
                min = 14;
                max = 20;
            }

            double progress = Math.Min(1.0, (localStage - 1) / 99.0);
            int preferred = min + (int)Math.Round((max - min) * progress);
            return Math.Max(min, Math.Min(max, preferred + random.Next(-1, 2)));
        }

        private static string GetStageIdPrefix(StageType type)
        {
            switch (type)
            {
                case StageType.TutorialStage:
                    return "tutorial";
                case StageType.SolveStage:
                    return "solve";
                default:
                    return "target";
            }
        }

        private static void GetStarMoveOffsets(
            StageType type,
            int localNumber,
            out int threeStarOffset,
            out int twoStarOffset,
            out int oneStarOffset)
        {
            int stage = Math.Max(1, localNumber);
            switch (type)
            {
                case StageType.TutorialStage:
                    threeStarOffset = 5;
                    twoStarOffset = 7;
                    oneStarOffset = 9;
                    return;
                case StageType.SolveStage:
                    if (stage <= 10)
                    {
                        threeStarOffset = 4;
                        twoStarOffset = 6;
                        oneStarOffset = 8;
                        return;
                    }

                    if (stage <= 50)
                    {
                        threeStarOffset = 3;
                        twoStarOffset = 5;
                        oneStarOffset = 7;
                        return;
                    }

                    if (stage <= 150)
                    {
                        threeStarOffset = 2;
                        twoStarOffset = 4;
                        oneStarOffset = 6;
                        return;
                    }

                    threeStarOffset = 1;
                    twoStarOffset = 3;
                    oneStarOffset = 5;
                    return;
                case StageType.ReverseTargetStage:
                    if (stage <= 20)
                    {
                        threeStarOffset = 3;
                        twoStarOffset = 5;
                        oneStarOffset = 7;
                        return;
                    }

                    if (stage <= 60)
                    {
                        threeStarOffset = 2;
                        twoStarOffset = 4;
                        oneStarOffset = 6;
                        return;
                    }

                    threeStarOffset = 1;
                    twoStarOffset = 3;
                    oneStarOffset = 5;
                    return;
                case StageType.InfinityStage:
                    if (stage <= 50)
                    {
                        threeStarOffset = 3;
                        twoStarOffset = 5;
                        oneStarOffset = 7;
                        return;
                    }

                    if (stage <= 150)
                    {
                        threeStarOffset = 2;
                        twoStarOffset = 4;
                        oneStarOffset = 6;
                        return;
                    }

                    threeStarOffset = 1;
                    twoStarOffset = 3;
                    oneStarOffset = 5;
                    return;
                default:
                    threeStarOffset = 2;
                    twoStarOffset = 4;
                    oneStarOffset = 6;
                    return;
            }
        }

        private static StageDifficulty GetDifficulty(int stageNumber)
        {
            if (stageNumber <= 20) return StageDifficulty.Easy;
            if (stageNumber <= 125) return StageDifficulty.Normal;
            if (stageNumber <= 230) return StageDifficulty.Hard;
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
