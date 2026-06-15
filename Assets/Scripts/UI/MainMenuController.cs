using CubeChallenge3D.Core;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Learn;
using CubeChallenge3D.UI.Records;
using CubeChallenge3D.UI.Rewards;
using CubeChallenge3D.UI.Settings;
using CubeChallenge3D.UI.Shop;
using CubeChallenge3D.UI.Solver;
using CubeChallenge3D.UI.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private ModalPanel comingSoonPanel;
        private SettingsPanelUI settingsPanel;
        private StageListPanelUI stageListPanel;
        private ShopPanelUI shopPanel;
        private RewardsPanelUI rewardsPanel;
        private SolverPanelUI solverPanel;
        private LearnModeHubView learnHub;
        private RecordsPanelUI recordsPanel;
        private Canvas mainMenuCanvas;
        private GraphicRaycaster mainMenuRaycaster;

        public void Initialize(
            ModalPanel modal,
            SettingsPanelUI settings,
            StageListPanelUI stages,
            ShopPanelUI shop,
            RewardsPanelUI rewards,
            SolverPanelUI solver,
            LearnModeHubView hub,
            RecordsPanelUI records)
        {
            comingSoonPanel = modal;
            settingsPanel = settings;
            stageListPanel = stages;
            shopPanel = shop;
            rewardsPanel = rewards;
            solverPanel = solver;
            learnHub = hub;
            recordsPanel = records;
        }

        public void SetMainMenuCanvas(Canvas canvas)
        {
            mainMenuCanvas = canvas;
            mainMenuRaycaster = canvas != null ? canvas.GetComponent<GraphicRaycaster>() : null;
        }

        private void Start()
        {
            GameManager.Instance?.SetState(AppState.MainMenu);
        }

        private void LateUpdate()
        {
            if (mainMenuCanvas == null)
            {
                return;
            }

            bool overlayOpen = false;
            foreach (Transform child in transform)
            {
                if (child == mainMenuCanvas.transform)
                {
                    continue;
                }

                Canvas childCanvas = child.GetComponent<Canvas>();
                if (childCanvas != null && child.gameObject.activeInHierarchy)
                {
                    overlayOpen = true;
                    break;
                }
            }

            mainMenuCanvas.enabled = !overlayOpen;
            if (mainMenuRaycaster != null)
            {
                mainMenuRaycaster.enabled = !overlayOpen;
            }
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
            stageListPanel?.Show();
        }

        public void Stages(string focusStageId)
        {
            stageListPanel?.Show(focusStageId);
        }

        public void SolverLearn()
        {
            learnHub?.Show();
        }

        public void Records()
        {
            recordsPanel?.Show();
        }

        public void Shop()
        {
            shopPanel?.Show();
        }

        public void Rewards()
        {
            rewardsPanel?.Show();
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
            Button rankingChallenge,
            Button stages,
            Button solverLearn,
            Button records,
            Button shop,
            Button rewards,
            Button settings)
        {
            SetButtonLabel(rankingChallenge, "ranking_challenge");
            SetButtonLabel(stages, "stages");
            SetButtonLabel(solverLearn, "solver_learn");
            SetButtonLabel(records, "records");
            SetButtonLabel(shop, "shop");
            SetButtonLabel(rewards, "rewards");
            SetButtonLabel(settings, "settings");
        }

        private static void SetButtonLabel(Button button, string key)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(key) : key;
            }
        }
    }
}
