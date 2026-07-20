using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Cube.Model
{
    public sealed class CubeState : IEquatable<CubeState>
    {
        public const int FaceSize = 3;
        public const int FaceletCount = 54;

        private readonly CubeColor[,,] facelets;

        public CubeState()
        {
            facelets = new CubeColor[6, FaceSize, FaceSize];
        }

        private CubeState(CubeColor[,,] source)
        {
            facelets = (CubeColor[,,])source.Clone();
        }

        public static CubeState CreateSolved()
        {
            var state = new CubeState();
            state.FillFace(CubeFace.Up, CubeColor.White);
            state.FillFace(CubeFace.Down, CubeColor.Yellow);
            state.FillFace(CubeFace.Front, CubeColor.Green);
            state.FillFace(CubeFace.Back, CubeColor.Blue);
            state.FillFace(CubeFace.Right, CubeColor.Red);
            state.FillFace(CubeFace.Left, CubeColor.Orange);
            return state;
        }

        public CubeState Clone()
        {
            return new CubeState(facelets);
        }

        public CubeColor GetColor(CubeFace face, int row, int col)
        {
            ValidateFacelet(face, row, col);
            return facelets[(int)face, row, col];
        }

        public void SetColor(CubeFace face, int row, int col, CubeColor color)
        {
            ValidateFacelet(face, row, col);
            if (!Enum.IsDefined(typeof(CubeColor), color))
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
            }

            facelets[(int)face, row, col] = color;
        }

        public bool IsSolved()
        {
            if (!CubeStateValidator.IsColorCountValid(this))
            {
                return false;
            }

            foreach (CubeFace face in Enum.GetValues(typeof(CubeFace)))
            {
                CubeColor center = GetColor(face, 1, 1);
                for (int row = 0; row < FaceSize; row++)
                {
                    for (int col = 0; col < FaceSize; col++)
                    {
                        if (GetColor(face, row, col) != center)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void ApplyMove(CubeMove move)
        {
            int positiveAxisTurns = move.AxisQuarterTurns == -1 ? 3 : move.AxisQuarterTurns;
            for (int i = 0; i < positiveAxisTurns; i++)
            {
                ApplyPositiveLayerTurn(move.Axis, move.LayerIndex);
            }
        }

        public void ApplyMoves(IEnumerable<CubeMove> moves)
        {
            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            foreach (CubeMove move in moves)
            {
                ApplyMove(move);
            }
        }

        public bool Equals(CubeState other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            foreach (CubeFace face in Enum.GetValues(typeof(CubeFace)))
            {
                for (int row = 0; row < FaceSize; row++)
                {
                    for (int col = 0; col < FaceSize; col++)
                    {
                        if (GetColor(face, row, col) != other.GetColor(face, row, col))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is CubeState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (CubeFace face in Enum.GetValues(typeof(CubeFace)))
                {
                    for (int row = 0; row < FaceSize; row++)
                    {
                        for (int col = 0; col < FaceSize; col++)
                        {
                            hash = (hash * 31) + (int)GetColor(face, row, col);
                        }
                    }
                }

                return hash;
            }
        }

        private void ApplyPositiveLayerTurn(CubeAxis rotationAxis, int layerIndex)
        {
            IntVector3 axis = GetAxisVector(rotationAxis);
            var next = (CubeColor[,,])facelets.Clone();

            foreach (CubeFace sourceFace in Enum.GetValues(typeof(CubeFace)))
            {
                for (int row = 0; row < FaceSize; row++)
                {
                    for (int col = 0; col < FaceSize; col++)
                    {
                        GetFaceletTransform(sourceFace, row, col, out IntVector3 position, out IntVector3 normal);
                        if (GetAxisValue(position, rotationAxis) != layerIndex)
                        {
                            continue;
                        }

                        IntVector3 rotatedPosition = RotatePositive(position, axis);
                        IntVector3 rotatedNormal = RotatePositive(normal, axis);
                        GetFaceletIndex(rotatedPosition, rotatedNormal, out CubeFace targetFace, out int targetRow, out int targetCol);
                        next[(int)targetFace, targetRow, targetCol] = facelets[(int)sourceFace, row, col];
                    }
                }
            }

            Array.Copy(next, facelets, next.Length);
        }

        private static IntVector3 GetAxisVector(CubeAxis axis)
        {
            switch (axis)
            {
                case CubeAxis.X: return new IntVector3(1, 0, 0);
                case CubeAxis.Y: return new IntVector3(0, 1, 0);
                case CubeAxis.Z: return new IntVector3(0, 0, 1);
                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        private static int GetAxisValue(IntVector3 value, CubeAxis axis)
        {
            switch (axis)
            {
                case CubeAxis.X: return value.X;
                case CubeAxis.Y: return value.Y;
                case CubeAxis.Z: return value.Z;
                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        private void FillFace(CubeFace face, CubeColor color)
        {
            for (int row = 0; row < FaceSize; row++)
            {
                for (int col = 0; col < FaceSize; col++)
                {
                    facelets[(int)face, row, col] = color;
                }
            }
        }

        private static void ValidateFacelet(CubeFace face, int row, int col)
        {
            if (!Enum.IsDefined(typeof(CubeFace), face))
            {
                throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }

            if (row < 0 || row >= FaceSize)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            if (col < 0 || col >= FaceSize)
            {
                throw new ArgumentOutOfRangeException(nameof(col));
            }
        }

        private static IntVector3 GetFaceNormal(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return new IntVector3(0, 1, 0);
                case CubeFace.Down: return new IntVector3(0, -1, 0);
                case CubeFace.Front: return new IntVector3(0, 0, 1);
                case CubeFace.Back: return new IntVector3(0, 0, -1);
                case CubeFace.Right: return new IntVector3(1, 0, 0);
                case CubeFace.Left: return new IntVector3(-1, 0, 0);
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static void GetFaceletTransform(
            CubeFace face,
            int row,
            int col,
            out IntVector3 position,
            out IntVector3 normal)
        {
            normal = GetFaceNormal(face);
            switch (face)
            {
                case CubeFace.Up: position = new IntVector3(col - 1, 1, row - 1); return;
                case CubeFace.Down: position = new IntVector3(col - 1, -1, 1 - row); return;
                case CubeFace.Front: position = new IntVector3(col - 1, 1 - row, 1); return;
                case CubeFace.Back: position = new IntVector3(1 - col, 1 - row, -1); return;
                case CubeFace.Right: position = new IntVector3(1, 1 - row, 1 - col); return;
                case CubeFace.Left: position = new IntVector3(-1, 1 - row, col - 1); return;
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static void GetFaceletIndex(
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

            throw new InvalidOperationException("Invalid facelet normal.");
        }

        private static IntVector3 RotatePositive(IntVector3 value, IntVector3 axis)
        {
            IntVector3 cross = IntVector3.Cross(axis, value);
            int dot = IntVector3.Dot(axis, value);
            return cross + (axis * dot);
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

            public static int Dot(IntVector3 left, IntVector3 right)
            {
                return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
            }

            public static IntVector3 Cross(IntVector3 left, IntVector3 right)
            {
                return new IntVector3(
                    (left.Y * right.Z) - (left.Z * right.Y),
                    (left.Z * right.X) - (left.X * right.Z),
                    (left.X * right.Y) - (left.Y * right.X));
            }

            public static IntVector3 operator -(IntVector3 value)
            {
                return new IntVector3(-value.X, -value.Y, -value.Z);
            }

            public static IntVector3 operator +(IntVector3 left, IntVector3 right)
            {
                return new IntVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
            }

            public static IntVector3 operator *(IntVector3 value, int scalar)
            {
                return new IntVector3(value.X * scalar, value.Y * scalar, value.Z * scalar);
            }
        }
    }
}
