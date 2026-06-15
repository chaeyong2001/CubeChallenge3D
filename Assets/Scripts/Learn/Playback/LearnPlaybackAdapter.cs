using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Learn.Model;
using CubeChallenge3D.Solver.Model;
using CubeChallenge3D.Solver.Services;

namespace CubeChallenge3D.Learn.Playback
{
    public static class LearnPlaybackAdapter
    {
        public static bool TryCreateSolution(
            LearnLessonData lesson,
            out SolverSolution solution,
            out string error)
        {
            return TryCreateSolutionInternal(lesson, null, out solution, out error);
        }

        public static bool TryCreateSolution(
            LearnLessonData lesson,
            LearnStepDemoData substep,
            out SolverSolution solution,
            out string error)
        {
            return TryCreateSolutionInternal(lesson, substep, out solution, out error);
        }

        private static bool TryCreateSolutionInternal(
            LearnLessonData lesson,
            LearnStepDemoData substep,
            out SolverSolution solution,
            out string error)
        {
            solution = null;
            error = string.Empty;
            if (lesson == null)
            {
                error = "Lesson data is unavailable.";
                return false;
            }

            string[] moves = substep?.demoMoves != null && substep.demoMoves.Length > 0
                ? substep.demoMoves
                : lesson.demoMoves != null && lesson.demoMoves.Length > 0
                    ? lesson.demoMoves
                : lesson.moveNotations ?? Array.Empty<string>();
            if (!lesson.has3DDemo || moves.Length == 0)
            {
                error = "This lesson does not have a 3D demo.";
                return false;
            }

            for (int i = 0; i < moves.Length; i++)
            {
                if (!CubeMove.TryParse(moves[i], out _))
                {
                    error = $"Unsupported move notation: {moves[i]}";
                    return false;
                }
            }

            CubeState sourceState;
            string startFacelets = !string.IsNullOrWhiteSpace(substep?.startFaceletString)
                ? substep.startFaceletString
                : lesson.demoStartFaceletString;
            if (!string.IsNullOrWhiteSpace(startFacelets))
            {
                try
                {
                    sourceState = CubeStateSerializer.FromFaceletString(startFacelets);
                }
                catch (Exception exception)
                {
                    error = $"Invalid demo start state: {exception.Message}";
                    return false;
                }
            }
            else
            {
                sourceState = CubeState.CreateSolved();
            }

            if (substep != null && substep.wholeCubeYawQuarterTurns != 0)
            {
                sourceState = LearnCubeOrientationUtility.RotateAroundUpAxis(
                    sourceState,
                    substep.wholeCubeYawQuarterTurns);
            }

            if (!string.IsNullOrWhiteSpace(substep?.goalSetupNotation))
            {
                try
                {
                    foreach (CubeMove move in MoveUtility.ParseSequence(substep.goalSetupNotation))
                    {
                        sourceState.ApplyMove(SolverPlaybackMoveMapper.ToPlaybackMove(move));
                    }
                }
                catch (Exception exception)
                {
                    error = $"Invalid demo goal setup: {exception.Message}";
                    return false;
                }
            }

            if (substep != null
                && TryParseTargetColor(substep.targetSideColorName, out CubeColor targetSideColor)
                && sourceState.GetColor(CubeFace.Front, 1, 1) != targetSideColor)
            {
                error = $"White Cross demo front center is not {targetSideColor}.";
                return false;
            }

            CubeState demoGoalState = sourceState.Clone();
            if (substep != null && substep.generateSetupFromDemoMoves)
            {
                try
                {
                    var parsedMoves = new CubeMove[moves.Length];
                    for (int i = 0; i < moves.Length; i++)
                    {
                        CubeMove.TryParse(moves[i], out parsedMoves[i]);
                    }

                    IReadOnlyList<CubeMove> inverseMoves = MoveUtility.InverseSequence(parsedMoves);
                    foreach (CubeMove move in inverseMoves)
                    {
                        sourceState.ApplyMove(SolverPlaybackMoveMapper.ToPlaybackMove(move));
                    }
                }
                catch (Exception exception)
                {
                    error = $"Invalid demo setup: {exception.Message}";
                    return false;
                }
            }
            else
            {
                string setupNotation = !string.IsNullOrWhiteSpace(substep?.setupNotation)
                    ? substep.setupNotation
                    : lesson.demoSetupNotation;
                if (!string.IsNullOrWhiteSpace(setupNotation))
                {
                    try
                    {
                        foreach (CubeMove move in MoveUtility.ParseSequence(setupNotation))
                        {
                            sourceState.ApplyMove(SolverPlaybackMoveMapper.ToPlaybackMove(move));
                        }
                    }
                    catch (Exception exception)
                    {
                        error = $"Invalid demo setup: {exception.Message}";
                        return false;
                    }
                }
            }

            if (!CubeStateValidator.IsColorCountValid(sourceState))
            {
                error = "Demo start state must contain exactly nine stickers of each color.";
                return false;
            }

            if (lesson.demoType != null
                && lesson.demoType.StartsWith("Beginner", StringComparison.Ordinal))
            {
                CubeState resultState = sourceState.Clone();
                try
                {
                    for (int i = 0; i < moves.Length; i++)
                    {
                        CubeMove.TryParse(moves[i], out CubeMove move);
                        resultState.ApplyMove(SolverPlaybackMoveMapper.ToPlaybackMove(move));
                    }
                }
                catch (Exception exception)
                {
                    error = $"Cannot validate beginner demo: {exception.Message}";
                    return false;
                }

                if (!resultState.Equals(demoGoalState))
                {
                    error = "Beginner demo setup does not match its target move sequence.";
                    return false;
                }

                if (string.Equals(lesson.demoType, "BeginnerMultiStep", StringComparison.Ordinal)
                    && !IsBottomWhiteCrossComplete(resultState))
                {
                    error = "White Cross demo did not finish with four white bottom edges.";
                    return false;
                }

                if (string.Equals(lesson.demoType, "BeginnerCornerStep", StringComparison.Ordinal))
                {
                    if (!IsBottomWhiteCrossComplete(sourceState))
                    {
                        error = "White Corners demo must start with the white cross complete.";
                        return false;
                    }

                    if (!IsBottomWhiteCrossComplete(resultState))
                    {
                        error = "White Corners demo broke the completed white cross.";
                        return false;
                    }

                    if (substep == null || !IsTargetCornerSolved(
                            resultState,
                            substep.targetGoalX,
                            substep.targetGoalY,
                            substep.targetGoalZ))
                    {
                        error = "White Corners demo did not solve the target corner.";
                        return false;
                    }

                    if (!ContainsTargetCorner(
                            sourceState,
                            substep.targetStartX,
                            substep.targetStartY,
                            substep.targetStartZ,
                            resultState,
                            substep.targetGoalX,
                            substep.targetGoalY,
                            substep.targetGoalZ))
                    {
                        error = "White Corners demo target highlight does not match the target piece.";
                        return false;
                    }

                    if (!IsWhiteStickerOnExpectedFace(
                            sourceState,
                            substep.targetStartX,
                            substep.targetStartY,
                            substep.targetStartZ,
                            substep.expectedWhiteFaceName))
                    {
                        error = $"White Corners case does not start with white facing {substep.expectedWhiteFaceName}.";
                        return false;
                    }
                }

                if (string.Equals(lesson.demoType, "BeginnerSecondLayer", StringComparison.Ordinal)
                    && !IsSecondLayerComplete(resultState))
                {
                    error = "Second Layer demo did not finish with the first two layers solved.";
                    return false;
                }

                if (string.Equals(lesson.demoType, "BeginnerYellowCross", StringComparison.Ordinal)
                    && (!IsSecondLayerComplete(resultState) || !IsTopCrossComplete(resultState)))
                {
                    error = "Yellow Cross demo did not finish with a yellow top cross.";
                    return false;
                }

                if (string.Equals(lesson.demoType, "BeginnerYellowFace", StringComparison.Ordinal)
                    && (!IsSecondLayerComplete(resultState) || !IsTopFaceComplete(resultState)))
                {
                    error = "Yellow Face demo did not finish with all nine top stickers yellow.";
                    return false;
                }

                if (string.Equals(lesson.demoType, "BeginnerLastCorners", StringComparison.Ordinal)
                    && (!IsTopFaceComplete(resultState) || !AreTopCornersPositioned(resultState)))
                {
                    error = "Last Layer Corners demo did not finish with all top corners positioned.";
                    return false;
                }

                if (string.Equals(lesson.demoType, "BeginnerLastEdges", StringComparison.Ordinal)
                    && !resultState.IsSolved())
                {
                    error = "Last Layer Edges demo did not finish with a solved cube.";
                    return false;
                }
            }

            if (string.Equals(lesson.demoType, "FormulaRightTrigger", StringComparison.Ordinal))
            {
                CubeState resultState = ApplyPlaybackMoves(sourceState, moves, out string validationError);
                if (resultState == null)
                {
                    error = validationError;
                    return false;
                }

                if (!resultState.Equals(demoGoalState)
                    || !IsBottomWhiteCrossComplete(sourceState)
                    || !IsBottomWhiteCrossComplete(resultState))
                {
                    error = "Right Trigger demo did not preserve the white cross and reach its target state.";
                    return false;
                }
            }

            if (string.Equals(lesson.demoType, "FormulaLeftTrigger", StringComparison.Ordinal)
                || string.Equals(lesson.demoType, "FormulaSledgehammer", StringComparison.Ordinal)
                || string.Equals(lesson.demoType, "FormulaYellowCross", StringComparison.Ordinal)
                || string.Equals(lesson.demoType, "FormulaRightAlgorithm", StringComparison.Ordinal))
            {
                CubeState resultState = ApplyPlaybackMoves(sourceState, moves, out string validationError);
                if (resultState == null)
                {
                    error = validationError;
                    return false;
                }

                bool targetValid = resultState.Equals(demoGoalState);
                if (string.Equals(lesson.demoType, "FormulaLeftTrigger", StringComparison.Ordinal))
                {
                    targetValid = targetValid
                        && IsBottomWhiteCrossComplete(sourceState)
                        && IsBottomWhiteCrossComplete(resultState);
                }
                else if (string.Equals(lesson.demoType, "FormulaSledgehammer", StringComparison.Ordinal))
                {
                    targetValid = targetValid && IsSecondLayerComplete(resultState);
                }
                else if (string.Equals(lesson.demoType, "FormulaYellowCross", StringComparison.Ordinal))
                {
                    targetValid = targetValid
                        && IsSecondLayerComplete(resultState)
                        && IsTopCrossComplete(resultState);
                }
                else
                {
                    targetValid = targetValid
                        && IsSecondLayerComplete(resultState)
                        && IsTopFaceComplete(resultState);
                }

                if (!targetValid)
                {
                    error = $"{lesson.title} demo did not reach its intended teaching result.";
                    return false;
                }
            }

            string sourceFacelets = CubeStateSerializer.ToFaceletString(sourceState);
            solution = new SolverSolution
            {
                sourceFaceletString = sourceFacelets,
                sourceColorFaceletString = sourceFacelets,
                solutionNotation = string.Join(" ", moves),
                moveNotations = moves,
                moveDescriptions = substep?.moveInstructions ?? Array.Empty<string>(),
                completionMessage = GetCompletionMessage(lesson.demoType, substep),
                completionGuideMessage = GetCompletionGuideMessage(lesson.demoType),
                moveCount = moves.Length,
                orientationMode = "LearnDemo",
                displayFrontFace = "Front",
                displayTopFace = "Top",
                displayRightFace = "Right",
                createdAtUtc = DateTime.UtcNow.ToString("o")
            };
            return true;
        }

