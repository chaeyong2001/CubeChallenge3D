using CubeChallenge3D.Cube.Debugging;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Save;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Settings
{
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        private SettingsStore settingsStore;
        private CubeControlModeController controlModeController;
        private CubeRuntimeDiagnostics diagnostics;
        private GameObject root;
        private Text debugLabel;
        private Text soundLabel;
        private Text vibrationLabel;
        private Text controlLabel;

        public void Initialize(
            SettingsStore store,
            CubeControlModeController controlController,
            CubeRuntimeDiagnostics runtimeDiagnostics)
        {
            settingsStore = store;
            controlModeController = controlController;
            diagnostics = runtimeDiagnostics;
            BuildUi();
            RefreshLabels();
        }

        public void Show()
        {
            if (root != null)
            {
                RefreshLabels();
                root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void BuildUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = RuntimeUiFactory.CreateCanvas(transform, "SettingsCanvas", 1550);
            root = canvas.gameObject;
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "SettingsPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 760f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Settings", 40, TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(20f, 660f);
            title.rectTransform.offsetMax = new Vector2(-20f, -24f);

            controlLabel = AddSettingButton(panel, "ControlMode", new Vector2(0f, 560f), ToggleControlMode);
            debugLabel = AddSettingButton(panel, "Debug", new Vector2(0f, 460f), ToggleDebug);
            soundLabel = AddSettingButton(panel, "Sound", new Vector2(0f, 360f), ToggleSound);
            vibrationLabel = AddSettingButton(panel, "Vibration", new Vector2(0f, 260f), ToggleVibration);
            AddStaticLabel(panel, "Language: English", new Vector2(0f, 170f));
            AddStaticLabel(panel, $"Version {Application.version}", new Vector2(0f, 110f));

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (debugLabel != null)
            {
                debugLabel.transform.parent.gameObject.SetActive(false);
            }
#endif

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Back", new Vector2(0f, 28f), new Vector2(340f, 68f));
            close.onClick.AddListener(Hide);
            Hide();
        }

        private Text AddSettingButton(RectTransform parent, string name, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            Button button = RuntimeUiFactory.CreateButton(parent, name, string.Empty, position, new Vector2(520f, 72f));
            button.onClick.AddListener(action);
            return button.GetComponentInChildren<Text>();
        }

        private static void AddStaticLabel(RectTransform parent, string label, Vector2 position)
        {
            GameObject row = new GameObject("LanguageRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(520f, 60f);
            RuntimeUiFactory.CreateText(rect, "Label", label, 28, TextAnchor.MiddleCenter);
        }

        private void ToggleControlMode()
        {
            if (settingsStore?.Current == null)
            {
                return;
            }

            if (controlModeController != null)
            {
                if (controlModeController.CurrentControlMode == CubeControlMode.Drag)
                {
                    controlModeController.SetKeypadControlMode();
                }
                else
                {
                    controlModeController.SetDragControlMode();
                }
            }
            else
            {
                settingsStore.Current.controlMode = settingsStore.Current.controlMode == CubeControlMode.Drag.ToString()
                    ? CubeControlMode.Keypad.ToString()
                    : CubeControlMode.Drag.ToString();
                settingsStore.Save();
            }

            RefreshLabels();
        }

        private void ToggleDebug()
        {
            if (settingsStore?.Current == null)
            {
                return;
            }

            settingsStore.Current.showDebugPanel = !settingsStore.Current.showDebugPanel;
            settingsStore.Save();
            diagnostics?.SetDebugPanelVisible(settingsStore.Current.showDebugPanel);
            RefreshLabels();
        }

        private void ToggleSound()
        {
            if (settingsStore?.Current == null)
            {
                return;
            }

            settingsStore.Current.soundEnabled = !settingsStore.Current.soundEnabled;
            settingsStore.Save();
            RefreshLabels();
        }

        private void ToggleVibration()
        {
            if (settingsStore?.Current == null)
            {
                return;
            }

            settingsStore.Current.vibrationEnabled = !settingsStore.Current.vibrationEnabled;
            settingsStore.Save();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (settingsStore?.Current == null)
            {
                return;
            }

            string controlMode = controlModeController != null
                ? controlModeController.CurrentControlMode.ToString()
                : settingsStore.Current.controlMode;
            controlLabel.text = $"Control Mode: {controlMode}";
            debugLabel.text = $"Show Debug Panel: {(settingsStore.Current.showDebugPanel ? "On" : "Off")}";
            soundLabel.text = $"Sound: {(settingsStore.Current.soundEnabled ? "On" : "Off")}";
            vibrationLabel.text = $"Vibration: {(settingsStore.Current.vibrationEnabled ? "On" : "Off")}";
        }
    }
}
