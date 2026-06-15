using System;

namespace CubeChallenge3D.Learn.Model
{
    [Serializable]
    public sealed class LearnLessonData
    {
        public string lessonId;
        public string categoryId;
        public string title;
        public string shortDescription;
        public string bodyText;
        public string[] moveNotations;
        public string[] demoMoves;
        public string demoSetupNotation;
        public string demoStartFaceletString;
        public string demoGoalDescription;
        public string demoType;
        public string demoPurpose;
        public int[] highlightedFaceletIndexes;
        public string highlightedCubieHint;
        public string targetSlotHint;
        public LearnStepDemoData[] demoSubsteps;
        public string[] keyPoints;
        public int order;
        public bool has3DDemo;
        public bool isExpandedContent;
    }
}
