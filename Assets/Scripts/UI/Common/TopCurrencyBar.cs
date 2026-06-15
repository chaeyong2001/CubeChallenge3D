using CubeChallenge3D.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Common
{
    public sealed class TopCurrencyBar : MonoBehaviour
    {
        private WalletStore walletStore;
        private Text valueText;
        private float nextRefresh;

        public static void Attach(Canvas canvas)
        {
            if (canvas == null || canvas.GetComponentInChildren<TopCurrencyBar>(true) != null)
            {
                return;
            }

            GameObject barObject = new GameObject(
                "TopCurrencyBar",
                typeof(RectTransform),
                typeof(Image),
                typeof(TopCurrencyBar));
            barObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 46f);
            Image background = barObject.GetComponent<Image>();
            background.color = new Color(0.025f, 0.035f, 0.05f, 0.96f);
            background.raycastTarget = false;
        }

        private void Awake()
        {
            walletStore = new WalletStore();
            RectTransform rect = (RectTransform)transform;
            valueText = RuntimeUiFactory.CreateText(
                rect,
                "Values",
                string.Empty,
                20,
                TextAnchor.MiddleCenter);
            valueText.rectTransform.offsetMin = new Vector2(20f, 0f);
            valueText.rectTransform.offsetMax = new Vector2(-20f, 0f);
            WalletStore.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            WalletStore.Changed -= Refresh;
        }

        private void LateUpdate()
        {
            transform.SetAsLastSibling();
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 1f;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (valueText == null || walletStore == null)
            {
                return;
            }

            int hearts = walletStore.Hearts;
            string timer = hearts < WalletStore.MaxNaturalHearts
                ? $"  {FormatTime(walletStore.SecondsUntilNextHeart)}"
                : string.Empty;
            valueText.text =
                $"\u2665 {hearts}{timer}     \u25CF {walletStore.Coins:N0}     \u25C6 {walletStore.Gems:N0}";
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = Mathf.Max(0, totalSeconds) / 60;
            int seconds = Mathf.Max(0, totalSeconds) % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
