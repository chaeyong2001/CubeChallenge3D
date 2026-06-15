using UnityEngine;

namespace CubeChallenge3D.Solver.Storage
{
    public static class SolverDebugCaseStore
    {
        private const string LatestFaceletsKey = "solver.debug.latestFacelets";

        public static void SaveLatest(string faceletString)
        {
            if (string.IsNullOrWhiteSpace(faceletString))
            {
                return;
            }

            PlayerPrefs.SetString(LatestFaceletsKey, faceletString);
            PlayerPrefs.Save();
        }

        public static bool TryLoadLatest(out string faceletString)
        {
            faceletString = PlayerPrefs.GetString(LatestFaceletsKey, string.Empty);
            return !string.IsNullOrWhiteSpace(faceletString);
        }
    }
}
