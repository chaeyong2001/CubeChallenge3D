using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using CubeChallenge3D.Audio;
using CubeChallenge3D.UI.Style;

namespace CubeChallenge3D.UI.Common
{
    public static class RuntimeUiFactory
    {
        public enum StatusBadgeStyle
        {
            Neutral,
            Today,
            Locked,
            Claimed,
            Equipped,
            Premium
        }

        public sealed class StatusBadgeView
        {
            public StatusBadgeView(RectTransform root, Image image, Text label)
            {
                Root = root;
                Image = image;
                Label = label;
            }

            public RectTransform Root { get; }
            public Image Image { get; }
            public Text Label { get; }
        }

        public sealed class RewardCardView
        {
            public RewardCardView(
                RectTransform root,
                Button button,
                Image image,
                Image dayBadgeImage,
                Text dayLabel,
                StatusBadgeView statusBadge,
                Image iconGlow,
                Image iconImage,
                Image rewardBarImage,
                Text rewardLabel,
                bool usesArtBackground)
            {
                Root = root;
                Button = button;
                Image = image;
                DayBadgeImage = dayBadgeImage;
                DayLabel = dayLabel;
                StatusBadge = statusBadge;
                IconGlow = iconGlow;
                IconImage = iconImage;
                RewardBarImage = rewardBarImage;
                RewardLabel = rewardLabel;
                UsesArtBackground = usesArtBackground;
            }

            public RectTransform Root { get; }
            public Button Button { get; }
            public Image Image { get; }
            public Image DayBadgeImage { get; }
            public Text DayLabel { get; }
            public StatusBadgeView StatusBadge { get; }
            public Image IconGlow { get; }
            public Image IconImage { get; }
            public Image RewardBarImage { get; }
            public Text RewardLabel { get; }
            public bool UsesArtBackground { get; }
        }

        public sealed class TabButtonView
        {
            public TabButtonView(Button button, Image image, Text label, RectTransform iconRoot)
            {
                Button = button;
                Image = image;
                Label = label;
                IconRoot = iconRoot;
            }

            public Button Button { get; }
            public Image Image { get; }
            public Text Label { get; }
            public RectTransform IconRoot { get; }
        }

        public sealed class ShopItemCardView
        {
            public ShopItemCardView(
                RectTransform root,
                Image image,
                RectTransform iconArea,
                Text title,
                Text description,
                RectTransform priceRow,
                Text priceText,
                Text effectText,
                Button actionButton)
            {
                Root = root;
                Image = image;
                IconArea = iconArea;
                Title = title;
                Description = description;
                PriceRow = priceRow;
                PriceText = priceText;
                EffectText = effectText;
                ActionButton = actionButton;
            }

            public RectTransform Root { get; }
            public Image Image { get; }
            public RectTransform IconArea { get; }
            public Text Title { get; }
            public Text Description { get; }
            public RectTransform PriceRow { get; }
            public Text PriceText { get; }
            public Text EffectText { get; }
            public Button ActionButton { get; }
        }

        public sealed class PremiumShowcaseCardView
        {
            public PremiumShowcaseCardView(
                RectTransform root,
                Image image,
                Text title,
                StatusBadgeView statusBadge,
                Text indexText,
                RectTransform previewArea,
                RawImage previewImage,
                Button previousButton,
                Button nextButton,
                Button actionButton)
            {
                Root = root;
                Image = image;
                Title = title;
                StatusBadge = statusBadge;
                IndexText = indexText;
                PreviewArea = previewArea;
                PreviewImage = previewImage;
                PreviousButton = previousButton;
                NextButton = nextButton;
                ActionButton = actionButton;
            }

            public RectTransform Root { get; }
            public Image Image { get; }
            public Text Title { get; }
            public StatusBadgeView StatusBadge { get; }
            public Text IndexText { get; }
            public RectTransform PreviewArea { get; }
            public RawImage PreviewImage { get; }
            public Button PreviousButton { get; }
            public Button NextButton { get; }
            public Button ActionButton { get; }
        }

