using System;

namespace CubeChallenge3D.Save.Settings
{
    [Serializable]
    public sealed class AppSettings
    {
        public const string ProductionRankingApiBaseUrl = "https://ranking-api-production-2163.up.railway.app";

        public int saveVersion;
        public string languageCode = "en";
        public string controlMode = "Drag";
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
        public bool pushNotificationsEnabled = true;
        public bool showDebugPanel;
        public float viewSensitivity = 0.25f;
        public float faceDragThreshold = 40f;
        public bool tutorialSeen;
        public string playerName = "Player";
        public string playerId;
        public bool useServerRanking = true;
        public string rankingApiBaseUrl = ProductionRankingApiBaseUrl;
        public int rankingRequestTimeoutSeconds = 8;

        public static AppSettings CreateDefault()
        {
            return new AppSettings();
        }
    }
}
