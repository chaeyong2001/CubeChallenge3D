using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeChallenge3D.UI.Common
{
    public sealed class PanelDragHandle : MonoBehaviour, IDragHandler
    {
        private RectTransform target;
        private Canvas parentCanvas;

        public void Initialize(RectTransform dragTarget)
        {
            target = dragTarget;
            parentCanvas = dragTarget != null ? dragTarget.GetComponentInParent<Canvas>() : null;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            float scale = parentCanvas != null && parentCanvas.scaleFactor > 0f
                ? parentCanvas.scaleFactor
                : 1f;
            target.anchoredPosition += eventData.delta / scale;
        }
    }
}
