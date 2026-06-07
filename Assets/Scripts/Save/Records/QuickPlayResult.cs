using System;

namespace CubeChallenge3D.Save.Records
{
    [Serializable]
    public sealed class QuickPlayResult
    {
        public string id;
        public string completedAtUtc;
        public float elapsedSeconds;
        public int moveCount;
        public string scrambleNotation;
        public string moveLogNotation;
        public string controlMode;
        public bool usedUndo;
        public bool completed;

        public static QuickPlayResult Create(
            float elapsed,
            int moves,
            string scramble,
            string moveLog,
            string control,
            bool undoUsed)
        {
            return new QuickPlayResult
            {
                id = Guid.NewGuid().ToString(),
                completedAtUtc = DateTime.UtcNow.ToString("o"),
                elapsedSeconds = elapsed,
                moveCount = moves,
                scrambleNotation = scramble,
                moveLogNotation = moveLog,
                controlMode = control,
                usedUndo = undoUsed,
                completed = true
            };
        }
    }
}
