using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Core;
using System.Text;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Stages
{
    public sealed class StageListPanelUI
    {
        private readonly GameObject root;
        private readonly Text titleText;
        private readonly Text listText;
        private readonly Text statusText;
        private readonly Text blockStatusText;
        private readonly RectTransform buttonContainer;
        private readonly RectTransform blockContainer;
        private readonly StageDataLoader loader = new StageDataLoader();
        private readonly StageProgressStore progressStore = new StageProgressStore();
        private readonly StageMilestoneRewardStore milestoneStore = new StageMilestoneRewardStore();
        private readonly WalletStore walletStore = new WalletStore();
        private readonly Vector2 minPanelSize = new Vector2(560f, 520f);
        private readonly Vector2 maxPanelSize = new Vector2(980f, 1300f);
        private Button claimMilestoneButton;
        private Button solveModeButton;
        private Button targetModeButton;
        private IReadOnlyList<StageData> currentStages;
        private StageType selectedType = StageType.SolveStage;
        private int selectedBlockIndex;
        private GameObject heartPopup;
        private Text heartPopupBody;
        private Action shopAction;

        public StageListPanelUI(Transform parent)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "StageListCanvas", 1450);
            root = canvas.gameObject;

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "StageListPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(820f, 920f));

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.raycastTarget = true;
            }

            AddDragBar(panel);
            AddResizeHandle(panel);
            AddTopCloseButton(panel);

            titleText = RuntimeUiFactory.CreateText(panel, "Title", "Stages", 38, TextAnchor.UpperCenter);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            titleText.rectTransform.sizeDelta = new Vector2(-156f, 58f);

            listText = RuntimeUiFactory.CreateText(panel, "StageList", string.Empty, 24, TextAnchor.UpperLeft);
            listText.rectTransform.anchorMin = new Vector2(0f, 1f);
            listText.rectTransform.anchorMax = new Vector2(1f, 1f);
            listText.rectTransform.pivot = new Vector2(0.5f, 1f);
            listText.rectTransform.anchoredPosition = new Vector2(0f, -122f);
            listText.rectTransform.sizeDelta = new Vector2(-84f, 62f);

            statusText = RuntimeUiFactory.CreateText(panel, "Status", string.Empty, 22, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0f, 144f);
            statusText.rectTransform.sizeDelta = new Vector2(-84f, 38f);

            solveModeButton = RuntimeUiFactory.CreateButton(panel, "SolveModeButton", "Solve Stages", new Vector2(-190f, 660f), new Vector2(330f, 58f));
            targetModeButton = RuntimeUiFactory.CreateButton(panel, "TargetModeButton", "Target Stages", new Vector2(190f, 660f), new Vector2(330f, 58f));
            solveModeButton.onClick.AddListener(() => SelectMode(StageType.SolveStage));
            targetModeButton.onClick.AddListener(() => SelectMode(StageType.ReverseTargetStage));

            blockContainer = CreateBlockContainer(panel);
            blockStatusText = RuntimeUiFactory.CreateText(panel, "BlockStatus", string.Empty, 20, TextAnchor.MiddleCenter);
            blockStatusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            blockStatusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            blockStatusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            blockStatusText.rectTransform.anchoredPosition = new Vector2(0f, 190f);
            blockStatusText.rectTransform.sizeDelta = new Vector2(-84f, 32f);

            buttonContainer = CreateButtonContainer(panel);

            claimMilestoneButton = RuntimeUiFactory.CreateButton(panel, "ClaimMilestoneButton", "Claim Milestone", new Vector2(0f, 88f), new Vector2(300f, 54f));
            claimMilestoneButton.onClick.AddListener(ClaimSelectedMilestone);

            Button closeButton = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Close", new Vector2(0f, 28f), new Vector2(260f, 72f));
            closeButton.onClick.AddListener(Hide);
            BuildHeartPopup(root.transform);

            Hide();
        }

        public void SetShopAction(Action action)
        {
            shopAction = action;
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
            rect.sizeDelta = new Vector2(0f, 62f);

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
            rect.anchoredPosition = new Vector2(-12f, 12f);
            rect.sizeDelta = new Vector2(44f, 44f);

            Image image = handleObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.35f);

            PanelResizeHandle handle = handleObject.AddComponent<PanelResizeHandle>();
            handle.Initialize(parent, minPanelSize, maxPanelSize);
        }

        private void AddTopCloseButton(RectTransform parent)
        {
            GameObject buttonObject = new GameObject("TopCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -10f);
            rect.sizeDelta = new Vector2(54f, 46f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.26f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(Hide);

            Text label = RuntimeUiFactory.CreateText(rect, "Label", "X", 24, TextAnchor.MiddleCenter);
            label.color = Color.white;
        }

        public void Show(string focusStageId = null)
        {
            IReadOnlyList<StageData> stages = loader.LoadAllStages();
            currentStages = stages;
            progressStore.EnsureStageDefaults(stages);
            milestoneStore.EnsureMilestones(stages);
            FocusStage(focusStageId, stages);
            StageValidationResult validation = StageDataValidator.ValidateAll(stages);
            titleText.text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText("stages")
                : "Stages";
            listText.text = BuildStageList(stages);
            BuildBlockButtons();
            RefreshSelectedBlock();
            statusText.text = validation.isValid
                ? $"{stages.Count} stages ready  |  {UIStrings.UsesOneHeart}"
                : $"Some stages are unavailable  |  {UIStrings.UsesOneHeart}";
            root.SetActive(true);
        }

        private void FocusStage(string stageId, IReadOnlyList<StageData> stages)
        {
            if (string.IsNullOrWhiteSpace(stageId) || stages == null)
            {
                return;
            }

            StageData stage = stages.FirstOrDefault(item => item != null && item.stageId == stageId);
            if (stage == null)
            {
                return;
            }

            selectedType = stage.stageType;
            selectedBlockIndex = Mathf.Clamp((GetLocalStageNumber(stage) - 1) / 10, 0, 9);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private void ClaimSelectedMilestone()
        {
            if (currentStages == null)
            {
                return;
            }

            StageMilestoneReward reward = GetSelectedMilestone();
            if (reward != null
                && milestoneStore.TryClaimBlock(reward.blockIndex, currentStages, progressStore, walletStore))
            {
                RefreshSelectedBlock();
            }
        }

        private StageMilestoneReward GetSelectedMilestone()
        {
            if (currentStages == null) return null;
            int globalBlock = selectedType == StageType.SolveStage
                ? selectedBlockIndex
                : selectedBlockIndex + 10;
            return milestoneStore.EnsureMilestones(currentStages)
                .FirstOrDefault(reward => reward.blockIndex == globalBlock);
        }

        private void SelectMode(StageType type)
        {
            selectedType = type;
            selectedBlockIndex = 0;
            BuildBlockButtons();
            RefreshSelectedBlock();
        }

        private string BuildStageList(IReadOnlyList<StageData> stages)
        {
            if (stages == null || stages.Count == 0)
            {
                return "No stages loaded.";
            }

            int solveCount = 0;
            int targetCount = 0;
            foreach (StageData stage in stages)
            {
                if (stage == null)
                {
                    continue;
                }

                if (stage.stageType == StageType.SolveStage)
                {
                    solveCount++;
                }
                else if (stage.stageType == StageType.ReverseTargetStage)
                {
                    targetCount++;
                }
            }

            int solveCleared = stages.Count(stage => stage.stageType == StageType.SolveStage && progressStore.GetProgress(stage.stageId).isCleared);
            int targetCleared = stages.Count(stage => stage.stageType == StageType.ReverseTargetStage && progressStore.GetProgress(stage.stageId).isCleared);
            int solveStars = stages.Where(stage => stage.stageType == StageType.SolveStage).Sum(stage => progressStore.GetProgress(stage.stageId).stars);
            int targetStars = stages.Where(stage => stage.stageType == StageType.ReverseTargetStage).Sum(stage => progressStore.GetProgress(stage.stageId).stars);
            return $"Solve Stages  {solveCleared}/{solveCount} cleared  |  Stars {solveStars}/{solveCount * 3}\nTarget Stages  {targetCleared}/{targetCount} cleared  |  Stars {targetStars}/{targetCount * 3}";
        }

        private RectTransform CreateBlockContainer(RectTransform parent)
        {
            GameObject containerObject = new GameObject("BlockButtons", typeof(RectTransform), typeof(GridLayoutGroup));
            containerObject.transform.SetParent(parent, false);
            RectTransform rect = containerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 512f);
            rect.sizeDelta = new Vector2(700f, 132f);

            GridLayoutGroup layout = containerObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(132f, 56f);
            layout.spacing = new Vector2(10f, 10f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
            return rect;
        }

        private RectTransform CreateButtonContainer(RectTransform parent)
        {
            GameObject viewportObject = new GameObject(
                "StageScrollViewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask),
                typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);

            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(42f, 235f);
            viewport.offsetMax = new Vector2(-42f, -420f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.02f, 0.025f, 0.035f, 0.7f);
            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject containerObject = new GameObject(
                "StageButtons",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            containerObject.transform.SetParent(viewport, false);
            RectTransform content = containerObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = containerObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 42f;
            return content;
        }

        private void BuildStageButtons(IReadOnlyList<StageData> stages)
        {
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(buttonContainer.GetChild(i).gameObject);
            }

            if (stages == null)
            {
                return;
            }

            int localStart = (selectedBlockIndex * 10) + 1;
            int stageNumberStart = selectedType == StageType.SolveStage ? localStart : localStart + 100;
            foreach (StageData stage in stages.Where(stage =>
                         stage != null
                         && stage.stageType == selectedType
                         && stage.stageNumber >= stageNumberStart
                         && stage.stageNumber < stageNumberStart + 10))
            {
                StageProgress progress = progressStore.GetProgress(stage.stageId);
                Button button = CreateStageButton(stage, progress);
                button.interactable = progress.isUnlocked;
                if (progress.isUnlocked)
                {
                    string stageId = stage.stageId;
                    button.onClick.AddListener(() =>
                    {
                        if (walletStore.Hearts <= 0)
                        {
                            ShowHeartPopup();
                            return;
                        }

                        GameLaunchContext.SetStagePlay(stageId);
                        SceneLoader.LoadGame();
                    });
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer);
            ScrollRect scrollRect = buttonContainer.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private Button CreateStageButton(StageData stage, StageProgress progress)
        {
            GameObject buttonObject = new GameObject($"{stage.stageId}Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(buttonContainer, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = stage.stageType == StageType.SolveStage
                ? new Color(0.12f, 0.16f, 0.2f, 0.96f)
                : new Color(0.08f, 0.09f, 0.1f, 0.72f);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 76f;

            Button button = buttonObject.GetComponent<Button>();
            string stateText = progress.isUnlocked ? (progress.isCleared ? "Cleared" : "Play") : "Locked";
            string stars = new string('★', progress.stars) + new string('☆', 3 - progress.stars);

            RuntimeUiFactory.CreateText(
                buttonObject.GetComponent<RectTransform>(),
                "Label",
                $"{GetLocalStageNumber(stage):00}  {stars}  {stage.difficulty}  Limit {stage.moveLimit}  {stateText}\n{UIStrings.UsesOneHeart}",
                19,
                TextAnchor.MiddleCenter);
            return button;
        }

        private void BuildBlockButtons()
        {
            for (int i = blockContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(blockContainer.GetChild(i).gameObject);
            }

            for (int block = 0; block < 10; block++)
            {
                int captured = block;
                GameObject buttonObject = new GameObject($"Block{block + 1}Button", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(blockContainer, false);
                Button button = buttonObject.GetComponent<Button>();
                bool unlocked = IsBlockUnlocked(block);
                int stars = GetBlockStars(block);
                button.interactable = unlocked;
                buttonObject.GetComponent<Image>().color = block == selectedBlockIndex
                    ? new Color(0.25f, 0.38f, 0.5f, 1f)
                    : new Color(0.12f, 0.16f, 0.2f, unlocked ? 0.96f : 0.45f);
                RuntimeUiFactory.CreateText(
                    buttonObject.GetComponent<RectTransform>(),
                    "Label",
                    unlocked ? $"Block {block + 1}\nStars {stars}/30" : $"Block {block + 1}\nLocked",
                    18,
                    TextAnchor.MiddleCenter);
                if (unlocked)
                {
                    button.onClick.AddListener(() =>
                    {
                        selectedBlockIndex = captured;
                        BuildBlockButtons();
                        RefreshSelectedBlock();
                    });
                }
            }

            SetButtonLabel(solveModeButton, selectedType == StageType.SolveStage ? "[Solve Stages]" : "Solve Stages");
            SetButtonLabel(targetModeButton, selectedType == StageType.ReverseTargetStage ? "[Target Stages]" : "Target Stages");
        }

        private void RefreshSelectedBlock()
        {
            if (currentStages == null) return;
            BuildStageButtons(currentStages);
            StageMilestoneReward reward = GetSelectedMilestone();
            int stars = reward != null ? milestoneStore.GetBlockStars(reward, currentStages, progressStore) : GetBlockStars(selectedBlockIndex);
            int cleared = GetBlockStages(selectedBlockIndex).Count(stage => progressStore.GetProgress(stage.stageId).isCleared);
            string rewardState = reward == null ? "-" : reward.isClaimed ? "Claimed" : stars >= reward.requiredStars ? "Ready" : "Locked";
            blockStatusText.text = $"Chapter {selectedBlockIndex + 1}  |  Cleared {cleared}/10  |  Stars {stars}/30  |  5 Gems: {rewardState}";
            claimMilestoneButton.gameObject.SetActive(reward != null);
            claimMilestoneButton.interactable = reward != null && !reward.isClaimed && stars >= reward.requiredStars;
            SetButtonLabel(claimMilestoneButton, reward != null && reward.isClaimed ? "Claimed" : "Claim 5 Gems");
            listText.text = BuildStageList(currentStages);
            statusText.text = $"Select an unlocked stage.  {UIStrings.UsesOneHeart}";
        }

        private void BuildHeartPopup(Transform parent)
        {
            heartPopup = new GameObject("NotEnoughHeartsPopup", typeof(RectTransform), typeof(Image));
            heartPopup.transform.SetParent(parent, false);
            RectTransform panel = heartPopup.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(680f, 430f);
            heartPopup.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.06f, 0.99f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", UIStrings.NotEnoughHearts, 36, TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(40f, 330f);
            title.rectTransform.offsetMax = new Vector2(-40f, -38f);

            heartPopupBody = RuntimeUiFactory.CreateText(panel, "Body", string.Empty, 25, TextAnchor.MiddleCenter);
            heartPopupBody.rectTransform.offsetMin = new Vector2(50f, 120f);
            heartPopupBody.rectTransform.offsetMax = new Vector2(-50f, -100f);

            Button shop = RuntimeUiFactory.CreateButton(panel, "GoToShopButton", UIStrings.GoToShop, new Vector2(-155f, 30f), new Vector2(260f, 64f));
            shop.onClick.AddListener(() =>
            {
                heartPopup.SetActive(false);
                Hide();
                shopAction?.Invoke();
            });
            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", UIStrings.Close, new Vector2(155f, 30f), new Vector2(220f, 64f));
            close.onClick.AddListener(() => heartPopup.SetActive(false));
            heartPopup.SetActive(false);
        }

        private void ShowHeartPopup()
        {
            if (heartPopup == null || heartPopup.activeSelf)
            {
                return;
            }

            int seconds = walletStore.SecondsUntilNextHeart;
            heartPopupBody.text =
                "Hearts are used to play stages.\n"
                + $"{UIStrings.HeartsRecharge}\n"
                + $"{UIStrings.NextHeartIn} {seconds / 60:00}:{seconds % 60:00}\n"
                + UIStrings.GetMoreHearts;
            heartPopup.SetActive(true);
            heartPopup.transform.SetAsLastSibling();
        }

        private IEnumerable<StageData> GetBlockStages(int block)
        {
            if (currentStages == null) return Enumerable.Empty<StageData>();
            int localStart = (block * 10) + 1;
            int numberStart = selectedType == StageType.SolveStage ? localStart : localStart + 100;
            return currentStages.Where(stage =>
                stage != null
                && stage.stageType == selectedType
                && stage.stageNumber >= numberStart
                && stage.stageNumber < numberStart + 10);
        }

        private bool IsBlockUnlocked(int block)
        {
            StageData first = GetBlockStages(block).OrderBy(stage => stage.stageNumber).FirstOrDefault();
            return first != null && progressStore.GetProgress(first.stageId).isUnlocked;
        }

        private int GetBlockStars(int block)
        {
            return GetBlockStages(block).Sum(stage => progressStore.GetProgress(stage.stageId).stars);
        }

        private static int GetLocalStageNumber(StageData stage)
        {
            return stage.stageType == StageType.ReverseTargetStage ? stage.stageNumber - 100 : stage.stageNumber;
        }

        private static void SetButtonLabel(Button button, string text)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = text;
        }

        private void AppendGroup(StringBuilder builder, IReadOnlyList<StageData> stages, StageType type, string title)
        {
            builder.AppendLine(title);
            foreach (StageData stage in stages)
            {
                if (stage == null || stage.stageType != type)
                {
                    continue;
                }

                StageProgress progress = progressStore.GetProgress(stage.stageId);
                string lockState = progress.isUnlocked ? "Unlocked" : "Locked";
                string clearState = progress.isCleared ? $"Cleared {progress.stars} stars" : "Not cleared";
                builder.Append(stage.stageNumber)
                    .Append(". ")
                    .Append(stage.title)
                    .Append(" | ")
                    .Append(stage.difficulty)
                    .Append(" | Min ")
                    .Append(GetMinimumMoves(stage))
                    .Append(" | Limit ")
                    .Append(stage.moveLimit)
                    .Append(" | ")
                    .Append(lockState)
                    .Append(" | ")
                    .Append(clearState)
                    .AppendLine();
            }
        }

        private static int GetMinimumMoves(StageData stage)
        {
            return stage.minimumMoves > 0 ? stage.minimumMoves : stage.minMoveCount;
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
