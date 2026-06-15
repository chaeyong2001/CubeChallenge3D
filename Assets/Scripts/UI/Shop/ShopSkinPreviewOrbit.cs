using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeChallenge3D.UI.Shop
{
    public sealed class ShopSkinPreviewOrbit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float autoRotationSpeed = 18f;
        [SerializeField] private float dragSensitivity = 0.28f;

        private Transform target;
        private bool dragging;

        public void Initialize(Transform rotationTarget)
        {
            target = rotationTarget;
        }

        private void Update()
        {
            if (!dragging && target != null)
            {
                target.Rotate(Vector3.up, autoRotationSpeed * Time.unscaledDeltaTime, Space.World);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            target.Rotate(Vector3.up, -eventData.delta.x * dragSensitivity, Space.World);
            target.Rotate(Vector3.right, eventData.delta.y * dragSensitivity, Space.World);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
        }
    }
}
