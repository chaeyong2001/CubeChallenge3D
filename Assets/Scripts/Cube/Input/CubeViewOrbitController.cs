using System.Collections;
using CubeChallenge3D.Cube.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeChallenge3D.Cube.Input
{
    public sealed class CubeViewOrbitController : MonoBehaviour
    {
        [SerializeField] private CubeController controller;
        [Tooltip("Degrees of orbit rotation per pointer pixel.")]
        [SerializeField] private float orbitSensitivity = 0.25f;
        [Tooltip("Pointer movement required before orbiting starts.")]
        [SerializeField] private float minDragDistance = 8f;
        [SerializeField] private float snapDuration = 0.25f;
        [SerializeField] private CubeInteractionMode interactionMode = CubeInteractionMode.View;

        private Transform viewRoot;
        private bool mouseDragging;
        private bool touchDragging;
        private bool mouseThresholdReached;
        private bool touchThresholdReached;
        private Vector2 mouseDragStart;
        private Vector2 touchDragStart;

        public CubeInteractionMode InteractionMode => interactionMode;
        public CubeInteractionMode CurrentMode => interactionMode;
        public bool IsSnapping { get; private set; }
        public bool IsDraggingView => mouseDragging || touchDragging;
        public float OrbitSensitivity => orbitSensitivity;
        public float MinDragDistance => minDragDistance;

        public void SetOrbitSensitivity(float sensitivity)
        {
            orbitSensitivity = Mathf.Max(0.01f, sensitivity);
        }

        public void Initialize(CubeController cubeController)
        {
            controller = cubeController;
            RefreshViewRoot();
        }

        public void SetViewMode()
        {
            if (IsSnapping || controller == null || controller.IsBusy)
            {
                return;
            }

            interactionMode = CubeInteractionMode.View;
            ResetDragState();
        }

        public void SetSolveMode()
        {
            if (IsSnapping || controller == null || controller.IsBusy)
            {
                return;
            }

            RefreshViewRoot();
            if (viewRoot == null)
            {
                return;
            }

            StartCoroutine(SnapToSolveMode());
        }

        public void ToggleMode()
        {
            if (interactionMode == CubeInteractionMode.View)
            {
                SetSolveMode();
            }
            else if (interactionMode == CubeInteractionMode.Solve)
            {
                SetViewMode();
            }
        }

        public void ToggleViewSolveMode()
        {
            ToggleMode();
        }

        public void ResetViewRotation()
        {
            RefreshViewRoot();
            if (viewRoot != null && !IsSnapping && (controller == null || !controller.IsBusy))
            {
                viewRoot.localRotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            RefreshViewRoot();
            if (interactionMode != CubeInteractionMode.View
                || IsSnapping
                || controller == null
                || controller.IsBusy
                || viewRoot == null)
            {
                ResetDragState();
                return;
            }

            HandleMouse();
            HandleTouch();
        }

        private void HandleMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 position = mouse.position.ReadValue();
                mouseDragging = !PointerUIUtility.IsScreenPositionOverUi(position);
                mouseThresholdReached = false;
                mouseDragStart = position;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                mouseDragging = false;
                mouseThresholdReached = false;
            }

            if (mouseDragging && mouse.leftButton.isPressed)
            {
                Vector2 position = mouse.position.ReadValue();
                mouseThresholdReached |= HasReachedThreshold(position - mouseDragStart);
                if (mouseThresholdReached)
                {
                    RotateView(mouse.delta.ReadValue());
                }
            }
        }

        private void HandleTouch()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                int touchId = touch.touchId.ReadValue();
                touchDragging = !PointerUIUtility.HasMultipleActiveTouches()
                    && !PointerUIUtility.IsPointerOverUi(touchId)
                    && !PointerUIUtility.IsScreenPositionOverUi(touch.position.ReadValue());
                touchThresholdReached = false;
                touchDragStart = touch.position.ReadValue();
            }

            if (touch.press.wasReleasedThisFrame)
            {
                touchDragging = false;
                touchThresholdReached = false;
            }

            if (PointerUIUtility.HasMultipleActiveTouches())
            {
                touchDragging = false;
                return;
            }

            if (touchDragging && touch.press.isPressed)
            {
                Vector2 position = touch.position.ReadValue();
                touchThresholdReached |= HasReachedThreshold(position - touchDragStart);
                if (touchThresholdReached)
                {
                    RotateView(touch.delta.ReadValue());
                }
            }
        }

        private void RotateView(Vector2 delta)
        {
            Camera camera = Camera.main;
            Vector3 pitchAxis = camera != null ? camera.transform.right : Vector3.right;
            Quaternion yaw = Quaternion.AngleAxis(-delta.x * orbitSensitivity, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(delta.y * orbitSensitivity, pitchAxis);
            viewRoot.rotation = pitch * yaw * viewRoot.rotation;
        }

        private bool HasReachedThreshold(Vector2 drag)
        {
            return drag.sqrMagnitude >= minDragDistance * minDragDistance;
        }

        private void ResetDragState()
        {
            mouseDragging = false;
            touchDragging = false;
            mouseThresholdReached = false;
            touchThresholdReached = false;
        }

        private IEnumerator SnapToSolveMode()
        {
            IsSnapping = true;
            interactionMode = CubeInteractionMode.Locked;
            ResetDragState();

            Quaternion startRotation = viewRoot.localRotation;
            Quaternion targetRotation = SnapRotationUtility.GetNearestRightAngleRotation(startRotation);
            float elapsed = 0f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float progress = snapDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / snapDuration);
                viewRoot.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                yield return null;
            }

            viewRoot.localRotation = targetRotation;
            IsSnapping = false;
            interactionMode = CubeInteractionMode.Solve;
        }

        private void RefreshViewRoot()
        {
            if (controller != null && controller.ViewRoot != null)
            {
                viewRoot = controller.ViewRoot;
            }
        }

    }
}
