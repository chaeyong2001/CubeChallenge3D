using System;

namespace CubeChallenge3D.Cloud
{
    [Serializable]
    public sealed class CloudSavePayload
    {
        public int saveVersion = 1;
        public CloudSaveProfile profile = new CloudSaveProfile();
        public string playerProfileJson = string.Empty;
        public string stageProgressJson = string.Empty;
        public string inventoryJson = string.Empty;
        public string economyWalletJson = string.Empty;
        public string quickPlayRecordsJson = string.Empty;
        public string rankingLocalRecordsJson = string.Empty;
        public CloudSaveUserSettings userSettings = new CloudSaveUserSettings();
        public string clientUpdatedAtUtc = string.Empty;
    }

    [Serializable]
    public sealed class CloudSaveProfile
    {
        public string profileId = string.Empty;
        public string nickname = string.Empty;
        public int avatarId;
        public bool isServerSynced;
        public bool googlePlayLinked;
        public bool googleLinked;
    }

    [Serializable]
    public sealed class CloudSaveUserSettings
    {
        public string languageCode = "en";
        public string controlMode = "Drag";
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
        public float viewSensitivity = 0.25f;
        public float faceDragThreshold = 40f;
    }
}
