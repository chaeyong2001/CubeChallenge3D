using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Core;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Solver;
using CubeChallenge3D.Solver.Engine;
using CubeChallenge3D.Solver.Model;
using CubeChallenge3D.Solver.Services;
using CubeChallenge3D.Solver.Storage;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Solver.Playback;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Solver
{
    public sealed class SolverPanelUI
    {
        public event Action Closed;

        private readonly GameObject root;
        private readonly SolverInputStore store = new SolverInputStore();
        private readonly SolverSolutionStore solutionStore = new SolverSolutionStore();
        private readonly SolverUsageStore usageStore = new SolverUsageStore();
        private readonly InventoryStore inventoryStore = new InventoryStore();
        private readonly ISolverEngine solverEngine = SolverEngineProvider.GetEngine();
        private readonly List<Button> faceButtons = new List<Button>();
        private readonly List<Button> cellButtons = new List<Button>();
        private readonly List<Button> paletteButtons = new List<Button>();
        private readonly List<Button> manualActionButtons = new List<Button>();
        private readonly Canvas solverCanvas;
        private readonly GraphicRaycaster solverRaycaster;
        private readonly Text titleText;
        private readonly Text currentFaceText;
        private readonly Text instructionText;
        private readonly Text validationText;
        private readonly Text statusText;
        private readonly Text resultText;
        private readonly Text debugText;
        private readonly RectTransform titleRect;
        private readonly RectTransform subtitleRect;
        private readonly RectTransform backButtonRect;
        private readonly RectTransform contentPanelRect;
        private readonly RectTransform faceLabelRect;
        private readonly RectTransform cubeBoardRect;
        private readonly RectTransform previousButtonRect;
        private readonly RectTransform nextButtonRect;
        private readonly RectTransform resultBarRect;
        private readonly RectTransform helperTextRect;
        private readonly RectTransform validateButtonRect;
        private readonly RectTransform clearButtonRect;
        private readonly RectTransform solveButtonRect;
        private readonly RectTransform play3DButtonRect;
        private readonly RectTransform solverBackButtonRect;
        private readonly RectTransform manualInputRoot;
        private readonly RectTransform gridRoot;
        private readonly RectTransform paletteRoot;
        private readonly ManualSolverLayoutConfig layout = ManualSolverLayoutConfig.Default;
        private SolverInputCube3DView inputCube3DView;
        private SolverFaceGuideView faceGuideView;
        private SolverPlaybackPanelUI playbackPanel;
        private Button validateButton;
        private Button solveButton;
        private Button play3DButton;
        private Text solverTicketCountText;
        private Image solverTicketIconImage;
        private SolverInputState state;
        private SolverSolution lastSolution;
        private SolverRequest lastSolverRequest;
        private SolverResult lastSolverResult;
        private int currentDisplayIndex;
        private int currentFrontFaceIndex = 2;
        private Vector3Int currentFrontNormal = Vector3Int.forward;
        private Vector3Int currentUpNormal = Vector3Int.up;
        private bool learnMode;
        private bool isSolving;
        private bool validationAttemptedForCurrentInput;
        private bool validationPassedForCurrentInput;
        private bool solveAttemptedForCurrentInput;
        private bool solutionReadyForCurrentInput;

        private const bool ShowDebugPanel = false;
        private static readonly Color PanelNavy = new Color32(7, 21, 31, 255);
        private static readonly Color GoldLine = new Color(1f, 0.67f, 0.08f, 1f);
        private static readonly Color ResultBrown = new Color(0.55f, 0.30f, 0.05f, 0.96f);
        private static readonly string[] InternalFaceLabels = { "U", "R", "F", "D", "L", "B" };
        private static readonly string[] InternalFaceDisplayNames = { "Top", "Right", "Front", "Bottom", "Left", "Back" };
        private static readonly int[][] DiagnosticCornerFacelets =
        {
            new[] { 8, 9, 20 },   // URF
            new[] { 6, 18, 38 },  // UFL
            new[] { 0, 36, 47 },  // ULB
            new[] { 2, 45, 11 },  // UBR
            new[] { 29, 26, 15 }, // DFR
            new[] { 27, 44, 24 }, // DLF
            new[] { 33, 53, 42 }, // DBL
            new[] { 35, 17, 51 }  // DRB
        };
        private static readonly string[] DiagnosticCornerNames = { "URF", "UFL", "ULB", "UBR", "DFR", "DLF", "DBL", "DRB" };
        private static readonly string[] DiagnosticCornerPieces = { "FRU", "FLU", "BLU", "BRU", "DFR", "DFL", "BDL", "BDR" };
        private static readonly int[][] DiagnosticEdgeFacelets =
        {
            new[] { 5, 10 },  // UR
            new[] { 7, 19 },  // UF
            new[] { 3, 37 },  // UL
            new[] { 1, 46 },  // UB
            new[] { 32, 16 }, // DR
            new[] { 28, 25 }, // DF
            new[] { 30, 43 }, // DL
            new[] { 34, 52 }, // DB
            new[] { 23, 12 }, // FR
            new[] { 21, 41 }, // FL
            new[] { 50, 39 }, // BL
            new[] { 48, 14 }  // BR
        };
        private static readonly string[] DiagnosticEdgeNames = { "UR", "UF", "UL", "UB", "DR", "DF", "DL", "DB", "FR", "FL", "BL", "BR" };
        private static readonly string[] DiagnosticEdgePieces = { "RU", "FU", "LU", "BU", "DR", "DF", "DL", "BD", "FR", "FL", "BL", "BR" };
        // Logical order names the physical face the user is entering.
        // Visible order matches the current 3D orientation sequence that has been verified in-editor.
        private static readonly int[] SolverDisplayOrder = { 2, 4, 0, 5, 1, 3 };
        private static readonly int[] SolverVisibleOrder = { 2, 1, 0, 5, 4, 3 };
        private static readonly Vector3[] SolverNextTurns =
        {
            new Vector3(0f, -90f, 0f), // Front visible turn, logical Front -> Left
            new Vector3(90f, 0f, 0f), // logical Left -> Top
            new Vector3(0f, -90f, 0f), // logical Top -> Right
            new Vector3(90f, 0f, 0f), // logical Right -> Back
            new Vector3(0f, -90f, 0f) // logical Back -> Bottom
        };
        private static readonly CubeColor[] PaletteColors =
        {
            CubeColor.White,
            CubeColor.Yellow,
            CubeColor.Red,
            CubeColor.Orange,
            CubeColor.Blue,
            CubeColor.Green
        };

        private struct ManualSolverLayoutConfig
        {
            public Vector2 TitlePosition;
            public Vector2 TitleSize;
            public Vector2 SubtitlePosition;
            public Vector2 SubtitleSize;
            public Vector2 BackButtonPosition;
            public Vector2 BackButtonSize;
            public Vector2 ContentPosition;
            public Vector2 ContentSize;
            public Vector2 FaceLabelPosition;
            public Vector2 FaceLabelSize;
            public Vector2 CubeBoardPosition;
            public Vector2 CubeBoardSize;
            public Vector2 PreviousButtonPosition;
            public Vector2 NextButtonPosition;
            public Vector2 FaceButtonSize;
            public Vector2 ResultBarPosition;
            public Vector2 ResultBarSize;
            public Vector2 PalettePosition;
            public Vector2 PaletteButtonSize;
            public float PaletteGapX;
            public float PaletteGapY;
            public Vector2 HelperTextPosition;
            public Vector2 HelperTextSize;
            public Vector2 ActionButtonSize;
            public Vector2 ValidateButtonPosition;
            public Vector2 ClearButtonPosition;
            public Vector2 SolveButtonPosition;
            public Vector2 Play3DButtonPosition;
            public Vector2 SolverBackButtonPosition;

            public static ManualSolverLayoutConfig Default => new ManualSolverLayoutConfig
            {
                TitlePosition = new Vector2(0f, -292f),
                TitleSize = new Vector2(800f, 92f),
                SubtitlePosition = new Vector2(0f, -382f),
                SubtitleSize = new Vector2(860f, 48f),
                BackButtonPosition = new Vector2(420f, -328f),
                BackButtonSize = new Vector2(92f, 84f),
                ContentPosition = new Vector2(0f, -265.2f),
                ContentSize = new Vector2(1040f, 1629.6f),
                FaceLabelPosition = new Vector2(0f, -42f),
                FaceLabelSize = new Vector2(320f, 48f),
                CubeBoardPosition = new Vector2(0f, -88f),
                CubeBoardSize = new Vector2(700f, 700f),
                PreviousButtonPosition = new Vector2(-435f, -440f),
                NextButtonPosition = new Vector2(435f, -440f),
                FaceButtonSize = new Vector2(165f, 76f),
                ResultBarPosition = new Vector2(0f, -835f),
                ResultBarSize = new Vector2(850f, 74f),
                PalettePosition = new Vector2(0f, -935f),
                PaletteButtonSize = new Vector2(285f, 78f),
                PaletteGapX = 28f,
                PaletteGapY = 22f,
                HelperTextPosition = new Vector2(0f, -1145f),
                HelperTextSize = new Vector2(860f, 42f),
                ActionButtonSize = new Vector2(430f, 94f),
                ValidateButtonPosition = new Vector2(-240f, -1255f),
                ClearButtonPosition = new Vector2(240f, -1255f),
                SolveButtonPosition = new Vector2(-240f, -1375f),
                Play3DButtonPosition = new Vector2(240f, -1375f),
                SolverBackButtonPosition = new Vector2(0f, -1495f)
            };
        }

        public SolverPanelUI(Transform parent)
        {
            state = SolverInputState.CreateEmpty();

            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "SolverCanvas", 1510);
            root = canvas.gameObject;
            solverCanvas = canvas;
            solverRaycaster = root.GetComponent<GraphicRaycaster>();
            CasualUIFactory.CreateBackdrop(root.transform, "SolverBackdrop", true, false);
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "SolverPanel",
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.transform.SetAsLastSibling();
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = Color.clear;
                panelImage.raycastTarget = false;
            }

            titleText = RuntimeUiFactory.CreateText(panel, "Title", T("manual_solver"), 58, TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.78f, 0.22f, 1f);
            titleRect = titleText.rectTransform;
            ApplyTextDepth(titleText, new Color(0f, 0f, 0f, 0.58f), new Vector2(0f, -3f));

            Text subtitleText = RuntimeUiFactory.CreateText(panel, "Subtitle", T("solver_instruction"), 28, TextAnchor.MiddleCenter);
            subtitleText.color = new Color(0.92f, 0.92f, 0.96f, 1f);
            subtitleRect = subtitleText.rectTransform;

            Button manualButton = RuntimeUiFactory.CreateButton(panel, "ManualSolverButton", T("manual_solver"), Vector2.zero, Vector2.zero);
            Button learnButton = RuntimeUiFactory.CreateButton(panel, "LearnBasicsButton", T("learn_basics"), Vector2.zero, Vector2.zero);
            manualButton.gameObject.SetActive(false);
            learnButton.gameObject.SetActive(false);
            Button backButton = RuntimeUiFactory.CreateButton(panel, "BackButton", "<", Vector2.zero, layout.BackButtonSize);
            backButtonRect = backButton.GetComponent<RectTransform>();
            SetButtonLabel(backButton, "<", 44);
            titleText.gameObject.SetActive(false);
            subtitleText.gameObject.SetActive(false);
            backButton.gameObject.SetActive(false);
            manualButton.onClick.AddListener(() => SetLearnMode(false));
            learnButton.onClick.AddListener(() => SetLearnMode(true));
            backButton.onClick.AddListener(Hide);

            faceGuideView = new GameObject("SolverFaceGuideView").AddComponent<SolverFaceGuideView>();
            faceGuideView.Initialize(panel);

            contentPanelRect = RuntimeUiFactory.CreatePanel(
                panel,
                "ManualSolverContentPanel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                layout.ContentPosition,
                layout.ContentSize);
            Image contentImage = contentPanelRect.GetComponent<Image>();
            if (contentImage != null)
            {
                contentImage.color = PanelNavy;
            }
            AddOutline(contentPanelRect.gameObject, GoldLine, new Vector2(3f, -3f));

            manualInputRoot = new GameObject("ManualInputRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            manualInputRoot.SetParent(contentPanelRect, false);
            manualInputRoot.anchorMin = Vector2.zero;
            manualInputRoot.anchorMax = Vector2.one;
            manualInputRoot.pivot = new Vector2(0.5f, 0.5f);
            manualInputRoot.offsetMin = Vector2.zero;
            manualInputRoot.offsetMax = Vector2.zero;

            currentFaceText = RuntimeUiFactory.CreateText(manualInputRoot, "CurrentFace", string.Empty, 24, TextAnchor.MiddleCenter);
            currentFaceText.fontStyle = FontStyle.Bold;
            currentFaceText.color = new Color(1f, 0.84f, 0.22f, 1f);
            faceLabelRect = currentFaceText.rectTransform;

            instructionText = RuntimeUiFactory.CreateText(manualInputRoot, "InputInstruction", T("solver_instruction"), 24, TextAnchor.MiddleCenter);
            instructionText.color = new Color(0.92f, 0.92f, 0.96f, 1f);
            helperTextRect = instructionText.rectTransform;

            CreateFaceButtons(manualInputRoot);

            gridRoot = CreateGridRoot(manualInputRoot);
            CreateCellButtons();
            gridRoot.gameObject.SetActive(false);

            inputCube3DView = new GameObject("SolverInputCube3DView").AddComponent<SolverInputCube3DView>();
            inputCube3DView.Initialize(
                manualInputRoot,
                state,
                () => (CubeColor)state.selectedColorIndex,
                OnInputCubeFaceChanged,
                OnInputCubeChanged);
            cubeBoardRect = inputCube3DView.GetComponent<RectTransform>();

            paletteRoot = CreatePaletteRoot(manualInputRoot);
            CreatePaletteButtons();

            Button previousButton = RuntimeUiFactory.CreateButton(manualInputRoot, "PreviousFaceButton", T("previous"), Vector2.zero, layout.FaceButtonSize);
            Button nextButton = RuntimeUiFactory.CreateButton(manualInputRoot, "NextFaceButton", T("next"), Vector2.zero, layout.FaceButtonSize);
            previousButtonRect = previousButton.GetComponent<RectTransform>();
            nextButtonRect = nextButton.GetComponent<RectTransform>();
            previousButton.onClick.AddListener(PreviousFace);
            nextButton.onClick.AddListener(NextFace);

            validationText = RuntimeUiFactory.CreateText(manualInputRoot, "ValidationSummary", string.Empty, 15, TextAnchor.MiddleCenter);
            validationText.gameObject.SetActive(false);

            RectTransform resultBar = RuntimeUiFactory.CreatePanel(
                manualInputRoot,
                "ResultBar",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 0.5f),
                layout.ResultBarPosition,
                layout.ResultBarSize);
            resultBarRect = resultBar;
            Image resultImage = resultBar.GetComponent<Image>();
            if (resultImage != null)
            {
                resultImage.color = ResultBrown;
            }
            AddOutline(resultBar.gameObject, new Color(1f, 0.65f, 0.13f, 0.9f), new Vector2(1.5f, -1.5f));

            resultText = RuntimeUiFactory.CreateText(resultBar, "SolverResult", T("solver_result_not_solved"), 24, TextAnchor.MiddleCenter);
            resultText.fontStyle = FontStyle.Bold;
            resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultText.verticalOverflow = VerticalWrapMode.Truncate;
            ApplyTextDepth(resultText, new Color(0f, 0f, 0f, 0.56f), new Vector2(1.5f, -1.5f));

            validateButton = RuntimeUiFactory.CreateButton(manualInputRoot, "ValidateButton", T("validate"), Vector2.zero, layout.ActionButtonSize);
            Button resetButton = RuntimeUiFactory.CreateButton(manualInputRoot, "ResetSolvedButton", T("reset_solved"), Vector2.zero, Vector2.zero);
            Button clearButton = RuntimeUiFactory.CreateButton(manualInputRoot, "ClearButton", T("clear"), Vector2.zero, layout.ActionButtonSize);
            Button saveButton = RuntimeUiFactory.CreateButton(manualInputRoot, "SaveButton", T("save"), Vector2.zero, Vector2.zero);
            Button loadButton = RuntimeUiFactory.CreateButton(manualInputRoot, "LoadButton", T("load"), Vector2.zero, Vector2.zero);
            solveButton = RuntimeUiFactory.CreateButton(manualInputRoot, "SolveButton", T("solve"), Vector2.zero, layout.ActionButtonSize);
            play3DButton = RuntimeUiFactory.CreateButton(manualInputRoot, "Play3DButton", T("play_3d"), Vector2.zero, layout.ActionButtonSize);
            Button solverBackButton = RuntimeUiFactory.CreateButton(manualInputRoot, "ManualSolverBackButton", T("back"), Vector2.zero, layout.ActionButtonSize);
            Button debugButton = RuntimeUiFactory.CreateButton(manualInputRoot, "SolverDebugButton", T("debug"), Vector2.zero, Vector2.zero);
            validateButtonRect = validateButton.GetComponent<RectTransform>();
            clearButtonRect = clearButton.GetComponent<RectTransform>();
            solveButtonRect = solveButton.GetComponent<RectTransform>();
            play3DButtonRect = play3DButton.GetComponent<RectTransform>();
            solverBackButtonRect = solverBackButton.GetComponent<RectTransform>();
            CreateSolverTicketCountContent(solveButtonRect);
            debugButton.gameObject.SetActive(ShowDebugPanel && (Application.isEditor || Debug.isDebugBuild));
            Button closeButton = RuntimeUiFactory.CreateButton(manualInputRoot, "CloseButton", T("close"), Vector2.zero, Vector2.zero);
            validateButton.onClick.AddListener(Validate);
            resetButton.onClick.AddListener(ResetSolved);
            clearButton.onClick.AddListener(ClearInput);
            saveButton.onClick.AddListener(Save);
            loadButton.onClick.AddListener(Load);
            solveButton.onClick.AddListener(Solve);
            debugButton.onClick.AddListener(ShowSolverDebugReport);
            play3DButton.onClick.AddListener(ShowPlayback);
            solverBackButton.onClick.AddListener(Hide);
            closeButton.onClick.AddListener(Hide);
            resetButton.gameObject.SetActive(false);
            saveButton.gameObject.SetActive(false);
            loadButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(false);
            manualActionButtons.Add(previousButton);
            manualActionButtons.Add(nextButton);
            manualActionButtons.Add(validateButton);
            manualActionButtons.Add(clearButton);
            manualActionButtons.Add(solveButton);
            manualActionButtons.Add(play3DButton);
            manualActionButtons.Add(solverBackButton);
            solveButton.interactable = false;
            play3DButton.interactable = false;

            statusText = RuntimeUiFactory.CreateText(manualInputRoot, "Status", string.Empty, 18, TextAnchor.MiddleCenter);
            statusText.gameObject.SetActive(false);

            debugText = RuntimeUiFactory.CreateText(manualInputRoot, "DebugText", string.Empty, 13, TextAnchor.UpperLeft);
            debugText.gameObject.SetActive(ShowDebugPanel);

            ApplyManualSolverLayout();
            playbackPanel = new SolverPlaybackPanelUI(contentPanelRect, ExitPlaybackMode);
            Hide();
        }

        public void Show()
        {
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.SolverDetailBgmReason, true);
            root.SetActive(true);
            solverCanvas.enabled = true;
            if (solverRaycaster != null)
            {
                solverRaycaster.enabled = true;
            }

            ResetFaceNavigator();
            ExitPlaybackMode();
            inventoryStore.Reload();
            SetLearnMode(false);
        }

        public void Hide()
        {
            playbackPanel?.Hide();
            if (manualInputRoot != null)
            {
                manualInputRoot.gameObject.SetActive(true);
            }

            if (solverRaycaster != null)
            {
                solverRaycaster.enabled = false;
            }

            solverCanvas.enabled = false;
            root.SetActive(false);
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.SolverDetailBgmReason, false);
            Closed?.Invoke();
        }

        private void SetLearnMode(bool enabled)
        {
            learnMode = enabled;
            titleText.text = learnMode ? T("learn_basics") : T("manual_solver");
            SetManualObjectsActive(!learnMode);
            statusText.text = learnMode
                ? T("solver_learn_hint")
                : T("solver_instruction");
            debugText.text = learnMode
                ? string.Empty
                : BuildDebugText();
            debugText.gameObject.SetActive(ShowDebugPanel);
            if (play3DButton != null)
            {
                play3DButton.interactable = false;
            }
            UpdateDebugFaceGuide();
            Refresh();
        }

        private void SetManualObjectsActive(bool active)
        {
            gridRoot.gameObject.SetActive(active && ShowDebugPanel);
            if (inputCube3DView != null)
            {
                inputCube3DView.gameObject.SetActive(active);
            }

            paletteRoot.gameObject.SetActive(active);
            currentFaceText.gameObject.SetActive(active);
            instructionText.gameObject.SetActive(active);
            validationText.gameObject.SetActive(false);
            resultText.gameObject.SetActive(active);
            foreach (Button button in faceButtons)
            {
                button.gameObject.SetActive(active && ShowDebugPanel);
            }

            foreach (Button button in manualActionButtons)
            {
                button.gameObject.SetActive(active);
            }

            if (faceGuideView != null)
            {
                faceGuideView.gameObject.SetActive(active && ShowDebugPanel);
            }
        }

        private void ApplyManualSolverLayout()
        {
            float bodyOffsetY = CalculateManualSolverBodyOffsetY();
            float headerOffsetY = CalculateManualSolverHeaderOffsetY(bodyOffsetY);
            SetTopAnchored(titleRect, ApplyHeaderOffset(layout.TitlePosition, bodyOffsetY, headerOffsetY), layout.TitleSize);
            SetTopAnchored(subtitleRect, ApplyHeaderOffset(layout.SubtitlePosition, bodyOffsetY, headerOffsetY), layout.SubtitleSize);
            SetTopAnchored(backButtonRect, ApplyHeaderOffset(layout.BackButtonPosition, bodyOffsetY, headerOffsetY), layout.BackButtonSize);
            SetTopAnchored(contentPanelRect, ApplyBodyOffset(layout.ContentPosition, bodyOffsetY), layout.ContentSize);
            StretchToParent(manualInputRoot, Vector2.zero, Vector2.zero);
            SetTopAnchored(faceLabelRect, layout.FaceLabelPosition, layout.FaceLabelSize);
            SetTopAnchored(cubeBoardRect, layout.CubeBoardPosition, layout.CubeBoardSize);
            SetCenterTopAnchored(previousButtonRect, layout.PreviousButtonPosition, layout.FaceButtonSize);
            SetCenterTopAnchored(nextButtonRect, layout.NextButtonPosition, layout.FaceButtonSize);
            SetCenterTopAnchored(resultBarRect, layout.ResultBarPosition, layout.ResultBarSize);
            SetTopAnchored(paletteRoot, layout.PalettePosition, new Vector2(
                (layout.PaletteButtonSize.x * 3f) + (layout.PaletteGapX * 2f),
                (layout.PaletteButtonSize.y * 2f) + layout.PaletteGapY));
            SetTopAnchored(helperTextRect, layout.HelperTextPosition, layout.HelperTextSize);
            SetCenterTopAnchored(validateButtonRect, layout.ValidateButtonPosition, layout.ActionButtonSize);
            SetCenterTopAnchored(clearButtonRect, layout.ClearButtonPosition, layout.ActionButtonSize);
            SetCenterTopAnchored(solveButtonRect, layout.SolveButtonPosition, layout.ActionButtonSize);
            SetCenterTopAnchored(play3DButtonRect, layout.Play3DButtonPosition, layout.ActionButtonSize);
            SetCenterTopAnchored(solverBackButtonRect, layout.SolverBackButtonPosition, layout.ActionButtonSize);

            for (int i = 0; i < paletteButtons.Count; i++)
            {
                int row = i / 3;
                int col = i % 3;
                RectTransform rect = paletteButtons[i].GetComponent<RectTransform>();
                Vector2 position = new Vector2(
                    (col - 1) * (layout.PaletteButtonSize.x + layout.PaletteGapX),
                    -row * (layout.PaletteButtonSize.y + layout.PaletteGapY));
                SetTopAnchored(rect, position, layout.PaletteButtonSize);
            }

            StretchToParent(resultText.rectTransform, new Vector2(18f, 0f), new Vector2(-18f, 0f));
            StylePrimaryActionButton(validateButtonRect.GetComponent<Button>());
            StylePrimaryActionButton(clearButtonRect.GetComponent<Button>());
            StylePrimaryActionButton(solveButtonRect.GetComponent<Button>());
            StylePrimaryActionButton(play3DButtonRect.GetComponent<Button>());
            StylePrimaryActionButton(solverBackButtonRect.GetComponent<Button>());
            StylePrimaryActionButton(previousButtonRect.GetComponent<Button>());
            StylePrimaryActionButton(nextButtonRect.GetComponent<Button>());
        }

        private float CalculateManualSolverBodyOffsetY()
        {
            const float referenceHeight = 1920f;
            RectTransform canvasRect = root != null ? root.GetComponent<RectTransform>() : null;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : referenceHeight;
            float extraHeight = Mathf.Max(0f, canvasHeight - referenceHeight);
            return Mathf.Clamp(extraHeight * 0.5f, 0f, 220f);
        }

        private float CalculateManualSolverHeaderOffsetY(float bodyOffsetY)
        {
            if (bodyOffsetY <= 0f)
            {
                return 0f;
            }

            RectTransform canvasRect = root != null ? root.GetComponent<RectTransform>() : null;
            TopCurrencyBar topCurrencyBar = root != null ? root.GetComponentInChildren<TopCurrencyBar>(true) : null;
            RectTransform topHudRect = topCurrencyBar != null ? topCurrencyBar.GetComponent<RectTransform>() : null;
            if (canvasRect == null || topHudRect == null)
            {
                return 0f;
            }

            Vector3[] corners = new Vector3[4];
            topHudRect.GetWorldCorners(corners);
            float topHudBottomY = float.MaxValue;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = canvasRect.InverseTransformPoint(corners[i]);
                topHudBottomY = Mathf.Min(topHudBottomY, local.y);
            }

            float topHudBottomDistanceFromTop = canvasRect.rect.yMax - topHudBottomY;
            float contentTopDistanceFromTop = -(layout.ContentPosition.y - bodyOffsetY);
            float topGap = Mathf.Max(0f, contentTopDistanceFromTop - topHudBottomDistanceFromTop);
            return Mathf.Clamp(topGap * 0.5f, 0f, 80f);
        }

        private static Vector2 ApplyBodyOffset(Vector2 basePosition, float bodyOffsetY)
        {
            return new Vector2(basePosition.x, basePosition.y - bodyOffsetY);
        }

        private static Vector2 ApplyHeaderOffset(Vector2 basePosition, float bodyOffsetY, float headerOffsetY)
        {
            return new Vector2(basePosition.x, basePosition.y - bodyOffsetY + headerOffsetY);
        }

        private static void SetTopAnchored(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetCenterTopAnchored(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void StretchToParent(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void StylePrimaryActionButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.12f, 0.35f, 0.96f, 1f);
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                ApplyTextDepth(label, new Color(0f, 0f, 0f, 0.56f), new Vector2(1.5f, -1.5f));
            }

            AddOutline(button.gameObject, GoldLine, new Vector2(2.5f, -2.5f));
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
            {
                return;
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void ApplyTextDepth(Text text, Color color, Vector2 distance)
        {
            if (text == null || text.GetComponent<Shadow>() != null)
            {
                return;
            }

            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private void CreateFaceButtons(RectTransform parent)
        {
            for (int i = 0; i < InternalFaceLabels.Length; i++)
            {
                int faceIndex = i;
                Button button = RuntimeUiFactory.CreateButton(parent, $"Face{InternalFaceLabels[i]}Button", InternalFaceLabels[i], new Vector2(-325f + (i * 130f), 690f), new Vector2(112f, 52f));
                button.onClick.AddListener(() =>
                {
                    SelectInternalFace(faceIndex);
                });
                faceButtons.Add(button);
            }
        }

        private RectTransform CreateGridRoot(RectTransform parent)
        {
            GameObject gridObject = new GameObject("FaceletGrid", typeof(RectTransform));
            gridObject.transform.SetParent(parent, false);
            RectTransform rect = gridObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -230f);
            rect.sizeDelta = new Vector2(312f, 312f);
            return rect;
        }

        private void CreateCellButtons()
        {
            const float size = 94f;
            const float gap = 9f;
            for (int cell = 0; cell < SolverInputState.FaceletPerFace; cell++)
            {
                int cellIndex = cell;
                Button button = RuntimeUiFactory.CreateButton(gridRoot, $"Cell{cell}", string.Empty, Vector2.zero, new Vector2(size, size));
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                int row = cell / 3;
                int col = cell % 3;
                rect.anchoredPosition = new Vector2((col - 1) * (size + gap), (1 - row) * (size + gap));
                button.onClick.AddListener(() =>
                {
                    int index = (currentFrontFaceIndex * SolverInputState.FaceletPerFace) + cellIndex;
                    state.faceletColorIndexes[index] = state.selectedColorIndex;
                    InvalidateSolverProgress();
                    Refresh();
                });
                cellButtons.Add(button);
            }
        }

        private RectTransform CreatePaletteRoot(RectTransform parent)
        {
            GameObject paletteObject = new GameObject("ColorPalette", typeof(RectTransform));
            paletteObject.transform.SetParent(parent, false);
            RectTransform rect = paletteObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -650f);
            rect.sizeDelta = new Vector2(640f, 112f);
            return rect;
        }

        private void CreateSolverTicketCountContent(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            Text label = parent.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }

            GameObject iconObject = new GameObject("SolverTicketIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-112f, 0f);
            iconRect.sizeDelta = new Vector2(68f, 68f);
            solverTicketIconImage = iconObject.GetComponent<Image>();
            solverTicketIconImage.sprite = Resources.Load<Sprite>("UI/Shop/Icons/solver_ticket");
            solverTicketIconImage.preserveAspect = true;
            solverTicketIconImage.color = Color.white;
            solverTicketIconImage.raycastTarget = false;

            solverTicketCountText = RuntimeUiFactory.CreateText(parent, "SolverTicketCount", string.Format(T("solve_count"), 0), 34, TextAnchor.MiddleLeft);
            solverTicketCountText.fontStyle = FontStyle.Bold;
            solverTicketCountText.color = Color.white;
            RectTransform textRect = solverTicketCountText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(-66f, 0f);
            textRect.sizeDelta = new Vector2(260f, 70f);
            ApplyTextDepth(solverTicketCountText, new Color(0f, 0f, 0f, 0.58f), new Vector2(1.5f, -1.5f));
        }

        private void CreatePaletteButtons()
        {
            for (int i = 0; i < PaletteColors.Length; i++)
            {
                CubeColor color = PaletteColors[i];
                int row = i / 3;
                int col = i % 3;
                Button button = RuntimeUiFactory.CreateButton(
                    paletteRoot,
                    $"{color}PaletteButton",
                    color.ToString(),
                    new Vector2(-190f + (col * 190f), 58f - (row * 54f)),
                    new Vector2(164f, 46f));
                button.onClick.AddListener(() =>
                {
                    state.selectedColorIndex = (int)color;
                    Refresh();
                });
                paletteButtons.Add(button);
            }
        }

        private void Refresh()
        {
            if (learnMode)
            {
                return;
            }

            state.EnsureShape();
            int internalFaceIndex = currentFrontFaceIndex;
            if (currentFaceText != null)
            {
                currentFaceText.text = $"FACE: {InternalFaceLabels[currentFrontFaceIndex]}";
            }

            if (instructionText != null)
            {
                instructionText.text = T("solver_instruction");
            }

            if (solverTicketCountText != null)
            {
                solverTicketCountText.text = string.Format(T("solve_count"), inventoryStore.SolverTickets);
            }

            for (int i = 0; i < cellButtons.Count; i++)
            {
                int index = (internalFaceIndex * SolverInputState.FaceletPerFace) + i;
                CubeColor color = (CubeColor)state.faceletColorIndexes[index];
                SetButtonColor(cellButtons[i], ToUnityColor(color));
                SetButtonLabel(cellButtons[i], i == 4 ? "Center" : string.Empty, 19);
                SetButtonTextColor(cellButtons[i], IsLightColor(color) ? Color.black : Color.white);
            }

            for (int i = 0; i < faceButtons.Count; i++)
            {
                SetButtonLabel(faceButtons[i], InternalFaceLabels[i], i == internalFaceIndex ? 16 : 14);
                SetButtonColor(faceButtons[i], i == internalFaceIndex
                    ? new Color(0.28f, 0.24f, 0.08f, 1f)
                    : new Color(0.12f, 0.16f, 0.2f, 0.96f));
                SetButtonTextColor(faceButtons[i], Color.white);
            }

            UpdateDebugFaceGuide();

            foreach (Button button in paletteButtons)
            {
                CubeColor color = GetPaletteColor(button.name);
                SetButtonColor(button, ToUnityColor(color));
                int count = CountColor(color);
                string warning = count > 9 ? " !" : string.Empty;
                SetButtonLabel(button, $"{ShortColorName(color)} {count}/9{warning}", 15);
                SetButtonTextColor(button, IsLightColor(color) ? Color.black : Color.white);
                button.transform.localScale = (int)color == state.selectedColorIndex ? Vector3.one * 1.08f : Vector3.one;
            }

            if (validationText != null)
            {
                validationText.text = BuildValidationSummary();
            }

            if (debugText != null)
            {
                debugText.text = BuildDebugText();
            }

            inputCube3DView?.RefreshColors();
            UpdateGuidanceButtons();
        }

        private void OnInputCubeFaceChanged(int internalFaceIndex)
        {
            currentFrontFaceIndex = Mathf.Clamp(internalFaceIndex, 0, InternalFaceLabels.Length - 1);
            currentDisplayIndex = GetDisplayIndexForInternalFace(currentFrontFaceIndex);
            currentFrontNormal = GetNormalForInternalFace(currentFrontFaceIndex);
            currentUpNormal = currentFrontNormal == Vector3Int.up || currentFrontNormal == Vector3Int.down
                ? Vector3Int.back
                : Vector3Int.up;
            Refresh();
        }

        private void OnInputCubeChanged()
        {
            InvalidateSolverProgress();
            Refresh();
        }

        private void InvalidateSolverProgress()
        {
            validationAttemptedForCurrentInput = false;
            validationPassedForCurrentInput = false;
            solveAttemptedForCurrentInput = false;
            solutionReadyForCurrentInput = false;
            lastSolution = null;
            if (solveButton != null)
            {
                solveButton.interactable = false;
            }

            if (play3DButton != null)
            {
                play3DButton.interactable = false;
            }

            StopPulse(solveButton);
            StopPulse(play3DButton);
        }

        private void UpdateGuidanceButtons()
        {
            if (validateButton != null)
            {
                validateButton.interactable = true;
            }

            bool complete = IsFullColorInputComplete();
            if (!complete)
            {
                validationPassedForCurrentInput = false;
                solutionReadyForCurrentInput = false;
                if (solveButton != null)
                {
                    solveButton.interactable = false;
                }

                if (play3DButton != null)
                {
                    play3DButton.interactable = false;
                }

                StopPulse(validateButton);
                StopPulse(solveButton);
                StopPulse(play3DButton);
                return;
            }

            if (!validationPassedForCurrentInput)
            {
                if (!validationAttemptedForCurrentInput)
                {
                    StartPulse(validateButton);
                }
                else
                {
                    StopPulse(validateButton);
                }

                if (solveButton != null)
                {
                    solveButton.interactable = false;
                }

                if (play3DButton != null)
                {
                    play3DButton.interactable = false;
                }

                StopPulse(solveButton);
                StopPulse(play3DButton);
                return;
            }

            StopPulse(validateButton);
            if (!solutionReadyForCurrentInput)
            {
                if (solveButton != null)
                {
                    solveButton.interactable = true;
                }

                if (!solveAttemptedForCurrentInput)
                {
                    StartPulse(solveButton);
                }
                else
                {
                    StopPulse(solveButton);
                }

                if (play3DButton != null)
                {
                    play3DButton.interactable = false;
                }

                StopPulse(play3DButton);
                return;
            }

            StopPulse(solveButton);
            if (play3DButton != null && play3DButton.interactable)
            {
                StartPulse(play3DButton);
            }
        }

        private bool IsFullColorInputComplete()
        {
            state.EnsureShape();
            for (int i = 0; i < PaletteColors.Length; i++)
            {
                if (CountColor(PaletteColors[i]) != SolverInputState.FaceletPerFace)
                {
                    return false;
                }
            }

            for (int i = 0; i < SolverInputState.FaceletCount; i++)
            {
                if ((CubeColor)state.faceletColorIndexes[i] == CubeColor.None)
                {
                    return false;
                }
            }

            return true;
        }

        private static void StartPulse(Button button)
        {
            if (button == null || !button.interactable)
            {
                return;
            }

            ButtonPulseHint hint = button.GetComponent<ButtonPulseHint>();
            if (hint == null)
            {
                hint = button.gameObject.AddComponent<ButtonPulseHint>();
            }

            hint.Play();
        }

        private static void StopPulse(Button button)
        {
            ButtonPulseHint hint = button != null ? button.GetComponent<ButtonPulseHint>() : null;
            if (hint != null)
            {
                hint.Stop();
            }
        }

        private void SelectInternalFace(int internalFaceIndex)
        {
            int displayIndex = GetDisplayIndexForInternalFace(internalFaceIndex);
            SelectDisplayFace(displayIndex);
        }

        private void PreviousFace()
        {
            if (currentDisplayIndex <= 0)
            {
                return;
            }

            SelectAdjacentDisplayFace(currentDisplayIndex - 1, -SolverNextTurns[currentDisplayIndex - 1]);
        }

        private void NextFace()
        {
            if (currentDisplayIndex >= SolverDisplayOrder.Length - 1)
            {
                return;
            }

            SelectAdjacentDisplayFace(currentDisplayIndex + 1, SolverNextTurns[currentDisplayIndex]);
        }

        private void SelectDisplayFace(int displayIndex)
        {
            if (inputCube3DView == null || inputCube3DView.IsRotating)
            {
                return;
            }

            currentDisplayIndex = Mathf.Clamp(displayIndex, 0, SolverDisplayOrder.Length - 1);
            inputCube3DView.RotateToFace(SolverDisplayOrder[currentDisplayIndex], SolverVisibleOrder[currentDisplayIndex]);
        }

        private void SelectAdjacentDisplayFace(int displayIndex, Vector3 screenTurn)
        {
            if (inputCube3DView == null || inputCube3DView.IsRotating)
            {
                return;
            }

            currentDisplayIndex = Mathf.Clamp(displayIndex, 0, SolverDisplayOrder.Length - 1);
            inputCube3DView.RotateByScreenQuarterTurn(
                SolverDisplayOrder[currentDisplayIndex],
                SolverVisibleOrder[currentDisplayIndex],
                screenTurn);
        }

        private void Validate()
        {
            StopPulse(validateButton);
            validationAttemptedForCurrentInput = true;
            validationPassedForCurrentInput = false;
            solveAttemptedForCurrentInput = false;
            solutionReadyForCurrentInput = false;
            if (solveButton != null)
            {
                solveButton.interactable = false;
            }

            if (play3DButton != null)
            {
                play3DButton.interactable = false;
            }

            if (!TryCreateRequest(out SolverRequest request, out string error))
            {
                statusText.text = $"{T("invalid_cube_input")}.\n{error}";
                resultText.text = BuildFailureResultText(T("invalid_cube_input"), error, null);
                validationText.text = BuildValidationSummary();
                debugText.text = BuildDebugText();
                UpdateGuidanceButtons();
                return;
            }

            SolverValidationResult result = solverEngine.Validate(request);
            validationPassedForCurrentInput = result.isValid;
            statusText.text = result.isValid
                ? T("cube_input_valid")
                : result.userMessage;
            resultText.text = result.isValid
                ? $"{T("status_label")}: {T("validation_passed")}\n{T("ready_to_solve")}."
                : BuildFailureResultText(T("invalid_cube"), result.userMessage, result.errorCode);
            validationText.text = BuildValidationSummary();
            debugText.text = BuildDebugText();
            UpdateGuidanceButtons();
        }

        private void ResetSolved()
        {
            state = SolverInputState.CreateSolved();
            ResetFaceNavigator();
            store.Save(state);
            inputCube3DView?.SetState(state);
            inputCube3DView?.ResetToFront();
            InvalidateSolverProgress();
            statusText.text = T("reset_to_solved_cube");
            resultText.text = $"{T("status_label")}: {T("cube_already_solved")}\n{T("moves_label")}: 0\n{T("solution_label")}: {T("no_moves_needed")}";
            Refresh();
        }

        private void ClearInput()
        {
            state.EnsureShape();
            for (int i = 0; i < SolverInputState.FaceletCount; i++)
            {
                state.faceletColorIndexes[i] = (int)CubeColor.None;
            }

            InvalidateSolverProgress();
            statusText.text = T("input_cleared");
            resultText.text = T("solver_result_not_solved");
            inputCube3DView?.RefreshColors();
            Refresh();
        }

        private void Save()
        {
            bool saved = store.Save(state);
            statusText.text = saved ? T("solver_input_saved") : T("save_failed");
            Refresh();
        }

        private void Load()
        {
            state = store.Load();
            inputCube3DView?.SetState(state);
            InvalidateSolverProgress();
            statusText.text = T("solver_input_loaded");
            resultText.text = T("solver_loaded_press_solve");
            Refresh();
        }

        private void Solve()
        {
            StopPulse(solveButton);
            solveAttemptedForCurrentInput = true;
            solutionReadyForCurrentInput = false;
            if (isSolving)
            {
                return;
            }

            if (!TryCreateRequest(out SolverRequest request, out string error))
            {
                validationPassedForCurrentInput = false;
                solutionReadyForCurrentInput = false;
                lastSolution = null;
                if (solveButton != null)
                {
                    solveButton.interactable = false;
                }

                play3DButton.interactable = false;
                statusText.text = $"{T("invalid_cube_input")}.\n{error}";
                resultText.text = BuildFailureResultText(T("invalid_cube_input"), error, null);
                Refresh();
                return;
            }

            bool alreadySolvedInput = request.faceletString == "UUUUUUUUURRRRRRRRRFFFFFFFFFDDDDDDDDDLLLLLLLLLBBBBBBBBB";
            if (!IsSolverUsageBypassed() && !alreadySolvedInput && usageStore.RemainingFreeUses <= 0 && inventoryStore.SolverTickets <= 0)
            {
                lastSolution = null;
                play3DButton.interactable = false;
                statusText.text = T("no_solver_uses_available");
                resultText.text = $"{T("status_label")}: {T("solver_unavailable")}\n{T("no_free_solver_uses")}";
                Refresh();
                return;
            }

            isSolving = true;
            statusText.text = T("solving");
            resultText.text = $"{T("status_label")}: {T("solving")}";
            SolverValidationResult validation = solverEngine.Validate(request);
            if (!validation.isValid)
            {
                validationPassedForCurrentInput = false;
                solutionReadyForCurrentInput = false;
                if (solveButton != null)
                {
                    solveButton.interactable = false;
                }

                if (play3DButton != null)
                {
                    play3DButton.interactable = false;
                }

                statusText.text = validation.userMessage;
                resultText.text = BuildFailureResultText(T("invalid_cube"), validation.userMessage, validation.errorCode);
                validationText.text = BuildValidationSummary();
                if (ShowDebugPanel)
                {
                    debugText.text = string.Join("\n", validation.details);
                }

                isSolving = false;
                UpdateGuidanceButtons();
                return;
            }

            SolverResult result = solverEngine.Solve(request);
            lastSolverRequest = request;
            lastSolverResult = result;
            if (result.success)
            {
                lastSolution = SolverSolution.FromResult(request.faceletString, result);
                if (SolverInputSerializer.TryToPlaybackCubeState(state, out CubeState playbackState, out _))
                {
                    lastSolution.sourceColorFaceletString = CubeStateSerializer.ToFaceletString(playbackState);
                }

                solutionStore.Save(lastSolution);
                play3DButton.interactable = result.moveCount > 0;
                solutionReadyForCurrentInput = true;
                string usageMessage = ConsumeSolverUseIfNeeded(result.moveCount);
                statusText.text = $"{T("solver_status")}: {result.message}";
                resultText.text = BuildSuccessResultText(result, usageMessage);
            }
            else if (result.errorCode == SolverErrorCode.SolverNotConnected || result.errorCode == SolverErrorCode.SolverEngineNotImplemented)
            {
                lastSolution = null;
                play3DButton.interactable = false;
                statusText.text = $"{result.message}\n{T("free_solver_uses_today")}: {usageStore.RemainingFreeUses} | {T("solver_tickets")}: {inventoryStore.SolverTickets}";
                resultText.text = BuildFailureResultText(T("solver_not_connected"), result.message, result.errorCode);
            }
            else if (result.isValidCube)
            {
                lastSolution = null;
                solutionReadyForCurrentInput = false;
                play3DButton.interactable = false;
                statusText.text = result.errorCode == SolverErrorCode.Timeout
                    ? T("solver_timed_out")
                    : T("solver_solution_not_found_valid");
                resultText.text = BuildFailureResultText(T("solution_not_found"), FriendlySolverMessage(result), result.errorCode);
            }
            else
            {
                lastSolution = null;
                validationPassedForCurrentInput = false;
                solutionReadyForCurrentInput = false;
                if (solveButton != null)
                {
                    solveButton.interactable = false;
                }

                play3DButton.interactable = false;
                statusText.text = $"{T("invalid_cube_state")}.\n{result.message}\n{T("check_corner_edge_colors")}";
                resultText.text = BuildFailureResultText(T("invalid_cube_state"), FriendlySolverMessage(result), result.errorCode);
            }

            if (ShowDebugPanel)
            {
                debugText.text =
                    $"Engine: {solverEngine.EngineName}\n" +
                    $"Fallback: {SolverEngineProvider.IsUsingFallback()}\n" +
                    $"Error: {result.errorCode}\n" +
                    $"Elapsed: {result.elapsedMs}ms\n" +
                    $"Max Depth: {result.maxDepth}\n" +
                    $"Timeout: {result.timeoutMs}ms\n" +
                    $"Searched Nodes: {result.searchedNodes}\n" +
                    request.faceletString;
            }

            isSolving = false;
            UpdateGuidanceButtons();
        }

        private void ShowPlayback()
        {
            StopPulse(play3DButton);
            if (lastSolution == null)
            {
                statusText.text = T("no_solver_solution_available");
                return;
            }

            if (lastSolution.moveNotations == null || lastSolution.moveNotations.Length == 0)
            {
                statusText.text = T("no_moves_needed_cube_solved");
                return;
            }

            manualInputRoot.gameObject.SetActive(false);
            playbackPanel?.Show(lastSolution);
        }

        private void ExitPlaybackMode()
        {
            playbackPanel?.Hide();
            if (manualInputRoot != null)
            {
                manualInputRoot.gameObject.SetActive(true);
            }
        }

        private string ConsumeSolverUseIfNeeded(int moveCount)
        {
            if (IsSolverUsageBypassed())
            {
                return T("dev_solver_not_consumed");
            }

            if (moveCount <= 0)
            {
                return T("solver_use_not_consumed");
            }

            if (usageStore.TryUseFree(System.DateTime.UtcNow))
            {
                return $"{T("free_solver_uses_today")}: {usageStore.RemainingFreeUses}";
            }

            if (inventoryStore.TryConsume(StageAssistItemType.SolverTicket))
            {
                usageStore.AddTicketUse(System.DateTime.UtcNow);
                return $"{T("solver_ticket_used")} {T("tickets")}: {inventoryStore.SolverTickets}";
            }

            return T("solution_found_no_use_consumed");
        }

        private static bool IsSolverUsageBypassed()
        {
            return false;
        }

        private static string BuildSuccessResultText(SolverResult result, string usageMessage)
        {
            if (result == null)
            {
                return $"{T("status_label")}: {T("failed")}\n{T("solver_returned_no_result")}";
            }

            if (result.moveCount <= 0)
            {
                return $"{T("status_label")}: {T("cube_already_solved")}\n{T("moves_label")}: 0\n{T("solution_label")}: {T("no_moves_needed")}";
            }

            return $"{T("status_label")}: {T("solution_found")}\n{T("moves_label")}: {result.moveCount}\n{T("solution_label")}: {DisplaySolution(result.solutionNotation)}\n{T("moves_list")}:\n{BuildMoveList(result.moveNotations)}\n{usageMessage}";
        }

        private static string BuildMoveList(string[] moveNotations)
        {
            if (moveNotations == null || moveNotations.Length == 0)
            {
                return T("no_moves_needed");
            }

            const int maxDisplayedMoves = 18;
            var lines = new List<string>();
            int count = Math.Min(moveNotations.Length, maxDisplayedMoves);
            for (int i = 0; i < count; i++)
            {
                lines.Add($"{i + 1}. {moveNotations[i]}");
            }

            if (moveNotations.Length > maxDisplayedMoves)
            {
                lines.Add($"... {moveNotations.Length - maxDisplayedMoves} {T("more")}");
            }

            return string.Join("   ", lines);
        }

        private static string BuildFailureResultText(string status, string message, string errorCode)
        {
            string friendly = string.IsNullOrWhiteSpace(message) ? T("please_check_cube_input") : message;
            return string.IsNullOrWhiteSpace(errorCode)
                ? $"{T("status_label")}: {status}\n{friendly}"
                : $"{T("status_label")}: {status}\n{friendly}\n{T("code_label")}: {errorCode}";
        }

        private static string FriendlySolverMessage(SolverResult result)
        {
            if (result == null)
            {
                return T("solver_returned_no_result");
            }

            switch (result.errorCode)
            {
                case SolverErrorCode.InvalidColorCount:
                    return T("check_color_counts");
                case SolverErrorCode.DuplicateCenters:
                    return T("center_colors_unique");
                case SolverErrorCode.InvalidCornerCubie:
                case SolverErrorCode.InvalidEdgeCubie:
                case SolverErrorCode.InvalidCubieState:
                case SolverErrorCode.ParityError:
                case SolverErrorCode.TwistError:
                case SolverErrorCode.FlipError:
                    return T("invalid_cube_state_unsolvable");
                case SolverErrorCode.Timeout:
                    return T("solver_timed_out");
                case SolverErrorCode.CurrentSolverLimitation:
                case SolverErrorCode.SolutionNotFound:
                    return T("solver_solution_not_found_stronger");
                case SolverErrorCode.SolverNotConnected:
                case SolverErrorCode.HighPerformanceEngineNotAvailable:
                case SolverErrorCode.SolverEngineNotImplemented:
                    return T("solver_engine_not_connected");
                default:
                    return string.IsNullOrWhiteSpace(result.message) ? T("solver_failed") : result.message;
            }
        }

        private bool TryCreateRequest(out SolverRequest request, out string error)
        {
            request = null;
            error = string.Empty;
            SolverInputValidationResult inputValidation = SolverInputValidator.Validate(state);
            if (!inputValidation.isValid)
            {
                error = string.Join("\n", inputValidation.messages);
                return false;
            }

            if (!SolverInputSerializer.TryToFaceletString(state, out string faceletString, out error))
            {
                return false;
            }

            request = new SolverRequest
            {
                faceletString = faceletString,
                faceOrder = SolverInputState.DefaultFaceOrder,
                maxDepth = SolverEngineProvider.IsHighPerformanceAvailable() ? 24 : 8,
                timeoutMs = SolverEngineProvider.IsHighPerformanceAvailable() ? 15000 : 5000,
                requireFullValidation = true
            };
            return true;
        }

        private void ShowSolverDebugReport()
        {
            string report = BuildSolverDebugReport();
            GUIUtility.systemCopyBuffer = report;
            statusText.text = T("solver_debug_saved");
            resultText.text = report;
            Debug.Log(report);
        }

        private string BuildSolverDebugReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Solver Debug");
            builder.AppendLine($"Active Engine: {solverEngine.EngineName}");
            builder.AppendLine($"Solver License: {(SolverEngineProvider.IsHighPerformanceAvailable() ? "MIT" : "Internal")}");
            builder.AppendLine($"High Performance Available: {SolverEngineProvider.IsHighPerformanceAvailable()}");
            builder.AppendLine($"Using Fallback: {SolverEngineProvider.IsUsingFallback()}");
            if (SolverEngineProvider.IsUsingFallback())
            {
                builder.AppendLine($"Fallback Reason: {SolverEngineProvider.GetFallbackReason()}");
            }

            SolverInputValidationResult inputValidation = SolverInputValidator.Validate(state);
            builder.AppendLine($"Input valid: {inputValidation.isValid}");
            if (inputValidation.messages != null && inputValidation.messages.Count > 0)
            {
                builder.AppendLine(string.Join(" | ", inputValidation.messages));
            }

            if (SolverInputSerializer.TryToFaceletString(state, out string faceletString, out string error))
            {
                SolverDebugCaseStore.SaveLatest(faceletString);
                builder.AppendLine($"Facelets: {faceletString}");
                builder.AppendLine($"Length: {faceletString.Length}");
                AppendFaceletCounts(builder, faceletString);

                var request = new SolverRequest
                {
                    faceletString = faceletString,
                    faceOrder = SolverInputState.DefaultFaceOrder,
                    maxDepth = SolverEngineProvider.IsHighPerformanceAvailable() ? 24 : 8,
                    timeoutMs = SolverEngineProvider.IsHighPerformanceAvailable() ? 15000 : 5000,
                    requireFullValidation = true
                };
                builder.AppendLine($"Request Max Depth: {request.maxDepth}");
                builder.AppendLine($"Request Timeout: {request.timeoutMs}ms");
                SolverValidationResult engineValidation = solverEngine.Validate(request);
                builder.AppendLine($"Engine valid: {engineValidation.isValid}");
                builder.AppendLine($"Error: {DisplayDebugValue(engineValidation.errorCode)}");
                builder.AppendLine($"Message: {DisplayDebugValue(engineValidation.userMessage)}");
                builder.AppendLine($"Debug: {DisplayDebugValue(engineValidation.debugMessage)}");
                if (engineValidation.details != null && engineValidation.details.Count > 0)
                {
                    builder.AppendLine($"Details: {string.Join(" | ", engineValidation.details)}");
                }

                AppendPieceDiagnostics(builder, faceletString);
                AppendFaceletGrid(builder, faceletString);

                if (lastSolverResult != null
                    && lastSolverRequest != null
                    && lastSolverRequest.faceletString == faceletString)
                {
                    builder.AppendLine($"Last Solve Error: {DisplayDebugValue(lastSolverResult.errorCode)}");
                    builder.AppendLine($"Last Solve Elapsed: {lastSolverResult.elapsedMs}ms");
                    builder.AppendLine($"Last Solve Max Depth: {lastSolverResult.maxDepth}");
                    builder.AppendLine($"Last Solve Timeout: {lastSolverResult.timeoutMs}ms");
                    builder.AppendLine($"Last Solve Searched Nodes: {lastSolverResult.searchedNodes}");
                    builder.AppendLine($"Last Solve Debug: {DisplayDebugValue(lastSolverResult.debugMessage)}");
                }
            }
            else
            {
                builder.AppendLine($"Facelet build failed: {error}");
            }

            return builder.ToString();
        }

        private static void AppendFaceletCounts(StringBuilder builder, string facelets)
        {
            string labels = "URFDLB";
            for (int i = 0; i < labels.Length; i++)
            {
                char label = labels[i];
                int count = 0;
                for (int j = 0; j < facelets.Length; j++)
                {
                    if (facelets[j] == label)
                    {
                        count++;
                    }
                }

                builder.Append(i == 0 ? "Counts: " : " | ");
                builder.Append(label);
                builder.Append('=');
                builder.Append(count);
            }

            builder.AppendLine();
        }

        private static void AppendPieceDiagnostics(StringBuilder builder, string facelets)
        {
            builder.AppendLine("Corners:");
            for (int i = 0; i < DiagnosticCornerFacelets.Length; i++)
            {
                AppendPieceDiagnostic(
                    builder,
                    DiagnosticCornerNames[i],
                    DiagnosticCornerFacelets[i],
                    DiagnosticCornerPieces,
                    facelets);
            }

            builder.AppendLine("Edges:");
            for (int i = 0; i < DiagnosticEdgeFacelets.Length; i++)
            {
                AppendPieceDiagnostic(
                    builder,
                    DiagnosticEdgeNames[i],
                    DiagnosticEdgeFacelets[i],
                    DiagnosticEdgePieces,
                    facelets);
            }
        }

        private static void AppendPieceDiagnostic(StringBuilder builder, string positionName, int[] indexes, string[] knownPieces, string facelets)
        {
            string raw = ReadRawPiece(facelets, indexes);
            string sorted = SortLetters(raw);
            bool known = Array.IndexOf(knownPieces, sorted) >= 0;
            builder.Append(known ? "OK " : "BAD ");
            builder.Append(positionName);
            builder.Append(": ");
            builder.Append(raw);
            builder.Append(" -> ");
            builder.Append(sorted);
            builder.Append(" | ");
            for (int i = 0; i < indexes.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(DescribeFacelet(indexes[i], facelets[indexes[i]]));
            }

            builder.AppendLine();
        }

        private static string ReadRawPiece(string facelets, int[] indexes)
        {
            var chars = new char[indexes.Length];
            for (int i = 0; i < indexes.Length; i++)
            {
                chars[i] = facelets[indexes[i]];
            }

            return new string(chars);
        }

        private static string SortLetters(string value)
        {
            char[] chars = value.ToCharArray();
            Array.Sort(chars);
            return new string(chars);
        }

        private static string DescribeFacelet(int index, char value)
        {
            int faceIndex = index / SolverInputState.FaceletPerFace;
            int cellIndex = index % SolverInputState.FaceletPerFace;
            int row = (cellIndex / 3) + 1;
            int col = (cellIndex % 3) + 1;
            return $"{InternalFaceLabels[faceIndex]}({row},{col})={value}";
        }

        private static void AppendFaceletGrid(StringBuilder builder, string facelets)
        {
            string[] names = { "U", "R", "F", "D", "L", "B" };
            for (int face = 0; face < names.Length; face++)
            {
                int start = face * SolverInputState.FaceletPerFace;
                builder.AppendLine($"{names[face]}:");
                builder.AppendLine(facelets.Substring(start, 3));
                builder.AppendLine(facelets.Substring(start + 3, 3));
                builder.AppendLine(facelets.Substring(start + 6, 3));
            }
        }

        private static string DisplayDebugValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private static string DisplaySolution(string notation)
        {
            return string.IsNullOrWhiteSpace(notation) ? T("none_value") : notation;
        }

        private string BuildDebugText()
        {
            if (!ShowDebugPanel)
            {
                return string.Empty;
            }

            if (SolverInputSerializer.TryToFaceletString(state, out string facelets, out string error))
            {
                return $"Internal face: {InternalFaceLabels[currentFrontFaceIndex]} | Order: URFDLB | Color: {(CubeColor)state.selectedColorIndex}\n{facelets}";
            }

            return $"Order: URFDLB | Color: {(CubeColor)state.selectedColorIndex} | {error}";
        }

        private string BuildValidationSummary()
        {
            state.EnsureShape();
            int filled = 0;
            for (int i = 0; i < SolverInputState.FaceletCount; i++)
            {
                CubeColor color = (CubeColor)state.faceletColorIndexes[i];
                if (color != CubeColor.None)
                {
                    filled++;
                }
            }

            SolverInputValidationResult result = SolverInputValidator.Validate(state);
            return result.isValid
                ? $"{T("filled")}: {filled}/54 | {T("status_label")}: {T("ready")}"
                : $"{T("filled")}: {filled}/54 | {T("status_label")}: {FirstValidationMessage(result)}";
        }

        private static string FirstValidationMessage(SolverInputValidationResult result)
        {
            if (result == null || result.messages == null || result.messages.Count == 0)
            {
                return T("check_input");
            }

            return result.messages[0];
        }

        private int CountColor(CubeColor target)
        {
            state.EnsureShape();
            int count = 0;
            for (int i = 0; i < SolverInputState.FaceletCount; i++)
            {
                if ((CubeColor)state.faceletColorIndexes[i] == target)
                {
                    count++;
                }
            }

            return count;
        }

        private static string T(string key)
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key;
        }

        private static CubeColor GetPaletteColor(string name)
        {
            foreach (CubeColor color in PaletteColors)
            {
                if (name.StartsWith(color.ToString()))
                {
                    return color;
                }
            }

            return CubeColor.None;
        }

        private static string ShortColorName(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return "White";
                case CubeColor.Yellow: return "Yellow";
                case CubeColor.Red: return "Red";
                case CubeColor.Orange: return "Orange";
                case CubeColor.Blue: return "Blue";
                case CubeColor.Green: return "Green";
                default: return "None";
            }
        }

        private void ResetFaceNavigator()
        {
            currentDisplayIndex = 0;
            currentFrontFaceIndex = 2;
            currentFrontNormal = Vector3Int.forward;
            currentUpNormal = Vector3Int.up;
            inputCube3DView?.ResetToFront();
        }

        private static int GetDisplayIndexForInternalFace(int internalFaceIndex)
        {
            for (int i = 0; i < SolverDisplayOrder.Length; i++)
            {
                if (SolverDisplayOrder[i] == internalFaceIndex)
                {
                    return i;
                }
            }

            return 0;
        }

        private static Vector3Int GetNormalForInternalFace(int faceIndex)
        {
            switch (faceIndex)
            {
                case 0: return Vector3Int.up;
                case 1: return Vector3Int.right;
                case 2: return Vector3Int.forward;
                case 3: return Vector3Int.down;
                case 4: return Vector3Int.left;
                case 5: return Vector3Int.back;
                default: return Vector3Int.forward;
            }
        }

        private static int GetInternalFaceIndexForNormal(Vector3Int normal)
        {
            if (normal == Vector3Int.up) return 0;
            if (normal == Vector3Int.right) return 1;
            if (normal == Vector3Int.forward) return 2;
            if (normal == Vector3Int.down) return 3;
            if (normal == Vector3Int.left) return 4;
            if (normal == Vector3Int.back) return 5;
            return -1;
        }

        private static Vector3Int Cross(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(
                (a.y * b.z) - (a.z * b.y),
                (a.z * b.x) - (a.x * b.z),
                (a.x * b.y) - (a.y * b.x));
        }

        private static Vector3Int Negate(Vector3Int value)
        {
            return new Vector3Int(-value.x, -value.y, -value.z);
        }

        private void UpdateDebugFaceGuide()
        {
            if (!ShowDebugPanel || faceGuideView == null)
            {
                return;
            }

            faceGuideView.SetSelectedFace(GetCubeFaceForInternalIndex(currentFrontFaceIndex));
        }

        private static CubeFace GetCubeFaceForInternalIndex(int faceIndex)
        {
            switch (faceIndex)
            {
                case 0: return CubeFace.Up;
                case 1: return CubeFace.Right;
                case 2: return CubeFace.Front;
                case 3: return CubeFace.Down;
                case 4: return CubeFace.Left;
                case 5: return CubeFace.Back;
                default: return CubeFace.Up;
            }
        }

        private static void SetButtonLabel(Button button, string label, int fontSize)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text == null)
            {
                return;
            }

            text.text = label;
            text.fontSize = fontSize;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetButtonTextColor(Button button, Color color)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = color;
            }
        }

        private static Color ToUnityColor(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return Color.white;
                case CubeColor.Yellow: return Color.yellow;
                case CubeColor.Red: return Color.red;
                case CubeColor.Orange: return new Color(1f, 0.45f, 0f, 1f);
                case CubeColor.Blue: return new Color(0.05f, 0.25f, 1f, 1f);
                case CubeColor.Green: return new Color(0.05f, 0.75f, 0.18f, 1f);
                default: return new Color(0.16f, 0.16f, 0.16f, 1f);
            }
        }

        private static bool IsLightColor(CubeColor color)
        {
            return color == CubeColor.White || color == CubeColor.Yellow;
        }

        private static void AddDragBar(RectTransform parent)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 62f);
            barObject.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.14f, 1f);
            barObject.AddComponent<PanelDragHandle>().Initialize(parent);
        }

        private static void AddResizeHandle(RectTransform parent)
        {
            GameObject handleObject = new GameObject("ResizeHandle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(parent, false);
            RectTransform rect = handleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-12f, 12f);
            rect.sizeDelta = new Vector2(44f, 44f);
            handleObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
            handleObject.AddComponent<PanelResizeHandle>().Initialize(parent, new Vector2(900f, 980f), new Vector2(1120f, 1240f));
        }

        private sealed class PanelDragHandle : MonoBehaviour, IDragHandler
        {
            private RectTransform target;
            private Canvas parentCanvas;

            public void Initialize(RectTransform dragTarget)
            {
                target = dragTarget;
                parentCanvas = dragTarget.GetComponentInParent<Canvas>();
            }

            public void OnDrag(PointerEventData eventData)
            {
                float scale = parentCanvas != null && parentCanvas.scaleFactor > 0f ? parentCanvas.scaleFactor : 1f;
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
                Vector2 nextSize = target.sizeDelta + new Vector2(eventData.delta.x, -eventData.delta.y);
                target.sizeDelta = new Vector2(
                    Mathf.Clamp(nextSize.x, minSize.x, maxSize.x),
                    Mathf.Clamp(nextSize.y, minSize.y, maxSize.y));
            }
        }

        private enum FaceTurnAxis
        {
            Horizontal,
            Vertical
        }

        private sealed class FaceTransitionAnimator : MonoBehaviour
        {
            private RectTransform rectTransform;
            private CanvasGroup canvasGroup;
            private Coroutine animation;
            private Vector2 basePosition;
            private RectTransform[] cells = new RectTransform[0];
            private Vector2[] cellBasePositions = new Vector2[0];

            private void Awake()
            {
                rectTransform = GetComponent<RectTransform>();
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                basePosition = rectTransform.anchoredPosition;
                CacheCells();
            }

            public void Play(FaceTurnAxis axis, int direction, Action onHalfTurn, Action onComplete)
            {
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }

                if (canvasGroup == null)
                {
                    canvasGroup = GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    }
                }

                if (animation != null)
                {
                    StopCoroutine(animation);
                }

                CacheCells();
                animation = StartCoroutine(Animate(axis, direction, onHalfTurn, onComplete));
            }

            private IEnumerator Animate(FaceTurnAxis axis, int direction, Action onHalfTurn, Action onComplete)
            {
                const float halfDuration = 0.18f;
                float turnSign = direction >= 0 ? 1f : -1f;
                Vector2 exitOffset = axis == FaceTurnAxis.Horizontal
                    ? new Vector2(-112f * turnSign, 0f)
                    : new Vector2(0f, -96f * turnSign);
                Vector2 enterOffset = -exitOffset;
                Vector3 exitScale = axis == FaceTurnAxis.Horizontal
                    ? new Vector3(0.34f, 0.92f, 1f)
                    : new Vector3(0.92f, 0.34f, 1f);
                Quaternion exitRotation = axis == FaceTurnAxis.Horizontal
                    ? Quaternion.Euler(0f, -54f * turnSign, 3f * turnSign)
                    : Quaternion.Euler(54f * turnSign, 0f, -3f * turnSign);

                basePosition = rectTransform.anchoredPosition;
                yield return AnimateFace(
                    basePosition,
                    basePosition + exitOffset,
                    Quaternion.identity,
                    exitRotation,
                    Vector3.one,
                    exitScale,
                    1f,
                    0.45f,
                    halfDuration);
                onHalfTurn?.Invoke();

                Quaternion enterRotation = Quaternion.Inverse(exitRotation);
                rectTransform.anchoredPosition = basePosition + enterOffset;
                rectTransform.localRotation = enterRotation;
                rectTransform.localScale = exitScale;
                canvasGroup.alpha = 0.45f;

                yield return AnimateFace(
                    basePosition + enterOffset,
                    basePosition,
                    enterRotation,
                    Quaternion.identity,
                    exitScale,
                    Vector3.one,
                    0.45f,
                    1f,
                    halfDuration);

                rectTransform.anchoredPosition = basePosition;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
                canvasGroup.alpha = 1f;
                ResetCells();
                animation = null;
                onComplete?.Invoke();
            }

            private IEnumerator AnimateFace(
                Vector2 fromPosition,
                Vector2 toPosition,
                Quaternion fromRotation,
                Quaternion toRotation,
                Vector3 fromScale,
                Vector3 toScale,
                float fromAlpha,
                float toAlpha,
                float duration)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = SmoothStep(t);
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, eased);
                    rectTransform.localRotation = Quaternion.Slerp(fromRotation, toRotation, eased);
                    rectTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                    canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                    AnimateCells(eased);
                    yield return null;
                }

                rectTransform.anchoredPosition = toPosition;
                rectTransform.localRotation = toRotation;
                rectTransform.localScale = toScale;
                canvasGroup.alpha = toAlpha;
                AnimateCells(1f);
            }

            private static float SmoothStep(float t)
            {
                return t * t * (3f - (2f * t));
            }

            private void CacheCells()
            {
                int childCount = rectTransform != null ? rectTransform.childCount : 0;
                cells = new RectTransform[childCount];
                cellBasePositions = new Vector2[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    cells[i] = rectTransform.GetChild(i) as RectTransform;
                    if (cells[i] != null)
                    {
                        cellBasePositions[i] = cells[i].anchoredPosition;
                    }
                }
            }

            private void AnimateCells(float t)
            {
                float wave = Mathf.Sin(t * Mathf.PI);
                for (int i = 0; i < cells.Length; i++)
                {
                    RectTransform cell = cells[i];
                    if (cell == null)
                    {
                        continue;
                    }

                    int row = i / 3;
                    int col = i % 3;
                    float depth = (col - 1) * 8f;
                    float verticalLift = (1 - row) * 5f;
                    cell.anchoredPosition = cellBasePositions[i] + new Vector2(depth * wave, verticalLift * wave);
                    float scale = 1f - (0.08f * wave) + (0.025f * (2 - col) * wave);
                    cell.localScale = new Vector3(scale, scale, 1f);
                }
            }

            private void ResetCells()
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    RectTransform cell = cells[i];
                    if (cell == null)
                    {
                        continue;
                    }

                    cell.anchoredPosition = cellBasePositions[i];
                    cell.localScale = Vector3.one;
                }
            }
        }

        private sealed class ButtonPulseHint : MonoBehaviour
        {
            private Coroutine pulseRoutine;
            private Vector3 baseScale = Vector3.one;

            public void Play()
            {
                if (pulseRoutine != null)
                {
                    return;
                }

                baseScale = transform.localScale;
                pulseRoutine = StartCoroutine(Pulse());
            }

            public void Stop()
            {
                if (pulseRoutine != null)
                {
                    StopCoroutine(pulseRoutine);
                    pulseRoutine = null;
                }

                transform.localScale = baseScale;
            }

            private void OnDisable()
            {
                Stop();
            }

            private IEnumerator Pulse()
            {
                const float speed = 3.8f;
                const float amount = 0.08f;
                while (true)
                {
                    float wave = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
                    float scale = 1f + (wave * amount);
                    transform.localScale = baseScale * scale;
                    yield return null;
                }
            }
        }
    }
}
