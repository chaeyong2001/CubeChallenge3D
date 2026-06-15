using System;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Learn
{
    public sealed class LearnModeHubView
    {
        private readonly GameObject root;
        private readonly Text statusText;
        private Action manualSolverAction;
        private Action<string> categoryAction;
        private Action practiceAction;

        public LearnModeHubView(Transform parent)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "SolverLearnHubCanvas", 1505);
            root = canvas.gameObject;

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

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Solver & Learn", 46, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, -70f, 70f, 60f);

            Text subtitle = RuntimeUiFactory.CreateText(
                panel,
                "Subtitle",
                "Solve your cube or build skills step by step.",
                24,
                TextAnchor.MiddleCenter);
            SetTopRect(subtitle.rectTransform, -138f, 70f, 48f);

            CreateCard(panel, "Manual Solver", "Enter your cube and get a solution.", 704f, true, () =>
            {
                Hide();
                manualSolverAction?.Invoke();
            });
            CreateCard(panel, "Learn Basics", "Understand faces, turns, and notation.", 604f, true, () => OpenCategory("basics"));
            CreateCard(panel, "Beginner Method", "Learn the 3x3 step by step.", 504f, true, () => OpenCategory("beginner"));
            CreateCard(panel, "Formula Practice", "Practice common move patterns.", 404f, true, () => OpenCategory("formulas"));
            CreateCard(panel, "Tutorial Playback", "Watch guided face-turn demonstrations.", 304f, true, () => OpenCategory("notation"));
            CreateCard(panel, "Practice", "Practice freely without limits. No hearts required.", 204f, true, () =>
            {
                Hide();
                practiceAction?.Invoke();
            });
            statusText = RuntimeUiFactory.CreateText(panel, "Status", string.Empty, 22, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0f, 92f);
            statusText.rectTransform.sizeDelta = new Vector2(-80f, 36f);

            Button back = RuntimeUiFactory.CreateButton(panel, "BackButton", "Back", new Vector2(0f, 24f), new Vector2(300f, 56f));
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

        public void Show()
        {
            statusText.text = "Choose a solver or learning activity.";
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private void CreateCard(
            RectTransform parent,
            string title,
            string description,
            float y,
            bool available,
            Action action)
        {
            GameObject cardObject = new GameObject(title.Replace(" ", string.Empty) + "Card", typeof(RectTransform), typeof(Image));
            cardObject.transform.SetParent(parent, false);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            float normalizedY = Mathf.Clamp01(y / 920f);
            card.anchorMin = new Vector2(0.08f, normalizedY);
            card.anchorMax = new Vector2(0.92f, normalizedY);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(0f, 96f);
            cardObject.GetComponent<Image>().color = available
                ? new Color(0.1f, 0.2f, 0.25f, 1f)
                : new Color(0.09f, 0.11f, 0.14f, 1f);

            Text titleText = RuntimeUiFactory.CreateText(card, "Title", title, 25, TextAnchor.UpperLeft);
            titleText.rectTransform.offsetMin = new Vector2(22f, 42f);
            titleText.rectTransform.offsetMax = new Vector2(-170f, -14f);

            Text body = RuntimeUiFactory.CreateText(card, "Description", description, 17, TextAnchor.LowerLeft);
            body.rectTransform.offsetMin = new Vector2(22f, 12f);
            body.rectTransform.offsetMax = new Vector2(-170f, -40f);

            Button open = RuntimeUiFactory.CreateButton(card, "OpenButton", available ? "Open" : "Coming Soon", Vector2.zero, new Vector2(142f, 48f));
            RectTransform openRect = open.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(1f, 0.5f);
            openRect.anchorMax = new Vector2(1f, 0.5f);
            openRect.pivot = new Vector2(1f, 0.5f);
            openRect.anchoredPosition = new Vector2(-20f, 0f);
            open.interactable = available;
            if (available && action != null)
            {
                open.onClick.AddListener(() => action());
            }
        }

        private void OpenCategory(string categoryId)
        {
            Hide();
            categoryAction?.Invoke(categoryId);
        }

        private static void AddDragBar(RectTransform parent)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image), typeof(PanelDragHandle));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 52f);
            barObject.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.14f, 1f);
            barObject.GetComponent<PanelDragHandle>().Initialize(parent);
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
    }
}
