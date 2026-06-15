using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.View;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.UI;
using CubeChallenge3D.Economy;

namespace CubeChallenge3D.UI.Solver
{
    public sealed class SolverFaceGuideView : MonoBehaviour
    {
        private readonly Dictionary<CubeFace, GuideFace> guideFaces = new Dictionary<CubeFace, GuideFace>();
        private Text selectedText;
        private Text instructionText;
        private Text[] legendLabels;
        private RawImage previewImage;
        private RenderTexture renderTexture;
        private Camera guideCamera;
        private GameObject sceneRoot;
        private Transform cubeRoot;
        private TextMesh selectedFaceMesh;
        private Vector3 guideViewDirection = new Vector3(2.1f, 1.55f, -3.45f).normalized;
        private Vector3 guideUpDirection = Vector3.up;
        private Vector3 guideRightDirection = Vector3.right;
        private Quaternion targetRotation = Quaternion.identity;
        private CubeFace selectedFace = CubeFace.Up;

        public void Initialize(RectTransform parent)
        {
            transform.SetParent(parent, false);
            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = new Vector2(-18f, -186f);
            root.sizeDelta = new Vector2(160f, 172f);

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.075f, 0.09f, 0.88f);

            selectedText = RuntimeUiFactory.CreateText(root, "SelectedFaceText", string.Empty, 11, TextAnchor.UpperCenter);
            selectedText.rectTransform.anchorMin = new Vector2(0f, 1f);
            selectedText.rectTransform.anchorMax = new Vector2(1f, 1f);
            selectedText.rectTransform.pivot = new Vector2(0.5f, 1f);
            selectedText.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            selectedText.rectTransform.sizeDelta = new Vector2(-10f, 24f);

            previewImage = CreatePreviewImage(root);
            BuildGuideScene();

            instructionText = RuntimeUiFactory.CreateText(root, "Instruction", "Face guide", 10, TextAnchor.UpperCenter);
            instructionText.rectTransform.anchorMin = new Vector2(0f, 0f);
            instructionText.rectTransform.anchorMax = new Vector2(1f, 0f);
            instructionText.rectTransform.pivot = new Vector2(0.5f, 0f);
            instructionText.rectTransform.anchoredPosition = new Vector2(0f, 34f);
            instructionText.rectTransform.sizeDelta = new Vector2(-10f, 18f);

            CreateLegend(root);
            SetSelectedFace(CubeFace.Up);
        }

        private void Update()
        {
            if (cubeRoot == null)
            {
                return;
            }

            cubeRoot.localRotation = Quaternion.Slerp(cubeRoot.localRotation, targetRotation, Time.unscaledDeltaTime * 9f);
            UpdateSelectedFaceLabelBillboard();
        }

