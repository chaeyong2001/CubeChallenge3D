using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Learn.Model
{
    [Serializable]
    public sealed class LearnCategoryData
    {
        public string categoryId;
        public string title;
        public string description;
        public int order;
        public List<LearnLessonData> lessons = new List<LearnLessonData>();
    }
}
