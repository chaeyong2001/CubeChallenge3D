using System;
using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Learn.Playback
{
    public static class LearnCubeOrientationUtility
    {
        public static CubeState RotateAroundUpAxis(CubeState source, int quarterTurns)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int turns = ((quarterTurns % 4) + 4) % 4;
            CubeState result = source.Clone();
            for (int i = 0; i < turns; i++)
            {
                result = RotatePositiveQuarterTurn(result);
            }

            return result;
        }

        private static CubeState RotatePositiveQuarterTurn(CubeState source)
        {
            var result = new CubeState();
            foreach (CubeFace face in Enum.GetValues(typeof(CubeFace)))
            {
                for (int row = 0; row < CubeState.FaceSize; row++)
                {
                    for (int col = 0; col < CubeState.FaceSize; col++)
                    {
                        GetTransform(face, row, col, out IntVector3 position, out IntVector3 normal);
                        IntVector3 rotatedPosition = RotatePositiveY(position);
                        IntVector3 rotatedNormal = RotatePositiveY(normal);
                        GetFacelet(rotatedPosition, rotatedNormal, out CubeFace targetFace, out int targetRow, out int targetCol);
                        result.SetColor(targetFace, targetRow, targetCol, source.GetColor(face, row, col));
                    }
                }
            }

            return result;
        }

        private static IntVector3 RotatePositiveY(IntVector3 value)
        {
            return new IntVector3(value.Z, value.Y, -value.X);
        }

        private static void GetTransform(
            CubeFace face,
            int row,
            int col,
            out IntVector3 position,
            out IntVector3 normal)
        {
            switch (face)
            {
                case CubeFace.Up:
                    position = new IntVector3(col - 1, 1, row - 1);
                    normal = new IntVector3(0, 1, 0);
                    return;
                case CubeFace.Down:
                    position = new IntVector3(col - 1, -1, 1 - row);
                    normal = new IntVector3(0, -1, 0);
                    return;
                case CubeFace.Front:
                    position = new IntVector3(col - 1, 1 - row, 1);
                    normal = new IntVector3(0, 0, 1);
                    return;
                case CubeFace.Back:
                    position = new IntVector3(1 - col, 1 - row, -1);
                    normal = new IntVector3(0, 0, -1);
                    return;
                case CubeFace.Right:
                    position = new IntVector3(1, 1 - row, 1 - col);
                    normal = new IntVector3(1, 0, 0);
                    return;
                case CubeFace.Left:
                    position = new IntVector3(-1, 1 - row, col - 1);
                    normal = new IntVector3(-1, 0, 0);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static void GetFacelet(
            IntVector3 position,
            IntVector3 normal,
            out CubeFace face,
            out int row,
            out int col)
        {
            if (normal.Y == 1)
            {
                face = CubeFace.Up; row = position.Z + 1; col = position.X + 1; return;
            }

            if (normal.Y == -1)
            {
                face = CubeFace.Down; row = 1 - position.Z; col = position.X + 1; return;
            }

            if (normal.Z == 1)
            {
                face = CubeFace.Front; row = 1 - position.Y; col = position.X + 1; return;
            }

            if (normal.Z == -1)
            {
                face = CubeFace.Back; row = 1 - position.Y; col = 1 - position.X; return;
            }

            if (normal.X == 1)
            {
                face = CubeFace.Right; row = 1 - position.Y; col = 1 - position.Z; return;
            }

            if (normal.X == -1)
            {
                face = CubeFace.Left; row = 1 - position.Y; col = position.Z + 1; return;
            }

            throw new InvalidOperationException("Invalid rotated facelet normal.");
        }

        private readonly struct IntVector3
        {
            public int X { get; }
            public int Y { get; }
            public int Z { get; }

            public IntVector3(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }
    }
}
