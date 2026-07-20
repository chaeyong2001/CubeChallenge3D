using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Economy;
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
        private CubeSkinData activeSkin;
        private CubeSkinData previewSkinOverride;
        private Transform viewRoot;
        private Transform cubeRoot;

        public Transform ViewRoot => viewRoot;
        public Transform CubeRoot => cubeRoot;
        public float CubieSpacing => cubieSize + gap;

        public void SetPreviewSkin(CubeSkinData skin)
        {
            previewSkinOverride = skin;
            activeSkin = null;
        }

        public void Build(CubeState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            EnsureVisualStyle();
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
            GameObject cubie = RuntimePrimitiveFactory.CreateCube(
                $"Cubie_{gridPosition.x}_{gridPosition.y}_{gridPosition.z}");
            cubie.transform.SetParent(cubeRoot, false);

            float spacing = cubieSize + gap;
            cubie.transform.localPosition = (Vector3)gridPosition * spacing;
            cubie.transform.localScale = Vector3.one * cubieSize;
            cubie.GetComponent<MeshRenderer>().sharedMaterial = GetBodyMaterial();
            cubie.AddComponent<BoxCollider>();
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

            GameObject sticker = RuntimePrimitiveFactory.CreateQuad($"Sticker_{face}");
            sticker.transform.SetParent(cubie, false);
            sticker.transform.localPosition = CubeFaceletMapping.FaceNormal(face) * (0.5f + GetEffectiveStickerOffset());
            sticker.transform.localRotation = CubeFaceletMapping.StickerRotation(face);
            sticker.transform.localScale = Vector3.one * GetEffectiveStickerSize();

            CubeColor color = state.GetColor(face, row, col);
            sticker.GetComponent<MeshRenderer>().sharedMaterial = GetColorMaterial(color);
            sticker.AddComponent<StickerVisual>().Initialize(face, row, col);

        }

        private Material GetBodyMaterial()
        {
            if (bodyMaterial != null)
            {
                return bodyMaterial;
            }

            if (runtimeBodyMaterial == null)
            {
                runtimeBodyMaterial = CreateRuntimeMaterial(
                    activeSkin != null ? activeSkin.bodyColor : new Color(0.025f, 0.03f, 0.035f),
                    activeSkin,
                    CubeColor.None);
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
                material = CreateRuntimeMaterial(
                    activeSkin != null ? activeSkin.GetColor(color) : GetDisplayColor(color),
                    activeSkin,
                    color);
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

        private float GetEffectiveStickerSize()
        {
            float multiplier = activeSkin != null
                ? Mathf.Max(0.75f, activeSkin.stickerSizeMultiplier)
                : 1f;
            return Mathf.Min(0.96f, stickerSize * multiplier);
        }

        private float GetEffectiveStickerOffset()
        {
            float extraOffset = activeSkin != null
                ? Mathf.Max(0f, activeSkin.stickerOffsetAdd)
                : 0f;
            return stickerOffset + extraOffset;
        }

        private static Material CreateRuntimeMaterial(Color color, CubeSkinData skin, CubeColor stickerColor)
        {
            Shader shader = Resources.Load<Shader>("RuntimeColor");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader);
            Color materialColor = skin != null && stickerColor != CubeColor.None
                ? Color.Lerp(color, Color.white, Mathf.Clamp01(skin.textureVisibility))
                : color;
            material.color = materialColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", materialColor);
            }
            if (stickerColor != CubeColor.None && material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

            if (skin == null)
            {
                return material;
            }

            string texturePath = stickerColor == CubeColor.None
                ? skin.textureResourcePath
                : skin.GetStickerTexturePath(stickerColor);
            if (!string.IsNullOrWhiteSpace(texturePath))
            {
                Texture2D texture = Resources.Load<Texture2D>(texturePath);
                if (texture != null)
                {
                    texture.wrapMode = string.IsNullOrWhiteSpace(skin.stickerTextureRoot)
                        ? TextureWrapMode.Repeat
                        : TextureWrapMode.Clamp;
                    material.mainTexture = texture;
                    material.mainTextureScale = Vector2.one * Mathf.Max(0.1f, skin.textureScale);
                    material.mainTextureOffset = Vector2.zero;
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", texture);
                        material.SetTextureScale("_BaseMap", Vector2.one * Mathf.Max(0.1f, skin.textureScale));
                        material.SetTextureOffset("_BaseMap", Vector2.zero);
                    }

                    if (skin.useTextureEmission && material.HasProperty("_EmissionMap"))
                    {
                        material.SetTexture("_EmissionMap", texture);
                    }
                }
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", Mathf.Clamp01(skin.metallic));
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(skin.smoothness));
            }
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", Mathf.Clamp01(skin.smoothness));
            }
            if (material.HasProperty("_SpecColor"))
            {
                Color specular = Color.Lerp(Color.white, color, stickerColor == CubeColor.None ? 0.2f : 0.45f);
                material.SetColor("_SpecColor", specular);
            }
            if (skin.emissionStrength > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                Color emissionColor = skin.useTextureEmission
                    ? Color.white * skin.emissionStrength
                    : color * skin.emissionStrength;
                material.SetColor("_EmissionColor", emissionColor);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
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

        private void EnsureVisualStyle()
        {
            CubeSkinData selected = previewSkinOverride ?? VisualCustomizationService.LoadSelectedSkin();
            if (activeSkin != null && selected != null && activeSkin.skinId == selected.skinId)
            {
                return;
            }

            DestroyRuntimeMaterial(runtimeBodyMaterial);
            runtimeBodyMaterial = null;
            foreach (Material material in runtimeMaterials.Values)
            {
                DestroyRuntimeMaterial(material);
            }
            runtimeMaterials.Clear();
            activeSkin = selected;
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
