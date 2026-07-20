using System;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Save;
using CubeChallenge3D.Solver.Model;
using CubeChallenge3D.Solver.Services;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Solver;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.Cube.Debugging
{
    public sealed class CubeStateDebugUI : MonoBehaviour
    {
        private static readonly CubeColor[] Palette =
        {
            CubeColor.White,
            CubeColor.Yellow,
            CubeColor.Red,
            CubeColor.Orange,
            CubeColor.Blue,
            CubeColor.Green
        };

        private static readonly string[] FaceLabels = { "U", "R", "F", "D", "L", "B" };
        private static readonly int[] LogicalToVisibleFace = { 0, 4, 2, 3, 1, 5 };

        private SolverInputState inputState;
        private SolverInputCube3DView inputCube;
        private CubeController cubeController;
        private CubeViewOrbitController orbitController;
        private CubeControlModeController controlModeController;
        private Canvas inputCanvas;
        private Canvas playCanvas;
        private Text inputStatus;
        private Text playStatus;
        private CubeState sourceState;

        private void Start()
        {
            inputState = SolverInputState.CreateEmpty();
            BuildInputUi();
            BuildPlayRuntime();
            ShowInput();
        }

        private void Update()
        {
            if (playCanvas == null || !playCanvas.enabled || playStatus == null || cubeController == null)
            {
                return;
            }

            string mode = orbitController != null ? orbitController.CurrentMode.ToString() : "-";
            string last = cubeController.LastMove.HasValue ? cubeController.LastMove.Value.ToString() : "-";
            playStatus.text =
                $"Mode: {mode}  |  Moves: {cubeController.UserMoveCount}  |  Last: {last}\n" +
                $"Solved: {cubeController.CurrentState != null && cubeController.CurrentState.IsSolved()}  |  " +
                "Middle slices: M / E / S";
        }

        private void BuildInputUi()
        {
            inputCanvas = RuntimeUiFactory.CreateCanvas(transform, "DiagnosticsCubeStateInputCanvas", 1800);
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                inputCanvas.transform,
                "InputPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(920f, 1320f));

            Text title = RuntimeUiFactory.CreateText(
                panel,
                "Title",
                "DEV Cube State Coordinate Check",
                34,
                TextAnchor.UpperCenter);
            SetTopRect(title.rectTransform, -34f, 54f);

            Text guide = RuntimeUiFactory.CreateText(
                panel,
                "Guide",
                "Paint all 6 faces, then open the same state as a freely playable 3D cube.",
                18,
                TextAnchor.UpperCenter);
            SetTopRect(guide.rectTransform, -88f, 48f);

            inputCube = new GameObject("DebugSolverInputCube").AddComponent<SolverInputCube3DView>();
            inputCube.Initialize(
                panel,
                inputState,
                () => (CubeColor)inputState.selectedColorIndex,
                _ => RefreshInputStatus(),
                RefreshInputStatus);

            RectTransform cubeRect = inputCube.GetComponent<RectTransform>();
            cubeRect.anchoredPosition = new Vector2(0f, -270f);
            cubeRect.sizeDelta = new Vector2(430f, 430f);

            for (int i = 0; i < FaceLabels.Length; i++)
            {
                int faceIndex = i;
                Button button = RuntimeUiFactory.CreateButton(
                    panel,
                    $"Face_{FaceLabels[i]}",
                    FaceLabels[i],
                    new Vector2(-325f + (130f * i), 730f),
                    new Vector2(112f, 54f));
                button.onClick.AddListener(
                    () => inputCube.RotateToFace(faceIndex, LogicalToVisibleFace[faceIndex]));
            }

            for (int i = 0; i < Palette.Length; i++)
            {
                CubeColor color = Palette[i];
                Button button = RuntimeUiFactory.CreateButton(
                    panel,
                    $"Color_{color}",
                    color.ToString(),
                    new Vector2(-325f + (130f * i), 250f),
                    new Vector2(112f, 58f));
                button.GetComponent<Image>().color = ToUnityColor(color);
                button.onClick.AddListener(() =>
                {
                    inputState.selectedColorIndex = (int)color;
                    RefreshInputStatus();
                });
            }

            Button clear = RuntimeUiFactory.CreateButton(
                panel, "Clear", "Clear", new Vector2(-230f, 120f), new Vector2(180f, 58f));
            Button solved = RuntimeUiFactory.CreateButton(
                panel, "Solved", "Reset Solved", Vector2.zero + new Vector2(0f, 120f), new Vector2(210f, 58f));
            Button play = RuntimeUiFactory.CreateButton(
                panel, "Play3D", "Open 3D Check", new Vector2(240f, 120f), new Vector2(230f, 58f));
            clear.onClick.AddListener(() => SetInputState(SolverInputState.CreateEmpty()));
            solved.onClick.AddListener(() => SetInputState(SolverInputState.CreateSolved()));
            play.onClick.AddListener(Open3DCheck);

            inputStatus = RuntimeUiFactory.CreateText(
                panel,
                "Status",
                string.Empty,
                18,
                TextAnchor.UpperCenter);
            inputStatus.rectTransform.anchorMin = new Vector2(0f, 0f);
            inputStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
            inputStatus.rectTransform.pivot = new Vector2(0.5f, 0f);
            inputStatus.rectTransform.anchoredPosition = new Vector2(0f, 28f);
            inputStatus.rectTransform.sizeDelta = new Vector2(-80f, 70f);
            RefreshInputStatus();
        }

        private void BuildPlayRuntime()
        {
            GameObject runtime = new GameObject("DebugCubeRuntime");
            runtime.transform.SetParent(transform, false);
            cubeController = runtime.AddComponent<CubeController>();
            orbitController = runtime.AddComponent<CubeViewOrbitController>();
            orbitController.Initialize(cubeController);

            controlModeController = runtime.AddComponent<CubeControlModeController>();
            controlModeController.Initialize(cubeController, orbitController, new SettingsStore());
            controlModeController.SetDragControlMode();

            CubeFaceDragInput dragInput = runtime.AddComponent<CubeFaceDragInput>();
            dragInput.Initialize(cubeController, orbitController, controlModeController);
            cubeController.InitializeSolved();

            playCanvas = RuntimeUiFactory.CreateCanvas(transform, "DiagnosticsCubeStatePlayCanvas", 1800);
            RectTransform toolbar = RuntimeUiFactory.CreatePanel(
                playCanvas.transform,
                "Toolbar",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 30f),
                new Vector2(1000f, 280f));

            AddPlayButton(toolbar, "Input", "Edit 6 Faces", -410f, ShowInput);
            AddPlayButton(toolbar, "Mode", "View / Solve", -205f, ToggleViewMode);
            AddPlayButton(toolbar, "Undo", "Undo", 0f, () => cubeController.Undo());
            AddPlayButton(toolbar, "Reset", "Reset State", 205f, ResetSourceState);
            AddPlayButton(toolbar, "ViewReset", "Reset View", 410f, () => orbitController.ResetViewRotation());

            AddMiddleButton(toolbar, "M", -375f, CubeAxis.X, 1);
            AddMiddleButton(toolbar, "MPrime", -225f, CubeAxis.X, -1);
            AddMiddleButton(toolbar, "E", -75f, CubeAxis.Y, 1);
            AddMiddleButton(toolbar, "EPrime", 75f, CubeAxis.Y, -1);
            AddMiddleButton(toolbar, "S", 225f, CubeAxis.Z, -1);
            AddMiddleButton(toolbar, "SPrime", 375f, CubeAxis.Z, 1);

            playStatus = RuntimeUiFactory.CreateText(
                toolbar,
                "PlayStatus",
                string.Empty,
                18,
                TextAnchor.UpperCenter);
            playStatus.rectTransform.anchorMin = new Vector2(0f, 1f);
            playStatus.rectTransform.anchorMax = new Vector2(1f, 1f);
            playStatus.rectTransform.pivot = new Vector2(0.5f, 1f);
            playStatus.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            playStatus.rectTransform.sizeDelta = new Vector2(-40f, 76f);
        }

        private void Open3DCheck()
        {
            if (!SolverInputSerializer.TryToPlaybackCubeState(inputState, out CubeState state, out string error))
            {
                inputStatus.text = $"Cannot open 3D check:\n{error}";
                return;
            }

            sourceState = state.Clone();
            cubeController.SetStateInstant(sourceState, true);
            cubeController.SetViewVisible(true);
            cubeController.SetUserInputEnabled(true);
            orbitController.SetSolveMode();
            inputCanvas.enabled = false;
            playCanvas.enabled = true;
        }

        private void ShowInput()
        {
            if (inputCanvas != null)
            {
                inputCanvas.enabled = true;
            }

            if (playCanvas != null)
            {
                playCanvas.enabled = false;
            }

            if (cubeController != null)
            {
                cubeController.SetViewVisible(false);
                cubeController.SetUserInputEnabled(false);
            }
        }

        private void ResetSourceState()
        {
            if (sourceState == null || cubeController.IsBusy)
            {
                return;
            }

            cubeController.SetStateInstant(sourceState, true);
        }

        private void ToggleViewMode()
        {
            if (orbitController.CurrentMode == CubeInteractionMode.View)
            {
                orbitController.SetSolveMode();
            }
            else
            {
                orbitController.SetViewMode();
            }
        }

        private void AddMiddleButton(
            RectTransform parent,
            string name,
            float x,
            CubeAxis axis,
            int axisTurns)
        {
            string label = axisTurns < 0 ? $"{name[0]}'" : name[0].ToString();
            Button button = RuntimeUiFactory.CreateButton(
                parent,
                name,
                label,
                new Vector2(x, 82f),
                new Vector2(130f, 54f));
            button.onClick.AddListener(() =>
            {
                if (!cubeController.IsBusy && cubeController.UserInputEnabled)
                {
                    cubeController.ApplyUserMove(CubeMove.CreateLayer(axis, 0, axisTurns));
                }
            });
        }

        private static void AddPlayButton(
            RectTransform parent,
            string name,
            string label,
            float x,
            UnityEngine.Events.UnityAction action)
        {
            Button button = RuntimeUiFactory.CreateButton(
                parent,
                name,
                label,
                new Vector2(x, 154f),
                new Vector2(190f, 58f));
            button.onClick.AddListener(action);
        }

        private void SetInputState(SolverInputState next)
        {
            inputState = next;
            inputCube.SetState(inputState);
            inputCube.ResetToFront();
            RefreshInputStatus();
        }

        private void RefreshInputStatus()
        {
            if (inputStatus == null || inputState == null)
            {
                return;
            }

            int filled = 0;
            foreach (int value in inputState.faceletColorIndexes)
            {
                if ((CubeColor)value != CubeColor.None)
                {
                    filled++;
                }
            }

            inputStatus.text =
                $"Selected: {(CubeColor)inputState.selectedColorIndex}  |  Filled: {filled}/54\n" +
                "This scene is editor/debug only and is not included in the app build.";
        }

        private static void SetTopRect(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-80f, height);
        }

        private static Color ToUnityColor(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return Color.white;
                case CubeColor.Yellow: return Color.yellow;
                case CubeColor.Red: return Color.red;
                case CubeColor.Orange: return new Color(1f, 0.35f, 0f);
                case CubeColor.Blue: return new Color(0.05f, 0.2f, 1f);
                case CubeColor.Green: return new Color(0f, 0.75f, 0.15f);
                default: return Color.magenta;
            }
        }
    }
}
