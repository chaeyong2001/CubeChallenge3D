using System;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Style
{
    public static class CasualUIFactory
    {
        public static RectTransform CreatePanel(
            Transform parent,
            string name,
            Color color,
            int radius = 24)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            CasualUIStyle.ApplyPanel(panelObject.GetComponent<Image>(), color, radius);
            return rect;
        }

        public static RectTransform CreateIconHolder(
            Transform parent,
            string iconKey,
            Color iconColor,
            Color holderColor)
        {
            RectTransform holder = CreatePanel(parent, "IconHolder", holderColor, 22);
            CasualIconFactory.Create(holder, iconKey, iconColor);

            GameObject highlightObject = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            highlightObject.transform.SetParent(holder, false);
            RectTransform highlight = highlightObject.GetComponent<RectTransform>();
            highlight.anchorMin = new Vector2(0.08f, 0.64f);
            highlight.anchorMax = new Vector2(0.92f, 0.9f);
            highlight.offsetMin = Vector2.zero;
            highlight.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(highlightObject.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.13f), 16);
            highlightObject.GetComponent<Image>().raycastTarget = false;
            return holder;
        }

        public static Button CreateLargeMenuCard(
            Transform parent,
            string name,
            string title,
            string subtitle,
            string iconKey,
            CasualUIColor theme,
            UnityAction action)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            buttonObject.AddComponent<LargeMenuCardButton>().Configure(iconKey, theme);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 184f;
            layout.minHeight = 168f;

            Sprite cardSprite = CasualIconFactory.LoadMainMenuKitSprite($"Cards/{iconKey}");
            bool useBakedCardSprite = cardSprite != null;
            if (useBakedCardSprite)
            {
                Image cardImage = buttonObject.GetComponent<Image>();
                cardImage.sprite = cardSprite;
                cardImage.type = Image.Type.Simple;
                cardImage.preserveAspect = false;
                cardImage.color = Color.white;

                ColorBlock cardColors = button.colors;
                cardColors.normalColor = Color.white;
                cardColors.highlightedColor = new Color(1f, 1f, 1f, 1f);
                cardColors.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
                cardColors.selectedColor = cardColors.highlightedColor;
                cardColors.disabledColor = new Color(0.55f, 0.55f, 0.58f, 0.72f);
                cardColors.colorMultiplier = 1f;
                button.colors = cardColors;
                button.transition = Selectable.Transition.ColorTint;
                buttonObject.AddComponent<CasualUIButton>();
                return button;
            }

            CasualUIStyle.ApplyButton(button, theme);

            GameObject lowerShadeObject = new GameObject("CardLowerShade", typeof(RectTransform), typeof(Image));
            lowerShadeObject.transform.SetParent(buttonObject.transform, false);
            RectTransform lowerShade = lowerShadeObject.GetComponent<RectTransform>();
            lowerShade.anchorMin = new Vector2(0.025f, 0.035f);
            lowerShade.anchorMax = new Vector2(0.975f, 0.31f);
            lowerShade.offsetMin = Vector2.zero;
            lowerShade.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyPanel(lowerShadeObject.GetComponent<Image>(), new Color(0.025f, 0.02f, 0.1f, 0.22f), 20);
            lowerShadeObject.GetComponent<Image>().raycastTarget = false;

            RectTransform iconHolder = CreateIconHolder(
                buttonObject.transform,
                iconKey,
                new Color(1f, 0.94f, 0.74f),
                new Color(0.025f, 0.045f, 0.12f, 0.3f));
            iconHolder.anchorMin = new Vector2(0.03f, 0.11f);
            iconHolder.anchorMax = new Vector2(0.245f, 0.89f);
            iconHolder.offsetMin = Vector2.zero;
            iconHolder.offsetMax = Vector2.zero;
            Outline iconOutline = iconHolder.gameObject.AddComponent<Outline>();
            iconOutline.effectColor = new Color(1f, 0.88f, 0.52f, 0.48f);
            iconOutline.effectDistance = new Vector2(2f, -2f);

            Text titleText = RuntimeUiFactory.CreateText(
                buttonObject.GetComponent<RectTransform>(),
                "Title",
                title,
                37,
                TextAnchor.MiddleLeft);
            titleText.fontStyle = FontStyle.Bold;
            titleText.rectTransform.anchorMin = new Vector2(0.285f, 0.42f);
            titleText.rectTransform.anchorMax = new Vector2(0.86f, 0.92f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(titleText, true);

            Text subtitleText = RuntimeUiFactory.CreateText(
                buttonObject.GetComponent<RectTransform>(),
                "Subtitle",
                subtitle,
                22,
                TextAnchor.MiddleLeft);
            subtitleText.color = new Color(1f, 1f, 1f, 0.94f);
            subtitleText.rectTransform.anchorMin = new Vector2(0.285f, 0.08f);
            subtitleText.rectTransform.anchorMax = new Vector2(0.88f, 0.49f);
            subtitleText.rectTransform.offsetMin = Vector2.zero;
            subtitleText.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(subtitleText, false);

            GameObject chevronObject = new GameObject("Chevron", typeof(RectTransform));
            chevronObject.transform.SetParent(buttonObject.transform, false);
            RectTransform chevron = chevronObject.GetComponent<RectTransform>();
            chevron.anchorMin = new Vector2(0.89f, 0.26f);
            chevron.anchorMax = new Vector2(0.97f, 0.74f);
            chevron.offsetMin = Vector2.zero;
            chevron.offsetMax = Vector2.zero;
            CasualIconFactory.Create(chevron, "chevron", Color.white);
            return button;
        }

        public static Button CreateActionButton(
            RectTransform parent,
            string name,
            string label,
            string iconKey,
            CasualUIColor theme,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            bool compact = false)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Button button = buttonObject.GetComponent<Button>();
            CasualUIStyle.ApplyButton(button, theme);
            bool itemButton = name.Contains("Plus")
                || name.Contains("Hint")
                || name.Contains("Undo");
            if (itemButton)
            {
                buttonObject.AddComponent<ItemActionButton>().Configure(iconKey, theme);
            }
            else
            {
                buttonObject.AddComponent<MediumActionButton>().Configure(iconKey, theme);
            }
            bool hasIcon = !string.IsNullOrEmpty(iconKey);
            if (hasIcon)
            {
                RectTransform iconHolder = CreateIconHolder(
                    rect,
                    iconKey,
                    new Color(1f, 0.95f, 0.76f),
                    new Color(0f, 0f, 0f, 0.12f));
                iconHolder.anchorMin = new Vector2(0.035f, 0.14f);
                iconHolder.anchorMax = new Vector2(compact ? 0.31f : 0.27f, 0.86f);
                iconHolder.offsetMin = Vector2.zero;
                iconHolder.offsetMax = Vector2.zero;
            }

            Text text = RuntimeUiFactory.CreateText(
                rect,
                "Label",
                label,
                compact ? 24 : 28,
                TextAnchor.MiddleCenter);
            if (hasIcon)
            {
                text.rectTransform.anchorMin = new Vector2(compact ? 0.28f : 0.24f, 0f);
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = Vector2.zero;
                text.rectTransform.offsetMax = Vector2.zero;
            }
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 17;
            text.resizeTextMaxSize = compact ? 24 : 28;
            CasualUIStyle.ApplyTextDepth(text, true);
            return button;
        }

        public static RectTransform CreateStatChip(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            out Text label)
        {
            RectTransform chip = CreatePanel(parent, name, new Color(0.035f, 0.035f, 0.085f, 0.88f), 28);
            chip.gameObject.AddComponent<InfoChip>().Configure(name);
            chip.anchorMin = new Vector2(0.5f, 1f);
            chip.anchorMax = new Vector2(0.5f, 1f);
            chip.pivot = new Vector2(0.5f, 1f);
            chip.anchoredPosition = position;
            chip.sizeDelta = size;

            Outline outline = chip.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.68f, 0.22f, 0.62f);
            outline.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = chip.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(0f, -6f);

            label = RuntimeUiFactory.CreateText(chip, "Label", string.Empty, 30, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(label, true);
            return chip;
        }

        public static RectTransform CreateBadge(Transform parent, string value = "0")
        {
            RectTransform badge = CreatePanel(parent, "CountBadge", new Color(0.22f, 0.78f, 0.08f), 22);
            badge.anchorMin = new Vector2(1f, 1f);
            badge.anchorMax = new Vector2(1f, 1f);
            badge.pivot = new Vector2(1f, 1f);
            badge.anchoredPosition = new Vector2(-8f, -8f);
            badge.sizeDelta = new Vector2(62f, 46f);
            Outline outline = badge.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.88f, 0.34f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            Text text = RuntimeUiFactory.CreateText(badge, "Value", value, 22, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(text, true);
            return badge;
        }

        public static void CreateBackdrop(
            Transform parent,
            string name = "CasualBackdrop",
            bool opaqueBackground = true,
            bool includeSparkles = true)
        {
            GameObject background = new GameObject(name, typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = background.GetComponent<Image>();
            CasualUIStyle.ApplyBackground(image);
            if (!opaqueBackground)
            {
                image.color = Color.clear;
            }
            image.raycastTarget = false;

            if (!includeSparkles)
            {
                background.transform.SetAsFirstSibling();
                return;
            }

            Vector2[] positions =
            {
                new Vector2(0.1f, 0.76f),
                new Vector2(0.89f, 0.7f),
                new Vector2(0.14f, 0.39f),
                new Vector2(0.82f, 0.31f),
                new Vector2(0.67f, 0.58f),
                new Vector2(0.34f, 0.84f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject sparkle = new GameObject($"Sparkle{i}", typeof(RectTransform), typeof(Image));
                sparkle.transform.SetParent(background.transform, false);
                RectTransform sparkleRect = sparkle.GetComponent<RectTransform>();
                sparkleRect.anchorMin = positions[i];
                sparkleRect.anchorMax = positions[i];
                sparkleRect.pivot = new Vector2(0.5f, 0.5f);
                sparkleRect.sizeDelta = i % 2 == 0 ? new Vector2(14f, 14f) : new Vector2(8f, 8f);
                sparkleRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image sparkleImage = sparkle.GetComponent<Image>();
                CasualUIStyle.ApplyPanel(sparkleImage, new Color(1f, 0.74f, 0.2f, 0.72f), 3);
                sparkleImage.raycastTarget = false;
            }

            background.transform.SetAsFirstSibling();
        }
    }
}
