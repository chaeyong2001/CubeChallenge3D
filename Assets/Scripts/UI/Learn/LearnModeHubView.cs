using System;
using System.Collections.Generic;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Core;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Learn
{
    public sealed class LearnModeHubView
    {
        private const float ReferencePanelHeight = 1920f;
        private const float FirstCardTopY = -410f;
        private const float BaseCardHeight = 120f;
        private const float BaseCardGap = 20f;
        private const float CardScale = 1.12f;
        private const float BackButtonBottomY = 70f;
        private const float BackButtonHeight = 70f;
        private const float ContentTopInset = 388f;
        private const float ContentBottomInset = 168f;
        private const float ContentTopPadding = 18f;
        private const float ContentBottomPadding = 26f;
        private const int CardCount = 6;

        private readonly GameObject root;
        private readonly TopCurrencyBar topCurrencyBar;
        private readonly Color cameraFallbackColor = new Color(0.018f, 0.045f, 0.085f, 1f);
        private Color previousCameraColor;
        private CameraClearFlags previousCameraClearFlags;
        private Camera fallbackCamera;
        private bool cameraFallbackApplied;
        private Action manualSolverAction;
        private Action<string> categoryAction;
        private Action practiceAction;
        private Text headerTitle;
        private Text headerSubtitle;
        private Text backButtonText;
        private readonly List<CardBinding> cardBindings = new List<CardBinding>();
        private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();

        public LearnModeHubView(Transform parent)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "SolverLearnHubCanvas", 1505, 0f);
            root = canvas.gameObject;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            CasualUIFactory.CreateBackdrop(root.transform, "SolverLearnBackdrop", true, false);
            topCurrencyBar = TopCurrencyBar.Attach(canvas, null, true);

            RectTransform safeArea = CreateSafeArea(root.transform);
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                safeArea,
                "SolverLearnHub",
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
                panelImage.color = new Color(0f, 0f, 0f, 0f);
                panelImage.raycastTarget = false;
            }

            CreateHeader(panel);
            RectTransform cardContent = CreateScrollableCardContent(panel);
            float cardHeight = CalculateCardHeight();
            float cardGap = CalculateCardGap();
            CreateCard(cardContent, "manual_solver", "learn_hub_manual_desc", "manual_solver", GetScrollableCardTopY(0, cardHeight, cardGap), cardHeight, () =>
            {
                Hide();
                manualSolverAction?.Invoke();
            });
            CreateCard(cardContent, "learn_basics", "learn_hub_basics_desc", "learn_basics", GetScrollableCardTopY(1, cardHeight, cardGap), cardHeight, () => OpenCategory("basics"));
            CreateCard(cardContent, "beginner_method", "learn_hub_beginner_desc", "beginner_method", GetScrollableCardTopY(2, cardHeight, cardGap), cardHeight, () => OpenCategory("beginner"));
            CreateCard(cardContent, "formula_practice", "learn_hub_formulas_desc", "formula_practice", GetScrollableCardTopY(3, cardHeight, cardGap), cardHeight, () => OpenCategory("formulas"));
            CreateCard(cardContent, "tutorial_playback", "learn_hub_playback_desc", "tutorial_playback", GetScrollableCardTopY(4, cardHeight, cardGap), cardHeight, () => OpenCategory("notation"));
            CreateCard(cardContent, "practice", "learn_hub_practice_desc", "practice", GetScrollableCardTopY(5, cardHeight, cardGap), cardHeight, () =>
            {
                practiceAction?.Invoke();
            });
            SetScrollableContentHeight(cardContent, cardHeight, cardGap);

            Button back = RuntimeUiFactory.CreateButton(panel, "BackButton", T("back"), new Vector2(0f, 70f), new Vector2(360f, 70f));
            backButtonText = back.GetComponentInChildren<Text>();
            CasualUIStyle.ApplyButton(back, CasualUIColor.Blue);
            StyleButtonText(back, 34);
            back.onClick.AddListener(Hide);
            Hide();
        }

        public void SetManualSolverAction(Action action)
        {
            manualSolverAction = action;
        }

        public void SetCategoryAction(Action<string> action)
        {
            categoryAction = action;
        }

        public void SetPracticeAction(Action action)
        {
            practiceAction = action;
        }

        public void SetTopHudActions(
            UnityAction shopAction,
            UnityAction heartPlusAction,
            UnityAction coinPlusAction,
            UnityAction gemPlusAction)
        {
            topCurrencyBar?.SetActions(shopAction, heartPlusAction, coinPlusAction, gemPlusAction);
        }

        public void Show()
        {
            AudioFeedbackManager.ClearMenuBgmSuppressions();
            ApplyCameraFallback();
            RefreshLocalizedText();
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
            RestoreCameraFallback();
        }

        private void CreateCard(
            RectTransform parent,
            string titleKey,
            string descriptionKey,
            string iconName,
            float topY,
            float cardHeight,
            Action action)
        {
            GameObject cardObject = new GameObject(iconName + "Card", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow));
            cardObject.transform.SetParent(parent, false);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            card.anchorMin = new Vector2(0.065f, 1f);
            card.anchorMax = new Vector2(0.935f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0f, topY);
            card.sizeDelta = new Vector2(0f, cardHeight);

            Image cardImage = cardObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(cardImage, new Color(1f, 0.62f, 0.16f, 0.52f), 28);
            Outline outline = cardObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.62f, 0.16f, 0.52f);
            outline.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = cardObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(0f, -7f);
            AddCardFill(card);
            AddCardGlow(card);

            CreateIcon(card, iconName);

            Text titleText = RuntimeUiFactory.CreateText(card, "Title", T(titleKey), ScaledFont(32), TextAnchor.UpperLeft);
            titleText.fontStyle = FontStyle.Bold;
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.rectTransform.anchoredPosition = ScaleVector(new Vector2(198f, -26f));
            titleText.rectTransform.sizeDelta = ScaleVector(new Vector2(-410f, 44f));
            CasualUIStyle.ApplyTextDepth(titleText, true);

            Text body = RuntimeUiFactory.CreateText(card, "Description", T(descriptionKey), ScaledFont(22), TextAnchor.LowerLeft);
            body.color = new Color(0.93f, 0.93f, 0.98f, 1f);
            body.rectTransform.anchorMin = new Vector2(0f, 1f);
            body.rectTransform.anchorMax = new Vector2(1f, 1f);
            body.rectTransform.pivot = new Vector2(0f, 1f);
            body.rectTransform.anchoredPosition = ScaleVector(new Vector2(198f, -72f));
            body.rectTransform.sizeDelta = ScaleVector(new Vector2(-410f, 72f));
            CasualUIStyle.ApplyTextDepth(body, false);

            Button open = RuntimeUiFactory.CreateButton(card, "OpenButton", T("open"), Vector2.zero, ScaleVector(new Vector2(166f, 62f)));
            Text openText = open.GetComponentInChildren<Text>();
            CasualUIStyle.ApplyButton(open, CasualUIColor.Blue);
            RectTransform openRect = open.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(1f, 0.5f);
            openRect.anchorMax = new Vector2(1f, 0.5f);
            openRect.pivot = new Vector2(1f, 0.5f);
            openRect.anchoredPosition = ScaleVector(new Vector2(-30f, 0f));
            StyleButtonText(open, ScaledFont(30));
            if (action != null)
            {
                open.onClick.AddListener(() => action());
            }

            cardBindings.Add(new CardBinding(titleKey, descriptionKey, titleText, body, openText));
        }

        private void OpenCategory(string categoryId)
        {
            Hide();
            categoryAction?.Invoke(categoryId);
        }

        private static float CalculateCardHeight()
        {
            return (BaseCardHeight + CalculateDistributedCardSpace()) * CardScale;
        }

        private static float CalculateCardGap()
        {
            return ((BaseCardGap + CalculateDistributedCardSpace()) * 0.5f) * CardScale;
        }

        private static float CalculateDistributedCardSpace()
        {
            float tutorialTopY = GetCardTopY(4, BaseCardHeight, BaseCardGap);
            float practiceTopY = GetCardTopY(5, BaseCardHeight, BaseCardGap);
            float currentGap = tutorialTopY - BaseCardHeight - practiceTopY;
            float practiceBottomFromTop = practiceTopY - BaseCardHeight;
            float backTopFromBottom = BackButtonBottomY + BackButtonHeight;
            float targetPracticeBottomFromTop = -(ReferencePanelHeight - (backTopFromBottom + currentGap));
            float expandableDistance = practiceBottomFromTop - targetPracticeBottomFromTop;
            int distributedSlots = CardCount + (CardCount - 1);
            return Mathf.Max(0f, expandableDistance / distributedSlots);
        }

        private static float GetCardTopY(int index, float cardHeight, float cardGap)
        {
            return FirstCardTopY - (index * (cardHeight + cardGap));
        }

        private static float GetScrollableCardTopY(int index, float cardHeight, float cardGap)
        {
            return -ContentTopPadding - (index * (cardHeight + cardGap));
        }

        private static void SetScrollableContentHeight(RectTransform content, float cardHeight, float cardGap)
        {
            float height = ContentTopPadding + (CardCount * cardHeight) + ((CardCount - 1) * cardGap) + ContentBottomPadding;
            content.sizeDelta = new Vector2(0f, height);
        }

        private void ApplyCameraFallback()
        {
            if (cameraFallbackApplied)
            {
                return;
            }

            fallbackCamera = Camera.main;
            if (fallbackCamera == null)
            {
                return;
            }

            previousCameraColor = fallbackCamera.backgroundColor;
            previousCameraClearFlags = fallbackCamera.clearFlags;
            fallbackCamera.clearFlags = CameraClearFlags.SolidColor;
            fallbackCamera.backgroundColor = cameraFallbackColor;
            cameraFallbackApplied = true;
        }

        private void RestoreCameraFallback()
        {
            if (!cameraFallbackApplied || fallbackCamera == null)
            {
                cameraFallbackApplied = false;
                fallbackCamera = null;
                return;
            }

            fallbackCamera.clearFlags = previousCameraClearFlags;
            fallbackCamera.backgroundColor = previousCameraColor;
            cameraFallbackApplied = false;
            fallbackCamera = null;
        }

        private void CreateHeader(RectTransform parent)
        {
            Text title = RuntimeUiFactory.CreateText(parent, "Title", T("solver_learn_title"), 62, TextAnchor.MiddleCenter);
            headerTitle = title;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.82f, 0.38f, 1f);
            SetTopRect(title.rectTransform, -278f, 80f, 84f);
            CasualUIStyle.ApplyTextDepth(title, true);

            Text subtitle = RuntimeUiFactory.CreateText(parent, "Subtitle", T("solver_learn_subtitle"), 28, TextAnchor.MiddleCenter);
            headerSubtitle = subtitle;
            subtitle.color = new Color(0.91f, 0.91f, 0.96f, 1f);
            SetTopRect(subtitle.rectTransform, -350f, 80f, 44f);
            CasualUIStyle.ApplyTextDepth(subtitle, false);
        }

        private static RectTransform CreateScrollableCardContent(RectTransform parent)
        {
            GameObject viewportObject = new GameObject("SolverLearnCardViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(0f, ContentBottomInset);
            viewport.offsetMax = new Vector2(0f, -ContentTopInset);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = new GameObject("SolverLearnCardContent", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            return content;
        }

        private static void AddCardGlow(RectTransform card)
        {
            GameObject glowObject = new GameObject("CardInnerGlow", typeof(RectTransform), typeof(Image));
            glowObject.transform.SetParent(card, false);
            glowObject.transform.SetSiblingIndex(1);
            RectTransform glow = glowObject.GetComponent<RectTransform>();
            glow.anchorMin = new Vector2(0.20f, 0.08f);
            glow.anchorMax = new Vector2(0.98f, 0.92f);
            glow.offsetMin = Vector2.zero;
            glow.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(glowObject.GetComponent<Image>(), new Color(0.20f, 0.42f, 0.68f, 0.10f), 30);
            glowObject.GetComponent<Image>().raycastTarget = false;
        }

        private static void AddCardFill(RectTransform card)
        {
            GameObject fillObject = new GameObject("CardFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(card, false);
            fillObject.transform.SetAsFirstSibling();
            RectTransform fill = fillObject.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            Image image = fillObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(0.025f, 0.105f, 0.19f, 0.98f), 25);
            image.raycastTarget = false;
        }

        private static void CreateIcon(RectTransform card, string iconName)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(card, false);
            RectTransform icon = iconObject.GetComponent<RectTransform>();
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = ScaleVector(new Vector2(88f, 0f));
            icon.sizeDelta = ScaleVector(new Vector2(132f, 112f));
            Image image = iconObject.GetComponent<Image>();
            image.sprite = LoadIcon(iconName);
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static Sprite LoadIcon(string iconName)
        {
            if (IconCache.TryGetValue(iconName, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>($"UI/SolverLearn/{iconName}");
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            IconCache[iconName] = sprite;
            return sprite;
        }

        private static void StyleButtonText(Button button, int fontSize)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(label, true);
        }

        private static Vector2 ScaleVector(Vector2 value)
        {
            return value * CardScale;
        }

        private static int ScaledFont(int fontSize)
        {
            return Mathf.RoundToInt(fontSize * CardScale);
        }

        private static RectTransform CreateSafeArea(Transform parent)
        {
            GameObject safeObject = new GameObject("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(parent, false);
            return safeObject.GetComponent<RectTransform>();
        }

        private static void SetTopRect(RectTransform rect, float y, float horizontalPadding, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-horizontalPadding * 2f, height);
        }

        private static string T(string key)
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key;
        }

        private void RefreshLocalizedText()
        {
            if (headerTitle != null)
            {
                headerTitle.text = T("solver_learn_title");
            }

            if (headerSubtitle != null)
            {
                headerSubtitle.text = T("solver_learn_subtitle");
            }

            if (backButtonText != null)
            {
                backButtonText.text = T("back");
            }

            foreach (CardBinding binding in cardBindings)
            {
                binding.Refresh();
            }
        }

        private sealed class CardBinding
        {
            private readonly string titleKey;
            private readonly string descriptionKey;
            private readonly Text titleText;
            private readonly Text descriptionText;
            private readonly Text openText;

            public CardBinding(string titleKey, string descriptionKey, Text titleText, Text descriptionText, Text openText)
            {
                this.titleKey = titleKey;
                this.descriptionKey = descriptionKey;
                this.titleText = titleText;
                this.descriptionText = descriptionText;
                this.openText = openText;
            }

            public void Refresh()
            {
                if (titleText != null)
                {
                    titleText.text = T(titleKey);
                }

                if (descriptionText != null)
                {
                    descriptionText.text = T(descriptionKey);
                }

                if (openText != null)
                {
                    openText.text = T("open");
                }
            }
        }
    }
}
