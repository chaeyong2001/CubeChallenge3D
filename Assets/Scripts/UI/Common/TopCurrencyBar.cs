using CubeChallenge3D.Economy;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Common
{
    public sealed class TopCurrencyBar : MonoBehaviour
    {
        public const float Height = 178f;

        private WalletStore walletStore;
        private Text heartValue;
        private Text heartStatus;
        private Text heartDetail;
        private Text coinValue;
        private Text gemValue;
        private Button shopButton;
        private Button heartPlusButton;
        private Button coinPlusButton;
        private Button gemPlusButton;
        private UnityAction pendingShopAction;
        private UnityAction pendingHeartPlusAction;
        private UnityAction pendingCoinPlusAction;
        private UnityAction pendingGemPlusAction;
        private bool mainMenuStyle;
        private float nextRefresh;
        private static UnityAction defaultShopAction;
        private static UnityAction defaultHeartPlusAction;
        private static UnityAction defaultCoinPlusAction;
        private static UnityAction defaultGemPlusAction;

        public static void SetDefaultActions(
            UnityAction shopAction,
            UnityAction heartPlusAction,
            UnityAction coinPlusAction,
            UnityAction gemPlusAction)
        {
            defaultShopAction = shopAction;
            defaultHeartPlusAction = heartPlusAction;
            defaultCoinPlusAction = coinPlusAction;
            defaultGemPlusAction = gemPlusAction;

            TopCurrencyBar[] bars = UnityEngine.Object.FindObjectsByType<TopCurrencyBar>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (TopCurrencyBar bar in bars)
            {
                bar.SetActions(
                    bar.pendingShopAction,
                    bar.pendingHeartPlusAction,
                    bar.pendingCoinPlusAction,
                    bar.pendingGemPlusAction);
            }
        }

        public static TopCurrencyBar Attach(
            Canvas canvas,
            UnityAction shopAction = null,
            bool useMainMenuStyle = false,
            UnityAction heartPlusAction = null,
            UnityAction coinPlusAction = null,
            UnityAction gemPlusAction = null,
            bool adjustCanvasScaler = true,
            Transform parentOverride = null,
            bool useSafeArea = true)
        {
            if (canvas == null)
            {
                return null;
            }
            useMainMenuStyle = true;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (adjustCanvasScaler && scaler != null)
            {
                scaler.matchWidthOrHeight = 0f;
            }

            TopCurrencyBar existing = canvas.GetComponentInChildren<TopCurrencyBar>(true);
            if (existing != null)
            {
                existing.mainMenuStyle |= useMainMenuStyle;
                existing.SetActions(shopAction, heartPlusAction, coinPlusAction, gemPlusAction);
                return existing;
            }

            Transform barParent = parentOverride != null ? parentOverride : canvas.transform;
            if (useSafeArea)
            {
                GameObject safeObject = new GameObject("TopCurrencySafeArea", typeof(RectTransform), typeof(MobileSafeArea));
                safeObject.transform.SetParent(barParent, false);
                barParent = safeObject.transform;
            }

            GameObject barObject = new GameObject(
                "TopCurrencyBar",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(Shadow));
            barObject.SetActive(false);
            barObject.transform.SetParent(barParent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, useMainMenuStyle ? -14f : -12f);
            rect.sizeDelta = new Vector2(useMainMenuStyle ? -24f : -28f, useMainMenuStyle ? 190f : Height);

            CasualUIStyle.ApplyPanel(
                barObject.GetComponent<Image>(),
                useMainMenuStyle
                    ? new Color(1f, 0.89f, 0.68f, 1f)
                    : new Color(1f, 0.9f, 0.72f, 0.99f),
                useMainMenuStyle ? 31 : 30);
            Outline outline = barObject.GetComponent<Outline>();
            outline.effectColor = useMainMenuStyle
                ? new Color(0.44f, 0.28f, 0.12f, 0.58f)
                : new Color(0.34f, 0.24f, 0.2f, 0.5f);
            outline.effectDistance = useMainMenuStyle ? new Vector2(3f, -3f) : new Vector2(3f, -3f);
            Shadow shadow = barObject.GetComponent<Shadow>();
            shadow.effectColor = useMainMenuStyle
                ? new Color(0.16f, 0.08f, 0.02f, 0.24f)
                : new Color(0.02f, 0.01f, 0.08f, 0.26f);
            shadow.effectDistance = useMainMenuStyle ? new Vector2(0f, -7f) : new Vector2(0f, -6f);

            if (useMainMenuStyle)
            {
                CreateHudFrame(barObject.transform);
            }

            GameObject depth = new GameObject("BottomDepth", typeof(RectTransform), typeof(Image));
            depth.transform.SetParent(barObject.transform, false);
            RectTransform depthRect = depth.GetComponent<RectTransform>();
            depthRect.anchorMin = new Vector2(0.025f, 0.02f);
            depthRect.anchorMax = new Vector2(0.975f, 0.22f);
            depthRect.offsetMin = Vector2.zero;
            depthRect.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(
                depth.GetComponent<Image>(),
                useMainMenuStyle
                    ? new Color(0.38f, 0.16f, 0.09f, 0.34f)
                    : new Color(0.45f, 0.22f, 0.12f, 0.24f),
                24);
            depth.GetComponent<Image>().raycastTarget = false;

            GameObject highlight = new GameObject("TopHighlight", typeof(RectTransform), typeof(Image));
            highlight.transform.SetParent(barObject.transform, false);
            RectTransform highlightRect = highlight.GetComponent<RectTransform>();
            highlightRect.anchorMin = new Vector2(0.03f, 0.68f);
            highlightRect.anchorMax = new Vector2(0.97f, 0.95f);
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(highlight.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.22f), 22);
            highlight.GetComponent<Image>().raycastTarget = false;

            TopCurrencyBar bar = barObject.AddComponent<TopCurrencyBar>();
            bar.mainMenuStyle = useMainMenuStyle;
            bar.SetActions(shopAction, heartPlusAction, coinPlusAction, gemPlusAction);
            barObject.SetActive(true);
            return bar;
        }

        private void Awake()
        {
            walletStore = new WalletStore();
            RectTransform root = (RectTransform)transform;
            bool useMainMenuKitIcons = mainMenuStyle;

            CreateCurrencyCell(
                root, "HeartCell", new Vector2(0.012f, 0.09f), new Vector2(0.275f, 0.91f),
                "heart", new Color(0.95f, 0.08f, 0.2f), useMainMenuKitIcons,
                mainMenuStyle, out heartValue, out heartStatus, out heartDetail, out heartPlusButton);
            CreateCurrencyCell(
                root, "CoinCell", new Vector2(0.28f, 0.09f), new Vector2(0.5425f, 0.91f),
                "coin", new Color(1f, 0.58f, 0.02f), useMainMenuKitIcons,
                mainMenuStyle, out coinValue, out _, out _, out coinPlusButton);
            CreateCurrencyCell(
                root, "GemCell", new Vector2(0.5475f, 0.09f), new Vector2(0.81f, 0.91f),
                "gem", new Color(0.75f, 0.06f, 0.95f), useMainMenuKitIcons,
                mainMenuStyle, out gemValue, out _, out _, out gemPlusButton);

            CreateSeparator(root, 0.2775f, mainMenuStyle);
            CreateSeparator(root, 0.545f, mainMenuStyle);
            CreateSeparator(root, 0.8125f, mainMenuStyle);
            shopButton = CreateShopButton(root, useMainMenuKitIcons, mainMenuStyle);
            SetActions(pendingShopAction, pendingHeartPlusAction, pendingCoinPlusAction, pendingGemPlusAction);
            WalletStore.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            WalletStore.Changed -= Refresh;
        }

        private void LateUpdate()
        {
            if (transform.parent != null)
            {
                if (transform.parent.GetComponent<MobileSafeArea>() != null)
                {
                    transform.parent.SetAsLastSibling();
                }
                else
                {
                    transform.SetAsLastSibling();
                }
            }
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 1f;
                Refresh();
            }
        }

        public void SetActions(
            UnityAction shopAction,
            UnityAction heartPlusAction,
            UnityAction coinPlusAction,
            UnityAction gemPlusAction)
        {
            pendingShopAction = shopAction;
            pendingHeartPlusAction = heartPlusAction;
            pendingCoinPlusAction = coinPlusAction;
            pendingGemPlusAction = gemPlusAction;

            SetButtonAction(shopButton, shopAction ?? defaultShopAction);
            SetButtonAction(heartPlusButton, heartPlusAction ?? defaultHeartPlusAction);
            SetButtonAction(coinPlusButton, coinPlusAction ?? defaultCoinPlusAction);
            SetButtonAction(gemPlusButton, gemPlusAction ?? defaultGemPlusAction);
        }

        public void SetShopAction(UnityAction action)
        {
            SetActions(action, pendingHeartPlusAction, pendingCoinPlusAction, pendingGemPlusAction);
        }

        private static void SetButtonAction(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            button.interactable = action != null;
        }

        private void Refresh()
        {
            if (heartValue == null || walletStore == null)
            {
                return;
            }

            int hearts = walletStore.Hearts;
            heartValue.text = hearts.ToString();
            bool isFull = hearts >= WalletStore.MaxNaturalHearts;
            heartStatus.text = isFull ? "Full" : "Recovering";
            if (heartDetail != null)
            {
                heartDetail.text = isFull
                    ? "2 min / +1, max 5"
                    : $"Next {FormatTime(walletStore.SecondsUntilNextHeart)} / +1";
            }
            coinValue.text = FormatCompactCount(walletStore.Coins);
            gemValue.text = FormatCompactCount(walletStore.Gems);
        }

        private static void CreateCurrencyCell(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string iconKey,
            Color iconColor,
            bool useMainMenuKitIcon,
            bool polished,
            out Text value,
            out Text status,
            out Text detail,
            out Button plusButton)
        {
            plusButton = null;
            GameObject cell = new GameObject(name, typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(parent, false);
            RectTransform rect = cell.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(5f, 4f);
            rect.offsetMax = new Vector2(-5f, -4f);
            CasualUIStyle.ApplyPanel(
                cell.GetComponent<Image>(),
                polished
                    ? new Color(1f, 0.97f, 0.84f, 0.38f)
                    : new Color(1f, 0.98f, 0.9f, 0.14f),
                20);
            cell.GetComponent<Image>().raycastTarget = false;
            if (polished)
            {
                CreateCellDepth(rect);
            }

            GameObject iconObject = new GameObject("IconPanel", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(rect, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.012f, 0.06f);
            iconRect.anchorMax = iconKey == "heart"
                ? new Vector2(0.39f, 0.94f)
                : new Vector2(0.38f, 0.94f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(
                iconObject.GetComponent<Image>(),
                polished ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.16f),
                22);
            iconObject.GetComponent<Image>().raycastTarget = false;
            if (!useMainMenuKitIcon
                || !CasualIconFactory.TryCreateMainMenuKitIcon(iconRect, iconKey, out _))
            {
                CasualIconFactory.Create(iconRect, iconKey, iconColor);
            }

            value = RuntimeUiFactory.CreateText(rect, "Value", "0", polished ? 40 : 38, TextAnchor.MiddleLeft);
            value.fontStyle = FontStyle.Bold;
            value.color = new Color(0.22f, 0.12f, 0.08f);
            value.rectTransform.anchorMin = new Vector2(0.4f, polished ? 0.2f : 0.28f);
            value.rectTransform.anchorMax = new Vector2(0.98f, polished ? 0.86f : 0.94f);
            value.rectTransform.offsetMin = Vector2.zero;
            value.rectTransform.offsetMax = Vector2.zero;
            AddTextShadow(value);

            status = RuntimeUiFactory.CreateText(
                rect,
                "Status",
                string.Empty,
                polished && iconKey == "heart" ? 26 : 17,
                TextAnchor.MiddleLeft);
            status.fontStyle = FontStyle.Bold;
            status.color = new Color(0.33f, 0.19f, 0.1f);
            status.rectTransform.anchorMin = new Vector2(0.4f, polished ? 0.42f : 0.02f);
            status.rectTransform.anchorMax = new Vector2(0.98f, polished ? 0.91f : 0.4f);
            status.rectTransform.offsetMin = Vector2.zero;
            status.rectTransform.offsetMax = Vector2.zero;
            detail = null;
            if (polished && iconKey == "heart")
            {
                detail = RuntimeUiFactory.CreateText(
                    rect,
                    "Detail",
                    string.Empty,
                    15,
                    TextAnchor.MiddleLeft);
                detail.fontStyle = FontStyle.Bold;
                detail.color = new Color(0.38f, 0.22f, 0.12f);
                detail.resizeTextForBestFit = true;
                detail.resizeTextMinSize = 11;
                detail.resizeTextMaxSize = 15;
                detail.rectTransform.anchorMin = new Vector2(0.4f, 0.06f);
                detail.rectTransform.anchorMax = new Vector2(0.99f, 0.48f);
                detail.rectTransform.offsetMin = Vector2.zero;
                detail.rectTransform.offsetMax = Vector2.zero;
            }
            plusButton = CreatePlusBadge(rect, polished);
            if (iconKey != "heart")
            {
                value.alignment = TextAnchor.MiddleCenter;
                value.rectTransform.anchorMin = new Vector2(0.38f, 0.12f);
                value.rectTransform.anchorMax = new Vector2(0.8f, 0.9f);
            }
            if (iconKey == "heart")
            {
                Text badgeValue = RuntimeUiFactory.CreateText(
                    iconRect,
                    "HeartValue",
                    "0",
                    polished ? 39 : 34,
                    TextAnchor.MiddleCenter);
                badgeValue.fontStyle = FontStyle.Bold;
                badgeValue.color = Color.white;
                AddTextShadow(badgeValue);
                value.gameObject.SetActive(false);
                value = badgeValue;
            }
        }

        private static void CreateSeparator(RectTransform parent, float anchorX, bool polished)
        {
            GameObject separatorObject = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            separatorObject.transform.SetParent(parent, false);
            RectTransform separator = separatorObject.GetComponent<RectTransform>();
            separator.anchorMin = new Vector2(anchorX, polished ? 0.19f : 0.17f);
            separator.anchorMax = new Vector2(anchorX, polished ? 0.81f : 0.83f);
            separator.sizeDelta = new Vector2(polished ? 3f : 2f, 0f);
            separator.anchoredPosition = Vector2.zero;
            Image image = separatorObject.GetComponent<Image>();
            image.color = polished
                ? new Color(0.38f, 0.21f, 0.13f, 0.34f)
                : new Color(0.45f, 0.27f, 0.14f, 0.28f);
            image.raycastTarget = false;
        }

        private static Button CreatePlusBadge(RectTransform parent, bool polished)
        {
            GameObject badgeObject = new GameObject("PlusBadge", typeof(RectTransform), typeof(Image), typeof(Button));
            badgeObject.transform.SetParent(parent, false);
            RectTransform badge = badgeObject.GetComponent<RectTransform>();
            badge.anchorMin = new Vector2(1f, 0.5f);
            badge.anchorMax = new Vector2(1f, 0.5f);
            badge.pivot = new Vector2(1f, 0.5f);
            float badgeSize = polished ? 38f : 34f;
            badge.sizeDelta = new Vector2(badgeSize, badgeSize);
            badge.anchoredPosition = polished ? new Vector2(-8f, 0f) : new Vector2(-6f, 0f);
            Button button = badgeObject.GetComponent<Button>();
            Sprite plusSprite = polished
                ? CasualIconFactory.LoadMainMenuKitSprite("Icons/plus")
                : null;
            Image badgeImage = badgeObject.GetComponent<Image>();
            if (plusSprite != null)
            {
                badgeImage.sprite = plusSprite;
                badgeImage.type = Image.Type.Simple;
                badgeImage.preserveAspect = true;
                badgeImage.color = Color.white;
            }
            else
            {
                CasualUIStyle.ApplyPanel(badgeImage, new Color(0.28f, 0.8f, 0.08f), 24);
            }
            badgeObject.GetComponent<Image>().raycastTarget = true;

            if (plusSprite == null)
            {
                CreatePlusBar(badge, "Vertical", new Vector2(0.44f, 0.24f), new Vector2(0.56f, 0.76f));
                CreatePlusBar(badge, "Horizontal", new Vector2(0.24f, 0.44f), new Vector2(0.76f, 0.56f));
            }

            return button;
        }

        private static void CreatePlusBar(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            GameObject barObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            RectTransform bar = barObject.GetComponent<RectTransform>();
            bar.anchorMin = min;
            bar.anchorMax = max;
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(barObject.GetComponent<Image>(), Color.white, 4);
            barObject.GetComponent<Image>().raycastTarget = false;
        }

        private static Button CreateShopButton(
            RectTransform parent,
            bool useMainMenuKitIcon,
            bool polished)
        {
            GameObject buttonObject = new GameObject(
                "ShopButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.805f, polished ? 0.08f : 0.1f);
            rect.anchorMax = new Vector2(0.99f, polished ? 0.92f : 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Button button = buttonObject.GetComponent<Button>();
            CasualUIStyle.ApplyButton(button, CasualUIColor.Orange);
            if (polished)
            {
                CreateShopPolish(rect);
            }

            GameObject iconHolder = new GameObject("IconHolder", typeof(RectTransform));
            iconHolder.transform.SetParent(rect, false);
            RectTransform iconRect = iconHolder.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.035f, 0.17f);
            iconRect.anchorMax = new Vector2(0.4f, 0.83f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            if (!useMainMenuKitIcon
                || !CasualIconFactory.TryCreateMainMenuKitIcon(iconRect, "shop", out _))
            {
                CasualIconFactory.Create(iconRect, "shop", Color.white);
            }

            Text label = RuntimeUiFactory.CreateText(rect, "Label", "Shop", 28, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = new Vector2(0.34f, 0f);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 28;
            CasualUIStyle.ApplyTextDepth(label, true);
            return button;
        }

        private static void CreateHudFrame(Transform parent)
        {
            GameObject rimObject = new GameObject("InnerRim", typeof(RectTransform), typeof(Image), typeof(Outline));
            rimObject.transform.SetParent(parent, false);
            RectTransform rim = rimObject.GetComponent<RectTransform>();
            rim.anchorMin = new Vector2(0.014f, 0.055f);
            rim.anchorMax = new Vector2(0.986f, 0.945f);
            rim.offsetMin = Vector2.zero;
            rim.offsetMax = Vector2.zero;
            Image image = rimObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(1f, 1f, 1f, 0.035f), 28);
            image.raycastTarget = false;
            Outline outline = rimObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.98f, 0.84f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void CreateCellDepth(RectTransform parent)
        {
            GameObject insetObject = new GameObject("InsetShadow", typeof(RectTransform), typeof(Image));
            insetObject.transform.SetParent(parent, false);
            RectTransform inset = insetObject.GetComponent<RectTransform>();
            inset.anchorMin = new Vector2(0.025f, 0.06f);
            inset.anchorMax = new Vector2(0.975f, 0.28f);
            inset.offsetMin = Vector2.zero;
            inset.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(
                insetObject.GetComponent<Image>(),
                new Color(0.35f, 0.18f, 0.08f, 0.13f),
                17);
            insetObject.GetComponent<Image>().raycastTarget = false;

            GameObject shineObject = new GameObject("CellHighlight", typeof(RectTransform), typeof(Image));
            shineObject.transform.SetParent(parent, false);
            RectTransform shine = shineObject.GetComponent<RectTransform>();
            shine.anchorMin = new Vector2(0.035f, 0.68f);
            shine.anchorMax = new Vector2(0.965f, 0.94f);
            shine.offsetMin = Vector2.zero;
            shine.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(
                shineObject.GetComponent<Image>(),
                new Color(1f, 1f, 1f, 0.2f),
                16);
            shineObject.GetComponent<Image>().raycastTarget = false;
        }

        private static void CreateShopPolish(RectTransform parent)
        {
            GameObject innerObject = new GameObject("ShopInnerRim", typeof(RectTransform), typeof(Image), typeof(Outline));
            innerObject.transform.SetParent(parent, false);
            RectTransform inner = innerObject.GetComponent<RectTransform>();
            inner.anchorMin = new Vector2(0.04f, 0.07f);
            inner.anchorMax = new Vector2(0.96f, 0.93f);
            inner.offsetMin = Vector2.zero;
            inner.offsetMax = Vector2.zero;
            Image image = innerObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(1f, 0.74f, 0.05f, 0.06f), 22);
            image.raycastTarget = false;
            Outline outline = innerObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.93f, 0.48f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void AddTextShadow(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = Mathf.Max(0, totalSeconds) / 60;
            int seconds = Mathf.Max(0, totalSeconds) % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static string FormatCompactCount(int value)
        {
            if (value < 10000)
            {
                return value.ToString("N0");
            }

            if (value < 100000)
            {
                return $"{value / 1000f:0.0}K";
            }

            if (value < 1000000)
            {
                return $"{value / 1000f:0.#}K";
            }

            if (value < 10000000)
            {
                return $"{value / 1000000f:0.0}M";
            }

            return $"{value / 1000000f:0.#}M";
        }
    }
}
