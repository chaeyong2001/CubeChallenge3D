using System;
using System.Collections.Generic;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Rewards
{
    public sealed class RewardsPanelUI
    {
        private readonly GameObject root;
        private readonly WalletStore walletStore;
        private readonly InventoryStore inventoryStore;
        private readonly DailyRewardStore dailyRewardStore;
        private readonly RewardedAdService rewardService;
        private readonly List<RuntimeUiFactory.RewardCardView> dailyCards = new List<RuntimeUiFactory.RewardCardView>();
        private readonly RuntimeUiFactory.RewardCardView daySevenCard;
        private RectTransform rewardsViewport;
        private readonly RectTransform rewardsSection;
        private readonly RectTransform bottomButtonsRoot;
        private readonly TopCurrencyBar topCurrencyBar;
        private readonly Text statusText;
        private readonly Button mainMenuButton;
        private readonly Dictionary<RectTransform, RectTransform> claimedCheckRoots = new Dictionary<RectTransform, RectTransform>();
        private RectTransform claimPopupRoot;
        private Image claimBaseIcon;
        private Text claimBaseAmountText;
        private Image claimSolverIcon;
        private Text claimSolverAmountText;
        private Button claimPopupOkButton;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly Button debugNextDayButton;
#endif

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
            dailyRewardStore = new DailyRewardStore();
            rewardService = ads ?? RewardedAdService.CreateDefault();

            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "RewardsUtilityCanvas", 1490, 0f);
            topCurrencyBar = TopCurrencyBar.Attach(canvas, null, true, null, null, null);
            root = canvas.gameObject;

            CasualUIFactory.CreateBackdrop(root.transform, "RewardsBackdrop", true, false);
            RectTransform safeArea = CreateSafeArea(root.transform);
            RectTransform panel = CreateRootPanel(safeArea);

            CreateTitle(panel);
            CreateSubtitle(panel);

            rewardsSection = CreateRewardsSection(panel);
            BuildDailyCards(rewardsSection);
            daySevenCard = CreateRewardCard(rewardsSection, 7, true);

            statusText = RuntimeUiFactory.CreateText(panel, "Status", string.Empty, 22, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0.06f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(0.94f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0f, 226f);
            statusText.rectTransform.sizeDelta = new Vector2(0f, 42f);
            statusText.color = new Color(0.86f, 0.89f, 0.98f, 0.92f);
            CasualUIStyle.ApplyTextDepth(statusText, false);
            statusText.gameObject.SetActive(false);

            bottomButtonsRoot = CreateBottomButtonsRoot(panel);
            mainMenuButton = RuntimeUiFactory.CreateButton(
                bottomButtonsRoot,
                "BackButton",
                "Back",
                Vector2.zero,
                RewardsLayout.MainMenuButtonSize);
            CasualUIStyle.ApplyButton(mainMenuButton, CasualUIColor.Blue);
            SetButtonLabel(mainMenuButton, "Back");
            Text mainMenuText = mainMenuButton.GetComponentInChildren<Text>();
            if (mainMenuText != null)
            {
                mainMenuText.fontSize = 34;
                mainMenuText.resizeTextMaxSize = 34;
                mainMenuText.resizeTextMinSize = 24;
            }
            mainMenuButton.onClick.AddListener(Hide);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugNextDayButton = RuntimeUiFactory.CreateButton(panel, "DebugNextDailyRewardButton", "Debug Next Day", new Vector2(0f, 176f), new Vector2(272f, 54f));
            RectTransform debugRect = debugNextDayButton.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(0.5f, 0f);
            debugRect.anchorMax = new Vector2(0.5f, 0f);
            debugRect.pivot = new Vector2(0.5f, 0f);
            debugNextDayButton.onClick.AddListener(DebugMakeCurrentRewardClaimable);
#endif

            ApplyRewardsFixedLayout();

            Hide();
        }

        public void SetTopHudActions(
            UnityAction shopAction,
            UnityAction heartPlusAction,
            UnityAction coinPlusAction,
            UnityAction gemPlusAction)
        {
            topCurrencyBar?.SetActions(shopAction, heartPlusAction, coinPlusAction, gemPlusAction);
        }

        private void ApplyRewardsFixedLayout()
        {
            float footerTopInset = RewardsLayout.BottomButtonsBottom + RewardsLayout.BottomButtonsHeight + RewardsLayout.DaySevenButtonMinGap;
            rewardsViewport.anchorMin = new Vector2(0.02f, 0f);
            rewardsViewport.anchorMax = new Vector2(0.98f, 1f);
            rewardsViewport.pivot = new Vector2(0.5f, 1f);
            rewardsViewport.offsetMin = new Vector2(0f, footerTopInset);
            rewardsViewport.offsetMax = new Vector2(0f, -RewardsLayout.RewardsSectionTop);

            rewardsSection.anchorMin = new Vector2(0f, 1f);
            rewardsSection.anchorMax = new Vector2(1f, 1f);
            rewardsSection.pivot = new Vector2(0.5f, 1f);
            rewardsSection.anchoredPosition = Vector2.zero;
            rewardsSection.sizeDelta = new Vector2(0f, RewardsLayout.RewardsSectionHeight);

            if (RewardsLayout.PreviewDayOneOnly)
            {
                ApplyDayOnePreviewLayout();
                return;
            }

            if (RewardsLayout.PreviewDaySevenOnly)
            {
                ApplyDaySevenPreviewLayout();
                return;
            }

            for (int i = 0; i < dailyCards.Count; i++)
            {
                RectTransform rect = dailyCards[i].Root;
                rect.gameObject.SetActive(true);
                int column = i % 2;
                int row = i / 2;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.localScale = Vector3.one * RewardsLayout.DailyCardScale;
                rect.anchoredPosition = new Vector2(
                    RewardsLayout.DailyStartX + (column * RewardsLayout.DailyColumnGap),
                    RewardsLayout.DailyStartY - (row * RewardsLayout.DailyRowGap));
                rect.sizeDelta = RewardsLayout.DailyCardSize;
            }

            daySevenCard.Root.anchorMin = new Vector2(0.5f, 1f);
            daySevenCard.Root.anchorMax = new Vector2(0.5f, 1f);
            daySevenCard.Root.pivot = new Vector2(0.5f, 1f);
            daySevenCard.Root.gameObject.SetActive(true);
            daySevenCard.Root.localScale = Vector3.one * RewardsLayout.DaySevenScale;
            daySevenCard.Root.anchoredPosition = new Vector2(0f, -RewardsLayout.DaySevenTop);
            daySevenCard.Root.sizeDelta = RewardsLayout.DaySevenSize;

            float daySevenBottomFromTop = RewardsLayout.RewardsSectionTop + RewardsLayout.DaySevenTop + (RewardsLayout.DaySevenSize.y * RewardsLayout.DaySevenScale);
            float bottomButtonsTopFromTop = RewardsLayout.ReferenceHeight - RewardsLayout.BottomButtonsBottom - RewardsLayout.BottomButtonsHeight;
            if (daySevenBottomFromTop > bottomButtonsTopFromTop - RewardsLayout.DaySevenButtonMinGap)
            {
                float clampedTop = bottomButtonsTopFromTop - RewardsLayout.DaySevenButtonMinGap - (RewardsLayout.DaySevenSize.y * RewardsLayout.DaySevenScale) - RewardsLayout.RewardsSectionTop;
                daySevenCard.Root.anchoredPosition = new Vector2(0f, -clampedTop);
            }

            bottomButtonsRoot.anchorMin = new Vector2(0.03f, 0f);
            bottomButtonsRoot.anchorMax = new Vector2(0.97f, 0f);
            bottomButtonsRoot.pivot = new Vector2(0.5f, 0f);
            bottomButtonsRoot.gameObject.SetActive(true);
            bottomButtonsRoot.localScale = Vector3.one;
            bottomButtonsRoot.anchoredPosition = new Vector2(0f, RewardsLayout.BottomButtonsBottom);
            bottomButtonsRoot.sizeDelta = new Vector2(0f, RewardsLayout.BottomButtonsHeight);

            RectTransform menuRect = mainMenuButton.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.5f, 0f);
            menuRect.anchorMax = new Vector2(0.5f, 0f);
            menuRect.pivot = new Vector2(0.5f, 0f);
            menuRect.anchoredPosition = Vector2.zero;
            menuRect.sizeDelta = RewardsLayout.MainMenuButtonSize;
        }

        private void ApplyDayOnePreviewLayout()
        {
            for (int i = 0; i < dailyCards.Count; i++)
            {
                RuntimeUiFactory.RewardCardView card = dailyCards[i];
                bool showCard = i == 0;
                card.Root.gameObject.SetActive(showCard);
                if (!showCard)
                {
                    continue;
                }

                RectTransform rect = card.Root;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.localScale = Vector3.one;
                rect.anchoredPosition = RewardsLayout.DayOnePreviewPosition;
                rect.sizeDelta = RewardsLayout.DayOnePreviewCardSize;
            }

            daySevenCard.Root.gameObject.SetActive(false);
            bottomButtonsRoot.gameObject.SetActive(false);
        }

        private void ApplyDaySevenPreviewLayout()
        {
            foreach (RuntimeUiFactory.RewardCardView card in dailyCards)
            {
                card.Root.gameObject.SetActive(false);
            }

            daySevenCard.Root.gameObject.SetActive(true);
            daySevenCard.Root.anchorMin = new Vector2(0.5f, 1f);
            daySevenCard.Root.anchorMax = new Vector2(0.5f, 1f);
            daySevenCard.Root.pivot = new Vector2(0.5f, 1f);
            daySevenCard.Root.localScale = Vector3.one;
            daySevenCard.Root.anchoredPosition = RewardsLayout.DaySevenPreviewPosition;
            daySevenCard.Root.sizeDelta = RewardsLayout.DaySevenPreviewCardSize;

            bottomButtonsRoot.gameObject.SetActive(false);
        }

        public void Show()
        {
            Refresh(null);
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private void BuildDailyCards(RectTransform parent)
        {
            for (int i = 0; i < 6; i++)
            {
                int day = i + 1;
                RuntimeUiFactory.RewardCardView card = CreateRewardCard(parent, day, false);
                dailyCards.Add(card);
            }
        }

        private static class RewardsLayout
        {
            public const bool PreviewDayOneOnly = false;
            public const bool PreviewDaySevenOnly = false;
            public const float ReferenceHeight = 1920f;
            public const float RewardsSectionTop = 420f;
            public const float RewardsSectionHeight = 1370f;
            public const float DailyStartX = -208f;
            public const float DailyStartY = 0f;
            public const float DailyColumnGap = 416f;
            public const float DailyRowGap = 332f;
            public const float DailyCardScale = 0.5f;
            public const float DaySevenTop = 960f;
            public const float DaySevenScale = 0.85f;
            public const float BottomButtonsBottom = 52f;
            public const float BottomButtonsHeight = 78f;
            public const float DaySevenButtonMinGap = 20f;

            public static readonly Vector2 DailyCardSize = new Vector2(800f, 640f);
            public static readonly Vector2 DayOnePreviewCardSize = new Vector2(580f, 640f);
            public static readonly Vector2 DayOnePreviewPosition = new Vector2(0f, -150f);
            public static readonly Vector2 DaySevenPreviewCardSize = new Vector2(960f, 410f);
            public static readonly Vector2 DaySevenPreviewPosition = new Vector2(0f, -260f);
            public static readonly Vector2 DaySevenSize = new Vector2(960f, 410f);
            public static readonly Vector2 WatchAdButtonSize = new Vector2(580f, 136f);
            public static readonly Vector2 MainMenuButtonSize = new Vector2(360f, 70f);
        }

        private void Refresh(string message)
        {
            bool canClaim = dailyRewardStore.CanClaim(DateTime.UtcNow);
            int currentDay = dailyRewardStore.CurrentDayNumber;
            int claimedUpTo = canClaim ? currentDay - 1 : (currentDay == 1 ? 7 : currentDay - 1);

            for (int i = 0; i < dailyCards.Count; i++)
            {
                int day = i + 1;
                RewardCardState state = ResolveState(day, currentDay, claimedUpTo, canClaim);
                ConfigureCard(dailyCards[i], day, state, false);
            }

            ConfigureCard(daySevenCard, 7, ResolveState(7, currentDay, claimedUpTo, canClaim), true);

            bool hasMessage = !string.IsNullOrEmpty(message);
            statusText.gameObject.SetActive(hasMessage);
            statusText.text = hasMessage ? message : string.Empty;
        }

        private RewardCardState ResolveState(int day, int currentDay, int claimedUpTo, bool canClaim)
        {
            if (canClaim)
            {
                if (day < currentDay)
                {
                    return RewardCardState.Claimed;
                }

                return day == currentDay ? RewardCardState.Claimable : RewardCardState.Locked;
            }

            return day <= claimedUpTo ? RewardCardState.Claimed : RewardCardState.Locked;
        }

        private void ConfigureCard(RuntimeUiFactory.RewardCardView card, int day, RewardCardState state, bool special)
        {
            DailyRewardDefinition reward = dailyRewardStore.GetRewardForDay(day);
            card.DayLabel.text = $"Day {day}";
            card.RewardLabel.text = BuildRewardLabel(reward);
            card.RewardLabel.fontSize = special ? 84 : 68;
            card.RewardLabel.resizeTextMinSize = special ? 52 : 44;
            card.RewardLabel.resizeTextMaxSize = special ? 84 : 68;
            card.IconImage.sprite = ResolveRewardSprite(reward.type);
            card.IconImage.preserveAspect = true;
            card.IconImage.color = Color.white;
            SetAttention(card.Root, state == RewardCardState.Claimable);

            card.Image.color = card.UsesArtBackground
                ? Color.white
                : special
                    ? (state == RewardCardState.Claimable ? new Color(0.39f, 0.09f, 0.60f, 1f) : new Color(0.25f, 0.06f, 0.40f, 1f))
                    : (state == RewardCardState.Claimable ? new Color(0.04f, 0.20f, 0.55f, 1f) : new Color(0.03f, 0.12f, 0.34f, 1f));

            if (card.IconGlow != null)
            {
                card.IconGlow.color = special
                    ? new Color(0.95f, 0.32f, 1f, state == RewardCardState.Claimable ? 0.28f : 0.20f)
                    : new Color(0.30f, 0.70f, 1f, state == RewardCardState.Claimable ? 0.19f : 0.135f);
            }

            if (card.RewardBarImage != null)
            {
                card.RewardBarImage.color = special
                    ? new Color(0.035f, 0.012f, 0.07f, 0.88f)
                    : new Color(0.012f, 0.018f, 0.055f, 0.84f);
            }

            if (state == RewardCardState.Claimed)
            {
                card.StatusBadge.Root.gameObject.SetActive(false);
                SetClaimedCheckVisible(card.Root, true);
                card.IconImage.color = new Color(1f, 1f, 1f, 0.58f);
                card.RewardLabel.color = new Color(0.84f, 0.86f, 0.93f, 0.70f);
                card.DayLabel.color = new Color(1f, 0.96f, 0.73f, 0.70f);
            }
            else if (state == RewardCardState.Claimable)
            {
                card.StatusBadge.Root.gameObject.SetActive(true);
                SetClaimedCheckVisible(card.Root, false);
                RuntimeUiFactory.SetStatusBadge(card.StatusBadge, "TODAY", RuntimeUiFactory.StatusBadgeStyle.Today);
                card.RewardLabel.color = new Color(1f, 0.96f, 0.82f, 1f);
                card.DayLabel.color = new Color(1f, 0.96f, 0.73f, 1f);
            }
            else
            {
                card.StatusBadge.Root.gameObject.SetActive(true);
                SetClaimedCheckVisible(card.Root, false);
                RuntimeUiFactory.SetStatusBadge(card.StatusBadge, "LOCKED", RuntimeUiFactory.StatusBadgeStyle.Locked);
                card.RewardLabel.color = new Color(0.78f, 0.81f, 0.90f, 0.72f);
                card.DayLabel.color = new Color(0.88f, 0.90f, 0.98f, 0.78f);
            }

            card.Button.interactable = true;
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() => OnRewardCardPressed(day, state));
        }

        private void OnRewardCardPressed(int day, RewardCardState state)
        {
            if (state != RewardCardState.Claimable
                || day != dailyRewardStore.CurrentDayNumber
                || !dailyRewardStore.CanClaim(DateTime.UtcNow))
            {
                Refresh(null);
                return;
            }

            DailyRewardDefinition reward = dailyRewardStore.GetRewardForDay(day);
            if (!dailyRewardStore.TryClaim(DateTime.UtcNow, walletStore, inventoryStore))
            {
                Refresh(null);
                return;
            }

            Refresh(null);
            ShowClaimPopup(reward);
        }

        private static void SetAttention(RectTransform root, bool active)
        {
            if (root == null)
            {
                return;
            }

            RewardAttentionEffect effect = root.GetComponent<RewardAttentionEffect>();
            if (active)
            {
                if (effect == null)
                {
                    effect = root.gameObject.AddComponent<RewardAttentionEffect>();
                }

                effect.enabled = true;
                return;
            }

            if (effect != null)
            {
                effect.enabled = false;
            }
        }

        private void SetClaimedCheckVisible(RectTransform cardRoot, bool visible)
        {
            RectTransform checkRoot = GetOrCreateClaimedCheck(cardRoot);
            if (checkRoot != null)
            {
                checkRoot.gameObject.SetActive(visible);
            }
        }

        private RectTransform GetOrCreateClaimedCheck(RectTransform cardRoot)
        {
            if (cardRoot == null)
            {
                return null;
            }

            if (claimedCheckRoots.TryGetValue(cardRoot, out RectTransform cached) && cached != null)
            {
                return cached;
            }

            GameObject rootObject = new GameObject("ClaimedCheckRoot", typeof(RectTransform));
            rootObject.transform.SetParent(cardRoot, false);
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(126f, 126f);

            Image circle = rootObject.AddComponent<Image>();
            CasualUIStyle.ApplyPanel(circle, new Color(0.05f, 0.42f, 0.18f, 0.88f), 58);
            circle.raycastTarget = false;

            CreateCheckStroke(rootRect, "CheckShortStroke", new Vector2(-16f, -4f), new Vector2(17f, 56f), 42f);
            CreateCheckStroke(rootRect, "CheckLongStroke", new Vector2(17f, 7f), new Vector2(18f, 86f), -42f);

            rootObject.SetActive(false);
            claimedCheckRoots[cardRoot] = rootRect;
            return rootRect;
        }

        private static void CreateCheckStroke(RectTransform parent, string name, Vector2 position, Vector2 size, float rotation)
        {
            GameObject strokeObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            strokeObject.transform.SetParent(parent, false);
            RectTransform rect = strokeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

            Image image = strokeObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, Color.white, 9);
            image.raycastTarget = false;
        }

        private void ShowClaimPopup(DailyRewardDefinition reward)
        {
            EnsureClaimPopup();
            claimBaseIcon.sprite = ResolveRewardSprite(reward.type);
            claimBaseIcon.preserveAspect = true;
            claimBaseAmountText.text = BuildBaseRewardText(reward);
            claimSolverIcon.sprite = ResolveSolverTicketSprite();
            claimSolverIcon.preserveAspect = true;
            claimSolverAmountText.text = $"Solver Ticket x{EconomyBalanceConfig.DailySolverTicketBonus}";
            claimPopupRoot.gameObject.SetActive(true);
        }

        private void EnsureClaimPopup()
        {
            if (claimPopupRoot != null)
            {
                return;
            }

            GameObject overlayObject = new GameObject("DailyRewardClaimPopup", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(root.transform, false);
            claimPopupRoot = overlayObject.GetComponent<RectTransform>();
            claimPopupRoot.anchorMin = Vector2.zero;
            claimPopupRoot.anchorMax = Vector2.one;
            claimPopupRoot.offsetMin = Vector2.zero;
            claimPopupRoot.offsetMax = Vector2.zero;

            Image overlay = overlayObject.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.56f);
            overlay.raycastTarget = true;

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                claimPopupRoot,
                "PopupPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 560f));
            Image panelImage = panel.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(panelImage, new Color(0.035f, 0.06f, 0.15f, 0.98f), 32);
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(1f, 0.75f, 0.22f, 0.88f);
            panelOutline.effectDistance = new Vector2(4f, -4f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Reward Claimed!", 48, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -34f);
            title.rectTransform.sizeDelta = new Vector2(0f, 72f);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.86f, 0.34f, 1f);
            CasualUIStyle.ApplyTextDepth(title, true);

            CreateClaimRewardRow(panel, "BaseRewardRow", new Vector2(0f, -182f), out claimBaseIcon, out claimBaseAmountText);
            CreateClaimRewardRow(panel, "SolverTicketRow", new Vector2(0f, -308f), out claimSolverIcon, out claimSolverAmountText);

            claimPopupOkButton = CreateBottomButton(
                panel,
                "OkButton",
                "OK",
                string.Empty,
                CasualUIColor.Blue,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 42f),
                new Vector2(300f, 88f));
            claimPopupOkButton.onClick.AddListener(() => claimPopupRoot.gameObject.SetActive(false));
            claimPopupRoot.gameObject.SetActive(false);
        }

        private static void CreateClaimRewardRow(RectTransform parent, string name, Vector2 position, out Image icon, out Text amountText)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            rowObject.transform.SetParent(parent, false);
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = position;
            row.sizeDelta = new Vector2(560f, 96f);

            Image rowImage = rowObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(rowImage, new Color(0.02f, 0.11f, 0.25f, 0.82f), 22);
            rowImage.raycastTarget = false;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(row, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(72f, 0f);
            iconRect.sizeDelta = new Vector2(72f, 72f);
            icon = iconObject.GetComponent<Image>();
            icon.color = Color.white;
            icon.raycastTarget = false;

            amountText = RuntimeUiFactory.CreateText(row, "Amount", string.Empty, 34, TextAnchor.MiddleLeft);
            amountText.rectTransform.anchorMin = new Vector2(0f, 0f);
            amountText.rectTransform.anchorMax = new Vector2(1f, 1f);
            amountText.rectTransform.offsetMin = new Vector2(132f, 0f);
            amountText.rectTransform.offsetMax = new Vector2(-30f, 0f);
            amountText.fontStyle = FontStyle.Bold;
            amountText.color = Color.white;
            CasualUIStyle.ApplyTextDepth(amountText, true);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void DebugMakeCurrentRewardClaimable()
        {
            dailyRewardStore.DebugMakeCurrentRewardClaimable(DateTime.UtcNow);
            Debug.Log($"[DailyRewardDebug] Made current reward claimable. day={dailyRewardStore.CurrentDayNumber}");
            Refresh("Debug: today's reward is claimable.");
        }
#endif

        private static string BuildRewardLabel(DailyRewardDefinition reward)
        {
            switch (reward.type)
            {
                case DailyRewardType.Coins:
                    return $"{reward.amount} Coins\n+{EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                case DailyRewardType.Gems:
                    return $"{reward.amount} Gems\n+{EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                case DailyRewardType.Hearts:
                    return $"{reward.amount} Hearts\n+{EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                case DailyRewardType.Item:
                    return $"{reward.amount} {reward.itemType}\n+{EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
                default:
                    return $"{reward.amount}\n+{EconomyBalanceConfig.DailySolverTicketBonus} Solver Tickets";
            }
        }

        private static string BuildBaseRewardText(DailyRewardDefinition reward)
        {
            switch (reward.type)
            {
                case DailyRewardType.Coins:
                    return $"Coins x{reward.amount}";
                case DailyRewardType.Gems:
                    return $"Gems x{reward.amount}";
                case DailyRewardType.Hearts:
                    return $"Hearts x{reward.amount}";
                case DailyRewardType.Item:
                    return $"{reward.itemType} x{reward.amount}";
                default:
                    return $"Reward x{reward.amount}";
            }
        }

        private static Sprite ResolveRewardSprite(DailyRewardType type)
        {
            string iconKey = type == DailyRewardType.Coins
                ? "coin"
                : type == DailyRewardType.Gems
                    ? "gem"
                    : "heart";

            return CasualIconFactory.LoadMainMenuKitSprite($"Icons/{iconKey}");
        }

        private static Sprite ResolveSolverTicketSprite()
        {
            return CasualIconFactory.LoadUiSprite("UI/Shop/Icons/solver_ticket")
                ?? CasualIconFactory.LoadMainMenuKitSprite("Icons/solver");
        }

        private static Button CreateBottomButton(
            RectTransform parent,
            string name,
            string label,
            string iconKey,
            CasualUIColor color,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            Button button = CasualUIFactory.CreateActionButton(
                parent,
                name,
                label,
                iconKey,
                color,
                anchor,
                new Vector2(0.5f, 0f),
                position,
                size,
                false);

            RectTransform iconHolder = button.transform.Find("IconHolder") as RectTransform;
            bool hasIconHolder = iconHolder != null;
            if (iconHolder != null)
            {
                iconHolder.anchorMin = new Vector2(0.05f, 0.18f);
                iconHolder.anchorMax = new Vector2(0.245f, 0.82f);
                iconHolder.offsetMin = Vector2.zero;
                iconHolder.offsetMax = Vector2.zero;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 28;
                text.resizeTextMinSize = 17;
                text.resizeTextMaxSize = 28;
                text.alignment = TextAnchor.MiddleCenter;
                text.rectTransform.anchorMin = hasIconHolder ? new Vector2(0.285f, 0.04f) : new Vector2(0.04f, 0.04f);
                text.rectTransform.anchorMax = new Vector2(0.98f, 0.96f);
                text.rectTransform.offsetMin = new Vector2(10f, 0f);
                text.rectTransform.offsetMax = new Vector2(-16f, 0f);
            }

            return button;
        }

        private static RectTransform CreateBottomButtonsRoot(RectTransform parent)
        {
            GameObject rootObject = new GameObject("BottomButtonsRoot", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            RectTransform rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, 0f);
            rect.anchorMax = new Vector2(0.97f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, RewardsLayout.BottomButtonsBottom);
            rect.sizeDelta = new Vector2(0f, RewardsLayout.BottomButtonsHeight);
            return rect;
        }

        private static void ApplyStageButtonSprite(Button button, string spriteName)
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

            for (int i = 0; i < button.transform.childCount; i++)
            {
                button.transform.GetChild(i).gameObject.SetActive(false);
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

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button != null ? button.GetComponentInChildren<Text>() : null;
            if (text == null)
            {
                return;
            }

            text.fontSize = 28;
            text.resizeTextMaxSize = 28;
            text.resizeTextMinSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
        }

        private static RectTransform CreateSafeArea(Transform parent)
        {
            GameObject safeObject = new GameObject("RewardsSafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(parent, false);
            return safeObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateRootPanel(RectTransform parent)
        {
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                parent,
                "RewardsPanel",
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(0.03f, 0.05f, 0.12f, 0.98f), 0);
            image.type = Image.Type.Simple;
            return panel;
        }

        private static void CreateTitle(RectTransform parent)
        {
            Text title = RuntimeUiFactory.CreateText(parent, "DailyRewardsTitle", "Daily Rewards", 76, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -222f);
            title.rectTransform.sizeDelta = new Vector2(0f, 106f);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.84f, 0.25f, 1f);
            CasualUIStyle.ApplyTextDepth(title, true);
        }

        private static void CreateSubtitle(RectTransform parent)
        {
            Text subtitle = RuntimeUiFactory.CreateText(parent, "DailyRewardsSubtitle", "Claim today's reward and get 3 Solver Tickets!", 28, TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -364f);
            subtitle.rectTransform.sizeDelta = new Vector2(0f, 44f);
            subtitle.color = new Color(1f, 0.93f, 0.55f, 0.98f);
            CasualUIStyle.ApplyTextDepth(subtitle, false);
        }

        private RectTransform CreateRewardsSection(RectTransform parent)
        {
            RectTransform viewport = RuntimeUiFactory.CreatePanel(
                parent,
                "RewardsSectionViewport",
                Vector2.zero,
                new Vector2(0.5f, 1f),
                Vector2.zero,
                Vector2.zero);
            rewardsViewport = viewport;
            viewport.anchorMin = new Vector2(0.02f, 0f);
            viewport.anchorMax = new Vector2(0.98f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.offsetMin = new Vector2(0f, RewardsLayout.BottomButtonsBottom + RewardsLayout.BottomButtonsHeight + RewardsLayout.DaySevenButtonMinGap);
            viewport.offsetMax = new Vector2(0f, -RewardsLayout.RewardsSectionTop);
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform section = RuntimeUiFactory.CreatePanel(
                viewport,
                "RewardsSectionContent",
                Vector2.zero,
                new Vector2(0.5f, 1f),
                Vector2.zero,
                Vector2.zero);
            section.anchorMin = new Vector2(0f, 1f);
            section.anchorMax = new Vector2(1f, 1f);
            section.pivot = new Vector2(0.5f, 1f);
            section.anchoredPosition = Vector2.zero;
            section.sizeDelta = new Vector2(0f, RewardsLayout.RewardsSectionHeight);
            Image image = section.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            scroll.viewport = viewport;
            scroll.content = section;
            return section;
        }

        private RuntimeUiFactory.RewardCardView CreateRewardCard(RectTransform parent, int day, bool special)
        {
            RuntimeUiFactory.RewardCardView card = RuntimeUiFactory.CreateRewardCard(parent, special ? "Day7Card" : $"Day{day}Card", special);

            if (special)
            {
                card.Root.anchorMin = new Vector2(0.5f, 1f);
                card.Root.anchorMax = new Vector2(0.5f, 1f);
                card.Root.pivot = new Vector2(0.5f, 1f);
                card.Root.anchoredPosition = new Vector2(0f, -996f);
                card.Root.sizeDelta = new Vector2(1016f, 286f);
            }

            return card;
        }

        private enum RewardCardState
        {
            Claimable,
            Claimed,
            Locked
        }
    }
}