        private void OnDestroy()
        {
            if (sceneRoot != null)
            {
                Destroy(sceneRoot);
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        public void SetSelectedFace(CubeFace face)
        {
            selectedFace = face;
            targetRotation = GetRotationForFace(face);
            HighlightFace(face);
        }

        public void HighlightFace(CubeFace face)
        {
            if (selectedText != null)
            {
                selectedText.text = $"Selected: {GetFaceCode(face)} - {GetFaceName(face)} Face";
            }

            foreach (KeyValuePair<CubeFace, GuideFace> pair in guideFaces)
            {
                bool highlighted = pair.Key == face;
                SetMaterialColor(
                    pair.Value.Renderer.material,
                    highlighted ? Color.Lerp(pair.Value.BaseColor, Color.yellow, 0.5f) : pair.Value.BaseColor);
                pair.Value.Transform.localPosition = pair.Value.BasePosition + (highlighted ? pair.Value.Normal * 0.08f : Vector3.zero);
                pair.Value.Transform.localScale = pair.Value.BaseScale * (highlighted ? 1.08f : 1f);

                if (highlighted)
                {
                    MoveSelectedFaceLabel(pair.Value, GetFaceCode(face));
                }
            }

            UpdateLegend(face);
        }

        public void ClearHighlight()
        {
            HighlightFace(selectedFace);
        }

        private void MoveSelectedFaceLabel(GuideFace face, string label)
        {
            EnsureSelectedFaceLabel();
            if (selectedFaceMesh == null)
            {
                return;
            }

            Transform labelTransform = selectedFaceMesh.transform;
            labelTransform.localPosition = face.BasePosition + (face.Normal * 0.18f);
            selectedFaceMesh.text = label;
            UpdateSelectedFaceLabelBillboard();
        }

        private void UpdateSelectedFaceLabelBillboard()
        {
            if (selectedFaceMesh == null || guideCamera == null)
            {
                return;
            }

            selectedFaceMesh.transform.rotation = guideCamera.transform.rotation;
        }

        private RawImage CreatePreviewImage(RectTransform parent)
        {
            GameObject imageObject = new GameObject("GuideCubePreview", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -34f);
            rect.sizeDelta = new Vector2(112f, 84f);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.color = Color.white;
            return image;
        }

        private void BuildGuideScene()
        {
            renderTexture = new RenderTexture(320, 240, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            previewImage.texture = renderTexture;

            sceneRoot = new GameObject("SolverFaceGuideScene");
            sceneRoot.transform.position = new Vector3(3000f, 3000f, 3000f);

            cubeRoot = new GameObject("GuideCubeRoot").transform;
            cubeRoot.SetParent(sceneRoot.transform, false);
            cubeRoot.localPosition = Vector3.zero;

            CreateFace(CubeFace.Up, "U", new Vector3(0f, 0.52f, 0f), new Vector3(1f, 0.04f, 1f), Vector3.up, Color.white);
            CreateFace(CubeFace.Down, "D", new Vector3(0f, -0.52f, 0f), new Vector3(1f, 0.04f, 1f), Vector3.down, Color.yellow);
            CreateFace(CubeFace.Front, "F", new Vector3(0f, 0f, 0.52f), new Vector3(1f, 1f, 0.04f), Vector3.forward, new Color(0.05f, 0.65f, 0.18f, 1f));
            CreateFace(CubeFace.Back, "B", new Vector3(0f, 0f, -0.52f), new Vector3(1f, 1f, 0.04f), Vector3.back, new Color(0.05f, 0.22f, 0.95f, 1f));
            CreateFace(CubeFace.Right, "R", new Vector3(0.52f, 0f, 0f), new Vector3(0.04f, 1f, 1f), Vector3.right, Color.red);
            CreateFace(CubeFace.Left, "L", new Vector3(-0.52f, 0f, 0f), new Vector3(0.04f, 1f, 1f), Vector3.left, new Color(1f, 0.45f, 0f, 1f));
            CreateEdgeFrames();

            GameObject core = RuntimePrimitiveFactory.CreateCube("GuideCore");
            core.name = "GuideCubeCore";
            core.transform.SetParent(cubeRoot, false);
            core.transform.localScale = Vector3.one * 0.84f;
            Renderer coreRenderer = core.GetComponent<Renderer>();
            coreRenderer.material = CreateMaterial(new Color(0.03f, 0.035f, 0.04f, 1f));

            GameObject cameraObject = new GameObject("SolverFaceGuideCamera", typeof(Camera));
            cameraObject.transform.SetParent(sceneRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(2.1f, 1.55f, -3.45f);
            cameraObject.transform.LookAt(sceneRoot.transform.position);
            guideCamera = cameraObject.GetComponent<Camera>();
            guideCamera.clearFlags = CameraClearFlags.SolidColor;
            guideCamera.backgroundColor = VisualCustomizationService.LoadSelectedTheme().backgroundColor;
            guideCamera.orthographic = true;
            guideCamera.orthographicSize = 1.55f;
            guideCamera.nearClipPlane = 0.01f;
            guideCamera.farClipPlane = 20f;
            guideCamera.targetTexture = renderTexture;
            guideViewDirection = cameraObject.transform.localPosition.normalized;
            guideUpDirection = cameraObject.transform.up;
            guideRightDirection = cameraObject.transform.right;

            Light light = new GameObject("SolverFaceGuideLight", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(sceneRoot.transform, false);
            light.transform.localPosition = new Vector3(1.5f, 2.5f, -2f);
            light.type = LightType.Directional;
            light.intensity = 1.15f;
        }

        private void CreateFace(CubeFace face, string label, Vector3 position, Vector3 scale, Vector3 normal, Color color)
        {
            GameObject faceObject = RuntimePrimitiveFactory.CreateCube($"GuideFace_{face}");
            faceObject.name = $"GuideFace_{label}";
            faceObject.transform.SetParent(cubeRoot, false);
            faceObject.transform.localPosition = position;
            faceObject.transform.localScale = scale;
            Renderer renderer = faceObject.GetComponent<Renderer>();
            renderer.material = CreateMaterial(color);
            guideFaces[face] = new GuideFace(faceObject.transform, renderer, color, position, scale, normal);
        }

        private void EnsureSelectedFaceLabel()
        {
            if (selectedFaceMesh != null || cubeRoot == null)
            {
                return;
            }

            GameObject labelObject = new GameObject("GuideSelectedFaceLabel", typeof(TextMesh));
            labelObject.transform.SetParent(cubeRoot, false);
            selectedFaceMesh = labelObject.GetComponent<TextMesh>();
            selectedFaceMesh.anchor = TextAnchor.MiddleCenter;
            selectedFaceMesh.alignment = TextAlignment.Center;
            selectedFaceMesh.fontSize = 54;
            selectedFaceMesh.characterSize = 0.12f;
            selectedFaceMesh.color = Color.black;
        }

        private void CreateEdgeFrames()
        {
            const float extent = 0.58f;
            const float thickness = 0.035f;
            Color edgeColor = new Color(0.005f, 0.006f, 0.008f, 1f);

            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    CreateEdge(new Vector3(0f, y * extent, z * extent), new Vector3(1.22f, thickness, thickness), edgeColor);
                }
            }

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    CreateEdge(new Vector3(x * extent, 0f, z * extent), new Vector3(thickness, 1.22f, thickness), edgeColor);
                }
            }

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    CreateEdge(new Vector3(x * extent, y * extent, 0f), new Vector3(thickness, thickness, 1.22f), edgeColor);
                }
            }
        }

        private void CreateEdge(Vector3 position, Vector3 scale, Color color)
        {
            GameObject edge = RuntimePrimitiveFactory.CreateCube("GuideEdge");
            edge.name = "GuideCubeEdge";
            edge.transform.SetParent(cubeRoot, false);
            edge.transform.localPosition = position;
            edge.transform.localScale = scale;
            Renderer renderer = edge.GetComponent<Renderer>();
            renderer.material = CreateMaterial(color);
        }

        private void CreateLegend(RectTransform root)
        {
            legendLabels = new Text[6];
            string[] labels =
            {
                "U Up", "R Right", "F Front", "D Down", "L Left", "B Back"
            };

            for (int i = 0; i < legendLabels.Length; i++)
            {
                GameObject labelRoot = new GameObject($"Legend{i}", typeof(RectTransform), typeof(Image));
                labelRoot.transform.SetParent(root, false);
                RectTransform rect = labelRoot.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(6f + ((i % 3) * 49f), 6f + ((1 - (i / 3)) * 18f));
                rect.sizeDelta = new Vector2(46f, 16f);
                labelRoot.GetComponent<Image>().color = new Color(0.11f, 0.14f, 0.17f, 0.92f);
                legendLabels[i] = RuntimeUiFactory.CreateText(rect, "Text", labels[i], 8, TextAnchor.MiddleCenter);
            }
        }

        private void UpdateLegend(CubeFace face)
        {
            if (legendLabels == null)
            {
                return;
            }

            for (int i = 0; i < legendLabels.Length; i++)
            {
                CubeFace legendFace = FaceFromLegendIndex(i);
                Text label = legendLabels[i];
                if (label == null)
                {
                    continue;
                }

                label.color = legendFace == face ? Color.yellow : Color.white;
                Image image = label.GetComponentInParent<Image>();
                if (image != null)
                {
                    image.color = legendFace == face
                        ? new Color(0.26f, 0.22f, 0.08f, 1f)
                        : new Color(0.11f, 0.14f, 0.17f, 0.92f);
                }
            }
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Resources.Load<Shader>("RuntimeColor");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            SetMaterialColor(material, color);
            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private Quaternion GetRotationForFace(CubeFace face)
        {
            Vector3 faceNormal = GetFaceNormal(face);
            // Keep a small angled view so adjacent faces are visible. The right offset is
            // intentionally negative so F shows L on screen-left and R on screen-right.
            Vector3 readableTargetNormal = (guideViewDirection - (guideRightDirection * 0.18f) + (guideUpDirection * 0.12f)).normalized;
            Quaternion alignSelectedFaceToCamera = Quaternion.FromToRotation(faceNormal, readableTargetNormal);

            Vector3 referenceNormal = face == CubeFace.Up || face == CubeFace.Down
                ? Vector3.forward
                : Vector3.up;
            Vector3 desiredReference = ProjectOnPlane(
                face == CubeFace.Up || face == CubeFace.Down ? Vector3.forward : guideUpDirection,
                readableTargetNormal);
            Vector3 currentReference = ProjectOnPlane(alignSelectedFaceToCamera * referenceNormal, readableTargetNormal);
            if (desiredReference.sqrMagnitude < 0.001f || currentReference.sqrMagnitude < 0.001f)
            {
                return alignSelectedFaceToCamera;
            }

            Quaternion twist = Quaternion.FromToRotation(currentReference.normalized, desiredReference.normalized);
            return twist * alignSelectedFaceToCamera;
        }

        private static Vector3 ProjectOnPlane(Vector3 value, Vector3 normal)
        {
            return value - (normal * Vector3.Dot(value, normal));
        }

        private static Vector3 GetFaceNormal(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return Vector3.up;
                case CubeFace.Down: return Vector3.down;
                case CubeFace.Front: return Vector3.forward;
                case CubeFace.Back: return Vector3.back;
                case CubeFace.Right: return Vector3.right;
                case CubeFace.Left: return Vector3.left;
                default: return Vector3.up;
            }
        }

        private static CubeFace FaceFromLegendIndex(int index)
        {
            switch (index)
            {
                case 0: return CubeFace.Up;
                case 1: return CubeFace.Right;
                case 2: return CubeFace.Front;
                case 3: return CubeFace.Down;
                case 4: return CubeFace.Left;
                case 5: return CubeFace.Back;
                default: return CubeFace.Up;
            }
        }

        private static string GetFaceCode(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return "U";
                case CubeFace.Right: return "R";
                case CubeFace.Front: return "F";
                case CubeFace.Down: return "D";
                case CubeFace.Left: return "L";
                case CubeFace.Back: return "B";
                default: return "U";
            }
        }

        private static string GetFaceName(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return "Up";
                case CubeFace.Right: return "Right";
                case CubeFace.Front: return "Front";
                case CubeFace.Down: return "Down";
                case CubeFace.Left: return "Left";
                case CubeFace.Back: return "Back";
                default: return "Up";
            }
        }

        private sealed class GuideFace
        {
            public GuideFace(Transform transform, Renderer renderer, Color baseColor, Vector3 basePosition, Vector3 baseScale, Vector3 normal)
            {
                Transform = transform;
                Renderer = renderer;
                BaseColor = baseColor;
                BasePosition = basePosition;
                BaseScale = baseScale;
                Normal = normal;
            }

            public Transform Transform { get; }
            public Renderer Renderer { get; }
            public Color BaseColor { get; }
            public Vector3 BasePosition { get; }
            public Vector3 BaseScale { get; }
            public Vector3 Normal { get; }
        }
    }
}
