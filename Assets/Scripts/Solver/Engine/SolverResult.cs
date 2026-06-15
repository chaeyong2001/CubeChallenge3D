using System;

namespace CubeChallenge3D.Solver.Engine
{
    [Serializable]
    public sealed class SolverResult
    {
        public bool success;
        public bool isValidCube;
        public bool isEngineAvailable;
        public string errorCode;
        public string message;
        public string[] moveNotations;
        public string solutionNotation;
        public int moveCount;
        public int elapsedMs;
        public string engineName;
        public string debugMessage;
        public int maxDepth;
        public int timeoutMs;
        public long searchedNodes;
    }
}
