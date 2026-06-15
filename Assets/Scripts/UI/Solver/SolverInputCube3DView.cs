using System;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.View;
using CubeChallenge3D.Solver.Model;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CubeChallenge3D.Economy;

namespace CubeChallenge3D.UI.Solver
{
    public sealed class SolverInputCube3DView : MonoBehaviour, IPointerClickHandler
    {
        private const float FaceOffset = 1.02f;
        private const float StickerSize = 0.56f;
        private const float StickerGap = 0.06f;
        private const float RotationDegreesPerSecond = 300f;

        private readonly StickerView[] stickers = new StickerView[SolverInputState.FaceletCount];

        private RawImage previewImage;
        private RenderTexture renderTexture;
        private Camera renderCamera;
        private GameObject sceneRoot;
        private Transform cubeRoot;
        private SolverInputState state;
        private Func<CubeColor> selectedColorProvider;
        private Action<int> faceChanged;
        private Action inputChanged;
        private Quaternion targetRotation = Quaternion.identity;
        private bool rotating;
        private int currentLogicalFaceIndex = 2;
        private int pendingLogicalFaceIndex = 2;
        private int currentVisibleFaceIndex = 2;
        private int pendingVisibleFaceIndex = 2;

        public int CurrentFaceIndex => currentLogicalFaceIndex;
        public bool IsRotating => rotating;

        public void Initialize(
            RectTransform parent,
            SolverInputState inputState,
            Func<CubeColor> getSelectedColor,
            Action<int> onFaceChanged,
            Action onInputChanged)
        {
            state = inputState;
            selectedColorProvider = getSelectedColor;
            faceChanged = onFaceChanged;
            inputChanged = onInputChanged;

            transform.SetParent(parent, false);
            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -250f);
            root.sizeDelta = new Vector2(320f, 320f);

            previewImage = gameObject.AddComponent<RawImage>();
            previewImage.color = Color.white;
            previewImage.raycastTarget = true;

            BuildScene();
            targetRotation = Quaternion.identity;
            if (cubeRoot != null)
            {
                cubeRoot.localRotation = targetRotation;
            }

            currentLogicalFaceIndex = 2;
            pendingLogicalFaceIndex = 2;
            currentVisibleFaceIndex = 2;
            pendingVisibleFaceIndex = 2;
            rotating = false;
            RefreshColors();
        }

