using System;

namespace CubeChallenge3D.Solver.Model
{
    [Serializable]
    public sealed class SolverSolution
    {
        public string sourceFaceletString;
        public string sourceColorFaceletString;
        public string solutionNotation;
        public string[] moveNotations;
        public string[] moveDescriptions;
        public string completionMessage;
        public string completionGuideMessage;
        public int moveCount;
        public string orientationMode;
        public string displayFrontFace;
        public string displayTopFace;
        public string displayRightFace;
        public string createdAtUtc;

        public static SolverSolution FromResult(string sourceFaceletString, CubeChallenge3D.Solver.Engine.SolverResult result)
        {
            return new SolverSolution
            {
                sourceFaceletString = sourceFaceletString,
                solutionNotation = result != null ? result.solutionNotation : string.Empty,
                moveNotations = result != null ? result.moveNotations : new string[0],
                moveCount = result != null ? result.moveCount : 0,
                orientationMode = "InputOrientation",
                displayFrontFace = "Front",
                displayTopFace = "Top",
                displayRightFace = "Right",
                createdAtUtc = DateTime.UtcNow.ToString("o")
            };
        }
    }
}
