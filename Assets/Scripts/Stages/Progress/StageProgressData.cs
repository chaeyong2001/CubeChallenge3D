using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Stages.Progress
{
    [Serializable]
    public sealed class StageProgressData
    {
        public int saveVersion;
        public List<StageProgress> stages = new List<StageProgress>();
    }
}
