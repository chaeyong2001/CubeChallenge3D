using System;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Cube.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeChallenge3D.Cube.Input
{
    public sealed class CubeFaceDragInput : MonoBehaviour
    {
        [SerializeField] private CubeController controller;
        [SerializeField] private CubeViewOrbitController orbitController;
        [SerializeField] private CubeControlModeController controlModeController;
        [SerializeField] private Camera inputCamera;
        [Tooltip("Minimum screen-space drag distance required to execute a face move.")]
        [SerializeField] private float dragThreshold = 40f;
        [SerializeField] private bool verboseDebugLogs;

        [Header("Direction Correction")]
        [SerializeField] private bool invertUp;
        [SerializeField] private bool invertDown;
        [SerializeField] private bool invertFront;
        [SerializeField] private bool invertBack;
        [SerializeField] private bool invertRight;
        [SerializeField] private bool invertLeft;

        private bool pointerActive;
        private Vector2 pointerStart;
        private CubeFace hitFace;
        private Vector3Int hitGridPosition;

        public CubeMove? LastDragMove { get; private set; }
        public CubeFace? LastHitFace { get; private set; }
        public Vector3Int? LastHitGridPosition { get; private set; }
        public LayerMoveDescriptor? LastLayerMove { get; private set; }
        public string LastInputIgnoredReason { get; private set; }
        public bool IsDraggingFace => pointerActive;
        public float DragThreshold => dragThreshold;

        public void SetDragThreshold(float threshold)
        {
            dragThreshold = Mathf.Max(1f, threshold);
        }

        public void Initialize(
            CubeController cubeController,
            CubeViewOrbitController viewOrbitController,
            CubeControlModeController controlController)
        {
            controller = cubeController;
            orbitController = viewOrbitController;
            controlModeController = controlController;
            inputCamera = inputCamera != null ? inputCamera : Camera.main;
        }

        private void Update()
        {
            if (!CanAcceptInput())
            {
                pointerActive = false;
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

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                BeginPointer(position, PointerUIUtility.IsScreenPositionOverUi(position));
            }

            if (pointerActive && mouse.leftButton.isPressed)
            {
                TryCompleteDrag(position);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                pointerActive = false;
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
            Vector2 position = touch.position.ReadValue();
            if (touch.press.wasPressedThisFrame)
            {
                BeginPointer(
                    position,
                    PointerUIUtility.HasMultipleActiveTouches()
                    || PointerUIUtility.IsPointerOverUi(touch.touchId.ReadValue())
                    || PointerUIUtility.IsScreenPositionOverUi(position));
            }

            if (PointerUIUtility.HasMultipleActiveTouches())
            {
                pointerActive = false;
                return;
            }

            if (pointerActive && touch.press.isPressed)
            {
                TryCompleteDrag(position);
            }

            if (touch.press.wasReleasedThisFrame)
            {
                pointerActive = false;
            }
        }

        private void BeginPointer(Vector2 screenPosition, bool overUi)
        {
            pointerActive = false;
            if (overUi || inputCamera == null || controller.CubeRoot == null)
            {
                LastInputIgnoredReason = overUi ? "Pointer started over UI" : "Missing input camera or CubeRoot";
                return;
            }

            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                LastInputIgnoredReason = "Raycast did not hit a cubie";
                return;
            }

            CubieVisual cubie = hit.collider.GetComponentInParent<CubieVisual>();
            if (cubie == null)
            {
                LastInputIgnoredReason = "Raycast hit has no CubieVisual";
                return;
            }

            Vector3 localNormal = controller.CubeRoot.InverseTransformDirection(hit.normal);
            hitFace = NormalToFace(localNormal);
            hitGridPosition = cubie.CurrentGridPosition;
            pointerStart = screenPosition;
            pointerActive = true;
            LastHitFace = hitFace;
            LastHitGridPosition = hitGridPosition;
            LastInputIgnoredReason = null;
        }

        private void TryCompleteDrag(Vector2 screenPosition)
        {
            Vector2 drag = screenPosition - pointerStart;
            if (drag.sqrMagnitude < dragThreshold * dragThreshold)
            {
                return;
            }

            LastDragMove = null;
            LastLayerMove = null;
            if (CubeDragMoveResolver.TryResolveLayer(
                hitFace,
                hitGridPosition,
                drag,
                inputCamera,
                controller.CubeRoot,
                ShouldInvert(hitFace),
                out LayerMoveDescriptor descriptor,
                out string ignoredReason))
            {
                LastLayerMove = descriptor;
                if (descriptor.TryToCubeMove(out CubeMove move))
                {
                    LastDragMove = move;
                    LastInputIgnoredReason = null;
                    controller.ApplyUserMove(move);
                    if (verboseDebugLogs)
                    {
                        Debug.Log($"Layer drag resolved: {descriptor} -> {move}", this);
                    }
                }
                else
                {
                    LastInputIgnoredReason = "Layer cannot be converted to a supported move";
                }
            }
            else
            {
                LastLayerMove = descriptor;
                LastInputIgnoredReason = ignoredReason;
                if (verboseDebugLogs)
                {
                    Debug.Log($"Layer drag ignored: {ignoredReason}", this);
                }
            }

            // A single pointer drag may produce at most one move.
            pointerActive = false;
        }

        private bool CanAcceptInput()
        {
            return controller != null
                && orbitController != null
                && controlModeController != null
                && orbitController.CurrentMode == CubeInteractionMode.Solve
                && controlModeController.CurrentControlMode == CubeControlMode.Drag
                && !orbitController.IsSnapping
                && !controller.IsBusy
                && controller.UserInputEnabled;
        }

        private bool ShouldInvert(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return invertUp;
                case CubeFace.Down: return invertDown;
                case CubeFace.Front: return invertFront;
                case CubeFace.Back: return invertBack;
                case CubeFace.Right: return invertRight;
                case CubeFace.Left: return invertLeft;
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static CubeFace NormalToFace(Vector3 normal)
        {
            Vector3 absolute = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            {
                return normal.x >= 0f ? CubeFace.Right : CubeFace.Left;
            }

            if (absolute.y >= absolute.z)
            {
                return normal.y >= 0f ? CubeFace.Up : CubeFace.Down;
            }

            return normal.z >= 0f ? CubeFace.Front : CubeFace.Back;
        }

    }
}
