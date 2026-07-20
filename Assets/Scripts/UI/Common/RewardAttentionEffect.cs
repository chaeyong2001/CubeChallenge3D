using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class RewardAttentionEffect : MonoBehaviour
    {
        [SerializeField] private float scaleAmplitude = 0.035f;
        [SerializeField] private float rotationAmplitude = 1.6f;
        [SerializeField] private float speed = 3.1f;

        private RectTransform rectTransform;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Outline outline;
        private Color baseOutlineColor;
        private bool outlineCreated;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
            baseRotation = rectTransform != null ? rectTransform.localRotation : Quaternion.identity;
        }

        private void OnDisable()
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
                rectTransform.localRotation = baseRotation;
            }

            if (outline != null)
            {
                Color color = baseOutlineColor;
                if (outlineCreated)
                {
                    color.a = 0f;
                }

                outline.effectColor = color;
            }
        }

        private void Update()
        {
            if (rectTransform == null)
            {
                return;
            }

            float pulse = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
            float scale = 1f + (pulse * scaleAmplitude);
            float rotation = Mathf.Sin(Time.unscaledTime * speed * 1.17f) * rotationAmplitude;
            rectTransform.localScale = baseScale * scale;
            rectTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rotation);

            if (outline != null)
            {
                Color color = baseOutlineColor;
                color.a = Mathf.Lerp(0.28f, 0.82f, pulse);
                outline.effectColor = color;
            }
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            rectTransform = GetComponent<RectTransform>();
            baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
            baseRotation = rectTransform != null ? rectTransform.localRotation : Quaternion.identity;

            outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(5f, -5f);
                outlineCreated = true;
            }

            baseOutlineColor = new Color(1f, 0.82f, 0.18f, outlineCreated ? 0.38f : outline.effectColor.a);
            outline.effectColor = baseOutlineColor;
            initialized = true;
        }
    }
}
