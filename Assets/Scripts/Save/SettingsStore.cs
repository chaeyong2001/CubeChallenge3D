using CubeChallenge3D.Save.Settings;
using System;

namespace CubeChallenge3D.Save
{
    public sealed class SettingsStore
    {
        private const string FileName = "app_settings.json";

        public AppSettings Current { get; private set; }

        public SettingsStore()
        {
            Load();
        }

        public AppSettings Load()
        {
            Current = SaveService.LoadJson(FileName, AppSettings.CreateDefault());
            bool changed = false;
            if (Current == null)
            {
                Current = AppSettings.CreateDefault();
                changed = true;
            }
            if (Current.saveVersion < SaveDataValidator.CurrentSaveVersion)
            {
                Current.saveVersion = SaveDataValidator.CurrentSaveVersion;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(Current.playerName))
            {
                Current.playerName = "Player";
                changed = true;
            }

            if (Current.rankingRequestTimeoutSeconds <= 0)
            {
                Current.rankingRequestTimeoutSeconds = 8;
                changed = true;
            }

            if (Current.rankingApiBaseUrl == null)
            {
                Current.rankingApiBaseUrl = string.Empty;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(Current.playerId))
            {
                Current.playerId = Guid.NewGuid().ToString();
                changed = true;
            }
            if (changed)
            {
                Save();
            }

            return Current;
        }

        public void Save()
        {
            SaveService.SaveJson(FileName, Current);
        }

        public void Update(AppSettings settings)
        {
            Current = settings ?? AppSettings.CreateDefault();
            Save();
        }
    }
}
