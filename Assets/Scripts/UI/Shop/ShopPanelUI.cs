using System.Collections.Generic;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Shop
{
    public sealed class ShopPanelUI
    {
        private readonly GameObject root;
        private readonly WalletStore walletStore;
        private readonly InventoryStore inventoryStore;
        private readonly IRewardService rewardService;
        private readonly VisualCustomizationService customizationService;
        private readonly List<ShopItemDefinition> shopItems;
        private readonly IReadOnlyList<CubeSkinData> skinItems;
        private readonly IReadOnlyList<ThemeData> themeItems;
        private readonly Text walletText;
        private readonly Text statusText;
        private readonly RectTransform itemContainer;
        private readonly Button coinTabButton;
        private readonly Button gemTabButton;
        private readonly Button skinTabButton;
        private readonly Button themeTabButton;
        private readonly Button adRewardButton;
        private RenderTexture skinPreviewTexture;
        private GameObject skinPreviewScene;
        private Camera skinPreviewCamera;
        private CubeVisualBuilder skinPreviewBuilder;
        private RawImage skinPreviewImage;
        private Text skinPreviewName;
        private Text skinPreviewInfo;
        private Button skinActionButton;
        private ShopSkinPreviewOrbit skinPreviewOrbit;
        private CubeSkinData previewedSkin;
        private int selectedSkinIndex;
        private ShopTab selectedTab;
        private readonly Vector2 minPanelSize = new Vector2(680f, 760f);
        private readonly Vector2 maxPanelSize = new Vector2(980f, 1280f);

        public ShopPanelUI(Transform parent, WalletStore wallet, InventoryStore inventory, IRewardService rewards)
        {
            walletStore = wallet ?? new WalletStore();
            inventoryStore = inventory ?? new InventoryStore();
            rewardService = rewards ?? RewardedAdService.CreateDefault();
            customizationService = new VisualCustomizationService(walletStore, inventoryStore);
            shopItems = new List<ShopItemDefinition>(ShopItemDefinition.CreateDefaults());
            skinItems = VisualCustomizationCatalog.GetSkins();
            themeItems = VisualCustomizationCatalog.GetThemes();

            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "ShopCanvas", 1480);
            root = canvas.gameObject;
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

            AddCloseButton(panel);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Shop", 38, TextAnchor.UpperCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            title.rectTransform.sizeDelta = new Vector2(-120f, 56f);

            walletText = RuntimeUiFactory.CreateText(panel, "Wallet", string.Empty, 25, TextAnchor.UpperCenter);
            walletText.rectTransform.anchorMin = new Vector2(0f, 1f);
            walletText.rectTransform.anchorMax = new Vector2(1f, 1f);
            walletText.rectTransform.pivot = new Vector2(0.5f, 1f);
            walletText.rectTransform.anchoredPosition = new Vector2(0f, -126f);
            walletText.rectTransform.sizeDelta = new Vector2(-80f, 42f);
            walletText.gameObject.SetActive(false);

            coinTabButton = RuntimeUiFactory.CreateButton(panel, "CoinItemsTab", "Coin Items", new Vector2(-288f, 720f), new Vector2(170f, 54f));
            gemTabButton = RuntimeUiFactory.CreateButton(panel, "GemItemsTab", "Gem Items", new Vector2(-96f, 720f), new Vector2(170f, 54f));
            skinTabButton = RuntimeUiFactory.CreateButton(panel, "SkinsTab", "Skins", new Vector2(96f, 720f), new Vector2(170f, 54f));
            themeTabButton = RuntimeUiFactory.CreateButton(panel, "ThemesTab", "Themes", new Vector2(288f, 720f), new Vector2(170f, 54f));
            SetTopButton(coinTabButton, -150f, -288f);
            SetTopButton(gemTabButton, -150f, -96f);
            SetTopButton(skinTabButton, -150f, 96f);
            SetTopButton(themeTabButton, -150f, 288f);
            coinTabButton.onClick.AddListener(() => SelectTab(ShopTab.CoinItems));
            gemTabButton.onClick.AddListener(() => SelectTab(ShopTab.GemItems));
            skinTabButton.onClick.AddListener(() => SelectTab(ShopTab.Skins));
            themeTabButton.onClick.AddListener(() => SelectTab(ShopTab.Themes));

            itemContainer = CreateItemContainer(panel);

            adRewardButton = RuntimeUiFactory.CreateButton(panel, "WatchAdCoinsButton", "Watch Ad: +100 coins", new Vector2(-178f, 92f), new Vector2(340f, 56f));
            adRewardButton.onClick.AddListener(WatchAdForCoins);

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Close", new Vector2(178f, 92f), new Vector2(260f, 56f));
            close.onClick.AddListener(Hide);

            Button addGems = RuntimeUiFactory.CreateButton(panel, "DebugAddGemsButton", "DEV +100 Gems", new Vector2(-178f, 28f), new Vector2(260f, 46f));
            addGems.onClick.AddListener(() =>
            {
                walletStore.AddGems(100);
                Refresh("Added 100 gems.");
            });
            Button resetVisuals = RuntimeUiFactory.CreateButton(panel, "DebugResetVisualsButton", "DEV Reset Visuals", new Vector2(178f, 28f), new Vector2(260f, 46f));
            resetVisuals.onClick.AddListener(() =>
            {
                inventoryStore.ResetVisualCustomizations();
                Refresh("Skins and themes reset.");
            });
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            addGems.gameObject.SetActive(false);
            resetVisuals.gameObject.SetActive(false);
#endif

            statusText = RuntimeUiFactory.CreateText(panel, "Status", string.Empty, 22, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0f, 82f);
            statusText.rectTransform.sizeDelta = new Vector2(-80f, 52f);

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

        public void Show()
        {
            SelectTab(ShopTab.CoinItems);
            root.SetActive(true);
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
            GameObject viewportObject = new GameObject("ShopViewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(40f, 160f);
            viewport.offsetMax = new Vector2(-40f, -280f);
            viewportObject.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 0.75f);
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
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
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

        private void Refresh(string message)
        {
            statusText.text = message;
            if (rewardService is RewardedAdService ads)
            {
                int remaining = ads.GetRemaining(RewardedAdPlacement.DailyCoins);
                Text label = adRewardButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.fontSize = 20;
                    label.text = ads.IsPlacementAvailable(RewardedAdPlacement.DailyCoins)
                        ? $"Watch Ad for +{ads.Config.dailyCoinRewardAmount} Coins  ({remaining}/{ads.Config.dailyCoinAdsMax} left)"
                        : "Ad not available";
                }
                adRewardButton.interactable = ads.CanShow(RewardedAdPlacement.DailyCoins);
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
                Object.Destroy(itemContainer.GetChild(i).gameObject);
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
                CreateHeartPackRow("Small Heart Pack", "Restore 3 hearts.", 10, 3);
                CreateHeartPackRow("Medium Heart Pack", "Restore 10 hearts.", 25, 10);
                CreateHeartPackRow("Large Heart Pack", "Restore 30 hearts.", 60, 30);
                CreateGemExchangeRow("Small Coin Pack", "Exchange gems for 500 coins.", 10, 500);
                CreateGemExchangeRow("Medium Coin Pack", "Exchange gems for 1500 coins.", 25, 1500);
                CreateGemExchangeRow("Large Coin Pack", "Exchange gems for 4500 coins.", 60, 4500);
            }
            else if (selectedTab == ShopTab.Skins)
            {
                CreateSkinCarousel();
            }
            else
            {
                foreach (ThemeData item in themeItems)
                {
                    CreateThemeRow(item);
                }
            }

            RefreshTabVisuals();
        }

        private void SelectTab(ShopTab tab)
        {
            selectedTab = tab;
            if (tab == ShopTab.Skins && previewedSkin == null)
            {
                selectedSkinIndex = FindSkinIndex(customizationService.SelectedSkin.skinId);
            }
            Refresh(tab == ShopTab.GemItems ? "Use gems for hearts or coin packs." : string.Empty);
        }

        private void CreateShopRow(ShopItemDefinition item)
        {
            GameObject rowObject = new GameObject($"{item.itemType}Row", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowObject.transform.SetParent(itemContainer, false);
            rowObject.GetComponent<Image>().color = new Color(0.1f, 0.13f, 0.16f, 0.96f);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 76f;

            RectTransform row = rowObject.GetComponent<RectTransform>();
            Text label = RuntimeUiFactory.CreateText(row, "Label", BuildItemText(item), 22, TextAnchor.MiddleLeft);
            label.rectTransform.offsetMin = new Vector2(24f, 0f);
            label.rectTransform.offsetMax = new Vector2(-190f, 0f);

            Button buy = RuntimeUiFactory.CreateButton(row, "BuyButton", "Buy", new Vector2(0f, 10f), new Vector2(150f, 54f));
            RectTransform buyRect = buy.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(1f, 0f);
            buyRect.anchorMax = new Vector2(1f, 0f);
            buyRect.pivot = new Vector2(1f, 0f);
            buyRect.anchoredPosition = new Vector2(-18f, 10f);
            buy.onClick.AddListener(() => BuyItem(item));
        }

        private void CreateGemExchangeRow(string title, string description, int gemPrice, int coinAmount)
        {
            RectTransform row = CreateProductRow($"{title.Replace(" ", string.Empty)}Row", $"{title}\n{description}\nPrice {gemPrice} gems");
            Button buy = CreateRowButton(row, "ExchangeButton", "Exchange");
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

        private void CreateHeartPackRow(string title, string description, int gemPrice, int heartAmount)
        {
            RectTransform row = CreateProductRow(
                $"{title.Replace(" ", string.Empty)}Row",
                $"\u2665  {title}\n+{heartAmount} Hearts\n{gemPrice} Gems");
            Button buy = CreateRowButton(row, "BuyHeartPackButton", "Buy");
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
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
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
            GameObject rowObject = new GameObject("SkinCarousel", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowObject.transform.SetParent(itemContainer, false);
            rowObject.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.98f);
            rowObject.GetComponent<LayoutElement>().preferredHeight = 570f;
            RectTransform row = rowObject.GetComponent<RectTransform>();

            skinPreviewName = RuntimeUiFactory.CreateText(row, "PreviewName", string.Empty, 28, TextAnchor.UpperCenter);
            skinPreviewName.rectTransform.anchorMin = new Vector2(0f, 1f);
            skinPreviewName.rectTransform.anchorMax = new Vector2(1f, 1f);
            skinPreviewName.rectTransform.pivot = new Vector2(0.5f, 1f);
            skinPreviewName.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            skinPreviewName.rectTransform.sizeDelta = new Vector2(-140f, 64f);

            GameObject imageObject = new GameObject("SkinCubePreview", typeof(RectTransform), typeof(RawImage), typeof(ShopSkinPreviewOrbit));
            imageObject.transform.SetParent(row, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = new Vector2(0f, 30f);
            imageRect.sizeDelta = new Vector2(360f, 360f);
            skinPreviewImage = imageObject.GetComponent<RawImage>();
            skinPreviewOrbit = imageObject.GetComponent<ShopSkinPreviewOrbit>();

            Button previous = RuntimeUiFactory.CreateButton(row, "PreviousSkinButton", "<", new Vector2(-270f, 245f), new Vector2(76f, 88f));
            previous.onClick.AddListener(() => ChangeSelectedSkin(-1));
            Button next = RuntimeUiFactory.CreateButton(row, "NextSkinButton", ">", new Vector2(270f, 245f), new Vector2(76f, 88f));
            next.onClick.AddListener(() => ChangeSelectedSkin(1));

            skinPreviewInfo = RuntimeUiFactory.CreateText(row, "SkinInfo", string.Empty, 20, TextAnchor.MiddleCenter);
            skinPreviewInfo.rectTransform.anchorMin = new Vector2(0f, 0f);
            skinPreviewInfo.rectTransform.anchorMax = new Vector2(1f, 0f);
            skinPreviewInfo.rectTransform.pivot = new Vector2(0.5f, 0f);
            skinPreviewInfo.rectTransform.anchoredPosition = new Vector2(0f, 66f);
            skinPreviewInfo.rectTransform.sizeDelta = new Vector2(-80f, 82f);

            skinActionButton = RuntimeUiFactory.CreateButton(row, "SkinActionButton", "Equip", new Vector2(0f, 8f), new Vector2(300f, 52f));
            skinActionButton.onClick.AddListener(UseSelectedSkin);

            EnsureSkinPreviewScene();
            skinPreviewImage.texture = skinPreviewTexture;
            ShowSkinPreview(skinItems[Mathf.Clamp(selectedSkinIndex, 0, skinItems.Count - 1)]);
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
            skinPreviewCamera.backgroundColor = new Color(0.015f, 0.02f, 0.028f, 1f);
            skinPreviewCamera.transform.localPosition = new Vector3(4.25f, 3.55f, -6f);
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
                skinPreviewBuilder.ViewRoot.localRotation = Quaternion.Euler(18f, -28f, 0f);
            }
            if (skinPreviewName != null)
            {
                skinPreviewName.text = $"{skin.displayName}  [{skin.rarity}]    {selectedSkinIndex + 1} / {skinItems.Count}";
            }
            if (skinPreviewImage != null)
            {
                skinPreviewImage.texture = skinPreviewTexture;
            }
            if (skinPreviewOrbit != null)
            {
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
            if (skinPreviewInfo != null)
            {
                string state = equipped ? "Equipped" : owned ? "Owned" : $"Price {previewedSkin.priceGems} gems";
                skinPreviewInfo.text = $"{previewedSkin.description}\n{state}";
            }
            if (skinActionButton != null)
            {
                Text label = skinActionButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = equipped ? "Equipped" : owned ? "Equip" : $"Buy  {previewedSkin.priceGems} Gems";
                }
                skinActionButton.interactable = !equipped;
            }
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
            SetTabColor(themeTabButton, selectedTab == ShopTab.Themes);
        }

        private static void SetTabColor(Button button, bool selected)
        {
            Image image = button != null ? button.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = selected
                    ? new Color(0.18f, 0.42f, 0.5f, 1f)
                    : new Color(0.12f, 0.16f, 0.2f, 0.96f);
            }
        }

        private string BuildItemText(ShopItemDefinition item)
        {
            return $"{GetItemName(item.itemType)}\nPrice {item.coinPrice} coins | Owned {inventoryStore.GetCount(item.itemType)}";
        }

        private static string GetItemName(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    return "Undo item";
                case StageAssistItemType.MovePlus1:
                    return "+1 move item";
                case StageAssistItemType.MovePlus2:
                    return "+2 move item";
                case StageAssistItemType.MovePlus3:
                    return "+3 move item";
                case StageAssistItemType.SolverTicket:
                    return "Solver Ticket";
                default:
                    return itemType.ToString();
            }
        }

        private void BuyItem(ShopItemDefinition item)
        {
            if (!walletStore.SpendCoins(item.coinPrice))
            {
                Refresh(UIStrings.NotEnoughCoins);
                return;
            }

            inventoryStore.Add(item.itemType, item.quantity);
            Refresh(UIStrings.Purchased);
        }

        private void WatchAdForCoins()
        {
            if (!rewardService.IsRewardAvailable(RewardType.EarnCoins))
            {
                Refresh(rewardService is RewardedAdService ads
                    ? ads.GetUnavailableMessage(RewardedAdPlacement.DailyCoins)
                    : "Reward is not available.");
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
                    : 100;
                walletStore.AddCoins(reward);
                Refresh($"{UIStrings.RewardClaimed} +{reward} Coins");
            });
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
            Themes
        }
    }
}
