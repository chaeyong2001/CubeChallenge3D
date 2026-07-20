using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeChallenge3D.UI.Shop
{
    public sealed class ShopSkinPreviewOrbit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] private float autoRotationSpeed = 18f;
        [SerializeField] private float dragSensitivity = 0.28f;
        [SerializeField] private float scrollZoomSensitivity = 0.08f;
        [SerializeField] private float pinchZoomSensitivity = 0.004f;
        [SerializeField] private float minZoom = 0.86f;
        [SerializeField] private float maxZoom = 1.18f;

        private Transform target;
        private bool dragging;
        private float zoom = 1f;
        private float previousPinchDistance;

        public void Initialize(Transform rotationTarget)
        {
            target = rotationTarget;
            zoom = 1f;
            previousPinchDistance = 0f;
            ApplyZoom();
        }

        private void Update()
        {
            HandlePinchZoom();
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

        public void OnScroll(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            SetZoom(zoom + (eventData.scrollDelta.y * scrollZoomSensitivity));
        }

        private void HandlePinchZoom()
        {
            if (target == null || Input.touchCount < 2)
            {
                previousPinchDistance = 0f;
                return;
            }

            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            float distance = Vector2.Distance(first.position, second.position);
            if (previousPinchDistance > 0f)
            {
                SetZoom(zoom + ((distance - previousPinchDistance) * pinchZoomSensitivity));
            }

            previousPinchDistance = distance;
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, minZoom, maxZoom);
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (target != null)
            {
                target.localScale = Vector3.one * zoom;
            }
        }
    }
}
