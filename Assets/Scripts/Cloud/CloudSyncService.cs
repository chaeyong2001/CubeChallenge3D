using System;
using System.IO;
using System.Threading.Tasks;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.Save.Settings;
using UnityEngine;

namespace CubeChallenge3D.Cloud
{
    public sealed class CloudSyncService
    {
        private const string PlayerProfileFile = "player_profile.json";
        private const string StageProgressFile = "stage_progress.json";
        private const string InventoryFile = "inventory.json";
        private const string EconomyWalletFile = "economy_wallet.json";
        private const string QuickPlayRecordsFile = "quick_play_records.json";
        private const string RankingLocalRecordsFile = "local_ranking_records.json";

        private readonly SettingsStore settingsStore;
        private readonly PlayerProfileStore profileStore;
        private readonly CloudSyncApiClient apiClient;
        private bool busy;

        public CloudSyncService(SettingsStore settingsStore)
        {
            this.settingsStore = settingsStore ?? new SettingsStore();
            profileStore = new PlayerProfileStore();
            apiClient = new CloudSyncApiClient(
                this.settingsStore.Current?.rankingApiBaseUrl ?? string.Empty,
                this.settingsStore.Current?.rankingRequestTimeoutSeconds ?? 8);
        }

        public bool IsBusy => busy;

