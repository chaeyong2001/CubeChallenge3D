using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CubeChallenge3D.Cube.View
{
    public sealed class CubeVisualBuilder : MonoBehaviour
    {
        private const string ViewRootName = "ViewRoot";
        private const string CubeRootName = "CubeRoot";

        [Header("Layout")]
        [SerializeField] private Transform cubeParent;
        [SerializeField] private float cubieSize = 1f;
        [SerializeField] private float gap = 0.04f;
        [SerializeField] private float stickerSize = 0.82f;
        [SerializeField] private float stickerOffset = 0.006f;

        [Header("Optional Material Overrides")]
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Material whiteMaterial;
        [SerializeField] private Material yellowMaterial;
        [SerializeField] private Material greenMaterial;
        [SerializeField] private Material blueMaterial;
        [SerializeField] private Material redMaterial;
        [SerializeField] private Material orangeMaterial;
        [SerializeField] private Material debugMaterial;

        private readonly Dictionary<CubeColor, Material> runtimeMaterials = new Dictionary<CubeColor, Material>();
        private Material runtimeBodyMaterial;
        private Transform viewRoot;
        private Transform cubeRoot;

        public Transform ViewRoot => viewRoot;
        public Transform CubeRoot => cubeRoot;
        public float CubieSpacing => cubieSize + gap;

        public void Build(CubeState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Clear();
            EnsureViewRoot();
            cubeRoot = new GameObject(CubeRootName).transform;
            cubeRoot.SetParent(viewRoot, false);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                        {
                            continue;
                        }

                        CreateCubie(state, new Vector3Int(x, y, z));
                    }
                }
            }
        }

        public void Render(CubeState state)
        {
            Build(state);
        }

        public void Clear()
        {
            Transform parent = cubeParent != null ? cubeParent : transform;
            EnsureViewRoot();
            Transform existingRoot = viewRoot.Find(CubeRootName);
            if (existingRoot == null)
            {
                existingRoot = parent.Find(CubeRootName);
            }

            if (existingRoot == null)
            {
                cubeRoot = null;
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
            }
            else
            {
                DestroyImmediate(existingRoot.gameObject);
            }

            cubeRoot = null;
        }

        private void EnsureViewRoot()
        {
            if (viewRoot != null)
            {
                return;
            }

            Transform parent = cubeParent != null ? cubeParent : transform;
            viewRoot = parent.Find(ViewRootName);
            if (viewRoot == null)
            {
                viewRoot = new GameObject(ViewRootName).transform;
                viewRoot.SetParent(parent, false);
            }
        }

        private void CreateCubie(CubeState state, Vector3Int gridPosition)
        {
            GameObject cubie = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubie.name = $"Cubie_{gridPosition.x}_{gridPosition.y}_{gridPosition.z}";
            cubie.transform.SetParent(cubeRoot, false);

            float spacing = cubieSize + gap;
            cubie.transform.localPosition = (Vector3)gridPosition * spacing;
            cubie.transform.localScale = Vector3.one * cubieSize;
            cubie.GetComponent<MeshRenderer>().sharedMaterial = GetBodyMaterial();
            cubie.AddComponent<CubieVisual>().Initialize(gridPosition);

            if (gridPosition.y == 1) CreateSticker(state, cubie.transform, gridPosition, CubeFace.Up);
            if (gridPosition.y == -1) CreateSticker(state, cubie.transform, gridPosition, CubeFace.Down);
            if (gridPosition.z == 1) CreateSticker(state, cubie.transform, gridPosition, CubeFace.Front);
            if (gridPosition.z == -1) CreateSticker(state, cubie.transform, gridPosition, CubeFace.Back);
            if (gridPosition.x == 1) CreateSticker(state, cubie.transform, gridPosition, CubeFace.Right);
            if (gridPosition.x == -1) CreateSticker(state, cubie.transform, gridPosition, CubeFace.Left);
        }

        private void CreateSticker(CubeState state, Transform cubie, Vector3Int gridPosition, CubeFace face)
        {
            CubeFaceletMapping.GridPositionToFacelet(face, gridPosition, out int row, out int col);

            GameObject sticker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sticker.name = $"Sticker_{face}";
            sticker.transform.SetParent(cubie, false);
            sticker.transform.localPosition = CubeFaceletMapping.FaceNormal(face) * (0.5f + stickerOffset);
            sticker.transform.localRotation = CubeFaceletMapping.StickerRotation(face);
            sticker.transform.localScale = Vector3.one * stickerSize;

            CubeColor color = state.GetColor(face, row, col);
            sticker.GetComponent<MeshRenderer>().sharedMaterial = GetColorMaterial(color);
            sticker.AddComponent<StickerVisual>().Initialize(face, row, col);

            Collider collider = sticker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private Material GetBodyMaterial()
        {
            if (bodyMaterial != null)
            {
                return bodyMaterial;
            }

            if (runtimeBodyMaterial == null)
            {
                runtimeBodyMaterial = CreateRuntimeMaterial(new Color(0.025f, 0.03f, 0.035f));
                runtimeBodyMaterial.name = "Runtime_CubeBody";
            }

            return runtimeBodyMaterial;
        }

        private Material GetColorMaterial(CubeColor color)
        {
            Material overrideMaterial = GetOverrideMaterial(color);
            if (overrideMaterial != null)
            {
                return overrideMaterial;
            }

            if (!runtimeMaterials.TryGetValue(color, out Material material))
            {
                material = CreateRuntimeMaterial(GetDisplayColor(color));
                material.name = $"Runtime_Sticker_{color}";
                runtimeMaterials[color] = material;
            }

            return material;
        }

        private Material GetOverrideMaterial(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return whiteMaterial;
                case CubeColor.Yellow: return yellowMaterial;
                case CubeColor.Green: return greenMaterial;
                case CubeColor.Blue: return blueMaterial;
                case CubeColor.Red: return redMaterial;
                case CubeColor.Orange: return orangeMaterial;
                default: return debugMaterial;
            }
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private static Color GetDisplayColor(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return new Color(0.95f, 0.95f, 0.95f);
                case CubeColor.Yellow: return new Color(1f, 0.85f, 0.05f);
                case CubeColor.Green: return new Color(0.05f, 0.65f, 0.25f);
                case CubeColor.Blue: return new Color(0.05f, 0.25f, 0.9f);
                case CubeColor.Red: return new Color(0.9f, 0.05f, 0.08f);
                case CubeColor.Orange: return new Color(1f, 0.35f, 0.03f);
                default: return Color.magenta;
            }
        }

        private void OnDestroy()
        {
            DestroyRuntimeMaterial(runtimeBodyMaterial);
            foreach (Material material in runtimeMaterials.Values)
            {
                DestroyRuntimeMaterial(material);
            }

            runtimeMaterials.Clear();
        }

        private static void DestroyRuntimeMaterial(Object material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }
    }
}
