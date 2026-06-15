using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Learn.Model
{
    [Serializable]
    public sealed class LearnLessonProgress
    {
        public int saveVersion;
        public List<string> completedLessonIds = new List<string>();
        public string lastOpenedCategory;
    }
}
