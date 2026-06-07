using System;
using System.Text;

namespace CubeChallenge3D.Cube.Model
{
    public static class CubeStateSerializer
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

        public static string ToFaceletString(CubeState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var result = new StringBuilder(CubeState.FaceletCount);
            foreach (CubeFace face in FaceletOrder)
            {
                for (int row = 0; row < CubeState.FaceSize; row++)
                {
                    for (int col = 0; col < CubeState.FaceSize; col++)
                    {
                        result.Append(ToFaceletCharacter(state.GetColor(face, row, col)));
                    }
                }
            }

            return result.ToString();
        }

        public static CubeState FromFaceletString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != CubeState.FaceletCount)
            {
                throw new FormatException($"A cube facelet string must contain {CubeState.FaceletCount} characters.");
            }

            var state = new CubeState();
            int index = 0;
            foreach (CubeFace face in FaceletOrder)
            {
                for (int row = 0; row < CubeState.FaceSize; row++)
                {
                    for (int col = 0; col < CubeState.FaceSize; col++)
                    {
                        state.SetColor(face, row, col, FromFaceletCharacter(value[index++]));
                    }
                }
            }

            return state;
        }

        private static char ToFaceletCharacter(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return 'U';
                case CubeColor.Red: return 'R';
                case CubeColor.Green: return 'F';
                case CubeColor.Yellow: return 'D';
                case CubeColor.Orange: return 'L';
                case CubeColor.Blue: return 'B';
                default: throw new FormatException($"Color {color} cannot be serialized as a solver facelet.");
            }
        }

        private static CubeColor FromFaceletCharacter(char value)
        {
            switch (char.ToUpperInvariant(value))
            {
                case 'U': return CubeColor.White;
                case 'R': return CubeColor.Red;
                case 'F': return CubeColor.Green;
                case 'D': return CubeColor.Yellow;
                case 'L': return CubeColor.Orange;
                case 'B': return CubeColor.Blue;
                default: throw new FormatException($"Invalid cube facelet character: {value}");
            }
        }
    }
}
