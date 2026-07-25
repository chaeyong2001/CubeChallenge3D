using System;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Profile
{
    public sealed class AccountChoicePanelUI
    {
        private readonly CanvasGroup rootGroup;
        private readonly Button googlePlayButton;
        private readonly Button guestButton;
        private readonly Text messageText;

        public AccountChoicePanelUI(RectTransform parent, Action onGooglePlaySelected, Action onGuestSelected)
        {
            Root = CreateRoot(parent);
            rootGroup = Root.GetComponent<CanvasGroup>();

            RectTransform panel = CasualUIFactory.CreatePanel(
                Root,
                "AccountChoicePanel",
                new Color(0.035f, 0.07f, 0.1f, 0.96f),
                28);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -255f);
            panel.sizeDelta = new Vector2(850f, 620f);

            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(1f, 0.66f, 0.18f, 0.85f);
            panelOutline.effectDistance = new Vector2(4f, -4f);
            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.62f);
            panelShadow.effectDistance = new Vector2(0f, -12f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "CubeChallenge3D", 54, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, -42f, 80f, -90f);
            title.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(title, true);

            Text guide = RuntimeUiFactory.CreateText(
                panel,
                "Guide",
                "Save your nickname and progress with Google Play Games.\nYou can also continue without sign-in.",
                28,
                TextAnchor.MiddleCenter);
            SetTopRect(guide.rectTransform, -128f, 100f, -92f);
            guide.color = new Color(1f, 1f, 1f, 0.88f);
            guide.lineSpacing = 1.08f;
            CasualUIStyle.ApplyTextDepth(guide, false);

            googlePlayButton = RuntimeUiFactory.CreateButton(
                panel,
                "GooglePlaySignInButton",
                "Sign in with Google Play Games",
                new Vector2(0f, 238f),
                new Vector2(620f, 88f));
            CasualUIStyle.ApplyButton(googlePlayButton, CasualUIColor.Green);
            googlePlayButton.onClick.AddListener(() => onGooglePlaySelected?.Invoke());

            guestButton = RuntimeUiFactory.CreateButton(
                panel,
                "GuestContinueButton",
                "Continue without sign-in",
                new Vector2(0f, 122f),
                new Vector2(620f, 82f));
            CasualUIStyle.ApplyButton(guestButton, CasualUIColor.Slate);
            guestButton.onClick.AddListener(() => onGuestSelected?.Invoke());

            messageText = RuntimeUiFactory.CreateText(panel, "Message", string.Empty, 24, TextAnchor.MiddleCenter);
            SetTopRect(messageText.rectTransform, -456f, 60f, -92f);
            messageText.color = new Color(1f, 0.82f, 0.48f, 0.95f);
            CasualUIStyle.ApplyTextDepth(messageText, false);

            SetVisible(false);
        }

        public RectTransform Root { get; }

        public void SetVisible(bool visible)
        {
            Root.gameObject.SetActive(visible);
            if (rootGroup != null)
            {
                rootGroup.interactable = visible;
                rootGroup.blocksRaycasts = visible;
            }
        }

        public void SetBusy(bool busy, string message)
        {
            googlePlayButton.interactable = !busy;
            guestButton.interactable = !busy;
            SetMessage(message);
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        private static RectTransform CreateRoot(RectTransform parent)
        {
            GameObject rootObject = new GameObject("AccountChoiceRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(parent, false);
            RectTransform rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void SetTopRect(RectTransform rect, float topY, float height, float horizontalInset)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, topY);
            rect.sizeDelta = new Vector2(horizontalInset, height);
        }
    }
}
