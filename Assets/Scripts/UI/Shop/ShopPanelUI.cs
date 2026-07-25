using System;
using System.Collections.Generic;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.IAP;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CubeChallenge3D.UI.Style;

namespace CubeChallenge3D.UI.Shop
{
    public sealed class ShopPanelUI
    {
        // DEV ONLY: keep true while testing monetization/shop flow on phone builds.
        // Set to false before release.
        private const bool EnableRuntimeGemDebugButton = false;
        private const int ActiveSortingOrder = 1700;

        private readonly GameObject root;
        private readonly Canvas canvas;
        private readonly WalletStore walletStore;
        private readonly InventoryStore inventoryStore;
        private readonly IRewardService rewardService;
        private readonly PromotionPurchaseService promotionPurchaseService;
        private readonly PlayerProfileStore profileStore;
        private readonly VisualCustomizationService customizationService;
        private readonly List<ShopItemDefinition> shopItems;
        private readonly IReadOnlyList<PromotionProductDefinition> promotionItems;
        private readonly IReadOnlyList<CubeSkinData> skinItems;
        private readonly IReadOnlyList<ThemeData> themeItems;
        private readonly Text walletText;
        private readonly Text statusText;
        private RectTransform itemViewport;
        private readonly RectTransform itemContainer;
        private readonly Button coinTabButton;
        private readonly Button gemTabButton;
        private readonly Button skinTabButton;
        private readonly Button themeTabButton;
        private readonly Button adRewardButton;
        private static readonly Dictionary<string, Sprite> ShopIconCache = new Dictionary<string, Sprite>();
        private RenderTexture skinPreviewTexture;
        private GameObject skinPreviewScene;
        private Camera skinPreviewCamera;
        private CubeVisualBuilder skinPreviewBuilder;
        private RawImage skinPreviewImage;
        private Text skinPreviewName;
        private Text skinIndexBadge;
        private Text skinPreviewInfo;
        private Text skinStateBadge;
        private RectTransform skinStateIconRoot;
        private Button skinActionButton;
        private ShopSkinPreviewOrbit skinPreviewOrbit;
        private readonly List<Image> skinCarouselDots = new List<Image>();
        private CubeSkinData previewedSkin;
        private int selectedSkinIndex;
        private ShopTab selectedTab;
        private readonly Vector2 minPanelSize = new Vector2(680f, 760f);
        private readonly Vector2 maxPanelSize = new Vector2(980f, 1280f);
        private const float BottomButtonGroupWidth = 996f;
        private const float BottomButtonGap = 40f;
        private const float ItemViewportFooterInset = 276f;
        private static readonly Vector2 BottomButtonSize = new Vector2((BottomButtonGroupWidth - BottomButtonGap) * 0.5f, 112f);

        public ShopPanelUI(Transform parent, WalletStore wallet, InventoryStore inventory, IRewardService rewards, PlayerProfileStore profile = null)
        {
            walletStore = wallet ?? new WalletStore();
            inventoryStore = inventory ?? new InventoryStore();
            rewardService = rewards ?? RewardedAdService.CreateDefault();
            promotionPurchaseService = new PromotionPurchaseService(walletStore, profileStore);
            profileStore = profile ?? new PlayerProfileStore();
            customizationService = new VisualCustomizationService(walletStore, inventoryStore);
            shopItems = new List<ShopItemDefinition>(ShopItemDefinition.CreateDefaults());
            promotionItems = PromotionProductDefinition.CreateDefaults();
            skinItems = VisualCustomizationCatalog.GetSkins();
            themeItems = VisualCustomizationCatalog.GetThemes();

            canvas = RuntimeUiFactory.CreateCanvas(parent, "Canvas", 1480);
            TopCurrencyBar.Attach(
                canvas,
                () => { },
                true,
                ShowGemItems,
                ShowGemItems,
                ShowPromotion);
            root = canvas.gameObject;
            CreateFullscreenBackdrop(root.transform);
            GameObject safeObject = new GameObject("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(root.transform, false);

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                safeObject.transform,
                "ShopPanel",
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                CasualUIStyle.ApplyPanel(panelImage, new Color(0.045f, 0.05f, 0.11f, 1f), 0);
                panelImage.type = Image.Type.Simple;
            }

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Shop", 38, TextAnchor.UpperCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -158f);
            title.rectTransform.sizeDelta = new Vector2(-120f, 56f);
            CasualUIStyle.ApplyTextDepth(title, true);
            title.gameObject.SetActive(false);

            walletText = RuntimeUiFactory.CreateText(panel, "Wallet", string.Empty, 25, TextAnchor.UpperCenter);
            walletText.rectTransform.anchorMin = new Vector2(0f, 1f);
            walletText.rectTransform.anchorMax = new Vector2(1f, 1f);
            walletText.rectTransform.pivot = new Vector2(0.5f, 1f);
            walletText.rectTransform.anchoredPosition = new Vector2(0f, -126f);
            walletText.rectTransform.sizeDelta = new Vector2(-80f, 42f);
            walletText.gameObject.SetActive(false);

            coinTabButton = RuntimeUiFactory.CreateButton(panel, "CoinItemsTab", "Coin Items", new Vector2(-306f, 720f), new Vector2(198f, 84f));
            gemTabButton = RuntimeUiFactory.CreateButton(panel, "GemItemsTab", "Gem Items", new Vector2(-102f, 720f), new Vector2(198f, 84f));
            skinTabButton = RuntimeUiFactory.CreateButton(panel, "SkinsTab", "Skins", new Vector2(102f, 720f), new Vector2(198f, 84f));
            themeTabButton = RuntimeUiFactory.CreateButton(panel, "PromotionTab", "Promotion", new Vector2(306f, 720f), new Vector2(198f, 84f));
            SetTopButton(coinTabButton, -228f, -306f);
            SetTopButton(gemTabButton, -228f, -102f);
            SetTopButton(skinTabButton, -228f, 102f);
            SetTopButton(themeTabButton, -228f, 306f);
            StyleTabButton(coinTabButton, CasualUIColor.Orange, "coin");
            StyleTabButton(gemTabButton, CasualUIColor.Purple, "gem");
            StyleTabButton(skinTabButton, CasualUIColor.Blue, "stages");
            StyleTabButton(themeTabButton, CasualUIColor.Pink, "rewards");
            coinTabButton.onClick.AddListener(() => SelectTab(ShopTab.CoinItems));
            gemTabButton.onClick.AddListener(() => SelectTab(ShopTab.GemItems));
            skinTabButton.onClick.AddListener(() => SelectTab(ShopTab.Skins));
            themeTabButton.onClick.AddListener(() => SelectTab(ShopTab.Promotion));

            itemContainer = CreateItemContainer(panel);

            float bottomButtonCenterOffset = (BottomButtonSize.x + BottomButtonGap) * 0.5f;
            adRewardButton = RuntimeUiFactory.CreateButton(panel, "WatchAdCoinsButton", "Watch Ad +50 Coins", new Vector2(-bottomButtonCenterOffset, 128f), BottomButtonSize);
            CasualUIStyle.ApplyButton(adRewardButton, CasualUIColor.Orange);
            ApplyBottomButtonSprite(adRewardButton, "watch_ad_coins_orange_clean", 0.25f);
            adRewardButton.onClick.AddListener(WatchAdForCoins);

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Close", new Vector2(bottomButtonCenterOffset, 128f), BottomButtonSize);
            CasualUIStyle.ApplyButton(close, CasualUIColor.Blue);
            ApplyBottomButtonSprite(close, "close_blue_clean", 0.05f);
            close.onClick.AddListener(Hide);

            Button addGems = RuntimeUiFactory.CreateButton(panel, "DebugAddGemsButton", "DEV +100 Gems", new Vector2(-190f, 52f), new Vector2(230f, 40f));
            CasualUIStyle.ApplyButton(addGems, CasualUIColor.Slate);
            addGems.onClick.AddListener(() =>
            {
                walletStore.AddGems(100);
                Refresh("Added 100 gems.");
            });
            Button resetVisuals = RuntimeUiFactory.CreateButton(panel, "DebugResetVisualsButton", "DEV Reset Visuals", new Vector2(190f, 52f), new Vector2(230f, 40f));
            CasualUIStyle.ApplyButton(resetVisuals, CasualUIColor.Slate);
            resetVisuals.onClick.AddListener(() =>
            {
                inventoryStore.ResetVisualCustomizations();
                Refresh("Skins and themes reset.");
            });
            addGems.gameObject.SetActive(EnableRuntimeGemDebugButton);
            resetVisuals.gameObject.SetActive(false);

            statusText = RuntimeUiFactory.CreateText(panel, "Status", string.Empty, 22, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0f, 102f);
            statusText.rectTransform.sizeDelta = new Vector2(-80f, 52f);
            statusText.gameObject.SetActive(false);

            promotionPurchaseService.StateChanged += HandlePromotionPurchaseStateChanged;
            WalletStore.Changed += HandleExternalEconomyChanged;
            InventoryStore.Changed += HandleExternalInventoryChanged;
            PlayerProfileStore.Changed += HandleExternalProfileChanged;

            Hide();
        }

