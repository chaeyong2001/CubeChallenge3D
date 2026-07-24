using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Core;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.GameModes.QuickPlay;
using CubeChallenge3D.GameModes.RankingChallenge;
using CubeChallenge3D.GameModes.Stages;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.Stages.Generation;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Stages;
using CubeChallenge3D.UI.Style;
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
        [SerializeField] private float clearPopupFadeDuration = 0.2f;
        [SerializeField] private float screenFadeDuration = 0.25f;
        [SerializeField] private float conditionStarIconSize = 55f;

        private Canvas canvas;
        private Text titleText;
        private Text timerText;
        private Text movesText;
        private Text statusText;
        private GameObject timerPill;
        private RectTransform actionRoot;
        private GameObject overlayRoot;
        private Image overlayBackground;
        private RectTransform overlayPanel;
        private Text overlayTitle;
        private Text overlayBody;
        private Button overlayPrimaryButton;
        private Button overlaySecondaryButton;
        private Button overlayTertiaryButton;
        private Button overlayCloseButton;
        private RectTransform overlayStarsRoot;
        private readonly List<Image> overlayStarIcons = new List<Image>();
        private RectTransform starConditionsRoot;
        private GameObject rankingPopupRoot;
        private GameObject weeklyRewardsPopupRoot;
        private Text weeklyRewardsTitleText;
        private Text weeklyRewardsDescriptionText;
        private Text weeklyRewardsMessageText;
        private RectTransform weeklyRewardsRowsRoot;
        private Button weeklyRewardsPrimaryButton;
        private Button weeklyRewardsCloseButton;
        private Text rankingEntryCostText;
        private Text rankingCountdownText;
        private RankingChallengeState? lastRankingState;
        private float rankingPreviewCountdown;
        private bool rankingCountdownStarted;
        private bool rankingStartTextShown;
        private Image rankingWorldTabImage;
        private Image rankingMyTabImage;
        private RectTransform rankingListContent;
        private RectTransform rankingStickyRow;
        private ScrollRect rankingScrollRect;
        private VerticalLayoutGroup rankingListLayout;
        private Text rankingStatusText;
        private int rankingLoadToken;
        private bool rankingShowingWorldTab = true;
        private string rankingStickySubmissionId = string.Empty;
        private readonly Dictionary<string, RectTransform> rankingRowsBySubmissionId = new Dictionary<string, RectTransform>();
        private Button weeklyRankingRewardsButton;
        private WeeklyRankingRewardsButtonAttention weeklyRankingRewardsAttention;
        private WeeklyRankingRewardDto pendingWeeklyRankingReward;
        private bool weeklyRewardClaimInProgress;
        private int weeklyRewardCheckToken;
        private TargetPreviewCubeView targetPreview;
        private Button targetMiniButton;
        private Button targetToggleButton;
        private Button interactionModeButton;
        private RectTransform stageStarsRoot;
        private Text stageStarsLabel;
        private Button stageStarInfoButton;
        private readonly List<Image> stageStarIcons = new List<Image>();
        private string previewMode = string.Empty;
        private string interactionSpriteKey = string.Empty;
        private bool isTransitioningToNextStage;
        private string autoStartedReverseTargetStageId = string.Empty;
        private static readonly Color RankingPanelColor = new Color(0.010f, 0.048f, 0.074f, 1f);
        private static readonly Color RankingInnerPanelColor = new Color(0.008f, 0.038f, 0.062f, 1f);
        private static readonly Color RankingActiveTabColor = new Color(0.035f, 0.260f, 0.900f, 1f);
        private static readonly Color RankingInactiveTabColor = new Color(0.018f, 0.056f, 0.094f, 1f);
        private static readonly Color RankingGoldColor = new Color(1f, 0.620f, 0.120f, 1f);
        private static readonly Color RankingWarmTextColor = new Color(1f, 0.875f, 0.565f, 1f);
        private static readonly Color RankingBodyTextColor = new Color(0.910f, 0.940f, 1f, 1f);
        private static readonly Color RankingMutedTextColor = new Color(0.660f, 0.735f, 0.865f, 1f);
        private const string StageGeneratedArtPath = "UI/Stages/Generated/";
        private static readonly Dictionary<string, Sprite> StageHudSpriteCache = new Dictionary<string, Sprite>();

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
            BackNavigationManager.SetCurrentHandler(GetBackNavigationScreenName(), HandleAndroidBack);
        }

        private void OnDestroy()
        {
            BackNavigationManager.ClearCurrentHandler(HandleAndroidBack);
        }

        private void Update()
        {
            Refresh();
        }

        private void BuildUi()
        {
            bool stageLayout = launchMode == GameLaunchMode.StagePlay;
            bool rankingLayout = IsRankingStyleMode();
            bool polishedGameplayLayout = stageLayout || rankingLayout;
            string canvasName = polishedGameplayLayout
                ? "MobileStageGameplayHudCanvas"
                : "GameplayHudCanvas";
            canvas = RuntimeUiFactory.CreateCanvas(transform, canvasName, 1250, 0f);
            // Gameplay is rendered by the camera. Keep this overlay transparent so it
            // cannot cover the cube while still allowing lightweight HUD sparkles.
            CasualUIFactory.CreateBackdrop(
                canvas.transform,
                "HudAtmosphere",
                false,
                !polishedGameplayLayout);
            TintGameplayCamera();
            TopCurrencyBar.Attach(
                canvas,
                OpenShop,
                polishedGameplayLayout,
                OpenGemItemsShop,
                OpenGemItemsShop,
                OpenPromotionShop);

            GameObject safeObject = new GameObject("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(canvas.transform, false);
            RectTransform safeArea = safeObject.GetComponent<RectTransform>();

            Vector2 titlePosition = stageLayout
                ? new Vector2(0f, -214f)
                : rankingLayout
                    ? new Vector2(0f, -166f)
                    : new Vector2(0f, -184f);
            Vector2 titleSize = stageLayout
                ? new Vector2(690f, 76f)
                : rankingLayout
                    ? new Vector2(720f, 60f)
                    : new Vector2(620f, 62f);
            int titleFontSize = stageLayout ? 38 : rankingLayout ? 32 : 36;
            Vector2 timerPosition = rankingLayout
                ? new Vector2(0f, -232f)
                : new Vector2(0f, -242f);
            Vector2 timerSize = rankingLayout
                ? new Vector2(480f, 60f)
                : new Vector2(520f, 70f);
            Vector2 movesPosition = stageLayout
                ? new Vector2(0f, -302f)
                : rankingLayout
                    ? new Vector2(0f, -300f)
                    : new Vector2(0f, -320f);
            Vector2 movesSize = stageLayout
                ? new Vector2(370f, 64f)
                : rankingLayout
                    ? new Vector2(310f, 54f)
                    : new Vector2(340f, 54f);
            float statusY = stageLayout ? -374f : rankingLayout ? -356f : -382f;
            int statusFontSize = stageLayout ? 24 : rankingLayout ? 21 : 22;
            float statusHeight = stageLayout ? 48f : rankingLayout ? 44f : 50f;
            Vector2 interactionPosition = stageLayout
                ? new Vector2(-28f, -302f)
                : rankingLayout
                    ? new Vector2(-28f, -300f)
                    : new Vector2(-28f, -212f);
            Vector2 interactionSize = stageLayout
                ? new Vector2(176f, 76f)
                : rankingLayout
                    ? new Vector2(160f, 68f)
                    : new Vector2(180f, 74f);

            CasualUIFactory.CreateStatChip(
                safeArea,
                "TitleChip",
                titlePosition,
                titleSize,
                out titleText);
            titleText.fontSize = titleFontSize;
            CasualUIFactory.CreateStatChip(
                safeArea,
                "TimerPill",
                timerPosition,
                timerSize,
                out timerText);
            timerPill = timerText.transform.parent.gameObject;
            CasualUIFactory.CreateStatChip(
                safeArea,
                "MovesPill",
                movesPosition,
                movesSize,
                out movesText);
            statusText = CreateTopText(
                safeArea,
                "Status",
                statusY,
                statusFontSize,
                statusHeight);
            if (stageLayout)
            {
                BuildStageStarsStatus(safeArea, statusY, statusHeight);
            }

            interactionModeButton = CreateAnchoredButton(
                safeArea, "InteractionModeButton", "Solve",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                interactionPosition,
            interactionSize);
            interactionModeButton.onClick.AddListener(ToggleInteractionMode);
            if (stageLayout)
            {
                targetToggleButton = CreateAnchoredButton(
                    safeArea,
                    "TargetTopButton",
                    "Target",
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(-interactionPosition.x, interactionPosition.y),
                    interactionSize);
                targetToggleButton.onClick.AddListener(ToggleTargetView);
                targetToggleButton.gameObject.SetActive(false);
            }
            if (polishedGameplayLayout)
            {
                PolishStageHeader(titleText, movesText, statusText);
                PolishStageActionButton(interactionModeButton, false);
                PolishStageActionButton(targetToggleButton, false);
            }

            if (rankingLayout)
            {
                RectTransform titleRoot = titleText != null && titleText.transform.parent != null
                    ? titleText.transform.parent as RectTransform
                    : null;
                RectTransform timerRoot = timerPill != null ? timerPill.GetComponent<RectTransform>() : null;
                RectTransform movesRoot = movesText != null && movesText.transform.parent != null
                    ? movesText.transform.parent as RectTransform
                    : null;
                RectTransform statusRoot = statusText != null ? statusText.rectTransform : null;
                RectTransform interactionRoot = interactionModeButton != null
                    ? interactionModeButton.GetComponent<RectTransform>()
                    : null;
                AddMobileTitleAligner(
                    safeArea.gameObject,
                    "RankingChallenge",
                    titleRoot,
                    true,
                    timerRoot,
                    movesRoot,
                    statusRoot,
                    interactionRoot);
            }

            if (launchMode == GameLaunchMode.RankingChallenge)
            {
                rankingCountdownText = CreateTopText(
                    safeArea,
                    "RankingCountdown",
                    -820f,
                    58,
                    120f);
                rankingCountdownText.fontStyle = FontStyle.Bold;
                rankingCountdownText.color = new Color(1f, 0.92f, 0.48f, 1f);
                CasualUIStyle.ApplyTextDepth(rankingCountdownText, true);
                rankingCountdownText.gameObject.SetActive(false);
            }

            GameObject actionsObject = new GameObject("Actions", typeof(RectTransform));
            actionsObject.transform.SetParent(safeArea, false);
            actionRoot = actionsObject.GetComponent<RectTransform>();
            actionRoot.anchorMin = new Vector2(0f, 0f);
            actionRoot.anchorMax = new Vector2(1f, 0f);
            actionRoot.pivot = new Vector2(0.5f, 0f);
            actionRoot.anchoredPosition = Vector2.zero;
            actionRoot.sizeDelta = new Vector2(0f, 560f);

            BuildOverlay(safeArea);
            targetPreview = new GameObject("TargetPreviewCubeView").AddComponent<TargetPreviewCubeView>();
            targetPreview.transform.SetParent(transform, false);
            targetPreview.Hide();
        }

        private void BuildStageStarsStatus(RectTransform parent, float statusY, float statusHeight)
        {
            GameObject rootObject = new GameObject("StageStarsStatus", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            stageStarsRoot = rootObject.GetComponent<RectTransform>();
            stageStarsRoot.anchorMin = new Vector2(0.5f, 1f);
            stageStarsRoot.anchorMax = new Vector2(0.5f, 1f);
            stageStarsRoot.pivot = new Vector2(0.5f, 0.5f);
            stageStarsRoot.anchoredPosition = new Vector2(0f, statusY - 32.4f);
            stageStarsRoot.sizeDelta = new Vector2(430f, statusHeight + 18f);

            stageStarsLabel = RuntimeUiFactory.CreateText(stageStarsRoot, "Label", string.Empty, 24, TextAnchor.MiddleRight);
            stageStarsLabel.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            stageStarsLabel.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            stageStarsLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
            stageStarsLabel.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            stageStarsLabel.rectTransform.sizeDelta = new Vector2(88f, statusHeight);
            stageStarsLabel.fontStyle = FontStyle.Bold;
            stageStarsLabel.color = Color.white;
            CasualUIStyle.ApplyTextDepth(stageStarsLabel, true);
            stageStarsLabel.gameObject.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                GameObject starObject = new GameObject($"Star{i + 1}", typeof(RectTransform), typeof(Image));
                starObject.transform.SetParent(stageStarsRoot, false);
                RectTransform starRect = starObject.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0f, 0.5f);
                starRect.anchorMax = new Vector2(0f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                starRect.anchoredPosition = new Vector2(168.5f + i * 56f, 0f);
                starRect.sizeDelta = new Vector2(54f, 54f);

                Image image = starObject.GetComponent<Image>();
                image.raycastTarget = false;
                ApplyStageStarSprite(image);
                stageStarIcons.Add(image);
            }

            stageStarInfoButton = CreateAnchoredButton(
                stageStarsRoot,
                "StarInfoButton",
                "?",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f),
                new Vector2(58f, 54f));
            PolishStageActionButton(stageStarInfoButton, false);
            stageStarInfoButton.onClick.AddListener(ShowStarConditionsOverlay);
            stageStarsRoot.gameObject.SetActive(false);
        }

        private static void AddMobileTitleAligner(
            GameObject host,
            string screenName,
            RectTransform titleRoot,
            params RectTransform[] followRoots)
        {
            AddMobileTitleAligner(host, screenName, titleRoot, false, followRoots);
        }

        private static void AddMobileTitleAligner(
            GameObject host,
            string screenName,
            RectTransform titleRoot,
            bool previewInEditor,
            params RectTransform[] followRoots)
        {
            if (host == null || titleRoot == null)
            {
                return;
            }

            MobileTitleSectionAligner aligner = host.GetComponent<MobileTitleSectionAligner>()
                ?? host.AddComponent<MobileTitleSectionAligner>();
            aligner.Configure(screenName, titleRoot, followRoots, enableEditorPreview: previewInEditor);
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
                case GameLaunchMode.PracticeRanking:
                    BuildPracticeRankingActions();
                    break;
                case GameLaunchMode.RankingChallenge:
                    BuildRankingActions();
                    break;
                default:
                    BuildPracticeActions();
                    break;
            }

            RefreshInteractionModeButton();
            if (GameLaunchContext.ConsumeGameScreenFadeInRequest())
            {
                SetOverlayContentVisible(false);
                StartCoroutine(PlayInitialScreenFadeIn());
            }
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
            if (launchMode == GameLaunchMode.StagePlay
                || IsRankingStyleMode())
            {
                string spriteKey = orbitController.CurrentMode == CubeInteractionMode.View
                    ? "solve"
                    : "view";
                if (interactionSpriteKey != spriteKey)
                {
                    interactionSpriteKey = spriteKey;
                    ApplyStageButtonSprite(interactionModeButton, spriteKey, false);
                }
            }
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
            Button scramble = CreateBottomButton(actionRoot, "ScrambleButton", "Scramble", 0f, 44f, 300f);
            Button undo = CreateBottomButton(actionRoot, "UndoButton", "Undo", 0.5f, 44f, 240f);
            Button menu = CreateBottomButton(actionRoot, "MenuButton", "Main Menu", 1f, 44f, 300f);
            Button retry = CreateBottomButton(actionRoot, "RetryButton", "Retry", 0.5f, 130f, 240f);

            scramble.onClick.AddListener(() => quickPlay?.StartNewGame());
            undo.onClick.AddListener(() => cubeController?.Undo());
            retry.onClick.AddListener(() => quickPlay?.Retry());
            menu.onClick.AddListener(SceneLoader.LoadMainMenu);
        }

        private void BuildRankingActions()
        {
            Button scramble = CreateStageBottomButton(actionRoot, "RankingScrambleButton", "Scramble", 0.18f, 178f, 330f, 112f);
            Button rewards = CreateStageBottomButton(actionRoot, "WeeklyRankingRewardsButton", "Rewards", 0.82f, 178f, 330f, 112f);
            Button rankings = CreateStageBottomButton(actionRoot, "RankingButton", "Ranking", 0.18f, 44f, 330f, 112f);
            Button menu = CreateStageBottomButton(actionRoot, "MenuButton", "Main Menu", 0.82f, 44f, 330f, 112f);

            PolishStageActionButton(scramble, false);
            PolishStageActionButton(rewards, false);
            PolishStageActionButton(rankings, false);
            PolishStageActionButton(menu, false);
            ApplyStageButtonSprite(scramble, "scramble", false);
            ApplyRankingRewardsButtonSprite(rewards);
            ApplyStageButtonSprite(rankings, "ranking", false);
            ApplyStageButtonSprite(menu, "main_menu", false);
            weeklyRankingRewardsButton = rewards;
            weeklyRankingRewardsAttention = rewards.gameObject.AddComponent<WeeklyRankingRewardsButtonAttention>();

            rankingEntryCostText = RuntimeUiFactory.CreateText(
                actionRoot,
                "RankingEntryCostText",
                "This challenge costs 50 Coins to enter.",
                24,
                TextAnchor.MiddleCenter);
            rankingEntryCostText.rectTransform.anchorMin = new Vector2(0f, 0f);
            rankingEntryCostText.rectTransform.anchorMax = new Vector2(1f, 0f);
            rankingEntryCostText.rectTransform.pivot = new Vector2(0.5f, 0f);
            rankingEntryCostText.rectTransform.anchoredPosition = new Vector2(0f, 314f);
            rankingEntryCostText.rectTransform.sizeDelta = new Vector2(-80f, 54f);
            rankingEntryCostText.fontStyle = FontStyle.Bold;
            rankingEntryCostText.color = new Color(1f, 0.90f, 0.66f, 1f);
            CasualUIStyle.ApplyTextDepth(rankingEntryCostText, false);

            scramble.onClick.AddListener(PrepareRankingChallenge);
            rankings.onClick.AddListener(ShowRankingOverlay);
            rewards.onClick.AddListener(() => _ = ShowWeeklyRankingRewardsOverlayAsync());
            menu.onClick.AddListener(SceneLoader.LoadMainMenu);
            _ = RefreshWeeklyRankingRewardAttentionAsync();
        }

        private void BuildPracticeRankingActions()
        {
            Button scramble = CreateStageBottomButton(actionRoot, "RankingScrambleButton", "Scramble", 0.18f, 178f, 330f, 112f);
            Button undo = CreateStageBottomButton(actionRoot, "UndoButton", "Undo", 0.82f, 178f, 330f, 112f);
            Button retry = CreateStageBottomButton(actionRoot, "RetryButton", "Retry", 0.18f, 44f, 330f, 112f);
            Button back = CreateStageBottomButton(actionRoot, "MenuButton", "Back", 0.82f, 44f, 330f, 112f);

            PolishStageActionButton(scramble, false);
            PolishStageActionButton(undo, false);
            PolishStageActionButton(retry, false);
            PolishStageActionButton(back, false);
            ApplyStageButtonSprite(scramble, "scramble", false);
            ApplyStageButtonSprite(undo, "undo", false);
            ApplyStageButtonSprite(retry, "retry", false);

            scramble.onClick.AddListener(PrepareRankingChallenge);
            undo.onClick.AddListener(() => cubeController?.Undo());
            retry.onClick.AddListener(PrepareRankingChallenge);
            back.onClick.AddListener(BackToSolverLearn);
        }

        private void BuildStageActions()
        {
            Button list = CreateStageBottomButton(actionRoot, "StageListButton", "Stage List", 0.18f, 32f, 360f, 118f);
            Button menu = CreateStageBottomButton(actionRoot, "MenuButton", "Main Menu", 0.82f, 32f, 360f, 118f);
            list.onClick.AddListener(() => stage?.ExitToStageList());
            menu.onClick.AddListener(SceneLoader.LoadMainMenu);

            Button retry = CreateStageBottomButton(actionRoot, "RetryButton", "Retry", 0.18f, 178f, 360f, 118f);
            retry.onClick.AddListener(() => stage?.RetryStage());

            Button undo = CreateStageBottomButton(actionRoot, "UndoButton", "Undo", 0.82f, 178f, 360f, 118f);
            undo.onClick.AddListener(() => stage?.UseUndoAssist());

            Button plus1 = CreateStageBottomButton(actionRoot, "Plus1Button", "+1\nMove", 0.14f, 318f, 202f, 210f);
            Button plus2 = CreateStageBottomButton(actionRoot, "Plus2Button", "+2\nMoves", 0.5f, 318f, 202f, 210f);
            Button plus3 = CreateStageBottomButton(actionRoot, "Plus3Button", "+3\nMoves", 0.86f, 318f, 202f, 210f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Button forceClear = CreateStageBottomButton(actionRoot, "ForceClearDebugButton", "Force\nClear", 0.5f, 462f, 260f, 86f);
            forceClear.onClick.AddListener(() => stage?.ForceClearForDebug());
#endif
            CreateCountBadge(plus1);
            CreateCountBadge(plus2);
            CreateCountBadge(plus3);
            CreateCountBadge(undo);
            ConfigureStagePowerUpContent(plus1);
            ConfigureStagePowerUpContent(plus2);
            ConfigureStagePowerUpContent(plus3);
            ConfigureStagePowerUpContent(undo);
            PolishStageActionButton(plus1, true);
            PolishStageActionButton(plus2, true);
            PolishStageActionButton(plus3, true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PolishStageActionButton(forceClear, false);
#endif
            ApplyStagePlusItemSprite(plus1, "plus1_item");
            ApplyStagePlusItemSprite(plus2, "plus2_item");
            ApplyStagePlusItemSprite(plus3, "plus3_item");
            PolishStageActionButton(undo, true);
            PolishStageActionButton(list, false);
            PolishStageActionButton(retry, false);
            PolishStageActionButton(menu, false);
            ApplyStageButtonSprite(undo, "undo", true);
            ApplyStageButtonSprite(list, "stage_list", false);
            ApplyStageButtonSprite(retry, "retry", false);
            ApplyStageButtonSprite(menu, "main_menu", false);
            plus1.onClick.AddListener(() => stage?.UseMoveItem(StageAssistItemType.MovePlus1));
            plus2.onClick.AddListener(() => stage?.UseMoveItem(StageAssistItemType.MovePlus2));
            plus3.onClick.AddListener(() => stage?.UseMoveItem(StageAssistItemType.MovePlus3));

            Button start = CreateStageBottomButton(actionRoot, "StartStageButton", "Start", 0.5f, 318f, 310f, 108f);
            start.onClick.AddListener(() => stage?.StartStage());
            start.gameObject.SetActive(false);
            targetMiniButton = null;
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
                case GameLaunchMode.PracticeRanking:
                    RefreshPracticeRanking();
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
            timerText.gameObject.SetActive(true);
            timerPill?.SetActive(true);
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
            timerText.gameObject.SetActive(true);
            timerPill?.SetActive(true);
            UpdateRankingPreviewCountdown();
            titleText.text = ranking?.Config != null
                ? $"Daily Challenge  {ranking.Config.dateUtc}"
                : "Ranking Challenge";
            timerText.text = $"Time  {FormatTime(ranking?.ElapsedTime ?? 0f)}";
            movesText.text = $"Moves  {ranking?.MoveCount ?? 0}";
            statusText.text = GetRankingStatusText();
            HideTarget();
            if (rankingEntryCostText != null)
            {
                int cost = ranking != null ? ranking.EntryCoinCost : 50;
                rankingEntryCostText.text = $"This challenge costs {cost} Coins to enter.";
                rankingEntryCostText.gameObject.SetActive(ranking == null || ranking.State == RankingChallengeState.Ready);
            }
            SetActionInteractable("WeeklyRankingRewardsButton", ranking != null);
            SetActionInteractable(
                "RankingScrambleButton",
                ranking != null
                && (ranking.State == RankingChallengeState.Ready
                    || ranking.State == RankingChallengeState.Solved));

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

        private string GetRankingStatusText()
        {
            if (ranking == null)
            {
                return string.Empty;
            }

            if (ranking.State == RankingChallengeState.Ready
                && !string.IsNullOrWhiteSpace(ranking.LastSubmitMessage)
                && ranking.LastSubmitMessage.StartsWith("Not enough coins", StringComparison.Ordinal))
            {
                return ranking.LastSubmitMessage;
            }

            return GetRankingStatus(ranking.State);
        }

        private void UpdateRankingPreviewCountdown()
        {
            if (ranking == null)
            {
                lastRankingState = null;
                rankingCountdownStarted = false;
                if (rankingCountdownText != null)
                {
                    rankingCountdownText.gameObject.SetActive(false);
                }
                return;
            }

            RankingChallengeState state = ranking.State;
            if (lastRankingState != state)
            {
                lastRankingState = state;
                if (state == RankingChallengeState.Previewing)
                {
                    rankingPreviewCountdown = 10f;
                    rankingCountdownStarted = true;
                    rankingStartTextShown = false;
                }
                else
                {
                    rankingCountdownStarted = false;
                    rankingStartTextShown = false;
                    if (rankingCountdownText != null)
                    {
                        rankingCountdownText.gameObject.SetActive(false);
                    }
                }
            }

            if (state != RankingChallengeState.Previewing || !rankingCountdownStarted)
            {
                return;
            }

            rankingPreviewCountdown = rankingStartTextShown
                ? rankingPreviewCountdown - Time.deltaTime
                : Mathf.Max(0f, rankingPreviewCountdown - Time.deltaTime);
            if (rankingCountdownText != null)
            {
                int displaySeconds = Mathf.CeilToInt(rankingPreviewCountdown);
                rankingCountdownText.text = displaySeconds > 0 ? displaySeconds.ToString() : "Start!";
                rankingCountdownText.gameObject.SetActive(true);
            }

            if (rankingPreviewCountdown <= 0f && !rankingStartTextShown)
            {
                rankingStartTextShown = true;
                rankingPreviewCountdown = -0.35f;
                return;
            }

            if (rankingStartTextShown && rankingPreviewCountdown <= -0.35f)
            {
                rankingCountdownStarted = false;
                StartRankingChallenge();
            }
        }

        private void RefreshPracticeRanking()
        {
            timerText.gameObject.SetActive(true);
            timerPill?.SetActive(true);
            titleText.text = "Practice";
            timerText.text = $"Time  {FormatTime(ranking?.ElapsedTime ?? 0f)}";
            movesText.text = $"Moves  {ranking?.MoveCount ?? 0}";
            statusText.text = ranking != null ? GetPracticeRankingStatus(ranking.State) : string.Empty;
            HideTarget();

            if (ranking != null
                && ranking.State == RankingChallengeState.Previewing
                && cubeController != null
                && !cubeController.IsBusy)
            {
                StartRankingChallenge();
            }

            bool busy = ranking != null && ranking.State == RankingChallengeState.Scrambling;
            SetActionInteractable(
                "RankingScrambleButton",
                ranking != null
                && !busy
                && ranking.State != RankingChallengeState.Playing);
            SetActionInteractable(
                "UndoButton",
                ranking != null
                && ranking.State == RankingChallengeState.Playing
                && cubeController != null
                && cubeController.MoveHistory.CanUndo);
            SetActionInteractable(
                "RetryButton",
                ranking != null && !busy);
        }

        private void RefreshStage()
        {
            if (stage == null || stage.CurrentStage == null)
            {
                return;
            }

            titleText.text = $"Stage {GetStageDisplayNumber(stage.CurrentStage)}  {stage.CurrentStage.title}";
            timerText.text = string.Empty;
            timerText.gameObject.SetActive(false);
            timerPill?.SetActive(false);
            movesText.text = $"Moves  {stage.Runtime.currentMoves}/{stage.Runtime.moveLimit}";
            statusText.text = string.Empty;
            statusText.gameObject.SetActive(false);
            RefreshStageStarsStatus();

            bool reverse = stage.IsReverseTargetStage;
            bool intro = stage.State == StagePlayState.TargetIntro;
            bool playing = stage.State == StagePlayState.Playing;
            bool popup = stage.State == StagePlayState.TargetPreviewPopup;

            if (reverse && intro)
            {
                TryAutoStartReverseTargetStage();
                return;
            }

            SetActionVisible("StartStageButton", false);
            SetActionVisible("TargetMiniButton", false);
            SetActionVisible("UndoButton", playing);
            SetActionVisible("Plus1Button", playing);
            SetActionVisible("Plus2Button", playing);
            SetActionVisible("Plus3Button", playing);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SetActionVisible("ForceClearDebugButton", !intro && !popup && stage.State != StagePlayState.Cleared);
#endif
            SetActionVisible("RetryButton", !intro && !popup && stage.State != StagePlayState.Cleared);
            RefreshTargetToggleButton(reverse, playing, popup);
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
                "Undo");
            SetActionBadge(
                "UndoButton",
                $"{stage.AssistState?.freeUndoRemaining ?? 0}/{stage.InventoryStore?.UndoItems ?? 0}");
            SetActionLabel(
                "Plus1Button",
                "+1\nMove");
            SetActionBadge(
                "Plus1Button",
                $"{stage.InventoryStore?.GetCount(StageAssistItemType.MovePlus1) ?? 0}");
            SetActionLabel(
                "Plus2Button",
                "+2\nMoves");
            SetActionBadge(
                "Plus2Button",
                $"{stage.InventoryStore?.GetCount(StageAssistItemType.MovePlus2) ?? 0}");
            SetActionLabel(
                "Plus3Button",
                "+3\nMoves");
            SetActionBadge(
                "Plus3Button",
                $"{stage.InventoryStore?.GetCount(StageAssistItemType.MovePlus3) ?? 0}");
            if (reverse && popup)
            {
                ShowTarget("popup");
            }
            else
            {
                HideTarget();
            }

            if (stage.State == StagePlayState.Cleared && !overlayRoot.activeSelf)
            {
                ShowStageClearOverlay();
            }
            else if (stage.State == StagePlayState.Failed && !overlayRoot.activeSelf)
            {
                ShowStageGameOverOverlay();
            }
        }

        private void TryAutoStartReverseTargetStage()
        {
            if (stage?.CurrentStage == null)
            {
                return;
            }

            string stageId = stage.CurrentStage.stageId;
            if (autoStartedReverseTargetStageId == stageId)
            {
                RefreshTargetToggleButton(true, false, false);
                HideTarget();
                return;
            }

            autoStartedReverseTargetStageId = stageId;
            stage.StartStage();
            if (stage.State == StagePlayState.Playing)
            {
                stage.SetTargetPreviewOpen(true);
                previewMode = string.Empty;
            }
        }

        private void RefreshTargetToggleButton(bool reverse, bool playing, bool popup)
        {
            if (targetToggleButton == null)
            {
                return;
            }

            bool visible = reverse && (playing || popup);
            if (targetToggleButton.gameObject.activeSelf != visible)
            {
                targetToggleButton.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            SetButtonLabel(targetToggleButton, popup ? "Play" : "Target");
        }

        private static int GetStageDisplayNumber(StageData stage)
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
                return Mathf.Max(1, stage.stageNumber - StagePackGenerator.NormalStageCount);
            }

            if (stage.stageType == StageType.InfinityStage)
            {
                return Mathf.Max(1, stage.stageNumber - (StagePackGenerator.NormalStageCount + StagePackGenerator.HardStageCount));
            }

            return stage.stageNumber;
        }

        private static string FormatOrdinal(int rank)
        {
            if (rank <= 0)
            {
                return "-";
            }

            int lastTwo = rank % 100;
            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return $"{rank}th";
            }

            switch (rank % 10)
            {
                case 1:
                    return $"{rank}st";
                case 2:
                    return $"{rank}nd";
                case 3:
                    return $"{rank}rd";
                default:
                    return $"{rank}th";
            }
        }

        private static string FormatWeeklyReward(WeeklyRankingRewardDto reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            string type = string.Equals(reward.rewardType, "gem", StringComparison.OrdinalIgnoreCase)
                ? "Gems"
                : "Coins";
            return $"{reward.rewardAmount} {type}";
        }

        private static bool IsTopThreeWeeklyReward(WeeklyRankingRewardDto reward)
        {
            return reward != null
                && reward.exists
                && reward.rank >= 1
                && reward.rank <= 3
                && reward.rewardAmount > 0;
        }

        private static bool IsClaimableTopThreeWeeklyReward(WeeklyRankingRewardDto reward)
        {
            return IsTopThreeWeeklyReward(reward) && !reward.claimed;
        }

        private static string GetWeeklyRewardSeenKey(WeeklyRankingRewardDto reward)
        {
            if (reward == null)
            {
                return "weekly_ranking_reward_seen_unknown";
            }

            string player = string.IsNullOrWhiteSpace(reward.playerId) ? "unknown" : reward.playerId.Trim();
            string week = string.IsNullOrWhiteSpace(reward.weekStartKst) ? "unknown_week" : reward.weekStartKst.Trim();
            return $"weekly_ranking_reward_seen_{player}_{week}_{reward.rank}_{reward.rewardType}_{reward.rewardAmount}";
        }

        private static bool HasSeenWeeklyRewardPopup(WeeklyRankingRewardDto reward)
        {
            return PlayerPrefs.GetInt(GetWeeklyRewardSeenKey(reward), 0) == 1;
        }

        private static void MarkWeeklyRewardPopupSeen(WeeklyRankingRewardDto reward)
        {
            if (reward == null)
            {
                return;
            }

            PlayerPrefs.SetInt(GetWeeklyRewardSeenKey(reward), 1);
            PlayerPrefs.Save();
        }

        private void RequestNextStageViaStageMap()
        {
            if (isTransitioningToNextStage)
            {
                return;
            }

            if (stage == null || stage.CurrentStage == null)
            {
                return;
            }

            var nextStage = stage.PeekNextPlayableStage();
            if (nextStage == null)
            {
                stage.ExitToStageList();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageNextButton] fromStage={stage.CurrentStage.stageNumber} toStage={nextStage.stageNumber} currentProgressStage={stage.CurrentProgressStageNumber} isReplayChain={IsReplayClearStage(stage.CurrentStage.stageNumber)}");
#endif
            string fromStageId = stage.CurrentStage.stageId;
            string toStageId = nextStage.stageId;
            StageInterstitialPolicy.TryShowBeforeNextStage(stage.CurrentStage, () =>
            {
                StartCoroutine(PlayNextStageTransitionSequence(fromStageId, toStageId));
            });
        }

        private IEnumerator PlayNextStageTransitionSequence(string fromStageId, string toStageId)
        {
            isTransitioningToNextStage = true;
            if (overlayPrimaryButton != null)
            {
                overlayPrimaryButton.interactable = false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageNextTransition] Start fromStage={fromStageId} toStage={toStageId}");
#endif
            SetOverlayContentVisible(false);
            yield return FadeOverlayAlpha(overlayBackground != null ? overlayBackground.color.a : 0.82f, 0f, clearPopupFadeDuration);
            SetOverlayContentVisible(false);
            yield return FadeOverlayAlpha(0f, 1f, screenFadeDuration);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[StageNextTransition] ShowStageList");
#endif
            GameLaunchContext.RequestStageAdvanceOnMainMenu(fromStageId, toStageId, true);
            SceneLoader.LoadMainMenu();
        }

        private void SetOverlayContentVisible(bool visible)
        {
            if (rankingPopupRoot != null)
            {
                rankingPopupRoot.SetActive(false);
            }

            if (weeklyRewardsPopupRoot != null)
            {
                weeklyRewardsPopupRoot.SetActive(false);
            }

            if (overlayPanel != null)
            {
                overlayPanel.gameObject.SetActive(visible);
            }

            if (overlayTitle != null)
            {
                overlayTitle.gameObject.SetActive(visible);
            }

            if (overlayBody != null)
            {
                overlayBody.gameObject.SetActive(visible);
            }

            if (overlayPrimaryButton != null)
            {
                overlayPrimaryButton.gameObject.SetActive(visible);
            }

            if (overlaySecondaryButton != null)
            {
                overlaySecondaryButton.gameObject.SetActive(false);
            }

            if (overlayTertiaryButton != null)
            {
                overlayTertiaryButton.gameObject.SetActive(false);
            }

            if (overlayCloseButton != null)
            {
                overlayCloseButton.gameObject.SetActive(false);
            }

            SetOverlayStarsVisible(0);
            if (starConditionsRoot != null)
            {
                starConditionsRoot.gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeOverlayAlpha(float fromAlpha, float toAlpha, float duration)
        {
            if (overlayRoot == null || overlayBackground == null)
            {
                yield break;
            }

            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            Color color = overlayBackground.color;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                overlayBackground.color = color;
                yield return null;
            }

            color.a = toAlpha;
            overlayBackground.color = color;
        }

        private IEnumerator PlayInitialScreenFadeIn()
        {
            yield return FadeOverlayAlpha(1f, 0f, screenFadeDuration);
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[StageNextTransition] Complete");
#endif
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
            Transform gameplayView = cubeController != null ? cubeController.ViewRoot : null;
            Vector3 position = gameplayView != null ? gameplayView.position : Vector3.zero;
            Vector3 eulerAngles = gameplayView != null
                ? gameplayView.rotation.eulerAngles
                : new Vector3(24f, -35f, 0f);
            float scale = gameplayView != null
                ? gameplayView.lossyScale.x
                : 0.72f;
            targetPreview.Show(stage.TargetState, position, eulerAngles, scale, true);
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

        private void ToggleTargetView()
        {
            if (stage == null || !stage.IsReverseTargetStage)
            {
                return;
            }

            if (stage.State == StagePlayState.TargetPreviewPopup)
            {
                CloseTarget();
                return;
            }

            OpenTarget();
        }

        private void PrepareRankingChallenge()
        {
            if (ranking == null || orbitController == null)
            {
                return;
            }

            rankingCountdownStarted = false;
            lastRankingState = null;
            if (rankingCountdownText != null)
            {
                rankingCountdownText.gameObject.SetActive(false);
            }
            orbitController.SetViewMode();
            bool isPaidRankingChallenge = launchMode == GameLaunchMode.RankingChallenge;
            ranking.PrepareChallenge(isPaidRankingChallenge, isPaidRankingChallenge);
        }

        private void StartRankingChallenge()
        {
            if (ranking == null || orbitController == null)
            {
                return;
            }

            orbitController.SetSolveMode();
            rankingCountdownStarted = false;
            if (rankingCountdownText != null)
            {
                rankingCountdownText.gameObject.SetActive(false);
            }
            ranking.StartChallenge();
        }

        private void CloseTarget()
        {
            targetPreview?.Hide();
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

        private async void ShowRankingOverlay()
        {
            if (ranking == null)
            {
                return;
            }

            if (rankingPopupRoot == null)
            {
                return;
            }

            SetOverlayShade(0.82f);
            if (weeklyRewardsPopupRoot != null)
            {
                weeklyRewardsPopupRoot.SetActive(false);
            }

            SetOverlayContentVisible(false);
            overlayTitle.gameObject.SetActive(false);
            overlayBody.gameObject.SetActive(false);
            overlayPrimaryButton.gameObject.SetActive(false);
            overlayCloseButton.gameObject.SetActive(false);
            rankingPopupRoot.SetActive(true);
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            await LoadRankingTabAsync(true);
        }

        private async System.Threading.Tasks.Task ShowWeeklyRankingRewardsOverlayAsync()
        {
            if (ranking == null)
            {
                return;
            }

            SetOverlayShade(0.82f);
            SetOverlayContentVisible(false);
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            if (weeklyRewardsPopupRoot != null)
            {
                weeklyRewardsPopupRoot.SetActive(true);
            }

            SetWeeklyRewardsPopupText(
                "Weekly Ranking Rewards",
                "Weekly rankings run from Monday to Sunday.\nRewards are distributed every Monday at 00:00 KST.",
                "Loading weekly rewards...");
            SetWeeklyRewardRowsFocus(0);
            ConfigureWeeklyRewardsPrimaryButton("OK", false, HideOverlay);

            pendingWeeklyRankingReward = await ranking.FetchWeeklyRankingRewardAsync();
            if (IsClaimableTopThreeWeeklyReward(pendingWeeklyRankingReward) && !HasSeenWeeklyRewardPopup(pendingWeeklyRankingReward))
            {
                ShowWeeklyRankingClaimPopup(pendingWeeklyRankingReward);
                return;
            }

            if (pendingWeeklyRankingReward != null
                && pendingWeeklyRankingReward.exists
                && pendingWeeklyRankingReward.rank >= 4
                && !HasSeenWeeklyRewardPopup(pendingWeeklyRankingReward))
            {
                ShowWeeklyRankingEncouragementPopup(pendingWeeklyRankingReward);
                return;
            }

            WeeklyRankingRewardInfoResponseDto info = await ranking.FetchWeeklyRankingRewardInfoAsync();
            string description = info != null && !string.IsNullOrWhiteSpace(info.description)
                ? info.description
                : "Weekly rankings run from Monday to Sunday.\nRewards are distributed every Monday at 00:00 KST.";
            SetWeeklyRewardsPopupText(
                "Weekly Ranking Rewards",
                description,
                "Place in the weekly Top 3 to earn these rewards.");
            SetWeeklyRewardRowsFocus(0);
            ConfigureWeeklyRewardsPrimaryButton("OK", true, HideOverlay);
        }

        private void ShowWeeklyRankingClaimPopup(WeeklyRankingRewardDto reward)
        {
            string rankText = FormatOrdinal(reward.rank);
            string rewardText = FormatWeeklyReward(reward);
            SetWeeklyRewardsPopupText(
                "Weekly Ranking Rewards",
                $"Congratulations!\nYou finished {rankText} this week!",
                $"Reward: {rewardText}");
            SetWeeklyRewardRowsFocus(reward.rank);
            ConfigureWeeklyRewardsPrimaryButton("Claim", true, () => _ = ClaimWeeklyRankingRewardAsync());
        }

        private void ShowWeeklyRankingEncouragementPopup(WeeklyRankingRewardDto reward)
        {
            string rankText = FormatOrdinal(reward.rank);
            SetWeeklyRewardsPopupText(
                "Weekly Ranking Rewards",
                $"Nice try!\nYou finished {rankText} this week.",
                "Reach the Top 3 next week to earn rewards!");
            SetWeeklyRewardRowsFocus(0);
            ConfigureWeeklyRewardsPrimaryButton("OK", true, () =>
            {
                MarkWeeklyRewardPopupSeen(reward);
                HideOverlay();
            });
        }

        private async System.Threading.Tasks.Task ClaimWeeklyRankingRewardAsync()
        {
            if (ranking == null || pendingWeeklyRankingReward == null || weeklyRewardClaimInProgress)
            {
                return;
            }

            weeklyRewardClaimInProgress = true;
            if (weeklyRewardsPrimaryButton != null)
            {
                weeklyRewardsPrimaryButton.interactable = false;
            }

            WeeklyRankingRewardClaimResponseDto response = await ranking.ClaimWeeklyRankingRewardAsync(pendingWeeklyRankingReward);
            weeklyRewardClaimInProgress = false;
            if (response != null && response.success && response.claimed)
            {
                MarkWeeklyRewardPopupSeen(pendingWeeklyRankingReward);
                pendingWeeklyRankingReward = null;
                SetWeeklyRewardsPopupText(
                    "Claimed!",
                    "Weekly rankings run from Monday to Sunday.\nRewards are distributed every Monday at 00:00 KST.",
                    "Your weekly ranking reward has been added.");
                SetWeeklyRewardRowsFocus(0);
                ConfigureWeeklyRewardsPrimaryButton("OK", true, HideOverlay);
                if (weeklyRankingRewardsAttention != null)
                {
                    weeklyRankingRewardsAttention.SetActive(false);
                }

                _ = RefreshWeeklyRankingRewardAttentionAsync();
                return;
            }

            SetWeeklyRewardsPopupText(
                "Weekly Ranking Rewards",
                "Weekly rankings run from Monday to Sunday.\nRewards are distributed every Monday at 00:00 KST.",
                response != null && !string.IsNullOrWhiteSpace(response.message)
                ? response.message
                : "Reward claim failed.");
            SetWeeklyRewardRowsFocus(0);
            ConfigureWeeklyRewardsPrimaryButton("OK", true, HideOverlay);
        }

        private void SetWeeklyRewardRowsFocus(int focusRank)
        {
            if (weeklyRewardsRowsRoot == null)
            {
                return;
            }

            for (int i = 0; i < weeklyRewardsRowsRoot.childCount; i++)
            {
                int rank = i + 1;
                RectTransform row = weeklyRewardsRowsRoot.GetChild(i) as RectTransform;
                if (row == null)
                {
                    continue;
                }

                bool show = focusRank <= 0 || rank == focusRank;
                row.gameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                if (focusRank > 0)
                {
                    row.anchorMin = new Vector2(0f, 0.355f);
                    row.anchorMax = new Vector2(1f, 0.645f);
                }
                else
                {
                    float yMin = rank == 1 ? 0.675f : rank == 2 ? 0.340f : 0.005f;
                    row.anchorMin = new Vector2(0f, yMin);
                    row.anchorMax = new Vector2(1f, yMin + 0.285f);
                }

                row.offsetMin = Vector2.zero;
                row.offsetMax = Vector2.zero;
            }
        }

        private void SetWeeklyRewardsPopupText(string title, string description, string message)
        {
            if (weeklyRewardsTitleText != null)
            {
                weeklyRewardsTitleText.text = title;
            }

            if (weeklyRewardsDescriptionText != null)
            {
                weeklyRewardsDescriptionText.text = description;
            }

            if (weeklyRewardsMessageText != null)
            {
                weeklyRewardsMessageText.text = message;
            }
        }

        private void ConfigureWeeklyRewardsPrimaryButton(
            string label,
            bool interactable,
            UnityEngine.Events.UnityAction action)
        {
            if (weeklyRewardsPrimaryButton == null)
            {
                return;
            }

            SetButtonLabel(weeklyRewardsPrimaryButton, label);
            weeklyRewardsPrimaryButton.interactable = interactable;
            weeklyRewardsPrimaryButton.onClick.RemoveAllListeners();
            if (action != null)
            {
                weeklyRewardsPrimaryButton.onClick.AddListener(action);
            }
        }

        private async System.Threading.Tasks.Task RefreshWeeklyRankingRewardAttentionAsync()
        {
            if (ranking == null || weeklyRankingRewardsAttention == null)
            {
                return;
            }

            int token = ++weeklyRewardCheckToken;
            WeeklyRankingRewardDto reward = await ranking.FetchWeeklyRankingRewardAsync();
            if (token != weeklyRewardCheckToken || weeklyRankingRewardsAttention == null)
            {
                return;
            }

            bool hasClaimable = IsClaimableTopThreeWeeklyReward(reward);
            weeklyRankingRewardsAttention.SetActive(hasClaimable);
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

            overlayPanel = CasualUIFactory.CreatePanel(
                rect,
                "ResultPopupPanel",
                new Color(0.011f, 0.043f, 0.070f, 0.985f),
                30);
            overlayPanel.anchorMin = new Vector2(0.115f, 0.245f);
            overlayPanel.anchorMax = new Vector2(0.885f, 0.815f);
            overlayPanel.offsetMin = Vector2.zero;
            overlayPanel.offsetMax = Vector2.zero;
            Outline panelOutline = overlayPanel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(1f, 0.640f, 0.150f, 0.96f);
            panelOutline.effectDistance = new Vector2(5f, -5f);
            Shadow panelShadow = overlayPanel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            panelShadow.effectDistance = new Vector2(0f, -12f);

            overlayTitle = RuntimeUiFactory.CreateText(rect, "Title", string.Empty, 40, TextAnchor.MiddleCenter);
            SetOverlayTextRect(overlayTitle.rectTransform, 0.68f, 0.82f);
            overlayBody = RuntimeUiFactory.CreateText(rect, "Body", string.Empty, 27, TextAnchor.UpperCenter);
            SetOverlayTextRect(overlayBody.rectTransform, 0.31f, 0.67f);

            overlayPrimaryButton = CreateAnchoredButton(
                rect, "PrimaryButton", "Continue",
                new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f),
                Vector2.zero, new Vector2(340f, 82f));
            overlaySecondaryButton = CreateAnchoredButton(
                rect, "SecondaryButton", "Close",
                new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f),
                new Vector2(160f, 0f), new Vector2(260f, 82f));
            overlaySecondaryButton.gameObject.SetActive(false);
            overlayTertiaryButton = CreateAnchoredButton(
                rect, "TertiaryButton", "Close",
                new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f),
                new Vector2(260f, 0f), new Vector2(230f, 78f));
            overlayTertiaryButton.gameObject.SetActive(false);
            BuildOverlayStars(rect);
            BuildStarConditionsContent(rect);
            overlayCloseButton = CreateAnchoredButton(
                rect, "CloseButton", "X",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -70f), new Vector2(72f, 64f));
            overlayCloseButton.onClick.AddListener(HideOverlay);
            BuildRankingPopup(rect);
            BuildWeeklyRewardsPopup(rect);
            overlayRoot.SetActive(false);
        }

        private void BuildOverlayStars(RectTransform parent)
        {
            GameObject rootObject = new GameObject("ResultStars", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            overlayStarsRoot = rootObject.GetComponent<RectTransform>();
            overlayStarsRoot.anchorMin = new Vector2(0.18f, 0.535f);
            overlayStarsRoot.anchorMax = new Vector2(0.82f, 0.620f);
            overlayStarsRoot.offsetMin = Vector2.zero;
            overlayStarsRoot.offsetMax = Vector2.zero;

            Text label = RuntimeUiFactory.CreateText(overlayStarsRoot, "Label", "Stars:", 28, TextAnchor.MiddleRight);
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.36f, 1f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            CasualUIStyle.ApplyTextDepth(label, true);

            for (int i = 0; i < 3; i++)
            {
                GameObject starObject = new GameObject($"Star{i + 1}", typeof(RectTransform), typeof(Image));
                starObject.transform.SetParent(overlayStarsRoot, false);
                RectTransform starRect = starObject.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.42f, 0.5f);
                starRect.anchorMax = new Vector2(0.42f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                starRect.anchoredPosition = new Vector2(i * 68f, 0f);
                starRect.sizeDelta = new Vector2(64f, 64f);

                Image image = starObject.GetComponent<Image>();
                image.raycastTarget = false;
                ApplyStageStarSprite(image);
                overlayStarIcons.Add(image);
            }

            overlayStarsRoot.gameObject.SetActive(false);
        }

        private void BuildStarConditionsContent(RectTransform parent)
        {
            GameObject rootObject = new GameObject("StarConditionsRows", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            starConditionsRoot = rootObject.GetComponent<RectTransform>();
            starConditionsRoot.anchorMin = new Vector2(0.130f, 0.350f);
            starConditionsRoot.anchorMax = new Vector2(0.870f, 0.680f);
            starConditionsRoot.offsetMin = Vector2.zero;
            starConditionsRoot.offsetMax = Vector2.zero;
            starConditionsRoot.gameObject.SetActive(false);
        }

        private void BuildWeeklyRewardsPopup(RectTransform parent)
        {
            weeklyRewardsPopupRoot = new GameObject("WeeklyRankingRewardsPopup", typeof(RectTransform));
            weeklyRewardsPopupRoot.transform.SetParent(parent, false);
            RectTransform root = weeklyRewardsPopupRoot.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            RectTransform panel = CasualUIFactory.CreatePanel(
                root,
                "WeeklyRewardsPanel",
                HexColor(0x07, 0x1A, 0x34, 0xFE),
                30);
            panel.anchorMin = new Vector2(0.070f, 0.205f);
            panel.anchorMax = new Vector2(0.930f, 0.790f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = HexColor(0xE3, 0xA2, 0x1A, 0xF5);
            panelOutline.effectDistance = new Vector2(5f, -5f);
            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            panelShadow.effectDistance = new Vector2(0f, -12f);

            weeklyRewardsTitleText = RuntimeUiFactory.CreateText(
                panel,
                "Title",
                "Weekly Ranking Rewards",
                43,
                TextAnchor.MiddleCenter);
            weeklyRewardsTitleText.fontStyle = FontStyle.Bold;
            weeklyRewardsTitleText.color = HexColor(0xFF, 0xD1, 0x5A);
            weeklyRewardsTitleText.rectTransform.anchorMin = new Vector2(0.13f, 0.845f);
            weeklyRewardsTitleText.rectTransform.anchorMax = new Vector2(0.87f, 0.965f);
            weeklyRewardsTitleText.rectTransform.offsetMin = Vector2.zero;
            weeklyRewardsTitleText.rectTransform.offsetMax = Vector2.zero;
            weeklyRewardsTitleText.resizeTextForBestFit = true;
            weeklyRewardsTitleText.resizeTextMinSize = 30;
            weeklyRewardsTitleText.resizeTextMaxSize = 43;
            CasualUIStyle.ApplyTextDepth(weeklyRewardsTitleText, true);

            weeklyRewardsCloseButton = CreateAnchoredButton(
                panel,
                "WeeklyCloseButton",
                "X",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-34f, -34f),
                new Vector2(74f, 74f));
            PolishRankingCloseButton(weeklyRewardsCloseButton);
            weeklyRewardsCloseButton.onClick.AddListener(HideOverlay);

            weeklyRewardsDescriptionText = RuntimeUiFactory.CreateText(
                panel,
                "Description",
                string.Empty,
                22,
                TextAnchor.MiddleCenter);
            weeklyRewardsDescriptionText.color = HexColor(0xE8, 0xEE, 0xF7, 0xF5);
            weeklyRewardsDescriptionText.rectTransform.anchorMin = new Vector2(0.08f, 0.712f);
            weeklyRewardsDescriptionText.rectTransform.anchorMax = new Vector2(0.92f, 0.825f);
            weeklyRewardsDescriptionText.rectTransform.offsetMin = Vector2.zero;
            weeklyRewardsDescriptionText.rectTransform.offsetMax = Vector2.zero;
            weeklyRewardsDescriptionText.resizeTextForBestFit = true;
            weeklyRewardsDescriptionText.resizeTextMinSize = 17;
            weeklyRewardsDescriptionText.resizeTextMaxSize = 22;
            CasualUIStyle.ApplyTextDepth(weeklyRewardsDescriptionText, false);

            weeklyRewardsRowsRoot = new GameObject("RewardRows", typeof(RectTransform)).GetComponent<RectTransform>();
            weeklyRewardsRowsRoot.transform.SetParent(panel, false);
            weeklyRewardsRowsRoot.anchorMin = new Vector2(0.065f, 0.250f);
            weeklyRewardsRowsRoot.anchorMax = new Vector2(0.935f, 0.690f);
            weeklyRewardsRowsRoot.offsetMin = Vector2.zero;
            weeklyRewardsRowsRoot.offsetMax = Vector2.zero;

            CreateWeeklyRewardRow(weeklyRewardsRowsRoot, 1, "1st Place", "15", "Gems", "gem", 0.675f);
            CreateWeeklyRewardRow(weeklyRewardsRowsRoot, 2, "2nd Place", "10", "Gems", "gem", 0.340f);
            CreateWeeklyRewardRow(weeklyRewardsRowsRoot, 3, "3rd Place", "100", "Coins", "coin", 0.005f);

            weeklyRewardsMessageText = RuntimeUiFactory.CreateText(
                panel,
                "Message",
                string.Empty,
                24,
                TextAnchor.MiddleCenter);
            weeklyRewardsMessageText.color = HexColor(0xE8, 0xEE, 0xF7);
            weeklyRewardsMessageText.rectTransform.anchorMin = new Vector2(0.08f, 0.180f);
            weeklyRewardsMessageText.rectTransform.anchorMax = new Vector2(0.92f, 0.240f);
            weeklyRewardsMessageText.rectTransform.offsetMin = Vector2.zero;
            weeklyRewardsMessageText.rectTransform.offsetMax = Vector2.zero;
            weeklyRewardsMessageText.resizeTextForBestFit = true;
            weeklyRewardsMessageText.resizeTextMinSize = 17;
            weeklyRewardsMessageText.resizeTextMaxSize = 24;
            CasualUIStyle.ApplyTextDepth(weeklyRewardsMessageText, false);

            weeklyRewardsPrimaryButton = CreateAnchoredButton(
                panel,
                "WeeklyClaimButton",
                "OK",
                new Vector2(0.5f, 0.105f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(300f, 78f));
            weeklyRewardsPrimaryButton.onClick.AddListener(HideOverlay);

            weeklyRewardsPopupRoot.SetActive(false);
        }

        private static void CreateWeeklyRewardRow(
            RectTransform parent,
            int rank,
            string placeText,
            string amountText,
            string rewardText,
            string rewardIconKey,
            float yMin)
        {
            Color rowColor;
            Color highlightColor;
            Color borderColor;
            Color shadeColor;
            switch (rank)
            {
                case 1:
                    rowColor = HexColor(0xA8, 0x6A, 0x00, 0xFC);
                    highlightColor = HexColor(0xD8, 0x9A, 0x1E, 0x54);
                    borderColor = HexColor(0xFF, 0xD1, 0x5A, 0xE6);
                    shadeColor = HexColor(0x7A, 0x4C, 0x00, 0x70);
                    break;
                case 2:
                    rowColor = HexColor(0x5F, 0x71, 0x87, 0xFC);
                    highlightColor = HexColor(0x91, 0xA4, 0xBC, 0x50);
                    borderColor = HexColor(0xE4, 0xEC, 0xF5, 0xD9);
                    shadeColor = HexColor(0x43, 0x52, 0x65, 0x70);
                    break;
                default:
                    rowColor = HexColor(0x8B, 0x4E, 0x2A, 0xFC);
                    highlightColor = HexColor(0xC4, 0x7A, 0x45, 0x54);
                    borderColor = HexColor(0xF0, 0xB0, 0x7A, 0xD9);
                    shadeColor = HexColor(0x64, 0x35, 0x1B, 0x70);
                    break;
            }

            RectTransform row = CasualUIFactory.CreatePanel(
                parent,
                $"RewardRow{rank}",
                rowColor,
                22);
            row.anchorMin = new Vector2(0f, yMin);
            row.anchorMax = new Vector2(1f, yMin + 0.285f);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            AddRowColorBand(row, "TopHighlight", new Vector2(0f, 0.64f), Vector2.one, highlightColor, 20);
            AddRowColorBand(row, "BottomShade", Vector2.zero, new Vector2(1f, 0.34f), shadeColor, 18);

            Sprite rankSprite = CasualIconFactory.LoadUiSprite($"UI/Records/world_rank_{rank}");
            if (rankSprite != null)
            {
                GameObject rankObject = new GameObject("RankIcon", typeof(RectTransform), typeof(Image));
                rankObject.transform.SetParent(row, false);
                RectTransform rankRect = rankObject.GetComponent<RectTransform>();
                rankRect.anchorMin = new Vector2(0f, 0.5f);
                rankRect.anchorMax = new Vector2(0f, 0.5f);
                rankRect.pivot = new Vector2(0.5f, 0.5f);
                rankRect.anchoredPosition = new Vector2(72f, 0f);
                rankRect.sizeDelta = new Vector2(82f, 82f);
                Image rankImage = rankObject.GetComponent<Image>();
                rankImage.sprite = rankSprite;
                rankImage.type = Image.Type.Simple;
                rankImage.preserveAspect = true;
                rankImage.color = Color.white;
                rankImage.raycastTarget = false;
            }

            Text place = RuntimeUiFactory.CreateText(row, "Place", placeText, 30, TextAnchor.MiddleLeft);
            place.fontStyle = FontStyle.Bold;
            place.color = HexColor(0xFF, 0xF4, 0xD6);
            place.rectTransform.anchorMin = new Vector2(0.215f, 0f);
            place.rectTransform.anchorMax = new Vector2(0.555f, 1f);
            place.rectTransform.offsetMin = Vector2.zero;
            place.rectTransform.offsetMax = Vector2.zero;
            place.resizeTextForBestFit = true;
            place.resizeTextMinSize = 20;
            place.resizeTextMaxSize = 30;
            CasualUIStyle.ApplyTextDepth(place, true);

            Sprite rewardSprite = CasualIconFactory.LoadMainMenuKitSprite($"Icons/{rewardIconKey}");
            if (rewardSprite != null)
            {
                GameObject rewardIconObject = new GameObject("RewardIcon", typeof(RectTransform), typeof(Image));
                rewardIconObject.transform.SetParent(row, false);
                RectTransform rewardIcon = rewardIconObject.GetComponent<RectTransform>();
                rewardIcon.anchorMin = new Vector2(0.610f, 0.5f);
                rewardIcon.anchorMax = new Vector2(0.610f, 0.5f);
                rewardIcon.pivot = new Vector2(0.5f, 0.5f);
                rewardIcon.anchoredPosition = Vector2.zero;
                rewardIcon.sizeDelta = new Vector2(58f, 58f);
                Image rewardImage = rewardIconObject.GetComponent<Image>();
                rewardImage.sprite = rewardSprite;
                rewardImage.type = Image.Type.Simple;
                rewardImage.preserveAspect = true;
                rewardImage.color = Color.white;
                rewardImage.raycastTarget = false;
            }

            Text amount = RuntimeUiFactory.CreateText(row, "Amount", $"{amountText} {rewardText}", 29, TextAnchor.MiddleLeft);
            amount.fontStyle = FontStyle.Bold;
            amount.color = HexColor(0xFF, 0xF4, 0xD6);
            amount.rectTransform.anchorMin = new Vector2(0.680f, 0f);
            amount.rectTransform.anchorMax = new Vector2(0.965f, 1f);
            amount.rectTransform.offsetMin = Vector2.zero;
            amount.rectTransform.offsetMax = Vector2.zero;
            amount.resizeTextForBestFit = true;
            amount.resizeTextMinSize = 19;
            amount.resizeTextMaxSize = 29;
            CasualUIStyle.ApplyTextDepth(amount, true);
        }

        private static void AddRowColorBand(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color,
            int cornerRadius)
        {
            RectTransform band = CasualUIFactory.CreatePanel(parent, name, color, cornerRadius);
            band.anchorMin = anchorMin;
            band.anchorMax = anchorMax;
            band.offsetMin = Vector2.zero;
            band.offsetMax = Vector2.zero;
            Image image = band.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }

        private static Color HexColor(byte r, byte g, byte b, byte a = 0xFF)
        {
            return new Color32(r, g, b, a);
        }

        private void BuildRankingPopup(RectTransform parent)
        {
            rankingPopupRoot = new GameObject("RankingPopup", typeof(RectTransform));
            rankingPopupRoot.transform.SetParent(parent, false);
            RectTransform root = rankingPopupRoot.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            RectTransform panel = CasualUIFactory.CreatePanel(
                root,
                "RankingPanel",
                RankingPanelColor,
                30);
            panel.anchorMin = new Vector2(0.060f, 0.070f);
            panel.anchorMax = new Vector2(0.940f, 0.805f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(1f, 0.620f, 0.140f, 0.96f);
            panelOutline.effectDistance = new Vector2(5f, -5f);
            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            panelShadow.effectDistance = new Vector2(0f, -12f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Weekly Ranking", 58, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.color = RankingWarmTextColor;
            title.rectTransform.anchorMin = new Vector2(0.18f, 0.895f);
            title.rectTransform.anchorMax = new Vector2(0.82f, 0.985f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(title, true);

            Button close = CreateAnchoredButton(
                panel,
                "RankingCloseButton",
                "X",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-34f, -34f),
                new Vector2(82f, 82f));
            PolishRankingCloseButton(close);
            close.onClick.AddListener(HideOverlay);

            Button worldTab = CreateAnchoredButton(
                panel,
                "WorldRankingTab",
                "World Ranking",
                new Vector2(0.28f, 0.845f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(360f, 86f));
            rankingWorldTabImage = worldTab.GetComponent<Image>();
            PolishRankingTab(worldTab);
            worldTab.onClick.AddListener(() => _ = LoadRankingTabAsync(true));

            Button myTab = CreateAnchoredButton(
                panel,
                "MyRankingTab",
                "My Records",
                new Vector2(0.72f, 0.845f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(360f, 86f));
            rankingMyTabImage = myTab.GetComponent<Image>();
            PolishRankingTab(myTab);
            myTab.onClick.AddListener(() => _ = LoadRankingTabAsync(false));

            rankingStatusText = RuntimeUiFactory.CreateText(panel, "StatusText", string.Empty, 23, TextAnchor.MiddleCenter);
            rankingStatusText.color = new Color(0.650f, 0.705f, 0.805f, 0.96f);
            rankingStatusText.rectTransform.anchorMin = new Vector2(0.08f, 0.765f);
            rankingStatusText.rectTransform.anchorMax = new Vector2(0.92f, 0.807f);
            rankingStatusText.rectTransform.offsetMin = Vector2.zero;
            rankingStatusText.rectTransform.offsetMax = Vector2.zero;

            GameObject scrollObject = new GameObject("RankingScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(panel, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.055f, 0.055f);
            scrollRectTransform.anchorMax = new Vector2(0.945f, 0.750f);
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;
            Image scrollImage = scrollObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(scrollImage, RankingInnerPanelColor, 22);
            scrollImage.raycastTarget = true;
            Outline scrollOutline = scrollObject.AddComponent<Outline>();
            scrollOutline.effectColor = new Color(1f, 0.590f, 0.120f, 0.88f);
            scrollOutline.effectDistance = new Vector2(2f, -2f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollRectTransform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12f, 12f);
            viewport.offsetMax = new Vector2(-12f, -12f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.010f);
            viewportImage.raycastTarget = false;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            rankingListContent = contentObject.GetComponent<RectTransform>();
            rankingListContent.anchorMin = new Vector2(0f, 1f);
            rankingListContent.anchorMax = new Vector2(1f, 1f);
            rankingListContent.pivot = new Vector2(0.5f, 1f);
            rankingListContent.offsetMin = Vector2.zero;
            rankingListContent.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            rankingListLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            rankingScrollRect = scrollObject.GetComponent<ScrollRect>();
            rankingScrollRect.viewport = viewport;
            rankingScrollRect.content = rankingListContent;
            rankingScrollRect.horizontal = false;
            rankingScrollRect.vertical = true;
            rankingScrollRect.movementType = ScrollRect.MovementType.Clamped;
            rankingScrollRect.scrollSensitivity = 34f;
            rankingScrollRect.onValueChanged.AddListener(_ => UpdateRankingStickyVisibility());

            rankingStickyRow = CreateRankingStickyRow(panel);
            rankingStickyRow.gameObject.SetActive(false);

            rankingPopupRoot.SetActive(false);
        }

        private static void PolishRankingTab(Button tab)
        {
            if (tab == null)
            {
                return;
            }

            tab.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = tab.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.86f, 0.95f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.42f, 0.45f, 0.52f, 0.7f);
            colors.colorMultiplier = 1f;
            tab.colors = colors;

            RemoveButtonLayer(tab.transform, "Depth");
            RemoveButtonLayer(tab.transform, "InnerStroke");
            RemoveButtonLayer(tab.transform, "Gloss");

            Transform iconHolder = tab.transform.Find("IconHolder");
            if (iconHolder != null)
            {
                RectTransform iconRect = iconHolder as RectTransform;
                if (iconRect != null)
                {
                    iconRect.anchorMin = new Vector2(0.075f, 0.22f);
                    iconRect.anchorMax = new Vector2(0.255f, 0.86f);
                    iconRect.offsetMin = Vector2.zero;
                    iconRect.offsetMax = Vector2.zero;
                }

                CreateRankingTabTrophyIcon(iconHolder);
            }
        }

        private async System.Threading.Tasks.Task LoadRankingTabAsync(bool worldRanking)
        {
            if (ranking == null || rankingPopupRoot == null || rankingListContent == null)
            {
                return;
            }

            int token = ++rankingLoadToken;
            rankingShowingWorldTab = worldRanking;
            SetRankingTabSelected(worldRanking);
            ClearRankingRows();
            SetRankingStickyRow(null, 0);
            rankingStatusText.text = worldRanking ? "Loading world ranking..." : "Loading my records...";

            RankingFetchResult result;
            RankingRankResult latestRank = null;
            if (worldRanking)
            {
                result = await ranking.FetchWorldRankingsAsync(50);
                latestRank = await ranking.FetchLatestRankAsync();
            }
            else
            {
                result = await ranking.FetchMyRankingsAsync(10);
            }
            if (token != rankingLoadToken)
            {
                return;
            }

            IReadOnlyList<RankingSubmission> records = result?.records ?? new List<RankingSubmission>();
            string source = result?.message ?? (worldRanking ? "World Ranking" : "My Records");
            rankingStatusText.text = BuildRankingStatus(source, records.Count, worldRanking);
            if (records.Count == 0)
            {
                AddRankingEmptyRow(worldRanking
                    ? (result != null && result.success ? "No records yet." : "Failed to load ranking.")
                    : (result != null && result.success ? "No records yet." : source));
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                bool highlight = !worldRanking && IsLatestRecord(records[i]) && !HasSeenLatestRecord(records[i]);
                AddRankingRow(records[i], i + 1, worldRanking, highlight);
            }

            if (worldRanking && latestRank != null && latestRank.success && latestRank.record != null)
            {
                SetRankingStickyRow(latestRank.record, latestRank.rank);
                UpdateRankingStickyVisibility();
            }
            else if (!worldRanking)
            {
                MarkLatestRecordSeen(records);
            }
        }

        private void SetRankingTabSelected(bool worldRanking)
        {
            if (rankingWorldTabImage != null)
            {
                CasualUIStyle.ApplyPanel(
                    rankingWorldTabImage,
                    worldRanking ? RankingActiveTabColor : RankingInactiveTabColor,
                    24);
                SetRankingTabOutline(rankingWorldTabImage, worldRanking);
                SetRankingTabLabel(rankingWorldTabImage, worldRanking);
            }

            if (rankingMyTabImage != null)
            {
                CasualUIStyle.ApplyPanel(
                    rankingMyTabImage,
                    worldRanking ? RankingInactiveTabColor : RankingActiveTabColor,
                    24);
                SetRankingTabOutline(rankingMyTabImage, !worldRanking);
                SetRankingTabLabel(rankingMyTabImage, !worldRanking);
            }
        }

        private static void SetRankingTabOutline(Image image, bool selected)
        {
            if (image == null)
            {
                return;
            }

            Outline outline = image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
            outline.effectColor = selected
                ? new Color(1f, 0.760f, 0.220f, 0.96f)
                : new Color(0.800f, 0.520f, 0.210f, 0.78f);
            outline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);

            Shadow shadow = image.GetComponent<Shadow>() ?? image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = selected
                ? new Color(1f, 0.420f, 0.050f, 0.58f)
                : new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = selected ? new Vector2(0f, -6f) : new Vector2(0f, -4f);
        }

        private static void SetRankingTabLabel(Image image, bool selected)
        {
            if (image == null)
            {
                return;
            }

            Text label = image.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.color = selected
                ? new Color(1f, 0.930f, 0.690f, 1f)
                : new Color(0.640f, 0.700f, 0.835f, 1f);
            label.fontStyle = FontStyle.Bold;

            Transform iconHolder = image.transform.Find("IconHolder");
            if (iconHolder == null)
            {
                return;
            }

            Image holderImage = iconHolder.GetComponent<Image>();
            if (holderImage != null)
            {
                holderImage.color = Color.clear;
                holderImage.raycastTarget = false;
            }

            Color iconColor = Color.white;
            Image[] iconImages = iconHolder.GetComponentsInChildren<Image>(true);
            foreach (Image iconImage in iconImages)
            {
                if (iconImage == null || iconImage == holderImage)
                {
                    continue;
                }

                iconImage.color = iconColor;
                iconImage.raycastTarget = false;
            }
        }

        private static void CreateRankingTabTrophyIcon(Transform parent)
        {
            RectTransform root = parent as RectTransform;
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
            }

            Sprite trophy = CasualIconFactory.LoadMainMenuKitSprite("Icons/ranking");
            if (trophy == null)
            {
                CasualIconFactory.Create(root, "ranking", new Color(1f, 0.86f, 0.36f, 1f));
                return;
            }

            GameObject iconObject = new GameObject("TrophyIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(root, false);
            RectTransform icon = iconObject.GetComponent<RectTransform>();
            icon.anchorMin = Vector2.zero;
            icon.anchorMax = Vector2.one;
            icon.offsetMin = new Vector2(2f, 2f);
            icon.offsetMax = new Vector2(-2f, -2f);
            Image image = iconObject.GetComponent<Image>();
            image.sprite = trophy;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static void RemoveButtonLayer(Transform parent, string layerName)
        {
            if (parent == null)
            {
                return;
            }

            Transform layer = parent.Find(layerName);
            if (layer != null)
            {
                UnityEngine.Object.Destroy(layer.gameObject);
            }
        }

        private void ClearRankingRows()
        {
            if (rankingListContent == null)
            {
                return;
            }

            for (int i = rankingListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(rankingListContent.GetChild(i).gameObject);
            }

            rankingRowsBySubmissionId.Clear();
        }

        private void AddRankingEmptyRow(string message)
        {
            RectTransform row = CreateRankingRowRoot("EmptyRow", 620f, RankingInnerPanelColor);
            Outline outline = row.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.clear;
            }

            GameObject cubeObject = new GameObject("EmptyCubeIcon", typeof(RectTransform));
            cubeObject.transform.SetParent(row, false);
            RectTransform cube = cubeObject.GetComponent<RectTransform>();
            cube.anchorMin = new Vector2(0.5f, 0.55f);
            cube.anchorMax = new Vector2(0.5f, 0.55f);
            cube.pivot = new Vector2(0.5f, 0.5f);
            cube.anchoredPosition = Vector2.zero;
            cube.sizeDelta = new Vector2(118f, 118f);
            CreateEmptyCubeGlyph(cube);

            Text title = RuntimeUiFactory.CreateText(row, "EmptyTitle", message, 34, TextAnchor.MiddleCenter);
            title.color = new Color(0.82f, 0.88f, 1f, 0.96f);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.34f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.44f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(title, true);

            Text subtitle = RuntimeUiFactory.CreateText(row, "EmptySubtitle", "Be the first to set the record!", 24, TextAnchor.MiddleCenter);
            subtitle.color = RankingMutedTextColor;
            subtitle.rectTransform.anchorMin = new Vector2(0.08f, 0.27f);
            subtitle.rectTransform.anchorMax = new Vector2(0.92f, 0.35f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;
        }

        private static void PolishRankingCloseButton(Button close)
        {
            if (close == null)
            {
                return;
            }

            close.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = close.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.84f, 0.92f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.42f, 0.45f, 0.52f, 0.7f);
            colors.colorMultiplier = 1f;
            close.colors = colors;

            RemoveButtonLayer(close.transform, "Depth");
            RemoveButtonLayer(close.transform, "InnerStroke");
            RemoveButtonLayer(close.transform, "Gloss");

            Transform iconHolder = close.transform.Find("IconHolder");
            if (iconHolder != null)
            {
                UnityEngine.Object.Destroy(iconHolder.gameObject);
            }

            Image image = close.GetComponent<Image>();
            if (image != null)
            {
                CasualUIStyle.ApplyPanel(image, new Color(0.020f, 0.055f, 0.110f, 1f), 24);
            }

            Outline outline = close.GetComponent<Outline>() ?? close.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.700f, 0.240f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            Text label = close.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "X";
                label.fontSize = 42;
                label.fontStyle = FontStyle.Bold;
                label.color = new Color(1f, 0.835f, 0.470f, 1f);
                label.resizeTextMinSize = 26;
                label.resizeTextMaxSize = 40;
                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                CasualUIStyle.ApplyTextDepth(label, true);
            }
        }

        private static void CreateEmptyCubeGlyph(RectTransform parent)
        {
            Color line = new Color(1f, 0.78f, 0.38f, 0.92f);
            CreateGlyphLine(parent, "LeftTop", new Vector2(0.30f, 0.54f), new Vector2(0.50f, 0.70f), line, 3f);
            CreateGlyphLine(parent, "RightTop", new Vector2(0.50f, 0.70f), new Vector2(0.70f, 0.54f), line, 3f);
            CreateGlyphLine(parent, "LeftMid", new Vector2(0.30f, 0.54f), new Vector2(0.30f, 0.34f), line, 3f);
            CreateGlyphLine(parent, "RightMid", new Vector2(0.70f, 0.54f), new Vector2(0.70f, 0.34f), line, 3f);
            CreateGlyphLine(parent, "BottomLeft", new Vector2(0.30f, 0.34f), new Vector2(0.50f, 0.20f), line, 3f);
            CreateGlyphLine(parent, "BottomRight", new Vector2(0.50f, 0.20f), new Vector2(0.70f, 0.34f), line, 3f);
            CreateGlyphLine(parent, "Center", new Vector2(0.50f, 0.70f), new Vector2(0.50f, 0.20f), new Color(1f, 0.78f, 0.38f, 0.55f), 3f);
        }

        private static void CreateGlyphLine(RectTransform parent, string name, Vector2 from, Vector2 to, Color color, float thickness)
        {
            GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(parent, false);
            RectTransform rect = lineObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 delta = to - from;
            Vector2 midpoint = (from + to) * 0.5f;
            float unit = parent.rect.width > 0f ? parent.rect.width : 118f;
            rect.anchoredPosition = new Vector2((midpoint.x - 0.5f) * unit, (midpoint.y - 0.5f) * unit);
            rect.sizeDelta = new Vector2(delta.magnitude * unit, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = lineObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, 2);
            image.raycastTarget = false;
        }

        private void AddRankingRow(RankingSubmission record, int rank, bool worldRanking, bool highlight = false)
        {
            Color rowColor = rank == 1
                ? new Color(0.300f, 0.205f, 0.045f, 0.98f)
                : rank == 2
                    ? new Color(0.135f, 0.185f, 0.235f, 0.98f)
                    : rank == 3
                        ? new Color(0.225f, 0.105f, 0.040f, 0.98f)
                        : new Color(0.025f, 0.070f, 0.115f, 0.94f);
            if (highlight)
            {
                rowColor = new Color(0.085f, 0.105f, 0.130f, 0.98f);
            }

            RectTransform row = CreateRankingRowRoot($"RankingRow{rank:00}", 96f, rowColor);
            if (!string.IsNullOrWhiteSpace(record?.submissionId))
            {
                rankingRowsBySubmissionId[record.submissionId] = row;
            }

            CreateRankingRankCell(row, rank, worldRanking);
            CreateRankingAvatar(row, record?.avatarId ?? -1);

            Text name = CreateRankingCell(row, "Nickname", string.IsNullOrWhiteSpace(record?.playerName) ? "Player" : record.playerName, 25, TextAnchor.MiddleLeft);
            name.rectTransform.anchorMin = new Vector2(0.305f, 0f);
            name.rectTransform.anchorMax = new Vector2(0.555f, 1f);
            name.fontStyle = FontStyle.Bold;
            name.color = RankingBodyTextColor;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 16;
            name.resizeTextMaxSize = 25;

            Text time = CreateRankingCell(row, "Time", FormatSeconds(record?.elapsedSeconds ?? 0f), 24, TextAnchor.MiddleRight);
            time.rectTransform.anchorMin = new Vector2(0.565f, 0f);
            time.rectTransform.anchorMax = new Vector2(0.740f, 1f);
            time.color = new Color(1f, 0.760f, 0.260f, 1f);
            time.fontStyle = FontStyle.Bold;

            Text moves = CreateRankingCell(row, "Moves", $"{Mathf.Max(0, record?.moveCount ?? 0)} moves", 22, TextAnchor.MiddleRight);
            moves.rectTransform.anchorMin = new Vector2(0.760f, 0f);
            moves.rectTransform.anchorMax = new Vector2(0.975f, 1f);
            moves.color = new Color(0.740f, 0.835f, 1f, 1f);

            Outline outline = row.GetComponent<Outline>();
            if (outline != null && rank <= 3)
            {
                outline.effectColor = rank == 1
                    ? new Color(1f, 0.730f, 0.140f, 0.86f)
                    : rank == 2
                        ? new Color(0.760f, 0.840f, 0.930f, 0.70f)
                        : new Color(0.980f, 0.430f, 0.120f, 0.72f);
            }
            else if (outline != null && highlight)
            {
                outline.effectColor = new Color(1f, 0.74f, 0.25f, 0.84f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
        }

        private void CreateRankingRankCell(RectTransform row, int rank, bool worldRanking)
        {
            if (rank > 3)
            {
                Text standardRankText = CreateRankingCell(row, "Rank", worldRanking ? FormatRankLabel(rank) : $"#{rank}", 24, TextAnchor.MiddleCenter);
                standardRankText.rectTransform.anchorMin = new Vector2(0.010f, 0f);
                standardRankText.rectTransform.anchorMax = new Vector2(0.165f, 1f);
                standardRankText.fontStyle = FontStyle.Bold;
                standardRankText.color = new Color(0.900f, 0.925f, 0.980f, 1f);
                return;
            }

            Sprite rankMedal = Resources.Load<Sprite>($"UI/Records/world_rank_{rank}");
            if (rankMedal != null)
            {
                GameObject medalObject = new GameObject($"RankMedal{rank}", typeof(RectTransform), typeof(Image));
                medalObject.transform.SetParent(row, false);
                RectTransform medal = medalObject.GetComponent<RectTransform>();
                medal.anchorMin = new Vector2(0.012f, 0.060f);
                medal.anchorMax = new Vector2(0.170f, 0.940f);
                medal.offsetMin = Vector2.zero;
                medal.offsetMax = Vector2.zero;

                Image medalImage = medalObject.GetComponent<Image>();
                medalImage.sprite = rankMedal;
                medalImage.type = Image.Type.Simple;
                medalImage.preserveAspect = true;
                medalImage.color = Color.white;
                medalImage.raycastTarget = false;
                return;
            }

            Color fill;
            Color edge;
            Color text;
            ResolvePodiumColors(rank, out fill, out edge, out text);
            RectTransform badge = CasualUIFactory.CreatePanel(row, "RankBadge", fill, 18);
            badge.anchorMin = new Vector2(0.010f, 0.105f);
            badge.anchorMax = new Vector2(0.170f, 0.895f);
            badge.offsetMin = Vector2.zero;
            badge.offsetMax = Vector2.zero;
            Outline outline = badge.gameObject.AddComponent<Outline>();
            outline.effectColor = edge;
            outline.effectDistance = new Vector2(2f, -2f);

            CreateRankingAccentLine(badge, "LeftAccent", new Vector2(0.115f, 0.18f), new Vector2(0.135f, 0.82f), edge);

            Text podiumRankText = RuntimeUiFactory.CreateText(badge, "Rank", FormatRankLabel(rank), 28, TextAnchor.MiddleCenter);
            podiumRankText.fontStyle = FontStyle.Bold;
            podiumRankText.color = text;
            podiumRankText.rectTransform.anchorMin = new Vector2(0.18f, 0f);
            podiumRankText.rectTransform.anchorMax = new Vector2(0.82f, 1f);
            podiumRankText.rectTransform.offsetMin = Vector2.zero;
            podiumRankText.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(podiumRankText, true);

            CreateRankingAccentLine(badge, "RightAccent", new Vector2(0.865f, 0.18f), new Vector2(0.885f, 0.82f), edge);
        }

        private static void CreateRankingAvatar(RectTransform row, int avatarId)
        {
            GameObject avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Outline));
            avatarObject.transform.SetParent(row, false);
            RectTransform avatar = avatarObject.GetComponent<RectTransform>();
            avatar.anchorMin = new Vector2(0.190f, 0.180f);
            avatar.anchorMax = new Vector2(0.285f, 0.820f);
            avatar.offsetMin = Vector2.zero;
            avatar.offsetMax = Vector2.zero;

            int normalizedAvatarId = Mathf.Clamp(avatarId < 0 ? 0 : avatarId, 0, 3);
            Image image = avatarObject.GetComponent<Image>();
            Sprite avatarSprite = LoadRankingAvatarSprite(normalizedAvatarId);
            if (avatarSprite != null)
            {
                image.sprite = avatarSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                CasualUIStyle.ApplyPanel(image, GetRankingAvatarColor(normalizedAvatarId), 20);
            }
            image.raycastTarget = false;

            Outline outline = avatarObject.GetComponent<Outline>();
            outline.effectColor = RankingWarmTextColor;
            outline.effectDistance = new Vector2(2f, -2f);

            if (avatarSprite == null)
            {
                Text label = RuntimeUiFactory.CreateText(avatar, "AvatarLabel", $"A{normalizedAvatarId + 1}", 18, TextAnchor.MiddleCenter);
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                CasualUIStyle.ApplyTextDepth(label, true);
            }
        }

        private static Sprite LoadRankingAvatarSprite(int avatarId)
        {
            int normalizedAvatarId = Mathf.Clamp(avatarId, 0, 3);
            return Resources.Load<Sprite>($"UI/Profile/Avatars/profile_avatar_{normalizedAvatarId + 1}");
        }

        private static void CreateRankingAccentLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform line = CasualUIFactory.CreatePanel(parent, name, new Color(color.r, color.g, color.b, 0.62f), 2);
            line.anchorMin = anchorMin;
            line.anchorMax = anchorMax;
            line.offsetMin = Vector2.zero;
            line.offsetMax = Vector2.zero;
        }

        private static Color GetRankingAvatarColor(int avatarId)
        {
            switch (Mathf.Clamp(avatarId, 0, 3))
            {
                case 1:
                    return new Color(0.92f, 0.35f, 0.55f, 1f);
                case 2:
                    return new Color(0.55f, 0.34f, 0.96f, 1f);
                case 3:
                    return new Color(0.15f, 0.62f, 0.58f, 1f);
                default:
                    return new Color(0.24f, 0.47f, 0.94f, 1f);
            }
        }

        private static void ResolvePodiumColors(int rank, out Color fill, out Color edge, out Color text)
        {
            switch (rank)
            {
                case 1:
                    fill = new Color(0.520f, 0.340f, 0.050f, 0.96f);
                    edge = new Color(1f, 0.760f, 0.120f, 1f);
                    text = new Color(1f, 0.900f, 0.460f, 1f);
                    break;
                case 2:
                    fill = new Color(0.160f, 0.230f, 0.295f, 0.96f);
                    edge = new Color(0.760f, 0.850f, 0.930f, 1f);
                    text = new Color(0.900f, 0.940f, 1f, 1f);
                    break;
                default:
                    fill = new Color(0.375f, 0.160f, 0.050f, 0.96f);
                    edge = new Color(0.980f, 0.470f, 0.135f, 1f);
                    text = new Color(1f, 0.650f, 0.300f, 1f);
                    break;
            }
        }

        private RectTransform CreateRankingStickyRow(RectTransform panel)
        {
            RectTransform row = CasualUIFactory.CreatePanel(panel, "LatestRecordStickyRow", new Color(0.012f, 0.070f, 0.115f, 0.99f), 20);
            row.anchorMin = new Vector2(0.080f, 0.058f);
            row.anchorMax = new Vector2(0.920f, 0.128f);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.620f, 0.120f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
            Shadow shadow = row.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(1f, 0.420f, 0.050f, 0.34f);
            shadow.effectDistance = new Vector2(0f, -5f);
            return row;
        }

        private void SetRankingStickyRow(RankingSubmission record, int rank)
        {
            if (rankingStickyRow == null)
            {
                return;
            }

            for (int i = rankingStickyRow.childCount - 1; i >= 0; i--)
            {
                Destroy(rankingStickyRow.GetChild(i).gameObject);
            }

            bool hasRecord = record != null && rank > 0;
            rankingStickySubmissionId = hasRecord ? record.submissionId : string.Empty;
            rankingStickyRow.gameObject.SetActive(hasRecord);
            SetRankingListBottomPadding(hasRecord ? 96 : 10);
            if (!hasRecord)
            {
                return;
            }

            RectTransform labelPanel = CasualUIFactory.CreatePanel(rankingStickyRow, "LatestLabelPanel", new Color(0.020f, 0.055f, 0.082f, 0.92f), 15);
            labelPanel.anchorMin = new Vector2(0.014f, 0.130f);
            labelPanel.anchorMax = new Vector2(0.220f, 0.870f);
            labelPanel.offsetMin = Vector2.zero;
            labelPanel.offsetMax = Vector2.zero;

            Text prefix = RuntimeUiFactory.CreateText(labelPanel, "StickyPrefix", "Your latest", 20, TextAnchor.MiddleCenter);
            prefix.rectTransform.anchorMin = Vector2.zero;
            prefix.rectTransform.anchorMax = new Vector2(1f, 1f);
            prefix.rectTransform.offsetMin = Vector2.zero;
            prefix.rectTransform.offsetMax = Vector2.zero;
            prefix.color = new Color(1f, 0.780f, 0.220f, 1f);
            prefix.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(prefix, true);

            Text rankText = CreateRankingCell(
                rankingStickyRow,
                "StickyRank",
                FormatRankLabel(rank),
                24,
                TextAnchor.MiddleCenter);
            rankText.rectTransform.anchorMin = new Vector2(0.245f, 0f);
            rankText.rectTransform.anchorMax = new Vector2(0.360f, 1f);
            rankText.color = new Color(1f, 0.780f, 0.220f, 1f);
            rankText.fontStyle = FontStyle.Bold;

            Text name = CreateRankingCell(rankingStickyRow, "StickyName", SafePlayerName(record), 23, TextAnchor.MiddleLeft);
            name.rectTransform.anchorMin = new Vector2(0.380f, 0f);
            name.rectTransform.anchorMax = new Vector2(0.605f, 1f);
            name.color = RankingBodyTextColor;
            name.fontStyle = FontStyle.Bold;

            Text time = CreateRankingCell(rankingStickyRow, "StickyTime", FormatSeconds(record.elapsedSeconds), 23, TextAnchor.MiddleRight);
            time.rectTransform.anchorMin = new Vector2(0.625f, 0f);
            time.rectTransform.anchorMax = new Vector2(0.770f, 1f);
            time.color = new Color(1f, 0.760f, 0.260f, 1f);
            time.fontStyle = FontStyle.Bold;

            Text moves = CreateRankingCell(rankingStickyRow, "StickyMoves", $"{Mathf.Max(0, record.moveCount)} moves", 21, TextAnchor.MiddleRight);
            moves.rectTransform.anchorMin = new Vector2(0.790f, 0f);
            moves.rectTransform.anchorMax = new Vector2(0.980f, 1f);
            moves.color = new Color(0.790f, 0.865f, 1f, 1f);
        }

        private void SetRankingListBottomPadding(int bottomPadding)
        {
            if (rankingListLayout != null)
            {
                rankingListLayout.padding = new RectOffset(10, 10, 10, bottomPadding);
            }
        }

        private void UpdateRankingStickyVisibility()
        {
            if (!rankingShowingWorldTab || rankingStickyRow == null || string.IsNullOrWhiteSpace(rankingStickySubmissionId))
            {
                return;
            }

            if (!rankingRowsBySubmissionId.TryGetValue(rankingStickySubmissionId, out RectTransform row))
            {
                rankingStickyRow.gameObject.SetActive(true);
                return;
            }

            rankingStickyRow.gameObject.SetActive(!IsRowVisibleInRankingViewport(row));
        }

        private bool IsRowVisibleInRankingViewport(RectTransform row)
        {
            if (row == null || rankingScrollRect == null || rankingScrollRect.viewport == null)
            {
                return false;
            }

            Vector3[] rowCorners = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            row.GetWorldCorners(rowCorners);
            rankingScrollRect.viewport.GetWorldCorners(viewportCorners);
            return rowCorners[1].y <= viewportCorners[1].y && rowCorners[0].y >= viewportCorners[0].y;
        }

        private bool IsLatestRecord(RankingSubmission record)
        {
            string latestId = ranking?.LatestSubmittedRecord?.submissionId;
            return record != null
                && !string.IsNullOrWhiteSpace(latestId)
                && record.submissionId == latestId;
        }

        private bool HasSeenLatestRecord(RankingSubmission record)
        {
            return record == null || PlayerPrefs.GetInt(GetLatestRecordSeenKey(record.submissionId), 0) == 1;
        }

        private void MarkLatestRecordSeen(IReadOnlyList<RankingSubmission> records)
        {
            RankingSubmission latest = records?.FirstOrDefault(IsLatestRecord);
            if (latest == null)
            {
                return;
            }

            PlayerPrefs.SetInt(GetLatestRecordSeenKey(latest.submissionId), 1);
            PlayerPrefs.Save();
        }

        private static string GetLatestRecordSeenKey(string submissionId)
        {
            return $"ranking_latest_record_seen_{submissionId}";
        }

        private static string SafePlayerName(RankingSubmission record)
        {
            return string.IsNullOrWhiteSpace(record?.playerName) ? "Player" : record.playerName;
        }

        private RectTransform CreateRankingRowRoot(string name, float height, Color color)
        {
            RectTransform row = CasualUIFactory.CreatePanel(rankingListContent, name, color, 18);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.36f, 0.56f, 0.86f, 0.34f);
            outline.effectDistance = new Vector2(2f, -2f);
            return row;
        }

        private Text CreateRankingCell(RectTransform row, string name, string value, int fontSize, TextAnchor alignment)
        {
            Text text = RuntimeUiFactory.CreateText(row, name, value, fontSize, alignment);
            text.rectTransform.offsetMin = new Vector2(6f, 0f);
            text.rectTransform.offsetMax = new Vector2(-6f, 0f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 13;
            text.resizeTextMaxSize = fontSize;
            CasualUIStyle.ApplyTextDepth(text, false);
            return text;
        }

        private static string BuildRankingStatus(string source, int count, bool worldRanking)
        {
            string prefix = string.IsNullOrWhiteSpace(source)
                ? (worldRanking ? "World Ranking" : "My Records")
                : source;
            string limit = worldRanking ? "Top 50" : "Best 10";
            return $"{prefix}  \u2022  {limit}  \u2022  {count} record{(count == 1 ? string.Empty : "s")}";
        }

        private static string FormatRankLabel(int rank)
        {
            int mod100 = rank % 100;
            if (mod100 >= 11 && mod100 <= 13)
            {
                return $"{rank}th";
            }

            switch (rank % 10)
            {
                case 1: return $"{rank}st";
                case 2: return $"{rank}nd";
                case 3: return $"{rank}rd";
                default: return $"{rank}th";
            }
        }

        private static string FormatSeconds(float seconds)
        {
            return $"{Mathf.Max(0f, seconds):0.00}s";
        }

        private void ShowOverlay(
            string title,
            string body,
            string primaryLabel,
            UnityEngine.Events.UnityAction primaryAction,
            bool allowClose)
        {
            if (rankingPopupRoot != null)
            {
                rankingPopupRoot.SetActive(false);
            }

            if (weeklyRewardsPopupRoot != null)
            {
                weeklyRewardsPopupRoot.SetActive(false);
            }

            ApplyOverlayPanelColors(
                RankingPanelColor,
                RankingGoldColor);
            if (overlayPanel != null)
            {
                overlayPanel.gameObject.SetActive(true);
            }

            overlayTitle.gameObject.SetActive(true);
            overlayBody.gameObject.SetActive(true);
            overlayPrimaryButton.gameObject.SetActive(true);
            overlayPrimaryButton.interactable = true;
            ConfigureOverlayButton(overlayPrimaryButton, Vector2.zero, new Vector2(340f, 82f));
            overlayTitle.text = title;
            overlayBody.text = body;
            overlayBody.alignment = TextAnchor.UpperCenter;
            SetOverlayTextRect(overlayBody.rectTransform, 0.31f, 0.67f);
            SetButtonLabel(overlayPrimaryButton, primaryLabel);
            overlayPrimaryButton.onClick.RemoveAllListeners();
            if (primaryAction != null)
            {
                overlayPrimaryButton.onClick.AddListener(primaryAction);
            }

            if (overlaySecondaryButton != null)
            {
                overlaySecondaryButton.gameObject.SetActive(false);
                overlaySecondaryButton.onClick.RemoveAllListeners();
            }

            if (overlayTertiaryButton != null)
            {
                overlayTertiaryButton.gameObject.SetActive(false);
                overlayTertiaryButton.onClick.RemoveAllListeners();
            }

            SetOverlayStarsVisible(0);
            if (starConditionsRoot != null)
            {
                starConditionsRoot.gameObject.SetActive(false);
            }

            overlayCloseButton.gameObject.SetActive(allowClose);
            SetOverlayShade(0.82f);
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        private void ApplyOverlayPanelColors(Color backgroundColor, Color borderColor)
        {
            if (overlayPanel == null)
            {
                return;
            }

            Image panelImage = overlayPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = backgroundColor;
            }

            Outline outline = overlayPanel.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = borderColor;
            }
        }

        private void ShowStageClearOverlay()
        {
            StageData currentStage = stage?.CurrentStage;
            if (currentStage == null)
            {
                return;
            }

            bool finalAvailableStage = IsFinalAvailableStage(currentStage);
            StageData nextStage = finalAvailableStage ? null : stage.PeekNextPlayableStage();
            bool hasNextStage = nextStage != null;
            bool isReplay = IsReplayClearStage(currentStage.stageNumber);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StageClearResult] stage={currentStage.stageNumber} earnedStars={stage.EarnedStars} previousBestStars={stage.PreviousBestStars} isReplay={isReplay} nextStage={(nextStage != null ? nextStage.stageNumber : -1)}");
#endif

            if (finalAvailableStage)
            {
                ShowOverlay(
                    "Congratulations!",
                    "You've cleared all available stages.\nMore stages are coming soon!",
                    "OK",
                    CloseStageClearResultToStageList,
                    false);

                ConfigureOverlayButton(overlayPrimaryButton, Vector2.zero, new Vector2(260f, 82f));
                return;
            }

            ShowOverlay(
                "Stage Clear!",
                $"Moves: {stage.Runtime.currentMoves}",
                "Next Stage",
                () =>
                {
                    SetOverlayContentVisible(false);
                    RequestNextStageViaStageMap();
                },
                false);

            SetOverlayStarsVisible(stage.EarnedStars);
            SetOverlayTextRect(overlayBody.rectTransform, 0.395f, 0.505f);
            overlayBody.alignment = TextAnchor.MiddleCenter;

            ConfigureOverlayButton(overlayPrimaryButton, new Vector2(-260f, 0f), new Vector2(230f, 78f));
            overlayPrimaryButton.interactable = hasNextStage;
            if (!hasNextStage)
            {
                SetButtonLabel(overlayPrimaryButton, "No Next");
            }

            if (overlaySecondaryButton != null)
            {
                overlaySecondaryButton.gameObject.SetActive(true);
                overlaySecondaryButton.interactable = true;
                ConfigureOverlayButton(overlaySecondaryButton, Vector2.zero, new Vector2(230f, 78f));
                SetButtonLabel(overlaySecondaryButton, "Retry");
                overlaySecondaryButton.onClick.RemoveAllListeners();
                overlaySecondaryButton.onClick.AddListener(() =>
                {
                    HideOverlay();
                    stage?.RetryStage();
                });
            }

            if (overlayTertiaryButton != null)
            {
                overlayTertiaryButton.gameObject.SetActive(true);
                overlayTertiaryButton.interactable = true;
                ConfigureOverlayButton(overlayTertiaryButton, new Vector2(260f, 0f), new Vector2(230f, 78f));
                SetButtonLabel(overlayTertiaryButton, "Close");
                overlayTertiaryButton.onClick.RemoveAllListeners();
                overlayTertiaryButton.onClick.AddListener(CloseStageClearResultToStageList);
            }
        }

        private void ShowStageGameOverOverlay()
        {
            ShowOverlay(
                "Out of Moves",
                "You've run out of moves.",
                $"+{stage.StageContinueMovesReward} Moves",
                () =>
                {
                    if (stage == null)
                    {
                        return;
                    }

                    if (overlayPrimaryButton != null)
                    {
                        overlayPrimaryButton.interactable = false;
                    }

                    stage.ContinueAfterAd(result =>
                    {
                        if (result == RewardedAdResult.Rewarded)
                        {
                            HideOverlay();
                            return;
                        }

                        if (overlayBody != null)
                        {
                            overlayBody.text = GetStageContinueAdFailureMessage(result);
                        }

                        if (overlayPrimaryButton != null)
                        {
                            overlayPrimaryButton.interactable = stage.CanRequestContinueAfterAd();
                        }
                    });
                },
                false);

            SetOverlayTextRect(overlayBody.rectTransform, 0.440f, 0.590f);
            overlayBody.alignment = TextAnchor.MiddleCenter;
            ConfigureOverlayButton(overlayPrimaryButton, new Vector2(-260f, 0f), new Vector2(230f, 78f));
            overlayPrimaryButton.interactable = stage != null && stage.CanRequestContinueAfterAd();

            if (overlaySecondaryButton != null)
            {
                overlaySecondaryButton.gameObject.SetActive(true);
                overlaySecondaryButton.interactable = true;
                ConfigureOverlayButton(overlaySecondaryButton, Vector2.zero, new Vector2(230f, 78f));
                SetButtonLabel(overlaySecondaryButton, "Retry");
                overlaySecondaryButton.onClick.RemoveAllListeners();
                overlaySecondaryButton.onClick.AddListener(() =>
                {
                    HideOverlay();
                    stage?.RetryStage();
                });
            }

            if (overlayTertiaryButton != null)
            {
                overlayTertiaryButton.gameObject.SetActive(true);
                overlayTertiaryButton.interactable = true;
                ConfigureOverlayButton(overlayTertiaryButton, new Vector2(260f, 0f), new Vector2(230f, 78f));
                SetButtonLabel(overlayTertiaryButton, "Close");
                overlayTertiaryButton.onClick.RemoveAllListeners();
                overlayTertiaryButton.onClick.AddListener(CloseStageClearResultToStageList);
            }
        }

        private static string GetStageContinueAdFailureMessage(RewardedAdResult result)
        {
            switch (result)
            {
                case RewardedAdResult.NotReady:
                    return "Ad failed to load.\nPlease try again later.";
                case RewardedAdResult.Closed:
                    return "Ad was not completed.\nNo moves were added.";
                case RewardedAdResult.LimitReached:
                    return "Stage continue limit reached.";
                case RewardedAdResult.Unavailable:
                    return "Ad is not available yet.";
                case RewardedAdResult.Busy:
                    return "Ad is already opening.";
                default:
                    return "Ad not completed.\nNo moves were added.";
            }
        }

        private static bool IsFinalAvailableStage(StageData stageData)
        {
            if (stageData == null)
            {
                return false;
            }

            int localStage = GetStageDisplayNumber(stageData);
            return stageData.stageType == StageType.SolveStage && localStage >= StagePackGenerator.NormalStageCount
                || stageData.stageType == StageType.ReverseTargetStage && localStage >= StagePackGenerator.HardStageCount
                || stageData.stageType == StageType.InfinityStage && localStage >= StagePackGenerator.InfinityStageCount;
        }

        private void CloseStageClearResultToStageList()
        {
            if (stage?.CurrentStage != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[StageResultClose] stage={stage.CurrentStage.stageNumber} returnMode=currentProgress");
#endif
            }

            HideOverlay();
            stage?.ExitToStageList();
        }

        private bool IsReplayClearStage(int clearedStageNumber)
        {
            if (stage == null)
            {
                return false;
            }

            int currentProgressStage = stage.CurrentProgressStageNumber;
            return stage.PreviousBestStars > 0
                || (currentProgressStage > 0 && clearedStageNumber < currentProgressStage - 1);
        }

        private static void ConfigureOverlayButton(Button button, Vector2 anchoredPosition, Vector2 size)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.285f);
            rect.anchorMax = new Vector2(0.5f, 0.285f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void HideOverlay()
        {
            if (rankingPopupRoot != null)
            {
                rankingPopupRoot.SetActive(false);
            }

            if (weeklyRewardsPopupRoot != null)
            {
                weeklyRewardsPopupRoot.SetActive(false);
            }

            overlayRoot.SetActive(false);
        }

        private string GetBackNavigationScreenName()
        {
            switch (launchMode)
            {
                case GameLaunchMode.StagePlay:
                    return "StagePlay";
                case GameLaunchMode.RankingChallenge:
                    return "RankingChallenge";
                case GameLaunchMode.PracticeRanking:
                    return "Practice";
                default:
                    return "Game";
            }
        }

        private bool HandleAndroidBack()
        {
            if (isTransitioningToNextStage)
            {
                Debug.Log($"[BackNavigation] currentScreen={GetBackNavigationScreenName()} popupOpen=False action=IgnoredTransition");
                return true;
            }

            if (TryCloseTopPopupForBack())
            {
                Debug.Log("[BackNavigation] Popup closed");
                return true;
            }

            switch (launchMode)
            {
                case GameLaunchMode.StagePlay:
                    Debug.Log("[BackNavigation] StagePlay -> StageList");
                    stage?.ExitToStageList();
                    return true;
                case GameLaunchMode.RankingChallenge:
                    Debug.Log("[BackNavigation] RankingChallenge -> MainMenu");
                    SceneLoader.LoadMainMenu();
                    return true;
                case GameLaunchMode.PracticeRanking:
                    Debug.Log("[BackNavigation] Practice -> SolverLearn");
                    BackToSolverLearn();
                    return true;
                default:
                    Debug.Log("[BackNavigation] Game -> MainMenu");
                    SceneLoader.LoadMainMenu();
                    return true;
            }
        }

        private bool TryCloseTopPopupForBack()
        {
            if (overlayRoot == null || !overlayRoot.activeInHierarchy)
            {
                return false;
            }

            bool hasVisiblePopup = rankingPopupRoot != null && rankingPopupRoot.activeInHierarchy
                || weeklyRewardsPopupRoot != null && weeklyRewardsPopupRoot.activeInHierarchy
                || starConditionsRoot != null && starConditionsRoot.gameObject.activeInHierarchy
                || overlayPanel != null && overlayPanel.gameObject.activeInHierarchy;

            if (!hasVisiblePopup)
            {
                return false;
            }

            HideOverlay();
            return true;
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

        private void OpenGemItemsShop()
        {
            GameLaunchContext.RequestShopOnMainMenu("GemItems");
            SceneLoader.LoadMainMenu();
        }

        private void OpenPromotionShop()
        {
            GameLaunchContext.RequestShopOnMainMenu("Promotion");
            SceneLoader.LoadMainMenu();
        }

        private void BackToSolverLearn()
        {
            GameLaunchContext.RequestSolverLearnOnMainMenu();
            SceneLoader.LoadMainMenu();
        }

        private bool IsRankingStyleMode()
        {
            return launchMode == GameLaunchMode.RankingChallenge
                || launchMode == GameLaunchMode.PracticeRanking;
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
            if (button == null)
            {
                return;
            }

            Text subtitle = button.transform.Find("StageSubtitle")?.GetComponent<Text>();
            if (subtitle == null)
            {
                SetButtonLabel(button, label);
                return;
            }

            string[] parts = label.Split('\n');
            SetButtonLabel(button, parts[0]);
            subtitle.text = parts.Length > 1 ? $"{parts[0]} {parts[1]}" : string.Empty;
        }

        private void SetActionBadge(string name, string label)
        {
            Transform badge = actionRoot.Find(name)?.Find("CountBadge");
            Text text = badge != null ? badge.GetComponentInChildren<Text>() : null;
            if (text != null)
            {
                text.text = name.Contains("Plus")
                    ? $"({label})"
                    : label;
            }
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
            text.fontStyle = FontStyle.Bold;
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(2f, -3f);
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
                new Vector2(width, name.Contains("Plus") ? 126f : 94f));
        }

        private static Button CreateStageBottomButton(
            RectTransform parent,
            string name,
            string label,
            float horizontalAnchor,
            float y,
            float width,
            float height)
        {
            return CreateAnchoredButton(
                parent,
                name,
                label,
                new Vector2(horizontalAnchor, 0f),
                new Vector2(horizontalAnchor, 0f),
                new Vector2(0f, y),
                new Vector2(width, height));
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
            string iconKey = GetButtonIconKey(name);
            return CasualUIFactory.CreateActionButton(
                parent,
                name,
                label,
                iconKey,
                GetButtonTheme(name),
                anchor,
                pivot,
                position,
                size,
                size.y < 100f);
        }

        private static string GetButtonIconKey(string name)
        {
            if (name.Contains("Hint")) return "hint";
            if (name.Contains("Undo")) return "undo";
            if (name.Contains("Retry")) return "retry";
            if (name.Contains("Plus")) return "move";
            if (name.Contains("Scramble")) return "scramble";
            if (name.Contains("Start")) return "start";
            if (name.Contains("Rewards")) return "rewards";
            if (name.Contains("Ranking")) return "ranking";
            if (name.Contains("Menu")) return "menu";
            if (name.Contains("List")) return "list";
            if (name.Contains("Interaction")) return "view";
            return string.Empty;
        }

        private static void CreateCountBadge(Button button)
        {
            if (button == null || button.transform.Find("CountBadge") != null)
            {
                return;
            }

            CasualUIFactory.CreateBadge(button.transform);
        }

        private static void PolishStageHeader(Text title, Text moves, Text status)
        {
            if (title != null)
            {
                RectTransform titlePanel = title.transform.parent as RectTransform;
                Image titlePanelImage = titlePanel != null ? titlePanel.GetComponent<Image>() : null;
                if (titlePanelImage != null)
                {
                    titlePanelImage.color = Color.clear;
                }

                Outline titlePanelOutline = titlePanel != null ? titlePanel.GetComponent<Outline>() : null;
                if (titlePanelOutline != null)
                {
                    titlePanelOutline.enabled = false;
                }

                Shadow titlePanelShadow = titlePanel != null ? titlePanel.GetComponent<Shadow>() : null;
                if (titlePanelShadow != null)
                {
                    titlePanelShadow.enabled = false;
                }

                title.fontStyle = FontStyle.Bold;
                title.color = new Color(1f, 0.98f, 0.9f, 1f);
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 28;
                title.resizeTextMaxSize = 38;
                CasualUIStyle.ApplyTextDepth(title, true);

                if (titlePanel != null)
                {
                    CreateStageTitleAccent(titlePanel, "LeftAccent", false);
                    CreateStageTitleAccent(titlePanel, "RightAccent", true);
                }
            }

            if (moves != null)
            {
                Image movesPanel = moves.transform.parent.GetComponent<Image>();
                if (movesPanel != null)
                {
                    CasualUIStyle.ApplyPanel(
                        movesPanel,
                        new Color(0.055f, 0.05f, 0.12f, 0.9f),
                        28);
                }

                Outline movesOutline = moves.transform.parent.GetComponent<Outline>();
                if (movesOutline != null)
                {
                    movesOutline.effectColor = new Color(0.88f, 0.75f, 0.55f, 0.55f);
                    movesOutline.effectDistance = new Vector2(2f, -2f);
                }

                moves.fontSize = 30;
                moves.fontStyle = FontStyle.Bold;
                moves.color = new Color(1f, 0.97f, 0.88f, 1f);
            }

            if (status != null)
            {
                status.color = new Color(0.95f, 0.94f, 1f, 0.94f);
            }
        }

        private static void ConfigureStagePowerUpContent(Button button)
        {
            if (button == null)
            {
                return;
            }

            RectTransform iconHolder = button.transform.Find("IconHolder") as RectTransform;
            if (iconHolder != null)
            {
                iconHolder.anchorMin = new Vector2(0.035f, 0.16f);
                iconHolder.anchorMax = button.name.Contains("Plus")
                    ? new Vector2(0.27f, 0.82f)
                    : new Vector2(0.25f, 0.84f);
            }

            Text label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = button.name.Contains("Plus") ? 38 : 32;

            if (!button.name.Contains("Plus"))
            {
                label.rectTransform.anchorMin = new Vector2(0.22f, 0.05f);
                label.rectTransform.anchorMax = new Vector2(0.88f, 0.95f);
                return;
            }

            string[] parts = label.text.Split('\n');
            label.text = parts[0];
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = new Vector2(0.12f, 0.42f);
            label.rectTransform.anchorMax = new Vector2(0.88f, 0.9f);

            Text subtitle = RuntimeUiFactory.CreateText(
                button.GetComponent<RectTransform>(),
                "StageSubtitle",
                parts.Length > 1 ? $"{parts[0]} {parts[1]}" : string.Empty,
                21,
                TextAnchor.MiddleCenter);
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.color = new Color(1f, 1f, 1f, 0.92f);
            subtitle.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
            subtitle.rectTransform.anchorMax = new Vector2(0.94f, 0.33f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(subtitle, false);
        }

        private static void ApplyStagePlusItemSprite(Button button, string spriteName)
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
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;

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

            for (int i = 0; i < button.transform.childCount; i++)
            {
                Transform child = button.transform.GetChild(i);
                child.gameObject.SetActive(child.name == "CountBadge");
            }

            Transform badge = button.transform.Find("CountBadge");
            if (badge is RectTransform badgeRect)
            {
                badgeRect.gameObject.SetActive(true);
                badgeRect.anchorMin = new Vector2(0.89f, 0.44f);
                badgeRect.anchorMax = badgeRect.anchorMin;
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.anchoredPosition = Vector2.zero;
                badgeRect.sizeDelta = new Vector2(68f, 68f);
                Image badgeImage = badgeRect.GetComponent<Image>();
                if (badgeImage != null)
                {
                    CasualUIStyle.ApplyPanel(
                        badgeImage,
                        new Color(0.02f, 0.08f, 0.18f, 0.94f),
                        30);
                }

                Text badgeText = badgeRect.GetComponentInChildren<Text>();
                if (badgeText != null)
                {
                    badgeText.fontSize = 24;
                    badgeText.resizeTextForBestFit = true;
                    badgeText.resizeTextMinSize = 18;
                    badgeText.resizeTextMaxSize = 24;
                    badgeText.color = Color.white;
                }
                badge.SetAsLastSibling();
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.62f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static void ApplyStageButtonSprite(
            Button button,
            string spriteName,
            bool preserveBadge)
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
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;

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

            for (int i = 0; i < button.transform.childCount; i++)
            {
                Transform child = button.transform.GetChild(i);
                bool keep = preserveBadge && child.name == "CountBadge";
                child.gameObject.SetActive(keep);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.48f, 0.48f, 0.52f, 0.62f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static void ApplyRankingRewardsButtonSprite(Button button)
        {
            if (button == null)
            {
                return;
            }

            RectTransform iconHolder = button.transform.Find("IconHolder") as RectTransform;
            Text label = button.transform.Find("Label")?.GetComponent<Text>();
            if (iconHolder == null)
            {
                return;
            }

            for (int i = iconHolder.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(iconHolder.GetChild(i).gameObject);
            }

            Sprite sprite = CasualIconFactory.LoadUiSprite("UI/Stages/Generated/icon_chest");
            if (sprite == null)
            {
                return;
            }

            iconHolder.anchorMin = new Vector2(0.035f, 0.08f);
            iconHolder.anchorMax = new Vector2(0.31f, 0.92f);
            iconHolder.offsetMin = Vector2.zero;
            iconHolder.offsetMax = Vector2.zero;

            GameObject iconObject = new GameObject("ChestIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(iconHolder, false);
            RectTransform icon = iconObject.GetComponent<RectTransform>();
            icon.anchorMin = Vector2.zero;
            icon.anchorMax = Vector2.one;
            icon.offsetMin = new Vector2(2f, 2f);
            icon.offsetMax = new Vector2(-2f, -2f);

            Image image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;

            if (label != null)
            {
                label.text = "Rewards";
                label.rectTransform.anchorMin = new Vector2(0.29f, 0f);
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
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

        private static void CreateStageTitleAccent(
            RectTransform parent,
            string name,
            bool right)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            GameObject accentObject = new GameObject(name, typeof(RectTransform));
            accentObject.transform.SetParent(parent, false);
            RectTransform accent = accentObject.GetComponent<RectTransform>();
            accent.anchorMin = right
                ? new Vector2(0.78f, 0.5f)
                : new Vector2(0.02f, 0.5f);
            accent.anchorMax = right
                ? new Vector2(0.98f, 0.5f)
                : new Vector2(0.22f, 0.5f);
            accent.pivot = new Vector2(0.5f, 0.5f);
            accent.sizeDelta = new Vector2(0f, 18f);

            GameObject lineObject = new GameObject("Line", typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(accent, false);
            RectTransform line = lineObject.GetComponent<RectTransform>();
            line.anchorMin = new Vector2(right ? 0f : 0.12f, 0.5f);
            line.anchorMax = new Vector2(right ? 0.88f : 1f, 0.5f);
            line.sizeDelta = new Vector2(0f, 3f);
            Image lineImage = lineObject.GetComponent<Image>();
            lineImage.color = new Color(0.96f, 0.72f, 0.28f, 0.55f);
            lineImage.raycastTarget = false;

            GameObject diamondObject = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
            diamondObject.transform.SetParent(accent, false);
            RectTransform diamond = diamondObject.GetComponent<RectTransform>();
            float diamondAnchor = right ? 0.94f : 0.06f;
            diamond.anchorMin = new Vector2(diamondAnchor, 0.5f);
            diamond.anchorMax = new Vector2(diamondAnchor, 0.5f);
            diamond.sizeDelta = new Vector2(12f, 12f);
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image diamondImage = diamondObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(
                diamondImage,
                new Color(1f, 0.76f, 0.25f, 0.9f),
                3);
            diamondImage.raycastTarget = false;
        }

        private static void PolishStageActionButton(Button button, bool powerUp)
        {
            if (button == null)
            {
                return;
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = powerUp
                    ? new Color(1f, 0.86f, 0.32f, 0.98f)
                    : new Color(1f, 0.75f, 0.38f, 0.78f);
                outline.effectDistance = powerUp
                    ? new Vector2(4f, -4f)
                    : new Vector2(3f, -3f);
            }

            Shadow shadow = button.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = new Color(0.01f, 0.005f, 0.06f, powerUp ? 0.82f : 0.7f);
                shadow.effectDistance = new Vector2(0f, powerUp ? -11f : -8f);
            }

            if (!powerUp || button.transform.Find("PowerUpShine") != null)
            {
                return;
            }

            GameObject shineObject = new GameObject("PowerUpShine", typeof(RectTransform), typeof(Image));
            shineObject.transform.SetParent(button.transform, false);
            RectTransform shine = shineObject.GetComponent<RectTransform>();
            shine.anchorMin = new Vector2(0.08f, 0.62f);
            shine.anchorMax = new Vector2(0.92f, 0.9f);
            shine.offsetMin = Vector2.zero;
            shine.offsetMax = Vector2.zero;
            Image shineImage = shineObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(shineImage, new Color(1f, 1f, 0.82f, 0.16f), 16);
            shineImage.raycastTarget = false;
            shineObject.transform.SetAsFirstSibling();

            Transform badgeTransform = button.transform.Find("CountBadge");
            if (badgeTransform is RectTransform badge)
            {
                bool plusButton = button.name.Contains("Plus");
                badge.anchorMin = new Vector2(1f, 1f);
                badge.anchorMax = badge.anchorMin;
                badge.pivot = new Vector2(1f, 1f);
                badge.anchoredPosition = plusButton
                    ? new Vector2(-2f, -2f)
                    : new Vector2(-8f, -8f);
                badge.sizeDelta = button.name.Contains("Undo")
                    ? new Vector2(82f, 50f)
                    : (plusButton ? new Vector2(56f, 44f) : new Vector2(62f, 50f));
            }
        }

        private static CasualUIColor GetButtonTheme(string name)
        {
            if (name.Contains("Start") || name.Contains("Hint")) return CasualUIColor.Green;
            if (name.Contains("Ranking")) return CasualUIColor.Purple;
            if (name.Contains("Retry") || name.Contains("Plus3")) return CasualUIColor.Orange;
            if (name.Contains("Plus2")) return CasualUIColor.Purple;
            if (name.Contains("Plus1") || name.Contains("Scramble") || name.Contains("List")) return CasualUIColor.Blue;
            if (name.Contains("Menu") || name.Contains("Undo")) return CasualUIColor.Purple;
            if (name.Contains("Interaction")) return CasualUIColor.Blue;
            return CasualUIColor.Slate;
        }

        private static void TintGameplayCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.Lerp(
                camera.backgroundColor,
                CasualUIPalette.BackgroundTop,
                0.72f);
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
                case RankingChallengeState.Previewing: return "Starting automatically.";
                case RankingChallengeState.Playing: return "Challenge in progress";
                case RankingChallengeState.Solved: return "Challenge complete";
                default: return "Tap Scramble when ready";
            }
        }

        private static string GetPracticeRankingStatus(RankingChallengeState state)
        {
            switch (state)
            {
                case RankingChallengeState.Scrambling: return "Preparing practice...";
                case RankingChallengeState.Previewing: return "Starting practice...";
                case RankingChallengeState.Playing: return "Practice in progress";
                case RankingChallengeState.Solved: return "Practice complete";
                default: return "Tap Scramble to practice";
            }
        }

        private string GetStageStatusText()
        {
            if (stage == null)
            {
                return string.Empty;
            }

            if (stage.State == StagePlayState.Playing)
            {
                return stage.IsReverseTargetStage
                    ? "Match the target cube."
                    : "Solve the cube.";
            }

            return stage.StatusMessage;
        }

        private void RefreshStageStarsStatus()
        {
            if (stageStarsRoot == null)
            {
                return;
            }

            bool show = stage != null
                && stage.CurrentStage != null
                && stage.State != StagePlayState.Cleared
                && stage.State != StagePlayState.Failed;
            stageStarsRoot.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            int stars = Mathf.Clamp(stage.GetCurrentPotentialStars(), 1, 3);
            for (int i = 0; i < stageStarIcons.Count; i++)
            {
                Image starIcon = stageStarIcons[i];
                if (starIcon == null)
                {
                    continue;
                }

                bool visible = stars >= 3
                    || (stars == 2 && i < 2)
                    || (stars == 1 && i == 1);
                starIcon.gameObject.SetActive(visible);
            }

            if (stageStarInfoButton != null)
            {
                stageStarInfoButton.gameObject.SetActive(true);
            }
        }

        private void ShowStarConditionsOverlay()
        {
            if (stage == null || stage.CurrentStage == null)
            {
                return;
            }

            ShowOverlay(
                T("star_conditions_title"),
                string.Empty,
                T("ok"),
                HideOverlay,
                false);

            if (overlayBody != null)
            {
                overlayBody.gameObject.SetActive(false);
            }

            ApplyOverlayPanelColors(
                HexColor(0x07, 0x1A, 0x34, 0xFE),
                HexColor(0xE3, 0xA2, 0x1A, 0xF5));
            PopulateStarConditionsRows(stage.CurrentStage);
            ConfigureOverlayButton(overlayPrimaryButton, Vector2.zero, new Vector2(260f, 82f));
        }

        private void PopulateStarConditionsRows(StageData stageData)
        {
            if (starConditionsRoot == null || stageData == null)
            {
                return;
            }

            for (int i = starConditionsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(starConditionsRoot.GetChild(i).gameObject);
            }

            int minMoves = stageData.minimumMoves > 0 ? stageData.minimumMoves : stageData.minMoveCount;
            int threeStarLimit = stageData.starMoveLimit3 > 0 ? stageData.starMoveLimit3 : minMoves;
            int twoStarLimit = stageData.starMoveLimit2 > 0 ? stageData.starMoveLimit2 : minMoves + 2;
            int oneStarLimit = stageData.starMoveLimit1 > 0 ? stageData.starMoveLimit1 : stageData.moveLimit;

            CreateStarConditionRow(
                0,
                3,
                string.Format(T("star_title_count"), 3),
                string.Format(T("star_condition_3"), threeStarLimit));
            CreateStarConditionRow(
                1,
                2,
                string.Format(T("star_title_count"), 2),
                string.Format(T("star_condition_2"), twoStarLimit));
            CreateStarConditionRow(
                2,
                1,
                string.Format(T("star_title_one"), 1),
                string.Format(T("star_condition_1"), oneStarLimit));
            starConditionsRoot.gameObject.SetActive(true);
        }

        private void CreateStarConditionRow(int rowIndex, int starCount, string title, string description)
        {
            float rowHeight = 1f / 3f;
            float yMax = 1f - rowIndex * rowHeight - 0.012f;
            float yMin = 1f - (rowIndex + 1) * rowHeight + 0.012f;
            RectTransform row = CreateChildRect(
                starConditionsRoot,
                $"ConditionRow_{starCount}Stars",
                new Vector2(0f, yMin),
                new Vector2(1f, yMax));
            row.anchorMin = new Vector2(0f, yMin);
            row.anchorMax = new Vector2(1f, yMax);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;

            RectTransform iconArea = CreateChildRect(row, "LeftIconArea", new Vector2(0f, 0f), new Vector2(0.34f, 1f));
            BuildConditionStars(iconArea, starCount);

            Text titleText = RuntimeUiFactory.CreateText(row, "ConditionTitle", title, 32, TextAnchor.MiddleLeft);
            titleText.rectTransform.anchorMin = new Vector2(0.365f, 0.52f);
            titleText.rectTransform.anchorMax = new Vector2(0.98f, 0.94f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = HexColor(0xFF, 0xF4, 0xD6);
            CasualUIStyle.ApplyTextDepth(titleText, true);

            Text descriptionText = RuntimeUiFactory.CreateText(row, "ConditionDescription", description, 27, TextAnchor.MiddleLeft);
            descriptionText.rectTransform.anchorMin = new Vector2(0.365f, 0.08f);
            descriptionText.rectTransform.anchorMax = new Vector2(0.98f, 0.58f);
            descriptionText.rectTransform.offsetMin = Vector2.zero;
            descriptionText.rectTransform.offsetMax = Vector2.zero;
            descriptionText.color = HexColor(0xE8, 0xEE, 0xF7);
            descriptionText.lineSpacing = 0.92f;
            CasualUIStyle.ApplyTextDepth(descriptionText, true);

            if (rowIndex < 2)
            {
                CreateStarConditionDivider(row);
            }
        }

        private static void CreateStarConditionDivider(RectTransform row)
        {
            GameObject dividerObject = new GameObject("ConditionDivider", typeof(RectTransform), typeof(Image));
            dividerObject.transform.SetParent(row, false);
            RectTransform divider = dividerObject.GetComponent<RectTransform>();
            divider.anchorMin = new Vector2(0.06f, 0f);
            divider.anchorMax = new Vector2(0.94f, 0f);
            divider.pivot = new Vector2(0.5f, 0.5f);
            divider.anchoredPosition = new Vector2(0f, -2f);
            divider.sizeDelta = new Vector2(0f, 3f);

            Image image = dividerObject.GetComponent<Image>();
            image.color = HexColor(0xE3, 0xA2, 0x1A, 0xAA);
            image.raycastTarget = false;
        }

        private RectTransform CreateChildRect(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void BuildConditionStars(RectTransform parent, int starCount)
        {
            float starSize = Mathf.Max(30f, conditionStarIconSize);
            float spacing = 6f;
            float totalWidth = starCount * starSize + Mathf.Max(0, starCount - 1) * spacing;
            float startX = -totalWidth * 0.5f + starSize * 0.5f;

            for (int i = 0; i < starCount; i++)
            {
                GameObject starObject = new GameObject($"Star{i + 1}", typeof(RectTransform), typeof(Image));
                starObject.transform.SetParent(parent, false);
                RectTransform starRect = starObject.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                starRect.anchoredPosition = new Vector2(startX + i * (starSize + spacing), 0f);
                starRect.sizeDelta = new Vector2(starSize, starSize);

                Image starImage = starObject.GetComponent<Image>();
                starImage.raycastTarget = false;
                ApplyStageStarSprite(starImage);
            }
        }

        private void SetOverlayStarsVisible(int stars)
        {
            if (overlayStarsRoot == null)
            {
                return;
            }

            int clampedStars = Mathf.Clamp(stars, 0, 3);
            overlayStarsRoot.gameObject.SetActive(clampedStars > 0);
            for (int i = 0; i < overlayStarIcons.Count; i++)
            {
                Image starIcon = overlayStarIcons[i];
                if (starIcon == null)
                {
                    continue;
                }

                starIcon.gameObject.SetActive(i < clampedStars);
            }
        }

        private static void ApplyStageStarSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = LoadStageHudSprite("star_row_1") ?? LoadStageHudSprite("icon_star_large");
            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        private static Sprite LoadStageHudSprite(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return null;
            }

            if (StageHudSpriteCache.TryGetValue(spriteName, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(StageGeneratedArtPath + spriteName);
            if (texture == null)
            {
                StageHudSpriteCache[spriteName] = null;
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
            StageHudSpriteCache[spriteName] = sprite;
            return sprite;
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remaining = seconds - minutes * 60f;
            return $"{minutes:00}:{remaining:00.00}";
        }

        private static string T(string key)
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key;
        }

        private sealed class WeeklyRankingRewardsButtonAttention : MonoBehaviour
        {
            private RectTransform rect;
            private Image image;
            private Vector2 basePosition;
            private Vector3 baseScale;
            private Color baseColor = Color.white;
            private bool active;

            private void Awake()
            {
                rect = GetComponent<RectTransform>();
                image = GetComponent<Image>();
                if (rect != null)
                {
                    basePosition = rect.anchoredPosition;
                    baseScale = rect.localScale;
                }

                if (image != null)
                {
                    baseColor = image.color;
                }
            }

            public void SetActive(bool value)
            {
                if (active == value)
                {
                    return;
                }

                active = value;
                if (!active)
                {
                    if (rect != null)
                    {
                        rect.anchoredPosition = basePosition;
                        rect.localScale = baseScale;
                    }

                    if (image != null)
                    {
                        image.color = baseColor;
                    }
                }
                else
                {
                    if (rect != null)
                    {
                        basePosition = rect.anchoredPosition;
                        baseScale = rect.localScale;
                    }

                    if (image != null)
                    {
                        baseColor = image.color;
                    }
                }
            }

            private void Update()
            {
                if (!active || rect == null)
                {
                    return;
                }

                float shake = Mathf.Sin(Time.unscaledTime * 5.2f);
                float pulse = (Mathf.Sin(Time.unscaledTime * 3.8f) + 1f) * 0.5f;
                rect.anchoredPosition = basePosition + new Vector2(shake * 2.4f, 0f);
                rect.localScale = baseScale * Mathf.Lerp(1f, 1.025f, pulse);
                if (image != null)
                {
                    Color lit = new Color(
                        Mathf.Clamp01(baseColor.r + 0.08f),
                        Mathf.Clamp01(baseColor.g + 0.06f),
                        Mathf.Clamp01(baseColor.b + 0.03f),
                        baseColor.a);
                    image.color = Color.Lerp(baseColor, lit, pulse * 0.55f);
                }
            }
        }
    }
}
