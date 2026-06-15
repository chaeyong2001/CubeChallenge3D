using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Solver.Engine
{
    [Serializable]
    public sealed class SolverValidationResult
    {
        public bool isValid;
        public string errorCode;
        public string userMessage;
        public string debugMessage;
        public List<string> details = new List<string>();
    }
}