        private static bool IsBottomWhiteCrossComplete(CubeState state)
        {
            return state != null
                && state.GetColor(CubeFace.Down, 0, 1) == CubeColor.White
                && state.GetColor(CubeFace.Down, 1, 0) == CubeColor.White
                && state.GetColor(CubeFace.Down, 1, 2) == CubeColor.White
                && state.GetColor(CubeFace.Down, 2, 1) == CubeColor.White;
        }

        private static string GetCompletionGuideMessage(string demoType)
        {
            switch (demoType)
            {
                case "BeginnerCornerStep":
                    return "Look underneath: the white face is complete, its corners match the side centers, and the white cross remains intact.";
                case "BeginnerSecondLayer":
                    return "Check the four sides: the bottom two rows now match their center colors.";
                case "BeginnerYellowCross":
                    return "Look straight at the top: the yellow center and four yellow edges form a cross.";
                case "BeginnerYellowFace":
                    return "Look straight at the top: all nine top stickers are yellow.";
                case "BeginnerLastCorners":
                    return "Check every top corner: its three colors now belong between the three matching centers.";
                case "BeginnerLastEdges":
                    return "The cube is solved: every face is now one uniform color.";
                case "FormulaRightTrigger":
                    return "Check the inserted corner: white is on the bottom and both side colors match their centers.";
                case "FormulaLeftTrigger":
                    return "Check the inserted corner: white is on the bottom and both side colors match their centers.";
                case "FormulaSledgehammer":
                    return "Check the target slot: the corner-edge pair is inserted and the first two layers are restored.";
                case "FormulaYellowCross":
                    return "Look straight at the top: the four yellow edges now form a cross around the center.";
                case "FormulaRightAlgorithm":
                    return "Look straight at the top: the remaining corners are oriented and all nine stickers are yellow.";
                default:
                    return "Look underneath: four white edges form a cross and every side color matches its center.";
            }
        }