        private static Sprite rewardCardBlueSprite;
        private static Sprite rewardCardPurpleWideSprite;

        public static Canvas CreateCanvas(
            Transform parent,
            string name,
            int sortingOrder,
            float matchWidthOrHeight = 0.5f)
        {
            EnsureEventSystem();
            GameObject canvasObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Mathf.Clamp01(matchWidthOrHeight);
            if (ShouldShowCurrencyBar(name))
            {
                TopCurrencyBar.Attach(canvas);
            }
            return canvas;
        }

        private static bool ShouldShowCurrencyBar(string canvasName)
        {
            if (string.IsNullOrEmpty(canvasName) || canvasName == "Canvas")
            {
                return false;
            }

            return !canvasName.Contains("Modal")
                && !canvasName.Contains("Popup")
                && !canvasName.Contains("Utility")
                && !canvasName.Contains("Mobile")
                && !canvasName.Contains("Diagnostics")
                && !canvasName.Contains("Help")
                && !canvasName.Contains("Playback");
        }

        public static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panelObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(0.04f, 0.05f, 0.09f, 0.94f), 20);
            return rect;
        }

        public static Button CreateButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Button button = buttonObject.GetComponent<Button>();
            AudioFeedbackManager.RegisterButton(button);
            CasualUIStyle.ApplyButton(button, CasualUIColor.Slate);
            Text text = CreateText(rect, "Label", label, 28, TextAnchor.MiddleCenter);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 28;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            return button;
        }

        public static Text CreateText(
            RectTransform parent,
            string name,
            string label,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1f;
            text.raycastTarget = false;
            return text;
        }

        public static StatusBadgeView CreateStatusBadge(
            Transform parent,
            string name,
            string label,
            StatusBadgeStyle style,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject badgeObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            badgeObject.transform.SetParent(parent, false);
            RectTransform rect = badgeObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = badgeObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, GetStatusBadgeColor(style), 14);

            Text text = CreateText(rect, "Label", label, 18, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 18;
            CasualUIStyle.ApplyTextDepth(text, false);
            return new StatusBadgeView(rect, image, text);
        }

        public static void SetStatusBadge(StatusBadgeView badge, string label, StatusBadgeStyle style)
        {
            if (badge == null)
            {
                return;
            }

            badge.Label.text = label;
            badge.Image.color = GetStatusBadgeColor(style);
            badge.Label.color = style == StatusBadgeStyle.Today
                ? new Color(0.16f, 0.09f, 0.02f, 1f)
                : Color.white;
            badge.Root.sizeDelta = style == StatusBadgeStyle.Today
                ? new Vector2(92f, 30f)
                : style == StatusBadgeStyle.Claimed
                    ? new Vector2(112f, 30f)
                    : new Vector2(96f, 30f);
        }

        public static TabButtonView CreateTabButton(
            RectTransform parent,
            string name,
            string label,
            string iconKey,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            bool selected)
        {
            Button button = CreateSimpleButton(parent, name, string.Empty, anchor, new Vector2(0.5f, 0.5f), position, size, GetTabColor(selected));
            RectTransform rect = button.GetComponent<RectTransform>();
            Image image = button.GetComponent<Image>();

            RectTransform iconRoot = null;
            if (!string.IsNullOrEmpty(iconKey))
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
                iconObject.transform.SetParent(rect, false);
                iconRoot = iconObject.GetComponent<RectTransform>();
                iconRoot.anchorMin = new Vector2(0.08f, 0.2f);
                iconRoot.anchorMax = new Vector2(0.27f, 0.8f);
                iconRoot.offsetMin = Vector2.zero;
                iconRoot.offsetMax = Vector2.zero;
                if (!CasualIconFactory.TryCreateMainMenuKitIcon(iconRoot, iconKey, out _))
                {
                    CasualIconFactory.Create(iconRoot, iconKey, Color.white);
                }
            }

            Text text = CreateText(rect, "Label", label, 24, iconRoot == null ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 24;
            text.rectTransform.anchorMin = iconRoot == null ? Vector2.zero : new Vector2(0.31f, 0f);
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = iconRoot == null ? new Vector2(8f, 0f) : Vector2.zero;
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            CasualUIStyle.ApplyTextDepth(text, selected);

            return new TabButtonView(button, image, text, iconRoot);
        }

        public static void SetTabButtonSelected(TabButtonView tab, bool selected)
        {
            if (tab == null)
            {
                return;
            }

            tab.Image.color = GetTabColor(selected);
            tab.Label.color = selected ? Color.white : new Color(0.82f, 0.86f, 0.96f, 1f);
        }

        public static Button CreateBottomActionButton(
            RectTransform parent,
            string name,
            string label,
            Color color,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            bool interactable = true)
        {
            Button button = CreateSimpleButton(parent, name, label, anchor, new Vector2(0.5f, 0f), position, size, color);
            button.interactable = interactable;
            return button;
        }

        public static RewardCardView CreateRewardCard(RectTransform parent, string name, bool wide)
        {
            GameObject cardObject = new GameObject(name, typeof(RectTransform), typeof(Button));
            cardObject.transform.SetParent(parent, false);
            RectTransform root = cardObject.GetComponent<RectTransform>();
            RectTransform backgroundRect = CreateRewardCardBackground(root);
            Image image = backgroundRect.GetComponent<Image>();
            bool hasArtBackground = ApplyRewardCardBackground(image, wide);

            Shadow shadow = backgroundRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.66f);
            shadow.effectDistance = new Vector2(0f, -10f);

            Button button = cardObject.GetComponent<Button>();
            AudioFeedbackManager.RegisterButton(button);
            button.targetGraphic = image;

            if (!hasArtBackground)
            {
                CasualUIStyle.ApplyPanel(image, wide ? new Color(0.24f, 0.06f, 0.38f, 1f) : new Color(0.035f, 0.12f, 0.34f, 1f), wide ? 30 : 26);
                Outline outline = backgroundRect.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.76f, 0.22f, 0.95f);
                outline.effectDistance = wide ? new Vector2(5f, -5f) : new Vector2(4f, -4f);
            }

            RectTransform contentRoot = CreateVisibleContentRoot(
                root,
                "ContentRoot",
                wide ? new Vector2(28f, 24f) : new Vector2(22f, 18f),
                wide ? new Vector2(-28f, -32f) : new Vector2(-22f, -24f));

            RectTransform dayBadge = CreateDecorPanel(
                contentRoot,
                "DayBadge",
                wide ? new Color(0.08f, 0.16f, 0.50f, 1f) : new Color(0.06f, 0.18f, 0.50f, 1f),
                wide ? 28 : 24,
                new Vector2(0.5f, 1f),
                wide ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f),
                wide ? new Vector2(0f, -24f) : new Vector2(0f, -50f),
                wide ? new Vector2(304f, 61f) : new Vector2(216f, 60f));
            Image dayBadgeImage = dayBadge.GetComponent<Image>();
            Outline dayOutline = dayBadge.gameObject.AddComponent<Outline>();
            dayOutline.effectColor = new Color(1f, 0.77f, 0.24f, 0.88f);
            dayOutline.effectDistance = new Vector2(2f, -2f);

            Text dayLabel = CreateText(dayBadge, "DayLabel", string.Empty, wide ? 40 : 33, TextAnchor.MiddleCenter);
            dayLabel.fontStyle = FontStyle.Bold;
            dayLabel.color = new Color(1f, 0.96f, 0.73f, 1f);
            dayLabel.resizeTextForBestFit = true;
            dayLabel.resizeTextMinSize = wide ? 24 : 20;
            dayLabel.resizeTextMaxSize = wide ? 40 : 33;
            dayLabel.rectTransform.offsetMin = new Vector2(24f, 0f);
            dayLabel.rectTransform.offsetMax = new Vector2(-24f, 0f);
            CasualUIStyle.ApplyTextDepth(dayLabel, true);

            StatusBadgeView badge = CreateStatusBadge(
                contentRoot,
                "StatusBadge",
                "LOCKED",
                StatusBadgeStyle.Locked,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                wide ? new Vector2(-42f, -40f) : new Vector2(-34f, -32f),
                wide ? new Vector2(124f, 36f) : new Vector2(96f, 30f));
            Outline badgeOutline = badge.Root.gameObject.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0f, 0f, 0f, 0.34f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);

            Image iconGlowImage = null;
            if (!hasArtBackground)
            {
                RectTransform iconGlow = CreateDecorPanel(
                    contentRoot,
                    "IconGlow",
                    wide ? new Color(0.85f, 0.30f, 1f, 0.22f) : new Color(0.25f, 0.65f, 1f, 0.17f),
                    wide ? 60 : 48,
                    new Vector2(0.5f, 0.53f),
                new Vector2(0.5f, 0.5f),
                wide ? new Vector2(0f, 0f) : new Vector2(0f, 30f),
                wide ? new Vector2(340f, 186f) : new Vector2(248f, 136f));
                iconGlowImage = iconGlow.GetComponent<Image>();
            }

            GameObject iconObject = new GameObject("RewardIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(contentRoot, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = wide ? new Vector2(0f, 0f) : new Vector2(0f, 16f);
            iconRect.sizeDelta = wide ? new Vector2(176f, 176f) : new Vector2(188f, 188f);
            Image icon = iconObject.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            RectTransform rewardBar = CreateDecorPanel(
                contentRoot,
                "RewardTextBar",
                new Color(0.008f, 0.014f, 0.048f, 0.90f),
                wide ? 26 : 20,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                wide ? new Vector2(0f, -98f) : new Vector2(0f, -118f),
                wide ? new Vector2(560f, 58f) : new Vector2(420f, 48f));
            Image rewardBarImage = rewardBar.GetComponent<Image>();
            rewardBarImage.enabled = false;

            Text rewardLabel = CreateText(rewardBar, "RewardLabel", string.Empty, wide ? 42 : 34, TextAnchor.MiddleCenter);
            rewardLabel.fontStyle = FontStyle.Bold;
            rewardLabel.color = new Color(1f, 0.96f, 0.82f, 1f);
            rewardLabel.resizeTextForBestFit = true;
            rewardLabel.resizeTextMinSize = wide ? 26 : 22;
            rewardLabel.resizeTextMaxSize = wide ? 42 : 34;
            rewardLabel.rectTransform.offsetMin = new Vector2(18f, 1f);
            rewardLabel.rectTransform.offsetMax = new Vector2(-18f, -1f);
            CasualUIStyle.ApplyTextDepth(rewardLabel, true);

            return new RewardCardView(root, button, image, dayBadgeImage, dayLabel, badge, iconGlowImage, icon, rewardBarImage, rewardLabel, hasArtBackground);
        }

        public static ShopItemCardView CreateShopItemCard(
            RectTransform parent,
            string name,
            float height,
            bool includeDivider = false,
            bool includeDecorations = false)
        {
            GameObject cardObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            cardObject.transform.SetParent(parent, false);
            RectTransform root = cardObject.GetComponent<RectTransform>();
            cardObject.GetComponent<LayoutElement>().preferredHeight = height;
            Image image = cardObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(0.052f, 0.072f, 0.13f, 0.985f), 22);

            if (includeDecorations)
            {
                AddSimpleShadow(cardObject);
            }

            RectTransform iconArea = CreateEmptyRect(root, "IconArea", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(156f, 156f));
            Text title = CreateText(root, "Title", string.Empty, 32, TextAnchor.UpperLeft);
            SetStretchTop(title.rectTransform, new Vector2(204f, -24f), new Vector2(-218f, -68f));
            title.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(title, true);

            Text description = CreateText(root, "Description", string.Empty, 22, TextAnchor.UpperLeft);
            SetStretchTop(description.rectTransform, new Vector2(204f, -72f), new Vector2(-218f, -126f));
            description.color = new Color(0.82f, 0.86f, 1f, 1f);

            RectTransform priceRow = CreateEmptyRect(root, "PriceRow", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(204f, 24f), new Vector2(180f, 38f));
            Text price = CreateText(priceRow, "Price", string.Empty, 28, TextAnchor.MiddleLeft);
            price.fontStyle = FontStyle.Bold;
            price.color = new Color(1f, 0.84f, 0.25f, 1f);

            Text effect = CreateText(root, "Effect", string.Empty, 24, TextAnchor.LowerLeft);
            effect.rectTransform.anchorMin = new Vector2(0f, 0f);
            effect.rectTransform.anchorMax = new Vector2(1f, 0f);
            effect.rectTransform.pivot = new Vector2(0f, 0f);
            effect.rectTransform.anchoredPosition = new Vector2(328f, 24f);
            effect.rectTransform.sizeDelta = new Vector2(-552f, 38f);
            effect.fontStyle = FontStyle.Bold;
            effect.color = new Color(0.64f, 0.82f, 1f, 1f);

            Button action = CreateBottomActionButton(root, "ActionButton", "BUY", new Color(0.18f, 0.62f, 0.24f, 1f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(176f, 90f));
            RectTransform actionRect = action.GetComponent<RectTransform>();
            actionRect.pivot = new Vector2(1f, 0.5f);

            if (includeDivider)
            {
                CreateDivider(root, new Vector2(1f, 0.18f), new Vector2(1f, 0.82f), new Vector2(-214f, 0f));
            }

            return new ShopItemCardView(root, image, iconArea, title, description, priceRow, price, effect, action);
        }

        public static PremiumShowcaseCardView CreatePremiumShowcaseCard(RectTransform parent, string name, float height)
        {
            GameObject cardObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            cardObject.transform.SetParent(parent, false);
            RectTransform root = cardObject.GetComponent<RectTransform>();
            cardObject.GetComponent<LayoutElement>().preferredHeight = height;
            Image image = cardObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(0.068f, 0.057f, 0.15f, 0.99f), 24);

            Text title = CreateText(root, "SkinName", string.Empty, 38, TextAnchor.UpperCenter);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            title.rectTransform.sizeDelta = new Vector2(-250f, 58f);
            CasualUIStyle.ApplyTextDepth(title, true);

            StatusBadgeView status = CreateStatusBadge(root, "StatusBadge", "LOCKED", StatusBadgeStyle.Locked, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(180f, 40f));

            Text index = CreateText(root, "IndexText", string.Empty, 20, TextAnchor.MiddleCenter);
            index.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            index.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            index.rectTransform.pivot = new Vector2(0.5f, 1f);
            index.rectTransform.anchoredPosition = new Vector2(0f, -130f);
            index.rectTransform.sizeDelta = new Vector2(220f, 34f);
            index.color = new Color(0.86f, 0.88f, 1f, 1f);

            RectTransform previewArea = CreateEmptyRect(root, "PreviewArea", new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            GameObject imageObject = new GameObject("PreviewImage", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(previewArea, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(520f, 520f);
            RawImage previewImage = imageObject.GetComponent<RawImage>();
            previewImage.raycastTarget = false;

            Button previous = CreateBottomActionButton(previewArea, "PreviousButton", "<", new Color(0.38f, 0.22f, 0.72f, 1f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(88f, 96f));
            previous.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            Button next = CreateBottomActionButton(previewArea, "NextButton", ">", new Color(0.38f, 0.22f, 0.72f, 1f), new Vector2(1f, 0.5f), new Vector2(-44f, 0f), new Vector2(88f, 96f));
            next.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

            Button action = CreateBottomActionButton(root, "ActionButton", "Equip", new Color(0.38f, 0.22f, 0.72f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(372f, 82f));
            return new PremiumShowcaseCardView(root, image, title, status, index, previewArea, previewImage, previous, next, action);
        }

        private static RectTransform CreateRewardCardBackground(RectTransform parent)
        {
            GameObject backgroundObject = new GameObject("BackgroundImage", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);
            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateVisibleContentRoot(
            RectTransform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject rootObject = new GameObject(name, typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            RectTransform rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static RectTransform CreateDecorPanel(
            RectTransform parent,
            string name,
            Color color,
            int radius,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = panelObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, radius);
            image.raycastTarget = false;
            return rect;
        }

        private static bool ApplyRewardCardBackground(Image image, bool wide)
        {
            Sprite sprite = GetRewardCardBackgroundSprite(wide);
            if (sprite == null)
            {
                return false;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = true;
            return true;
        }

        private static Sprite GetRewardCardBackgroundSprite(bool wide)
        {
            if (wide)
            {
                return rewardCardPurpleWideSprite ?? (rewardCardPurpleWideSprite = CreateRewardCardSprite(
                    "UI/Rewards/reward_card_purple_wide",
                    new Vector4(96f, 96f, 96f, 96f)));
            }

            return rewardCardBlueSprite ?? (rewardCardBlueSprite = CreateRewardCardSprite(
                "UI/Rewards/reward_card_blue",
                new Vector4(86f, 86f, 86f, 86f)));
        }

        private static Sprite CreateRewardCardSprite(string resourcePath, Vector4 border)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = texture.name;
            return sprite;
        }

        private static void AddRewardBarHighlight(RectTransform rewardBar, bool wide)
        {
            RectTransform highlight = CreateDecorPanel(
                rewardBar,
                "BarTopHighlight",
                new Color(1f, 1f, 1f, wide ? 0.075f : 0.06f),
                wide ? 18 : 14,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -4f),
                wide ? new Vector2(520f, 18f) : new Vector2(296f, 14f));
            highlight.SetAsFirstSibling();
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static Button CreateSimpleButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, 20);
            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, color, 0.55f);
            colors.pressedColor = Color.Lerp(Color.black, color, 0.72f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.44f, 0.50f, 0.58f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            AudioFeedbackManager.RegisterButton(button);

            if (!string.IsNullOrEmpty(label))
            {
                Text text = CreateText(rect, "Label", label, 26, TextAnchor.MiddleCenter);
                text.fontStyle = FontStyle.Bold;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 14;
                text.resizeTextMaxSize = 26;
                text.rectTransform.offsetMin = new Vector2(10f, 0f);
                text.rectTransform.offsetMax = new Vector2(-10f, 0f);
                CasualUIStyle.ApplyTextDepth(text, true);
            }

            return button;
        }

        private static RectTransform CreateEmptyRect(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void SetStretchTop(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void CreateDivider(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
        {
            GameObject dividerObject = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerObject.transform.SetParent(parent, false);
            RectTransform divider = dividerObject.GetComponent<RectTransform>();
            divider.anchorMin = anchorMin;
            divider.anchorMax = anchorMax;
            divider.pivot = new Vector2(1f, 0.5f);
            divider.anchoredPosition = position;
            divider.sizeDelta = new Vector2(2f, 0f);
            Image image = dividerObject.GetComponent<Image>();
            image.color = new Color(0.34f, 0.75f, 1f, 0.32f);
            image.raycastTarget = false;
        }

        private static void AddSimpleShadow(GameObject gameObject)
        {
            Shadow shadow = gameObject.GetComponent<Shadow>() ?? gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -5f);
        }

        private static Color GetStatusBadgeColor(StatusBadgeStyle style)
        {
            switch (style)
            {
                case StatusBadgeStyle.Today:
                    return new Color(0.96f, 0.67f, 0.14f, 1f);
                case StatusBadgeStyle.Claimed:
                    return new Color(0.22f, 0.72f, 0.26f, 1f);
                case StatusBadgeStyle.Equipped:
                    return new Color(0.12f, 0.54f, 0.28f, 1f);
                case StatusBadgeStyle.Premium:
                    return new Color(0.56f, 0.28f, 0.84f, 1f);
                case StatusBadgeStyle.Locked:
                    return new Color(0.34f, 0.38f, 0.54f, 0.95f);
                default:
                    return new Color(0.26f, 0.30f, 0.42f, 0.95f);
            }
        }

        private static Color GetTabColor(bool selected)
        {
            return selected
                ? new Color(0.42f, 0.27f, 0.82f, 1f)
                : new Color(0.12f, 0.17f, 0.30f, 1f);
        }
    }
}
