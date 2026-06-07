using System;

namespace CubeChallenge3D.Cube.Model
{
    public readonly struct CubeMove : IEquatable<CubeMove>
    {
        public CubeFace Face { get; }
        public int QuarterTurns { get; }

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
        }

        public override string ToString()
        {
            string face = FaceToNotation(Face);
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
            if (!TryParseFace(value[0], out CubeFace face))
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

            move = new CubeMove(face, turns);
            return true;
        }

        public bool Equals(CubeMove other)
        {
            return Face == other.Face && QuarterTurns == other.QuarterTurns;
        }

        public override bool Equals(object obj)
        {
            return obj is CubeMove other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Face * 397) ^ QuarterTurns;
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

        private static bool TryParseFace(char value, out CubeFace face)
        {
            switch (value)
            {
                case 'U': face = CubeFace.Up; return true;
                case 'D': face = CubeFace.Down; return true;
                case 'F': face = CubeFace.Front; return true;
                case 'B': face = CubeFace.Back; return true;
                case 'R': face = CubeFace.Right; return true;
                case 'L': face = CubeFace.Left; return true;
                default: face = default; return false;
            }
        }
    }
}
