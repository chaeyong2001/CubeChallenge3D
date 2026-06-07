using CubeChallenge3D.Core;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private ModalPanel comingSoonPanel;
        private SettingsPanelUI settingsPanel;

        public void Initialize(ModalPanel modal, SettingsPanelUI settings)
        {
            comingSoonPanel = modal;
            settingsPanel = settings;
        }

        private void Start()
        {
            GameManager.Instance?.SetState(AppState.MainMenu);
        }

        public void Play()
        {
            GameLaunchContext.SetMode(GameLaunchMode.QuickPlay);
            SceneLoader.LoadGame();
        }

        public void RankingChallenge()
        {
            GameLaunchContext.SetMode(GameLaunchMode.RankingChallenge);
            SceneLoader.LoadGame();
        }

        public void Stages()
        {
            ShowComingSoon("Stages", "Solve Stage and Target Stage.");
        }

        public void SolverLearn()
        {
            ShowComingSoon("Solver / Learn", "Learn basics and manual solver.");
        }

        public void Records()
        {
            ShowComingSoon("Records", "Recent records screen is coming soon.");
        }

        public void Settings()
        {
            GameManager.Instance?.SetState(AppState.Settings);
            settingsPanel?.Show();
        }

        public void ShowComingSoon(string title, string body)
        {
            comingSoonPanel?.Show("Coming Soon", $"{title}\n\n{body}");
        }

        public void ApplyLocalizedLabels(
            Button quickPlay,
            Button rankingChallenge,
            Button stages,
            Button solverLearn,
            Button records,
            Button settings)
        {
            SetButtonLabel(quickPlay, "quick_play");
            SetButtonLabel(rankingChallenge, "ranking_challenge");
            SetButtonLabel(stages, "stages");
            SetButtonLabel(solverLearn, "solver_learn");
            SetButtonLabel(records, "records");
            SetButtonLabel(settings, "settings");
        }

        private static void SetButtonLabel(Button button, string key)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(key) : key;
            }
        }
    }
}