        private static string GetCompletionMessage(string demoType, LearnStepDemoData substep)
        {
            switch (demoType)
            {
                case "BeginnerMultiStep":
                    return "White edge placed in the bottom cross.";
                case "BeginnerCornerStep":
                    return "White corner placed in the completed first layer.";
                case "BeginnerSecondLayer":
                    return "Middle edge inserted without breaking the white layer.";
                case "BeginnerYellowCross":
                    return "Yellow cross completed on the top face.";
                case "BeginnerYellowFace":
                    return "Yellow face completed.";
                case "BeginnerLastCorners":
                    return "All last-layer corners are in their correct positions.";
                case "BeginnerLastEdges":
                    return "Cube solved.";
                case "FormulaRightTrigger":
                    return "Right Trigger complete - target corner inserted.";
                case "FormulaLeftTrigger":
                    return "Left Trigger complete - target corner inserted.";
                case "FormulaSledgehammer":
                    return "Sledgehammer complete - prepared pair inserted.";
                case "FormulaYellowCross":
                    return "Yellow Cross Formula complete - yellow cross formed.";
                case "FormulaRightAlgorithm":
                    return "Right Algorithm complete - yellow face formed.";
                default:
                    return substep == null
                        ? "Demo finished."
                        : $"Placed the {substep.highlightedCubieHint} in the {substep.targetSlotHint}.";
            }
        }