        public bool CanSync(out string reason)
        {
            PlayerProfile profile = profileStore.Current;
            if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
            {
                reason = "Create a profile first.";
                return false;
            }

            if (!profile.linkedGooglePlay && !profile.linkedGoogle)
            {
                reason = "Link Google Play Games or Google Account to enable cloud sync.";
                return false;
            }

            if (!apiClient.HasServerUrl)
            {
                reason = "Server URL is empty.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public CloudSavePayload BuildLocalPayload()
        {
            PlayerProfile profile = profileStore.Current;
            AppSettings settings = settingsStore.Current ?? AppSettings.CreateDefault();
            CloudSavePayload payload = new CloudSavePayload
            {
                saveVersion = 1,
                playerProfileJson = ReadSaveFile(PlayerProfileFile),
                stageProgressJson = ReadSaveFile(StageProgressFile),
                inventoryJson = ReadSaveFile(InventoryFile),
                economyWalletJson = ReadSaveFile(EconomyWalletFile),
                quickPlayRecordsJson = ReadSaveFile(QuickPlayRecordsFile),
                rankingLocalRecordsJson = ReadSaveFile(RankingLocalRecordsFile),
                clientUpdatedAtUtc = DateTime.UtcNow.ToString("o"),
                profile = new CloudSaveProfile
                {
                    profileId = profile?.profileId ?? string.Empty,
                    nickname = profile?.nickname ?? string.Empty,
                    avatarId = profile?.avatarId ?? 0,
                    isServerSynced = profile?.isServerSynced ?? false,
                    googlePlayLinked = profile?.linkedGooglePlay ?? false,
                    googleLinked = profile?.linkedGoogle ?? false
                },
                userSettings = new CloudSaveUserSettings
                {
                    languageCode = settings.languageCode,
                    controlMode = settings.controlMode,
                    soundEnabled = settings.soundEnabled,
                    vibrationEnabled = settings.vibrationEnabled,
                    viewSensitivity = settings.viewSensitivity,
                    faceDragThreshold = settings.faceDragThreshold
                }
            };
            return payload;
        }

        public async Task<CloudSaveStatusResult> GetCloudSaveStatusAsync()
        {
            if (!CanSync(out string reason))
            {
                return CloudSaveStatusResult.Unavailable(reason);
            }

            return await apiClient.GetStatusAsync(profileStore.Current.profileId);
        }

        public async Task<CloudSaveUploadResult> UploadCurrentSaveAsync()
        {
            if (busy)
            {
                return CloudSaveUploadResult.Unavailable("Cloud sync is already running.");
            }

            if (!CanSync(out string reason))
            {
                return CloudSaveUploadResult.Unavailable(reason);
            }

            busy = true;
            try
            {
                CloudSavePayload payload = BuildLocalPayload();
                return await apiClient.UploadAsync(
                    profileStore.Current.profileId,
                    payload,
                    string.Empty,
                    Application.version);
            }
            finally
            {
                busy = false;
            }
        }

        public async Task<CloudSaveDownloadResult> DownloadCloudSaveAsync()
        {
            if (busy)
            {
                return CloudSaveDownloadResult.Unavailable("Cloud sync is already running.");
            }

            if (!CanSync(out string reason))
            {
                return CloudSaveDownloadResult.Unavailable(reason);
            }

            busy = true;
            try
            {
                CloudSaveDownloadResult result = await apiClient.DownloadAsync(profileStore.Current.profileId);
                if (result.success && result.response?.payload != null)
                {
                    ApplyCloudSave(result.response.payload);
                }

                return result;
            }
            finally
            {
                busy = false;
            }
        }

        public string ApplyCloudSave(CloudSavePayload payload)
        {
            if (payload == null)
            {
                return string.Empty;
            }

            string backupDirectory = CreateRestoreBackup();
            WriteSaveFile(PlayerProfileFile, payload.playerProfileJson);
            WriteSaveFile(StageProgressFile, payload.stageProgressJson);
            WriteSaveFile(InventoryFile, payload.inventoryJson);
            WriteSaveFile(EconomyWalletFile, payload.economyWalletJson);
            WriteSaveFile(QuickPlayRecordsFile, payload.quickPlayRecordsJson);
            WriteSaveFile(RankingLocalRecordsFile, payload.rankingLocalRecordsJson);
            ApplyUserSettings(payload.userSettings);
            profileStore.ReloadFromDisk();
            Debug.Log($"[CloudSync] Local backup created before restore: {backupDirectory}");
            return backupDirectory;
        }

        private void ApplyUserSettings(CloudSaveUserSettings cloudSettings)
        {
            if (cloudSettings == null || settingsStore.Current == null)
            {
                return;
            }

            AppSettings current = settingsStore.Current;
            current.languageCode = string.IsNullOrWhiteSpace(cloudSettings.languageCode) ? current.languageCode : cloudSettings.languageCode;
            current.controlMode = string.IsNullOrWhiteSpace(cloudSettings.controlMode) ? current.controlMode : cloudSettings.controlMode;
            current.soundEnabled = cloudSettings.soundEnabled;
            current.vibrationEnabled = cloudSettings.vibrationEnabled;
            current.viewSensitivity = cloudSettings.viewSensitivity;
            current.faceDragThreshold = cloudSettings.faceDragThreshold;
            settingsStore.Save();
        }

        private static string CreateRestoreBackup()
        {
            string directory = Path.Combine(
                Application.persistentDataPath,
                "cloud_restore_backup_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(directory);
            BackupFile(PlayerProfileFile, directory);
            BackupFile(StageProgressFile, directory);
            BackupFile(InventoryFile, directory);
            BackupFile(EconomyWalletFile, directory);
            BackupFile(QuickPlayRecordsFile, directory);
            BackupFile(RankingLocalRecordsFile, directory);
            BackupFile("app_settings.json", directory);
            return directory;
        }

        private static void BackupFile(string fileName, string backupDirectory)
        {
            string source = SaveService.GetPath(fileName);
            if (!File.Exists(source))
            {
                return;
            }

            File.Copy(source, Path.Combine(backupDirectory, fileName), true);
        }

        private static string ReadSaveFile(string fileName)
        {
            string path = SaveService.GetPath(fileName);
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CloudSync] Failed to read '{fileName}': {exception.Message}");
                return string.Empty;
            }
        }

        private static void WriteSaveFile(string fileName, string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            string path = SaveService.GetPath(fileName);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = path + ".cloud_tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, path + ".bak", true);
            }
            else
            {
                File.Move(tempPath, path);
                File.Copy(path, path + ".bak", true);
            }
        }
    }
}
