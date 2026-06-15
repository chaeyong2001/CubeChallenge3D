using System.Collections.Generic;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Rewards
{
    public sealed class RewardsPanelUI
    {
        private readonly GameObject root;
        private readonly WalletStore walletStore;
        private readonly InventoryStore inventoryStore;
        private readonly DailyRewardStore dailyRewardStore;
        private readonly StageProgressStore progressStore;
        private readonly StageMilestoneRewardStore milestoneStore;
        private readonly StageDataLoader stageLoader = new StageDataLoader();
        private readonly RewardedAdService rewardService;
        private readonly Text walletText;
        private readonly Text dailyText;
        private readonly Text milestoneText;
        private readonly Text statusText;
        private readonly Button dailyClaimButton;
        private readonly Button milestoneClaimButton;
        private readonly Button watchAdButton;
        private readonly Button solverTicketAdButton;
        private readonly Button debugResetDailyButton;
        private readonly Vector2 minPanelSize = new Vector2(640f, 620f);
        private readonly Vector2 maxPanelSize = new Vector2(920f, 1040f);

        public RewardsPanelUI(
            Transform parent,
            WalletStore wallet,
            InventoryStore inventory,
            StageProgressStore progress,
            StageMilestoneRewardStore milestones,
            RewardedAdService ads = null)
        {
            walletStore = wallet ?? new WalletStore();
            inventoryStore = inventory ?? new InventoryStore();
            progressStore = progress ?? new StageProgressStore();
            milestoneStore = milestones ?? new StageMilestoneRewardStore();
            dailyRewardStore = new DailyRewardStore();
            rewardService = ads ?? RewardedAdService.CreateDefault();

            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "RewardsCanvas", 1490);
            root = canvas.gameObject;

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "RewardsPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 760f));

            AddDragBar(panel);
            AddResizeHandle(panel);
            AddCloseButton(panel);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Rewards", 38, TextAnchor.UpperCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            title.rectTransform.sizeDelta = new Vector2(-120f, 56f);

            walletText = CreateTextBlock(panel, "WalletText", new Vector2(0f, -132f), 70f, 24);
            dailyText = CreateTextBlock(panel, "DailyText", new Vector2(0f, -220f), 120f, 24);
            milestoneText = CreateTextBlock(panel, "MilestoneText", new Vector2(0f, -370f), 128f, 24);
            CreateTextBlock(panel, "AdRewardText", new Vector2(0f, -510f), 78f, 22).text =
                "Rewarded Ads\nCoins and Solver Tickets";
            statusText = CreateTextBlock(panel, "StatusText", new Vector2(0f, -606f), 48f, 21);

            dailyClaimButton = RuntimeUiFactory.CreateButton(panel, "DailyClaimButton", "Claim Daily", new Vector2(-170f, 160f), new Vector2(260f, 56f));
            dailyClaimButton.onClick.AddListener(ClaimDaily);
            milestoneClaimButton = RuntimeUiFactory.CreateButton(panel, "MilestoneClaimButton", "Claim Milestone", new Vector2(170f, 160f), new Vector2(280f, 56f));
            milestoneClaimButton.onClick.AddListener(ClaimMilestone);
            watchAdButton = RuntimeUiFactory.CreateButton(panel, "WatchAdRewardButton", "Watch Ad for +100 Coins", new Vector2(-176f, 94f), new Vector2(330f, 56f));
            watchAdButton.onClick.AddListener(WatchAdForCoins);
            solverTicketAdButton = RuntimeUiFactory.CreateButton(panel, "SolverTicketAdButton", "Watch Ad for 1 Ticket", new Vector2(176f, 94f), new Vector2(330f, 56f));
            solverTicketAdButton.onClick.AddListener(WatchAdForSolverTicket);
            debugResetDailyButton = RuntimeUiFactory.CreateButton(panel, "DebugResetDailyButton", "DEV Reset Daily", new Vector2(0f, 28f), new Vector2(280f, 48f));
            debugResetDailyButton.onClick.AddListener(DebugResetDaily);
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            debugResetDailyButton.gameObject.SetActive(false);
#endif

            Hide();
        }

        public void Show()
        {
            Refresh(string.Empty);
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private Text CreateTextBlock(RectTransform parent, string name, Vector2 position, float height, int fontSize)
        {
            Text text = RuntimeUiFactory.CreateText(parent, name, string.Empty, fontSize, TextAnchor.UpperCenter);
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = new Vector2(-80f, height);
            return text;
        }

        private void Refresh(string message)
        {
            IReadOnlyList<StageData> stages = stageLoader.LoadAllStages();
            progressStore.EnsureStageDefaults(stages);
            milestoneStore.EnsureMilestones(stages);

            walletText.text = $"Solver Tickets  {inventoryStore.SolverTickets}";
            bool canClaimDaily = dailyRewardStore.CanClaim(System.DateTime.UtcNow);
            dailyText.text = $"Daily Reward\n{dailyRewardStore.GetRewardDescription()}\n{(canClaimDaily ? "Claim" : "Claimed. Come back tomorrow.")}";
            dailyClaimButton.interactable = canClaimDaily;
            int coinAdsLeft = rewardService.GetRemaining(RewardedAdPlacement.DailyCoins);
            int ticketAdsLeft = rewardService.GetRemaining(RewardedAdPlacement.SolverBonusTicket);
            SetButtonLabel(
                watchAdButton,
                rewardService.IsPlacementAvailable(RewardedAdPlacement.DailyCoins)
                    ? $"Watch Ad for +{rewardService.Config.dailyCoinRewardAmount} Coins\n{coinAdsLeft}/{rewardService.Config.dailyCoinAdsMax} left"
                    : "Ad not available");
            SetButtonLabel(
                solverTicketAdButton,
                rewardService.IsPlacementAvailable(RewardedAdPlacement.SolverBonusTicket)
                    ? $"Watch Ad for {rewardService.Config.solverTicketRewardAmount} Solver Ticket\n{ticketAdsLeft}/{rewardService.Config.dailySolverTicketAdsMax} left"
                    : "Ad not available");
            watchAdButton.interactable = rewardService.CanShow(RewardedAdPlacement.DailyCoins);
            solverTicketAdButton.interactable = rewardService.CanShow(RewardedAdPlacement.SolverBonusTicket);

            StageMilestoneReward milestone = GetFirstMilestone(stages);
            if (milestone != null)
            {
                int stars = milestoneStore.GetBlockStars(milestone, stages, progressStore);
                milestoneText.text = $"Milestone Reward\nStages {milestone.startStageNumber}-{milestone.endStageNumber}: {stars}/{milestone.requiredStars} stars\nReward: {milestone.rewardGems} gems | {(milestone.isClaimed ? "Claimed" : "Unclaimed")}";
                milestoneClaimButton.interactable = !milestone.isClaimed && stars >= milestone.requiredStars;
            }
            else
            {
                milestoneText.text = "Milestone Reward\nNo milestone data.";
                milestoneClaimButton.interactable = false;
            }

            statusText.text = message;
        }

        private void ClaimDaily()
        {
            bool claimed = dailyRewardStore.TryClaim(System.DateTime.UtcNow, walletStore, inventoryStore);
            Refresh(claimed ? "Daily reward claimed." : "Daily reward is not available.");
        }

        private void ClaimMilestone()
        {
            IReadOnlyList<StageData> stages = stageLoader.LoadAllStages();
            StageMilestoneReward milestone = GetFirstMilestone(stages);
            if (milestone == null)
            {
                Refresh("No milestone reward.");
                return;
            }

            bool claimed = milestoneStore.TryClaimBlock(milestone.blockIndex, stages, progressStore, walletStore);
            Refresh(claimed ? "Milestone reward claimed." : "Milestone is not ready.");
        }

        private void DebugResetDaily()
        {
            dailyRewardStore.ResetForDebug();
            rewardService.ResetLimitsForDebug();
            Refresh("Daily reward and ad limits reset for debug.");
        }

        private void WatchAdForCoins()
        {
            if (!rewardService.CanShow(RewardedAdPlacement.DailyCoins))
            {
                Refresh(BuildUnavailableMessage(RewardedAdPlacement.DailyCoins));
                return;
            }

            rewardService.Show(
                RewardedAdPlacement.DailyCoins,
                () =>
                {
                    walletStore.AddCoins(rewardService.Config.dailyCoinRewardAmount);
                },
                result =>
                {
                    Refresh(result == RewardedAdResult.Rewarded
                        ? UIStrings.RewardClaimed
                        : BuildResultMessage(result));
                });
        }

        private void WatchAdForSolverTicket()
        {
            if (!rewardService.CanShow(RewardedAdPlacement.SolverBonusTicket))
            {
                Refresh(BuildUnavailableMessage(RewardedAdPlacement.SolverBonusTicket));
                return;
            }

            rewardService.Show(
                RewardedAdPlacement.SolverBonusTicket,
                () =>
                {
                    inventoryStore.Add(StageAssistItemType.SolverTicket, rewardService.Config.solverTicketRewardAmount);
                },
                result =>
                {
                    Refresh(result == RewardedAdResult.Rewarded
                        ? UIStrings.RewardClaimed
                        : BuildResultMessage(result));
                });
        }

        private string BuildUnavailableMessage(RewardedAdPlacement placement)
        {
            return rewardService.GetUnavailableMessage(placement);
        }

        private static string BuildResultMessage(RewardedAdResult result)
        {
            return result == RewardedAdResult.NotReady
                ? UIStrings.AdNotReady
                : result == RewardedAdResult.Unavailable
                    ? UIStrings.AdsUnavailable
                    : UIStrings.AdNotCompleted;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label != null)
            {
                label.fontSize = 20;
                label.text = value;
            }
        }

        private StageMilestoneReward GetFirstMilestone(IReadOnlyList<StageData> stages)
        {
            StageMilestoneReward first = null;
            foreach (StageMilestoneReward reward in milestoneStore.EnsureMilestones(stages))
            {
                if (reward.isClaimed)
                {
                    continue;
                }

                first ??= reward;
                if (milestoneStore.GetBlockStars(reward, stages, progressStore) >= reward.requiredStars)
                {
                    return reward;
                }
            }

            return first;
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
            handleObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
            handleObject.AddComponent<PanelResizeHandle>().Initialize(parent, minPanelSize, maxPanelSize);
        }

        private void AddCloseButton(RectTransform parent)
        {
            Button button = RuntimeUiFactory.CreateButton(parent, "TopCloseButton", "X", new Vector2(-14f, -52f), new Vector2(54f, 46f));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            button.onClick.AddListener(Hide);
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
    }
}
