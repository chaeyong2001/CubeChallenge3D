using System.Collections.Generic;
using System.Text;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Cube.Utils;
using CubeChallenge3D.Cube.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.Cube.Debugging
{
    public sealed class CubeRuntimeDiagnostics : MonoBehaviour
    {
        [SerializeField] private CubeController controller;
        [SerializeField] private CubeViewOrbitController orbitController;
        [SerializeField] private CubeControlModeController controlModeController;
        [SerializeField] private CubeFaceDragInput faceDragInput;
        [SerializeField] private bool showDebugPanel = true;
        [SerializeField] private float refreshInterval = 0.1f;
        [SerializeField] private Vector2 panelMinSize = new Vector2(320f, 280f);
        [SerializeField] private Vector2 panelMaxSize = new Vector2(760f, 900f);

        private string cachedSummary = "Cube diagnostics unavailable";
        private float nextRefreshTime;
        private GameObject debugPanel;
        private Text debugText;

        public void Initialize(
            CubeController cubeController,
            CubeViewOrbitController viewOrbitController,
            CubeControlModeController controlController,
            CubeFaceDragInput dragInput)
        {
            controller = cubeController;
            orbitController = viewOrbitController;
            controlModeController = controlController;
            faceDragInput = dragInput;
            CreateDebugPanel();
            RefreshSummary();
        }

        public string BuildSummary()
        {
            if (controller == null || controller.CurrentState == null)
            {
                return "Cube diagnostics unavailable";
            }

            CubeState state = controller.CurrentState;
            var summary = new StringBuilder();
            summary.AppendLine($"Solved: {state.IsSolved()}");
            summary.AppendLine($"Moves: {controller.UserMoveCount}");
            summary.AppendLine($"Rotating: {controller.IsRotating}");
            summary.AppendLine($"Sequence: {controller.IsSequenceRunning}");
            summary.AppendLine($"Mode: {(orbitController != null ? orbitController.InteractionMode.ToString() : "-")}");
            summary.AppendLine($"Control: {(controlModeController != null ? controlModeController.CurrentControlMode.ToString() : "-")}");
            summary.AppendLine($"Snapping: {orbitController != null && orbitController.IsSnapping}");
            summary.AppendLine($"Dragging View: {orbitController != null && orbitController.IsDraggingView}");
            summary.AppendLine($"Dragging Face: {faceDragInput != null && faceDragInput.IsDraggingFace}");
            summary.AppendLine($"Drag Threshold: {(faceDragInput != null ? faceDragInput.DragThreshold : 0f):0}");
            summary.AppendLine($"Last Move: {(controller.LastMove.HasValue ? controller.LastMove.Value.ToString() : "-")}");
            summary.AppendLine($"Last Hit Face: {(faceDragInput != null && faceDragInput.LastHitFace.HasValue ? faceDragInput.LastHitFace.Value.ToString() : "-")}");
            summary.AppendLine($"Last Hit Cubie: {(faceDragInput != null && faceDragInput.LastHitGridPosition.HasValue ? faceDragInput.LastHitGridPosition.Value.ToString() : "-")}");
            summary.AppendLine($"Last Layer: {(faceDragInput != null && faceDragInput.LastLayerMove.HasValue ? faceDragInput.LastLayerMove.Value.ToString() : "-")}");
            summary.AppendLine($"Last Drag Move: {(faceDragInput != null && faceDragInput.LastDragMove.HasValue ? faceDragInput.LastDragMove.Value.ToString() : "-")}");
            summary.AppendLine($"Ignored: {(faceDragInput != null ? faceDragInput.LastInputIgnoredReason : "-")}");
            summary.AppendLine($"Scramble Length: {controller.LastScrambleMoves.Count}");
            summary.AppendLine($"Color Valid: {CubeStateValidator.IsColorCountValid(state)}");
            summary.Append($"Facelets: {CubeStateSerializer.ToFaceletString(state)}");
            return summary.ToString();
        }

        public void SetDebugPanelVisible(bool visible)
        {
            showDebugPanel = visible;
        }

        public string BuildPanelSummary()
        {
            if (controller == null || controller.CurrentState == null)
            {
                return "Cube diagnostics unavailable";
            }

            CubeState state = controller.CurrentState;
            var summary = new StringBuilder();
            summary.AppendLine($"Solved: {state.IsSolved()}");
            summary.AppendLine($"Moves: {controller.UserMoveCount}");
            summary.AppendLine($"Rotating: {controller.IsRotating}");
            summary.AppendLine($"Sequence: {controller.IsSequenceRunning}");
            summary.AppendLine($"Mode: {(orbitController != null ? orbitController.InteractionMode.ToString() : "-")}");
            summary.AppendLine($"Control: {(controlModeController != null ? controlModeController.CurrentControlMode.ToString() : "-")}");
            summary.AppendLine($"Snapping: {orbitController != null && orbitController.IsSnapping}");
            summary.AppendLine($"Dragging View: {orbitController != null && orbitController.IsDraggingView}");
            summary.AppendLine($"Dragging Face: {faceDragInput != null && faceDragInput.IsDraggingFace}");
            summary.AppendLine($"Drag Threshold: {(faceDragInput != null ? faceDragInput.DragThreshold : 0f):0}");
            summary.AppendLine($"Last Move: {(controller.LastMove.HasValue ? controller.LastMove.Value.ToString() : "-")}");
            summary.AppendLine($"Last Hit Face: {(faceDragInput != null && faceDragInput.LastHitFace.HasValue ? faceDragInput.LastHitFace.Value.ToString() : "-")}");
            summary.AppendLine($"Last Hit Cubie: {(faceDragInput != null && faceDragInput.LastHitGridPosition.HasValue ? faceDragInput.LastHitGridPosition.Value.ToString() : "-")}");
            summary.AppendLine($"Last Layer: {(faceDragInput != null && faceDragInput.LastLayerMove.HasValue ? faceDragInput.LastLayerMove.Value.ToString() : "-")}");
            summary.AppendLine($"Last Drag Move: {(faceDragInput != null && faceDragInput.LastDragMove.HasValue ? faceDragInput.LastDragMove.Value.ToString() : "-")}");
            summary.AppendLine($"Ignored: {(faceDragInput != null ? faceDragInput.LastInputIgnoredReason : "-")}");
            summary.AppendLine($"Scramble Length: {controller.LastScrambleMoves.Count}");
            summary.Append($"Color Valid: {CubeStateValidator.IsColorCountValid(state)}");
            return summary.ToString();
        }

        public void LogSummary()
        {
            UnityEngine.Debug.Log(BuildSummary(), this);
        }

        public void LogValidation()
        {
            bool colorValid = controller != null
                && CubeStateValidator.ValidateBasic(controller.CurrentState);
            bool viewValid = ValidateView(true);
            UnityEngine.Debug.Log($"Cube validation - Color: {colorValid}, View: {viewValid}", this);
        }

        public void RunSelfCheck()
        {
            bool passed = CubeModelSelfCheck.RunAll();
            UnityEngine.Debug.Log($"Cube model self-check: {(passed ? "PASS" : "FAIL")}", this);
        }

        public bool ValidateView(bool logWarnings)
        {
            if (controller == null)
            {
                return false;
            }

            CubieVisual[] cubies = controller.GetComponentsInChildren<CubieVisual>();
            bool valid = cubies.Length == 26;
            if (!valid && logWarnings)
            {
                UnityEngine.Debug.LogWarning($"Expected 26 cubies, found {cubies.Length}.", this);
            }

            var occupied = new HashSet<Vector3Int>();
            foreach (CubieVisual cubie in cubies)
            {
                Vector3Int position = cubie.CurrentGridPosition;
                bool inRange = IsInGridRange(position);
                bool unique = occupied.Add(position);
                valid &= inRange && unique;

                if (logWarnings && !inRange)
                {
                    UnityEngine.Debug.LogWarning($"{cubie.name} has invalid grid position {position}.", cubie);
                }

                if (logWarnings && !unique)
                {
                    UnityEngine.Debug.LogWarning($"Duplicate cubie grid position {position}.", cubie);
                }
            }

            return valid;
        }

        private void Update()
        {
            if (debugPanel != null && debugPanel.activeSelf != showDebugPanel)
            {
                debugPanel.SetActive(showDebugPanel);
            }

            if (showDebugPanel && Time.unscaledTime >= nextRefreshTime)
            {
                RefreshSummary();
            }
        }

        private void CreateDebugPanel()
        {
            if (debugPanel != null)
            {
                return;
            }

            debugPanel = new GameObject("DEV_CubeDiagnosticsCanvas");
            debugPanel.transform.SetParent(transform, false);

            Canvas canvas = debugPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1300;
            debugPanel.AddComponent<CanvasScaler>();
            debugPanel.AddComponent<GraphicRaycaster>();

            GameObject backgroundObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(debugPanel.transform, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 1f);
            backgroundRect.anchorMax = new Vector2(0f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            backgroundRect.anchoredPosition = new Vector2(12f, -12f);
            backgroundRect.sizeDelta = new Vector2(430f, 500f);

            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.82f);
            background.raycastTarget = true;
            DiagnosticsPanelDragHandle panelDragHandle = backgroundObject.AddComponent<DiagnosticsPanelDragHandle>();
            panelDragHandle.Initialize(backgroundRect);

            AddDragBar(backgroundRect);

            GameObject textObject = new GameObject("DiagnosticsText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(backgroundObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 12f);
            textRect.offsetMax = new Vector2(-14f, -52f);

            debugText = textObject.GetComponent<Text>();
            debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            debugText.fontSize = 18;
            debugText.color = Color.white;
            debugText.alignment = TextAnchor.UpperLeft;
            debugText.horizontalOverflow = HorizontalWrapMode.Overflow;
            debugText.verticalOverflow = VerticalWrapMode.Overflow;
            debugText.raycastTarget = false;

            AddResizeHandle(backgroundRect);
        }

        private static void AddDragBar(RectTransform parent)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 44f);

            Image image = barObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.11f, 0.14f, 0.9f);

            DiagnosticsPanelDragHandle handle = barObject.AddComponent<DiagnosticsPanelDragHandle>();
            handle.Initialize(parent);
        }

        private void AddResizeHandle(RectTransform parent)
        {
            GameObject handleObject = new GameObject("ResizeHandle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(parent, false);
            RectTransform rect = handleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-10f, 10f);
            rect.sizeDelta = new Vector2(40f, 40f);

            Image image = handleObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.35f);

            DiagnosticsPanelResizeHandle handle = handleObject.AddComponent<DiagnosticsPanelResizeHandle>();
            handle.Initialize(parent, panelMinSize, panelMaxSize);
        }

        private void RefreshSummary()
        {
            cachedSummary = BuildPanelSummary();
            if (debugText != null)
            {
                debugText.text = "DEV Cube Diagnostics\n\n" + cachedSummary;
            }

            nextRefreshTime = Time.unscaledTime + refreshInterval;
        }

        private static bool IsInGridRange(Vector3Int position)
        {
            return position.x >= -1 && position.x <= 1
                && position.y >= -1 && position.y <= 1
                && position.z >= -1 && position.z <= 1
                && position != Vector3Int.zero;
        }

        private sealed class DiagnosticsPanelDragHandle : MonoBehaviour, IDragHandler
        {
            private RectTransform target;
            private Canvas canvas;

            public void Initialize(RectTransform dragTarget)
            {
                target = dragTarget;
                canvas = dragTarget.GetComponentInParent<Canvas>();
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (target == null)
                {
                    return;
                }

                float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                target.anchoredPosition += eventData.delta / scale;
            }
        }

        private sealed class DiagnosticsPanelResizeHandle : MonoBehaviour, IDragHandler
        {
            private RectTransform target;
            private Vector2 minSize;
            private Vector2 maxSize;

            public void Initialize(RectTransform resizeTarget, Vector2 minimumSize, Vector2 maximumSize)
            {
                target = resizeTarget;
                minSize = minimumSize;
                maxSize = maximumSize;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (target == null)
                {
                    return;
                }

                Vector2 nextSize = target.sizeDelta + new Vector2(eventData.delta.x, -eventData.delta.y);
                nextSize.x = Mathf.Clamp(nextSize.x, minSize.x, maxSize.x);
                nextSize.y = Mathf.Clamp(nextSize.y, minSize.y, maxSize.y);
                target.sizeDelta = nextSize;
            }
        }
    }
}
