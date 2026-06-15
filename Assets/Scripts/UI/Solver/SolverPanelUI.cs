using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        private readonly RectTransform gridRoot;
        private readonly RectTransform paletteRoot;
        private SolverInputCube3DView inputCube3DView;
        private SolverFaceGuideView faceGuideView;
        private SolverPlaybackPanelUI playbackPanel;
        private Button play3DButton;
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

        private const bool ShowDebugPanel = false;
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

        public SolverPanelUI(Transform parent)
        {
            state = SolverInputState.CreateEmpty();

            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "SolverCanvas", 1510);
            root = canvas.gameObject;
            solverCanvas = canvas;
            solverRaycaster = root.GetComponent<GraphicRaycaster>();
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "SolverPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(920f, 1040f));

            AddDragBar(panel);
            AddResizeHandle(panel);

            titleText = RuntimeUiFactory.CreateText(panel, "Title", "Manual Solver", 30, TextAnchor.UpperCenter);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -56f);
            titleText.rectTransform.sizeDelta = new Vector2(-100f, 42f);
            titleText.gameObject.SetActive(false);

            Button manualButton = RuntimeUiFactory.CreateButton(panel, "ManualSolverButton", "Manual Solver", new Vector2(-250f, 944f), new Vector2(220f, 50f));
            Button learnButton = RuntimeUiFactory.CreateButton(panel, "LearnBasicsButton", "Learn Basics", new Vector2(0f, 944f), new Vector2(220f, 50f));
            Button backButton = RuntimeUiFactory.CreateButton(panel, "BackButton", "Back", new Vector2(250f, 944f), new Vector2(170f, 50f));
            manualButton.onClick.AddListener(() => SetLearnMode(false));
            learnButton.onClick.AddListener(() => SetLearnMode(true));
            backButton.onClick.AddListener(Hide);

            faceGuideView = new GameObject("SolverFaceGuideView").AddComponent<SolverFaceGuideView>();
            faceGuideView.Initialize(panel);

            currentFaceText = RuntimeUiFactory.CreateText(panel, "CurrentFace", string.Empty, 24, TextAnchor.UpperCenter);
            currentFaceText.rectTransform.anchorMin = new Vector2(0f, 1f);
            currentFaceText.rectTransform.anchorMax = new Vector2(1f, 1f);
            currentFaceText.rectTransform.pivot = new Vector2(0.5f, 1f);
            currentFaceText.rectTransform.anchoredPosition = new Vector2(0f, -122f);
            currentFaceText.rectTransform.sizeDelta = new Vector2(-260f, 34f);

            instructionText = RuntimeUiFactory.CreateText(panel, "InputInstruction", "Select a color, then tap stickers on the current face.", 15, TextAnchor.UpperCenter);
            instructionText.rectTransform.anchorMin = new Vector2(0f, 1f);
            instructionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            instructionText.rectTransform.pivot = new Vector2(0.5f, 1f);
            instructionText.rectTransform.anchoredPosition = new Vector2(0f, -158f);
            instructionText.rectTransform.sizeDelta = new Vector2(-260f, 30f);

            CreateFaceButtons(panel);

            gridRoot = CreateGridRoot(panel);
            CreateCellButtons();
            gridRoot.gameObject.SetActive(false);

            inputCube3DView = new GameObject("SolverInputCube3DView").AddComponent<SolverInputCube3DView>();
            inputCube3DView.Initialize(
                panel,
                state,
                () => (CubeColor)state.selectedColorIndex,
                OnInputCubeFaceChanged,
                OnInputCubeChanged);

            paletteRoot = CreatePaletteRoot(panel);
            CreatePaletteButtons();

            Button previousButton = RuntimeUiFactory.CreateButton(panel, "PreviousFaceButton", "Previous", new Vector2(-370f, 560f), new Vector2(150f, 50f));
            Button nextButton = RuntimeUiFactory.CreateButton(panel, "NextFaceButton", "Next", new Vector2(370f, 560f), new Vector2(150f, 50f));
            previousButton.onClick.AddListener(PreviousFace);
            nextButton.onClick.AddListener(NextFace);

            validationText = RuntimeUiFactory.CreateText(panel, "ValidationSummary", string.Empty, 15, TextAnchor.UpperCenter);
            validationText.rectTransform.anchorMin = new Vector2(0f, 0f);
            validationText.rectTransform.anchorMax = new Vector2(1f, 0f);
            validationText.rectTransform.pivot = new Vector2(0.5f, 0f);
            validationText.rectTransform.anchoredPosition = new Vector2(0f, 168f);
            validationText.rectTransform.sizeDelta = new Vector2(-80f, 36f);

            Button validateButton = RuntimeUiFactory.CreateButton(panel, "ValidateButton", "Validate", new Vector2(-300f, 92f), new Vector2(160f, 48f));
            Button resetButton = RuntimeUiFactory.CreateButton(panel, "ResetSolvedButton", "Reset Solved", new Vector2(-112f, 92f), new Vector2(180f, 48f));
            Button clearButton = RuntimeUiFactory.CreateButton(panel, "ClearButton", "Clear", new Vector2(86f, 92f), new Vector2(140f, 48f));
            Button saveButton = RuntimeUiFactory.CreateButton(panel, "SaveButton", "Save", new Vector2(254f, 92f), new Vector2(140f, 48f));
            Button loadButton = RuntimeUiFactory.CreateButton(panel, "LoadButton", "Load", new Vector2(-270f, 34f), new Vector2(150f, 48f));
            Button solveButton = RuntimeUiFactory.CreateButton(panel, "SolveButton", "Solve", new Vector2(-92f, 34f), new Vector2(150f, 48f));
            play3DButton = RuntimeUiFactory.CreateButton(panel, "Play3DButton", "Play 3D", new Vector2(86f, 34f), new Vector2(150f, 48f));
            Button debugButton = RuntimeUiFactory.CreateButton(panel, "SolverDebugButton", "Debug", new Vector2(264f, 34f), new Vector2(150f, 48f));
            debugButton.gameObject.SetActive(ShowDebugPanel && (Application.isEditor || Debug.isDebugBuild));
            Button closeButton = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Close", new Vector2(264f, 20f), new Vector2(150f, 48f));
            validateButton.onClick.AddListener(Validate);
            resetButton.onClick.AddListener(ResetSolved);
            clearButton.onClick.AddListener(ClearInput);
            saveButton.onClick.AddListener(Save);
            loadButton.onClick.AddListener(Load);
            solveButton.onClick.AddListener(Solve);
            debugButton.onClick.AddListener(ShowSolverDebugReport);
            play3DButton.onClick.AddListener(ShowPlayback);
            closeButton.onClick.AddListener(Hide);
            closeButton.gameObject.SetActive(false);
            manualActionButtons.Add(previousButton);
            manualActionButtons.Add(nextButton);
            manualActionButtons.Add(validateButton);
            manualActionButtons.Add(resetButton);
            manualActionButtons.Add(clearButton);
            manualActionButtons.Add(saveButton);
            manualActionButtons.Add(loadButton);
            manualActionButtons.Add(solveButton);
            manualActionButtons.Add(play3DButton);
            manualActionButtons.Add(debugButton);

            statusText = RuntimeUiFactory.CreateText(panel, "Status", string.Empty, 22, TextAnchor.UpperCenter);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0f, 206f);
            statusText.rectTransform.sizeDelta = new Vector2(-70f, 34f);

            resultText = RuntimeUiFactory.CreateText(panel, "SolverResult", "Result: Not solved yet.", 15, TextAnchor.UpperLeft);
            resultText.rectTransform.anchorMin = new Vector2(0f, 0f);
            resultText.rectTransform.anchorMax = new Vector2(1f, 0f);
            resultText.rectTransform.pivot = new Vector2(0.5f, 0f);
            resultText.rectTransform.anchoredPosition = new Vector2(0f, 242f);
            resultText.rectTransform.sizeDelta = new Vector2(-80f, 102f);
            resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultText.verticalOverflow = VerticalWrapMode.Truncate;

            debugText = RuntimeUiFactory.CreateText(panel, "DebugText", string.Empty, 13, TextAnchor.UpperLeft);
            debugText.rectTransform.anchorMin = new Vector2(0f, 0f);
            debugText.rectTransform.anchorMax = new Vector2(1f, 0f);
            debugText.rectTransform.pivot = new Vector2(0.5f, 0f);
            debugText.rectTransform.anchoredPosition = new Vector2(0f, 352f);
            debugText.rectTransform.sizeDelta = new Vector2(-80f, 28f);
            debugText.gameObject.SetActive(ShowDebugPanel);

            playbackPanel = new SolverPlaybackPanelUI(root.transform);
            Hide();
        }

        public void Show()
        {
            root.SetActive(true);
            solverCanvas.enabled = true;
            if (solverRaycaster != null)
            {
                solverRaycaster.enabled = true;
            }

            ResetFaceNavigator();
            SetLearnMode(false);
        }

        public void Hide()
        {
            if (solverRaycaster != null)
            {
                solverRaycaster.enabled = false;
            }

            solverCanvas.enabled = false;
            root.SetActive(false);
            Closed?.Invoke();
        }

        private void SetLearnMode(bool enabled)
        {
            learnMode = enabled;
            titleText.text = learnMode ? "Learn Basics" : "Manual Solver";
            SetManualObjectsActive(!learnMode);
            statusText.text = learnMode
                ? "Learn faces, turns, and notation from the Solver & Learn menu."
                : "Select a color, then enter the stickers.";
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
            validationText.gameObject.SetActive(active);
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
                currentFaceText.text = $"Enter {InternalFaceDisplayNames[currentFrontFaceIndex]} Face";
            }

            if (instructionText != null)
            {
                instructionText.text = "Select a color, then tap the 9 stickers.";
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
            Refresh();
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
            if (!TryCreateRequest(out SolverRequest request, out string error))
            {
                statusText.text = $"Invalid cube input.\n{error}";
                resultText.text = BuildFailureResultText("Invalid cube input", error, null);
                validationText.text = BuildValidationSummary();
                debugText.text = BuildDebugText();
                return;
            }

            SolverValidationResult result = solverEngine.Validate(request);
            statusText.text = result.isValid
                ? "Cube input is valid."
                : result.userMessage;
            resultText.text = result.isValid
                ? "Status: Validation passed\nReady to solve."
                : BuildFailureResultText("Invalid cube", result.userMessage, result.errorCode);
            validationText.text = BuildValidationSummary();
            debugText.text = BuildDebugText();
        }

        private void ResetSolved()
        {
            state = SolverInputState.CreateSolved();
            ResetFaceNavigator();
            store.Save(state);
            inputCube3DView?.SetState(state);
            inputCube3DView?.ResetToFront();
            lastSolution = null;
            play3DButton.interactable = false;
            statusText.text = "Reset to solved cube.";
            resultText.text = "Status: Cube is already solved\nMoves: 0\nSolution: No moves needed";
            Refresh();
        }

        private void ClearInput()
        {
            state.EnsureShape();
            for (int i = 0; i < SolverInputState.FaceletCount; i++)
            {
                state.faceletColorIndexes[i] = (int)CubeColor.None;
            }

            lastSolution = null;
            play3DButton.interactable = false;
            statusText.text = "Input cleared.";
            resultText.text = "Result: Not solved yet.";
            inputCube3DView?.RefreshColors();
            Refresh();
        }

        private void Save()
        {
            bool saved = store.Save(state);
            statusText.text = saved ? "Solver input saved." : "Save failed.";
            Refresh();
        }

        private void Load()
        {
            state = store.Load();
            inputCube3DView?.SetState(state);
            lastSolution = null;
            play3DButton.interactable = false;
            statusText.text = "Solver input loaded.";
            resultText.text = "Result: Loaded input. Press Solve to calculate a solution.";
            Refresh();
        }

        private void Solve()
        {
            if (isSolving)
            {
                return;
            }

            if (!TryCreateRequest(out SolverRequest request, out string error))
            {
                lastSolution = null;
                play3DButton.interactable = false;
                statusText.text = $"Invalid cube input.\n{error}";
                resultText.text = BuildFailureResultText("Invalid cube input", error, null);
                Refresh();
                return;
            }

            bool alreadySolvedInput = request.faceletString == "UUUUUUUUURRRRRRRRRFFFFFFFFFDDDDDDDDDLLLLLLLLLBBBBBBBBB";
            if (!IsSolverUsageBypassed() && !alreadySolvedInput && usageStore.RemainingFreeUses <= 0 && inventoryStore.SolverTickets <= 0)
            {
                lastSolution = null;
                play3DButton.interactable = false;
                statusText.text = "No solver uses available.\nUse a Solver Ticket or earn more uses later.";
                resultText.text = "Status: Solver unavailable\nNo free solver uses or solver tickets are available.";
                Refresh();
                return;
            }

            isSolving = true;
            statusText.text = "Solving...";
            resultText.text = "Status: Solving...";
            SolverValidationResult validation = solverEngine.Validate(request);
            if (!validation.isValid)
            {
                statusText.text = validation.userMessage;
                resultText.text = BuildFailureResultText("Invalid cube", validation.userMessage, validation.errorCode);
                validationText.text = BuildValidationSummary();
                if (ShowDebugPanel)
                {
                    debugText.text = string.Join("\n", validation.details);
                }

                isSolving = false;
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
                string usageMessage = ConsumeSolverUseIfNeeded(result.moveCount);
                statusText.text = $"Solver status: {result.message}";
                resultText.text = BuildSuccessResultText(result, usageMessage);
            }
            else if (result.errorCode == SolverErrorCode.SolverNotConnected || result.errorCode == SolverErrorCode.SolverEngineNotImplemented)
            {
                lastSolution = null;
                play3DButton.interactable = false;
                statusText.text = $"{result.message}\nFree solver uses today: {usageStore.RemainingFreeUses} | Solver tickets: {inventoryStore.SolverTickets}";
                resultText.text = BuildFailureResultText("Solver not connected", result.message, result.errorCode);
            }
            else if (result.isValidCube)
            {
                lastSolution = null;
                play3DButton.interactable = false;
                statusText.text = result.errorCode == SolverErrorCode.Timeout
                    ? "The current solver timed out."
                    : "This cube is valid, but the current solver could not find a solution.";
                resultText.text = BuildFailureResultText("Solution not found", FriendlySolverMessage(result), result.errorCode);
            }
            else
            {
                lastSolution = null;
                play3DButton.interactable = false;
                statusText.text = $"Invalid cube state.\n{result.message}\nPlease check corner and edge colors.";
                resultText.text = BuildFailureResultText("Invalid cube state", FriendlySolverMessage(result), result.errorCode);
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
        }

        private void ShowPlayback()
        {
            if (lastSolution == null)
            {
                statusText.text = "No solver solution available.";
                return;
            }

            if (lastSolution.moveNotations == null || lastSolution.moveNotations.Length == 0)
            {
                statusText.text = "No moves needed. Cube is already solved.";
                return;
            }

            playbackPanel?.Show(lastSolution);
        }

        private string ConsumeSolverUseIfNeeded(int moveCount)
        {
            if (IsSolverUsageBypassed())
            {
                return "Dev mode: solver use not consumed.";
            }

            if (moveCount <= 0)
            {
                return "Solver use not consumed.";
            }

            if (usageStore.TryUseFree(System.DateTime.UtcNow))
            {
                return $"Free solver uses today: {usageStore.RemainingFreeUses}";
            }

            if (inventoryStore.TryConsume(StageAssistItemType.SolverTicket))
            {
                usageStore.AddTicketUse(System.DateTime.UtcNow);
                return $"Solver ticket used. Tickets: {inventoryStore.SolverTickets}";
            }

            return "Solution found, but no solver use was consumed.";
        }

        private static bool IsSolverUsageBypassed()
        {
            return Application.isEditor || Debug.isDebugBuild;
        }

        private static string BuildSuccessResultText(SolverResult result, string usageMessage)
        {
            if (result == null)
            {
                return "Status: Failed\nSolver returned no result.";
            }

            if (result.moveCount <= 0)
            {
                return "Status: Cube is already solved\nMoves: 0\nSolution: No moves needed";
            }

            return $"Status: Solution found\nMoves: {result.moveCount}\nSolution: {DisplaySolution(result.solutionNotation)}\nMoves list:\n{BuildMoveList(result.moveNotations)}\n{usageMessage}";
        }

        private static string BuildMoveList(string[] moveNotations)
        {
            if (moveNotations == null || moveNotations.Length == 0)
            {
                return "No moves needed";
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
                lines.Add($"... {moveNotations.Length - maxDisplayedMoves} more");
            }

            return string.Join("   ", lines);
        }

        private static string BuildFailureResultText(string status, string message, string errorCode)
        {
            string friendly = string.IsNullOrWhiteSpace(message) ? "Please check the cube input." : message;
            return string.IsNullOrWhiteSpace(errorCode)
                ? $"Status: {status}\n{friendly}"
                : $"Status: {status}\n{friendly}\nCode: {errorCode}";
        }

        private static string FriendlySolverMessage(SolverResult result)
        {
            if (result == null)
            {
                return "Solver returned no result.";
            }

            switch (result.errorCode)
            {
                case SolverErrorCode.InvalidColorCount:
                    return "Check color counts. Each color must appear 9 times.";
                case SolverErrorCode.DuplicateCenters:
                    return "Center colors must be unique.";
                case SolverErrorCode.InvalidCornerCubie:
                case SolverErrorCode.InvalidEdgeCubie:
                case SolverErrorCode.InvalidCubieState:
                case SolverErrorCode.ParityError:
                case SolverErrorCode.TwistError:
                case SolverErrorCode.FlipError:
                    return "Invalid cube state. This cube cannot be solved from normal turns.";
                case SolverErrorCode.Timeout:
                    return "The current solver timed out.";
                case SolverErrorCode.CurrentSolverLimitation:
                case SolverErrorCode.SolutionNotFound:
                    return "This cube is valid, but the current solver could not find a solution. A stronger solver engine is required for this pattern.";
                case SolverErrorCode.SolverNotConnected:
                case SolverErrorCode.HighPerformanceEngineNotAvailable:
                case SolverErrorCode.SolverEngineNotImplemented:
                    return "Solver engine is not connected.";
                default:
                    return string.IsNullOrWhiteSpace(result.message) ? "Solver failed." : result.message;
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
            statusText.text = "Solver debug report copied and saved as the latest debug case.";
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
            return string.IsNullOrWhiteSpace(notation) ? "(none)" : notation;
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
                ? $"Filled: {filled}/54 | Status: Ready"
                : $"Filled: {filled}/54 | Status: {FirstValidationMessage(result)}";
        }

        private static string FirstValidationMessage(SolverInputValidationResult result)
        {
            if (result == null || result.messages == null || result.messages.Count == 0)
            {
                return "Check input";
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
    }
}
