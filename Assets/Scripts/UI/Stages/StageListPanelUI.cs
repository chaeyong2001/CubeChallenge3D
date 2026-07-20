using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Core;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Stages.Generation;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Stages
{
    public sealed class StageListPanelUI
    {
        private const int StagesPerBlock = 10;
        private const int VisibleStageSlots = 8;
        private const string StageArtPath = "UI/Stages/Generated/";
        private const int HeartAdDailyLimit = 6;
        private const int HeartAdBatchLimit = 3;
        private const float HeartAdBatchCooldownMinutes = 60f;
        private static readonly Vector2 BlockRewardCardSize = new Vector2(949f, 230f);
        private const float BlockRewardCardStride = 256f;
        private static readonly Vector2 DefaultMarkerOffset = new Vector2(0f, 92f);
        private static readonly Vector2 DefaultMarkerSize = new Vector2(256f, 256f);
        private static readonly bool AutoStartNextStageAfterAdvance = false;

        private static readonly Color ScreenTop = new Color(0.03f, 0.09f, 0.33f, 1f);
        private static readonly Color ScreenBottom = new Color(0.015f, 0.028f, 0.16f, 1f);
        private static readonly Color Gold = new Color(1f, 0.71f, 0.16f, 1f);
        private static readonly Color Cream = new Color(1f, 0.94f, 0.76f, 1f);
        private static readonly Color LockedGray = new Color(0.28f, 0.32f, 0.48f, 0.82f);
        private static readonly Color ShopCardOutline = new Color(1f, 0.62f, 0.16f, 0.52f);
        private static readonly Dictionary<string, Sprite> StageSpriteCache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> MissingStageSpriteWarnings = new HashSet<string>();

        private enum StageNodeVisualState
        {
            Cleared,
            Current,
            Available,
            Locked,
            PreviewLocked
        }

        private enum StageMapDisplayMode
        {
            CurrentProgress,
            BlockBrowse
        }

        private enum BlockRewardVisualState
        {
            Locked,
            InProgress,
            ClaimReady,
            Claimed
        }

        private readonly GameObject root;
        private readonly RectTransform modeSelectRoot;
        private readonly RectTransform mapRoot;
        private readonly StageDataLoader loader = new StageDataLoader();
        private readonly StageProgressStore progressStore = new StageProgressStore();
        private readonly StageMilestoneRewardStore milestoneStore = new StageMilestoneRewardStore();
        private readonly WalletStore walletStore = new WalletStore();
        private readonly AdsRewardLimitStore adsRewardLimitStore = new AdsRewardLimitStore();
        private readonly RewardedAdService rewardService = RewardedAdService.CreateDefault();

        private IReadOnlyList<StageData> currentStages = Array.Empty<StageData>();
        private StageType selectedType = StageType.SolveStage;
        private int selectedBlockIndex;
        private StageMapDisplayMode displayMode = StageMapDisplayMode.CurrentProgress;
        private int blockBrowseScrollOffset;
        private GameObject heartPopup;
        private RectTransform heartPopupPanel;
        private Text heartPopupBody;
        private Text heartAdStatusText;
        private Button heartAdButton;
        private Coroutine heartPopupCountdownRoutine;
        private Text mapTitle;
        private Text mapSubtitle;
        private Text mapBlockChipText;
        private Image mapBlockChipImage;
        private RectTransform modeSelectTitleGroup;
        private RectTransform stageMapTitleGroup;
        private RectTransform stageMapChipRow;
        private RectTransform mapBackgroundRect;
        private RectTransform mapNodeRoot;
        private RectTransform currentStageMarker;
        private Image currentStageMarkerImage;
        private GameObject transitionFadeRoot;
        private Image transitionFadeImage;
        private bool suppressAutoMarkerPlacementDuringTransition;
        private readonly Dictionary<int, Vector2> visibleStagePositions = new Dictionary<int, Vector2>();
        private StageMapAnimationRunner animationRunner;
        private StageMapMarkerSettings markerSettings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int? debugHighestClearedLocalOverride;
#endif
        private GameObject blockSelectorPopup;
        private RectTransform blockSelectorContent;
        private GameObject blockRewardsPopup;
        private RectTransform blockRewardsContent;
        private Button mapRewardsButton;
        private RewardsButtonAttention mapRewardsAttention;
        private Action shopAction;
        private bool mapUsesArtBackground;
        private StageSlotLayoutConfig slotLayoutConfig;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private StageNodePlacementDebugUI placementDebugUI;
        private RectTransform spritePreviewPanel;
#endif

        public StageListPanelUI(Transform parent)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "StageListCanvas", 1450);
            root = canvas.gameObject;
            animationRunner = root.GetComponent<StageMapAnimationRunner>() ?? root.AddComponent<StageMapAnimationRunner>();
            markerSettings = root.GetComponent<StageMapMarkerSettings>() ?? root.AddComponent<StageMapMarkerSettings>();
            CreateStageBackdrop(root.transform);

            modeSelectRoot = CreateScreenRoot(root.transform, "StageModeSelectRoot");
            mapRoot = CreateScreenRoot(root.transform, "StageMapRoot");

            BuildModeSelect();
            BuildStageMapShell();
            BuildHeartPopup(root.transform);
            BuildBlockSelectorPopup(root.transform);
            BuildBlockRewardsPopup(root.transform);
            BuildTransitionFade(root.transform);
            Hide();
        }

        public void SetShopAction(Action action)
        {
            shopAction = action;
        }

        public void Show(string focusStageId = null)
        {
            currentStages = loader.LoadAllStages();
            progressStore.EnsureStageDefaults(currentStages);
            milestoneStore.EnsureMilestones(currentStages);
            root.SetActive(true);
            root.transform.SetAsLastSibling();

            if (GameLaunchContext.ConsumeStageAdvanceOnMainMenuRequest(out string fromStageId, out string toStageId, out bool autoStart)
                && ShowPendingStageAdvance(fromStageId, toStageId, autoStart))
            {
                AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.StageAdvanceBgmReason, true);
                return;
            }

            AudioFeedbackManager.ClearMenuBgmSuppressions();
            if (!string.IsNullOrWhiteSpace(focusStageId) && FocusStage(focusStageId))
            {
                ShowMap(selectedType);
                return;
            }

            ShowModeSelect();
        }

        private bool ShowPendingStageAdvance(string fromStageId, string toStageId, bool autoStart)
        {
            StageData fromStage = currentStages.FirstOrDefault(stage => stage != null && stage.stageId == fromStageId);
            StageData toStage = currentStages.FirstOrDefault(stage => stage != null && stage.stageId == toStageId);
            if (fromStage == null || toStage == null)
            {
                return false;
            }

            selectedType = toStage.stageType;
            displayMode = StageMapDisplayMode.CurrentProgress;
            blockBrowseScrollOffset = 0;
            modeSelectRoot.gameObject.SetActive(false);
            mapRoot.gameObject.SetActive(true);
            PrepareStageListForAdvanceAnimation(GetLocalStageNumber(fromStage), GetLocalStageNumber(toStage));
            PlayAdvanceToNextStageSequence(GetLocalStageNumber(fromStage), GetLocalStageNumber(toStage), autoStart);
            return true;
        }

        public void Hide()
        {
            StopHeartPopupCountdown();
            root.SetActive(false);
        }

        private void ShowModeSelect()
        {
            modeSelectRoot.gameObject.SetActive(true);
            mapRoot.gameObject.SetActive(false);
        }

        private void ShowMap(StageType type)
        {
            selectedType = type;
            displayMode = StageMapDisplayMode.CurrentProgress;
            blockBrowseScrollOffset = 0;
            modeSelectRoot.gameObject.SetActive(false);
            mapRoot.gameObject.SetActive(true);
            RefreshMap();
        }

        private bool FocusStage(string stageId)
        {
            StageData stage = currentStages.FirstOrDefault(item => item != null && item.stageId == stageId);
            if (stage == null)
            {
                return false;
            }

            selectedType = stage.stageType;
            selectedBlockIndex = Mathf.Clamp((GetLocalStageNumber(stage) - 1) / StagesPerBlock, 0, GetMaxBlockIndexForSelectedMode());
            return true;
        }

        private void BuildModeSelect()
        {
            Text modeTitle = CreateTitle(modeSelectRoot, "Stages", "Choose your mode and keep progressing.", -250f, 104f);
            modeSelectTitleGroup = modeTitle.transform.parent as RectTransform;

            RectTransform tutorialCard = CreateModeCard(
                modeSelectRoot,
                "TutorialModeCard",
                "TUTORIAL MODE",
                "Start with gentle cube challenges.",
                "1-10 Stages",
                "stages",
                new Color(0.10f, 0.58f, 0.86f, 0.98f),
                new Color(0.42f, 0.92f, 1f, 0.20f),
                new Vector2(0f, -570f),
                true,
                () =>
                {
                    selectedBlockIndex = 0;
                    ShowMap(StageType.TutorialStage);
                });

            RectTransform normalCard = CreateModeCard(
                modeSelectRoot,
                "NormalModeCard",
                "NORMAL MODE",
                "Solve scrambled cubes.",
                "1-300 Stages",
                "stages",
                new Color(0.08f, 0.78f, 0.48f, 0.98f),
                new Color(0.18f, 0.95f, 0.64f, 0.20f),
                new Vector2(0f, -850f),
                true,
                () =>
                {
                    selectedBlockIndex = 0;
                    ShowMap(StageType.SolveStage);
                });

            RectTransform hardCard = CreateModeCard(
                modeSelectRoot,
                "HardModeCard",
                "HARD MODE",
                "Match the target pattern.",
                "1-100 Stages",
                "target",
                new Color(0.54f, 0.20f, 0.86f, 0.98f),
                new Color(1f, 0.34f, 0.78f, 0.18f),
                new Vector2(0f, -1130f),
                true,
                () =>
                {
                    selectedBlockIndex = 0;
                    ShowMap(StageType.ReverseTargetStage);
                });

            RectTransform infinityCard = CreateModeCard(
                modeSelectRoot,
                "InfinityModeCard",
                "INFINITY MODE",
                "Unlock after clearing Normal.",
                IsInfinityUnlocked() ? "1-500 Stages" : "Locked",
                "infinity",
                LockedGray,
                new Color(0.62f, 0.50f, 1f, 0.13f),
                new Vector2(0f, -1410f),
                IsInfinityUnlocked(),
                () =>
                {
                    if (!IsInfinityUnlocked())
                    {
                        ShowModeMessage("Clear Normal to unlock Infinity Mode.");
                        return;
                    }

                    selectedBlockIndex = 0;
                    ShowMap(StageType.InfinityStage);
                });

            Button mainMenu = CasualUIFactory.CreateActionButton(
                modeSelectRoot,
                "MainMenuButton",
                "Main Menu",
                "home",
                CasualUIColor.Purple,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 72f),
                new Vector2(330f, 112f));
            UseStageButtonArt(mainMenu, "main_menu");
            mainMenu.onClick.AddListener(Hide);

            AddMobileTitleAligner(
                modeSelectRoot.gameObject,
                "ModeSelect",
                modeSelectTitleGroup,
                tutorialCard,
                normalCard,
                hardCard,
                infinityCard);
        }

        private void BuildStageMapShell()
        {
            RectTransform artBackground = CreateImage(mapRoot, "StageMapBackgroundArt", "stage_map_background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mapBackgroundRect = artBackground;
            mapUsesArtBackground = artBackground != null;
            if (artBackground != null)
            {
                artBackground.offsetMin = Vector2.zero;
                artBackground.offsetMax = Vector2.zero;
                artBackground.SetAsFirstSibling();
            }

            mapTitle = CreateTitle(mapRoot, "Normal Mode", "Clear stages and earn stars.", -260f, 96f);
            stageMapTitleGroup = mapTitle.transform.parent as RectTransform;
            mapSubtitle = mapTitle.transform.parent.Find("Subtitle")?.GetComponent<Text>();

            RectTransform chipRow = CreateEmpty(mapRoot, "ProgressChips", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(470f, 106f));
            stageMapChipRow = chipRow;
            RectTransform blockChip = CreateChip(chipRow, "BlockChip", Vector2.zero, new Vector2(310f, 68f), out mapBlockChipText);
            mapBlockChipImage = blockChip.GetComponent<Image>();
            Button blockButton = blockChip.gameObject.AddComponent<Button>();
            blockButton.transition = Selectable.Transition.None;
            blockChip.GetComponent<Image>().raycastTarget = true;
            blockButton.onClick.AddListener(ShowBlockSelectorPopup);

            RectTransform scene = CreatePanel(mapRoot, "StageMapScene", new Color(0.17f, 0.44f, 0.86f, 0.32f), 36, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 198f), new Vector2(1080f, 1260f));
            scene.pivot = new Vector2(0.5f, 0f);
            scene.anchoredPosition = new Vector2(0f, 198f);
            scene.GetComponent<Image>().raycastTarget = true;
            StageBlockBrowseScrollInput scrollInput = scene.gameObject.AddComponent<StageBlockBrowseScrollInput>();
            scrollInput.Initialize(HandleBlockBrowseScrollInput);
            if (mapUsesArtBackground)
            {
                scene.GetComponent<Image>().color = Color.clear;
            }
            else
            {
                AddMapLandscape(scene);
            }
            mapNodeRoot = CreateEmpty(mapRoot, "StageNodes", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            currentStageMarker = CreateEmpty(mapRoot, "StageCurrentMarkerHook", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, GetMarkerSize());
            currentStageMarkerImage = currentStageMarker.gameObject.AddComponent<Image>();
            if (!ApplyStageSprite(currentStageMarkerImage, "stage_cube_marker", Image.Type.Simple))
            {
                currentStageMarkerImage.enabled = false;
                Debug.LogWarning("[StageMapMarker] markerSet=False reason=missing stage_cube_marker");
            }
            else
            {
                currentStageMarkerImage.preserveAspect = true;
                currentStageMarkerImage.raycastTarget = false;
            }
            currentStageMarker.gameObject.SetActive(false);
            slotLayoutConfig = StageSlotLayoutConfig.LoadOrDefault();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CreatePlacementToolToggle();
            CreateSpritePreviewToggle();
            CreateTestAdvanceToggle();
            CreateClearTenStagesToggle();
            CreateResetProgressToggle();
            CreateStageNodeSpritePreviewPanel();
            placementDebugUI = StageNodePlacementDebugUI.Create(mapRoot, slotLayoutConfig, RefreshMap);
#endif

            Button modeSelect = CasualUIFactory.CreateActionButton(
                mapRoot,
                "ModeSelectButton",
                "Mode Select",
                "stages",
                CasualUIColor.Purple,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(12f, 84f),
                new Vector2(390f, 151f));
            UseFullArtButtonSprite(modeSelect, "button_stage_mode_select");
            modeSelect.onClick.AddListener(ShowModeSelect);

            Button rewards = CasualUIFactory.CreateActionButton(
                mapRoot,
                "RewardsButton",
                "Rewards",
                "rewards",
                CasualUIColor.Blue,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-12f, 84f),
                new Vector2(230f, 151f));
            UseFullArtButtonSprite(rewards, "button_stage_rewards");
            mapRewardsButton = rewards;
            mapRewardsAttention = rewards.gameObject.AddComponent<RewardsButtonAttention>();
            rewards.onClick.AddListener(ShowBlockRewardsPopup);

            AddMobileTitleAligner(
                mapRoot.gameObject,
                "StageMap",
                stageMapTitleGroup,
                stageMapChipRow);
        }

        private void RefreshMap()
        {
            SetMapTitleForSelectedMode();

            IReadOnlyList<StageData> modeStages = currentStages
                .Where(stage => stage != null && stage.stageType == selectedType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            int total = modeStages.Count;
            selectedBlockIndex = Mathf.Clamp(selectedBlockIndex, 0, Mathf.Max(0, (total - 1) / StagesPerBlock));
            int highestClearedLocal = GetHighestClearedLocalStage(modeStages);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugHighestClearedLocalOverride.HasValue)
            {
                highestClearedLocal = Mathf.Clamp(debugHighestClearedLocalOverride.Value, 0, total);
            }
#endif
            int currentLocal = total > 0 ? Mathf.Clamp(highestClearedLocal + 1, 1, total) : 0;
            if (displayMode == StageMapDisplayMode.BlockBrowse)
            {
                selectedBlockIndex = Mathf.Clamp(selectedBlockIndex, 0, Mathf.Max(0, (total - 1) / StagesPerBlock));
                mapBlockChipText.text = $"Block {selectedBlockIndex + 1}";
                RefreshBlockChipVisual();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                LogStageMapLayoutDebug(currentLocal, total);
#endif
                RefreshBlockBrowseView(modeStages, currentLocal, highestClearedLocal);
                RefreshRewardsButtonAttention();
                return;
            }

            selectedBlockIndex = currentLocal > 0
                ? Mathf.Clamp((currentLocal - 1) / StagesPerBlock, 0, Mathf.Max(0, (total - 1) / StagesPerBlock))
                : 0;
            mapBlockChipText.text = $"Block {selectedBlockIndex + 1}";
            RefreshBlockChipVisual();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogStageMapLayoutDebug(currentLocal, total);
#endif
            BuildMapNodes(modeStages, currentLocal, highestClearedLocal);
            RefreshRewardsButtonAttention();
        }

        private void BuildMapNodes(IReadOnlyList<StageData> modeStages, int currentLocal, int highestClearedLocal)
        {
            for (int i = mapNodeRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(mapNodeRoot.GetChild(i).gameObject);
            }
            visibleStagePositions.Clear();

            int blockNumber = currentLocal > 0 ? ((currentLocal - 1) / StagesPerBlock) + 1 : 1;
            int blockStartStage = ((blockNumber - 1) * StagesPerBlock) + 1;
            int blockEndStage = Mathf.Min(blockStartStage + StagesPerBlock - 1, modeStages.Count);
            int blockStageIndex = currentLocal > 0 ? currentLocal - blockStartStage + 1 : 1;
            Vector2? currentMarkerPosition = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageProgressLayout] currentStage={currentLocal} blockNumber={blockNumber} blockStartStage={blockStartStage} blockStageIndex={blockStageIndex}");
