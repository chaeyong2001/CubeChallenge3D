using System;
using System.Text;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Solver.Model;

namespace CubeChallenge3D.Solver.Services
{
    public static class SolverInputSerializer
    {
        private static readonly CubeFace[] FaceletOrder =
        {
            CubeFace.Up,
            CubeFace.Right,
            CubeFace.Front,
            CubeFace.Down,
            CubeFace.Left,
            CubeFace.Back
        };

        public static bool TryToFaceletString(SolverInputState state, out string faceletString, out string error)
        {
            faceletString = string.Empty;
            error = string.Empty;
            SolverInputValidationResult validation = SolverInputValidator.Validate(state);
            if (!validation.isValid)
            {
                error = string.Join("\n", validation.messages);
                return false;
            }

            var centerToFace = new char[Enum.GetValues(typeof(CubeColor)).Length];
            centerToFace[state.faceletColorIndexes[4]] = 'U';
            centerToFace[state.faceletColorIndexes[13]] = 'R';
            centerToFace[state.faceletColorIndexes[22]] = 'F';
            centerToFace[state.faceletColorIndexes[31]] = 'D';
            centerToFace[state.faceletColorIndexes[40]] = 'L';
            centerToFace[state.faceletColorIndexes[49]] = 'B';

            var builder = new StringBuilder(SolverInputState.FaceletCount);
            for (int faceIndex = 0; faceIndex < SolverInputState.FaceCount; faceIndex++)
            {
                for (int cellIndex = 0; cellIndex < SolverInputState.FaceletPerFace; cellIndex++)
                {
                    int inputIndex = GetInputIndexForCanonicalFacelet(faceIndex, cellIndex);
                    int colorIndex = state.faceletColorIndexes[inputIndex];
                    if (colorIndex < 0 || colorIndex >= centerToFace.Length || centerToFace[colorIndex] == '\0')
                    {
                        error = $"Facelet {inputIndex} cannot be mapped to URFDLB.";
                        return false;
                    }

                    builder.Append(centerToFace[colorIndex]);
                }
            }

            faceletString = builder.ToString();
            return true;
        }

        public static bool TryToCubeState(SolverInputState state, out CubeState cubeState, out string error)
        {
            cubeState = null;
            error = string.Empty;
            SolverInputValidationResult validation = SolverInputValidator.Validate(state);
            if (!validation.isValid)
            {
                error = string.Join("\n", validation.messages);
                return false;
            }

            cubeState = new CubeState();
            for (int faceIndex = 0; faceIndex < SolverInputState.FaceCount; faceIndex++)
            {
                CubeFace face = FaceletOrder[faceIndex];
                for (int cellIndex = 0; cellIndex < SolverInputState.FaceletPerFace; cellIndex++)
                {
                    int inputIndex = GetInputIndexForCanonicalFacelet(faceIndex, cellIndex);
                    CubeColor color = (CubeColor)state.faceletColorIndexes[inputIndex];
                    int row = cellIndex / 3;
                    int col = cellIndex % 3;
                    cubeState.SetColor(face, row, col, color);
                }
            }

            return true;
        }

        public static bool TryToPlaybackCubeState(SolverInputState state, out CubeState cubeState, out string error)
        {
            cubeState = null;
            error = string.Empty;
            SolverInputValidationResult validation = SolverInputValidator.Validate(state);
            if (!validation.isValid)
            {
                error = string.Join("\n", validation.messages);
                return false;
            }

            cubeState = new CubeState();
            for (int visualFaceIndex = 0; visualFaceIndex < SolverInputState.FaceCount; visualFaceIndex++)
            {
                CubeFace face = FaceletOrder[visualFaceIndex];
                int logicalFaceIndex = VisualToLogicalFaceIndex(visualFaceIndex);
                for (int visualCellIndex = 0; visualCellIndex < SolverInputState.FaceletPerFace; visualCellIndex++)
                {
                    int playbackCellIndex = MapVisualCellToPlaybackCell(visualFaceIndex, visualCellIndex);
                    int logicalCellIndex = MapVisualCellToLogicalCell(visualFaceIndex, logicalFaceIndex, visualCellIndex);
                    int logicalIndex = (logicalFaceIndex * SolverInputState.FaceletPerFace) + logicalCellIndex;
                    CubeColor color = (CubeColor)state.faceletColorIndexes[logicalIndex];
                    int row = playbackCellIndex / 3;
                    int col = playbackCellIndex % 3;
                    cubeState.SetColor(face, row, col, color);
                }
            }

            return true;
        }