        private static void SetTopButton(Button button, float y, float x)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void StyleTabButton(Button button, CasualUIColor color, string iconKey)
        {
            CasualUIStyle.ApplyButton(button, color);
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 28;
                label.resizeTextMaxSize = 28;
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.anchorMin = new Vector2(0.31f, 0f);
                label.rectTransform.anchorMax = new Vector2(0.96f, 1f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                CasualUIStyle.ApplyTextDepth(label, true);
            }

            Image rootImage = button.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(0.11f, 0.16f, 0.29f, 1f);
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            GameObject iconHolderObject = new GameObject("TabIconHolder", typeof(RectTransform), typeof(Image));
            iconHolderObject.transform.SetParent(rect, false);
            RectTransform iconHolder = iconHolderObject.GetComponent<RectTransform>();
            iconHolder.anchorMin = new Vector2(0.07f, 0.18f);
            iconHolder.anchorMax = new Vector2(0.27f, 0.82f);
            iconHolder.offsetMin = Vector2.zero;
            iconHolder.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(iconHolderObject.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.05f), 18);

            if (!CasualIconFactory.TryCreateMainMenuKitIcon(iconHolder, iconKey, out _))
            {
                CasualIconFactory.Create(iconHolder, iconKey, Color.white);
            }
        }

        private static void ApplyBottomButtonSprite(Button button, string spriteName, float labelAnchorMinX)
        {
            if (button == null)
            {
                return;
            }

            Sprite sprite = CasualIconFactory.LoadUiSprite($"UI/Shop/Buttons/{spriteName}");
            if (sprite != null)
            {
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprite;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                    image.color = Color.white;
                }
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

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.anchorMin = new Vector2(labelAnchorMinX, 0.08f);
                label.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 28;
                CasualUIStyle.ApplyTextDepth(label, true);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.86f, 0.86f, 0.9f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.56f, 0.56f, 0.6f, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        public void Show()
        {
            ShowCoinItems();
        }

        public void ShowCoinItems()
        {
            SelectTab(ShopTab.CoinItems);
            ShowRoot();
        }

        public void ShowGemItems()
        {
            SelectTab(ShopTab.GemItems);
            ShowRoot();
        }

        public void ShowPromotion()
        {
            SelectTab(ShopTab.Promotion);
            ShowRoot();
        }

        private void ShowRoot()
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (canvas != null)
            {
                canvas.sortingOrder = ActiveSortingOrder;
            }
        }

        public void Hide()
        {
            root.SetActive(false);
            if (skinPreviewScene != null)
            {
                skinPreviewScene.SetActive(false);
            }
        }

