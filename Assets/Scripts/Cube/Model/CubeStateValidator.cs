using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Cube.Model
{
    public static class CubeStateValidator
    {
        public static bool IsColorCountValid(CubeState state)
        {
            if (state == null)
            {
                return false;
            }

            var counts = new Dictionary<CubeColor, int>();
            foreach (CubeColor color in Enum.GetValues(typeof(CubeColor)))
            {
                counts[color] = 0;
            }

            foreach (CubeFace face in Enum.GetValues(typeof(CubeFace)))
            {
                for (int row = 0; row < CubeState.FaceSize; row++)
                {
                    for (int col = 0; col < CubeState.FaceSize; col++)
                    {
                        counts[state.GetColor(face, row, col)]++;
                    }
                }
            }

            return counts[CubeColor.White] == 9
                && counts[CubeColor.Yellow] == 9
                && counts[CubeColor.Green] == 9
                && counts[CubeColor.Blue] == 9
                && counts[CubeColor.Red] == 9
                && counts[CubeColor.Orange] == 9
                && counts[CubeColor.None] == 0;
        }

        public static bool ValidateBasic(CubeState state)
        {
            return IsColorCountValid(state);
        }
    }
}
