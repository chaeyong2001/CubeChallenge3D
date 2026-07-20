#if UNITY_EDITOR
using System.IO;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.View;
using CubeChallenge3D.Economy;
using UnityEditor;
using UnityEngine;

namespace CubeChallenge3D.Editor
{
    public static class SkinRenderExporter
    {
        private const string OutputDirectory = "_preview_only/skin_renders";
        private const int DefaultResolution = 2048;
        private static readonly Vector3 RenderViewEuler = Vector3.zero;
        private static readonly Vector3 RenderCameraPosition = new Vector3(5.15f, 4.35f, -5.85f);
        private static readonly Vector3 RenderCameraLookAtOffset = new Vector3(0f, 0.02f, 0f);
        private const float RenderCameraFieldOfView = 30f;

        [MenuItem("Tools/CubeChallenge3D/Export Skin Renders/All Skins 2048 PNG")]
        public static void ExportAllSkins2048()
        {
            ExportAllSkins(DefaultResolution);
        }

        [MenuItem("Tools/CubeChallenge3D/Export Skin Renders/All Skins 1024 PNG")]
        public static void ExportAllSkins1024()
        {
            ExportAllSkins(1024);
        }

        private static void ExportAllSkins(int resolution)
        {
            Directory.CreateDirectory(OutputDirectory);

            int count = 0;
            foreach (CubeSkinData skin in VisualCustomizationCatalog.GetSkins())
            {
                string fileName = $"{SanitizeFileName(skin.skinId)}_{resolution}.png";
                string outputPath = Path.Combine(OutputDirectory, fileName);
                ExportSkin(skin, resolution, outputPath);
                count++;
            }

            AssetDatabase.Refresh();
            string fullOutputPath = Path.GetFullPath(OutputDirectory);
            Debug.Log($"[SkinRenderExporter] Exported {count} skin renders to {fullOutputPath}");
            EditorUtility.RevealInFinder(fullOutputPath);
        }

        private static void ExportSkin(CubeSkinData skin, int resolution, string outputPath)
        {
            GameObject sceneRoot = new GameObject($"SkinRender_{skin.skinId}");
            sceneRoot.hideFlags = HideFlags.HideAndDontSave;

            RenderTexture renderTexture = null;
            Texture2D outputTexture = null;
            Camera renderCamera = null;

            try
            {
                CubeVisualBuilder builder = sceneRoot.AddComponent<CubeVisualBuilder>();
                builder.SetPreviewSkin(skin);
                builder.Build(CubeState.CreateSolved());
                if (builder.ViewRoot != null)
                {
                    builder.ViewRoot.localRotation = Quaternion.Euler(RenderViewEuler);
                }

                AddLighting(sceneRoot.transform);
                renderCamera = CreateCamera(sceneRoot.transform, resolution);
                renderTexture = CreateRenderTexture(resolution);
                renderCamera.targetTexture = renderTexture;

                RenderTexture previous = RenderTexture.active;
                try
                {
                    renderCamera.Render();
                    RenderTexture.active = renderTexture;

                    outputTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                    outputTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                    outputTexture.Apply(false, false);
                }
                finally
                {
                    RenderTexture.active = previous;
                }

                File.WriteAllBytes(outputPath, outputTexture.EncodeToPNG());
                Debug.Log($"[SkinRenderExporter] {skin.displayName} -> {outputPath}");
            }
            finally
            {
                if (renderCamera != null)
                {
                    renderCamera.targetTexture = null;
                }
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
                if (outputTexture != null)
                {
                    Object.DestroyImmediate(outputTexture);
                }
                Object.DestroyImmediate(sceneRoot);
            }
        }

        private static Camera CreateCamera(Transform parent, int resolution)
        {
            GameObject cameraObject = new GameObject("SkinRenderCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.transform.SetParent(parent, false);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.transform.localPosition = RenderCameraPosition;
            camera.fieldOfView = RenderCameraFieldOfView;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.aspect = 1f;
            camera.transform.LookAt(parent.position + RenderCameraLookAtOffset);
            return camera;
        }

        private static RenderTexture CreateRenderTexture(int resolution)
        {
            RenderTexture renderTexture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
            {
                name = $"SkinRender_{resolution}",
                antiAliasing = 8,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            return renderTexture;
        }

        private static void AddLighting(Transform parent)
        {
            CreateDirectionalLight(parent, "KeyLight", 2.55f, new Color(1f, 0.94f, 0.84f), new Vector3(42f, -34f, 0f));
            CreateDirectionalLight(parent, "FillLight", 0.85f, new Color(0.56f, 0.72f, 1f), new Vector3(20f, 145f, 0f));
            CreateDirectionalLight(parent, "RimLight", 1.25f, new Color(1f, 0.72f, 0.38f), new Vector3(58f, 132f, 0f));
        }

        private static void CreateDirectionalLight(Transform parent, string name, float intensity, Color color, Vector3 eulerAngles)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.transform.SetParent(parent, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            lightObject.transform.localRotation = Quaternion.Euler(eulerAngles);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }
    }
}
#endif
