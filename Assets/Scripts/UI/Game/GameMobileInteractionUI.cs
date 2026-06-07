using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Game
{
    public sealed class GameMobileInteractionUI : MonoBehaviour
    {
        [SerializeField] private CubeViewOrbitController orbitController;
        [SerializeField] private CubeControlModeController controlModeController;
        [SerializeField] private CubeController cubeController;
        [SerializeField] private bool allowMiddleLayerControls;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private float bottomMargin = 90f;
        [SerializeField] private float sideMargin = 40f;

        private Canvas canvas;
        private Button viewButton;
        private Button solveButton;
        private Button resetViewButton;
        private Button dragModeButton;
        private Button keypadModeButton;
        private GameObject keypadPanel;
        private readonly System.Collections.Generic.List<Button> keypadButtons =
            new System.Collections.Generic.List<Button>();

        public void Initialize(
            CubeViewOrbitController viewOrbitController,
            CubeControlModeController controlController,
            CubeController controller)
        {
            orbitController = viewOrbitController;
            controlModeController = controlController;
            cubeController = controller;
            EnsureEventSystem();
            BuildUi();
        }

        public void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.enabled = visible;
            }
        }

        private void Update()
        {
            if (viewButton == null || orbitController == null)
            {
                return;
            }

            bool busy = orbitController.IsSnapping
                || (cubeController != null && (cubeController.IsBusy || !cubeController.UserInputEnabled));
            viewButton.interactable = !busy;
            solveButton.interactable = !busy;
            resetViewButton.interactable = !busy;
            dragModeButton.interactable = !busy;
            keypadModeButton.interactable = !busy;
            foreach (Button button in keypadButtons)
            {
                button.interactable = !busy;
            }

            bool showKeypad = orbitController.CurrentMode == CubeInteractionMode.Solve
                && controlModeController != null
                && controlModeController.CurrentControlMode == CubeControlMode.Keypad;
            if (keypadPanel != null && keypadPanel.activeSelf != showKeypad)
            {
                keypadPanel.SetActive(showKeypad);
            }
        }

        private void BuildUi()
        {
            if (canvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "GameMobileInteractionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject controls = new GameObject("ModeControls", typeof(RectTransform));
            controls.transform.SetParent(canvasObject.transform, false);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(1f, 0f);
            controlsRect.anchorMax = new Vector2(1f, 0f);
            controlsRect.pivot = new Vector2(1f, 0f);
            controlsRect.anchoredPosition = new Vector2(-sideMargin, bottomMargin);
            controlsRect.sizeDelta = new Vector2(280f, 450f);

            viewButton = CreateButton(controlsRect, "ViewButton", "View", new Vector2(0f, 360f));
            solveButton = CreateButton(controlsRect, "SolveButton", "Solve", new Vector2(0f, 270f));
            resetViewButton = CreateButton(controlsRect, "ResetViewButton", "Reset View", new Vector2(0f, 180f));
            dragModeButton = CreateButton(controlsRect, "DragModeButton", "Drag", new Vector2(0f, 90f));
            keypadModeButton = CreateButton(controlsRect, "KeypadModeButton", "Keypad", Vector2.zero);

            viewButton.onClick.AddListener(() => orbitController?.SetViewMode());
            solveButton.onClick.AddListener(() => orbitController?.SetSolveMode());
            resetViewButton.onClick.AddListener(() => orbitController?.ResetViewRotation());
            dragModeButton.onClick.AddListener(() => controlModeController?.SetDragControlMode());
            keypadModeButton.onClick.AddListener(() => controlModeController?.SetKeypadControlMode());
            BuildKeypad(canvasObject.transform);
        }

        private void BuildKeypad(Transform parent)
        {
            keypadPanel = new GameObject("KeypadPanel", typeof(RectTransform));
            keypadPanel.transform.SetParent(parent, false);
            RectTransform panelRect = keypadPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, bottomMargin);
            panelRect.sizeDelta = new Vector2(620f, 340f);

            AddKeypadButton(panelRect, "TopLeft", "Top <", new Vector2(-155f, 255f), CubeFace.Up, -1);
            AddKeypadButton(panelRect, "TopRight", "Top >", new Vector2(155f, 255f), CubeFace.Up, 1);
            AddKeypadButton(panelRect, "BottomLeft", "Bot <", new Vector2(-155f, 170f), CubeFace.Down, 1);
            AddKeypadButton(panelRect, "BottomRight", "Bot >", new Vector2(155f, 170f), CubeFace.Down, -1);
            AddKeypadButton(panelRect, "LeftUp", "L Up", new Vector2(-155f, 85f), CubeFace.Left, -1);
            AddKeypadButton(panelRect, "LeftDown", "L Down", new Vector2(-155f, 0f), CubeFace.Left, 1);
            AddKeypadButton(panelRect, "RightUp", "R Up", new Vector2(155f, 85f), CubeFace.Right, 1);
            AddKeypadButton(panelRect, "RightDown", "R Down", new Vector2(155f, 0f), CubeFace.Right, -1);

            // Reserved for future M/E/S support.
            _ = allowMiddleLayerControls;
            keypadPanel.SetActive(false);
        }

        private void AddKeypadButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 position,
            CubeFace face,
            int turns)
        {
            Button button = CreateFixedButton(parent, name, label, position);
            button.onClick.AddListener(() => ApplyKeypadMove(face, turns));
            keypadButtons.Add(button);
        }

        private void ApplyKeypadMove(CubeFace face, int turns)
        {
            if (cubeController == null
                || orbitController == null
                || controlModeController == null
                || cubeController.IsBusy
                || !cubeController.UserInputEnabled
                || orbitController.CurrentMode != CubeInteractionMode.Solve
                || controlModeController.CurrentControlMode != CubeControlMode.Keypad)
            {
                return;
            }

            cubeController.ApplyUserMove(new CubeMove(face, turns));
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 position)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(0f, 72f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.17f, 0.92f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.22f, 0.28f, 0.34f, 1f);
            colors.pressedColor = new Color(0.08f, 0.55f, 0.75f, 1f);
            button.colors = colors;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return button;
        }

        private static Button CreateFixedButton(RectTransform parent, string name, string label, Vector2 position)
        {
            Button button = CreateButton(parent, name, label, position);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(280f, 68f);
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }
}