        public static bool TryFromFaceletString(string value, out SolverInputState state, out string error)
        {
            state = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(value) || value.Length != SolverInputState.FaceletCount)
            {
                error = "Facelet string must contain 54 characters.";
                return false;
            }

            state = SolverInputState.CreateSolved();
            for (int faceIndex = 0; faceIndex < SolverInputState.FaceCount; faceIndex++)
            {
                for (int cellIndex = 0; cellIndex < SolverInputState.FaceletPerFace; cellIndex++)
                {
                    int canonicalIndex = (faceIndex * SolverInputState.FaceletPerFace) + cellIndex;
                    CubeColor color = FromFaceCharacter(value[canonicalIndex]);
                    if (color == CubeColor.None)
                    {
                        error = $"Invalid facelet character: {value[canonicalIndex]}";
                        return false;
                    }

                    int inputIndex = GetInputIndexForCanonicalFacelet(faceIndex, cellIndex);
                    state.faceletColorIndexes[inputIndex] = (int)color;
                }
            }

            state.updatedAtUtc = DateTime.UtcNow.ToString("o");
            return true;
        }

        private static int GetInputIndexForCanonicalFacelet(int faceIndex, int cellIndex)
        {
            int row = cellIndex / 3;
            int col = cellIndex % 3;
            int inputRow = row;
            int inputCol = col;

            switch (faceIndex)
            {
                case 0: // U: manual 3D entry is viewed opposite to canonical top face orientation.
                case 3: // D: same correction as U.
                    inputRow = 2 - row;
                    inputCol = 2 - col;
                    break;
                case 2: // F: manual 3D entry is horizontally mirrored from canonical URFDLB.
                case 5: // B: same correction as F.
                    inputCol = 2 - col;
                    break;
            }

            return (faceIndex * SolverInputState.FaceletPerFace) + (inputRow * 3) + inputCol;
        }

        private static int VisualToLogicalFaceIndex(int visualFaceIndex)
        {
            switch (visualFaceIndex)
            {
                case 0: return 0;
                case 1: return 4;
                case 2: return 2;
                case 3: return 3;
                case 4: return 1;
                case 5: return 5;
                default: return visualFaceIndex;
            }
        }

        private static int MapVisualCellToLogicalCell(int visualFaceIndex, int logicalFaceIndex, int visualCellIndex)
        {
            if ((visualFaceIndex == 1 && logicalFaceIndex == 4)
                || (visualFaceIndex == 4 && logicalFaceIndex == 1))
            {
                int row = visualCellIndex / 3;
                int col = visualCellIndex % 3;
                return (row * 3) + (2 - col);
            }

            return visualCellIndex;
        }

        private static int MapVisualCellToPlaybackCell(int visualFaceIndex, int visualCellIndex)
        {
            if (visualFaceIndex == 0 || visualFaceIndex == 3)
            {
                int row = visualCellIndex / 3;
                int col = visualCellIndex % 3;
                return ((2 - row) * 3) + col;
            }

            return visualCellIndex;
        }

        private static CubeColor FromFaceCharacter(char value)
        {
            switch (char.ToUpperInvariant(value))
            {
                case 'U': return CubeColor.White;
                case 'R': return CubeColor.Red;
                case 'F': return CubeColor.Green;
                case 'D': return CubeColor.Yellow;
                case 'L': return CubeColor.Orange;
                case 'B': return CubeColor.Blue;
                default: return CubeColor.None;
            }
        }
    }
}