        private void Update()
        {
            if (cubeRoot == null)
            {
                return;
            }

            cubeRoot.localRotation = Quaternion.RotateTowards(
                cubeRoot.localRotation,
                targetRotation,
                RotationDegreesPerSecond * Time.unscaledDeltaTime);
            if (rotating && Quaternion.Angle(cubeRoot.localRotation, targetRotation) < 0.25f)
            {
                cubeRoot.localRotation = targetRotation;
                rotating = false;
                currentLogicalFaceIndex = pendingLogicalFaceIndex;
                currentVisibleFaceIndex = pendingVisibleFaceIndex;
                faceChanged?.Invoke(currentLogicalFaceIndex);
            }
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

        public void SetState(SolverInputState inputState)
        {
            state = inputState;
            RefreshColors();
        }

        public void ResetToFront()
        {
            targetRotation = Quaternion.identity;
            if (cubeRoot != null)
            {
                cubeRoot.localRotation = targetRotation;
            }

            currentLogicalFaceIndex = 2;
            pendingLogicalFaceIndex = 2;
            currentVisibleFaceIndex = 2;
            pendingVisibleFaceIndex = 2;
            rotating = false;
            faceChanged?.Invoke(currentLogicalFaceIndex);
        }

        public void RotateToFace(int faceIndex)
        {
            RotateToFace(faceIndex, faceIndex);
        }

        public void RotateToFace(int logicalFaceIndex, int visibleFaceIndex)
        {
            if (rotating || cubeRoot == null)
            {
                return;
            }

            int safeLogicalFaceIndex = Mathf.Clamp(logicalFaceIndex, 0, 5);
            int safeVisibleFaceIndex = Mathf.Clamp(visibleFaceIndex, 0, 5);
            pendingLogicalFaceIndex = safeLogicalFaceIndex;
            pendingVisibleFaceIndex = safeVisibleFaceIndex;
            targetRotation = GetTargetRotationForFace(safeVisibleFaceIndex);
            rotating = true;
        }

        public void RotateLeft()
        {
            RotateBy(Quaternion.Euler(0f, 90f, 0f));
        }

        public void RotateRight()
        {
            RotateBy(Quaternion.Euler(0f, -90f, 0f));
        }

        public void RotateUp()
        {
            RotateBy(Quaternion.Euler(90f, 0f, 0f));
        }

        public void RotateDown()
        {
            RotateBy(Quaternion.Euler(-90f, 0f, 0f));
        }

        public void RotateByScreenQuarterTurn(int faceIndex, Vector3 eulerDegrees)
        {
            RotateByScreenQuarterTurn(faceIndex, faceIndex, eulerDegrees);
        }

        public void RotateByScreenQuarterTurn(int logicalFaceIndex, int visibleFaceIndex, Vector3 eulerDegrees)
        {
            if (rotating || cubeRoot == null)
            {
                return;
            }

            pendingLogicalFaceIndex = Mathf.Clamp(logicalFaceIndex, 0, 5);
            pendingVisibleFaceIndex = Mathf.Clamp(visibleFaceIndex, 0, 5);
            targetRotation = Quaternion.Euler(eulerDegrees) * targetRotation;
            rotating = true;
        }

        public void RefreshColors()
        {
            if (state == null)
            {
                return;
            }

            state.EnsureShape();
            for (int i = 0; i < stickers.Length; i++)
            {
                StickerView sticker = stickers[i];
                if (sticker == null || sticker.Renderer == null)
                {
                    continue;
                }

                int visualFaceIndex = i / SolverInputState.FaceletPerFace;
                int cellIndex = i % SolverInputState.FaceletPerFace;
                int logicalFaceIndex = VisualToLogicalFaceIndex(visualFaceIndex);
                int logicalCellIndex = MapVisualCellToLogicalCell(visualFaceIndex, logicalFaceIndex, cellIndex);
                int logicalIndex = (logicalFaceIndex * SolverInputState.FaceletPerFace) + logicalCellIndex;
                CubeColor color = (CubeColor)state.faceletColorIndexes[logicalIndex];
                SetMaterialColor(sticker.Renderer.material, ToUnityColor(color));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (rotating || renderCamera == null || previewImage == null || state == null)
            {
                return;
            }

            RectTransform rect = previewImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Rect imageRect = rect.rect;
            float u = Mathf.InverseLerp(imageRect.xMin, imageRect.xMax, localPoint.x);
            float v = Mathf.InverseLerp(imageRect.yMin, imageRect.yMax, localPoint.y);
            Ray ray = renderCamera.ViewportPointToRay(new Vector3(u, v, 0f));
            if (!TryGetCurrentFaceStickerHit(ray, out SolverInputCube3DStickerHit stickerHit))
            {
                return;
            }

            int logicalCellIndex = MapVisualCellToLogicalCell(currentVisibleFaceIndex, currentLogicalFaceIndex, stickerHit.CellIndex);
            int index = (currentLogicalFaceIndex * SolverInputState.FaceletPerFace) + logicalCellIndex;
            state.faceletColorIndexes[index] = (int)selectedColorProvider();
            RefreshColors();
            inputChanged?.Invoke();
        }

        private bool TryGetCurrentFaceStickerHit(Ray ray, out SolverInputCube3DStickerHit stickerHit)
        {
            stickerHit = null;
            RaycastHit[] hits = Physics.RaycastAll(ray, 20f);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                SolverInputCube3DStickerHit candidate = hits[i].collider.GetComponent<SolverInputCube3DStickerHit>();
                if (candidate != null && candidate.FaceIndex == currentVisibleFaceIndex)
                {
                    stickerHit = candidate;
                    return true;
                }
            }

            return false;
        }

        private void RotateBy(Quaternion delta)
        {
            if (rotating || cubeRoot == null)
            {
                return;
            }

            targetRotation = delta * targetRotation;
            pendingLogicalFaceIndex = GetFaceIndexFacingCamera(targetRotation);
            pendingVisibleFaceIndex = pendingLogicalFaceIndex;
            rotating = true;
        }

        private int GetFaceIndexFacingCamera(Quaternion rotation)
        {
            Vector3 worldForward = rotation * Vector3.forward;
            Vector3 worldRight = rotation * Vector3.right;
            Vector3 worldUp = rotation * Vector3.up;

            Vector3 cameraDirection = Vector3.forward;
            float best = -999f;
            int bestIndex = 2;
            TryPickFace(worldForward, 2, cameraDirection, ref best, ref bestIndex);
            TryPickFace(-worldForward, 5, cameraDirection, ref best, ref bestIndex);
            TryPickFace(worldRight, 1, cameraDirection, ref best, ref bestIndex);
            TryPickFace(-worldRight, 4, cameraDirection, ref best, ref bestIndex);
            TryPickFace(worldUp, 0, cameraDirection, ref best, ref bestIndex);
            TryPickFace(-worldUp, 3, cameraDirection, ref best, ref bestIndex);
            return bestIndex;
        }

        private static Quaternion GetTargetRotationForFace(int faceIndex)
        {
            switch (faceIndex)
            {
                case 0: return Quaternion.Euler(90f, 0f, 0f);
                case 1: return Quaternion.Euler(0f, -90f, 0f);
                case 2: return Quaternion.identity;
                case 3: return Quaternion.Euler(-90f, 0f, 0f);
                case 4: return Quaternion.Euler(0f, 90f, 0f);
                case 5: return Quaternion.Euler(0f, 180f, 0f);
                default: return Quaternion.identity;
            }
        }

        private static int VisualToLogicalFaceIndex(int visualFaceIndex)
        {
            switch (visualFaceIndex)
            {
                case 0: return 0; // U remains U.
                case 1: return 4; // The verified first side view is visually entered as L.
                case 2: return 2; // F remains F.
                case 3: return 3; // D remains D.
                case 4: return 1; // Later side view is logically R.
                case 5: return 5; // Back-facing verified view is logically B.
                default: return visualFaceIndex;
            }
        }

        private static int MapVisualCellToLogicalCell(int visualFaceIndex, int logicalFaceIndex, int visualCellIndex)
        {
            if ((visualFaceIndex == 1 && logicalFaceIndex == 4)
                || (visualFaceIndex == 4 && logicalFaceIndex == 1))
            {
                int row = visualCellIndex / 3;
                int col = visualCellIndex % 3;
                return (row * 3) + (2 - col);
            }

            return visualCellIndex;
        }

        private static void TryPickFace(Vector3 normal, int faceIndex, Vector3 cameraDirection, ref float best, ref int bestIndex)
        {
            float dot = Vector3.Dot(normal, cameraDirection);
            if (dot > best)
            {
                best = dot;
                bestIndex = faceIndex;
            }
        }

        private void BuildScene()
        {
            renderTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            previewImage.texture = renderTexture;

            sceneRoot = new GameObject("SolverInputCube3DScene");
            sceneRoot.transform.position = new Vector3(4200f, 4200f, 4200f);

            cubeRoot = new GameObject("SolverInputCube3DRoot").transform;
            cubeRoot.SetParent(sceneRoot.transform, false);

            CreateFace(0, Vector3.up, Quaternion.Euler(90f, 0f, 0f));
            CreateFace(3, Vector3.down, Quaternion.Euler(-90f, 0f, 0f));
            CreateFace(2, Vector3.forward, Quaternion.identity);
            CreateFace(5, Vector3.back, Quaternion.Euler(0f, 180f, 0f));
            CreateFace(1, Vector3.right, Quaternion.Euler(0f, 90f, 0f));
            CreateFace(4, Vector3.left, Quaternion.Euler(0f, -90f, 0f));

            GameObject core = RuntimePrimitiveFactory.CreateCube("SolverInputCubeCore");
            core.transform.SetParent(cubeRoot, false);
            core.transform.localScale = Vector3.one * 1.82f;
            core.GetComponent<Renderer>().material = CreateMaterial(new Color(0.02f, 0.025f, 0.03f, 1f));

            GameObject cameraObject = new GameObject("SolverInputCubeCamera", typeof(Camera));
            cameraObject.transform.SetParent(sceneRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, 5.2f);
            cameraObject.transform.LookAt(sceneRoot.transform.position);
            renderCamera = cameraObject.GetComponent<Camera>();
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = VisualCustomizationService.LoadSelectedTheme().backgroundColor;
            renderCamera.fieldOfView = 34f;
            renderCamera.nearClipPlane = 0.01f;
            renderCamera.farClipPlane = 30f;
            renderCamera.targetTexture = renderTexture;

            Light light = new GameObject("SolverInputCubeLight", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(sceneRoot.transform, false);
            light.transform.localRotation = Quaternion.Euler(35f, -30f, 0f);
            light.type = LightType.Directional;
            light.intensity = 1.15f;
        }

        private void CreateFace(int faceIndex, Vector3 normal, Quaternion rotation)
        {
            Transform faceRoot = new GameObject($"Face_{faceIndex}").transform;
            faceRoot.SetParent(cubeRoot, false);
            faceRoot.localPosition = normal * FaceOffset;
            faceRoot.localRotation = rotation;

            for (int cell = 0; cell < SolverInputState.FaceletPerFace; cell++)
            {
                int row = cell / 3;
                int col = cell % 3;
                GameObject sticker = RuntimePrimitiveFactory.CreateQuad($"Sticker_{faceIndex}_{cell}");
                sticker.transform.SetParent(faceRoot, false);
                sticker.transform.localPosition = new Vector3(
                    (col - 1) * (StickerSize + StickerGap),
                    (1 - row) * (StickerSize + StickerGap),
                    0.025f);
                sticker.transform.localScale = new Vector3(StickerSize, StickerSize, 1f);
                Renderer renderer = sticker.GetComponent<Renderer>();
                renderer.material = CreateMaterial(Color.white);
                BoxCollider collider = sticker.AddComponent<BoxCollider>();
                collider.size = new Vector3(1f, 1f, 0.08f);
                SolverInputCube3DStickerHit hit = sticker.AddComponent<SolverInputCube3DStickerHit>();
                hit.FaceIndex = faceIndex;
                hit.CellIndex = cell;
                stickers[(faceIndex * SolverInputState.FaceletPerFace) + cell] = new StickerView(renderer);
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

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
        }

        private static Color ToUnityColor(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return Color.white;
                case CubeColor.Yellow: return Color.yellow;
                case CubeColor.Red: return Color.red;
                case CubeColor.Orange: return new Color(1f, 0.45f, 0f, 1f);
                case CubeColor.Blue: return new Color(0.05f, 0.25f, 1f, 1f);
                case CubeColor.Green: return new Color(0.05f, 0.75f, 0.18f, 1f);
                default: return new Color(0.16f, 0.16f, 0.16f, 1f);
            }
        }

        private sealed class StickerView
        {
            public StickerView(Renderer renderer)
            {
                Renderer = renderer;
            }

            public Renderer Renderer { get; }
        }

    }
}
