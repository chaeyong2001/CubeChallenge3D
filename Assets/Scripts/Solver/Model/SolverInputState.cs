using System;
using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Solver.Model
{
    [Serializable]
    public sealed class SolverInputState
    {
        public const int FaceCount = 6;
        public const int FaceletPerFace = 9;
        public const int FaceletCount = FaceCount * FaceletPerFace;
        public const string DefaultFaceOrder = "URFDLB";

        public int[] faceletColorIndexes = new int[FaceletCount];
        public int selectedColorIndex = (int)CubeColor.White;
        public string updatedAtUtc;
        public string faceOrder = DefaultFaceOrder;

        public static SolverInputState CreateEmpty()
        {
            var state = new SolverInputState();
            for (int i = 0; i < FaceletCount; i++)
            {
                state.faceletColorIndexes[i] = (int)CubeColor.None;
            }

            state.selectedColorIndex = (int)CubeColor.White;
            state.updatedAtUtc = DateTime.UtcNow.ToString("o");
            state.faceOrder = DefaultFaceOrder;
            return state;
        }

        public static SolverInputState CreateSolved()
        {
            var state = new SolverInputState();
            for (int face = 0; face < FaceCount; face++)
            {
                CubeColor color = GetSolvedColorForFace(face);
                for (int cell = 0; cell < FaceletPerFace; cell++)
                {
                    state.faceletColorIndexes[(face * FaceletPerFace) + cell] = (int)color;
                }
            }

            state.updatedAtUtc = DateTime.UtcNow.ToString("o");
            return state;
        }

        public void EnsureShape()
        {
            if (faceletColorIndexes == null || faceletColorIndexes.Length != FaceletCount)
            {
                faceletColorIndexes = CreateEmpty().faceletColorIndexes;
            }

            if (string.IsNullOrWhiteSpace(faceOrder))
            {
                faceOrder = DefaultFaceOrder;
            }
        }

        public static CubeColor GetSolvedColorForFace(int faceIndex)
        {
            switch (faceIndex)
            {
                case 0: return CubeColor.White;  // U
                case 1: return CubeColor.Red;    // R
                case 2: return CubeColor.Green;  // F
                case 3: return CubeColor.Yellow; // D
                case 4: return CubeColor.Orange; // L
                case 5: return CubeColor.Blue;   // B
                default: return CubeColor.None;
            }
        }
    }
}
