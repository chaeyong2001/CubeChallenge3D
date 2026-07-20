using CubeChallenge3D.Save.Settings;
using System;
using UnityEngine;

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
                Current.rankingApiBaseUrl = AppSettings.ProductionRankingApiBaseUrl;
                changed = true;
            }

            changed |= ApplyProductionRankingSettings(Current);

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

        private static bool ApplyProductionRankingSettings(AppSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            if (!ShouldUseProductionRankingSettings(settings))
            {
                return false;
            }

            settings.useServerRanking = true;
            settings.rankingApiBaseUrl = AppSettings.ProductionRankingApiBaseUrl;
            return true;
        }

        private static bool ShouldUseProductionRankingSettings(AppSettings settings)
        {
            if (settings == null)
            {
                return true;
            }

            if (!settings.useServerRanking)
            {
                return true;
            }

            string url = settings.rankingApiBaseUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                return true;
            }

            return url.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Save()
        {
            SaveService.SaveJson(FileName, Current);
            Debug.Log($"[RankingServerConfig] useServerRanking={Current?.useServerRanking} rankingApiBaseUrl={Current?.rankingApiBaseUrl}");
        }

        public void Update(AppSettings settings)
        {
            Current = settings ?? AppSettings.CreateDefault();
            ApplyProductionRankingSettings(Current);
            Save();
        }
    }
}
