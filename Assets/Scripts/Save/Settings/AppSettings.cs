using System;

namespace CubeChallenge3D.Save.Settings
{
    [Serializable]
    public sealed class AppSettings
    {
        public string languageCode = "en";
        public string controlMode = "Drag";
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
        public bool showDebugPanel = true;
        public float viewSensitivity = 0.25f;
        public float faceDragThreshold = 40f;
        public bool tutorialSeen;
        public string playerName = "Player";
        public string playerId;

        public static AppSettings CreateDefault()
        {
            return new AppSettings();
        }
    }
}
