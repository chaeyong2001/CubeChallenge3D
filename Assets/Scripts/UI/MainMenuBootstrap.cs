using CubeChallenge3D.Core;
using CubeChallenge3D.Save;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Settings;
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
            CreateBackground(canvas.transform);
            RectTransform menuPanel = CreateMenuPanel(canvas.transform);
            VerticalLayoutGroup layout = CreateButtonLayout(menuPanel);

            Button quickPlay = CreateButton(layout.transform, "quick_play", controller.Play);
            Button ranking = CreateButton(layout.transform, "ranking_challenge", controller.RankingChallenge);
            Button stages = CreateButton(layout.transform, "stages", controller.Stages);
            Button solverLearn = CreateButton(layout.transform, "solver_learn", controller.SolverLearn);
            Button records = CreateButton(layout.transform, "records", controller.Records);
            Button settings = CreateButton(layout.transform, "settings", controller.Settings);

            ModalPanel comingSoon = new ModalPanel(root.transform, "MainMenuModalCanvas");
            SettingsPanelUI settingsPanel = root.AddComponent<SettingsPanelUI>();
            settingsPanel.Initialize(new SettingsStore(), null, null);

            controller.Initialize(comingSoon, settingsPanel);
            controller.ApplyLocalizedLabels(quickPlay, ranking, stages, solverLearn, records, settings);
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
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.96f);
        }

        private static RectTransform CreateMenuPanel(Transform parent)
        {
            GameObject panelObject = new("MenuPanel");
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(620f, 760f);

            Image image = panelObject.AddComponent<Image>();
            image.color = new Color(0.03f, 0.04f, 0.055f, 0.98f);
            return rect;
        }

        private static VerticalLayoutGroup CreateButtonLayout(Transform parent)
        {
            GameObject layoutObject = new("MenuButtons");
            layoutObject.transform.SetParent(parent, false);

            RectTransform rect = layoutObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(500f, 620f);

            VerticalLayoutGroup layout = layoutObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
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
            layout.preferredHeight = 72f;

            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);

            Text label = labelObject.AddComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(key) : key;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

    }
}
