using System.Collections.Generic;
using UnityEngine;

namespace CubeChallenge3D.Core
{
    public enum AppLanguage
    {
        English,
        Korean
    }

    public sealed class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

        private readonly Dictionary<AppLanguage, Dictionary<string, string>> localizedText = new()
        {
            {
                AppLanguage.English,
                new Dictionary<string, string>
                {
                    { "play", "Play" },
                    { "quick_play", "Quick Play" },
                    { "ranking_challenge", "Ranking Challenge" },
                    { "stages", "Stages" },
                    { "target", "Target" },
                    { "solver", "Solver" },
                    { "solver_learn", "Solver / Learn" },
                    { "records", "Records" },
                    { "ranking", "Ranking" },
                    { "settings", "Settings" },
                    { "back", "Back" },
                    { "home", "Home" },
                    { "retry", "Retry" },
                    { "new_game", "New Game" },
                    { "reset", "Reset" },
                    { "time", "Time" },
                    { "moves", "Moves" },
                    { "continue", "Continue" },
                    { "hint", "Hint" },
                    { "view", "View" },
                    { "solve", "Solve" },
                    { "drag", "Drag" },
                    { "keypad", "Keypad" },
                    { "undo", "Undo" },
                    { "help", "Help" },
                    { "close", "Close" },
                    { "show_debug", "Show Debug Panel" },
                    { "sound", "Sound" },
                    { "vibration", "Vibration" },
                    { "language", "Language" },
                    { "coming_soon", "Coming Soon" }
                }
            },
            {
                AppLanguage.Korean,
                new Dictionary<string, string>()
            }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetLanguage(AppLanguage language)
        {
            CurrentLanguage = language;
        }

        public string GetText(string key)
        {
            if (localizedText.TryGetValue(CurrentLanguage, out Dictionary<string, string> languageTable) &&
                languageTable.TryGetValue(key, out string value))
            {
                return value;
            }

            if (localizedText[AppLanguage.English].TryGetValue(key, out string fallback))
            {
                return fallback;
            }

            return key;
        }
    }
}
