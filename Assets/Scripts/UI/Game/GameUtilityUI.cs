using CubeChallenge3D.Core;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Game
{
    public sealed class GameUtilityUI : MonoBehaviour
    {
        private CubeController cubeController;
        private SettingsPanelUI settingsPanel;
        private ModalPanel helpPanel;
        private Button undoButton;
        private Canvas canvas;

        public void Initialize(CubeController controller, SettingsPanelUI panel)
        {
            cubeController = controller;
            settingsPanel = panel;
            BuildUi();
        }

        private void BuildUi()
        {
            if (canvas != null)
            {
                return;
            }

            canvas = RuntimeUiFactory.CreateCanvas(transform, "GameUtilityCanvas", 700);
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "UtilityButtons",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -72f),
                new Vector2(260f, 360f));

            Button home = RuntimeUiFactory.CreateButton(panel, "HomeButton", "Home", new Vector2(0f, 264f), new Vector2(220f, 64f));
            undoButton = RuntimeUiFactory.CreateButton(panel, "UndoButton", "\u21B6  Undo", new Vector2(0f, 188f), new Vector2(220f, 64f));
            Button help = RuntimeUiFactory.CreateButton(panel, "HelpButton", "Help", new Vector2(0f, 112f), new Vector2(220f, 64f));
            Button settings = RuntimeUiFactory.CreateButton(panel, "SettingsButton", "Settings", new Vector2(0f, 36f), new Vector2(220f, 64f));

            home.onClick.AddListener(SceneLoader.LoadMainMenu);
            undoButton.onClick.AddListener(() => cubeController?.Undo());
            help.onClick.AddListener(ShowHelp);
            settings.onClick.AddListener(() => settingsPanel?.Show());
            helpPanel = new ModalPanel(transform, "HelpCanvas");
        }

        public void SetUndoEnabled(bool enabled)
        {
            if (undoButton != null)
            {
                undoButton.interactable = enabled;
            }
        }

        private void ShowHelp()
        {
            helpPanel.Show(
                "Help",
                "View: Rotate the cube to look around.\n\n"
                + "Solve: Align the cube and make moves.\n\n"
                + "Drag: Swipe a row or column to turn it.\n\n"
                + "Keypad: Use buttons to turn rows.\n\n"
                + "Undo: Revert last move.");
        }
    }
}
