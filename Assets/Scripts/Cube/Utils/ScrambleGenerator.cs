using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Cube.Utils
{
    public static class ScrambleGenerator
    {
        private static readonly CubeFace[] Faces =
        {
            CubeFace.Up,
            CubeFace.Down,
            CubeFace.Front,
            CubeFace.Back,
            CubeFace.Right,
            CubeFace.Left
        };

        private static readonly int[] TurnOptions = { 1, -1, 2 };

        public static List<CubeMove> Generate(int length = 20, int? seed = null)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Random random = seed.HasValue ? new Random(seed.Value) : new Random();
            var result = new List<CubeMove>(length);
            CubeFace? previousFace = null;
            CubeAxis? previousAxis = null;

            for (int i = 0; i < length; i++)
            {
                CubeFace face = PickFace(random, previousFace, previousAxis);
                int turns = TurnOptions[random.Next(TurnOptions.Length)];
                result.Add(new CubeMove(face, turns));
                previousFace = face;
                previousAxis = GetAxis(face);
            }

            return result;
        }

        private static CubeFace PickFace(Random random, CubeFace? previousFace, CubeAxis? previousAxis)
        {
            var candidates = new List<CubeFace>(Faces.Length);
            foreach (CubeFace face in Faces)
            {
                if (face != previousFace && GetAxis(face) != previousAxis)
                {
                    candidates.Add(face);
                }
            }

            return candidates[random.Next(candidates.Count)];
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
                case CubeFace.Front:
                case CubeFace.Back:
                    return CubeAxis.Z;
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }
    }
}
