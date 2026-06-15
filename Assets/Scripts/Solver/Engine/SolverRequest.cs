using System;

namespace CubeChallenge3D.Solver.Engine
{
    [Serializable]
    public sealed class SolverRequest
    {
        public string faceletString;
        public string faceOrder = "URFDLB";
        public int maxDepth = 25;
        public int timeoutMs = 5000;
        public bool requireFullValidation = true;
    }
}
