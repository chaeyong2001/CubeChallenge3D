using System;

namespace CubeChallenge3D.Solver.Engine.HighPerformance
{
    public static class TwoPhaseFaceletConverter
    {
        private const string FaceOrder = "URFLBD";
        private static readonly int[] SourceOffsets = { 0, 9, 18, 36, 45, 27 };
        private static readonly int[] RingIndexes = { 0, 1, 2, 5, 8, 7, 6, 3 };

        // The library stores each face ring clockwise:
        // top-left, top, top-right, right, bottom-right, bottom, bottom-left, left.
        // Faces themselves are ordered U,R,F,L,B,D.
        public static byte[] ToTwoPhaseColors(string urfdlbFacelets)
        {
            if (string.IsNullOrWhiteSpace(urfdlbFacelets) || urfdlbFacelets.Length != 54)
            {
                throw new ArgumentException("Expected a 54-character URFDLB facelet string.", nameof(urfdlbFacelets));
            }

            string value = urfdlbFacelets.Trim().ToUpperInvariant();
            byte[] result = new byte[48];
            int target = 0;
            for (int face = 0; face < FaceOrder.Length; face++)
            {
                for (int i = 0; i < RingIndexes.Length; i++)
                {
                    char facelet = value[SourceOffsets[face] + RingIndexes[i]];
                    int color = FaceOrder.IndexOf(facelet);
                    if (color < 0)
                    {
                        throw new ArgumentException($"Unsupported facelet character: {facelet}", nameof(urfdlbFacelets));
                    }

                    result[target++] = (byte)color;
                }
            }

            return result;
        }
    }
}
