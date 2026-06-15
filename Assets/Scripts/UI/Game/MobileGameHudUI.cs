using System.Text;
using CubeChallenge3D.Core;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.GameModes.QuickPlay;
using CubeChallenge3D.GameModes.RankingChallenge;
using CubeChallenge3D.GameModes.Stages;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Game
{
    public sealed class MobileGameHudUI : MonoBehaviour
    {
        private CubeController cubeController;
        private CubeViewOrbitController orbitController;
        private QuickPlayGameMode quickPlay;
        private RankingChallengeGameMode ranking;
        private StagePlayGameMode stage;
        private GameLaunchMode launchMode;

        private Canvas canvas;
        private Text titleText;
        private Text timerText;
        private Text movesText;
        private Text statusText;
        private RectTransform actionRoot;
        private GameObject overlayRoot;
        private Image overlayBackground;
        private Text overlayTitle;
        private Text overlayBody;
        private Button overlayPrimaryButton;
        private Button overlayCloseButton;
        private TargetPreviewCubeView targetPreview;
        private Button targetMiniButton;
        private Button interactionModeButton;
        private string previewMode = string.Empty;

        public void Initialize(
            CubeController controller,
            CubeViewOrbitController viewOrbitController,
            GameLaunchMode mode,
            QuickPlayGameMode quickPlayMode,
            RankingChallengeGameMode rankingMode,
            StagePlayGameMode stageMode)
        {
            cubeController = controller;
            orbitController = viewOrbitController;
            launchMode = mode;
            quickPlay = quickPlayMode;
            ranking = rankingMode;
            stage = stageMode;
            BuildUi();
            RebuildActions();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void BuildUi()
        {
            canvas = RuntimeUiFactory.CreateCanvas(transform, "GameplayHudCanvas", 1250);
            TopCurrencyBar.Attach(canvas);

            GameObject safeObject = new GameObject("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(canvas.transform, false);
            RectTransform safeArea = safeObject.GetComponent<RectTransform>();

            titleText = CreateTopText(safeArea, "Title", -64f, 32, 50f);
            timerText = CreateTopText(safeArea, "Timer", -116f, 38, 54f);
            movesText = CreateTopText(safeArea, "Moves", -170f, 27, 42f);
            statusText = CreateTopText(safeArea, "Status", -212f, 21, 54f);

            Button shop = CreateAnchoredButton(
                safeArea, "ShopButton", "Shop",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-22f, -56f), new Vector2(150f, 58f));
            shop.onClick.AddListener(OpenShop);

            interactionModeButton = CreateAnchoredButton(
                safeArea, "InteractionModeButton", "Solve",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-22f, -126f), new Vector2(150f, 58f));
            interactionModeButton.onClick.AddListener(ToggleInteractionMode);

            GameObject actionsObject = new GameObject("Actions", typeof(RectTransform));
            actionsObject.transform.SetParent(safeArea, false);
            actionRoot = actionsObject.GetComponent<RectTransform>();
            actionRoot.anchorMin = new Vector2(0f, 0f);
            actionRoot.anchorMax = new Vector2(1f, 0f);
            actionRoot.pivot = new Vector2(0.5f, 0f);
            actionRoot.anchoredPosition = Vector2.zero;
            actionRoot.sizeDelta = new Vector2(0f, 390f);

            BuildOverlay(safeArea);
            targetPreview = new GameObject("TargetPreviewCubeView").AddComponent<TargetPreviewCubeView>();
            targetPreview.transform.SetParent(transform, false);
            targetPreview.Hide();
        }

        private void RebuildActions()
        {
            for (int i = actionRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(actionRoot.GetChild(i).gameObject);
            }

            targetMiniButton = null;
            switch (launchMode)
            {
                case GameLaunchMode.StagePlay:
                    BuildStageActions();
                    break;
                case GameLaunchMode.RankingChallenge:
                    BuildRankingActions();
                    break;
                default:
                    BuildPracticeActions();
                    break;
            }

            RefreshInteractionModeButton();
        }

        private void RefreshInteractionModeButton()
        {
            if (interactionModeButton == null || orbitController == null)
            {
                return;
            }

            bool busy = orbitController.IsSnapping
                || (cubeController != null && cubeController.IsBusy);
            interactionModeButton.interactable = !busy;
            SetButtonLabel(
                interactionModeButton,
                orbitController.CurrentMode == CubeInteractionMode.View ? "Solve" : "View");
        }

        private void ToggleInteractionMode()
        {
            if (orbitController == null)
            {
                return;
            }

            if (orbitController.CurrentMode == CubeInteractionMode.View)
            {
                orbitController.SetSolveMode();
            }
            else if (orbitController.CurrentMode == CubeInteractionMode.Solve)
            {
                orbitController.SetViewMode();
            }
        }

        private void BuildPracticeActions()
        {
            Button scramble = CreateBottomButton(actionRoot, "ScrambleButton", "Scramble", 0f, 24f, 280f);
            Button undo = CreateBottomButton(actionRoot, "UndoButton", "Undo", 0.5f, 24f, 220f);
            Button menu = CreateBottomButton(actionRoot, "MenuButton", "Main Menu", 1f, 24f, 280f);
            Button retry = CreateBottomButton(actionRoot, "RetryButton", "Retry", 0.5f, 108f, 220f);

            scramble.onClick.AddListener(() => quickPlay?.StartNewGame());
            undo.onClick.AddListener(() => cubeController?.Undo());
            retry.onClick.AddListener(() => quickPlay?.Retry());
            menu.onClick.AddListener(SceneLoader.LoadMainMenu);
        }

        private void BuildRankingActions()
        {
            Button scramble = CreateBottomButton(actionRoot, "RankingScrambleButton", "Scramble", 0f, 110f, 430f);
            Button start = CreateBottomButton(actionRoot, "StartButton", "Start", 1f, 110f, 430f);
            Button rankings = CreateBottomButton(actionRoot, "RankingButton", "Ranking", 0f, 24f, 430f);
            Button menu = CreateBottomButton(actionRoot, "MenuButton", "Main Menu", 1f, 24f, 430f);

            scramble.onClick.AddListener(PrepareRankingChallenge);
            rankings.onClick.AddListener(ShowRankingOverlay);
            start.onClick.AddListener(StartRankingChallenge);
            menu.onClick.AddListener(SceneLoader.LoadMainMenu);
        }

        private void BuildStageActions()
        {
            Button list = CreateBottomButton(actionRoot, "StageListButton", "Stage List", 0f, 20f, 260f);
            Button menu = CreateBottomButton(actionRoot, "MenuButton", "Main Menu", 1f, 20f, 260f);
            list.onClick.AddListener(() => stage?.ExitToStageList());
            menu.onClick.AddListener(SceneLoader.LoadMainMenu);

            Button retry = CreateBottomButton(actionRoot, "RetryButton", "Retry", 1f, 102f, 260f);
            retry.onClick.AddListener(() => stage?.RetryStage());

            Button hint = CreateBottomButton(actionRoot, "HintButton", "Hint", 0f, 102f, 210f);
            Button undo = CreateBottomButton(actionRoot, "UndoButton", "Undo", 0.5f, 102f, 210f);
            hint.onClick.AddListener(() => stage?.RequestHint());
            undo.onClick.AddListener(() => stage?.UseUndoAssist());

            Button plus1 = CreateBottomButton(actionRoot, "Plus1Button", "+1", 0.16f, 184f, 180f);
            Button plus2 = CreateBottomButton(actionRoot, "Plus2Button", "+2", 0.5f, 184f, 180f);
            Button plus3 = CreateBottomButton(actionRoot, "Plus3Button", "+3", 0.84f, 184f, 180f);
            plus1.onClick.AddListener(() => stage?.UseMoveItem(StageAssistItemType.MovePlus1));
            plus2.onClick.AddListener(() => stage?.UseMoveItem(StageAssistItemType.MovePlus2));
            plus3.onClick.AddListener(() => stage?.UseMoveItem(StageAssistItemType.MovePlus3));

            Button start = CreateBottomButton(actionRoot, "StartStageButton", "Start", 0.5f, 270f, 250f);
            start.onClick.AddListener(() => stage?.StartStage());

            targetMiniButton = CreateBottomButton(actionRoot, "TargetMiniButton", "Target", 0.5f, 20f, 220f);
            targetMiniButton.onClick.AddListener(OpenTarget);
            Image targetButtonImage = targetMiniButton.GetComponent<Image>();
            if (targetButtonImage != null)
            {
                targetButtonImage.color = new Color(0.08f, 0.18f, 0.23f, 0.12f);
            }
        }

        private void Refresh()
        {
            if (titleText == null)
            {
                return;
            }

            RefreshInteractionModeButton();
            switch (launchMode)
            {
                case GameLaunchMode.StagePlay:
                    RefreshStage();
                    break;
                case GameLaunchMode.RankingChallenge:
                    RefreshRanking();
                    break;
                default:
                    RefreshPractice();
                    break;
            }
        }

        private void RefreshPractice()
        {
            titleText.text = "Practice";
            timerText.text = $"Time  {FormatTime(quickPlay?.ElapsedTime ?? 0f)}";
            movesText.text = $"Moves  {quickPlay?.MoveCount ?? 0}";
            statusText.text = quickPlay != null ? GetPracticeStatus(quickPlay.State) : string.Empty;
            HideTarget();
            bool busy = quickPlay != null && quickPlay.State == QuickPlayState.Scrambling;
            SetActionInteractable("ScrambleButton", !busy);
            SetActionInteractable("RetryButton", !busy && quickPlay != null && quickPlay.ActiveScramble.Count > 0);
            SetActionInteractable("UndoButton", !busy && cubeController != null && cubeController.MoveHistory.CanUndo);

            if (quickPlay != null && quickPlay.State == QuickPlayState.Solved && !overlayRoot.activeSelf)
            {
                ShowOverlay(
                    "Clear!",
                    $"Time  {FormatTime(quickPlay.ElapsedTime)}\nMoves  {quickPlay.MoveCount}",
                    "New Game",
                    () =>
                    {
                        HideOverlay();
                        quickPlay.StartNewGame();
                    },
                    true);
            }
        }

        private void RefreshRanking()
        {
            titleText.text = ranking?.Config != null
                ? $"Daily Challenge  {ranking.Config.dateUtc}"
                : "Ranking Challenge";
            timerText.text = $"Time  {FormatTime(ranking?.ElapsedTime ?? 0f)}";
            movesText.text = $"Moves  {ranking?.MoveCount ?? 0}";
            statusText.text = ranking != null ? GetRankingStatus(ranking.State) : string.Empty;
            HideTarget();
            SetActionInteractable(
                "StartButton",
                ranking != null && ranking.State == RankingChallengeState.Previewing);
            SetActionInteractable(
                "RankingScrambleButton",
                ranking != null
                && ranking.State != RankingChallengeState.Scrambling
                && ranking.State != RankingChallengeState.Playing);

            if (ranking != null && ranking.State == RankingChallengeState.Solved && !overlayRoot.activeSelf)
            {
                ShowOverlay(
                    "Challenge Complete",
                    $"Time  {FormatTime(ranking.ElapsedTime)}\nMoves  {ranking.MoveCount}\n{ranking.LastSubmitMessage}",
                    "Ranking",
                    ShowRankingOverlay,
                    true);
            }
        }

        private void RefreshStage()
        {
            if (stage == null || stage.CurrentStage == null)
            {
                return;
            }

            titleText.text = $"Stage {stage.CurrentStage.stageNumber}  {stage.CurrentStage.title}";
            timerText.text = string.Empty;
            timerText.gameObject.SetActive(false);
            movesText.text = $"Moves  {stage.Runtime.currentMoves}/{stage.Runtime.moveLimit}";
            statusText.text = stage.StatusMessage;

            bool reverse = stage.IsReverseTargetStage;
            bool intro = stage.State == StagePlayState.TargetIntro;
            bool playing = stage.State == StagePlayState.Playing;
            bool popup = stage.State == StagePlayState.TargetPreviewPopup;

            SetActionVisible("StartStageButton", intro);
            SetActionVisible("TargetMiniButton", reverse && playing);
            SetActionVisible("HintButton", playing);
            SetActionVisible("UndoButton", playing);
            SetActionVisible("Plus1Button", playing);
            SetActionVisible("Plus2Button", playing);
            SetActionVisible("Plus3Button", playing);
            SetActionVisible("RetryButton", !intro && !popup);
            SetActionInteractable("UndoButton", playing && cubeController != null && cubeController.MoveHistory.CanUndo);
            SetActionInteractable(
                "Plus1Button",
                playing && stage.InventoryStore != null && stage.InventoryStore.GetCount(StageAssistItemType.MovePlus1) > 0);
            SetActionInteractable(
                "Plus2Button",
                playing && stage.InventoryStore != null && stage.InventoryStore.GetCount(StageAssistItemType.MovePlus2) > 0);
            SetActionInteractable(
                "Plus3Button",
                playing && stage.InventoryStore != null && stage.InventoryStore.GetCount(StageAssistItemType.MovePlus3) > 0);
            SetActionLabel(
                "UndoButton",
                $"Undo {stage.AssistState?.freeUndoRemaining ?? 0}/{stage.InventoryStore?.UndoItems ?? 0}");
            SetActionLabel(
                "Plus1Button",
                $"+1 ({stage.InventoryStore?.GetCount(StageAssistItemType.MovePlus1) ?? 0})");
            SetActionLabel(
                "Plus2Button",
                $"+2 ({stage.InventoryStore?.GetCount(StageAssistItemType.MovePlus2) ?? 0})");
            SetActionLabel(
                "Plus3Button",
                $"+3 ({stage.InventoryStore?.GetCount(StageAssistItemType.MovePlus3) ?? 0})");

            if (reverse && intro)
            {
                ShowTarget("intro");
            }
            else if (reverse && playing)
            {
                ShowTarget("mini");
            }
            else if (reverse && popup)
            {
                ShowTarget("popup");
            }
            else
            {
                HideTarget();
            }

            if (stage.State == StagePlayState.Cleared && !overlayRoot.activeSelf)
            {
                ShowOverlay(
                    "Stage Clear!",
                    $"Stars  {stage.EarnedStars}\nMoves  {stage.Runtime.currentMoves}",
                    "Next Stage",
                    () =>
                    {
                        HideOverlay();
                        stage.LoadNextStage();
                    },
                    true);
            }
            else if (stage.State == StagePlayState.Failed && !overlayRoot.activeSelf)
            {
                ShowOverlay(
                    "Out of Moves",
                    stage.StatusMessage,
                    "Retry",
                    () =>
                    {
                        HideOverlay();
                        stage.RetryStage();
                    },
                    true);
            }
        }

        private void ShowTarget(string mode)
        {
            if (targetPreview == null || stage?.TargetState == null)
            {
                return;
            }

            if (previewMode == mode)
            {
                return;
            }

            previewMode = mode;
            if (mode == "mini")
            {
                targetPreview.Show(
                    stage.TargetState,
                    GetMiniTargetWorldPosition(),
                    new Vector3(24f, -35f, 0f),
                    0.24f,
                    false);
                return;
            }

            targetPreview.Show(stage.TargetState, Vector3.zero, new Vector3(24f, -35f, 0f), 1f, true);
            if (mode == "intro")
            {
                ShowOverlay(
                    "Target Pattern",
                    "Make this cube pattern.\nDrag to inspect every side.",
                    "Start",
                    () =>
                    {
                        HideOverlay();
                        previewMode = string.Empty;
                        stage.StartStage();
                    },
                    true);
                SetOverlayShade(0.18f);
            }
            else
            {
                ShowOverlay(
                    "Target Pattern",
                    "Check the target, then return to play.",
                    "Play!",
                    CloseTarget,
                    false);
                SetOverlayShade(0.18f);
            }
        }

        private void OpenTarget()
        {
            if (stage == null || !stage.IsReverseTargetStage)
            {
                return;
            }

            stage.SetTargetPreviewOpen(true);
            previewMode = string.Empty;
        }

        private void PrepareRankingChallenge()
        {
            if (ranking == null || orbitController == null)
            {
                return;
            }

            orbitController.SetViewMode();
            ranking.PrepareChallenge();
        }

        private void StartRankingChallenge()
        {
            if (ranking == null || orbitController == null)
            {
                return;
            }

            orbitController.SetSolveMode();
            ranking.StartChallenge();
        }

        private void CloseTarget()
        {
            HideOverlay();
            previewMode = string.Empty;
            stage?.SetTargetPreviewOpen(false);
        }

        private void HideTarget()
        {
            if (string.IsNullOrEmpty(previewMode))
            {
                return;
            }

            previewMode = string.Empty;
            targetPreview?.Hide();
        }

        private void ShowRankingOverlay()
        {
            if (ranking == null)
            {
                return;
            }

            var builder = new StringBuilder();
            builder.Append(ranking.RankingSourceLabel).AppendLine(" Top 10");
            if (ranking.DisplayedRankingRecords.Count == 0)
            {
                builder.Append("No records yet.");
            }
            else
            {
                for (int i = 0; i < ranking.DisplayedRankingRecords.Count; i++)
                {
                    RankingSubmission record = ranking.DisplayedRankingRecords[i];
                    builder.Append(i + 1)
                        .Append(". ")
                        .Append(record.playerName)
                        .Append("  ")
                        .Append(FormatTime(record.elapsedSeconds))
                        .Append("  ")
                        .Append(record.moveCount)
                        .AppendLine(" moves");
                }
            }

            ShowOverlay("Ranking", builder.ToString(), "Close", HideOverlay, false);
        }

        private void BuildOverlay(RectTransform parent)
        {
            overlayRoot = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlayRoot.transform.SetParent(parent, false);
            RectTransform rect = overlayRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlayBackground = overlayRoot.GetComponent<Image>();
            overlayBackground.color = new Color(0.015f, 0.02f, 0.03f, 0.82f);

            overlayTitle = RuntimeUiFactory.CreateText(rect, "Title", string.Empty, 40, TextAnchor.MiddleCenter);
            SetOverlayTextRect(overlayTitle.rectTransform, 0.68f, 0.82f);
            overlayBody = RuntimeUiFactory.CreateText(rect, "Body", string.Empty, 27, TextAnchor.UpperCenter);
            SetOverlayTextRect(overlayBody.rectTransform, 0.31f, 0.67f);

            overlayPrimaryButton = CreateAnchoredButton(
                rect, "PrimaryButton", "Continue",
                new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f),
                Vector2.zero, new Vector2(340f, 82f));
            overlayCloseButton = CreateAnchoredButton(
                rect, "CloseButton", "X",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -70f), new Vector2(72f, 64f));
            overlayCloseButton.onClick.AddListener(HideOverlay);
            overlayRoot.SetActive(false);
        }

        private void ShowOverlay(
            string title,
            string body,
            string primaryLabel,
            UnityEngine.Events.UnityAction primaryAction,
            bool allowClose)
        {
            overlayTitle.text = title;
            overlayBody.text = body;
            SetButtonLabel(overlayPrimaryButton, primaryLabel);
            overlayPrimaryButton.onClick.RemoveAllListeners();
            if (primaryAction != null)
            {
                overlayPrimaryButton.onClick.AddListener(primaryAction);
            }

            overlayCloseButton.gameObject.SetActive(allowClose);
            SetOverlayShade(0.82f);
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        private void HideOverlay()
        {
            overlayRoot.SetActive(false);
        }

        private void SetOverlayShade(float alpha)
        {
            if (overlayBackground != null)
            {
                Color color = overlayBackground.color;
                color.a = alpha;
                overlayBackground.color = color;
            }
        }

        private void OpenShop()
        {
            GameLaunchContext.RequestShopOnMainMenu();
            SceneLoader.LoadMainMenu();
        }

        private void SetActionVisible(string name, bool visible)
        {
            Transform child = actionRoot.Find(name);
            if (child != null && child.gameObject.activeSelf != visible)
            {
                child.gameObject.SetActive(visible);
            }
        }

        private void SetActionInteractable(string name, bool interactable)
        {
            Button button = actionRoot.Find(name)?.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void SetActionLabel(string name, string label)
        {
            Button button = actionRoot.Find(name)?.GetComponent<Button>();
            SetButtonLabel(button, label);
        }

        private static Vector3 GetMiniTargetWorldPosition()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return new Vector3(0f, -2.65f, 0.25f);
            }

            float distance = Vector3.Distance(camera.transform.position, Vector3.zero);
            return camera.ViewportToWorldPoint(new Vector3(0.5f, 0.105f, distance));
        }

        private static Text CreateTopText(
            RectTransform parent,
            string name,
            float y,
            int fontSize,
            float height)
        {
            Text text = RuntimeUiFactory.CreateText(parent, name, string.Empty, fontSize, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            text.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.anchoredPosition = new Vector2(0f, y);
            text.rectTransform.sizeDelta = new Vector2(0f, height);
            return text;
        }

        private static Button CreateBottomButton(
            RectTransform parent,
            string name,
            string label,
            float horizontalAnchor,
            float y,
            float width)
        {
            return CreateAnchoredButton(
                parent,
                name,
                label,
                new Vector2(horizontalAnchor, 0f),
                new Vector2(horizontalAnchor, 0f),
                new Vector2(0f, y),
                new Vector2(width, 68f));
        }

        private static Button CreateAnchoredButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            buttonObject.GetComponent<Image>().color = new Color(0.08f, 0.18f, 0.23f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            Text text = RuntimeUiFactory.CreateText(rect, "Label", label, 25, TextAnchor.MiddleCenter);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 25;
            return button;
        }

        private static void SetOverlayTextRect(RectTransform rect, float minY, float maxY)
        {
            rect.anchorMin = new Vector2(0.08f, minY);
            rect.anchorMax = new Vector2(0.92f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button != null ? button.GetComponentInChildren<Text>() : null;
            if (text != null)
            {
                text.text = label;
            }
        }

        private static string GetPracticeStatus(QuickPlayState state)
        {
            switch (state)
            {
                case QuickPlayState.Scrambling: return "Scrambling...";
                case QuickPlayState.Playing: return "Solve the cube";
                case QuickPlayState.Solved: return "Solved";
                case QuickPlayState.Paused: return "Paused";
                default: return "Tap Scramble to begin";
            }
        }

        private static string GetRankingStatus(RankingChallengeState state)
        {
            switch (state)
            {
                case RankingChallengeState.Scrambling: return "Preparing challenge...";
                case RankingChallengeState.Previewing: return "Inspect the scramble, then tap Start";
                case RankingChallengeState.Playing: return "Challenge in progress";
                case RankingChallengeState.Solved: return "Challenge complete";
                default: return "Tap Start when ready";
            }
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remaining = seconds - minutes * 60f;
            return $"{minutes:00}:{remaining:00.00}";
        }
    }
}
