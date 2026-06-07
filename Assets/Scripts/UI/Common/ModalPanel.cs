using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Common
{
    public sealed class ModalPanel
    {
        private readonly GameObject root;
        private readonly Text titleText;
        private readonly Text bodyText;

        public ModalPanel(Transform parent, string name)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, name, 1600);
            root = canvas.gameObject;

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 520f));

            titleText = RuntimeUiFactory.CreateText(panel, "Title", string.Empty, 38, TextAnchor.UpperCenter);
            titleText.rectTransform.offsetMin = new Vector2(28f, 400f);
            titleText.rectTransform.offsetMax = new Vector2(-28f, -28f);

            bodyText = RuntimeUiFactory.CreateText(panel, "Body", string.Empty, 28, TextAnchor.MiddleCenter);
            bodyText.rectTransform.offsetMin = new Vector2(40f, 120f);
            bodyText.rectTransform.offsetMax = new Vector2(-40f, -110f);

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Close", new Vector2(0f, 28f), new Vector2(260f, 72f));
            close.onClick.AddListener(Hide);
            Hide();
        }

        public void Show(string title, string body)
        {
            titleText.text = title;
            bodyText.text = body;
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
