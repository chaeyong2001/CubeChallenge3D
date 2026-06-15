using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeChallenge3D.UI.Stages
{
    public sealed class TargetPreviewCubeView : MonoBehaviour
    {
        [SerializeField] private float orbitSensitivity = 0.25f;
        [SerializeField] private float minDragDistance = 8f;

        private CubeVisualBuilder builder;
        private bool orbitEnabled;
        private bool mouseDragging;
        private bool touchDragging;
        private bool mouseThresholdReached;
        private bool touchThresholdReached;
        private Vector2 mouseDragStart;
        private Vector2 touchDragStart;

        public void Show(CubeState targetState, Vector3 position, Vector3 eulerAngles, float scale, bool allowOrbit)
        {
            if (targetState == null)
            {
                Hide();
                return;
            }

            EnsureBuilder();
            gameObject.SetActive(true);
            transform.position = position;
            transform.rotation = Quaternion.Euler(eulerAngles);
            transform.localScale = Vector3.one * scale;
            orbitEnabled = allowOrbit;
            ResetDragState();
            builder.Build(targetState);
            RemovePreviewColliders();
        }

        public void Hide()
        {
            if (builder != null)
            {
                builder.Clear();
            }

            orbitEnabled = false;
            ResetDragState();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!orbitEnabled || !gameObject.activeSelf)
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
                mouseDragging = true;
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
                    RotatePreview(mouse.delta.ReadValue());
                }
            }
        }

        private void HandleTouch()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null || PointerUIUtility.HasMultipleActiveTouches())
            {
                touchDragging = false;
                return;
            }

            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                touchDragging = true;
                touchThresholdReached = false;
                touchDragStart = touch.position.ReadValue();
            }

            if (touch.press.wasReleasedThisFrame)
            {
                touchDragging = false;
                touchThresholdReached = false;
            }

            if (touchDragging && touch.press.isPressed)
            {
                Vector2 position = touch.position.ReadValue();
                touchThresholdReached |= HasReachedThreshold(position - touchDragStart);
                if (touchThresholdReached)
                {
                    RotatePreview(touch.delta.ReadValue());
                }
            }
        }

        private void RotatePreview(Vector2 delta)
        {
            Camera camera = Camera.main;
            Vector3 pitchAxis = camera != null ? camera.transform.right : Vector3.right;
            Quaternion yaw = Quaternion.AngleAxis(-delta.x * orbitSensitivity, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(delta.y * orbitSensitivity, pitchAxis);
            transform.rotation = pitch * yaw * transform.rotation;
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

        private void EnsureBuilder()
        {
            if (builder == null)
            {
                builder = GetComponent<CubeVisualBuilder>();
            }

            if (builder == null)
            {
                builder = gameObject.AddComponent<CubeVisualBuilder>();
            }
        }

        private void RemovePreviewColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider previewCollider in colliders)
            {
                Destroy(previewCollider);
            }
        }
    }
}
