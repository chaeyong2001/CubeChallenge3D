using CubeChallenge3D.Core;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Learn.Services;
using CubeChallenge3D.Learn.Storage;
using CubeChallenge3D.Save;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Learn;
using CubeChallenge3D.UI.Records;
using CubeChallenge3D.UI.Rewards;
using CubeChallenge3D.UI.Settings;
using CubeChallenge3D.UI.Shop;
using CubeChallenge3D.UI.Solver;
using CubeChallenge3D.UI.Stages;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeChallenge3D.UI
{
    public static class MainMenuBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryBuildMainMenu(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBuildMainMenu(scene);
        }

        private static void TryBuildMainMenu(Scene scene)
        {
            EnsurePersistentManagers();

            if (scene.name != SceneLoader.MainMenuScene || Object.FindFirstObjectByType<MainMenuController>() != null)
            {
                return;
            }

            GameObject root = new("MainMenu");
            MainMenuController controller = root.AddComponent<MainMenuController>();

            Canvas canvas = RuntimeUiFactory.CreateCanvas(root.transform, "Canvas", 100);
            controller.SetMainMenuCanvas(canvas);
            CreateBackground(canvas.transform);
            RectTransform safeArea = CreateSafeArea(canvas.transform);
            RectTransform menuPanel = CreateMenuPanel(safeArea);
            VerticalLayoutGroup layout = CreateButtonLayout(menuPanel);
            CreateHeader(menuPanel);

            Button stages = CreateButton(layout.transform, "stages", controller.Stages);
            Button ranking = CreateButton(layout.transform, "ranking_challenge", controller.RankingChallenge);
            Button solverLearn = CreateButton(layout.transform, "solver_learn", controller.SolverLearn);
            Button shop = CreateButton(layout.transform, "shop", controller.Shop);
            Button rewards = CreateButton(layout.transform, "rewards", controller.Rewards);
            Button records = CreateButton(layout.transform, "records", controller.Records);
            Button settings = CreateButton(layout.transform, "settings", controller.Settings);

            ModalPanel comingSoon = new ModalPanel(root.transform, "MainMenuModalCanvas");
            WalletStore walletStore = new WalletStore();
            InventoryStore inventoryStore = new InventoryStore();
            StageProgressStore progressStore = new StageProgressStore();
            StageMilestoneRewardStore milestoneStore = new StageMilestoneRewardStore();
            RewardedAdService rewardService = RewardedAdService.CreateDefault();
            StageListPanelUI stageListPanel = new StageListPanelUI(root.transform);
            ShopPanelUI shopPanel = new ShopPanelUI(root.transform, walletStore, inventoryStore, rewardService);
            stageListPanel.SetShopAction(shopPanel.Show);
            RewardsPanelUI rewardsPanel = new RewardsPanelUI(root.transform, walletStore, inventoryStore, progressStore, milestoneStore, rewardService);
            SolverPanelUI solverPanel = TryCreateSolverPanel(root.transform);
            LearnModeHubView learnHub = new LearnModeHubView(root.transform);
            LearnLessonBrowserUI learnBrowser = new LearnLessonBrowserUI(
                root.transform,
                new LearnContentProvider(),
                new LearnLessonProgressStore());
            RecordsPanelUI recordsPanel = new RecordsPanelUI(root.transform);
            SettingsPanelUI settingsPanel = root.AddComponent<SettingsPanelUI>();
            settingsPanel.Initialize(new SettingsStore(), null, null);

            learnHub.SetManualSolverAction(() =>
            {
                if (solverPanel != null)
                {
                    solverPanel.Show();
                }
                else
                {
                    comingSoon.Show("Solver unavailable", "The solver screen could not be loaded.");
                }
            });
            learnHub.SetCategoryAction(learnBrowser.ShowCategory);
            learnHub.SetPracticeAction(controller.Play);
            learnBrowser.Closed += learnHub.Show;
            if (solverPanel != null)
            {
                solverPanel.Closed += learnHub.Show;
            }
            controller.Initialize(
                comingSoon,
                settingsPanel,
                stageListPanel,
                shopPanel,
                rewardsPanel,
                solverPanel,
                learnHub,
                recordsPanel);
            controller.ApplyLocalizedLabels(ranking, stages, solverLearn, records, shop, rewards, settings);

            if (GameLaunchContext.ConsumeStageListOnMainMenuRequest())
            {
                controller.Stages(GameLaunchContext.StageId);
            }
            else if (GameLaunchContext.ConsumeShopOnMainMenuRequest())
            {
                controller.Shop();
            }
        }

        private static void EnsurePersistentManagers()
        {
            if (GameManager.Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }

            if (LocalizationManager.Instance == null)
            {
                new GameObject("LocalizationManager").AddComponent<LocalizationManager>();
            }
        }

        private static SolverPanelUI TryCreateSolverPanel(Transform parent)
        {
            try
            {
                return new SolverPanelUI(parent);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                Transform partialCanvas = parent.Find("SolverCanvas");
                if (partialCanvas != null)
                {
                    Object.Destroy(partialCanvas.gameObject);
                }
                return null;
            }
        }

        private static void CreateBackground(Transform parent)
        {
            GameObject backgroundObject = new("Background");
            backgroundObject.transform.SetParent(parent, false);
            RectTransform rect = backgroundObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = backgroundObject.AddComponent<Image>();
            Color themeColor = VisualCustomizationService.LoadSelectedTheme().backgroundColor;
            image.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.96f);
        }

        private static RectTransform CreateSafeArea(Transform parent)
        {
            GameObject safeObject = new("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(parent, false);
            return safeObject.GetComponent<RectTransform>();
        }

        private static void CreateHeader(RectTransform parent)
        {
            Text title = RuntimeUiFactory.CreateText(parent, "AppTitle", "Cube Challenge 3D", 44, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -92f);
            title.rectTransform.sizeDelta = new Vector2(-60f, 70f);

            Text subtitle = RuntimeUiFactory.CreateText(parent, "AppSubtitle", "Play. Learn. Improve.", 22, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.72f, 0.8f, 0.84f, 1f);
            subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -158f);
            subtitle.rectTransform.sizeDelta = new Vector2(-60f, 38f);
        }

        private static RectTransform CreateMenuPanel(Transform parent)
        {
            GameObject panelObject = new("MenuPanel");
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = panelObject.AddComponent<Image>();
            image.color = new Color(0.03f, 0.04f, 0.055f, 0.94f);
            return rect;
        }

        private static VerticalLayoutGroup CreateButtonLayout(Transform parent)
        {
            GameObject layoutObject = new("MenuButtons");
            layoutObject.transform.SetParent(parent, false);

            RectTransform rect = layoutObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.07f, 0.055f);
            rect.anchorMax = new Vector2(0.93f, 0.79f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = layoutObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return layout;
        }

        private static Button CreateButton(Transform parent, string key, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new($"{key}Button");
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.13f, 0.18f, 0.24f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(action);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 128f;

            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);

            Text label = labelObject.AddComponent<Text>();
            label.alignment = TextAnchor.UpperCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 25;
            label.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(key) : key;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 30f);
            labelRect.offsetMax = new Vector2(-12f, -10f);

            Text subtitle = RuntimeUiFactory.CreateText(
                buttonObject.GetComponent<RectTransform>(),
                "Subtitle",
                GetMenuSubtitle(key),
                15,
                TextAnchor.LowerCenter);
            subtitle.color = new Color(0.72f, 0.8f, 0.84f, 1f);
            subtitle.rectTransform.offsetMin = new Vector2(16f, 7f);
            subtitle.rectTransform.offsetMax = new Vector2(-16f, -38f);

            return button;
        }

        private static string GetMenuSubtitle(string key)
        {
            switch (key)
            {
                case "stages":
                    return "Clear puzzles and earn rewards.";
                case "ranking_challenge":
                    return "Compete for the best time.";
                case "solver_learn":
                    return "Solve, study, and practice.";
                case "shop":
                    return "Unlock items, skins, and themes.";
                case "rewards":
                    return "Claim daily rewards.";
                case "records":
                    return "Review your progress.";
                case "settings":
                    return "Adjust controls and preferences.";
                default:
                    return string.Empty;
            }
        }

    }
}
