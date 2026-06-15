using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Solver.Model;

namespace CubeChallenge3D.Solver.Services
{
    public static class SolverInputValidator
    {
        public static SolverInputValidationResult Validate(SolverInputState state)
        {
            var result = new SolverInputValidationResult();
            if (state == null)
            {
                result.messages.Add("Input state is empty.");
                return result;
            }

            state.EnsureShape();
            if (state.faceOrder != SolverInputState.DefaultFaceOrder)
            {
                result.messages.Add("Face order must be URFDLB.");
            }

            var counts = new Dictionary<CubeColor, int>
            {
                { CubeColor.White, 0 },
                { CubeColor.Red, 0 },
                { CubeColor.Green, 0 },
                { CubeColor.Yellow, 0 },
                { CubeColor.Orange, 0 },
                { CubeColor.Blue, 0 }
            };

            for (int i = 0; i < SolverInputState.FaceletCount; i++)
            {
                CubeColor color = (CubeColor)state.faceletColorIndexes[i];
                if (!counts.ContainsKey(color))
                {
                    result.messages.Add($"Facelet {i} is not filled.");
                    continue;
                }

                counts[color]++;
            }

            foreach (KeyValuePair<CubeColor, int> count in counts)
            {
                if (count.Value != 9)
                {
                    result.messages.Add($"{count.Key} count must be 9. Current: {count.Value}");
                }
            }

            var centerColors = new HashSet<CubeColor>();
            for (int face = 0; face < SolverInputState.FaceCount; face++)
            {
                CubeColor center = (CubeColor)state.faceletColorIndexes[(face * SolverInputState.FaceletPerFace) + 4];
                if (center == CubeColor.None)
                {
                    result.messages.Add($"Center facelet {face} is empty.");
                }
                else if (!centerColors.Add(center))
                {
                    result.messages.Add("Center facelets must all have different colors.");
                }
            }

            // Step 13 will add full cubie legality checks when the real solver is connected.
            result.isValid = result.messages.Count == 0;
            if (result.isValid)
            {
                result.messages.Add("Ready for solver.");
            }

            return result;
        }
    }
}
