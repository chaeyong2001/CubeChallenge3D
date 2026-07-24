using System;
using System.Collections;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Cube.View;
using CubeChallenge3D.Solver.Model;
using CubeChallenge3D.Solver.Services;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Solver.Playback
{
    public sealed class SolverPlaybackPanelUI
    {
        private GameObject root;
        private Canvas canvas;
        private RawImage cubeImage;
        private Text titleText;
        private Text statusText;
        private Text moveText;
        private Text progressText;
        private Text guideText;
        private Button previousButton;
        private Button nextButton;
        private Button autoButton;
        private Button pauseButton;
        private Button resetButton;
        private Button backButton;
        private bool embeddedMode;
        private Action embeddedBackAction;
        private bool useCompactEmbeddedLayout;

        private RenderTexture renderTexture;
        private GameObject sceneRoot;
        private Camera renderCamera;
        private CubeController cubeController;
        private CubeState sourceState;
        private string currentOrientationMode = string.Empty;
        private Quaternion initialViewRotation = Quaternion.identity;
        private Vector3 initialCameraLocalPosition = new Vector3(4f, 4f, 6f);
        private Vector3 completionCameraLocalPosition;
        private bool hasCompletionCameraPosition;
        private bool learningHighlightsEnabled;
        private bool showFaceLabels = true;
        private Vector3Int highlightedCubiePosition;
        private Vector3Int highlightedSlotPosition;
        private Material targetHighlightMaterial;
        private Material slotHighlightMaterial;
        private readonly List<CubeMove> moves = new List<CubeMove>();
        private readonly List<string> moveLabels = new List<string>();
        private readonly List<string> moveDescriptions = new List<string>();
        private string baseGuideText = string.Empty;
        private string completionMessage = string.Empty;
        private string completionGuideMessage = string.Empty;
        private int moveIndex;
        private bool autoPlaying;
        private Coroutine autoRoutine;
        private Coroutine cameraTransitionRoutine;
        private const float CameraTransitionDuration = 0.7f;
        private MonoBehaviourHost host;

        private struct EmbeddedLayoutConfig
        {
            public Vector2 TitlePosition;
            public Vector2 TitleSize;
            public Vector2 CubeAreaPosition;
            public Vector2 CubeAreaSize;
            public Vector2 CurrentMoveTextPosition;
            public Vector2 CurrentMoveTextSize;
            public Vector2 MoveProgressTextPosition;
            public Vector2 MoveProgressTextSize;
            public Vector2 PlaybackButtonSize;
            public Vector2 PreviousButtonPosition;
            public Vector2 NextButtonPosition;
            public Vector2 AutoPlayButtonPosition;
            public Vector2 BackButtonPosition;

            public static EmbeddedLayoutConfig Default => new EmbeddedLayoutConfig
            {
                TitlePosition = new Vector2(0f, -38f),
                TitleSize = new Vector2(620f, 48f),
                CubeAreaPosition = new Vector2(0f, -110f),
                CubeAreaSize = new Vector2(860f, 860f),
                CurrentMoveTextPosition = new Vector2(0f, -1010f),
                CurrentMoveTextSize = new Vector2(820f, 54f),
                MoveProgressTextPosition = new Vector2(0f, -1065f),
                MoveProgressTextSize = new Vector2(820f, 40f),
                PlaybackButtonSize = new Vector2(430f, 94f),
                PreviousButtonPosition = new Vector2(-240f, -1162f),
                NextButtonPosition = new Vector2(240f, -1162f),
                AutoPlayButtonPosition = new Vector2(-240f, -1282f),
                BackButtonPosition = new Vector2(240f, -1282f)
            };

            public static EmbeddedLayoutConfig CompactLesson => new EmbeddedLayoutConfig
            {
                TitlePosition = new Vector2(0f, -24f),
                TitleSize = new Vector2(720f, 48f),
                CubeAreaPosition = new Vector2(0f, -86f),
                CubeAreaSize = new Vector2(760f, 570f),
                CurrentMoveTextPosition = new Vector2(0f, -668f),
                CurrentMoveTextSize = new Vector2(820f, 50f),
                MoveProgressTextPosition = new Vector2(0f, -718f),
                MoveProgressTextSize = new Vector2(820f, 38f),
                PlaybackButtonSize = new Vector2(360f, 76f),
                PreviousButtonPosition = new Vector2(-205f, -805f),
                NextButtonPosition = new Vector2(205f, -805f),
                AutoPlayButtonPosition = new Vector2(-205f, -888f),
                BackButtonPosition = new Vector2(205f, -888f)
            };
        }

        public SolverPlaybackPanelUI(
            Transform parent,
            string panelTitle = "Solver Playback",
            string orientationGuide = "Follow using the same orientation you entered. Keep Front, Top, and Right fixed.")
        {
            embeddedMode = false;
            canvas = RuntimeUiFactory.CreateCanvas(parent, "SolverPlaybackCanvas", 1520);
            root = canvas.gameObject;
            host = root.AddComponent<MonoBehaviourHost>();

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "SolverPlaybackPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 1040f));

            InitializeUi(panel, panelTitle, orientationGuide);
            Hide();
        }

        public SolverPlaybackPanelUI(
            RectTransform parent,
            Action backAction,
            string panelTitle = "Solver Playback",
            string orientationGuide = "Follow using the same orientation you entered. Keep Front, Top, and Right fixed.",
            bool compactEmbeddedLayout = false)
        {
            embeddedMode = true;
            embeddedBackAction = backAction;
            useCompactEmbeddedLayout = compactEmbeddedLayout;
            root = new GameObject("SolverPlaybackInlineRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform panel = root.GetComponent<RectTransform>();
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            host = root.AddComponent<MonoBehaviourHost>();

            InitializeUi(panel, panelTitle, orientationGuide);
            Hide();
        }

        private void InitializeUi(RectTransform panel, string panelTitle, string orientationGuide)
        {
            titleText = RuntimeUiFactory.CreateText(panel, "Title", panelTitle, 32, TextAnchor.UpperCenter);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            titleText.rectTransform.sizeDelta = new Vector2(-80f, 46f);

            backButton = RuntimeUiFactory.CreateButton(panel, "BackButton", "Back", new Vector2(330f, 930f), new Vector2(150f, 54f));
            backButton.onClick.AddListener(HandleBack);

            cubeImage = CreateCubeImage(panel);

            moveText = RuntimeUiFactory.CreateText(panel, "CurrentMove", string.Empty, 36, TextAnchor.MiddleCenter);
            moveText.rectTransform.anchorMin = new Vector2(0f, 0f);
            moveText.rectTransform.anchorMax = new Vector2(1f, 0f);
            moveText.rectTransform.pivot = new Vector2(0.5f, 0f);
            moveText.rectTransform.anchoredPosition = new Vector2(0f, 368f);
            moveText.rectTransform.sizeDelta = new Vector2(-80f, 56f);

            progressText = RuntimeUiFactory.CreateText(panel, "Progress", string.Empty, 24, TextAnchor.MiddleCenter);
            progressText.rectTransform.anchorMin = new Vector2(0f, 0f);
            progressText.rectTransform.anchorMax = new Vector2(1f, 0f);
            progressText.rectTransform.pivot = new Vector2(0.5f, 0f);
            progressText.rectTransform.anchoredPosition = new Vector2(0f, 326f);
            progressText.rectTransform.sizeDelta = new Vector2(-80f, 42f);

            guideText = RuntimeUiFactory.CreateText(
                panel,
                "OrientationGuide",
                orientationGuide,
                18,
                TextAnchor.MiddleCenter);
            guideText.rectTransform.anchorMin = new Vector2(0f, 0f);
            guideText.rectTransform.anchorMax = new Vector2(1f, 0f);
            guideText.rectTransform.pivot = new Vector2(0.5f, 0f);
            guideText.rectTransform.anchoredPosition = new Vector2(0f, 282f);
            guideText.rectTransform.sizeDelta = new Vector2(-100f, 48f);

            previousButton = RuntimeUiFactory.CreateButton(panel, "PreviousMoveButton", "Previous", new Vector2(-300f, 190f), new Vector2(170f, 58f));
            nextButton = RuntimeUiFactory.CreateButton(panel, "NextMoveButton", "Next", new Vector2(-100f, 190f), new Vector2(170f, 58f));
            autoButton = RuntimeUiFactory.CreateButton(panel, "AutoPlayButton", "Auto Play", new Vector2(100f, 190f), new Vector2(170f, 58f));
            pauseButton = RuntimeUiFactory.CreateButton(panel, "PauseButton", "Pause", new Vector2(300f, 190f), new Vector2(170f, 58f));
            resetButton = RuntimeUiFactory.CreateButton(panel, "ResetPlaybackButton", "Reset", Vector2.zero, new Vector2(220f, 58f));

            previousButton.onClick.AddListener(PreviousMove);
            nextButton.onClick.AddListener(NextMove);
            autoButton.onClick.AddListener(ToggleAutoPlay);
            pauseButton.onClick.AddListener(PauseAutoPlay);
            resetButton.onClick.AddListener(ResetPlayback);

            statusText = RuntimeUiFactory.CreateText(panel, "Status", string.Empty, 22, TextAnchor.UpperCenter);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0f, 72f);
            statusText.rectTransform.sizeDelta = new Vector2(-90f, 92f);

            BuildScene();
            PlaybackOrbitDragHandle dragHandle = cubeImage.gameObject.AddComponent<PlaybackOrbitDragHandle>();
            dragHandle.Initialize(cubeController, renderCamera);

            if (embeddedMode)
            {
                ApplyEmbeddedLayout();
            }
        }

        public void Show(SolverSolution solution)
        {
            StopAutoPlay();
            StopCameraTransition();
            moves.Clear();
            moveLabels.Clear();
            moveDescriptions.Clear();
            moveIndex = 0;
            if (sceneRoot != null)
            {
                sceneRoot.SetActive(true);
            }

            if (!TryLoadSolution(solution, out string error))
            {
                root.SetActive(true);
                if (canvas != null)
                {
                    canvas.enabled = true;
                }
                statusText.text = error;
                moveText.text = "No playback";
                progressText.text = "0 / 0";
                SetButtons(false);
                return;
            }

            root.SetActive(true);
            if (canvas != null)
            {
                canvas.enabled = true;
            }
            cubeController.SetStateInstant(sourceState, true);
            ApplyCameraPosition();
            if (cubeController.ViewRoot != null)
            {
                cubeController.ViewRoot.localRotation = initialViewRotation;
            }
            if (showFaceLabels)
            {
                AddPlaybackFaceLabels();
            }
            AddLearningHighlights();
            UpdateView("Ready.");
        }

        public void SetPresentation(string title, string guide)
        {
            if (titleText != null && !string.IsNullOrWhiteSpace(title))
            {
                titleText.text = title;
            }

            if (guideText != null && !string.IsNullOrWhiteSpace(guide))
            {
                baseGuideText = guide;
                guideText.text = guide;
            }
        }

        public void SetInitialViewEuler(Vector3 euler)
        {
            initialViewRotation = Quaternion.Euler(euler);
        }

        public void SetCameraLocalPosition(Vector3 position)
        {
            initialCameraLocalPosition = position;
        }

        public void SetCompletionCameraLocalPosition(Vector3 position)
        {
            completionCameraLocalPosition = position;
            hasCompletionCameraPosition = true;
        }

        public void ClearCompletionCameraPosition()
        {
            hasCompletionCameraPosition = false;
        }

        public void SetLearningHighlights(bool enabled, Vector3Int targetCubie, Vector3Int targetSlot)
        {
            learningHighlightsEnabled = enabled;
            highlightedCubiePosition = targetCubie;
            highlightedSlotPosition = targetSlot;
        }

        public void SetShowFaceLabels(bool show)
        {
            showFaceLabels = show;
        }

        public void Hide()
        {
            StopAutoPlay();
            StopCameraTransition();
            if (canvas != null)
            {
                canvas.enabled = false;
            }
            root.SetActive(false);
            if (sceneRoot != null)
            {
                sceneRoot.SetActive(false);
            }
        }

        private void HandleBack()
        {
            if (embeddedMode)
            {
                embeddedBackAction?.Invoke();
                return;
            }

            Hide();
        }

        private RawImage CreateCubeImage(RectTransform parent)
        {
            GameObject imageObject = new GameObject("PlaybackCubeImage", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -122f);
            rect.sizeDelta = new Vector2(620f, 620f);

            RawImage image = imageObject.GetComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = true;
            return image;
        }

        private void BuildScene()
        {
            renderTexture = new RenderTexture(768, 768, 16, RenderTextureFormat.ARGB32);
            cubeImage.texture = renderTexture;

            sceneRoot = new GameObject("SolverPlaybackScene");
            sceneRoot.transform.position = new Vector3(500f, 500f, 500f);

            GameObject lightObject = new GameObject("PlaybackLight", typeof(Light));
            lightObject.transform.SetParent(sceneRoot.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            GameObject cameraObject = new GameObject("PlaybackCamera", typeof(Camera));
            cameraObject.transform.SetParent(sceneRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(4f, 4f, 6f);
            cameraObject.transform.LookAt(sceneRoot.transform.position);
            renderCamera = cameraObject.GetComponent<Camera>();
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = VisualCustomizationService.LoadSelectedTheme().backgroundColor;
            renderCamera.targetTexture = renderTexture;
            targetHighlightMaterial = CreateHighlightMaterial(new Color(1f, 0.82f, 0.05f));
            slotHighlightMaterial = CreateHighlightMaterial(new Color(0.05f, 0.9f, 1f));

            GameObject cubeObject = new GameObject("PlaybackCubeController");
            cubeObject.transform.SetParent(sceneRoot.transform, false);
            cubeController = cubeObject.AddComponent<CubeController>();
            cubeController.SetRotationDuration(0.55f);
            cubeController.SetUserInputEnabled(false);
            cubeController.MoveAnimationCompleted += HandleMoveAnimationCompleted;
            sceneRoot.SetActive(false);
        }

        private bool TryLoadSolution(SolverSolution solution, out string error)
        {
            error = string.Empty;
            if (solution == null)
            {
                error = "No solver solution available.";
                return false;
            }

            currentOrientationMode = solution.orientationMode ?? string.Empty;
            completionMessage = solution.completionMessage ?? string.Empty;
            completionGuideMessage = solution.completionGuideMessage ?? string.Empty;

            string playbackFacelets = !string.IsNullOrWhiteSpace(solution.sourceColorFaceletString)
                ? solution.sourceColorFaceletString
                : solution.sourceFaceletString;

            if (string.IsNullOrWhiteSpace(playbackFacelets))
            {
                error = "No source cube state saved for playback.";
                return false;
            }

            try
            {
                sourceState = CubeStateSerializer.FromFaceletString(playbackFacelets);
            }
            catch (Exception exception)
            {
                error = $"Cannot load source cube state.\n{exception.Message}";
                return false;
            }

            string[] notations = solution.moveNotations ?? new string[0];
            for (int i = 0; i < notations.Length; i++)
            {
                if (!CubeMove.TryParse(notations[i], out CubeMove move))
                {
                    error = $"Unsupported move notation: {notations[i]}";
                    return false;
                }

                moves.Add(SolverPlaybackMoveMapper.ToPlaybackMove(move));
                moveLabels.Add(notations[i]);
            }

            string[] descriptions = solution.moveDescriptions ?? Array.Empty<string>();
            for (int i = 0; i < notations.Length; i++)
            {
                moveDescriptions.Add(i < descriptions.Length ? descriptions[i] : string.Empty);
            }

            return true;
        }

        private void NextMove()
        {
            if (cubeController == null || cubeController.IsBusy || moveIndex >= moves.Count)
            {
                return;
            }

            StopCameraTransition();
            cubeController.ApplySystemMove(moves[moveIndex]);
            moveIndex++;
            UpdateView();
        }

        private void PreviousMove()
        {
            if (cubeController == null || cubeController.IsBusy || moveIndex <= 0)
            {
                return;
            }

            StopCameraTransition();
            moveIndex--;
            cubeController.ApplySystemMove(MoveUtility.Inverse(moves[moveIndex]));
            UpdateView();
        }

        private void ResetPlayback()
        {
            StopAutoPlay();
            StopCameraTransition();
            if (cubeController == null || cubeController.IsBusy || sourceState == null)
            {
                return;
            }

            moveIndex = 0;
            cubeController.SetStateInstant(sourceState, true);
            ApplyCameraPosition();
            if (showFaceLabels)
            {
                AddPlaybackFaceLabels();
            }
            AddLearningHighlights();
            UpdateView("Reset.");
        }

        private void StartAutoPlay()
        {
            if (autoPlaying || root == null || !root.activeSelf)
            {
                return;
            }

            autoPlaying = true;
            autoRoutine = host.StartCoroutine(AutoPlay());
            UpdateView();
        }

        private void ToggleAutoPlay()
        {
            if (autoPlaying)
            {
                PauseAutoPlay();
                return;
            }

            StartAutoPlay();
        }

        private void PauseAutoPlay()
        {
            StopAutoPlay();
            UpdateView("Paused.");
        }

        private IEnumerator AutoPlay()
        {
            while (autoPlaying && moveIndex < moves.Count)
            {
                if (!cubeController.IsBusy)
                {
                    NextMove();
                }

                yield return new WaitForSeconds(0.85f);
            }

            autoPlaying = false;
            UpdateView();
        }

        private void StopAutoPlay()
        {
            autoPlaying = false;
            if (autoRoutine != null && root != null)
            {
                host.StopCoroutine(autoRoutine);
                autoRoutine = null;
            }
        }

        private void ApplyEmbeddedLayout()
        {
            EmbeddedLayoutConfig layout = useCompactEmbeddedLayout
                ? EmbeddedLayoutConfig.CompactLesson
                : EmbeddedLayoutConfig.Default;
            SetTopAnchored(titleText.rectTransform, layout.TitlePosition, layout.TitleSize);
            SetTopAnchored(cubeImage.rectTransform, layout.CubeAreaPosition, layout.CubeAreaSize);
            SetCenterTopAnchored(moveText.rectTransform, layout.CurrentMoveTextPosition, layout.CurrentMoveTextSize);
            SetCenterTopAnchored(progressText.rectTransform, layout.MoveProgressTextPosition, layout.MoveProgressTextSize);
            SetCenterTopAnchored(previousButton.GetComponent<RectTransform>(), layout.PreviousButtonPosition, layout.PlaybackButtonSize);
            SetCenterTopAnchored(nextButton.GetComponent<RectTransform>(), layout.NextButtonPosition, layout.PlaybackButtonSize);
            SetCenterTopAnchored(autoButton.GetComponent<RectTransform>(), layout.AutoPlayButtonPosition, layout.PlaybackButtonSize);
            SetCenterTopAnchored(backButton.GetComponent<RectTransform>(), layout.BackButtonPosition, layout.PlaybackButtonSize);

            guideText.gameObject.SetActive(false);
            statusText.gameObject.SetActive(false);
            autoButton.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(false);
            resetButton.gameObject.SetActive(false);
            StyleEmbeddedButton(previousButton);
            StyleEmbeddedButton(nextButton);
            StyleEmbeddedButton(autoButton);
            StyleEmbeddedButton(backButton);
        }

        private static void SetTopAnchored(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetCenterTopAnchored(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void StyleEmbeddedButton(Button button)
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
            }
        }

        private void UpdateView(string message = null)
        {
            bool hasMoves = moves.Count > 0;
            bool busy = cubeController != null && cubeController.IsBusy;
            string current = moveIndex < moveLabels.Count ? moveLabels[moveIndex] : "-";
            string previous = moveIndex > 0 && moveIndex - 1 < moveLabels.Count ? moveLabels[moveIndex - 1] : "-";
            moveText.text = moveIndex >= moves.Count
                ? "Current: Finished"
                : $"Current: {current}";
            progressText.text = $"Move {moveIndex} / {moves.Count} | Previous: {previous}";
            if (guideText != null)
            {
                string phase = moveIndex < moveDescriptions.Count ? moveDescriptions[moveIndex] : string.Empty;
                guideText.text = string.IsNullOrWhiteSpace(phase)
                    ? baseGuideText
                    : phase;
            }

            if (!hasMoves)
            {
                statusText.text = "No moves needed. Cube is already solved.";
            }
            else if (moveIndex >= moves.Count)
            {
                if (string.Equals(currentOrientationMode, "LearnDemo", System.StringComparison.Ordinal))
                {
                    statusText.text = string.IsNullOrWhiteSpace(completionMessage)
                        ? "Demo finished."
                        : completionMessage;
                    if (guideText != null)
                    {
                        guideText.text = string.IsNullOrWhiteSpace(completionGuideMessage)
                            ? baseGuideText
                            : completionGuideMessage;
                    }
                }
                else
                {
                    bool solved = cubeController != null && cubeController.CurrentState != null && cubeController.CurrentState.IsSolved();
                    statusText.text = solved
                        ? "Finished - Cube solved."
                        : "Finished, but cube is not solved. Check solver mapping.";
                }
            }
            else
            {
                statusText.text = string.IsNullOrWhiteSpace(message)
                    ? "Use Next, Previous, or Auto Play."
                    : message;
            }

            previousButton.interactable = hasMoves && !busy && moveIndex > 0;
            nextButton.interactable = hasMoves && !busy && moveIndex < moves.Count;
            autoButton.interactable = hasMoves && !busy && moveIndex < moves.Count;
            pauseButton.interactable = autoPlaying;
            resetButton.interactable = !busy;
            SetButtonLabel(autoButton, autoPlaying ? "Pause" : "Auto Play");
        }

        private void SetButtons(bool interactable)
        {
            previousButton.interactable = interactable;
            nextButton.interactable = interactable;
            autoButton.interactable = interactable;
            pauseButton.interactable = interactable;
            resetButton.interactable = interactable;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button != null ? button.GetComponentInChildren<Text>() : null;
            if (text != null)
            {
                text.text = label;
            }
        }

        private void AddPlaybackFaceLabels()
        {
            if (cubeController == null || cubeController.CubeRoot == null)
            {
                return;
            }

            AddPlaybackFaceLabel(CubeFace.Up, "T");
            AddPlaybackFaceLabel(CubeFace.Right, "L");
            AddPlaybackFaceLabel(CubeFace.Front, "F");
        }

        private void ApplyCameraPosition()
        {
            if (renderCamera == null || sceneRoot == null)
            {
                return;
            }

            renderCamera.transform.localPosition = initialCameraLocalPosition;
            renderCamera.transform.LookAt(sceneRoot.transform.position);
        }

        private void HandleMoveAnimationCompleted(CubeMove _)
        {
            if (moveIndex >= moves.Count && hasCompletionCameraPosition)
            {
                StartCameraTransition(completionCameraLocalPosition);
            }
            else
            {
                StartCameraTransition(initialCameraLocalPosition);
            }

            UpdateView();
        }

        private void StartCameraTransition(Vector3 targetLocalPosition)
        {
            StopCameraTransition();
            if (renderCamera == null || sceneRoot == null)
            {
                return;
            }

            cameraTransitionRoutine = host.StartCoroutine(
                AnimateCameraPosition(targetLocalPosition));
        }

        private IEnumerator AnimateCameraPosition(Vector3 targetLocalPosition)
        {
            Transform cameraTransform = renderCamera.transform;
            Vector3 startLocalPosition = cameraTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < CameraTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / CameraTransitionDuration);
                float eased = normalized * normalized * (3f - (2f * normalized));
                cameraTransform.localPosition = Vector3.Lerp(
                    startLocalPosition,
                    targetLocalPosition,
                    eased);
                cameraTransform.LookAt(sceneRoot.transform.position);
                yield return null;
            }

            cameraTransform.localPosition = targetLocalPosition;
            cameraTransform.LookAt(sceneRoot.transform.position);
            cameraTransitionRoutine = null;
        }

        private void StopCameraTransition()
        {
            if (cameraTransitionRoutine == null || host == null)
            {
                return;
            }

            host.StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = null;
        }

        private void AddLearningHighlights()
        {
            if (!learningHighlightsEnabled || cubeController == null || cubeController.CubeRoot == null)
            {
                return;
            }

            CubieVisual targetCubie = FindCubie(highlightedCubiePosition);
            CubieVisual slotCubie = FindCubie(highlightedSlotPosition);
            if (targetCubie != null)
            {
                CreateWireMarker(
                    targetCubie.transform,
                    "LearnTargetEdgeHighlight",
                    Vector3.zero,
                    targetHighlightMaterial);
            }

            if (slotCubie != null)
            {
                CreateWireMarker(
                    cubeController.CubeRoot,
                    "LearnTargetSlotHighlight",
                    slotCubie.transform.localPosition,
                    slotHighlightMaterial);
            }
        }

        private CubieVisual FindCubie(Vector3Int gridPosition)
        {
            CubieVisual[] cubies = cubeController.CubeRoot.GetComponentsInChildren<CubieVisual>();
            foreach (CubieVisual cubie in cubies)
            {
                if (cubie.CurrentGridPosition == gridPosition)
                {
                    return cubie;
                }
            }

            return null;
        }

        private static void CreateWireMarker(
            Transform parent,
            string markerName,
            Vector3 localPosition,
            Material material)
        {
            Transform existing = parent.Find(markerName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject marker = new GameObject(markerName);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;

            const float half = 0.54f;
            const float length = 1.08f;
            const float thickness = 0.035f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    CreateMarkerBar(marker.transform, new Vector3(0f, y * half, x * half), new Vector3(length, thickness, thickness), material);
                    CreateMarkerBar(marker.transform, new Vector3(x * half, 0f, y * half), new Vector3(thickness, length, thickness), material);
                    CreateMarkerBar(marker.transform, new Vector3(x * half, y * half, 0f), new Vector3(thickness, thickness, length), material);
                }
            }
        }

        private static void CreateMarkerBar(
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject bar = RuntimePrimitiveFactory.CreateCube("HighlightBar");
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localScale = localScale;
            bar.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material CreateHighlightMaterial(Color color)
        {
            Shader shader = Resources.Load<Shader>("RuntimeColor");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private void AddPlaybackFaceLabel(CubeFace face, string label)
        {
            CubieVisual centerCubie = FindFaceCenterCubie(face);
            if (centerCubie == null)
            {
                return;
            }

            Transform existing = centerCubie.transform.Find($"PlaybackLabel_{label}");
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject labelObject = new GameObject($"PlaybackLabel_{label}", typeof(TextMesh));
            labelObject.transform.SetParent(centerCubie.transform, false);
            labelObject.transform.localPosition = CubeFaceletMapping.FaceNormal(face) * 0.512f;
            labelObject.transform.localRotation = GetLabelRotation(face);

            ConfigureLabelText(labelObject.GetComponent<TextMesh>(), label);
            AddBoldShadow(labelObject.transform, label, new Vector3(0.006f, 0f, 0f));
            AddBoldShadow(labelObject.transform, label, new Vector3(-0.006f, 0f, 0f));
            AddBoldShadow(labelObject.transform, label, new Vector3(0f, 0.006f, 0f));
            AddBoldShadow(labelObject.transform, label, new Vector3(0f, -0.006f, 0f));
        }

        private static void AddBoldShadow(Transform parent, string label, Vector3 localPosition)
        {
            GameObject boldShadow = new GameObject("BoldShadow", typeof(TextMesh));
            boldShadow.transform.SetParent(parent, false);
            boldShadow.transform.localPosition = localPosition;
            boldShadow.transform.localRotation = Quaternion.identity;
            ConfigureLabelText(boldShadow.GetComponent<TextMesh>(), label);
        }

        private static void ConfigureLabelText(TextMesh text, string label)
        {
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.08f;
            text.fontSize = 56;
            text.color = Color.black;

            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 2;
            }
        }

        private CubieVisual FindFaceCenterCubie(CubeFace face)
        {
            if (cubeController == null || cubeController.CubeRoot == null)
            {
                return null;
            }

            Vector3Int target;
            switch (face)
            {
                case CubeFace.Up: target = new Vector3Int(0, 1, 0); break;
                case CubeFace.Right: target = new Vector3Int(1, 0, 0); break;
                case CubeFace.Front: target = new Vector3Int(0, 0, 1); break;
                default: return null;
            }

            CubieVisual[] cubies = cubeController.CubeRoot.GetComponentsInChildren<CubieVisual>();
            foreach (CubieVisual cubie in cubies)
            {
                if (cubie.CurrentGridPosition == target)
                {
                    return cubie;
                }
            }

            return null;
        }

        private static Quaternion GetLabelRotation(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return Quaternion.Euler(90f, 180f, 0f);
                case CubeFace.Right: return Quaternion.Euler(0f, 270f, 0f);
                case CubeFace.Front: return Quaternion.Euler(0f, 180f, 0f);
                default: return Quaternion.identity;
            }
        }

        private sealed class MonoBehaviourHost : MonoBehaviour
        {
        }
    }
}