#endif
            for (int slotIndex = 0; slotIndex < VisibleStageSlots; slotIndex++)
            {
                int localStageNumber = blockStageIndex <= 3
                    ? blockStartStage + slotIndex
                    : currentLocal + (slotIndex - 2);
                if (localStageNumber < blockStartStage || localStageNumber > blockEndStage || localStageNumber > modeStages.Count)
                {
                    continue;
                }

                StageData stage = modeStages.FirstOrDefault(item => item != null && GetLocalStageNumber(item) == localStageNumber);
                if (stage == null)
                {
                    continue;
                }

                StageSlotLayout slot = slotLayoutConfig.GetSlot(slotIndex);
                StageNodeVisualState visualState = ResolveFixedWindowVisualState(localStageNumber, currentLocal, highestClearedLocal);
                Vector2 anchoredPosition = ToStageSlotAnchoredPosition(slot.normalizedPosition);
                visibleStagePositions[localStageNumber] = anchoredPosition;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                LogStageNodeSlot(slotIndex, stage, visualState, slot, anchoredPosition);
                Debug.Log($"[StageProgressSlot] slotIndex={slotIndex + 1} displayStageNumber={localStageNumber} state={visualState}");
#endif
                CreateStageNode(mapNodeRoot, stage, anchoredPosition, slot.scale, visualState);
                if (visualState == StageNodeVisualState.Current)
                {
                    currentMarkerPosition = anchoredPosition + GetMarkerOffset();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[StageMarkerSlot] currentStage={currentLocal} currentSlotIndex={slotIndex + 1} blockStageIndex={blockStageIndex}");
#endif
                }
            }

            if (!suppressAutoMarkerPlacementDuringTransition)
            {
                RefreshCurrentStageMarker(currentMarkerPosition);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageMapRefresh] visibleStages={string.Join(",", visibleStagePositions.Keys.OrderBy(value => value))} currentStage={currentLocal}");
#endif
        }

        private void PrepareStageListForAdvanceAnimation(int fromStage, int toStage)
        {
            SetMapTitleForSelectedMode();

            IReadOnlyList<StageData> modeStages = currentStages
                .Where(stage => stage != null && stage.stageType == selectedType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            int total = modeStages.Count;
            int safeFromStage = Mathf.Clamp(fromStage, 1, Mathf.Max(1, total));
            selectedBlockIndex = Mathf.Clamp((safeFromStage - 1) / StagesPerBlock, 0, Mathf.Max(0, (total - 1) / StagesPerBlock));
            mapBlockChipText.text = $"Block {selectedBlockIndex + 1}";
            suppressAutoMarkerPlacementDuringTransition = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageNextTransition] ShowStageList visibleBlock={selectedBlockIndex + 1} fromStage={fromStage} toStage={toStage}");
            Debug.Log("[StageNextTransition] SuppressAutoMarkerPlacement=true");
#endif
            BuildMapNodes(modeStages, safeFromStage, Mathf.Max(0, safeFromStage - 1));
            suppressAutoMarkerPlacementDuringTransition = false;

            Vector2 markerOffset = GetMarkerOffset();
            if (visibleStagePositions.TryGetValue(safeFromStage, out Vector2 fromNodePosition))
            {
                Vector2 markerPosition = fromNodePosition + markerOffset;
                RefreshCurrentStageMarker(markerPosition);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                int fromSlot = GetVisibleSlotIndexForStage(safeFromStage, safeFromStage);
                int toSlot = GetVisibleSlotIndexForStage(safeFromStage, toStage);
                Debug.Log($"[StageNextTransition] Prepare fromStage={safeFromStage} toStage={toStage} fromSlot={fromSlot} toSlot={toSlot}");
                Debug.Log($"[StageNextTransition] MarkerInitialPosition fromSlot={fromSlot} pos={markerPosition}");
#endif
            }
            else
            {
                RefreshCurrentStageMarker(null);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[StageNextTransition] MarkerInitialPosition missing fromStage={safeFromStage} toStage={toStage}");
#endif
            }
        }

        private static int GetVisibleSlotIndexForStage(int currentLocal, int localStageNumber)
        {
            int blockNumber = currentLocal > 0 ? ((currentLocal - 1) / StagesPerBlock) + 1 : 1;
            int blockStartStage = ((blockNumber - 1) * StagesPerBlock) + 1;
            int blockStageIndex = currentLocal > 0 ? currentLocal - blockStartStage + 1 : 1;
            for (int slotIndex = 0; slotIndex < VisibleStageSlots; slotIndex++)
            {
                int displayStageNumber = blockStageIndex <= 3
                    ? blockStartStage + slotIndex
                    : currentLocal + (slotIndex - 2);
                if (displayStageNumber == localStageNumber)
                {
                    return slotIndex + 1;
                }
            }

            return -1;
        }

        private void RefreshBlockBrowseView(IReadOnlyList<StageData> modeStages, int currentLocal, int highestClearedLocal)
        {
            for (int i = mapNodeRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(mapNodeRoot.GetChild(i).gameObject);
            }
            visibleStagePositions.Clear();

            int blockStartStage = (selectedBlockIndex * StagesPerBlock) + 1;
            int blockEndStage = Mathf.Min(blockStartStage + StagesPerBlock - 1, modeStages.Count);
            int blockStageCount = Mathf.Max(0, blockEndStage - blockStartStage + 1);
            int maxScrollOffset = Mathf.Max(0, blockStageCount - VisibleStageSlots);
            blockBrowseScrollOffset = Mathf.Clamp(blockBrowseScrollOffset, 0, maxScrollOffset);

            Vector2? currentMarkerPosition = null;
            for (int slotIndex = 0; slotIndex < VisibleStageSlots; slotIndex++)
            {
                int localStageNumber = blockStartStage + blockBrowseScrollOffset + slotIndex;
                if (localStageNumber < blockStartStage || localStageNumber > blockEndStage)
                {
                    continue;
                }

                StageData stage = modeStages.FirstOrDefault(item => item != null && GetLocalStageNumber(item) == localStageNumber);
                if (stage == null)
                {
                    continue;
                }

                StageSlotLayout slot = slotLayoutConfig.GetSlot(slotIndex);
                StageNodeVisualState visualState = ResolveFixedWindowVisualState(localStageNumber, currentLocal, highestClearedLocal);
                Vector2 anchoredPosition = ToStageSlotAnchoredPosition(slot.normalizedPosition);
                visibleStagePositions[localStageNumber] = anchoredPosition;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                LogStageNodeSlot(slotIndex, stage, visualState, slot, anchoredPosition);
#endif
                CreateStageNode(mapNodeRoot, stage, anchoredPosition, slot.scale, visualState);
                if (localStageNumber == currentLocal)
                {
                    currentMarkerPosition = anchoredPosition + GetMarkerOffset();
                }
            }

            if (!suppressAutoMarkerPlacementDuringTransition)
            {
                RefreshCurrentStageMarker(currentMarkerPosition);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageBlockBrowse] block={selectedBlockIndex + 1} blockStartStage={blockStartStage} blockEndStage={blockEndStage} scrollOffset={blockBrowseScrollOffset}");
            Debug.Log($"[StageMapRefresh] mode=BlockBrowse visibleStages={string.Join(",", visibleStagePositions.Keys.OrderBy(value => value))} currentStage={currentLocal}");
#endif
        }

        private void HandleBlockBrowseScrollInput(float direction)
        {
            if (displayMode != StageMapDisplayMode.BlockBrowse || Mathf.Approximately(direction, 0f))
            {
                return;
            }

            int maxScrollOffset = GetSelectedBlockMaxScrollOffset();
            int nextOffset = Mathf.Clamp(blockBrowseScrollOffset + (direction > 0f ? 1 : -1), 0, maxScrollOffset);
            if (nextOffset == blockBrowseScrollOffset)
            {
                return;
            }

            blockBrowseScrollOffset = nextOffset;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageBlockBrowseScroll] block={selectedBlockIndex + 1} scrollOffset={blockBrowseScrollOffset}");