        private RectTransform CreateItemContainer(RectTransform parent)
        {
            GameObject viewportObject = new GameObject("ShopViewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(Outline), typeof(Shadow));
            viewportObject.transform.SetParent(parent, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            itemViewport = viewport;
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(24f, 164f);
            viewport.offsetMax = new Vector2(-24f, -302f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(viewportImage, new Color(0.035f, 0.04f, 0.095f, 1f), 30);
            Outline viewportOutline = viewportObject.GetComponent<Outline>();
            viewportOutline.effectColor = new Color(0.65f, 0.34f, 0.98f, 0.22f);
            viewportOutline.effectDistance = new Vector2(2f, -2f);
            Shadow viewportShadow = viewportObject.GetComponent<Shadow>();
            viewportShadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            viewportShadow.effectDistance = new Vector2(0f, -6f);
            viewportObject.GetComponent<Mask>().showMaskGraphic = true;

            GameObject container = new GameObject("ShopItems", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            container.transform.SetParent(viewport, false);
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(18, 18, 18, 42);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = rect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return rect;
        }

        private static void CreateFullscreenBackdrop(Transform parent)
        {
            GameObject backgroundObject = new GameObject("ShopFullscreenBackdrop", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();
            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = backgroundObject.GetComponent<Image>();
            CasualUIStyle.ApplyBackground(image);
            image.raycastTarget = false;
        }

        private void Refresh(string message)
        {
            inventoryStore.Reload();
            profileStore.ReloadFromDisk();
            Debug.Log("[Shop] Refresh item counts");
            statusText.text = string.Empty;
            statusText.gameObject.SetActive(false);
            UpdateViewportLayoutForCurrentTab();
            if (rewardService is RewardedAdService ads)
            {
                ads.EnsureLoaded(RewardedAdPlacement.ShopCoinReward);
                int remaining = ads.GetRemaining(RewardedAdPlacement.ShopCoinReward);
                Text label = adRewardButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.fontSize = 20;
                    bool ready = ads.IsReady(RewardedAdPlacement.ShopCoinReward);
                    label.text = ads.IsPlacementAvailable(RewardedAdPlacement.ShopCoinReward)
                        ? $"Watch Ad for +{ads.Config.dailyCoinRewardAmount} Coins  ({remaining}/{ads.Config.dailyCoinAdsMax} left)"
                        : "Ad not available";
                    if (ads.IsPlacementAvailable(RewardedAdPlacement.ShopCoinReward) && !ready)
                    {
                        label.text = $"Loading Ad +{ads.Config.dailyCoinRewardAmount} Coins  ({remaining}/{ads.Config.dailyCoinAdsMax} left)";
                    }
                }
                adRewardButton.interactable = ads.CanRequest(RewardedAdPlacement.ShopCoinReward);
            }
            else
            {
                adRewardButton.interactable = rewardService.IsRewardAvailable(RewardType.EarnCoins);
            }
            if (skinPreviewScene != null && selectedTab != ShopTab.Skins)
            {
                skinPreviewScene.SetActive(false);
            }

            for (int i = itemContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(itemContainer.GetChild(i).gameObject);
            }

            if (selectedTab == ShopTab.CoinItems)
            {
                foreach (ShopItemDefinition item in shopItems)
                {
                    CreateShopRow(item);
                }
            }
            else if (selectedTab == ShopTab.GemItems)
            {
                CreateHeartPackRow("Small Heart Pack", "Refill your lives and keep the challenge going!", EconomyBalanceConfig.SmallHeartPackGemPrice, EconomyBalanceConfig.SmallHeartPackHearts, "heart_pack_small");
                CreateHeartPackRow("Medium Heart Pack", "More hearts to help you beat your best score.", EconomyBalanceConfig.MediumHeartPackGemPrice, EconomyBalanceConfig.MediumHeartPackHearts, "heart_pack_medium");
                CreateHeartPackRow("Large Heart Pack", "Plenty of hearts for serious cube masters.", EconomyBalanceConfig.LargeHeartPackGemPrice, EconomyBalanceConfig.LargeHeartPackHearts, "heart_pack_large");
                CreateGemExchangeRow("Small Coin Pack", $"Exchange gems for {EconomyBalanceConfig.SmallCoinPackCoins} coins.", EconomyBalanceConfig.SmallCoinPackGemPrice, EconomyBalanceConfig.SmallCoinPackCoins, "coin_pack_small");
                CreateGemExchangeRow("Medium Coin Pack", $"Exchange gems for {EconomyBalanceConfig.MediumCoinPackCoins} coins.", EconomyBalanceConfig.MediumCoinPackGemPrice, EconomyBalanceConfig.MediumCoinPackCoins, "coin_pack_medium");
                CreateGemExchangeRow("Large Coin Pack", $"Exchange gems for {EconomyBalanceConfig.LargeCoinPackCoins} coins.", EconomyBalanceConfig.LargeCoinPackGemPrice, EconomyBalanceConfig.LargeCoinPackCoins, "coin_pack_large");
            }
            else if (selectedTab == ShopTab.Skins)
            {
                CreateSkinCarousel();
            }
            else
            {
                CreatePromotionRows();
            }

            RefreshTabVisuals();

            if (!string.IsNullOrEmpty(message))
            {
                statusText.text = message;
                statusText.gameObject.SetActive(true);
            }
        }

        private void UpdateViewportLayoutForCurrentTab()
        {
            if (itemViewport == null)
            {
                return;
            }

            float bottomInset = ItemViewportFooterInset;
            itemViewport.offsetMin = new Vector2(24f, bottomInset);
            itemViewport.offsetMax = new Vector2(-24f, -302f);
        }

        private void SelectTab(ShopTab tab)
        {
            selectedTab = tab;
            if (tab == ShopTab.Skins && previewedSkin == null)
            {
                selectedSkinIndex = FindSkinIndex(customizationService.SelectedSkin.skinId);
            }
            Refresh(string.Empty);
        }

        private void CreateShopRow(ShopItemDefinition item)
        {
            string iconKey = GetCoinItemIconKey(item.itemType);
            RectTransform row = CreateShopCard($"{item.itemType}Row", 214f);
            CreateIconPanel(row, iconKey, GetFallbackIconText(item.itemType), CasualUIColor.Blue);
            CreateCardTexts(
                row,
                GetItemName(item.itemType),
                GetItemDescription(item.itemType),
                item.coinPrice.ToString(),
                $"Owned: {GetOwnedCount(item.itemType)}",
                "coin");

            Button buy = CreateCardButton(row, "BuyButton", "BUY", CasualUIColor.Green, "coin", item.coinPrice.ToString());
            buy.onClick.AddListener(() => BuyItem(item));
        }

        private void CreateGemExchangeRow(string title, string description, int gemPrice, int coinAmount, string iconKey)
        {
            RectTransform row = CreateShopCard($"{title.Replace(" ", string.Empty)}Row", 228f);
            CreateIconPanel(row, iconKey, string.Empty, CasualUIColor.Blue);
            CreateCardTexts(row, title, description, gemPrice.ToString(), $"Receive: {coinAmount:N0} Coins", "gem");
            Button buy = CreateCardButton(row, "ExchangeButton", "EXCHANGE", CasualUIColor.Blue, "gem", gemPrice.ToString());
            buy.onClick.AddListener(() =>
            {
                if (!walletStore.SpendGems(gemPrice))
                {
                    Refresh(UIStrings.NotEnoughGems);
                    return;
                }

                walletStore.AddCoins(coinAmount);
                Refresh($"Received {coinAmount} Coins.");
            });
        }

        private void CreateHeartPackRow(string title, string description, int gemPrice, int heartAmount, string iconKey)
        {
            RectTransform row = CreateShopCard($"{title.Replace(" ", string.Empty)}Row", 228f);
            CreateIconPanel(row, iconKey, string.Empty, CasualUIColor.Pink);
            CreateCardTexts(row, title, description, gemPrice.ToString(), $"+{heartAmount} Hearts", "gem");
            Button buy = CreateCardButton(row, "BuyHeartPackButton", "BUY", CasualUIColor.Green, "gem", gemPrice.ToString());
            buy.onClick.AddListener(() =>
            {
                if (!walletStore.SpendGems(gemPrice))
                {
                    Refresh(UIStrings.NotEnoughGems);
                    return;
                }

                walletStore.AddHearts(heartAmount);
                Refresh(UIStrings.Purchased);
            });
        }

        private void CreateSectionHeader(string title)
        {
            GameObject headerObject = new GameObject($"{title.Replace(" ", string.Empty)}Header", typeof(RectTransform), typeof(LayoutElement));
            headerObject.transform.SetParent(itemContainer, false);
            headerObject.GetComponent<LayoutElement>().preferredHeight = 52f;
            RectTransform header = headerObject.GetComponent<RectTransform>();

            Text label = RuntimeUiFactory.CreateText(header, "Label", title, 26, TextAnchor.MiddleCenter);
            label.color = new Color(1f, 0.88f, 0.42f, 1f);
            CasualUIStyle.ApplyTextDepth(label, true);
        }

        private void CreateThemesPlaceholder()
        {
            RectTransform card = CreateShopCard("ThemesComingSoon", 320f);
            Text title = RuntimeUiFactory.CreateText(card, "Title", "Themes", 34, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -42f);
            title.rectTransform.sizeDelta = new Vector2(-80f, 54f);
            CasualUIStyle.ApplyTextDepth(title, true);

            Text body = RuntimeUiFactory.CreateText(
                card,
                "Body",
                "Theme shop is coming soon.\nThe tab is kept here so the menu flow stays ready.",
                24,
                TextAnchor.MiddleCenter);
            body.rectTransform.offsetMin = new Vector2(50f, 32f);
            body.rectTransform.offsetMax = new Vector2(-50f, -92f);
            body.color = new Color(0.86f, 0.9f, 1f, 1f);
        }

        private void CreatePromotionRows()
        {
            foreach (PromotionProductDefinition item in promotionItems)
            {
                CreatePromotionRow(item);
            }
        }

        private void CreatePromotionRow(PromotionProductDefinition item)
        {
            RectTransform row = CreateShopCard($"{item.id}Row", 228f);
            CreateIconPanel(
                row,
                item.iconKey,
                item.itemType == "RemoveAds" ? "ADS" : "GEM",
                item.itemType == "RemoveAds" ? CasualUIColor.Orange : CasualUIColor.Pink);

            Text titleText = RuntimeUiFactory.CreateText(row, "Title", item.displayName, 34, TextAnchor.UpperLeft);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(206f, -30f);
            titleText.rectTransform.sizeDelta = new Vector2(-392f, 48f);
            CasualUIStyle.ApplyTextDepth(titleText, true);

            Text descriptionText = RuntimeUiFactory.CreateText(row, "Description", item.description, 23, TextAnchor.UpperLeft);
            descriptionText.rectTransform.anchorMin = new Vector2(0f, 1f);
            descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            descriptionText.rectTransform.pivot = new Vector2(0f, 1f);
            descriptionText.rectTransform.anchoredPosition = new Vector2(206f, -82f);
            descriptionText.rectTransform.sizeDelta = new Vector2(-392f, 52f);
            descriptionText.color = new Color(0.82f, 0.86f, 1f, 1f);

            GameObject rewardRowObject = new GameObject("RewardRow", typeof(RectTransform));
            rewardRowObject.transform.SetParent(row, false);
            RectTransform rewardRow = rewardRowObject.GetComponent<RectTransform>();
            rewardRow.anchorMin = new Vector2(0f, 0f);
            rewardRow.anchorMax = new Vector2(0f, 0f);
            rewardRow.pivot = new Vector2(0f, 0f);
            rewardRow.anchoredPosition = new Vector2(206f, 22f);
            rewardRow.sizeDelta = new Vector2(360f, 40f);

            Sprite rewardSprite = CasualIconFactory.LoadMainMenuKitSprite(
                item.itemType == "RemoveAds"
                    ? "Icons/rewards"
                    : "Icons/gem");
            if (rewardSprite != null)
            {
                GameObject iconObject = new GameObject("RewardIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(rewardRow, false);
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, 19f);
                iconRect.sizeDelta = new Vector2(28f, 28f);
                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = rewardSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            Text rewardText = RuntimeUiFactory.CreateText(rewardRow, "RewardText", item.rewardText, 28, TextAnchor.MiddleLeft);
            rewardText.rectTransform.anchorMin = new Vector2(0f, 0f);
            rewardText.rectTransform.anchorMax = new Vector2(1f, 1f);
            rewardText.rectTransform.offsetMin = new Vector2(38f, 0f);
            rewardText.rectTransform.offsetMax = Vector2.zero;
            rewardText.color = new Color(1f, 0.84f, 0.25f, 1f);
            rewardText.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(rewardText, false);

            bool owned = !item.isConsumable && promotionPurchaseService.IsProductOwned(item.productId);
            bool canPurchase = promotionPurchaseService.CanPurchase(item);
            string buttonLabel = owned ? "OWNED" : canPurchase ? item.buttonLabel : "UNAVAILABLE";
            string priceLabel = promotionPurchaseService.GetDisplayPrice(item);
            Button buy = CreateCardButton(row, "BuyButton", buttonLabel, owned ? CasualUIColor.Blue : canPurchase ? CasualUIColor.Green : CasualUIColor.Slate);
            AttachPromotionPriceLabel(buy, priceLabel);
            buy.interactable = !owned && canPurchase;
            if (!owned && canPurchase)
            {
                buy.onClick.AddListener(() => HandlePromotionPurchase(item));
            }
        }

        private static void AttachPromotionPriceLabel(Button button, string priceTextValue)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            Text footer = RuntimeUiFactory.CreateText(rect, "FooterValue", priceTextValue, 24, TextAnchor.MiddleCenter);
            footer.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            footer.rectTransform.anchorMax = new Vector2(1f, 0.42f);
            footer.rectTransform.offsetMin = new Vector2(8f, 0f);
            footer.rectTransform.offsetMax = new Vector2(-8f, 0f);
            footer.color = Color.white;
            footer.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(footer, true);
        }

        private void HandlePromotionPurchase(PromotionProductDefinition item)
        {
            PromotionPurchaseResult result = promotionPurchaseService.Purchase(item);
            Refresh(result.message);
        }

        private void HandlePromotionPurchaseStateChanged(string message)
        {
            if (selectedTab == ShopTab.Promotion)
            {
                Refresh(message);
            }
        }

        private RectTransform CreateShopCard(string name, float height)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Shadow), typeof(Outline));
            rowObject.transform.SetParent(itemContainer, false);
            RectTransform row = rowObject.GetComponent<RectTransform>();
            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.preferredHeight = height;

            Image image = rowObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(0.052f, 0.072f, 0.13f, 0.985f), 28);
            Shadow shadow = rowObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(0f, -6f);
            Outline outline = rowObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.62f, 0.16f, 0.52f);
            outline.effectDistance = new Vector2(2f, -2f);
            AddCardGloss(row, "CardTopGloss", new Vector2(0.018f, 0.84f), new Vector2(0.982f, 0.952f), new Color(1f, 1f, 1f, 0.028f), 18);
            AddCardGloss(row, "IconGlow", new Vector2(0.02f, 0.08f), new Vector2(0.22f, 0.92f), new Color(0.5f, 0.2f, 1f, 0.07f), 24);
            CreateCardDivider(row);
            return row;
        }

        private static void CreateCardDivider(RectTransform row)
        {
            GameObject dividerObject = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerObject.transform.SetParent(row, false);
            RectTransform divider = dividerObject.GetComponent<RectTransform>();
            divider.anchorMin = new Vector2(1f, 0.18f);
            divider.anchorMax = new Vector2(1f, 0.82f);
            divider.pivot = new Vector2(1f, 0.5f);
            divider.anchoredPosition = new Vector2(-214f, 0f);
            divider.sizeDelta = new Vector2(2f, 0f);
            Image image = dividerObject.GetComponent<Image>();
            image.color = new Color(0.34f, 0.75f, 1f, 0.32f);
            image.raycastTarget = false;
        }

        private static void AddCardGloss(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, int radius)
        {
            GameObject gloss = new GameObject(name, typeof(RectTransform), typeof(Image));
            gloss.transform.SetParent(parent, false);
            RectTransform rect = gloss.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = gloss.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, radius);
            image.raycastTarget = false;
        }

        private static void AddBoxFrame(RectTransform parent, string name, float inset, float thickness, Color color)
        {
            CreateFrameSegment(parent, $"{name}_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(inset, -(inset + thickness)), new Vector2(-inset, -inset), color);
            CreateFrameSegment(parent, $"{name}_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(inset, inset), new Vector2(-inset, inset + thickness), color);
            CreateFrameSegment(parent, $"{name}_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(inset, inset), new Vector2(inset + thickness, -inset), color);
            CreateFrameSegment(parent, $"{name}_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-(inset + thickness), inset), new Vector2(-inset, -inset), color);
        }

        private static void CreateRoundedShowcaseFrame(RectTransform parent, string name, float inset, Color fillColor, Color outlineColor)
        {
            GameObject frameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            frameObject.transform.SetParent(parent, false);
            RectTransform frame = frameObject.GetComponent<RectTransform>();
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.offsetMin = new Vector2(inset, inset);
            frame.offsetMax = new Vector2(-inset, -inset);

            Image image = frameObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, fillColor, 30);
            image.raycastTarget = false;

            Outline outline = frameObject.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
        }

        private static void CreateFrameSegment(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject segmentObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            segmentObject.transform.SetParent(parent, false);
            RectTransform segment = segmentObject.GetComponent<RectTransform>();
            segment.anchorMin = anchorMin;
            segment.anchorMax = anchorMax;
            segment.offsetMin = offsetMin;
            segment.offsetMax = offsetMax;
            Image image = segmentObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void CreateIconPanel(RectTransform row, string iconKey, string fallbackText, CasualUIColor fallbackColor)
        {
            GameObject holder = new GameObject("IconPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            holder.transform.SetParent(row, false);
            RectTransform holderRect = holder.GetComponent<RectTransform>();
            holderRect.anchorMin = new Vector2(0f, 0.5f);
            holderRect.anchorMax = new Vector2(0f, 0.5f);
            holderRect.pivot = new Vector2(0f, 0.5f);
            holderRect.anchoredPosition = new Vector2(24f, 0f);
            holderRect.sizeDelta = new Vector2(162f, 162f);

            Image holderImage = holder.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(holderImage, new Color(0.14f, 0.10f, 0.28f, 0.96f), 24);
            holder.GetComponent<Outline>().effectColor = new Color(0.78f, 0.35f, 1f, 0.42f);

            Sprite sprite = LoadShopIcon(iconKey);
            if (sprite != null)
            {
                GameObject imageObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(holderRect, false);
                RectTransform rect = imageObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.10f, 0.10f);
                rect.anchorMax = new Vector2(0.90f, 0.90f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Image image = imageObject.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            Text fallback = RuntimeUiFactory.CreateText(holderRect, "FallbackIcon", fallbackText, 42, TextAnchor.MiddleCenter);
            fallback.color = CasualUIPalette.Get(fallbackColor);
            CasualUIStyle.ApplyTextDepth(fallback, true);
        }

        private static Sprite LoadShopIcon(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey))
            {
                return null;
            }

            if (ShopIconCache.TryGetValue(iconKey, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>($"UI/Shop/Icons/{iconKey}");
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = iconKey;
            ShopIconCache[iconKey] = sprite;
            return sprite;
        }

        private static void CreateCardTexts(
            RectTransform row,
            string title,
            string description,
            string price,
            string ownedOrReward,
            string priceIconKey)
        {
            Text titleText = RuntimeUiFactory.CreateText(row, "Title", title, 36, TextAnchor.UpperLeft);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(206f, -30f);
            titleText.rectTransform.sizeDelta = new Vector2(-392f, 50f);
            CasualUIStyle.ApplyTextDepth(titleText, true);

            Text descText = RuntimeUiFactory.CreateText(row, "Description", description, 23, TextAnchor.UpperLeft);
            descText.rectTransform.anchorMin = new Vector2(0f, 1f);
            descText.rectTransform.anchorMax = new Vector2(1f, 1f);
            descText.rectTransform.pivot = new Vector2(0f, 1f);
            descText.rectTransform.anchoredPosition = new Vector2(206f, -82f);
            descText.rectTransform.sizeDelta = new Vector2(-392f, 62f);
            descText.color = new Color(0.82f, 0.86f, 1f, 1f);

            GameObject priceRowObject = new GameObject("PriceRow", typeof(RectTransform));
            priceRowObject.transform.SetParent(row, false);
            RectTransform priceRow = priceRowObject.GetComponent<RectTransform>();
            priceRow.anchorMin = new Vector2(0f, 0f);
            priceRow.anchorMax = new Vector2(0f, 0f);
            priceRow.pivot = new Vector2(0f, 0f);
            priceRow.anchoredPosition = new Vector2(206f, 28f);
            priceRow.sizeDelta = new Vector2(196f, 38f);

            Sprite currencySprite = CasualIconFactory.LoadMainMenuKitSprite($"Icons/{priceIconKey}");
            if (currencySprite != null)
            {
                GameObject iconObject = new GameObject("PriceIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(priceRow, false);
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, 19f);
                iconRect.sizeDelta = new Vector2(28f, 28f);
                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = currencySprite;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;
            }
            else
            {
                RectTransform priceIcon = CasualIconFactory.Create(
                    priceRow,
                    priceIconKey,
                    priceIconKey == "coin" ? new Color(1f, 0.68f, 0.12f) : new Color(0.86f, 0.24f, 1f));
                priceIcon.anchorMin = new Vector2(0f, 0.1f);
                priceIcon.anchorMax = new Vector2(0f, 0.9f);
                priceIcon.pivot = new Vector2(0f, 0.5f);
                priceIcon.sizeDelta = new Vector2(24f, 24f);
                priceIcon.anchoredPosition = new Vector2(0f, 19f);
            }

            Text priceText = RuntimeUiFactory.CreateText(priceRow, "Price", price, 31, TextAnchor.MiddleLeft);
            priceText.rectTransform.anchorMin = new Vector2(0f, 0f);
            priceText.rectTransform.anchorMax = new Vector2(1f, 1f);
            priceText.rectTransform.offsetMin = new Vector2(38f, 0f);
            priceText.rectTransform.offsetMax = Vector2.zero;
            priceText.color = new Color(1f, 0.84f, 0.25f, 1f);
            priceText.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(priceText, false);

            Text ownedText = RuntimeUiFactory.CreateText(row, "Owned", ownedOrReward, 27, TextAnchor.LowerLeft);
            ownedText.rectTransform.anchorMin = new Vector2(0f, 0f);
            ownedText.rectTransform.anchorMax = new Vector2(1f, 0f);
            ownedText.rectTransform.pivot = new Vector2(0f, 0f);
            ownedText.rectTransform.anchoredPosition = new Vector2(322f, 28f);
            ownedText.rectTransform.sizeDelta = new Vector2(-410f, 38f);
            ownedText.color = new Color(0.64f, 0.82f, 1f, 1f);
            ownedText.fontStyle = FontStyle.Bold;
        }

        private static Button CreateCardButton(
            RectTransform row,
            string name,
            string label,
            CasualUIColor color,
            string priceIconKey = null,
            string footerValue = null)
        {
            Button button = RuntimeUiFactory.CreateButton(row, name, label, Vector2.zero, new Vector2(176f, 90f));
            CasualUIStyle.ApplyButton(button, color);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-24f, 0f);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 26;
                text.resizeTextMaxSize = 28;
                text.rectTransform.anchorMin = new Vector2(0f, 0.48f);
                text.rectTransform.anchorMax = new Vector2(1f, 0.92f);
                text.rectTransform.offsetMin = new Vector2(10f, 0f);
                text.rectTransform.offsetMax = new Vector2(-10f, 0f);
                CasualUIStyle.ApplyTextDepth(text, true);
            }

            if (!string.IsNullOrEmpty(priceIconKey) && !string.IsNullOrEmpty(footerValue))
            {
                Sprite currencySprite = CasualIconFactory.LoadMainMenuKitSprite($"Icons/{priceIconKey}");
                if (currencySprite != null)
                {
                    GameObject iconObject = new GameObject("FooterIcon", typeof(RectTransform), typeof(Image));
                    iconObject.transform.SetParent(rect, false);
                    RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0.18f, 0.14f);
                    iconRect.anchorMax = new Vector2(0.18f, 0.40f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.sizeDelta = new Vector2(24f, 24f);
                    Image iconImage = iconObject.GetComponent<Image>();
                    iconImage.sprite = currencySprite;
                    iconImage.preserveAspect = true;
                    iconImage.color = Color.white;
                    iconImage.raycastTarget = false;
                }

                Text footer = RuntimeUiFactory.CreateText(rect, "FooterValue", footerValue, 24, TextAnchor.MiddleCenter);
                footer.rectTransform.anchorMin = new Vector2(0f, 0.08f);
                footer.rectTransform.anchorMax = new Vector2(1f, 0.42f);
                footer.rectTransform.offsetMin = new Vector2(34f, 0f);
                footer.rectTransform.offsetMax = new Vector2(-8f, 0f);
                footer.color = Color.white;
                footer.fontStyle = FontStyle.Bold;
                CasualUIStyle.ApplyTextDepth(footer, true);
            }

            return button;
        }

        private void CreateSkinRow(CubeSkinData item)
        {
            bool owned = customizationService.OwnsSkin(item.skinId);
            bool equipped = inventoryStore.Data.selectedSkinId == item.skinId;
            RectTransform row = CreateProductRow(
                $"{item.skinId}Row",
                $"{item.displayName}  [{item.rarity}]\n{item.description}\n{(owned ? (equipped ? "Equipped" : "Owned") : $"Price {item.priceGems} gems")}");
            Text label = row.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.rectTransform.offsetMax = new Vector2(-330f, -8f);
            }
            AddColorChips(row, item.GetColorChips());
            Button preview = CreateRowButton(row, "PreviewButton", "Preview", -176f, 128f);
            preview.onClick.AddListener(() => ShowSkinPreview(item));
            Button button = CreateRowButton(row, owned ? "EquipButton" : "BuyButton", equipped ? "Equipped" : owned ? "Equip" : "Buy", -18f, 128f);
            button.interactable = !equipped;
            button.onClick.AddListener(() =>
            {
                string message;
                if (!customizationService.OwnsSkin(item.skinId))
                {
                    customizationService.TryBuySkin(item, out message);
                }
                else
                {
                    customizationService.EquipSkin(item.skinId, out message);
                }
                Refresh(message);
            });
        }

        private void CreateThemeRow(ThemeData item)
        {
            bool owned = customizationService.OwnsTheme(item.themeId);
            bool equipped = inventoryStore.Data.selectedThemeId == item.themeId;
            RectTransform row = CreateProductRow(
                $"{item.themeId}Row",
                $"{item.displayName}\n{item.description}\n{(owned ? (equipped ? "Equipped" : "Owned") : $"Price {item.priceGems} gems")}");
            AddColorChips(row, new[] { item.backgroundColor, item.panelColor });
            Button button = CreateRowButton(row, owned ? "EquipButton" : "BuyButton", equipped ? "Equipped" : owned ? "Equip" : "Buy");
            button.interactable = !equipped;
            button.onClick.AddListener(() =>
            {
                string message;
                if (!customizationService.OwnsTheme(item.themeId))
                {
                    customizationService.TryBuyTheme(item, out message);
                }
                else
                {
                    customizationService.EquipTheme(item.themeId, out message);
                    ApplyEquippedTheme();
                }
                Refresh(message);
            });
        }

        private static void ApplyEquippedTheme()
        {
            ThemeData theme = VisualCustomizationService.LoadSelectedTheme();
            foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = theme.backgroundColor;
            }

            GameObject background = GameObject.Find("Background");
            Image image = background != null ? background.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = new Color(
                    theme.backgroundColor.r,
                    theme.backgroundColor.g,
                    theme.backgroundColor.b,
                    image.color.a);
            }
        }

        private RectTransform CreateProductRow(string name, string text)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowObject.transform.SetParent(itemContainer, false);
            rowObject.GetComponent<Image>().color = new Color(0.1f, 0.13f, 0.16f, 0.96f);
            rowObject.GetComponent<LayoutElement>().preferredHeight = 92f;
            RectTransform row = rowObject.GetComponent<RectTransform>();
            Text label = RuntimeUiFactory.CreateText(row, "Label", text, 20, TextAnchor.MiddleLeft);
            label.rectTransform.offsetMin = new Vector2(24f, 8f);
            label.rectTransform.offsetMax = new Vector2(-190f, -8f);
            return row;
        }

        private static Button CreateRowButton(RectTransform row, string name, string label)
        {
            return CreateRowButton(row, name, label, -18f, 156f);
        }

        private static Button CreateRowButton(RectTransform row, string name, string label, float x, float width)
        {
            Button button = RuntimeUiFactory.CreateButton(row, name, label, Vector2.zero, new Vector2(width, 56f));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            return button;
        }

        private void CreateSkinCarousel()
        {
            const float previewImageSize = 578f;
            Vector2 titlePosition = new Vector2(0f, -56f);
            Vector2 stateBadgePosition = new Vector2(0f, -126f);
            Vector2 indexBadgePosition = new Vector2(-82f, -46f);
            Vector2 previewImagePosition = new Vector2(0f, 18f);
            Vector2 arrowPositionOffset = new Vector2(-54f, 0f);
            Vector2 dotsPosition = new Vector2(0f, 230f);
            Vector2 actionButtonPosition = new Vector2(0f, 104f);

            skinCarouselDots.Clear();
            GameObject rowObject = new GameObject("SkinCarousel", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Outline), typeof(Shadow));
            rowObject.transform.SetParent(itemContainer, false);
            CasualUIStyle.ApplyPanel(rowObject.GetComponent<Image>(), new Color(0.018f, 0.028f, 0.070f, 0.99f), 32);
            rowObject.GetComponent<Outline>().effectColor = new Color(1f, 0.66f, 0.18f, 0.78f);
            rowObject.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            rowObject.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.75f);
            rowObject.GetComponent<Shadow>().effectDistance = new Vector2(0f, -9f);
            rowObject.GetComponent<LayoutElement>().preferredHeight = 1260f;
            RectTransform row = rowObject.GetComponent<RectTransform>();
            AddCardGloss(row, "SkinBottomGlow", new Vector2(0.16f, 0.07f), new Vector2(0.84f, 0.33f), new Color(0.62f, 0.12f, 1f, 0.07f), 30);

            GameObject featuredObject = new GameObject("FeaturedSkinPanel", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow));
            featuredObject.transform.SetParent(row, false);
            RectTransform featured = featuredObject.GetComponent<RectTransform>();
            featured.anchorMin = new Vector2(0.025f, 0.018f);
            featured.anchorMax = new Vector2(0.975f, 0.985f);
            featured.offsetMin = Vector2.zero;
            featured.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(featuredObject.GetComponent<Image>(), new Color(0.025f, 0.028f, 0.088f, 0.995f), 32);
            Outline featuredOutline = featuredObject.GetComponent<Outline>();
            featuredOutline.effectColor = new Color(1f, 0.70f, 0.20f, 0.95f);
            featuredOutline.effectDistance = new Vector2(5f, -5f);
            featuredObject.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.56f);
            featuredObject.GetComponent<Shadow>().effectDistance = new Vector2(0f, -8f);
        CreateRoundedShowcaseFrame(
            featured,
            "FeaturedVisibleFrame",
            14f,
            new Color(0.025f, 0.028f, 0.088f, 0.995f),
            new Color(1f, 0.66f, 0.18f, 0.82f));

            AddCardGloss(featured, "FeaturedTopSheen", new Vector2(0.26f, 0.91f), new Vector2(0.74f, 0.95f), new Color(1f, 1f, 1f, 0.003f), 24);

            skinPreviewName = RuntimeUiFactory.CreateText(featured, "PreviewName", string.Empty, 52, TextAnchor.UpperCenter);
            skinPreviewName.rectTransform.anchorMin = new Vector2(0f, 1f);
            skinPreviewName.rectTransform.anchorMax = new Vector2(1f, 1f);
            skinPreviewName.rectTransform.pivot = new Vector2(0.5f, 1f);
            skinPreviewName.rectTransform.anchoredPosition = titlePosition;
            skinPreviewName.rectTransform.sizeDelta = new Vector2(-280f, 70f);
            skinPreviewName.resizeTextForBestFit = true;
            skinPreviewName.resizeTextMinSize = 38;
            skinPreviewName.resizeTextMaxSize = 52;
            skinPreviewName.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(skinPreviewName, true);

            skinStateBadge = CreateSkinBadge(
                featured,
                "StateBadge",
                "EQUIPPED",
                new Vector2(0.5f, 1f),
                stateBadgePosition,
                new Vector2(286f, 52f),
                new Color(0.12f, 0.46f, 0.14f, 0.98f),
                new Color(0.82f, 1f, 0.86f, 1f));
            skinStateBadge.fontSize = 28;
            skinStateBadge.resizeTextForBestFit = true;
            skinStateBadge.resizeTextMinSize = 20;
            skinStateBadge.resizeTextMaxSize = 28;
            skinStateBadge.rectTransform.offsetMin = new Vector2(58f, 0f);
            skinStateBadge.rectTransform.offsetMax = new Vector2(-16f, 0f);
            skinStateIconRoot = CreateSkinStateIconRoot(skinStateBadge.transform.parent as RectTransform);

            skinIndexBadge = CreateSkinBadge(
                featured,
                "IndexBadge",
                string.Empty,
                new Vector2(1f, 1f),
                indexBadgePosition,
                new Vector2(104f, 44f),
                new Color(0.035f, 0.040f, 0.105f, 0.94f),
                new Color(1f, 0.88f, 0.34f, 1f));
            skinIndexBadge.fontSize = 24;
            skinIndexBadge.transform.parent.gameObject.SetActive(false);

            GameObject previewGroupObject = new GameObject("PreviewGroup", typeof(RectTransform));
            previewGroupObject.transform.SetParent(featured, false);
            RectTransform previewGroup = previewGroupObject.GetComponent<RectTransform>();
            previewGroup.anchorMin = new Vector2(0.095f, 0.305f);
            previewGroup.anchorMax = new Vector2(0.905f, 0.775f);
            previewGroup.offsetMin = Vector2.zero;
            previewGroup.offsetMax = Vector2.zero;

            GameObject imageObject = new GameObject("SkinCubePreview", typeof(RectTransform), typeof(RawImage), typeof(ShopSkinPreviewOrbit));
            imageObject.transform.SetParent(previewGroup, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = previewImagePosition;
            imageRect.sizeDelta = new Vector2(previewImageSize, previewImageSize);
            skinPreviewImage = imageObject.GetComponent<RawImage>();
            skinPreviewImage.color = Color.white;
            skinPreviewImage.raycastTarget = true;
            skinPreviewOrbit = imageObject.GetComponent<ShopSkinPreviewOrbit>();

            Button previous = RuntimeUiFactory.CreateButton(previewGroup, "PreviousSkinButton", "<", Vector2.zero, new Vector2(88f, 96f));
            CasualUIStyle.ApplyButton(previous, CasualUIColor.Purple);
            RectTransform previousRect = previous.GetComponent<RectTransform>();
            previousRect.anchorMin = new Vector2(0f, 0.5f);
            previousRect.anchorMax = new Vector2(0f, 0.5f);
            previousRect.pivot = new Vector2(0.5f, 0.5f);
            previousRect.anchoredPosition = arrowPositionOffset;
            previousRect.sizeDelta = new Vector2(100f, 110f);
            StyleArrowLabel(previous, 62);
            previous.onClick.AddListener(() => ChangeSelectedSkin(-1));
            Button next = RuntimeUiFactory.CreateButton(previewGroup, "NextSkinButton", ">", Vector2.zero, new Vector2(88f, 96f));
            CasualUIStyle.ApplyButton(next, CasualUIColor.Purple);
            RectTransform nextRect = next.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(1f, 0.5f);
            nextRect.anchorMax = new Vector2(1f, 0.5f);
            nextRect.pivot = new Vector2(0.5f, 0.5f);
            nextRect.anchoredPosition = new Vector2(-arrowPositionOffset.x, arrowPositionOffset.y);
            nextRect.sizeDelta = new Vector2(100f, 110f);
            StyleArrowLabel(next, 62);
            next.onClick.AddListener(() => ChangeSelectedSkin(1));

            CreateSkinCarouselDots(featured, dotsPosition);

            skinPreviewInfo = RuntimeUiFactory.CreateText(featured, "SkinInfo", string.Empty, 22, TextAnchor.MiddleCenter);
            skinPreviewInfo.rectTransform.anchorMin = new Vector2(0f, 0f);
            skinPreviewInfo.rectTransform.anchorMax = new Vector2(1f, 0f);
            skinPreviewInfo.rectTransform.pivot = new Vector2(0.5f, 0f);
            skinPreviewInfo.rectTransform.anchoredPosition = new Vector2(0f, 244f);
            skinPreviewInfo.rectTransform.sizeDelta = new Vector2(-180f, 56f);
            skinPreviewInfo.color = new Color(0.92f, 0.9f, 1f, 1f);
            skinPreviewInfo.gameObject.SetActive(false);

            skinActionButton = RuntimeUiFactory.CreateButton(featured, "SkinActionButton", "Equip", actionButtonPosition, new Vector2(560f, 96f));
            CasualUIStyle.ApplyButton(skinActionButton, CasualUIColor.Purple);
            StyleSkinActionLabel(skinActionButton);
            skinActionButton.onClick.AddListener(UseSelectedSkin);

            EnsureSkinPreviewScene();
            skinPreviewImage.texture = skinPreviewTexture;
            ShowSkinPreview(skinItems[Mathf.Clamp(selectedSkinIndex, 0, skinItems.Count - 1)]);
        }

        private void CreateSkinCarouselDots(RectTransform parent, Vector2 position)
        {
            GameObject dotsObject = new GameObject("CarouselDots", typeof(RectTransform));
            dotsObject.transform.SetParent(parent, false);
            RectTransform dots = dotsObject.GetComponent<RectTransform>();
            dots.anchorMin = new Vector2(0.5f, 0f);
            dots.anchorMax = new Vector2(0.5f, 0f);
            dots.pivot = new Vector2(0.5f, 0f);
            dots.anchoredPosition = position;
            dots.sizeDelta = new Vector2(136f, 30f);

            for (int i = 0; i < 3; i++)
            {
                GameObject dotObject = new GameObject($"Dot{i + 1}", typeof(RectTransform), typeof(Image), typeof(Outline));
                dotObject.transform.SetParent(dots, false);
                RectTransform dot = dotObject.GetComponent<RectTransform>();
                dot.anchorMin = new Vector2(0.5f, 0.5f);
                dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.pivot = new Vector2(0.5f, 0.5f);
                dot.anchoredPosition = new Vector2(-40f + (i * 40f), 0f);
                dot.sizeDelta = new Vector2(i == 1 ? 24f : 18f, i == 1 ? 24f : 18f);

                Image image = dotObject.GetComponent<Image>();
                CasualUIStyle.ApplyPanel(image, i == 1
                    ? new Color(0.82f, 0.28f, 1f, 1f)
                    : new Color(0.68f, 0.58f, 0.24f, 0.92f),
                    18);
                Outline outline = dotObject.GetComponent<Outline>();
                outline.effectColor = new Color(1f, 0.85f, 0.34f, 0.70f);
                outline.effectDistance = new Vector2(1f, -1f);
                image.raycastTarget = false;
                skinCarouselDots.Add(image);
            }
        }

        private static void StyleArrowLabel(Button button, int maxFontSize)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = maxFontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 40;
            label.resizeTextMaxSize = maxFontSize;
            CasualUIStyle.ApplyTextDepth(label, true);
        }

        private static void StyleSkinActionLabel(Button button)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = 36;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 24;
            label.resizeTextMaxSize = 36;
            label.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(label, true);
        }

        private void EnsureSkinPreviewScene()
        {
            if (skinPreviewScene != null)
            {
                skinPreviewScene.SetActive(true);
                return;
            }

            skinPreviewScene = new GameObject("ShopSkinPreviewScene");
            skinPreviewScene.transform.position = new Vector3(600f, 600f, 600f);
            skinPreviewBuilder = skinPreviewScene.AddComponent<CubeVisualBuilder>();

            GameObject cameraObject = new GameObject("ShopSkinPreviewCamera");
            cameraObject.transform.SetParent(skinPreviewScene.transform, false);
            skinPreviewCamera = cameraObject.AddComponent<Camera>();
            skinPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            skinPreviewCamera.backgroundColor = new Color(0.025f, 0.028f, 0.088f, 0f);
            skinPreviewCamera.transform.localPosition = new Vector3(5.82f, 4.38f, -8.35f);
            skinPreviewCamera.fieldOfView = 24f;
            skinPreviewCamera.transform.LookAt(skinPreviewScene.transform.position);

            skinPreviewTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            skinPreviewTexture.name = "ShopSkinPreviewTexture";
            skinPreviewCamera.targetTexture = skinPreviewTexture;

            GameObject lightObject = new GameObject("ShopSkinPreviewLight");
            lightObject.transform.SetParent(skinPreviewScene.transform, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.35f;
            lightObject.transform.localRotation = Quaternion.Euler(45f, -35f, 0f);

            GameObject fillLightObject = new GameObject("ShopSkinPreviewFillLight");
            fillLightObject.transform.SetParent(skinPreviewScene.transform, false);
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(.45f, .65f, 1f);
            fillLight.intensity = .75f;
            fillLightObject.transform.localRotation = Quaternion.Euler(20f, 145f, 0f);
        }

        private void ShowSkinPreview(CubeSkinData skin)
        {
            if (skin == null)
            {
                return;
            }

            EnsureSkinPreviewScene();
            previewedSkin = skin;
            skinPreviewBuilder.SetPreviewSkin(skin);
            skinPreviewBuilder.Build(CubeState.CreateSolved());
            if (skinPreviewBuilder.ViewRoot != null)
            {
                skinPreviewBuilder.ViewRoot.localRotation = Quaternion.Euler(11f, -31f, 0f);
            }
            if (skinPreviewName != null)
            {
                skinPreviewName.text = $"{skin.displayName} ({selectedSkinIndex + 1}/{skinItems.Count})";
            }
            if (skinPreviewImage != null)
            {
                skinPreviewImage.texture = skinPreviewTexture;
            }
            if (skinPreviewOrbit != null)
            {
                skinPreviewOrbit.enabled = true;
                skinPreviewOrbit.Initialize(skinPreviewBuilder.ViewRoot);
            }

            UpdateSkinCarouselText();
        }

        private void ChangeSelectedSkin(int direction)
        {
            if (skinItems.Count == 0)
            {
                return;
            }

            selectedSkinIndex = (selectedSkinIndex + direction + skinItems.Count) % skinItems.Count;
            ShowSkinPreview(skinItems[selectedSkinIndex]);
        }

        private void UseSelectedSkin()
        {
            if (skinItems.Count == 0)
            {
                return;
            }

            CubeSkinData skin = skinItems[Mathf.Clamp(selectedSkinIndex, 0, skinItems.Count - 1)];
            string message;
            if (!customizationService.OwnsSkin(skin.skinId))
            {
                customizationService.TryBuySkin(skin, out message);
            }
            else
            {
                customizationService.EquipSkin(skin.skinId, out message);
            }

            Refresh(message);
        }

        private void UpdateSkinCarouselText()
        {
            if (previewedSkin == null)
            {
                return;
            }

            bool owned = customizationService.OwnsSkin(previewedSkin.skinId);
            bool equipped = inventoryStore.Data.selectedSkinId == previewedSkin.skinId;
            if (skinStateBadge != null)
            {
                skinStateBadge.text = equipped ? "EQUIPPED" : owned ? "OWNED" : "LOCKED";
                skinStateBadge.color = equipped
                    ? new Color(0.90f, 1f, 0.88f, 1f)
                    : owned
                        ? new Color(0.94f, 0.94f, 1f, 1f)
                        : new Color(1f, 0.90f, 0.94f, 1f);
                Image stateBg = skinStateBadge.transform.parent.GetComponent<Image>();
                if (stateBg != null)
                {
                    CasualUIStyle.ApplyPanel(
                        stateBg,
                        equipped ? new Color(0.16f, 0.62f, 0.18f, 0.98f) :
                        owned ? new Color(0.045f, 0.052f, 0.13f, 0.98f) :
                        new Color(0.82f, 0.08f, 0.14f, 0.98f),
                        18);
                }
                SetSkinStateIcon(equipped ? SkinStateVisual.Equipped : owned ? SkinStateVisual.Owned : SkinStateVisual.Locked);
            }
            if (skinIndexBadge != null)
            {
                skinIndexBadge.text = $"{selectedSkinIndex + 1} / {skinItems.Count}";
            }
            for (int i = 0; i < skinCarouselDots.Count; i++)
            {
                Image dot = skinCarouselDots[i];
                if (dot == null)
                {
                    continue;
                }

                bool selected = i == 1;
                dot.color = selected
                    ? new Color(0.82f, 0.28f, 1f, 1f)
                    : new Color(0.68f, 0.58f, 0.24f, 0.92f);
                RectTransform dotRect = dot.GetComponent<RectTransform>();
                if (dotRect != null)
                {
                    dotRect.sizeDelta = selected ? new Vector2(24f, 24f) : new Vector2(18f, 18f);
                }
            }
            if (skinPreviewInfo != null)
            {
                skinPreviewInfo.text = string.Empty;
                skinPreviewInfo.gameObject.SetActive(false);
            }
            if (skinActionButton != null)
            {
                Text label = skinActionButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = equipped ? "Equipped" : owned ? "Equip" : $"Buy {previewedSkin.priceGems} Gems";
                }
                CasualUIStyle.ApplyButton(
                    skinActionButton,
                    equipped ? CasualUIColor.Green :
                    owned ? CasualUIColor.Purple :
                    CasualUIColor.Purple);
                StyleSkinActionLabel(skinActionButton);
                skinActionButton.interactable = true;
            }

        }

        private enum SkinStateVisual
        {
            Equipped,
            Owned,
            Locked
        }

        private static RectTransform CreateSkinStateIconRoot(RectTransform badge)
        {
            GameObject rootObject = new GameObject("StateIcon", typeof(RectTransform));
            rootObject.transform.SetParent(badge, false);
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(40f, 0f);
            root.sizeDelta = new Vector2(42f, 36f);

            CreateStateIconStroke(root, "CheckShortStroke", new Vector2(-7f, -2f), new Vector2(7f, 18f), 42f);
            CreateStateIconStroke(root, "CheckLongStroke", new Vector2(7f, 4f), new Vector2(8f, 34f), -42f);

            CreateStateIconStroke(root, "LockShackle", new Vector2(0f, 9f), new Vector2(26f, 8f), 0f);
            CreateStateIconStroke(root, "LockLeftShackle", new Vector2(-11f, 3f), new Vector2(7f, 16f), 0f);
            CreateStateIconStroke(root, "LockRightShackle", new Vector2(11f, 3f), new Vector2(7f, 16f), 0f);

            GameObject lockBodyObject = new GameObject("LockBody", typeof(RectTransform), typeof(Image), typeof(Outline));
            lockBodyObject.transform.SetParent(root, false);
            RectTransform lockBody = lockBodyObject.GetComponent<RectTransform>();
            lockBody.anchorMin = new Vector2(0.5f, 0.5f);
            lockBody.anchorMax = new Vector2(0.5f, 0.5f);
            lockBody.pivot = new Vector2(0.5f, 0.5f);
            lockBody.anchoredPosition = new Vector2(0f, -5f);
            lockBody.sizeDelta = new Vector2(28f, 20f);
            CasualUIStyle.ApplyPanel(lockBodyObject.GetComponent<Image>(), new Color(1f, 0.92f, 0.82f, 1f), 6);
            Outline bodyOutline = lockBodyObject.GetComponent<Outline>();
            bodyOutline.effectColor = new Color(0.55f, 0.05f, 0.08f, 0.55f);
            bodyOutline.effectDistance = new Vector2(1f, -1f);

            CreateStateIconStroke(root, "LockKeyhole", new Vector2(0f, -5f), new Vector2(5f, 10f), 0f);
            rootObject.SetActive(false);
            return root;
        }

        private static void CreateStateIconStroke(RectTransform parent, string name, Vector2 position, Vector2 size, float rotation)
        {
            GameObject strokeObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            strokeObject.transform.SetParent(parent, false);
            RectTransform stroke = strokeObject.GetComponent<RectTransform>();
            stroke.anchorMin = new Vector2(0.5f, 0.5f);
            stroke.anchorMax = new Vector2(0.5f, 0.5f);
            stroke.pivot = new Vector2(0.5f, 0.5f);
            stroke.anchoredPosition = position;
            stroke.sizeDelta = size;
            stroke.localRotation = Quaternion.Euler(0f, 0f, rotation);
            strokeObject.GetComponent<Image>().raycastTarget = false;
        }

        private void SetSkinStateIcon(SkinStateVisual visual)
        {
            if (skinStateIconRoot == null)
            {
                return;
            }

            bool showCheck = visual == SkinStateVisual.Equipped;
            bool showLock = visual == SkinStateVisual.Locked;
            skinStateIconRoot.gameObject.SetActive(showCheck || showLock);

            foreach (Transform child in skinStateIconRoot)
            {
                bool isCheck = child.name.StartsWith("Check", StringComparison.Ordinal);
                bool isLock = child.name.StartsWith("Lock", StringComparison.Ordinal);
                child.gameObject.SetActive((showCheck && isCheck) || (showLock && isLock));

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.color = showCheck
                        ? new Color(0.88f, 1f, 0.74f, 1f)
                        : child.name == "LockKeyhole"
                            ? new Color(0.45f, 0.03f, 0.05f, 0.95f)
                            : new Color(1f, 0.92f, 0.82f, 1f);
                    image.raycastTarget = false;
                }
            }
        }

        private static Text CreateSkinBadge(
            RectTransform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Color backgroundColor,
            Color textColor)
        {
            GameObject badgeObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            badgeObject.transform.SetParent(parent, false);
            RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
            badgeRect.anchorMin = anchor;
            badgeRect.anchorMax = anchor;
            badgeRect.pivot = new Vector2(anchor.x, anchor.y >= 0.5f ? 1f : 0f);
            badgeRect.anchoredPosition = anchoredPosition;
            badgeRect.sizeDelta = size;

            Image badgeImage = badgeObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(badgeImage, backgroundColor, 18);
            Outline outline = badgeObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.35f, 0.28f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text badgeText = RuntimeUiFactory.CreateText(badgeRect, "Label", label, 20, TextAnchor.MiddleCenter);
            badgeText.color = textColor;
            badgeText.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(badgeText, false);
            return badgeText;
        }

        private int FindSkinIndex(string skinId)
        {
            for (int i = 0; i < skinItems.Count; i++)
            {
                if (skinItems[i].skinId == skinId)
                {
                    return i;
                }
            }

            return 0;
        }

        private static void AddColorChips(RectTransform row, IReadOnlyList<Color> colors)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                GameObject chip = new GameObject($"ColorChip{i}", typeof(RectTransform), typeof(Image));
                chip.transform.SetParent(row, false);
                RectTransform rect = chip.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(24f + (i * 25f), 8f);
                rect.sizeDelta = new Vector2(20f, 12f);
                chip.GetComponent<Image>().color = colors[i];
            }
        }

        private void RefreshTabVisuals()
        {
            SetTabColor(coinTabButton, selectedTab == ShopTab.CoinItems);
            SetTabColor(gemTabButton, selectedTab == ShopTab.GemItems);
            SetTabColor(skinTabButton, selectedTab == ShopTab.Skins);
            SetTabColor(themeTabButton, selectedTab == ShopTab.Promotion);
        }

        private static void SetTabColor(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            CasualUIStyle.ApplyButton(button, selected ? CasualUIColor.Purple : CasualUIColor.Slate);
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected
                    ? new Color(0.80f, 0.20f, 0.98f, 1f)
                    : new Color(0.12f, 0.17f, 0.30f, 1f);
            }

            Image iconHolder = button.transform.Find("TabIconHolder")?.GetComponent<Image>();
            if (iconHolder != null)
            {
                iconHolder.color = selected
                    ? new Color(1f, 1f, 1f, 0.10f)
                    : new Color(1f, 1f, 1f, 0.04f);
            }
        }

        private string BuildItemText(ShopItemDefinition item)
        {
            return $"{GetItemName(item.itemType)}\nPrice {item.coinPrice} coins | Owned {GetOwnedCount(item.itemType)}";
        }

        private int GetOwnedCount(StageAssistItemType itemType)
        {
            return itemType == StageAssistItemType.NicknameTicket
                ? profileStore.Current?.nicknameChangeTickets ?? 0
                : inventoryStore.GetCount(itemType);
        }

        private static string GetItemName(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    return "Undo Item";
                case StageAssistItemType.MovePlus1:
                    return "+1 Move Item";
                case StageAssistItemType.MovePlus2:
                    return "+2 Move Item";
                case StageAssistItemType.MovePlus3:
                    return "+3 Move Item";
                case StageAssistItemType.SolverTicket:
                    return "Solver Ticket";
                case StageAssistItemType.NicknameTicket:
                    return "Nickname Ticket";
                default:
                    return itemType.ToString();
            }
        }

        private static string GetItemDescription(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    return "Undo your last move.";
                case StageAssistItemType.MovePlus1:
                    return "Add 1 move to your limit.";
                case StageAssistItemType.MovePlus2:
                    return "Add 2 moves to your limit.";
                case StageAssistItemType.MovePlus3:
                    return "Add 3 moves to your limit.";
                case StageAssistItemType.SolverTicket:
                    return "Solve the puzzle instantly.";
                case StageAssistItemType.NicknameTicket:
                    return "Change your nickname once.";
                default:
                    return "Useful stage assist item.";
            }
        }

        private static string GetCoinItemIconKey(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    return "undo";
                case StageAssistItemType.MovePlus1:
                    return "plus1_item";
                case StageAssistItemType.MovePlus2:
                    return "plus2_item";
                case StageAssistItemType.MovePlus3:
                    return "plus3_item";
                case StageAssistItemType.SolverTicket:
                    return "solver_ticket";
                case StageAssistItemType.NicknameTicket:
                    return "nickname_ticket";
                default:
                    return null;
            }
        }

        private static string GetFallbackIconText(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    return "\u21B6";
                case StageAssistItemType.MovePlus1:
                    return "+1";
                case StageAssistItemType.MovePlus2:
                    return "+2";
                case StageAssistItemType.MovePlus3:
                    return "+3";
                case StageAssistItemType.SolverTicket:
                    return "\u2605";
                case StageAssistItemType.NicknameTicket:
                    return "ID";
                default:
                    return "?";
            }
        }

        private void BuyItem(ShopItemDefinition item)
        {
            if (item.itemType == StageAssistItemType.NicknameTicket && !profileStore.Exists())
            {
                Refresh("Create a profile first.");
                return;
            }

            if (item.itemType == StageAssistItemType.NicknameTicket
                && GetOwnedCount(item.itemType) >= PlayerProfileStore.MaxNicknameChangeTickets)
            {
                Refresh("You can hold up to 3 Nickname Tickets.");
                return;
            }

            if (!walletStore.SpendCoins(item.coinPrice))
            {
                Refresh(UIStrings.NotEnoughCoins);
                return;
            }

            if (item.itemType == StageAssistItemType.NicknameTicket)
            {
                if (!profileStore.AddNicknameChangeTickets(item.quantity))
                {
                    walletStore.AddCoins(item.coinPrice);
                    Refresh("Could not add nickname ticket.");
                    return;
                }
            }
            else
            {
                inventoryStore.Add(item.itemType, item.quantity);
            }

            Debug.Log($"[Shop] Purchase success itemId={item.itemType} newCount={GetOwnedCount(item.itemType)}");
            Refresh(UIStrings.Purchased);
        }

        private void HandleExternalEconomyChanged()
        {
            if (root != null && root.activeInHierarchy)
            {
                Refresh(string.Empty);
            }
        }

        private void HandleExternalInventoryChanged()
        {
            if (root != null && root.activeInHierarchy)
            {
                Refresh(string.Empty);
            }
        }

        private void HandleExternalProfileChanged()
        {
            if (root != null && root.activeInHierarchy)
            {
                Refresh(string.Empty);
            }
        }

        private void WatchAdForCoins()
        {
            if (rewardService is RewardedAdService ads)
            {
                if (!ads.CanRequest(RewardedAdPlacement.ShopCoinReward))
                {
                    Refresh(ads.GetUnavailableMessage(RewardedAdPlacement.ShopCoinReward));
                    return;
                }

                ads.Show(
                    RewardedAdPlacement.ShopCoinReward,
                    () =>
                    {
                        int reward = ads.Config.dailyCoinRewardAmount;
                        int before = walletStore.Coins;
                        walletStore.AddCoins(reward);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        int after = walletStore.Coins;
                        Debug.Log($"[ShopCoinAd] dailyCount={ads.DailyShopCoinAdsUsed}, reward={reward}, canWatch={ads.CanRequest(RewardedAdPlacement.ShopCoinReward)}");
                        Debug.Log($"[AdRewardApplied] placement={RewardedAdPlacement.ShopCoinReward}, before={before}, after={after}");
#endif
                        Refresh($"{UIStrings.RewardClaimed} +{reward} Coins");
                    },
                    result =>
                    {
                        if (result == RewardedAdResult.Rewarded)
                        {
                            return;
                        }

                        Refresh(GetRewardedAdFailureMessage(result, RewardedAdPlacement.ShopCoinReward));
                    });
                return;
            }

            rewardService.ShowReward(RewardType.EarnCoins, completed =>
            {
                if (!completed)
                {
                    Refresh(UIStrings.AdNotCompleted);
                    return;
                }

                int reward = rewardService is RewardedAdService ads
                    ? ads.Config.dailyCoinRewardAmount
                    : AdsConfig.Default.dailyCoinRewardAmount;
                int before = walletStore.Coins;
                walletStore.AddCoins(reward);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                int after = walletStore.Coins;
                int dailyCount = rewardService is RewardedAdService service ? service.DailyShopCoinAdsUsed : -1;
                Debug.Log($"[ShopCoinAd] dailyCount={dailyCount}, reward={reward}, canWatch={rewardService.IsRewardAvailable(RewardType.EarnCoins)}");
                Debug.Log($"[AdRewardApplied] placement={RewardedAdPlacement.ShopCoinReward}, before={before}, after={after}");
#endif
                Refresh($"{UIStrings.RewardClaimed} +{reward} Coins");
            });
        }

        private string GetRewardedAdFailureMessage(RewardedAdResult result, RewardedAdPlacement placement)
        {
            switch (result)
            {
                case RewardedAdResult.NotReady:
                    return "Ad is loading. Try again shortly.";
                case RewardedAdResult.Closed:
                    return UIStrings.AdNotCompleted;
                case RewardedAdResult.LimitReached:
                case RewardedAdResult.Unavailable:
                    return rewardService is RewardedAdService ads
                        ? ads.GetUnavailableMessage(placement)
                        : "Reward is not available.";
                case RewardedAdResult.Busy:
                    return "Ad is already opening.";
                default:
                    return UIStrings.AdNotCompleted;
            }
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

        private enum ShopTab
        {
            CoinItems,
            GemItems,
            Skins,
            Promotion
        }
    }
}
