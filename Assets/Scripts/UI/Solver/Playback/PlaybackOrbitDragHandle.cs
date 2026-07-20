using CubeChallenge3D.Cube.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeChallenge3D.UI.Solver.Playback
{
    public sealed class PlaybackOrbitDragHandle : MonoBehaviour, IDragHandler
    {
        private CubeController cubeController;
        private Camera renderCamera;
        private RectTransform touchArea;
        private float sensitivity;
        private float previousPinchDistance;
        private bool pinching;

        private const float PinchZoomSensitivity = 0.035f;
        private const float MinFieldOfView = 28f;
        private const float MaxFieldOfView = 70f;

        public void Initialize(CubeController controller, Camera camera, float dragSensitivity = 0.3f)
        {
            cubeController = controller;
            renderCamera = camera;
            touchArea = GetComponent<RectTransform>();
            sensitivity = Mathf.Max(0.01f, dragSensitivity);
        }

        private void Update()
        {
            if (renderCamera == null || Input.touchCount != 2)
            {
                pinching = false;
                previousPinchDistance = 0f;
                return;
            }

            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            if (!IsTouchInside(first.position) || !IsTouchInside(second.position))
            {
                pinching = false;
                previousPinchDistance = 0f;
                return;
            }

            float currentDistance = Vector2.Distance(first.position, second.position);
            if (!pinching)
            {
                pinching = true;
                previousPinchDistance = currentDistance;
                return;
            }

            float delta = currentDistance - previousPinchDistance;
            previousPinchDistance = currentDistance;
            renderCamera.fieldOfView = Mathf.Clamp(
                renderCamera.fieldOfView - (delta * PinchZoomSensitivity),
                MinFieldOfView,
                MaxFieldOfView);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Transform viewRoot = cubeController != null ? cubeController.ViewRoot : null;
            if (viewRoot == null || cubeController.IsBusy)
            {
                return;
            }

            Vector3 pitchAxis = renderCamera != null ? renderCamera.transform.right : Vector3.right;
            Quaternion yaw = Quaternion.AngleAxis(-eventData.delta.x * sensitivity, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(eventData.delta.y * sensitivity, pitchAxis);
            viewRoot.rotation = pitch * yaw * viewRoot.rotation;
        }

        private bool IsTouchInside(Vector2 screenPosition)
        {
            return touchArea == null
                || RectTransformUtility.RectangleContainsScreenPoint(touchArea, screenPosition, null);
        }
    }
}
