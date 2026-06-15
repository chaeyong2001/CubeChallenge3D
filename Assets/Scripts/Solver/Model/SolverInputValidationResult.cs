using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Solver.Model
{
    [Serializable]
    public sealed class SolverInputValidationResult
    {
        public bool isValid;
        public List<string> messages = new List<string>();
    }
}
