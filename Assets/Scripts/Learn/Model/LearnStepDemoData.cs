using System;

namespace CubeChallenge3D.Learn.Model
{
    [Serializable]
    public sealed class LearnStepDemoData
    {
        public string substepTitle;
        public string instructionText;
        public string setupNotation;
        public string goalSetupNotation;
        public string startFaceletString;
        public string[] demoMoves;
        public string[] moveInstructions;
        public string targetDescription;
        public string highlightedCubieHint;
        public string targetSlotHint;
        public string targetSideColorName;
        public string expectedWhiteFaceName;
        public int wholeCubeYawQuarterTurns;
        public bool generateSetupFromDemoMoves;
        public int targetStartX;
        public int targetStartY;
        public int targetStartZ;
        public int targetGoalX;
        public int targetGoalY;
        public int targetGoalZ;
    }
}
