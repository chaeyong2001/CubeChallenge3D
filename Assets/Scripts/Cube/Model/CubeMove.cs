using System;

namespace CubeChallenge3D.Cube.Model
{
    public readonly struct CubeMove : IEquatable<CubeMove>
    {
        public CubeFace Face { get; }
        public CubeAxis Axis { get; }
        public int LayerIndex { get; }
        public int QuarterTurns { get; }
        public int AxisQuarterTurns { get; }
        public bool IsMiddleLayer => LayerIndex == 0;

        public CubeMove(CubeFace face, int quarterTurns)
        {
            if (!Enum.IsDefined(typeof(CubeFace), face))
            {
                throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }

            int normalizedTurns = MoveUtility.NormalizeQuarterTurns(quarterTurns);
            if (normalizedTurns == 0)
            {
                throw new ArgumentException("A cube move must turn a face.", nameof(quarterTurns));
            }

            Face = face;
            QuarterTurns = normalizedTurns;
            GetFaceLayer(face, out CubeAxis axis, out int layerIndex);
            Axis = axis;
            LayerIndex = layerIndex;
            AxisQuarterTurns = MoveUtility.NormalizeQuarterTurns(-normalizedTurns * layerIndex);
        }

        private CubeMove(CubeAxis axis, int layerIndex, int quarterTurns, bool axisTurns)
        {
            if (!Enum.IsDefined(typeof(CubeAxis), axis))
            {
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            if (layerIndex < -1 || layerIndex > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }

            int normalizedTurns = MoveUtility.NormalizeQuarterTurns(quarterTurns);
            if (normalizedTurns == 0)
            {
                throw new ArgumentException("A cube move must turn a layer.", nameof(quarterTurns));
            }

            Axis = axis;
            LayerIndex = layerIndex;
            AxisQuarterTurns = axisTurns
                ? normalizedTurns
                : MoveUtility.NormalizeQuarterTurns(-normalizedTurns * layerIndex);
            Face = GetNotationFace(axis, layerIndex);
            QuarterTurns = layerIndex == 0
                ? AxisTurnsToMiddleNotation(axis, AxisQuarterTurns)
                : MoveUtility.NormalizeQuarterTurns(-AxisQuarterTurns * layerIndex);
        }

        public static CubeMove CreateLayer(CubeAxis axis, int layerIndex, int axisQuarterTurns)
        {
            return new CubeMove(axis, layerIndex, axisQuarterTurns, true);
        }

        public override string ToString()
        {
            string face = IsMiddleLayer ? AxisToMiddleNotation(Axis) : FaceToNotation(Face);
            if (QuarterTurns == -1)
            {
                return face + "'";
            }

            return QuarterTurns == 2 ? face + "2" : face;
        }

        public static bool TryParse(string notation, out CubeMove move)
        {
            move = default;
            if (string.IsNullOrWhiteSpace(notation))
            {
                return false;
            }

            string value = notation.Trim().ToUpperInvariant();
            if (!TryParseBase(value[0], out CubeFace face, out CubeAxis middleAxis, out bool middle))
            {
                return false;
            }

            int turns;
            if (value.Length == 1)
            {
                turns = 1;
            }
            else if (value.Length == 2 && value[1] == '\'')
            {
                turns = -1;
            }
            else if (value.Length == 2 && value[1] == '2')
            {
                turns = 2;
            }
            else
            {
                return false;
            }

            move = middle
                ? CreateLayer(middleAxis, 0, MiddleNotationToAxisTurns(middleAxis, turns))
                : new CubeMove(face, turns);
            return true;
        }

        public bool Equals(CubeMove other)
        {
            return Axis == other.Axis
                && LayerIndex == other.LayerIndex
                && AxisQuarterTurns == other.AxisQuarterTurns;
        }

        public override bool Equals(object obj)
        {
            return obj is CubeMove other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Axis;
                hash = (hash * 397) ^ LayerIndex;
                return (hash * 397) ^ AxisQuarterTurns;
            }
        }

        public static bool operator ==(CubeMove left, CubeMove right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CubeMove left, CubeMove right)
        {
            return !left.Equals(right);
        }

        private static string FaceToNotation(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return "U";
                case CubeFace.Down: return "D";
                case CubeFace.Front: return "F";
                case CubeFace.Back: return "B";
                case CubeFace.Right: return "R";
                case CubeFace.Left: return "L";
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static bool TryParseBase(
            char value,
            out CubeFace face,
            out CubeAxis middleAxis,
            out bool middle)
        {
            middleAxis = default;
            middle = false;
            switch (value)
            {
                case 'U': face = CubeFace.Up; return true;
                case 'D': face = CubeFace.Down; return true;
                case 'F': face = CubeFace.Front; return true;
                case 'B': face = CubeFace.Back; return true;
                case 'R': face = CubeFace.Right; return true;
                case 'L': face = CubeFace.Left; return true;
                case 'M': face = CubeFace.Left; middleAxis = CubeAxis.X; middle = true; return true;
                case 'E': face = CubeFace.Down; middleAxis = CubeAxis.Y; middle = true; return true;
                case 'S': face = CubeFace.Front; middleAxis = CubeAxis.Z; middle = true; return true;
                default: face = default; return false;
            }
        }

        private static void GetFaceLayer(CubeFace face, out CubeAxis axis, out int layerIndex)
        {
            switch (face)
            {
                case CubeFace.Right: axis = CubeAxis.X; layerIndex = 1; return;
                case CubeFace.Left: axis = CubeAxis.X; layerIndex = -1; return;
                case CubeFace.Up: axis = CubeAxis.Y; layerIndex = 1; return;
                case CubeFace.Down: axis = CubeAxis.Y; layerIndex = -1; return;
                case CubeFace.Front: axis = CubeAxis.Z; layerIndex = 1; return;
                case CubeFace.Back: axis = CubeAxis.Z; layerIndex = -1; return;
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static CubeFace GetNotationFace(CubeAxis axis, int layerIndex)
        {
            switch (axis)
            {
                case CubeAxis.X: return layerIndex > 0 ? CubeFace.Right : CubeFace.Left;
                case CubeAxis.Y: return layerIndex > 0 ? CubeFace.Up : CubeFace.Down;
                case CubeAxis.Z: return layerIndex < 0 ? CubeFace.Back : CubeFace.Front;
                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        private static string AxisToMiddleNotation(CubeAxis axis)
        {
            switch (axis)
            {
                case CubeAxis.X: return "M";
                case CubeAxis.Y: return "E";
                case CubeAxis.Z: return "S";
                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        private static int MiddleNotationToAxisTurns(CubeAxis axis, int notationTurns)
        {
            return axis == CubeAxis.Z ? -notationTurns : notationTurns;
        }

        private static int AxisTurnsToMiddleNotation(CubeAxis axis, int axisTurns)
        {
            return MoveUtility.NormalizeQuarterTurns(axis == CubeAxis.Z ? -axisTurns : axisTurns);
        }
    }
}
