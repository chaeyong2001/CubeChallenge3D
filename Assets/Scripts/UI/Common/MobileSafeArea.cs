using UnityEngine;

namespace CubeChallenge3D.UI.Common
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MobileSafeArea : MonoBehaviour
    {
        private const float MinimumTopInsetRatio = 0.012f;
        private const float MinimumBottomInsetRatio = 0.018f;
        private const float MaximumMinimumInsetPixels = 56f;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea
                || lastScreenSize.x != Screen.width
                || lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            float minimumTopInset = Mathf.Min(Screen.height * MinimumTopInsetRatio, MaximumMinimumInsetPixels);
            float minimumBottomInset = Mathf.Min(Screen.height * MinimumBottomInsetRatio, MaximumMinimumInsetPixels);
            safeArea.yMin = Mathf.Max(safeArea.yMin, minimumBottomInset);
            safeArea.yMax = Mathf.Min(safeArea.yMax, Screen.height - minimumTopInset);
            rectTransform.anchorMin = new Vector2(
                safeArea.xMin / Screen.width,
                safeArea.yMin / Screen.height);
            rectTransform.anchorMax = new Vector2(
                safeArea.xMax / Screen.width,
                safeArea.yMax / Screen.height);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
