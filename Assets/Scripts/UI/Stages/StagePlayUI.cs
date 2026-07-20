using CubeChallenge3D.Core;
using CubeChallenge3D.GameModes.Stages;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Stages
{
    public sealed class StagePlayUI : MonoBehaviour
    {
        private StagePlayGameMode gameMode;
        private StageProgressStore progressStore;
        private Text titleText;
        private Text statsText;
        private Text resultText;
        private Button retryButton;
        private Button nextButton;
        private Button startButton;
        private Button viewTargetButton;
        private Button hintButton;
        private Button undoButton;
        private Button movePlus1Button;
        private Button movePlus2Button;
        private Button movePlus3Button;
        private Button continueButton;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private Button forceClearButton;
#endif
        private GameObject targetPopupRoot;
        private Text popupText;
        private TargetPreviewCubeView previewView;
        private string previewKey = string.Empty;
        private Canvas canvas;
        private GameObject heartPopup;
        private Text heartPopupBody;
        private bool heartPopupDismissed;
        private readonly Vector2 minPanelSize = new Vector2(620f, 520f);
        private readonly Vector2 maxPanelSize = new Vector2(920f, 820f);

        public void Initialize(StagePlayGameMode mode, StageProgressStore store)
        {
            gameMode = mode;
            progressStore = store;
            BuildUi();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void BuildUi()
        {
            if (canvas != null)
            {
                return;
            }

            canvas = RuntimeUiFactory.CreateCanvas(transform, "StagePlayCanvas", 1180);
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "StagePlayPanel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(126f, -82f),
                new Vector2(620f, 620f));

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.raycastTarget = true;
            }

            AddDragBar(panel);
            AddResizeHandle(panel);

            titleText = CreateRow(panel, "Title", new Vector2(0f, -52f), 28, 58f);
            statsText = CreateRow(panel, "Stats", new Vector2(0f, -112f), 21, 242f);
            statsText.alignment = TextAnchor.UpperCenter;
            resultText = CreateRow(panel, "Result", new Vector2(0f, -358f), 21, 120f);

            startButton = RuntimeUiFactory.CreateButton(panel, "StartButton", "Start", new Vector2(-205f, 90f), new Vector2(180f, 58f));
            viewTargetButton = RuntimeUiFactory.CreateButton(panel, "ViewTargetButton", "View Target", new Vector2(215f, 90f), new Vector2(200f, 58f));
            retryButton = RuntimeUiFactory.CreateButton(panel, "RetryButton", "Retry", new Vector2(-205f, 24f), new Vector2(180f, 58f));
            Button listButton = RuntimeUiFactory.CreateButton(panel, "StageListButton", "Stage List", new Vector2(0f, 24f), new Vector2(200f, 58f));
            nextButton = RuntimeUiFactory.CreateButton(panel, "NextButton", "Next Stage", new Vector2(215f, 24f), new Vector2(200f, 58f));
            hintButton = RuntimeUiFactory.CreateButton(panel, "HintButton", "Hint", new Vector2(-205f, 90f), new Vector2(180f, 54f));
            undoButton = RuntimeUiFactory.CreateButton(panel, "AssistUndoButton", "Undo", new Vector2(0f, 90f), new Vector2(180f, 54f));
            continueButton = RuntimeUiFactory.CreateButton(panel, "ContinueButton", "Continue +2", new Vector2(215f, 90f), new Vector2(200f, 54f));
            movePlus1Button = RuntimeUiFactory.CreateButton(panel, "MovePlus1Button", "+1 Move", new Vector2(-205f, 154f), new Vector2(180f, 54f));
            movePlus2Button = RuntimeUiFactory.CreateButton(panel, "MovePlus2Button", "+2 Move", new Vector2(0f, 154f), new Vector2(180f, 54f));
            movePlus3Button = RuntimeUiFactory.CreateButton(panel, "MovePlus3Button", "+3 Move", new Vector2(215f, 154f), new Vector2(200f, 54f));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            forceClearButton = RuntimeUiFactory.CreateButton(panel, "ForceClearDebugButton", "Force Clear", new Vector2(205f, -52f), new Vector2(210f, 48f));
            Image forceClearImage = forceClearButton.GetComponent<Image>();
            if (forceClearImage != null)
            {
                forceClearImage.color = new Color(0.45f, 0.22f, 0.04f, 0.94f);
            }
#endif

            startButton.onClick.AddListener(() => gameMode?.StartStage());
            viewTargetButton.onClick.AddListener(ShowTargetPopup);
            retryButton.onClick.AddListener(() => gameMode?.RetryStage());
            listButton.onClick.AddListener(() => gameMode?.ExitToStageList());
            nextButton.onClick.AddListener(() => gameMode?.LoadNextStage());
            hintButton.onClick.AddListener(() => gameMode?.RequestHint());
            undoButton.onClick.AddListener(() => gameMode?.UseUndoAssist());
            continueButton.onClick.AddListener(() => gameMode?.ContinueAfterAd());
            movePlus1Button.onClick.AddListener(() => gameMode?.UseMoveItem(StageAssistItemType.MovePlus1));
            movePlus2Button.onClick.AddListener(() => gameMode?.UseMoveItem(StageAssistItemType.MovePlus2));
            movePlus3Button.onClick.AddListener(() => gameMode?.UseMoveItem(StageAssistItemType.MovePlus3));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            forceClearButton.onClick.AddListener(() => gameMode?.ForceClearForDebug());
#endif
            BuildTargetPopup();
            BuildHeartPopup();
            previewView = new GameObject("TargetPreviewCubeView").AddComponent<TargetPreviewCubeView>();
            previewView.transform.SetParent(transform, false);
            previewView.Hide();
        }

        private void AddDragBar(RectTransform parent)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 46f);

            Image image = barObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.11f, 0.14f, 1f);

            PanelDragHandle handle = barObject.AddComponent<PanelDragHandle>();
            handle.Initialize(parent);
        }

        private void AddResizeHandle(RectTransform parent)
        {
            GameObject handleObject = new GameObject("ResizeHandle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(parent, false);
            RectTransform rect = handleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-10f, 10f);
            rect.sizeDelta = new Vector2(38f, 38f);

            Image image = handleObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.35f);

            PanelResizeHandle handle = handleObject.AddComponent<PanelResizeHandle>();
            handle.Initialize(parent, minPanelSize, maxPanelSize);
        }

        private static Text CreateRow(RectTransform parent, string name, Vector2 position, int size, float height)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(0f, height);
            return RuntimeUiFactory.CreateText(rect, "Text", string.Empty, size, TextAnchor.MiddleCenter);
        }

        private void Refresh()
        {
            if (gameMode == null || titleText == null)
            {
                return;
            }

            var stage = gameMode.CurrentStage;
            StageRuntimeState runtime = gameMode.Runtime;
            titleText.text = stage != null
                ? $"Stage {stage.stageNumber}  {stage.title}"
                : "Stage";
            statsText.text = stage != null
                ? $"{GetStageTypeLabel()}\nDifficulty  {stage.difficulty}\n{GetGoalText()}\nTime  {FormatTime(runtime.elapsedSeconds)}\nMoves Used  {runtime.currentMoves}\nMove Limit  {runtime.moveLimit}\nRemaining  {runtime.remainingMoves}\nAssists Used  {GetAssistUseCount()}"
                : "-";

            if (gameMode.State == StagePlayState.Cleared)
            {
                StageProgress progress = progressStore?.GetProgress(stage.stageId);
                string bestStars = gameMode.BestStarsUpdated
                    ? $"Best Stars  {gameMode.PreviousBestStars} -> {progress?.stars ?? gameMode.EarnedStars}"
                    : $"Best Stars  {progress?.stars ?? gameMode.EarnedStars}";
                string assist = GetAssistUseCount() == 0
                    ? "Assists Used  0"
                    : $"Assists Used  {GetAssistUseCount()}  |  Max Stars {gameMode.MaxStarsAllowed}";
                resultText.text =
                    $"Stage Clear!  Stars {gameMode.EarnedStars}  |  {bestStars}\n" +
                    $"Moves Used {runtime.currentMoves}/{runtime.moveLimit}  |  Coins Earned {gameMode.EarnedCoins}\n" +
                    $"{assist}. Using move assists can limit your stars.";
            }
            else if (gameMode.State == StagePlayState.Failed)
            {
                int used = gameMode.AssistState != null ? gameMode.AssistState.adContinueCount : 0;
                resultText.text = $"Out of moves\nWatch Ad for +{gameMode.StageContinueMovesReward} Moves\n{used}/{gameMode.StageContinueMaxPerRun} used";
            }
            else
            {
                resultText.text = gameMode.StatusMessage;
            }

            bool canRetry = gameMode.State != StagePlayState.Preparing;
            retryButton.interactable = canRetry;
            nextButton.interactable = gameMode.State == StagePlayState.Cleared && gameMode.HasNextPlayableStage();
            bool isReverse = gameMode.IsReverseTargetStage;
            bool isIntro = gameMode.State == StagePlayState.TargetIntro;
            bool isPlaying = gameMode.State == StagePlayState.Playing;
            bool isFailed = gameMode.State == StagePlayState.Failed;
            startButton.gameObject.SetActive(isReverse && isIntro);
            viewTargetButton.gameObject.SetActive(isReverse && !isIntro && gameMode.State != StagePlayState.Cleared && gameMode.State != StagePlayState.Failed);
            retryButton.gameObject.SetActive(!isIntro);
            nextButton.gameObject.SetActive(!isIntro);
            hintButton.gameObject.SetActive(isPlaying);
            undoButton.gameObject.SetActive(isPlaying);
            movePlus1Button.gameObject.SetActive(isPlaying);
            movePlus2Button.gameObject.SetActive(isPlaying);
            movePlus3Button.gameObject.SetActive(isPlaying);
            continueButton.gameObject.SetActive(isFailed);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            forceClearButton.gameObject.SetActive(gameMode.State != StagePlayState.Cleared);
#endif
            continueButton.interactable = gameMode.CanRequestContinueAfterAd() && !gameMode.IsShowingAd;
            UpdateAssistButtonLabels();
            UpdateAssistButtonInteractivity(isPlaying);
            UpdateTargetPreview();
            bool lacksHearts = gameMode.StatusMessage.StartsWith("Not enough hearts");
            if (!lacksHearts)
            {
                heartPopupDismissed = false;
            }
            else if (!heartPopupDismissed && heartPopup != null)
            {
                int seconds = gameMode.WalletStore?.SecondsUntilNextHeart ?? 0;
                heartPopupBody.text =
                    "Hearts are used to play stages.\n"
                    + $"Hearts recharge over time.\nNext heart in {seconds / 60:00}:{seconds % 60:00}\n"
                    + "Get more hearts in the Shop.";
                heartPopup.SetActive(true);
                heartPopup.transform.SetAsLastSibling();
            }
        }

        private void BuildHeartPopup()
        {
            heartPopup = new GameObject("NotEnoughHeartsPopup", typeof(RectTransform), typeof(Image));
            heartPopup.transform.SetParent(canvas.transform, false);
            RectTransform panel = heartPopup.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(680f, 430f);
            heartPopup.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.06f, 0.99f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", UIStrings.NotEnoughHearts, 36, TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(40f, 330f);
            title.rectTransform.offsetMax = new Vector2(-40f, -38f);

            heartPopupBody = RuntimeUiFactory.CreateText(
                panel,
                "Body",
                "Hearts are used to play stages.\nHearts recharge over time.\nGet more hearts in the Shop.",
                25,
                TextAnchor.MiddleCenter);
            heartPopupBody.rectTransform.offsetMin = new Vector2(50f, 120f);
            heartPopupBody.rectTransform.offsetMax = new Vector2(-50f, -100f);

            Button shop = RuntimeUiFactory.CreateButton(panel, "GoToShopButton", UIStrings.GoToShop, new Vector2(-155f, 30f), new Vector2(260f, 64f));
            shop.onClick.AddListener(() =>
            {
                GameLaunchContext.RequestShopOnMainMenu();
                SceneLoader.LoadMainMenu();
            });
            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", UIStrings.Close, new Vector2(155f, 30f), new Vector2(220f, 64f));
            close.onClick.AddListener(() =>
            {
                heartPopupDismissed = true;
                heartPopup.SetActive(false);
            });
            heartPopup.SetActive(false);
        }

        private int GetAssistUseCount()
        {
            return gameMode?.AssistState != null ? gameMode.AssistState.assistUseCount : 0;
        }

        private void UpdateAssistButtonLabels()
        {
            if (gameMode == null)
            {
                return;
            }

            int freeUndo = gameMode.AssistState != null ? gameMode.AssistState.freeUndoRemaining : 0;
            int paidUndo = gameMode.InventoryStore != null ? gameMode.InventoryStore.UndoItems : 0;
            SetButtonLabel(undoButton, $"Undo {freeUndo}/{paidUndo}");
            SetButtonLabel(movePlus1Button, $"+1 ({GetItemCount(StageAssistItemType.MovePlus1)})");
            SetButtonLabel(movePlus2Button, $"+2 ({GetItemCount(StageAssistItemType.MovePlus2)})");
            SetButtonLabel(movePlus3Button, $"+3 ({GetItemCount(StageAssistItemType.MovePlus3)})");
            int continues = gameMode.AssistState != null ? gameMode.AssistState.adContinueCount : 0;
            SetButtonLabel(
                continueButton,
                gameMode.CanRequestContinueAfterAd() || gameMode.IsShowingAd
                    ? $"Watch Ad for +{gameMode.StageContinueMovesReward} Moves\n{continues}/{gameMode.StageContinueMaxPerRun} used"
                    : gameMode.StageContinueAdStatus);
            Text continueLabel = continueButton.GetComponentInChildren<Text>();
            if (continueLabel != null)
            {
                continueLabel.fontSize = 18;
            }
        }

        private void UpdateAssistButtonInteractivity(bool isPlaying)
        {
            if (gameMode == null)
            {
                return;
            }

            int freeUndo = gameMode.AssistState != null ? gameMode.AssistState.freeUndoRemaining : 0;
            int paidUndo = gameMode.InventoryStore != null ? gameMode.InventoryStore.UndoItems : 0;
            undoButton.interactable = isPlaying && (freeUndo > 0 || paidUndo > 0);
            movePlus1Button.interactable = isPlaying && GetItemCount(StageAssistItemType.MovePlus1) > 0;
            movePlus2Button.interactable = isPlaying && GetItemCount(StageAssistItemType.MovePlus2) > 0;
            movePlus3Button.interactable = isPlaying && GetItemCount(StageAssistItemType.MovePlus3) > 0;
            hintButton.interactable = isPlaying;
        }

        private int GetItemCount(StageAssistItemType itemType)
        {
            return gameMode?.InventoryStore != null ? gameMode.InventoryStore.GetCount(itemType) : 0;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private string GetGoalText()
        {
            if (gameMode != null && gameMode.IsReverseTargetStage)
            {
                return "Goal  Make this target";
            }

            return "Goal  Solve the cube";
        }

        private string GetStageTypeLabel()
        {
            return gameMode != null && gameMode.IsReverseTargetStage
                ? "Target Stage"
                : "Solve Stage";
        }

        private void UpdateTargetPreview()
        {
            if (previewView == null || gameMode == null || !gameMode.IsReverseTargetStage)
            {
                previewView?.Hide();
                previewKey = string.Empty;
                return;
            }

            if (gameMode.State == StagePlayState.TargetIntro)
            {
                ShowPreviewIfChanged("intro");
            }
            else if (gameMode.State == StagePlayState.Playing)
            {
                ShowPreviewIfChanged("mini");
            }
            else if (gameMode.State != StagePlayState.TargetPreviewPopup)
            {
                previewView.Hide();
                previewKey = string.Empty;
            }
        }

        private void ShowPreviewIfChanged(string mode)
        {
            string stageId = gameMode.CurrentStage != null ? gameMode.CurrentStage.stageId : string.Empty;
            string nextKey = $"{stageId}:{mode}";
            if (previewKey == nextKey)
            {
                return;
            }

            previewKey = nextKey;
            if (mode == "intro")
            {
                previewView.Show(gameMode.TargetState, Vector3.zero, new Vector3(24f, -35f, 0f), 1f, true);
            }
            else
            {
                previewView.Show(gameMode.TargetState, new Vector3(-3.1f, 1.25f, 0.1f), new Vector3(24f, -35f, 0f), 0.32f, false);
            }
        }

        private void BuildTargetPopup()
        {
            Canvas popupCanvas = RuntimeUiFactory.CreateCanvas(transform, "TargetPreviewPopupCanvas", 1500);
            targetPopupRoot = popupCanvas.gameObject;
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                targetPopupRoot.transform,
                "TargetPreviewPopup",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(620f, 300f));
            AddPopupDragBar(panel);
            popupText = RuntimeUiFactory.CreateText(panel, "PopupText", "Target Pattern", 30, TextAnchor.MiddleCenter);
            popupText.rectTransform.offsetMin = new Vector2(32f, 100f);
            popupText.rectTransform.offsetMax = new Vector2(-32f, -32f);
            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Close", new Vector2(0f, 28f), new Vector2(240f, 64f));
            close.onClick.AddListener(HideTargetPopup);
            targetPopupRoot.SetActive(false);
        }

        private void AddPopupDragBar(RectTransform parent)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 44f);

            Image image = barObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.11f, 0.14f, 1f);

            PanelDragHandle handle = barObject.AddComponent<PanelDragHandle>();
            handle.Initialize(parent);
        }

        private void ShowTargetPopup()
        {
            if (gameMode == null || !gameMode.IsReverseTargetStage)
            {
                return;
            }

            gameMode.SetTargetPreviewOpen(true);
            popupText.text = "Target Pattern\nMatch this cube.";
            targetPopupRoot.SetActive(true);
            previewKey = $"{gameMode.CurrentStage?.stageId}:popup";
            previewView?.Show(gameMode.TargetState, Vector3.zero, new Vector3(24f, -35f, 0f), 1f, true);
        }

        private void HideTargetPopup()
        {
            targetPopupRoot.SetActive(false);
            previewKey = string.Empty;
            gameMode?.SetTargetPreviewOpen(false);
        }

        private static string FormatBestMoves(StageProgress progress)
        {
            return progress != null && progress.bestMoves >= 0 ? progress.bestMoves.ToString() : "-";
        }

        private static string FormatBestTime(StageProgress progress)
        {
            return progress != null && progress.bestTimeSeconds >= 0f ? FormatTime(progress.bestTimeSeconds) : "-";
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remaining = seconds - (minutes * 60f);
            return $"{minutes:00}:{remaining:00.00}";
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
                if (target == null)
                {
                    return;
                }

                float scale = parentCanvas != null && parentCanvas.scaleFactor > 0f
                    ? parentCanvas.scaleFactor
                    : 1f;
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
