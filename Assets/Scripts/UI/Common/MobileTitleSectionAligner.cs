using UnityEngine;

namespace CubeChallenge3D.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class MobileTitleSectionAligner : MonoBehaviour
    {
        private const float DefaultReferenceTitleY = -265f;

        [SerializeField] private bool previewInEditor;
        [SerializeField] private string screenName = "Unknown";
        [SerializeField] private RectTransform titleRoot;
        [SerializeField] private RectTransform[] followRoots;
        [SerializeField] private float referenceTitleY = DefaultReferenceTitleY;
        [SerializeField] private float minDelta = -220f;
        [SerializeField] private float maxDelta = 80f;

        private Vector2 originalTitlePosition;
        private Vector2[] originalFollowPositions;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool initialized;
        private bool logged;

        public void Configure(
            string targetScreenName,
            RectTransform targetTitleRoot,
            RectTransform[] targetFollowRoots = null,
            float targetReferenceTitleY = DefaultReferenceTitleY,
            bool enableEditorPreview = false)
        {
            screenName = string.IsNullOrWhiteSpace(targetScreenName) ? "Unknown" : targetScreenName;
            titleRoot = targetTitleRoot;
            followRoots = targetFollowRoots ?? new RectTransform[0];
            referenceTitleY = targetReferenceTitleY;
            previewInEditor = enableEditorPreview;
            CacheOriginalPositions();
            Apply();
        }

        private void Awake()
        {
            CacheOriginalPositions();
        }

        private void OnEnable()
        {
            CacheOriginalPositions();
            Apply();
        }

        private void Update()
        {
            if (!ShouldApply())
            {
                return;
            }

            if (lastSafeArea != Screen.safeArea
                || lastScreenSize.x != Screen.width
                || lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void CacheOriginalPositions()
        {
            if (initialized || titleRoot == null)
            {
                return;
            }

            originalTitlePosition = titleRoot.anchoredPosition;
            int count = followRoots != null ? followRoots.Length : 0;
            originalFollowPositions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                originalFollowPositions[i] = followRoots[i] != null ? followRoots[i].anchoredPosition : Vector2.zero;
            }

            initialized = true;
        }

        private void Apply()
        {
            CacheOriginalPositions();
            if (!initialized || titleRoot == null || !ShouldApply())
            {
                RestoreOriginalPositions();
                return;
            }

            float safeTopInset = IsInsideMobileSafeArea(titleRoot) ? 0f : GetSafeTopInsetCanvasUnits();
            float targetY = referenceTitleY - safeTopInset;
            float deltaY = Mathf.Clamp(targetY - originalTitlePosition.y, minDelta, maxDelta);
            titleRoot.anchoredPosition = originalTitlePosition + new Vector2(0f, deltaY);

            if (followRoots != null && originalFollowPositions != null)
            {
                int count = Mathf.Min(followRoots.Length, originalFollowPositions.Length);
                for (int i = 0; i < count; i++)
                {
                    if (followRoots[i] != null)
                    {
                        followRoots[i].anchoredPosition = originalFollowPositions[i] + new Vector2(0f, deltaY);
                    }
                }
            }

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            LogOnce(deltaY, safeTopInset, targetY);
        }

        private void RestoreOriginalPositions()
        {
            if (!initialized || titleRoot == null)
            {
                return;
            }

            titleRoot.anchoredPosition = originalTitlePosition;
            if (followRoots != null && originalFollowPositions != null)
            {
                int count = Mathf.Min(followRoots.Length, originalFollowPositions.Length);
                for (int i = 0; i < count; i++)
                {
                    if (followRoots[i] != null)
                    {
                        followRoots[i].anchoredPosition = originalFollowPositions[i];
                    }
                }
            }
        }

        private bool ShouldApply()
        {
            if (Application.isEditor && !previewInEditor)
            {
                return false;
            }

#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return previewInEditor;
#endif
        }

        private static bool IsInsideMobileSafeArea(RectTransform rect)
        {
            Transform current = rect != null ? rect.parent : null;
            while (current != null)
            {
                if (current.GetComponent<MobileSafeArea>() != null)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private float GetSafeTopInsetCanvasUnits()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return 0f;
            }

            Canvas canvas = titleRoot != null ? titleRoot.GetComponentInParent<Canvas>() : null;
            RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            float canvasHeight = canvasRect != null && canvasRect.rect.height > 1f ? canvasRect.rect.height : 1920f;
            float safeTopPixels = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            return safeTopPixels / Screen.height * canvasHeight;
        }

        private void LogOnce(float deltaY, float safeTopInset, float targetY)
        {
            if (logged)
            {
                return;
            }

            logged = true;
            Debug.Log(
                $"[MobileTitleAlign] screen={screenName} "
                + $"resolution={Screen.width}x{Screen.height} "
                + $"safeArea={Screen.safeArea} "
                + $"mainMenuTitleY={referenceTitleY:0.0} "
                + $"targetTitleY={targetY:0.0} "
                + $"safeTopInsetCanvas={safeTopInset:0.0} "
                + $"originalTitlePos={originalTitlePosition} "
                + $"appliedOffset=(0,{deltaY:0.0}) "
                + $"finalTitlePos={titleRoot.anchoredPosition} "
                + $"previewInEditor={previewInEditor}");
        }
    }
}