#endif
            RefreshMap();
        }

        private int GetSelectedBlockMaxScrollOffset()
        {
            int total = currentStages.Count(stage => stage != null && stage.stageType == selectedType);
            int blockStartStage = (selectedBlockIndex * StagesPerBlock) + 1;
            int blockEndStage = Mathf.Min(blockStartStage + StagesPerBlock - 1, total);
            int blockStageCount = Mathf.Max(0, blockEndStage - blockStartStage + 1);
            return Mathf.Max(0, blockStageCount - VisibleStageSlots);
        }

        private Vector2 ToStageSlotAnchoredPosition(Vector2 normalizedPosition)
        {
            Rect rect = mapRoot.rect;
            float width = rect.width > 1f ? rect.width : 1080f;
            float height = rect.height > 1f ? rect.height : 1920f;
            return new Vector2((normalizedPosition.x - 0.5f) * width, normalizedPosition.y * height);
        }

        private Vector2 GetMarkerOffset()
        {
            return markerSettings != null ? markerSettings.markerOffset : DefaultMarkerOffset;
        }

        private Vector2 GetMarkerSize()
        {
            return markerSettings != null ? markerSettings.markerSize : DefaultMarkerSize;
        }

        private float GetMarkerHopDuration()
        {
            return markerSettings != null ? Mathf.Max(0.05f, markerSettings.markerHopDuration) : 0.42f;
        }

        private float GetMarkerHopHeight(bool hopInPlace)
        {
            if (markerSettings == null)
            {
                return hopInPlace ? 42f : 58f;
            }

            return hopInPlace ? markerSettings.markerHopInPlaceHeight : markerSettings.markerHopMoveHeight;
        }

        private float GetWaitBeforeMarkerHop()
        {
            return markerSettings != null ? Mathf.Max(0f, markerSettings.waitBeforeMarkerHop) : 0.25f;
        }

        private float GetWaitAfterMarkerHop()
        {
            return markerSettings != null ? Mathf.Max(0f, markerSettings.waitAfterMarkerHop) : 0.35f;
        }

        private float GetScreenFadeDuration()
        {
            return markerSettings != null ? Mathf.Max(0.01f, markerSettings.screenFadeDuration) : 0.25f;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void CreatePlacementToolToggle()
        {
            Button toggle = RuntimeUiFactory.CreateButton(
                mapRoot,
                "StageNodePlacementToolToggle",
                "Node Tool",
                new Vector2(-430f, -510f),
                new Vector2(150f, 48f));
            RectTransform rect = toggle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            CasualUIStyle.ApplyPanel(toggle.GetComponent<Image>(), new Color(0.03f, 0.08f, 0.22f, 0.82f), 12);
            Text label = toggle.GetComponentInChildren<Text>();
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;

            bool visible = false;
            toggle.onClick.AddListener(() =>
            {
                visible = !visible;
                placementDebugUI?.SetVisible(visible);
            });
        }

        private void CreateSpritePreviewToggle()
        {
            Button toggle = RuntimeUiFactory.CreateButton(
                mapRoot,
                "StageNodeSpritePreviewToggle",
                "Sprite Preview",
                new Vector2(-250f, -510f),
                new Vector2(190f, 48f));
            RectTransform rect = toggle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            CasualUIStyle.ApplyPanel(toggle.GetComponent<Image>(), new Color(0.03f, 0.08f, 0.22f, 0.82f), 12);
            Text label = toggle.GetComponentInChildren<Text>();
            label.fontSize = 17;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;

            toggle.onClick.AddListener(() =>
            {
                if (spritePreviewPanel != null)
                {
                    spritePreviewPanel.gameObject.SetActive(!spritePreviewPanel.gameObject.activeSelf);
                    if (spritePreviewPanel.gameObject.activeSelf)
                    {
                        spritePreviewPanel.SetAsLastSibling();
                    }
                }
            });
        }

        private void CreateTestAdvanceToggle()
        {
            Button toggle = RuntimeUiFactory.CreateButton(
                mapRoot,
                "StageMapTestAdvanceToggle",
                "Test Advance",
                new Vector2(-35f, -510f),
                new Vector2(190f, 48f));
            RectTransform rect = toggle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            CasualUIStyle.ApplyPanel(toggle.GetComponent<Image>(), new Color(0.03f, 0.08f, 0.22f, 0.82f), 12);
            Text label = toggle.GetComponentInChildren<Text>();
            label.fontSize = 17;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            toggle.onClick.AddListener(TestAdvanceStage);
        }

        private void CreateResetProgressToggle()
        {
            Button toggle = RuntimeUiFactory.CreateButton(
                mapRoot,
                "StageMapResetProgressToggle",
                "Reset",
                new Vector2(178f, -510f),
                new Vector2(184f, 48f));
            RectTransform rect = toggle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            CasualUIStyle.ApplyPanel(toggle.GetComponent<Image>(), new Color(0.11f, 0.07f, 0.02f, 0.86f), 12);
            Text label = toggle.GetComponentInChildren<Text>();
            label.fontSize = 17;
            label.fontStyle = FontStyle.Bold;
            label.color = Cream;
            toggle.onClick.AddListener(ResetStageProgressForDebug);
        }

        private void CreateClearTenStagesToggle()
        {
            Button toggle = RuntimeUiFactory.CreateButton(
                mapRoot,
                "StageMapClearTenToggle",
                "Clear +10",
                new Vector2(-35f, -458f),
                new Vector2(190f, 48f));
            RectTransform rect = toggle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            CasualUIStyle.ApplyPanel(toggle.GetComponent<Image>(), new Color(0.06f, 0.16f, 0.08f, 0.86f), 12);
            Text label = toggle.GetComponentInChildren<Text>();
            label.fontSize = 17;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            toggle.onClick.AddListener(ClearTenStagesForDebug);
        }

        private void TestAdvanceStage()
        {
            IReadOnlyList<StageData> modeStages = currentStages
                .Where(stage => stage != null && stage.stageType == selectedType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            if (modeStages.Count == 0)
            {
                return;
            }

            int realHighest = GetHighestClearedLocalStage(modeStages);
            int highestCleared = Mathf.Clamp(debugHighestClearedLocalOverride ?? realHighest, 0, modeStages.Count - 1);
            int fromStage = Mathf.Clamp(highestCleared + 1, 1, modeStages.Count);
            int toStage = Mathf.Clamp(fromStage + 1, 1, modeStages.Count);
            if (toStage == fromStage)
            {
                return;
            }

            PlayAdvanceToNextStageAnimation(fromStage, toStage, () =>
            {
                debugHighestClearedLocalOverride = Mathf.Clamp(toStage - 1, 0, modeStages.Count);
            });
        }

        private void ClearTenStagesForDebug()
        {
            IReadOnlyList<StageData> modeStages = currentStages
                .Where(stage => stage != null && stage.stageType == selectedType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            if (modeStages.Count == 0)
            {
                return;
            }

            int highestCleared = GetHighestClearedLocalStage(modeStages);
            int firstLocal = Mathf.Clamp(highestCleared + 1, 1, modeStages.Count);
            int lastLocal = Mathf.Clamp(highestCleared + 10, 1, modeStages.Count);
            if (highestCleared >= modeStages.Count)
            {
                return;
            }

            for (int local = firstLocal; local <= lastLocal; local++)
            {
                StageData stage = modeStages[local - 1];
                progressStore.UnlockStage(stage.stageId);
                progressStore.MarkCleared(stage.stageId, 0, 0f, 3);
            }

            int nextLocal = Mathf.Min(lastLocal + 1, modeStages.Count);
            if (nextLocal > lastLocal)
            {
                progressStore.UnlockStage(modeStages[nextLocal - 1].stageId);
            }

            debugHighestClearedLocalOverride = null;
            displayMode = StageMapDisplayMode.CurrentProgress;
            blockBrowseScrollOffset = 0;
            RefreshMap();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageDebug] clearTen=True mode={selectedType} clearedLocal={firstLocal}-{lastLocal} nextLocal={nextLocal}");
#endif
        }

        private void ResetStageProgressForDebug()
        {
            progressStore.ClearAllForDebug();
            milestoneStore.ClearClaimedForDebug();
            progressStore.EnsureStageDefaults(currentStages);
            debugHighestClearedLocalOverride = null;
            displayMode = StageMapDisplayMode.CurrentProgress;
            blockBrowseScrollOffset = 0;
            RefreshMap();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[StageDebug] resetProgress=True resetBlockRewards=True currentStage=1");
#endif
        }

        private void CreateStageNodeSpritePreviewPanel()
        {
            spritePreviewPanel = CreatePanel(
                mapRoot,
                "StageNodeSpritePreviewPanel",
                new Color(0.02f, 0.04f, 0.08f, 0.96f),
                18,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-250f, 20f),
                new Vector2(620f, 1120f));
            spritePreviewPanel.pivot = new Vector2(1f, 0.5f);
            AddOutline(spritePreviewPanel.gameObject, Gold, 2f);

            Text title = RuntimeUiFactory.CreateText(spritePreviewPanel, "Title", "Stage Node Sprite Preview", 25, TextAnchor.UpperCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-30f, 36f);
            title.fontStyle = FontStyle.Bold;
            title.color = Cream;

            ScrollRect scrollRect = spritePreviewPanel.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 34f;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(spritePreviewPanel, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(18f, 18f);
            viewport.offsetMax = new Vector2(-18f, -68f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewport;

            string[] spriteNames =
            {
                "node_unlocked",
                "node_current",
                "node_locked",
                "node_star_1",
                "node_star_2",
                "node_star_3",
                "lock_silver",
                "icon_star_large",
                "star_row_1",
                "star_row_2",
                "star_row_3",
                "node_body_blue",
                "node_body_current",
                "node_body_locked",
                "current_glow",
                "lock_icon"
            };

            const float itemHeight = 176f;
            const float itemGap = 12f;
            const float contentPadding = 10f;
            float contentHeight = contentPadding + (spriteNames.Length * itemHeight) + ((spriteNames.Length - 1) * itemGap) + contentPadding;
            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, contentHeight);
            scrollRect.content = content;

            for (int i = 0; i < spriteNames.Length; i++)
            {
                CreateSpritePreviewItem(content, spriteNames[i], i, contentPadding, itemHeight, itemGap);
            }

            spritePreviewPanel.gameObject.SetActive(false);
        }

        private void CreateSpritePreviewItem(RectTransform parent, string spriteName, int index, float topPadding, float itemHeight, float itemGap)
        {
            GameObject itemObject = new GameObject($"{spriteName}Preview", typeof(RectTransform), typeof(Image));
            itemObject.transform.SetParent(parent, false);
            RectTransform item = itemObject.GetComponent<RectTransform>();
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.anchoredPosition = new Vector2(0f, -topPadding - (index * (itemHeight + itemGap)));
            item.sizeDelta = new Vector2(-18f, itemHeight);
            Image itemImage = itemObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(itemImage, new Color(0.04f, 0.09f, 0.16f, 0.88f), 12);
            itemImage.raycastTarget = false;

            Sprite sprite = LoadStageSprite(spriteName);
            Vector2 textureSize = sprite != null ? new Vector2(sprite.texture.width, sprite.texture.height) : Vector2.zero;
            Vector2 displaySize = GetPreviewDisplaySize(textureSize, 150f, 104f);

            Text nameText = RuntimeUiFactory.CreateText(item, "NameText", spriteName, 17, TextAnchor.UpperCenter);
            nameText.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameText.rectTransform.anchorMax = new Vector2(0f, 1f);
            nameText.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameText.rectTransform.anchoredPosition = new Vector2(92f, -8f);
            nameText.rectTransform.sizeDelta = new Vector2(170f, 24f);
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = sprite != null ? Cream : new Color(1f, 0.45f, 0.45f, 1f);
            nameText.raycastTarget = false;

            Text sizeText = RuntimeUiFactory.CreateText(item, "SizeText", sprite != null ? $"{textureSize.x:0} x {textureSize.y:0}" : "Missing", 15, TextAnchor.UpperCenter);
            sizeText.rectTransform.anchorMin = new Vector2(0f, 1f);
            sizeText.rectTransform.anchorMax = new Vector2(0f, 1f);
            sizeText.rectTransform.pivot = new Vector2(0.5f, 1f);
            sizeText.rectTransform.anchoredPosition = new Vector2(92f, -32f);
            sizeText.rectTransform.sizeDelta = new Vector2(170f, 22f);
            sizeText.color = sprite != null ? Color.white : new Color(1f, 0.45f, 0.45f, 1f);
            sizeText.raycastTarget = false;

            RectTransform imageRect = CreateEmpty(item, "Sprite", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, -20f), displaySize);
            Image image = imageRect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = sprite != null ? Color.white : new Color(1f, 0.2f, 0.2f, 0.18f);
            image.raycastTarget = false;

            string alphaBBox = sprite != null ? CalculateAlphaBounds(sprite.texture) : "missing";
            string usage = GetStageSpriteUsage(spriteName);
            string sourcePath = $"Resources/UI/Stages/Generated/{spriteName}";
            Text info = RuntimeUiFactory.CreateText(item, "Info", $"loaded: {(sprite != null ? "yes" : "no")}\nrect: {displaySize.x:0}x{displaySize.y:0}\nbbox: {alphaBBox}\npath: {sourcePath}\nused: {usage}", 12, TextAnchor.UpperLeft);
            info.rectTransform.anchorMin = new Vector2(0f, 0f);
            info.rectTransform.anchorMax = new Vector2(1f, 1f);
            info.rectTransform.pivot = new Vector2(0f, 0.5f);
            info.rectTransform.offsetMin = new Vector2(184f, 10f);
            info.rectTransform.offsetMax = new Vector2(-10f, -10f);
            info.color = Color.white;
            info.raycastTarget = false;
        }

        private static Vector2 GetPreviewDisplaySize(Vector2 textureSize, float maxWidth, float maxHeight)
        {
            if (textureSize.x <= 0f || textureSize.y <= 0f)
            {
                return new Vector2(maxWidth, maxHeight);
            }

            float scale = Mathf.Min(maxWidth / textureSize.x, maxHeight / textureSize.y);
            return new Vector2(textureSize.x * scale, textureSize.y * scale);
        }

        private static string GetStageSpriteUsage(string spriteName)
        {
            switch (spriteName)
            {
                case "node_unlocked":
                    return "legacy preview only";
                case "node_current":
                    return "legacy preview only";
                case "node_locked":
                    return "legacy preview only";
                case "lock_silver":
                    return "legacy fallback lock";
                case "node_body_blue":
                    return "body: cleared/available";
                case "node_body_current":
                    return "body: current";
                case "node_body_locked":
                    return "body: locked/preview";
                case "star_row_1":
                    return "cleared 1-star row";
                case "star_row_2":
                    return "cleared 2-star row";
                case "star_row_3":
                    return "cleared 3-star row";
                case "current_glow":
                    return "current glow";
                case "lock_icon":
                    return "lock icon";
                case "node_star_1":
                case "node_star_2":
                case "node_star_3":
                case "icon_star_large":
                    return "not used by current StageNodeView";
                default:
                    return "unknown";
            }
        }

        private static string CalculateAlphaBounds(Texture2D texture)
        {
            if (texture == null)
            {
                return "missing";
            }

            try
            {
                Color32[] pixels = texture.GetPixels32();
                int minX = texture.width;
                int minY = texture.height;
                int maxX = -1;
                int maxY = -1;

                for (int y = 0; y < texture.height; y++)
                {
                    int row = y * texture.width;
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[row + x].a == 0)
                        {
                            continue;
                        }

                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                return maxX < 0 ? "empty" : $"{minX},{minY}-{maxX + 1},{maxY + 1}";
            }
            catch (Exception)
            {
                return "unreadable";
            }
        }

        private void LogStageMapLayoutDebug(int currentStage, int visibleStageCount)
        {
            Canvas canvas = root != null ? root.GetComponent<Canvas>() : null;
            string platform =
#if UNITY_ANDROID
                "Android";
#elif UNITY_EDITOR
                "Editor";
#else
                Application.platform.ToString();
#endif

            Debug.Log(
                "[StageMapLayoutDebug] "
                + $"platform={platform} "
                + $"screen={Screen.width}x{Screen.height} "
                + $"safeArea={FormatRect(Screen.safeArea)} "
                + $"persistentDataPath={Application.persistentDataPath}");

            Debug.Log(
                "[StageMapLayoutDebug] "
                + $"layoutSource={slotLayoutConfig.loadedSource} "
                + $"layoutPath={slotLayoutConfig.loadedPath} "
                + $"fileExists={slotLayoutConfig.loadedFileExists} "
                + $"usingDefault={slotLayoutConfig.usingDefaultLayout} "
                + $"forceLastSlotAsLockPreview={slotLayoutConfig.forceLastSlotAsLockPreview}");

            Debug.Log(
                "[StageMapLayoutDebug] "
                + $"canvasScaleFactor={(canvas != null ? canvas.scaleFactor : 0f):0.###} "
                + $"currentStage={currentStage} "
                + $"visibleSlotCount={VisibleStageSlots} "
                + $"modeStageCount={visibleStageCount} "
                + $"mapRoot={FormatRectTransform(mapRoot)} "
                + $"background={FormatRectTransform(mapBackgroundRect)} "
                + $"nodeContainer={FormatRectTransform(mapNodeRoot)} "
                + $"sameParent={GetParentName(mapBackgroundRect) == GetParentName(mapNodeRoot)} "
                + $"backgroundParent={GetParentName(mapBackgroundRect)} "
                + $"nodeParent={GetParentName(mapNodeRoot)} "
                + $"conversionBasis=mapRoot.rect");
        }

        private void LogStageNodeSlot(int slotIndex, StageData stage, StageNodeVisualState state, StageSlotLayout slot, Vector2 convertedAnchoredPosition)
        {
            string bodySprite = GetStageNodeBodySpriteName(state);
            string starRow = state == StageNodeVisualState.Cleared ? GetStageNodeStarRowSpriteName(progressStore.GetProgress(stage.stageId).stars) : "-";
            Vector2 size = GetStageNodeSize(state);
            Debug.Log(
                $"[StageNodeSlot] slot={slotIndex} stage={GetLocalStageNumber(stage)} state={state} "
                + $"norm=({slot.normalizedPosition.x:0.0000},{slot.normalizedPosition.y:0.0000}) "
                + $"scale={slot.scale:0.0000} "
                + $"convertedAnchored=({convertedAnchoredPosition.x:0.0},{convertedAnchoredPosition.y:0.0}) "
                + $"finalAnchored=({convertedAnchoredPosition.x:0.0},{convertedAnchoredPosition.y:0.0}) "
                + $"finalLocalScale=({slot.scale:0.0000},{slot.scale:0.0000},{slot.scale:0.0000}) "
                + $"bodySprite={bodySprite} starRow={starRow} sizeDelta={size.x:0}x{size.y:0} "
                + $"imageType=Simple preserveAspect=True tint=White "
                + $"numberVisible={(state != StageNodeVisualState.Locked && state != StageNodeVisualState.PreviewLocked)} "
                + $"lockVisible={(state == StageNodeVisualState.Locked || state == StageNodeVisualState.PreviewLocked)}");
        }

        private static string FormatRect(Rect rect)
        {
            return $"x={rect.x:0.0},y={rect.y:0.0},w={rect.width:0.0},h={rect.height:0.0}";
        }

        private static string FormatRectTransform(RectTransform rect)
        {
            if (rect == null)
            {
                return "null";
            }

            return $"{rect.name}[anchorMin=({rect.anchorMin.x:0.###},{rect.anchorMin.y:0.###}) "
                + $"anchorMax=({rect.anchorMax.x:0.###},{rect.anchorMax.y:0.###}) "
                + $"pivot=({rect.pivot.x:0.###},{rect.pivot.y:0.###}) "
                + $"anchored=({rect.anchoredPosition.x:0.0},{rect.anchoredPosition.y:0.0}) "
                + $"sizeDelta=({rect.sizeDelta.x:0.0},{rect.sizeDelta.y:0.0}) "
                + $"rect=({rect.rect.width:0.0},{rect.rect.height:0.0}) "
                + $"localScale=({rect.localScale.x:0.###},{rect.localScale.y:0.###},{rect.localScale.z:0.###})]";
        }

        private static string GetParentName(RectTransform rect)
        {
            return rect != null && rect.parent != null ? rect.parent.name : "null";
        }
#endif

        private void BuildTransitionFade(Transform parent)
        {
            transitionFadeRoot = new GameObject("StageTransitionFade", typeof(RectTransform), typeof(Image));
            transitionFadeRoot.transform.SetParent(parent, false);
            RectTransform rect = transitionFadeRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            transitionFadeImage = transitionFadeRoot.GetComponent<Image>();
            transitionFadeImage.color = new Color(0f, 0f, 0f, 0f);
            transitionFadeImage.raycastTarget = true;
            transitionFadeRoot.SetActive(false);
        }

        private IEnumerator FadeTransitionOverlay(float fromAlpha, float toAlpha, float duration)
        {
            if (transitionFadeRoot == null || transitionFadeImage == null)
            {
                yield break;
            }

            transitionFadeRoot.SetActive(true);
            transitionFadeRoot.transform.SetAsLastSibling();
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            Color color = transitionFadeImage.color;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                transitionFadeImage.color = color;
                yield return null;
            }

            color.a = toAlpha;
            transitionFadeImage.color = color;
            if (Mathf.Approximately(toAlpha, 0f))
            {
                transitionFadeRoot.SetActive(false);
            }
        }

        private void CreateStageNode(RectTransform parent, StageData stage, Vector2 position, float scale, StageNodeVisualState visualState)
        {
            StageProgress progress = progressStore.GetProgress(stage.stageId);
            bool locked = visualState == StageNodeVisualState.Locked || visualState == StageNodeVisualState.PreviewLocked;

            Button button = RuntimeUiFactory.CreateButton(parent, $"{stage.stageId}Node", string.Empty, position, GetStageNodeSize(visualState));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * scale;
            Image image = button.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.001f);
            button.transition = Selectable.Transition.None;
            button.interactable = true;

            Text baseLabel = button.GetComponentInChildren<Text>();
            if (baseLabel != null)
            {
                baseLabel.gameObject.SetActive(false);
            }

            StageNodeView visual = StageNodeView.Create(rect);
            visual.SetVisualState(visualState, GetLocalStageNumber(stage), progress.stars);

            string stageId = stage.stageId;
            button.onClick.AddListener(() => OnStageNodeClicked(stageId, visualState));
        }

        private void OnStageNodeClicked(string stageId, StageNodeVisualState visualState)
        {
            if (visualState == StageNodeVisualState.Locked || visualState == StageNodeVisualState.PreviewLocked)
            {
                ShowModeMessage("Clear previous stages to unlock this stage.");
                return;
            }

            StartStage(stageId);
        }

        private static StageNodeVisualState ResolveFixedWindowVisualState(int displayStageNumber, int currentStageNumber, int highestClearedStage)
        {
            if (displayStageNumber <= highestClearedStage)
            {
                return StageNodeVisualState.Cleared;
            }

            if (displayStageNumber == currentStageNumber)
            {
                return StageNodeVisualState.Current;
            }

            return StageNodeVisualState.Locked;
        }

        private static StageNodeVisualState ResolveStageNodeVisualState(StageProgress progress, bool forceLockPreview, bool isCurrentStage)
        {
            if (forceLockPreview)
            {
                return StageNodeVisualState.PreviewLocked;
            }

            if (!progress.isUnlocked)
            {
                return StageNodeVisualState.Locked;
            }

            if (progress.isCleared)
            {
                return StageNodeVisualState.Cleared;
            }

            return isCurrentStage ? StageNodeVisualState.Current : StageNodeVisualState.Available;
        }

        private static string GetStageNodeBodySpriteName(StageNodeVisualState state)
        {
            switch (state)
            {
                case StageNodeVisualState.Current:
                    return "node_body_current";
                case StageNodeVisualState.Locked:
                case StageNodeVisualState.PreviewLocked:
                    return "node_body_locked";
                default:
                    return "node_body_blue";
            }
        }

        private static string GetStageNodeStarRowSpriteName(int stars)
        {
            return $"star_row_{Mathf.Clamp(stars, 1, 3)}";
        }

        private static Vector2 GetStageNodeSize(StageNodeVisualState state)
        {
            switch (state)
            {
                case StageNodeVisualState.Current:
                    return new Vector2(190f, 128f);
                case StageNodeVisualState.Cleared:
                    return new Vector2(166f, 126f);
                case StageNodeVisualState.Locked:
                case StageNodeVisualState.PreviewLocked:
                    return new Vector2(148f, 112f);
                default:
                    return new Vector2(148f, 102f);
            }
        }

        private static Color GetStageNodeFallbackColor(StageNodeVisualState state)
        {
            switch (state)
            {
                case StageNodeVisualState.Current:
                    return new Color(0.95f, 0.13f, 0.42f, 1f);
                case StageNodeVisualState.Cleared:
                case StageNodeVisualState.Available:
                    return new Color(0.08f, 0.45f, 0.96f, 1f);
                default:
                    return LockedGray;
            }
        }

        private sealed class StageNodeView
        {
            private const float NumberVerticalLift = 12f;

            private readonly Image glowImage;
            private readonly Image bodyImage;
            private readonly Text numberText;
            private readonly Image starRowImage;
            private readonly Image lockIconImage;

            private StageNodeView(
                Image glowImage,
                Image bodyImage,
                Text numberText,
                Image starRowImage,
                Image lockIconImage)
            {
                this.glowImage = glowImage;
                this.bodyImage = bodyImage;
                this.numberText = numberText;
                this.starRowImage = starRowImage;
                this.lockIconImage = lockIconImage;
            }

            public static StageNodeView Create(RectTransform parent)
            {
                RectTransform visualRoot = CreateEmpty(parent, "VisualRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                visualRoot.offsetMin = Vector2.zero;
                visualRoot.offsetMax = Vector2.zero;

                Image glowImage = CreateLayer(visualRoot, "GlowImage", Vector2.zero, new Vector2(230f, 180f));
                Image bodyImage = CreateLayer(visualRoot, "BodyImage", Vector2.zero, new Vector2(170f, 116f));
                Image starRowImage = CreateLayer(visualRoot, "StarRowImage", new Vector2(0f, -45f), new Vector2(122f, 48f));
                Image lockIconImage = CreateLayer(visualRoot, "LockIconImage", Vector2.zero, new Vector2(58f, 58f));

                Text numberText = RuntimeUiFactory.CreateText(visualRoot, "NumberText", string.Empty, 48, TextAnchor.MiddleCenter);
                numberText.fontStyle = FontStyle.Bold;
                numberText.color = Color.white;
                numberText.raycastTarget = false;
                CasualUIStyle.ApplyTextDepth(numberText, true);

                return new StageNodeView(glowImage, bodyImage, numberText, starRowImage, lockIconImage);
            }

            public void SetVisualState(StageNodeVisualState state, int stageNumber, int stars)
            {
                bool locked = state == StageNodeVisualState.Locked || state == StageNodeVisualState.PreviewLocked;
                bool current = state == StageNodeVisualState.Current;
                bool cleared = state == StageNodeVisualState.Cleared;

                ApplyBody(state);
                ConfigureBodyRect(state);
                ConfigureNumber(state);

                numberText.gameObject.SetActive(!locked);
                numberText.text = locked ? string.Empty : stageNumber.ToString();
                numberText.fontSize = current ? 62 : 46;

                lockIconImage.gameObject.SetActive(locked);
                if (locked)
                {
                    if (!ApplyOptionalSprite(lockIconImage, "lock_icon"))
                    {
                        ApplyOptionalSprite(lockIconImage, "lock_silver");
                    }
                }

                if (current)
                {
                    if (ApplyOptionalSprite(glowImage, "current_glow"))
                    {
                        glowImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        glowImage.gameObject.SetActive(false);
                        WarnMissingSprite("current_glow", "Current stage glow is hidden until a dedicated current_glow sprite is provided.");
                    }
                }
                else
                {
                    glowImage.gameObject.SetActive(false);
                }

                starRowImage.gameObject.SetActive(false);
                if (cleared)
                {
                    string starRowSpriteName = GetStageNodeStarRowSpriteName(stars);
                    if (ApplyOptionalSprite(starRowImage, starRowSpriteName))
                    {
                        ConfigureStarRowRect(stars);
                        starRowImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        WarnMissingSprite(starRowSpriteName, "Cleared stage star rows are not shown until dedicated star_row sprites are provided.");
                    }
                }
            }

            private void ApplyBody(StageNodeVisualState state)
            {
                string bodySpriteName = GetStageNodeBodySpriteName(state);
                if (ApplyOptionalSprite(bodyImage, bodySpriteName))
                {
                    bodyImage.color = Color.white;
                    return;
                }

                bodyImage.sprite = null;
                bodyImage.color = GetStageNodeFallbackColor(state);
            }

            private void ConfigureBodyRect(StageNodeVisualState state)
            {
                RectTransform bodyRect = bodyImage.rectTransform;
                RectTransform glowRect = glowImage.rectTransform;
                RectTransform lockRect = lockIconImage.rectTransform;

                switch (state)
                {
                    case StageNodeVisualState.Current:
                        bodyRect.sizeDelta = new Vector2(190f, 128f);
                        bodyRect.anchoredPosition = Vector2.zero;
                        glowRect.sizeDelta = new Vector2(224f, 170f);
                        lockRect.sizeDelta = new Vector2(58f, 58f);
                        break;
                    case StageNodeVisualState.Cleared:
                        bodyRect.sizeDelta = new Vector2(150f, 96f);
                        bodyRect.anchoredPosition = new Vector2(0f, 16f);
                        glowRect.sizeDelta = new Vector2(188f, 142f);
                        lockRect.sizeDelta = new Vector2(54f, 54f);
                        break;
                    case StageNodeVisualState.Locked:
                    case StageNodeVisualState.PreviewLocked:
                        bodyRect.sizeDelta = new Vector2(148f, 92f);
                        bodyRect.anchoredPosition = Vector2.zero;
                        glowRect.sizeDelta = new Vector2(176f, 126f);
                        lockRect.sizeDelta = new Vector2(54f, 54f);
                        break;
                    default:
                        bodyRect.sizeDelta = new Vector2(148f, 96f);
                        bodyRect.anchoredPosition = Vector2.zero;
                        glowRect.sizeDelta = new Vector2(176f, 126f);
                        lockRect.sizeDelta = new Vector2(54f, 54f);
                        break;
                }
            }

            private void ConfigureStarRowRect(int stars)
            {
                RectTransform starRect = starRowImage.rectTransform;
                starRect.anchoredPosition = new Vector2(0f, -48f);
                switch (Mathf.Clamp(stars, 1, 3))
                {
                    case 1:
                        starRect.sizeDelta = new Vector2(72f, 68f);
                        break;
                    case 2:
                        starRect.sizeDelta = new Vector2(132f, 72f);
                        break;
                    default:
                        starRect.sizeDelta = new Vector2(168f, 72f);
                        break;
                }
            }

            private void ConfigureNumber(StageNodeVisualState state)
            {
                RectTransform numberRect = numberText.rectTransform;
                numberRect.anchorMin = Vector2.zero;
                numberRect.anchorMax = Vector2.one;
                numberRect.pivot = new Vector2(0.5f, 0.5f);
                numberRect.anchoredPosition = Vector2.zero;
                numberRect.sizeDelta = Vector2.zero;

                switch (state)
                {
                    case StageNodeVisualState.Cleared:
                        numberRect.offsetMin = new Vector2(0f, 28f + NumberVerticalLift);
                        numberRect.offsetMax = new Vector2(0f, 8f + NumberVerticalLift);
                        break;
                    case StageNodeVisualState.Current:
                        numberRect.offsetMin = new Vector2(0f, 8f + NumberVerticalLift);
                        numberRect.offsetMax = new Vector2(0f, NumberVerticalLift);
                        break;
                    default:
                        numberRect.offsetMin = new Vector2(0f, NumberVerticalLift);
                        numberRect.offsetMax = new Vector2(0f, NumberVerticalLift);
                        break;
                }
            }

            private static Image CreateLayer(RectTransform parent, string name, Vector2 position, Vector2 size)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(parent, false);

                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;

                Image image = obj.GetComponent<Image>();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return image;
            }

            private static bool ApplyOptionalSprite(Image image, string spriteName)
            {
                Sprite sprite = LoadStageSprite(spriteName);
                if (sprite == null)
                {
                    return false;
                }

                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
                return true;
            }
        }

        private static void WarnMissingSprite(string spriteName, string message)
        {
            if (MissingStageSpriteWarnings.Add(spriteName))
            {
                Debug.LogWarning($"[StageNodeView] Missing sprite '{spriteName}'. {message}");
            }
        }

        private void StartStage(string stageId)
        {
            if (walletStore.Hearts <= 0)
            {
                ShowHeartPopup();
                return;
            }

            GameLaunchContext.SetStagePlay(stageId);
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.StageGameplayBgmReason, true);
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.StageAdvanceBgmReason, false);
            SceneLoader.LoadGame();
        }

        private RectTransform CreateModeCard(
            RectTransform parent,
            string name,
            string title,
            string body,
            string badge,
            string iconKey,
            Color color,
            Color shine,
            Vector2 position,
            bool enabled,
            Action action)
        {
            GameObject cardObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            cardObject.transform.SetParent(parent, false);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = position;
            card.sizeDelta = new Vector2(960f, 240f);
            Image cardImage = cardObject.GetComponent<Image>();
            bool tutorialCard = title.StartsWith("TUTORIAL", StringComparison.Ordinal);
            string cardSpriteName = tutorialCard ? "mode_card_normal" : title.StartsWith("NORMAL", StringComparison.Ordinal) ? "mode_card_normal" : title.StartsWith("HARD", StringComparison.Ordinal) ? "mode_card_hard" : "mode_card_infinity";
            bool usesCardArt = ApplyStageSprite(cardImage, cardSpriteName, Image.Type.Simple);
            if (usesCardArt)
            {
                cardImage.color = tutorialCard ? color : Color.white;
                cardImage.preserveAspect = false;
            }
            if (!usesCardArt)
            {
                CasualUIStyle.ApplyPanel(cardImage, color, 34);
                AddOutline(cardObject, enabled ? new Color(0.94f, 1f, 0.62f, 0.86f) : new Color(0.78f, 0.82f, 0.98f, 0.42f), 4f);
                AddShadow(cardObject, 0.62f, new Vector2(0f, -12f));
            }

            Button button = cardObject.GetComponent<Button>();
            button.transition = usesCardArt ? Selectable.Transition.None : button.transition;
            button.onClick.AddListener(() => action?.Invoke());

            if (!usesCardArt)
            {
                RectTransform sheen = CreatePanel(card, "CardSheen", shine, 28, new Vector2(0f, 0.58f), new Vector2(1f, 0.98f), Vector2.zero, Vector2.zero);
                sheen.GetComponent<Image>().raycastTarget = false;
                sheen.SetAsFirstSibling();

                RectTransform icon = CreatePanel(card, "ModeIcon", new Color(1f, 1f, 1f, enabled ? 0.12f : 0.08f), 30, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(235f, 200f));
                icon.pivot = new Vector2(0f, 0.5f);
                CasualIconFactory.Create(icon, iconKey == "target" ? "stages" : iconKey == "infinity" ? "retry" : iconKey, enabled ? Color.white : new Color(0.72f, 0.74f, 0.86f, 1f));
            }

            Text titleText = RuntimeUiFactory.CreateText(card, "Title", title, 54, TextAnchor.UpperLeft);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(350f, -48f);
            titleText.rectTransform.sizeDelta = new Vector2(-470f, 76f);
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = enabled ? Color.white : new Color(0.72f, 0.74f, 0.84f, 1f);
            CasualUIStyle.ApplyTextDepth(titleText, true);

            Text bodyText = RuntimeUiFactory.CreateText(card, "Body", body, 30, TextAnchor.UpperLeft);
            bodyText.rectTransform.anchorMin = new Vector2(0f, 1f);
            bodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            bodyText.rectTransform.pivot = new Vector2(0f, 1f);
            bodyText.rectTransform.anchoredPosition = new Vector2(354f, -118f);
            bodyText.rectTransform.sizeDelta = new Vector2(-500f, 54f);
            bodyText.fontStyle = FontStyle.Bold;
            bodyText.color = enabled ? new Color(1f, 1f, 1f, 0.94f) : new Color(0.72f, 0.76f, 0.88f, 1f);
            CasualUIStyle.ApplyTextDepth(bodyText, false);

            RectTransform badgePanel = CreatePanel(card, "RangeBadge", new Color(0.02f, 0.08f, 0.08f, 0.38f), 28, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(350f, 50f), new Vector2(340f, 58f));
            Text badgeText = RuntimeUiFactory.CreateText(badgePanel, "Label", badge, 29, TextAnchor.MiddleCenter);
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.color = enabled ? new Color(1f, 0.95f, 0.68f, 1f) : new Color(0.78f, 0.80f, 0.88f, 1f);
            CasualUIStyle.ApplyTextDepth(badgeText, true);

            if (!usesCardArt)
            {
                Text arrow = RuntimeUiFactory.CreateText(card, "Arrow", enabled ? ">" : "LOCK", enabled ? 76 : 25, TextAnchor.MiddleCenter);
                arrow.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                arrow.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
                arrow.rectTransform.anchoredPosition = new Vector2(-38f, 0f);
                arrow.rectTransform.sizeDelta = new Vector2(110f, 106f);
                arrow.fontStyle = FontStyle.Bold;
                arrow.color = enabled ? Color.white : new Color(0.82f, 0.84f, 0.9f, 1f);
                CasualUIStyle.ApplyTextDepth(arrow, true);
            }

            return card;
        }

        private static void AddMobileTitleAligner(
            GameObject host,
            string screenName,
            RectTransform titleRoot,
            params RectTransform[] followRoots)
        {
            if (host == null || titleRoot == null)
            {
                return;
            }

            MobileTitleSectionAligner aligner = host.GetComponent<MobileTitleSectionAligner>()
                ?? host.AddComponent<MobileTitleSectionAligner>();
            aligner.Configure(screenName, titleRoot, followRoots);
        }

        private Text CreateTitle(RectTransform parent, string title, string subtitle, float y, float titleHeight)
        {
            RectTransform group = CreateEmpty(parent, "TitleGroup", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(920f, 150f));
            group.pivot = new Vector2(0.5f, 1f);
            group.anchoredPosition = new Vector2(0f, y);
            Text titleText = RuntimeUiFactory.CreateText(group, "Title", title, 78, TextAnchor.UpperCenter);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchoredPosition = Vector2.zero;
            titleText.rectTransform.sizeDelta = new Vector2(0f, titleHeight);
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = title == "Stages" || title.EndsWith("Mode", StringComparison.Ordinal)
                ? Gold
                : Color.white;
            CasualUIStyle.ApplyTextDepth(titleText, true);

            Text subtitleText = RuntimeUiFactory.CreateText(group, "Subtitle", subtitle, 34, TextAnchor.UpperCenter);
            subtitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            subtitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitleText.rectTransform.anchoredPosition = new Vector2(0f, -titleHeight + 4f);
            subtitleText.rectTransform.sizeDelta = new Vector2(0f, 50f);
            subtitleText.fontStyle = FontStyle.Bold;
            subtitleText.color = new Color(0.88f, 0.86f, 1f, 1f);
            CasualUIStyle.ApplyTextDepth(subtitleText, false);
            return titleText;
        }

        private RectTransform CreateScreenRoot(Transform parent, string name)
        {
            GameObject screen = new GameObject(name, typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            RectTransform rect = screen.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void CreateStageBackdrop(Transform parent)
        {
            GameObject background = new GameObject("StageBackdrop", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = background.GetComponent<Image>();
            bool usesArt = ApplyStageSprite(image, "stage_mode_background", Image.Type.Simple);
            if (!usesArt)
            {
                image.sprite = CreateVerticalGradientSprite("StageBackdropGradient", ScreenBottom, ScreenTop);
            }
            image.color = Color.white;
            image.raycastTarget = false;
            if (!usesArt)
            {
                AddDecorStars(rect);
            }
            background.transform.SetAsFirstSibling();
        }

        private static RectTransform CreateImage(
            Transform parent,
            string name,
            string spriteName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            Sprite sprite = LoadStageSprite(spriteName);
            if (sprite == null)
            {
                return null;
            }

            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
            return rect;
        }

        private static bool ApplyStageSprite(Image image, string spriteName, Image.Type type)
        {
            Sprite sprite = LoadStageSprite(spriteName);
            if (sprite == null)
            {
                return false;
            }

            image.sprite = sprite;
            image.type = type;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = image.raycastTarget;
            return true;
        }

        private static Sprite LoadStageSprite(string spriteName)
        {
            if (StageSpriteCache.TryGetValue(spriteName, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(StageArtPath + spriteName);
            if (texture == null)
            {
                StageSpriteCache[spriteName] = null;
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = spriteName;
            StageSpriteCache[spriteName] = sprite;
            return sprite;
        }

        private static void UseButtonSprite(Button button, string spriteName)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            if (ApplyStageSprite(image, spriteName, Image.Type.Simple))
            {
                Outline outline = button.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }

        private static void UseFullArtButtonSprite(Button button, string spriteName)
        {
            UseButtonSprite(button, spriteName);
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                image.preserveAspect = false;
                image.color = Color.white;
            }

            button.transition = Selectable.Transition.None;
            foreach (Graphic childGraphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (childGraphic != image)
                {
                    childGraphic.gameObject.SetActive(false);
                }
            }
        }

        private static void UseStageButtonArt(Button button, string spriteName)
        {
            if (button == null)
            {
                return;
            }

            Sprite sprite = CasualIconFactory.LoadUiSprite($"UI/Stage/Buttons/{spriteName}");
            if (sprite == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }

            Shadow shadow = button.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.enabled = false;
            }

            button.transition = Selectable.Transition.None;
            foreach (Graphic childGraphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (childGraphic != image)
                {
                    childGraphic.gameObject.SetActive(false);
                }
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.48f, 0.48f, 0.52f, 0.62f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private void AddMapLandscape(RectTransform scene)
        {
            Image sceneImage = scene.GetComponent<Image>();
            sceneImage.sprite = CreateVerticalGradientSprite("StageMapGradient", new Color(0.13f, 0.45f, 0.24f, 0.92f), new Color(0.42f, 0.68f, 1f, 0.95f));
            sceneImage.type = Image.Type.Simple;

            CreatePanel(scene, "FarHills", new Color(0.46f, 0.78f, 0.42f, 0.55f), 60, new Vector2(0f, 0.44f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero);
            CreatePanel(scene, "Waterfall", new Color(0.23f, 0.68f, 1f, 0.50f), 28, new Vector2(0.73f, 0.20f), new Vector2(0.98f, 0.58f), Vector2.zero, Vector2.zero);
            CreatePanel(scene, "Ground", new Color(0.18f, 0.60f, 0.24f, 0.74f), 50, new Vector2(0f, 0f), new Vector2(1f, 0.47f), Vector2.zero, Vector2.zero);
            CreatePanel(scene, "LeftStatue", new Color(0.42f, 0.62f, 0.88f, 0.82f), 24, new Vector2(0.03f, 0.37f), new Vector2(0.25f, 0.56f), Vector2.zero, Vector2.zero);
            CreatePanel(scene, "RightCubeHouse", new Color(0.50f, 0.18f, 0.86f, 0.78f), 26, new Vector2(0.78f, 0.06f), new Vector2(0.96f, 0.25f), Vector2.zero, Vector2.zero);
        }

        private RectTransform CreateChip(RectTransform parent, string name, Vector2 position, Vector2 size, out Text label)
        {
            RectTransform chip = CreatePanel(parent, name, GetBlockRewardCardColor(0, BlockRewardVisualState.InProgress), 30, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Image image = chip.GetComponent<Image>();
            image.preserveAspect = false;
            AddOutline(chip.gameObject, GetBlockRewardOutlineColor(0, BlockRewardVisualState.InProgress), 3f);
            AddShadow(chip.gameObject, 0.48f, new Vector2(0f, -5f));

            label = RuntimeUiFactory.CreateText(chip, "Label", string.Empty, 26, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(label, true);
            return chip;
        }

        private void RefreshBlockChipVisual()
        {
            if (mapBlockChipImage == null)
            {
                return;
            }

            int blockIndex = Mathf.Max(0, selectedBlockIndex);
            CasualUIStyle.ApplyPanel(mapBlockChipImage, GetBlockRewardCardColor(blockIndex, BlockRewardVisualState.InProgress), 30);
            Outline outline = mapBlockChipImage.GetComponent<Outline>() ?? mapBlockChipImage.gameObject.AddComponent<Outline>();
            outline.effectColor = GetBlockRewardOutlineColor(blockIndex, BlockRewardVisualState.InProgress);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        private RectTransform CreatePanel(
            Transform parent,
            string name,
            Color color,
            int radius,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, radius);
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateEmpty(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private void CreateSmallIcon(RectTransform parent, string key, Vector2 position, Vector2 size, Color color)
        {
            RectTransform icon = CreateEmpty(parent, $"{key}Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, size);
            CasualIconFactory.Create(icon, key, color);
        }

        private static Image CreateStageSpriteIcon(RectTransform parent, string name, string spriteName, Vector2 position, Vector2 size)
        {
            Image image = CreateSpriteImage(parent, name, position, size);
            ApplyStageSprite(image, spriteName, Image.Type.Simple);
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        private static Image CreateMainMenuSpriteIcon(RectTransform parent, string name, string relativePath, Vector2 position, Vector2 size)
        {
            Image image = CreateSpriteImage(parent, name, position, size);
            image.sprite = CasualIconFactory.LoadMainMenuKitSprite(relativePath);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        private static Image CreateSpriteImage(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void AddPathSegment(RectTransform parent, Vector2 a, Vector2 b)
        {
            Vector2 midpoint = (a + b) * 0.5f;
            float length = Vector2.Distance(a, b);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            RectTransform segment = CreatePanel(parent, "PathSegment", new Color(0.96f, 0.70f, 0.40f, 0.78f), 18, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), midpoint, new Vector2(length, 58f));
            segment.pivot = new Vector2(0.5f, 0.5f);
            segment.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void AddDecorStars(RectTransform parent)
        {
            Vector2[] positions =
            {
                new Vector2(0.11f, 0.71f),
                new Vector2(0.84f, 0.78f),
                new Vector2(0.25f, 0.62f),
                new Vector2(0.91f, 0.38f),
                new Vector2(0.17f, 0.22f),
                new Vector2(0.71f, 0.16f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject star = new GameObject($"DecorStar{i}", typeof(RectTransform), typeof(Image));
                star.transform.SetParent(parent, false);
                RectTransform rect = star.GetComponent<RectTransform>();
                rect.anchorMin = positions[i];
                rect.anchorMax = positions[i];
                rect.sizeDelta = i % 2 == 0 ? new Vector2(20f, 20f) : new Vector2(13f, 13f);
                rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                CasualUIStyle.ApplyPanel(star.GetComponent<Image>(), i % 2 == 0 ? Gold : new Color(0.40f, 0.76f, 1f, 1f), 4);
                star.GetComponent<Image>().raycastTarget = false;
            }
        }

        private static void AddOutline(GameObject obj, Color color, float distance)
        {
            Outline outline = obj.GetComponent<Outline>() ?? obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private static void AddShadow(GameObject obj, float alpha, Vector2 distance)
        {
            Shadow shadow = obj.GetComponent<Shadow>() ?? obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = distance;
        }

        private bool IsInfinityUnlocked()
        {
            return HasClearedAll(StageType.SolveStage);
        }

        private bool HasClearedAll(StageType type)
        {
            List<StageData> stages = currentStages.Where(stage => stage != null && stage.stageType == type).ToList();
            return stages.Count > 0 && stages.All(stage => progressStore.GetProgress(stage.stageId).isCleared);
        }

        private int GetHighestClearedLocalStage(IReadOnlyList<StageData> modeStages)
        {
            int highest = 0;
            foreach (StageData stage in modeStages)
            {
                if (stage != null && progressStore.GetProgress(stage.stageId).isCleared)
                {
                    highest = Mathf.Max(highest, GetLocalStageNumber(stage));
                }
            }

            return highest;
        }

        private void RefreshCurrentStageMarker(Vector2? currentPosition)
        {
            if (currentStageMarker == null || currentStageMarkerImage == null || !currentStageMarkerImage.enabled)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[StageMapMarker] markerSet=False reason=missing-marker");
#endif
                return;
            }

            if (!currentPosition.HasValue)
            {
                currentStageMarker.gameObject.SetActive(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[StageMapMarker] markerSet=False reason=no-current-node");
#endif
                return;
            }

            currentStageMarker.sizeDelta = GetMarkerSize();
            currentStageMarker.anchoredPosition = currentPosition.Value;
            currentStageMarker.gameObject.SetActive(true);
            currentStageMarker.SetAsLastSibling();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageMapMarker] markerConnected=True markerSprite=stage_cube_marker markerSize={currentStageMarker.sizeDelta} markerOffset={GetMarkerOffset()} toPos={currentPosition.Value} mode=set");
#endif
        }

        public void PlayAdvanceToNextStageAnimation(int fromStage, int toStage, Action onComplete = null, bool autoStart = false)
        {
            if (animationRunner == null)
            {
                return;
            }

            animationRunner.Play(PlayAdvanceToNextStageRoutine(fromStage, toStage, onComplete, autoStart));
        }

        private IEnumerator PlayAdvanceToNextStageRoutine(int fromStage, int toStage, Action onComplete, bool autoStart, bool refreshToCurrentProgress = true)
        {
            if (currentStageMarker == null || currentStageMarkerImage == null || !currentStageMarkerImage.enabled)
            {
                if (refreshToCurrentProgress)
                {
                    RefreshFixedWindowAroundCurrentStage();
                }
                else
                {
                    RefreshStageListForAdvanceTarget(toStage);
                }

                onComplete?.Invoke();
                if (autoStart || AutoStartNextStageAfterAdvance)
                {
                    StartCurrentStageIfVisible(toStage);
                }

                yield break;
            }

            Vector2 markerOffset = GetMarkerOffset();
            Vector2 fromPosition = visibleStagePositions.TryGetValue(fromStage, out Vector2 fromNode)
                ? fromNode + markerOffset
                : currentStageMarker.anchoredPosition;
            bool hasToNode = visibleStagePositions.TryGetValue(toStage, out Vector2 toNode);
            int fromBlock = ((Mathf.Max(1, fromStage) - 1) / StagesPerBlock) + 1;
            int toBlock = ((Mathf.Max(1, toStage) - 1) / StagesPerBlock) + 1;
            int fromBlockStageIndex = fromStage - (((fromBlock - 1) * StagesPerBlock) + 1) + 1;
            int toBlockStageIndex = toStage - (((toBlock - 1) * StagesPerBlock) + 1) + 1;
            bool directMove = fromBlock == toBlock
                && fromBlockStageIndex <= 2
                && toBlockStageIndex <= 3
                && hasToNode;
            Vector2 toPosition = directMove
                ? toNode + markerOffset
                : fromPosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageMapAdvance] fromStage={fromStage} toStage={toStage} currentStage={toStage} highestClearedStage={Mathf.Max(0, toStage - 1)}");
            Debug.Log($"[StageMapMarkerMove] mode={(directMove ? "move" : "hopInPlace")} fromPos={fromPosition} toPos={toPosition}");
#endif
            yield return MoveMarkerRoutine(fromPosition, toPosition, !directMove, null);
            onComplete?.Invoke();
            if (refreshToCurrentProgress)
            {
                RefreshFixedWindowAroundCurrentStage();
            }
            OnAdvanceAnimationFinished();
            if (autoStart || AutoStartNextStageAfterAdvance)
            {
                StartCurrentStageIfVisible(toStage);
            }
        }

        private void RefreshStageListForAdvanceTarget(int toStage)
        {
            IReadOnlyList<StageData> modeStages = currentStages
                .Where(stage => stage != null && stage.stageType == selectedType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            int total = modeStages.Count;
            int safeToStage = Mathf.Clamp(toStage, 1, Mathf.Max(1, total));
            selectedBlockIndex = Mathf.Clamp((safeToStage - 1) / StagesPerBlock, 0, Mathf.Max(0, (total - 1) / StagesPerBlock));
            mapBlockChipText.text = $"Block {selectedBlockIndex + 1}";
            RefreshBlockChipVisual();
            BuildMapNodes(modeStages, safeToStage, Mathf.Max(0, safeToStage - 1));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageNextTransition] RefreshToStage visibleBlock={selectedBlockIndex + 1} toStage={safeToStage}");
#endif
        }

        private void PlayAdvanceToNextStageSequence(int fromStage, int toStage, bool autoStart)
        {
            if (animationRunner == null)
            {
                if (autoStart)
                {
                    StartCurrentStageIfVisible(toStage);
                }

                return;
            }

            animationRunner.Play(PlayNextStageTransitionSequence(fromStage, toStage, autoStart));
        }

        private IEnumerator PlayNextStageTransitionSequence(int fromStage, int toStage, bool autoStart)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageNextTransition] ShowStageList fromStage={fromStage} toStage={toStage}");
#endif
            yield return new WaitForSecondsRealtime(GetWaitBeforeMarkerHop());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageNextTransition] MarkerHopStart fromSlot={GetVisibleSlotIndexForStage(fromStage, fromStage)} toSlot={GetVisibleSlotIndexForStage(fromStage, toStage)}");
#endif
            yield return PlayAdvanceToNextStageRoutine(fromStage, toStage, null, false, false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[StageNextTransition] MarkerHopComplete");
            Debug.Log($"[StageNextTransition] RefreshToStage toStage={toStage}");
#endif
            RefreshStageListForAdvanceTarget(toStage);
            yield return new WaitForSecondsRealtime(GetWaitAfterMarkerHop());
            yield return FadeTransitionOverlay(0f, 1f, GetScreenFadeDuration());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[StageNextTransition] LoadNextStage");
#endif
            if (autoStart)
            {
                GameLaunchContext.RequestGameScreenFadeInOnNextLoad();
                StartCurrentStageIfVisible(toStage);
            }
        }

        private void RefreshFixedWindowAroundCurrentStage()
        {
            RefreshMap();
        }

        private void OnAdvanceAnimationFinished()
        {
        }

        private IEnumerator MoveMarkerRoutine(Vector2 fromPosition, Vector2 toPosition, bool hopInPlace, Action onComplete)
        {
            if (currentStageMarker == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float duration = GetMarkerHopDuration();
            float hopHeight = GetMarkerHopHeight(hopInPlace);
            currentStageMarker.gameObject.SetActive(true);
            currentStageMarker.SetAsLastSibling();
            currentStageMarker.sizeDelta = GetMarkerSize();
            currentStageMarker.anchoredPosition = fromPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector2 basePosition = Vector2.Lerp(fromPosition, toPosition, eased);
                float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
                currentStageMarker.anchoredPosition = basePosition + new Vector2(0f, arc);
                yield return null;
            }

            currentStageMarker.anchoredPosition = toPosition;
            onComplete?.Invoke();
        }

        private void StartCurrentStageIfVisible(int localStageNumber)
        {
            StageData stage = currentStages
                .Where(item => item != null && item.stageType == selectedType)
                .FirstOrDefault(item => GetLocalStageNumber(item) == localStageNumber);
            if (stage != null)
            {
                StartStage(stage.stageId);
            }
        }

        private static bool IsBlockRewardStage(int stageNumber)
        {
            return stageNumber > 0 && stageNumber % StagesPerBlock == 0;
        }

        private IEnumerable<StageData> GetBlockStages(int block)
        {
            int localStart = (block * StagesPerBlock) + 1;
            return currentStages.Where(stage =>
                stage != null
                && stage.stageType == selectedType
                && GetLocalStageNumber(stage) >= localStart
                && GetLocalStageNumber(stage) < localStart + StagesPerBlock);
        }

        private int GetBlockStars(int block)
        {
            return GetBlockStages(block).Sum(stage => progressStore.GetProgress(stage.stageId).stars);
        }

        private bool IsBlockUnlockedForRewards(int blockIndex)
        {
            IReadOnlyList<StageData> modeStages = currentStages
                .Where(stage => stage != null && stage.stageType == selectedType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            if (modeStages.Count == 0)
            {
                return blockIndex == 0;
            }

            int highestClearedLocal = GetHighestClearedLocalStage(modeStages);
            int currentLocal = Mathf.Clamp(highestClearedLocal + 1, 1, modeStages.Count);
            int currentBlockIndex = Mathf.Clamp((currentLocal - 1) / StagesPerBlock, 0, Mathf.Max(0, (modeStages.Count - 1) / StagesPerBlock));
            return blockIndex <= currentBlockIndex;
        }

        private int GetBlockCountForSelectedMode()
        {
            int stageCount = currentStages.Count(stage => stage != null && stage.stageType == selectedType);
            return Mathf.Max(1, Mathf.CeilToInt(stageCount / (float)StagesPerBlock));
        }

        private int GetMaxBlockIndexForSelectedMode()
        {
            return Mathf.Max(0, GetBlockCountForSelectedMode() - 1);
        }

        private static Color GetBlockRewardCardColor(int blockIndex, BlockRewardVisualState state)
        {
            Color color;
            switch (blockIndex % 5)
            {
                case 0:
                    color = new Color(0.045f, 0.255f, 0.68f, 0.98f);
                    break;
                case 1:
                    color = new Color(0.10f, 0.47f, 0.055f, 0.98f);
                    break;
                case 2:
                    color = new Color(0.015f, 0.50f, 0.50f, 0.98f);
                    break;
                case 3:
                    color = new Color(0.39f, 0.095f, 0.66f, 0.98f);
                    break;
                default:
                    color = new Color(0.55f, 0.30f, 0.025f, 0.98f);
                    break;
            }

            if (state == BlockRewardVisualState.Locked)
            {
                color = Color.Lerp(color, new Color(0.025f, 0.028f, 0.040f, 1f), 0.48f);
                color.a = 0.82f;
            }

            return color;
        }

        private static Color GetBlockRewardOutlineColor(int blockIndex, BlockRewardVisualState state)
        {
            if (state == BlockRewardVisualState.Locked)
            {
                return new Color(0.44f, 0.49f, 0.63f, 0.64f);
            }

            switch (blockIndex % 5)
            {
                case 0: return new Color(0.33f, 0.67f, 1f, 0.90f);
                case 1: return new Color(0.56f, 1f, 0.32f, 0.90f);
                case 2: return new Color(0.20f, 0.95f, 0.92f, 0.90f);
                case 3: return new Color(0.82f, 0.36f, 1f, 0.90f);
                default: return new Color(1f, 0.70f, 0.16f, 0.92f);
            }
        }

        private static Color GetBlockRewardFillColor(int blockIndex)
        {
            switch (blockIndex % 5)
            {
                case 0: return new Color(0.24f, 0.58f, 1f, 1f);
                case 1: return new Color(0.46f, 0.95f, 0.20f, 1f);
                case 2: return new Color(0.12f, 0.92f, 0.92f, 1f);
                case 3: return new Color(0.70f, 0.20f, 1f, 1f);
                default: return new Color(1f, 0.68f, 0.08f, 1f);
            }
        }

        private static int GetLocalStageNumber(StageData stage)
        {
            if (stage == null)
            {
                return 0;
            }

            string id = stage.stageId ?? string.Empty;
            int separator = id.LastIndexOf('_');
            if (separator >= 0
                && separator < id.Length - 1
                && int.TryParse(id.Substring(separator + 1), out int parsed)
                && parsed > 0)
            {
                return parsed;
            }

            if (stage.stageType == StageType.ReverseTargetStage)
            {
                return stage.stageNumber - StagePackGenerator.NormalStageCount;
            }

            if (stage.stageType == StageType.InfinityStage)
            {
                return stage.stageNumber - (StagePackGenerator.NormalStageCount + StagePackGenerator.HardStageCount);
            }

            if (stage.stageType == StageType.TutorialStage)
            {
                return stage.stageNumber - StagePackGenerator.TutorialFirstStageNumber + 1;
            }

            return stage.stageNumber;
        }

        private void SetMapTitleForSelectedMode()
        {
            switch (selectedType)
            {
                case StageType.TutorialStage:
                    mapTitle.text = "Tutorial Mode";
                    mapSubtitle.text = "Clear beginner stages and earn stars.";
                    break;
                case StageType.ReverseTargetStage:
                    mapTitle.text = "Hard Mode";
                    mapSubtitle.text = "Match target stages and earn stars.";
                    break;
                case StageType.InfinityStage:
                    mapTitle.text = "Infinity Mode";
                    mapSubtitle.text = "Mixed stages with endless-style progression.";
                    break;
                default:
                    mapTitle.text = "Normal Mode";
                    mapSubtitle.text = "Clear stages and earn stars.";
                    break;
            }
        }

        private sealed class BlockRewardViewModel
        {
            public int blockIndex;
            public int blockNumber;
            public int startStage;
            public int endStage;
            public int totalStars;
            public int requiredStars;
            public int rewardGems;
            public bool unlocked;
            public bool rewardClaimed;
            public BlockRewardVisualState state;
        }

        private void BuildBlockSelectorPopup(Transform parent)
        {
            blockSelectorPopup = new GameObject("StageBlockSelectorPopup", typeof(RectTransform), typeof(Image));
            blockSelectorPopup.transform.SetParent(parent, false);
            RectTransform panel = blockSelectorPopup.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(760f, 900f);
            CasualUIStyle.ApplyPanel(blockSelectorPopup.GetComponent<Image>(), new Color(0.028f, 0.044f, 0.11f, 0.99f), 30);
            AddOutline(blockSelectorPopup, Gold, 3f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Select Block", 42, TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(40f, 802f);
            title.rectTransform.offsetMax = new Vector2(-40f, -28f);
            title.fontStyle = FontStyle.Bold;
            title.color = Cream;
            CasualUIStyle.ApplyTextDepth(title, true);

            Text subtitle = RuntimeUiFactory.CreateText(panel, "Subtitle", "Choose a block to inspect.", 24, TextAnchor.UpperCenter);
            subtitle.rectTransform.offsetMin = new Vector2(42f, 754f);
            subtitle.rectTransform.offsetMax = new Vector2(-42f, -92f);
            subtitle.color = new Color(0.82f, 0.86f, 1f, 1f);

            RectTransform viewport = CreatePanel(
                panel,
                "BlockListViewport",
                new Color(0f, 0f, 0f, 0.01f),
                16,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -430f),
                new Vector2(680f, 560f));
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.raycastTarget = true;

            blockSelectorContent = CreateEmpty(viewport, "BlockList", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(640f, 560f));
            blockSelectorContent.pivot = new Vector2(0.5f, 1f);

            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = blockSelectorContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 38f;

            Button close = CasualUIFactory.CreateActionButton(panel, "CloseButton", "Close", string.Empty, CasualUIColor.Blue, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(260f, 72f));
            close.onClick.AddListener(() => blockSelectorPopup.SetActive(false));
            blockSelectorPopup.SetActive(false);
        }

        private void ShowBlockSelectorPopup()
        {
            if (blockSelectorPopup == null || blockSelectorContent == null)
            {
                return;
            }

            PopulateBlockSelectorPopup();
            blockSelectorPopup.SetActive(true);
            blockSelectorPopup.transform.SetAsLastSibling();
        }

        private void PopulateBlockSelectorPopup()
        {
            for (int i = blockSelectorContent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(blockSelectorContent.GetChild(i).gameObject);
            }

            int blockCount = GetBlockCountForSelectedMode();
            int rows = Mathf.CeilToInt(blockCount / 2f);
            float rowStride = 116f;
            float contentHeight = Mathf.Max(560f, 96f + rows * rowStride);
            blockSelectorContent.sizeDelta = new Vector2(640f, contentHeight);
            blockSelectorContent.anchoredPosition = Vector2.zero;
            for (int i = 0; i < blockCount; i++)
            {
                int blockIndex = i;
                int row = i / 2;
                int col = i % 2;
                Vector2 position = new Vector2(col == 0 ? -170f : 170f, -56f - (row * rowStride));
                Button button = CasualUIFactory.CreateActionButton(
                    blockSelectorContent,
                    $"Block{blockIndex + 1}Button",
                    $"Block {blockIndex + 1}",
                    string.Empty,
                    blockIndex == selectedBlockIndex ? CasualUIColor.Orange : CasualUIColor.Blue,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    position,
                    new Vector2(285f, 66f));
                button.onClick.AddListener(() => OnBlockSelected(blockIndex));
            }
        }

        private void OnBlockSelected(int blockIndex)
        {
            selectedBlockIndex = blockIndex;
            displayMode = StageMapDisplayMode.BlockBrowse;
            blockBrowseScrollOffset = 0;
            if (blockSelectorPopup != null)
            {
                blockSelectorPopup.SetActive(false);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            IReadOnlyList<StageData> modeStages = currentStages
                .Where(stage => stage != null && stage.stageType == selectedType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            int blockStartStage = (selectedBlockIndex * StagesPerBlock) + 1;
            int blockEndStage = Mathf.Min(blockStartStage + StagesPerBlock - 1, modeStages.Count);
            Debug.Log($"[StageBlockSelect] selectedBlock={selectedBlockIndex + 1} blockStartStage={blockStartStage} blockEndStage={blockEndStage}");
#endif
            RefreshMap();
        }

        private void BuildBlockRewardsPopup(Transform parent)
        {
            blockRewardsPopup = new GameObject("BlockRewardsPopup", typeof(RectTransform), typeof(Image));
            blockRewardsPopup.transform.SetParent(parent, false);
            RectTransform panel = blockRewardsPopup.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -30f);
            panel.sizeDelta = new Vector2(1040f, 1300f);
            CasualUIStyle.ApplyPanel(blockRewardsPopup.GetComponent<Image>(), new Color(0.035f, 0.036f, 0.046f, 0.98f), 38);
            AddOutline(blockRewardsPopup, ShopCardOutline, 2f);
            AddShadow(blockRewardsPopup, 0.55f, new Vector2(0f, -12f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Block Rewards", 68, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -44f);
            title.rectTransform.sizeDelta = new Vector2(-190f, 98f);
            title.fontStyle = FontStyle.Bold;
            title.color = Cream;
            CasualUIStyle.ApplyTextDepth(title, true);

            Button close = CasualUIFactory.CreateActionButton(
                panel,
                "CloseButton",
                "X",
                string.Empty,
                CasualUIColor.Pink,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-64f, -52f),
                new Vector2(82f, 82f));
            close.onClick.AddListener(() => blockRewardsPopup.SetActive(false));

            RectTransform viewport = CreatePanel(
                panel,
                "RewardsViewport",
                new Color(0.018f, 0.022f, 0.035f, 0.96f),
                24,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -686f),
                new Vector2(970f, 1060f));
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0.018f, 0.022f, 0.035f, 0.96f);
            viewportImage.raycastTarget = true;

            blockRewardsContent = CreateEmpty(viewport, "Content", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(950f, 2660f));
            blockRewardsContent.pivot = new Vector2(0.5f, 1f);

            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = blockRewardsContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 46f;

            Text footer = RuntimeUiFactory.CreateText(panel, "Footer", "Complete stages to earn stars and get amazing rewards!", 28, TextAnchor.MiddleCenter);
            footer.rectTransform.anchorMin = new Vector2(0f, 0f);
            footer.rectTransform.anchorMax = new Vector2(1f, 0f);
            footer.rectTransform.pivot = new Vector2(0.5f, 0f);
            footer.rectTransform.anchoredPosition = new Vector2(0f, 26f);
            footer.rectTransform.sizeDelta = new Vector2(-80f, 48f);
            footer.color = Color.white;
            CasualUIStyle.ApplyTextDepth(footer, false);

            blockRewardsPopup.SetActive(false);
        }

        private void ShowBlockRewardsPopup()
        {
            if (blockRewardsPopup == null || blockRewardsContent == null)
            {
                return;
            }

            RefreshBlockRewardsPopup();
            blockRewardsPopup.SetActive(true);
            blockRewardsPopup.transform.SetAsLastSibling();
        }

        private void RefreshBlockRewardsPopup()
        {
            for (int i = blockRewardsContent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(blockRewardsContent.GetChild(i).gameObject);
            }

            milestoneStore.EnsureMilestones(currentStages);
            int blockCount = GetBlockCountForSelectedMode();
            for (int block = 0; block < blockCount; block++)
            {
                CreateBlockRewardCard(blockRewardsContent, BuildBlockRewardViewModel(block), new Vector2(0f, -118f - (block * BlockRewardCardStride)));
            }

            blockRewardsContent.sizeDelta = new Vector2(950f, Mathf.Max(1060f, 118f + blockCount * BlockRewardCardStride));
        }

        private BlockRewardViewModel BuildBlockRewardViewModel(int blockIndex)
        {
            StageMilestoneReward reward = milestoneStore.Data.rewards.FirstOrDefault(item => item.stageType == selectedType && item.blockIndex == blockIndex);
            int startStage = (blockIndex * StagesPerBlock) + 1;
            int endStage = startStage + StagesPerBlock - 1;
            int rewardGems = reward != null && reward.rewardGems > 0 ? reward.rewardGems : 5;
            int requiredStars = reward != null && reward.requiredStars > 0 ? reward.requiredStars : 30;
            int totalStars = reward != null ? milestoneStore.GetBlockStars(reward, currentStages, progressStore) : GetBlockStars(blockIndex);
            bool unlocked = IsBlockUnlockedForRewards(blockIndex);
            bool rewardClaimed = reward != null && reward.claimedByUser;
            bool hasEnoughStars = totalStars >= requiredStars;
            BlockRewardVisualState state = !unlocked
                ? BlockRewardVisualState.Locked
                : !hasEnoughStars
                    ? BlockRewardVisualState.InProgress
                    : rewardClaimed
                        ? BlockRewardVisualState.Claimed
                        : BlockRewardVisualState.ClaimReady;

            return new BlockRewardViewModel
            {
                blockIndex = blockIndex,
                blockNumber = blockIndex + 1,
                startStage = startStage,
                endStage = endStage,
                totalStars = Mathf.Clamp(totalStars, 0, requiredStars),
                requiredStars = requiredStars,
                rewardGems = rewardGems,
                unlocked = unlocked,
                rewardClaimed = rewardClaimed,
                state = state
            };
        }

        private void CreateBlockRewardCard(RectTransform parent, BlockRewardViewModel data, Vector2 position)
        {
            AddRewardCardFrame(parent, data, position);

            GameObject cardObject = new GameObject($"Block{data.blockNumber}RewardCard", typeof(RectTransform), typeof(Image));
            cardObject.transform.SetParent(parent, false);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = position;
            card.sizeDelta = BlockRewardCardSize;

            Image background = cardObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(background, GetBlockRewardCardColor(data.blockIndex, data.state), 30);
            background.raycastTarget = false;
            AddOutline(cardObject, ShopCardOutline, 2f);
            AddShadow(cardObject, data.state == BlockRewardVisualState.Locked ? 0.28f : 0.42f, new Vector2(0f, -5f));

            RectTransform leftArea = CreateEmpty(card, "LeftArea", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(18f, 0f), new Vector2(150f, 0f));
            leftArea.pivot = new Vector2(0f, 0.5f);

            RectTransform leftDivider = CreatePanel(card, "LeftDivider", new Color(1f, 1f, 1f, 0.13f), 2, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(176f, 0f), new Vector2(2f, 146f));
            leftDivider.GetComponent<Image>().raycastTarget = false;

            RectTransform rewardDivider = CreatePanel(card, "RewardDivider", new Color(1f, 1f, 1f, 0.12f), 2, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(565f, 0f), new Vector2(2f, 146f));
            rewardDivider.GetComponent<Image>().raycastTarget = false;

            Text blockName = RuntimeUiFactory.CreateText(leftArea, "BlockName", $"Block\n{data.blockNumber}", 38, TextAnchor.MiddleCenter);
            blockName.rectTransform.anchorMin = Vector2.zero;
            blockName.rectTransform.anchorMax = Vector2.one;
            blockName.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            blockName.rectTransform.offsetMin = Vector2.zero;
            blockName.rectTransform.offsetMax = Vector2.zero;
            blockName.fontStyle = FontStyle.Bold;
            blockName.lineSpacing = 0.78f;
            blockName.color = data.state == BlockRewardVisualState.Locked ? new Color(0.75f, 0.78f, 0.86f, 1f) : Color.white;
            CasualUIStyle.ApplyTextDepth(blockName, true);

            Text stageRange = RuntimeUiFactory.CreateText(card, "StageRange", $"Stages {data.startStage} - {data.endStage}", 29, TextAnchor.MiddleLeft);
            stageRange.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            stageRange.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            stageRange.rectTransform.pivot = new Vector2(0f, 0.5f);
            stageRange.rectTransform.anchoredPosition = new Vector2(202f, 38f);
            stageRange.rectTransform.sizeDelta = new Vector2(230f, 56f);
            stageRange.fontStyle = FontStyle.Bold;
            stageRange.color = data.state == BlockRewardVisualState.Locked ? new Color(0.76f, 0.80f, 0.88f, 1f) : Color.white;
            CasualUIStyle.ApplyTextDepth(stageRange, true);

            if (data.state != BlockRewardVisualState.Locked)
            {
                CreateStageSpriteIcon(card, "RewardStarIcon", "star_row_1", new Vector2(435f, 38f), new Vector2(54f, 54f));
                Text stars = RuntimeUiFactory.CreateText(card, "Stars", $"{data.totalStars}/{data.requiredStars}", 31, TextAnchor.MiddleLeft);
                stars.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                stars.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                stars.rectTransform.pivot = new Vector2(0f, 0.5f);
                stars.rectTransform.anchoredPosition = new Vector2(475f, 38f);
                stars.rectTransform.sizeDelta = new Vector2(95f, 56f);
                stars.fontStyle = FontStyle.Bold;
                stars.color = Color.white;
                CasualUIStyle.ApplyTextDepth(stars, true);

                RectTransform barBack = CreatePanel(card, "ProgressBarBack", new Color(0.015f, 0.02f, 0.04f, 0.68f), 16, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(202f, 56f), new Vector2(330f, 34f));
                barBack.pivot = new Vector2(0f, 0.5f);
                AddOutline(barBack.gameObject, new Color(1f, 1f, 1f, 0.16f), 1f);
                RectTransform barFill = CreatePanel(barBack, "ProgressBarFill", GetBlockRewardFillColor(data.blockIndex), 14, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(Mathf.Max(0f, 320f * (data.totalStars / (float)Mathf.Max(1, data.requiredStars))), 24f));
                barFill.pivot = new Vector2(0f, 0.5f);

                CreateMainMenuSpriteIcon(card, "RewardGemIcon", "Icons/gem", new Vector2(620f, 0f), new Vector2(62f, 62f));
                Text gems = RuntimeUiFactory.CreateText(card, "Gems", $"x{data.rewardGems}", 32, TextAnchor.MiddleLeft);
                gems.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                gems.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                gems.rectTransform.pivot = new Vector2(0f, 0.5f);
                gems.rectTransform.anchoredPosition = new Vector2(676f, 0f);
                gems.rectTransform.sizeDelta = new Vector2(70f, 62f);
                gems.fontStyle = FontStyle.Bold;
                gems.color = Color.white;
                CasualUIStyle.ApplyTextDepth(gems, true);
            }
            else
            {
                Text lockedRange = RuntimeUiFactory.CreateText(card, "LockedRange", "Locked", 28, TextAnchor.MiddleLeft);
                lockedRange.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                lockedRange.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                lockedRange.rectTransform.pivot = new Vector2(0f, 0.5f);
                lockedRange.rectTransform.anchoredPosition = new Vector2(202f, -24f);
                lockedRange.rectTransform.sizeDelta = new Vector2(280f, 48f);
                lockedRange.fontStyle = FontStyle.Bold;
                lockedRange.color = new Color(0.74f, 0.78f, 0.88f, 1f);
                CasualUIStyle.ApplyTextDepth(lockedRange, true);
            }

            CreateBlockRewardStatus(card, data);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BlockRewardCard] block={data.blockNumber} unlocked={data.unlocked} totalStars={data.totalStars}/{data.requiredStars} rewardClaimed={data.rewardClaimed} state={data.state} cardSprite=runtimePanel claimReadyEffect={data.state == BlockRewardVisualState.ClaimReady} lockVisible={data.state == BlockRewardVisualState.Locked} gemVisible={data.state != BlockRewardVisualState.Locked} statusVisible={data.state != BlockRewardVisualState.Locked}");
            Debug.Log($"[BlockClaimReadyEffect] block={data.blockNumber} state={data.state} glow={(data.state == BlockRewardVisualState.ClaimReady ? "ON" : "OFF")} sweep=OFF sparkle=OFF mode=integrated-frame-and-claim-button");
#endif
        }

        private void AddRewardCardFrame(RectTransform parent, BlockRewardViewModel data, Vector2 position)
        {
            Color outer = data.state == BlockRewardVisualState.Locked
                ? new Color(0.82f, 0.86f, 1f, 0.34f)
                : new Color(1f, 0.68f, 0.16f, 0.95f);
            Color inner = data.state == BlockRewardVisualState.Locked
                ? new Color(1f, 1f, 1f, 0.14f)
                : new Color(1f, 0.93f, 0.56f, 0.52f);

            RectTransform outerFrame = CreateRoundedFramePanel(parent, $"Block{data.blockNumber}RewardOuterFrame", position, BlockRewardCardSize + new Vector2(14f, 14f), outer, 34);
            RectTransform innerFrame = CreateRoundedFramePanel(parent, $"Block{data.blockNumber}RewardInnerFrame", position, BlockRewardCardSize + new Vector2(6f, 6f), inner, 32);

            if (data.state == BlockRewardVisualState.ClaimReady)
            {
                outerFrame.gameObject.AddComponent<RewardFramePulse>().Configure(outer, 0.68f, 1f, 1.006f);
                innerFrame.gameObject.AddComponent<RewardFramePulse>().Configure(inner, 0.32f, 0.62f, 1.003f);
            }
        }

        private RectTransform CreateRoundedFramePanel(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, int radius)
        {
            RectTransform frame = CreatePanel(parent, name, color, radius, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.GetComponent<Image>().raycastTarget = false;
            return frame;
        }

        private void CreateBlockRewardStatus(RectTransform card, BlockRewardViewModel data)
        {
            if (data.state == BlockRewardVisualState.Locked)
            {
                CreateStageSpriteIcon(card, "RewardLockIcon", "lock_silver", new Vector2(812f, 5f), new Vector2(72f, 72f));
                return;
            }

            string label = data.state == BlockRewardVisualState.ClaimReady
                ? "Claim"
                : data.state == BlockRewardVisualState.Claimed
                    ? "Claimed"
                    : "In Progress";
            Color color = data.state == BlockRewardVisualState.ClaimReady
                ? new Color(0.34f, 0.88f, 0.10f, 1f)
                : data.state == BlockRewardVisualState.Claimed
                    ? new Color(0.22f, 0.56f, 0.20f, 1f)
                    : new Color(0.12f, 0.36f, 0.82f, 1f);

            RectTransform badge = CreatePanel(card, "StatusBadge", color, 20, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-98f, 0f), new Vector2(184f, 74f));
            Image badgeImage = badge.GetComponent<Image>();
            badgeImage.raycastTarget = data.state == BlockRewardVisualState.ClaimReady;
            if (data.state == BlockRewardVisualState.ClaimReady)
            {
                Button claimButton = badge.gameObject.AddComponent<Button>();
                claimButton.transition = Selectable.Transition.None;
                claimButton.onClick.AddListener(() => TryClaimBlockReward(data.blockIndex));
            }

            AddOutline(badge.gameObject, data.state == BlockRewardVisualState.ClaimReady ? new Color(1f, 0.93f, 0.36f, 1f) : new Color(0.88f, 0.92f, 1f, 0.55f), 2f);
            Text status = RuntimeUiFactory.CreateText(badge, "Label", label, data.state == BlockRewardVisualState.InProgress ? 22 : 25, TextAnchor.MiddleCenter);
            status.fontStyle = FontStyle.Bold;
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 18;
            status.resizeTextMaxSize = data.state == BlockRewardVisualState.InProgress ? 22 : 25;
            status.color = Color.white;
            status.rectTransform.offsetMin = Vector2.zero;
            status.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(status, true);

            if (data.state == BlockRewardVisualState.ClaimReady)
            {
                badge.gameObject.AddComponent<ClaimReadyBadgePulse>().Configure(color);
            }
        }

        private void TryClaimBlockReward(int blockIndex)
        {
            BlockRewardViewModel before = BuildBlockRewardViewModel(blockIndex);
            int beforeGems = walletStore.Gems;
            bool claimable = IsBlockRewardClaimable(blockIndex);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BlockRewardClaimCheck] block={blockIndex + 1} stars={before.totalStars}/{before.requiredStars} claimed={before.rewardClaimed} claimable={claimable} state={before.state}");
#endif
            if (!claimable || !milestoneStore.TryClaimBlock(blockIndex, selectedType, currentStages, progressStore, walletStore))
            {
                return;
            }

            int afterGems = walletStore.Gems;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BlockRewardClaim] block={blockIndex + 1} rewardAmount={before.rewardGems} beforeGems={beforeGems} afterGems={afterGems} claimedSaved=True");
#endif
            RefreshBlockRewardsPopup();
            RefreshRewardsButtonAttention();
        }

        private void RefreshRewardsButtonAttention()
        {
            if (mapRewardsAttention != null)
            {
                bool hasClaimable = HasAnyClaimableBlockReward();
                mapRewardsAttention.SetActive(hasClaimable);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[RewardsAttentionEffect] buttonActive={mapRewardsButton != null && mapRewardsButton.gameObject.activeInHierarchy} hasClaimable={hasClaimable} mode=integrated-button-tint-scale");
#endif
            }
        }

        private bool HasAnyClaimableBlockReward()
        {
            milestoneStore.EnsureMilestones(currentStages);
            return Enumerable.Range(0, GetBlockCountForSelectedMode()).Any(IsBlockRewardClaimable);
        }

        private bool IsBlockRewardClaimable(int blockIndex)
        {
            BlockRewardViewModel data = BuildBlockRewardViewModel(blockIndex);
            bool claimable = data.state == BlockRewardVisualState.ClaimReady;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BlockRewardClaimCheck] block={data.blockNumber} stars={data.totalStars}/{data.requiredStars} claimed={data.rewardClaimed} claimable={claimable} state={data.state}");
#endif
            return claimable;
        }

        private void ReturnToCurrentProgressView()
        {
            displayMode = StageMapDisplayMode.CurrentProgress;
            blockBrowseScrollOffset = 0;
            RefreshFixedWindowAroundCurrentStage();
        }

        private void ShowModeMessage(string message)
        {
            if (heartPopup == null || heartPopup.activeSelf)
            {
                return;
            }

            StopHeartPopupCountdown();
            ConfigureHeartPopupLayout(false);
            SetHeartAdControlsVisible(false);
            heartPopupBody.text = message;
            heartPopup.SetActive(true);
            heartPopup.transform.SetAsLastSibling();
        }

        private void BuildHeartPopup(Transform parent)
        {
            heartPopup = new GameObject("StageInfoPopup", typeof(RectTransform), typeof(Image));
            heartPopup.transform.SetParent(parent, false);
            RectTransform panel = heartPopup.GetComponent<RectTransform>();
            heartPopupPanel = panel;
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(720f, 420f);
            CasualUIStyle.ApplyPanel(heartPopup.GetComponent<Image>(), new Color(0.028f, 0.044f, 0.11f, 0.99f), 28);
            AddOutline(heartPopup, Gold, 3f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Stages", 40, TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(40f, 320f);
            title.rectTransform.offsetMax = new Vector2(-40f, -28f);
            title.fontStyle = FontStyle.Bold;
            title.color = Cream;
            CasualUIStyle.ApplyTextDepth(title, true);

            heartPopupBody = RuntimeUiFactory.CreateText(panel, "Body", string.Empty, 27, TextAnchor.MiddleCenter);
            heartPopupBody.rectTransform.offsetMin = new Vector2(52f, 124f);
            heartPopupBody.rectTransform.offsetMax = new Vector2(-52f, -96f);

            heartAdStatusText = RuntimeUiFactory.CreateText(panel, "HeartAdStatus", string.Empty, 22, TextAnchor.MiddleCenter);
            heartAdStatusText.rectTransform.offsetMin = new Vector2(52f, 158f);
            heartAdStatusText.rectTransform.offsetMax = new Vector2(-52f, -356f);
            heartAdStatusText.color = new Color(0.82f, 0.9f, 1f, 1f);

            heartAdButton = CasualUIFactory.CreateActionButton(panel, "WatchHeartAdButton", "Watch Ad +1 Heart", string.Empty, CasualUIColor.Green, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 124f), new Vector2(545f, 70f));
            heartAdButton.onClick.AddListener(RequestRewardedHeartAd);

            Button shop = CasualUIFactory.CreateActionButton(panel, "GoToShopButton", "Shop", "shop", CasualUIColor.Orange, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 34f), new Vector2(245f, 68f));
            ApplyTopHudShopIcon(shop);
            shop.onClick.AddListener(() =>
            {
                ClearStageTransitionFade();
                StopHeartPopupCountdown();
                heartPopup.SetActive(false);
                Hide();
                shopAction?.Invoke();
            });

            Button close = CasualUIFactory.CreateActionButton(panel, "CloseButton", "Close", string.Empty, CasualUIColor.Blue, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150f, 34f), new Vector2(245f, 68f));
            close.onClick.AddListener(() =>
            {
                ClearStageTransitionFade();
                StopHeartPopupCountdown();
                EnsureStageMapVisibleBehindPopup();
                heartPopup.SetActive(false);
            });
            SetHeartAdControlsVisible(false);
            heartPopup.SetActive(false);
        }

        private static void ApplyTopHudShopIcon(Button button)
        {
            if (button == null)
            {
                return;
            }

            Sprite shopSprite = CasualIconFactory.LoadMainMenuKitSprite("Icons/shop");
            if (shopSprite == null)
            {
                return;
            }

            Transform iconHolder = button.transform.Find("IconHolder");
            if (iconHolder == null)
            {
                return;
            }

            for (int i = iconHolder.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(iconHolder.GetChild(i).gameObject);
            }

            GameObject iconObject = new GameObject("TopHudShopIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(iconHolder, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = shopSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
        }

        private void ShowHeartPopup()
        {
            ClearStageTransitionFade();
            EnsureStageMapVisibleBehindPopup();
            ConfigureHeartPopupLayout(true);
            SetHeartAdControlsVisible(true);
            RefreshHeartPopupContent();
            heartPopup.SetActive(true);
            heartPopup.transform.SetAsLastSibling();
            StartHeartPopupCountdown();
        }

        private void ConfigureHeartPopupLayout(bool showAdReward)
        {
            if (heartPopupPanel == null || heartPopupBody == null)
            {
                return;
            }

            heartPopupPanel.sizeDelta = showAdReward ? new Vector2(760f, 650f) : new Vector2(720f, 420f);
            heartPopupBody.fontSize = showAdReward ? 26 : 27;
            heartPopupBody.rectTransform.offsetMin = showAdReward ? new Vector2(52f, 262f) : new Vector2(52f, 124f);
            heartPopupBody.rectTransform.offsetMax = showAdReward ? new Vector2(-52f, -112f) : new Vector2(-52f, -96f);
            if (heartAdStatusText != null)
            {
                heartAdStatusText.rectTransform.offsetMin = showAdReward ? new Vector2(52f, 174f) : new Vector2(52f, 158f);
                heartAdStatusText.rectTransform.offsetMax = showAdReward ? new Vector2(-52f, -448f) : new Vector2(-52f, -356f);
            }

            if (heartAdButton != null)
            {
                RectTransform adRect = heartAdButton.GetComponent<RectTransform>();
                adRect.anchoredPosition = showAdReward ? new Vector2(0f, 124f) : new Vector2(0f, 112f);
                adRect.sizeDelta = showAdReward ? new Vector2(545f, 70f) : new Vector2(460f, 70f);
            }
        }

        private void SetHeartAdControlsVisible(bool visible)
        {
            if (heartAdButton != null)
            {
                heartAdButton.gameObject.SetActive(visible);
            }

            if (heartAdStatusText != null)
            {
                heartAdStatusText.gameObject.SetActive(visible);
            }
        }

        private void RefreshHeartPopupContent()
        {
            if (heartPopupBody == null)
            {
                return;
            }

            walletStore.RefreshHearts();
            int hearts = walletStore.Hearts;
            int seconds = walletStore.SecondsUntilNextHeart;
            string countdown = hearts >= WalletStore.MaxNaturalHearts
                ? "Hearts are full"
                : $"{UIStrings.NextHeartIn} {FormatCountdown(seconds)}";
            heartPopupBody.text =
                "Hearts are used to play stages.\n"
                + $"{UIStrings.HeartsRecharge}\n"
                + $"{countdown}\n\n"
                + "Need a heart now?\n"
                + "Watch an ad to get 1 Heart.";

            RefreshHeartAdButtonState(hearts);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[HeartPopup] hearts={hearts}, maxHearts={WalletStore.MaxNaturalHearts}, nextHeartSeconds={seconds}, countdownText={countdown}");
#endif
        }

        private void RefreshHeartAdButtonState(int hearts)
        {
            if (heartAdButton == null)
            {
                return;
            }

            bool canWatch = CanWatchRewardedHeartAd(hearts, out string reason, out string buttonText, out string statusText);
            heartAdButton.interactable = canWatch;
            SetButtonLabel(heartAdButton, buttonText);
            if (heartAdStatusText != null)
            {
                heartAdStatusText.text = statusText;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[HeartAdReward] dailyCount={adsRewardLimitStore.DailyHeartAdTotalCount}, batchCount={adsRewardLimitStore.HeartAdBatchCount}, cooldownEnd={adsRewardLimitStore.HeartAdBatchCooldownEndTimeUtc:o}, canWatch={canWatch}, reason={reason}");
#endif
        }

        private bool CanWatchRewardedHeartAd(int hearts, out string reason, out string buttonText, out string statusText)
        {
            if (hearts >= WalletStore.MaxNaturalHearts)
            {
                reason = "hearts_full";
                buttonText = "Hearts are full";
                statusText = "Your hearts are already full.";
                return false;
            }

            if (adsRewardLimitStore.DailyHeartAdTotalCount >= HeartAdDailyLimit)
            {
                reason = "daily_limit";
                buttonText = "Ad limit reached today";
                statusText = "You can watch more heart ads tomorrow.";
                return false;
            }

            DateTime cooldownEnd = adsRewardLimitStore.HeartAdBatchCooldownEndTimeUtc;
            if (adsRewardLimitStore.HeartAdBatchCount >= HeartAdBatchLimit && DateTime.UtcNow < cooldownEnd)
            {
                TimeSpan remaining = cooldownEnd - DateTime.UtcNow;
                reason = "batch_cooldown";
                buttonText = "Ad hearts available later";
                statusText = $"Next ad heart batch in {FormatCountdown(Mathf.CeilToInt((float)remaining.TotalSeconds))}.";
                return false;
            }

            if (!IsRewardedHeartAdReady())
            {
                reason = "ad_not_ready";
                buttonText = "Ad not ready";
                statusText = "Ad is not ready. Try again later.";
                return false;
            }

            reason = "ready";
            buttonText = "Watch Ad +1 Heart";
            statusText = $"Ad hearts today: {adsRewardLimitStore.DailyHeartAdTotalCount}/{HeartAdDailyLimit}  Batch: {adsRewardLimitStore.HeartAdBatchCount}/{HeartAdBatchLimit}";
            return true;
        }

        private void RequestRewardedHeartAd()
        {
            walletStore.RefreshHearts();
            int hearts = walletStore.Hearts;
            if (!CanWatchRewardedHeartAd(hearts, out _, out _, out _))
            {
                RefreshHeartPopupContent();
                return;
            }

            if (heartAdButton != null)
            {
                heartAdButton.interactable = false;
            }

            rewardService.Show(
                RewardedAdPlacement.HeartPlus1,
                OnRewardedHeartAdCompleted,
                result =>
                {
                    if (result != RewardedAdResult.Rewarded)
                    {
                        OnRewardedHeartAdFailedOrCancelled();
                    }
                });
        }

        private void OnRewardedHeartAdCompleted()
        {
            int heartsBefore = walletStore.Hearts;
            if (!CanWatchRewardedHeartAd(heartsBefore, out _, out _, out _))
            {
                RefreshHeartPopupContent();
                return;
            }

            walletStore.AddHearts(1);
            adsRewardLimitStore.RecordHeartAdReward(DateTime.UtcNow, HeartAdBatchLimit, TimeSpan.FromMinutes(HeartAdBatchCooldownMinutes));
            int heartsAfter = walletStore.Hearts;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[HeartAdRewardComplete] heartsBefore={heartsBefore}, heartsAfter={heartsAfter}, dailyCount={adsRewardLimitStore.DailyHeartAdTotalCount}, batchCount={adsRewardLimitStore.HeartAdBatchCount}");
            Debug.Log($"[AdRewardApplied] placement={RewardedAdPlacement.HeartPlus1}, before={heartsBefore}, after={heartsAfter}");
#endif
            RefreshHeartPopupContent();
        }

        private void OnRewardedHeartAdFailedOrCancelled()
        {
            if (heartAdStatusText != null)
            {
                heartAdStatusText.text = "Ad was not completed.";
            }

            RefreshHeartPopupContent();
        }

        private bool IsRewardedHeartAdReady()
        {
            return rewardService != null && rewardService.CanShow(RewardedAdPlacement.HeartPlus1);
        }

        private void StartHeartPopupCountdown()
        {
            StopHeartPopupCountdown();
            if (animationRunner != null)
            {
                heartPopupCountdownRoutine = animationRunner.StartCoroutine(HeartPopupCountdownLoop());
            }
        }

        private void StopHeartPopupCountdown()
        {
            if (animationRunner != null && heartPopupCountdownRoutine != null)
            {
                animationRunner.StopCoroutine(heartPopupCountdownRoutine);
            }

            heartPopupCountdownRoutine = null;
        }

        private IEnumerator HeartPopupCountdownLoop()
        {
            while (heartPopup != null && heartPopup.activeSelf && heartAdButton != null && heartAdButton.gameObject.activeSelf)
            {
                RefreshHeartPopupContent();
                yield return new WaitForSecondsRealtime(1f);
            }

            heartPopupCountdownRoutine = null;
        }

        private static string FormatCountdown(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private void ClearStageTransitionFade()
        {
            if (transitionFadeImage != null)
            {
                Color color = transitionFadeImage.color;
                color.a = 0f;
                transitionFadeImage.color = color;
            }

            if (transitionFadeRoot != null)
            {
                transitionFadeRoot.SetActive(false);
            }
        }

        private void EnsureStageMapVisibleBehindPopup()
        {
            root.SetActive(true);
            modeSelectRoot.gameObject.SetActive(false);
            mapRoot.gameObject.SetActive(true);
            mapRoot.transform.SetAsLastSibling();
            if (heartPopup != null)
            {
                heartPopup.transform.SetAsLastSibling();
            }
        }

        private static Sprite CreateVerticalGradientSprite(string name, Color bottom, Color top)
        {
            const int width = 8;
            const int height = 256;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                Color color = Color.Lerp(bottom, top, t);
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        public sealed class StageMapMarkerSettings : MonoBehaviour
        {
            public Vector2 markerOffset = DefaultMarkerOffset;
            public Vector2 markerSize = DefaultMarkerSize;
            public float markerHopDuration = 0.4f;
            public float markerHopMoveHeight = 58f;
            public float markerHopInPlaceHeight = 42f;
            public float waitBeforeMarkerHop = 0.25f;
            public float waitAfterMarkerHop = 0.35f;
            public float screenFadeDuration = 0.25f;
        }

        private sealed class RewardsButtonAttention : MonoBehaviour
        {
            private RectTransform rect;
            private Image buttonImage;
            private Vector3 baseScale;
            private Vector2 baseAnchoredPosition;
            private Quaternion baseRotation;
            private Color baseColor = Color.white;
            private bool active;

            private void Awake()
            {
                rect = GetComponent<RectTransform>();
                buttonImage = GetComponent<Image>();
                if (buttonImage != null)
                {
                    baseColor = buttonImage.color;
                }

                if (rect != null)
                {
                    baseScale = rect.localScale;
                    baseAnchoredPosition = rect.anchoredPosition;
                    baseRotation = rect.localRotation;
                }
            }

            public void SetActive(bool value)
            {
                if (active == value)
                {
                    return;
                }

                active = value;
                if (rect == null)
                {
                    return;
                }

                if (active)
                {
                    baseScale = rect.localScale;
                    baseAnchoredPosition = rect.anchoredPosition;
                    baseRotation = rect.localRotation;
                    baseColor = buttonImage != null ? buttonImage.color : baseColor;
                    return;
                }

                rect.localScale = baseScale;
                rect.anchoredPosition = baseAnchoredPosition;
                rect.localRotation = baseRotation;
                if (buttonImage != null)
                {
                    buttonImage.color = baseColor;
                }
            }

            private void Update()
            {
                if (!active || rect == null)
                {
                    return;
                }

                float wave = Mathf.Sin(Time.unscaledTime * 5.6f);
                float slowPulse = (Mathf.Sin(Time.unscaledTime * 4.2f) + 1f) * 0.5f;
                rect.anchoredPosition = baseAnchoredPosition + new Vector2(wave * 2.2f, 0f);
                rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wave * 0.9f);
                rect.localScale = baseScale * Mathf.Lerp(1f, 1.025f, slowPulse);

                if (buttonImage != null)
                {
                    Color lit = new Color(
                        Mathf.Clamp01(baseColor.r + 0.08f),
                        Mathf.Clamp01(baseColor.g + 0.07f),
                        Mathf.Clamp01(baseColor.b + 0.02f),
                        baseColor.a);
                    buttonImage.color = Color.Lerp(baseColor, lit, slowPulse * 0.55f);
                }
            }
        }

        private sealed class RewardFramePulse : MonoBehaviour
        {
            private RectTransform rect;
            private Image image;
            private Vector3 baseScale;
            private Color baseColor;
            private float minAlpha;
            private float maxAlpha;
            private float maxScale = 1.004f;

            public void Configure(Color color, float minAlpha, float maxAlpha, float maxScale)
            {
                baseColor = color;
                this.minAlpha = minAlpha;
                this.maxAlpha = maxAlpha;
                this.maxScale = maxScale;
                image = GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }

            private void Awake()
            {
                rect = GetComponent<RectTransform>();
                image = GetComponent<Image>();
                baseScale = rect != null ? rect.localScale : Vector3.one;
                if (image != null && baseColor == default)
                {
                    baseColor = image.color;
                    minAlpha = Mathf.Max(0f, baseColor.a * 0.74f);
                    maxAlpha = baseColor.a;
                }
            }

            private void Update()
            {
                if (rect == null || image == null)
                {
                    return;
                }

                float t = (Mathf.Sin(Time.unscaledTime * 3.6f) + 1f) * 0.5f;
                rect.localScale = baseScale * Mathf.Lerp(1f, maxScale, t);
                Color color = baseColor;
                color.a = Mathf.Lerp(minAlpha, maxAlpha, t);
                image.color = color;
            }
        }

        private sealed class ClaimReadyBadgePulse : MonoBehaviour
        {
            private RectTransform rect;
            private Image image;
            private Outline outline;
            private Vector3 baseScale;
            private Color baseColor;
            private Color baseOutlineColor;

            public void Configure(Color color)
            {
                baseColor = color;
                image = GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }

            private void Awake()
            {
                rect = GetComponent<RectTransform>();
                image = GetComponent<Image>();
                outline = GetComponent<Outline>();
                baseScale = rect != null ? rect.localScale : Vector3.one;
                if (image != null && baseColor == default)
                {
                    baseColor = image.color;
                }

                if (outline != null)
                {
                    baseOutlineColor = outline.effectColor;
                }
            }

            private void Update()
            {
                if (rect == null || image == null)
                {
                    return;
                }

                float t = (Mathf.Sin(Time.unscaledTime * 4.8f) + 1f) * 0.5f;
                rect.localScale = baseScale * Mathf.Lerp(1f, 1.035f, t);

                Color lit = new Color(
                    Mathf.Clamp01(baseColor.r + 0.12f),
                    Mathf.Clamp01(baseColor.g + 0.10f),
                    Mathf.Clamp01(baseColor.b + 0.03f),
                    baseColor.a);
                image.color = Color.Lerp(baseColor, lit, t * 0.55f);

                if (outline != null)
                {
                    Color outlineColor = baseOutlineColor;
                    outlineColor.a = Mathf.Lerp(0.65f, 1f, t);
                    outline.effectColor = outlineColor;
                    outline.effectDistance = Vector2.one * Mathf.Lerp(2f, 3f, t);
                }
            }
        }

        private sealed class StageBlockBrowseScrollInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
        {
            private const float DragThreshold = 42f;
            private Action<float> scrollAction;
            private float accumulatedDragY;

            public void Initialize(Action<float> onScroll)
            {
                scrollAction = onScroll;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                accumulatedDragY = 0f;
            }

            public void OnDrag(PointerEventData eventData)
            {
                accumulatedDragY += eventData.delta.y;
                if (Mathf.Abs(accumulatedDragY) < DragThreshold)
                {
                    return;
                }

                float direction = accumulatedDragY > 0f ? 1f : -1f;
                accumulatedDragY = 0f;
                scrollAction?.Invoke(direction);
            }

            public void OnScroll(PointerEventData eventData)
            {
                if (Mathf.Approximately(eventData.scrollDelta.y, 0f))
                {
                    return;
                }

                scrollAction?.Invoke(eventData.scrollDelta.y < 0f ? 1f : -1f);
            }
        }

        private sealed class StageMapAnimationRunner : MonoBehaviour
        {
            private Coroutine running;

            public void Play(IEnumerator routine)
            {
                if (running != null)
                {
                    StopCoroutine(running);
                }

                running = StartCoroutine(Wrap(routine));
            }

            private IEnumerator Wrap(IEnumerator routine)
            {
                yield return routine;
                running = null;
            }
        }
    }
}
