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
            if (Current == null)
            {
                Current = AppSettings.CreateDefault();
            }

            if (string.IsNullOrWhiteSpace(Current.playerName))
            {
                Current.playerName = "Player";
            }

            if (string.IsNullOrWhiteSpace(Current.playerId))
            {
                Current.playerId = Guid.NewGuid().ToString();
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