        private static CubeState ApplyPlaybackMoves(
            CubeState sourceState,
            IReadOnlyList<string> moves,
            out string error)
        {
            error = string.Empty;
            CubeState result = sourceState?.Clone();
            if (result == null)
            {
                error = "Demo state is unavailable.";
                return null;
            }

            try
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    if (!CubeMove.TryParse(moves[i], out CubeMove move))
                    {
                        error = $"Unsupported move notation: {moves[i]}";
                        return null;
                    }

                    result.ApplyMove(SolverPlaybackMoveMapper.ToPlaybackMove(move));
                }
            }
            catch (Exception exception)
            {
                error = $"Cannot validate formula demo: {exception.Message}";
                return null;
            }

            return result;
        }

        private static bool IsFirstLayerComplete(CubeState state)
        {
            if (state == null)
            {
                return false;
            }

            CubeColor downColor = state.GetColor(CubeFace.Down, 1, 1);
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (state.GetColor(CubeFace.Down, row, col) != downColor)
                    {
                        return false;
                    }
                }
            }

            CubeFace[] sides =
            {
                CubeFace.Front,
                CubeFace.Right,
                CubeFace.Back,
                CubeFace.Left
            };
            foreach (CubeFace face in sides)
            {
                CubeColor center = state.GetColor(face, 1, 1);
                for (int col = 0; col < 3; col++)
                {
                    if (state.GetColor(face, 2, col) != center)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsSecondLayerComplete(CubeState state)
        {
            if (!IsFirstLayerComplete(state))
            {
                return false;
            }

            CubeFace[] sides =
            {
                CubeFace.Front,
                CubeFace.Right,
                CubeFace.Back,
                CubeFace.Left
            };
            foreach (CubeFace face in sides)
            {
                CubeColor center = state.GetColor(face, 1, 1);
                for (int col = 0; col < 3; col++)
                {
                    if (state.GetColor(face, 1, col) != center)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsTopCrossComplete(CubeState state)
        {
            if (state == null)
            {
                return false;
            }

            CubeColor center = state.GetColor(CubeFace.Up, 1, 1);
            return state.GetColor(CubeFace.Up, 0, 1) == center
                && state.GetColor(CubeFace.Up, 1, 0) == center
                && state.GetColor(CubeFace.Up, 1, 2) == center
                && state.GetColor(CubeFace.Up, 2, 1) == center;
        }

        private static bool IsTopFaceComplete(CubeState state)
        {
            if (state == null)
            {
                return false;
            }

            CubeColor center = state.GetColor(CubeFace.Up, 1, 1);
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (state.GetColor(CubeFace.Up, row, col) != center)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool AreTopCornersPositioned(CubeState state)
        {
            return IsTargetCornerSolved(state, -1, 1, 1)
                && IsTargetCornerSolved(state, 1, 1, 1)
                && IsTargetCornerSolved(state, -1, 1, -1)
                && IsTargetCornerSolved(state, 1, 1, -1);
        }

        private static bool TryParseTargetColor(string value, out CubeColor color)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "green": color = CubeColor.Green; return true;
                case "red": color = CubeColor.Red; return true;
                case "blue": color = CubeColor.Blue; return true;
                case "orange": color = CubeColor.Orange; return true;
                default: color = CubeColor.None; return false;
            }
        }

        private static bool IsTargetCornerSolved(CubeState state, int x, int y, int z)
        {
            if (!TryGetCornerFacelets(x, y, z, out CornerFacelet[] facelets))
            {
                return false;
            }

            foreach (CornerFacelet facelet in facelets)
            {
                if (state.GetColor(facelet.Face, facelet.Row, facelet.Col)
                    != state.GetColor(facelet.Face, 1, 1))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsTargetCorner(
            CubeState source,
            int sourceX,
            int sourceY,
            int sourceZ,
            CubeState goal,
            int goalX,
            int goalY,
            int goalZ)
        {
            if (!TryGetCornerColors(source, sourceX, sourceY, sourceZ, out CubeColor[] sourceColors)
                || !TryGetCornerColors(goal, goalX, goalY, goalZ, out CubeColor[] goalColors))
            {
                return false;
            }

            Array.Sort(sourceColors);
            Array.Sort(goalColors);
            for (int i = 0; i < sourceColors.Length; i++)
            {
                if (sourceColors[i] != goalColors[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWhiteStickerOnExpectedFace(
            CubeState state,
            int x,
            int y,
            int z,
            string expectedFaceName)
        {
            if (!Enum.TryParse(expectedFaceName, true, out CubeFace expectedFace)
                || !TryGetCornerFacelets(x, y, z, out CornerFacelet[] facelets))
            {
                return false;
            }

            foreach (CornerFacelet facelet in facelets)
            {
                if (facelet.Face == expectedFace)
                {
                    return state.GetColor(facelet.Face, facelet.Row, facelet.Col) == CubeColor.White;
                }
            }

            return false;
        }

        private static bool TryGetCornerColors(
            CubeState state,
            int x,
            int y,
            int z,
            out CubeColor[] colors)
        {
            colors = null;
            if (!TryGetCornerFacelets(x, y, z, out CornerFacelet[] facelets))
            {
                return false;
            }

            colors = new CubeColor[3];
            for (int i = 0; i < facelets.Length; i++)
            {
                CornerFacelet facelet = facelets[i];
                colors[i] = state.GetColor(facelet.Face, facelet.Row, facelet.Col);
            }

            return true;
        }

        private static bool TryGetCornerFacelets(
            int x,
            int y,
            int z,
            out CornerFacelet[] facelets)
        {
            facelets = null;
            if (Math.Abs(x) != 1 || Math.Abs(y) != 1 || Math.Abs(z) != 1)
            {
                return false;
            }

            CubeFace yFace = y > 0 ? CubeFace.Up : CubeFace.Down;
            CubeFace zFace = z > 0 ? CubeFace.Front : CubeFace.Back;
            CubeFace xFace = x > 0 ? CubeFace.Right : CubeFace.Left;
            facelets = new[]
            {
                ToCornerFacelet(yFace, x, y, z),
                ToCornerFacelet(zFace, x, y, z),
                ToCornerFacelet(xFace, x, y, z)
            };
            return true;
        }

        private static CornerFacelet ToCornerFacelet(CubeFace face, int x, int y, int z)
        {
            switch (face)
            {
                case CubeFace.Up: return new CornerFacelet(face, z + 1, x + 1);
                case CubeFace.Down: return new CornerFacelet(face, 1 - z, x + 1);
                case CubeFace.Front: return new CornerFacelet(face, 1 - y, x + 1);
                case CubeFace.Back: return new CornerFacelet(face, 1 - y, 1 - x);
                case CubeFace.Right: return new CornerFacelet(face, 1 - y, 1 - z);
                case CubeFace.Left: return new CornerFacelet(face, 1 - y, z + 1);
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private readonly struct CornerFacelet
        {
            public CubeFace Face { get; }
            public int Row { get; }
            public int Col { get; }

            public CornerFacelet(CubeFace face, int row, int col)
            {
                Face = face;
                Row = row;
                Col = col;
            }
        }
    }
}
