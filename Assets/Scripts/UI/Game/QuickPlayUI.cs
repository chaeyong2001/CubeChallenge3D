using CubeChallenge3D.GameModes.QuickPlay;
using CubeChallenge3D.Cube.Debugging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Game
{
    public sealed class QuickPlayUI : MonoBehaviour
    {
        [SerializeField] private QuickPlayGameMode gameMode;
        [SerializeField] private GameMobileInteractionUI mobileInteractionUi;
        [SerializeField] private CubeRuntimeDiagnostics diagnostics;
        [SerializeField] private bool hideOtherPanelsWhileResult = true;
        [SerializeField] private bool hideDiagnosticsWhileResult = true;
        [SerializeField] private bool hideQuickPlayPanelsWhileResult = true;

        [Header("Adjustable Layout")]
        [SerializeField] private Vector2 hudPosition = new Vector2(0f, -40f);
        [SerializeField] private Vector2 hudSize = new Vector2(560f, 310f);
        [SerializeField] private Vector2 hudMinSize = new Vector2(420f, 250f);
        [SerializeField] private Vector2 hudMaxSize = new Vector2(760f, 420f);
        [SerializeField] private Vector2 actionsPosition = new Vector2(35f, 40f);
        [SerializeField] private Vector2 actionsSize = new Vector2(270f, 280f);
        [SerializeField] private Vector2 resultPosition = Vector2.zero;
        [SerializeField] private Vector2 resultSize = new Vector2(560f, 360f);
        [SerializeField] private Vector2 resultMinSize = new Vector2(440f, 300f);
        [SerializeField] private Vector2 resultMaxSize = new Vector2(900f, 720f);

        private Text statusText;
        private Text timerText;
        private Text moveText;
        private Text bestText;
        private GameObject hudPanel;
        private GameObject actionsPanel;
        private GameObject resultPanel;
        private Text resultText;
        private Button startButton;
        private Button retryButton;
        private Button resetButton;
        private RectTransform resultRect;
        private bool lastResultVisible;

        public void Initialize(QuickPlayGameMode mode)
        {
            Initialize(mode, null, null);
        }

        public void Initialize(
            QuickPlayGameMode mode,
            GameMobileInteractionUI interactionUi,
            CubeRuntimeDiagnostics runtimeDiagnostics)
        {
            gameMode = mode;
            mobileInteractionUi = interactionUi;
            diagnostics = runtimeDiagnostics;
            BuildUi();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "QuickPlayCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform hud = CreatePanel(canvasObject.transform, "QuickPlayHUD", new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), hudPosition, hudSize);
            hudPanel = hud.gameObject;
            CreateWindowBar(hud, "Status", hudMinSize, hudMaxSize, true, true);
            statusText = CreateText(hud, "StatusValue", new Vector2(0f, -60f), 34);
            timerText = CreateText(hud, "Timer", new Vector2(0f, -112f), 42);
            moveText = CreateText(hud, "Moves", new Vector2(0f, -170f), 30);
            bestText = CreateText(hud, "Best", new Vector2(0f, -214f), 24);

            RectTransform actions = CreatePanel(canvasObject.transform, "QuickPlayActions", new Vector2(0f, 0f),
                new Vector2(0f, 0f), actionsPosition, actionsSize);
            actionsPanel = actions.gameObject;
            startButton = CreateButton(actions, "StartButton", "New Game", new Vector2(0f, 180f));
            retryButton = CreateButton(actions, "RetryButton", "Retry", new Vector2(0f, 90f));
            resetButton = CreateButton(actions, "ResetButton", "Reset", Vector2.zero);
            startButton.onClick.AddListener(() => gameMode?.StartNewGame());
            retryButton.onClick.AddListener(() => gameMode?.Retry());
            resetButton.onClick.AddListener(() => gameMode?.ResetToReady());

            RectTransform result = CreatePanel(canvasObject.transform, "QuickPlayResult", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), resultPosition, resultSize);
            resultRect = result;
            resultPanel = result.gameObject;
            resultPanel.transform.SetAsLastSibling();
            CreateWindowBar(result, "Result", resultMinSize, resultMaxSize, true, true);
            resultText = CreateText(result, "ResultText", new Vector2(0f, -78f), 32);
            resultText.rectTransform.sizeDelta = new Vector2(0f, 112f);
            Button resultRetry = CreateCenteredButton(result, "ResultRetry", "Retry", new Vector2(0f, 96f), 320f);
            Button resultNew = CreateCenteredButton(result, "ResultNew", "New Game", new Vector2(0f, 16f), 320f);
            resultRetry.onClick.AddListener(() => gameMode?.Retry());
            resultNew.onClick.AddListener(() => gameMode?.StartNewGame());
        }

        private void Refresh()
        {
            if (gameMode == null || statusText == null)
            {
                return;
            }

            statusText.text = gameMode.State.ToString();
            timerText.text = $"Time  {FormatTime(gameMode.ElapsedTime)}";
            moveText.text = $"Moves  {gameMode.MoveCount}";
            bestText.text = BuildBestSummary();
            bool busy = gameMode.State == QuickPlayState.Scrambling;
            startButton.interactable = !busy;
            retryButton.interactable = !busy && gameMode.ActiveScramble.Count > 0;
            resetButton.interactable = !busy;
            bool resultVisible = gameMode.State == QuickPlayState.Solved;
            if (hideQuickPlayPanelsWhileResult)
            {
                hudPanel.SetActive(!resultVisible);
                actionsPanel.SetActive(!resultVisible);
            }

            resultPanel.SetActive(resultVisible);
            UpdateResultOverlayVisibility(resultVisible);
            resultSize = resultRect != null ? resultRect.sizeDelta : resultSize;
            resultText.text = $"Clear!\n\nTime  {FormatTime(gameMode.ElapsedTime)}\nMoves  {gameMode.MoveCount}";
        }

        private string BuildBestSummary()
        {
            if (gameMode.RecordStore == null || gameMode.RecordStore.Count == 0)
            {
                return "Best Time  -    Best Moves  -";
            }

            var bestTime = gameMode.RecordStore.GetBestByTime();
            var bestMoves = gameMode.RecordStore.GetBestByMoves();
            string timeText = bestTime != null ? FormatTime(bestTime.elapsedSeconds) : "-";
            string movesText = bestMoves != null ? bestMoves.moveCount.ToString() : "-";
            return $"Best Time  {timeText}    Best Moves  {movesText}";
        }

        private void UpdateResultOverlayVisibility(bool resultVisible)
        {
            if (resultVisible == lastResultVisible)
            {
                return;
            }

            lastResultVisible = resultVisible;
            if (hideOtherPanelsWhileResult)
            {
                mobileInteractionUi?.SetVisible(!resultVisible);
            }

            if (hideDiagnosticsWhileResult)
            {
                diagnostics?.SetDebugPanelVisible(!resultVisible);
            }
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remaining = seconds - (minutes * 60f);
            return $"{minutes:00}:{remaining:00.00}";
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.9f);
            return rect;
        }

        private static Text CreateText(RectTransform parent, string name, Vector2 position, int fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(0f, 90f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 position)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(0f, 72f);
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.2f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            Text text = CreateText(rect, "Label", Vector2.zero, 28);
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            return button;
        }

        private static Button CreateCenteredButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 position,
            float width)
        {
            Button button = CreateButton(parent, name, label, position);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(width, 72f);
            return button;
        }

        private static void CreateResizeHandle(RectTransform parent, Vector2 minSize, Vector2 maxSize)
        {
            GameObject handleObject = new GameObject("ResizeHandle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(parent, false);
            RectTransform rect = handleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-12f, 12f);
            rect.sizeDelta = new Vector2(44f, 44f);

            Image image = handleObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.35f);

            PanelResizeHandle handle = handleObject.AddComponent<PanelResizeHandle>();
            handle.Initialize(parent, minSize, maxSize);
        }

        private static void CreateWindowBar(
            RectTransform parent,
            string titleText,
            Vector2 minSize,
            Vector2 maxSize,
            bool draggable,
            bool resizable)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 52f);

            Image image = barObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.11f, 0.14f, 0.95f);

            Text title = CreateText(rect, "Title", Vector2.zero, 24);
            title.text = titleText;
            title.alignment = TextAnchor.MiddleCenter;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = Vector2.zero;

            if (draggable)
            {
                PanelDragHandle handle = barObject.AddComponent<PanelDragHandle>();
                handle.Initialize(parent);
            }

            if (resizable)
            {
                CreateResizeHandle(parent, minSize, maxSize);
            }
        }

        private sealed class PanelDragHandle : MonoBehaviour, IDragHandler
        {
            private RectTransform target;
            private Canvas canvas;

            public void Initialize(RectTransform dragTarget)
            {
                target = dragTarget;
                canvas = dragTarget.GetComponentInParent<Canvas>();
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (target == null)
                {
                    return;
                }

                float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                target.anchoredPosition += eventData.delta / scale;
            }
        }

        private sealed class PanelResizeHandle : MonoBehaviour, IDragHandler
        {
            private RectTransform target;
            private Vector2 minSize;
            private Vector2 maxSize;

            public void Initialize(RectTransform resizeTarget, Vector2 minimumSize, Vector2 maximumSize)
            {
                target = resizeTarget;
                minSize = minimumSize;
                maxSize = maximumSize;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (target == null)
                {
                    return;
                }

                Vector2 nextSize = target.sizeDelta + new Vector2(eventData.delta.x, -eventData.delta.y);
                nextSize.x = Mathf.Clamp(nextSize.x, minSize.x, maxSize.x);
                nextSize.y = Mathf.Clamp(nextSize.y, minSize.y, maxSize.y);
                target.sizeDelta = nextSize;
            }
        }
    }
}
