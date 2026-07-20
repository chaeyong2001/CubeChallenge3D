#if UNITY_EDITOR
using UnityEditor;

namespace CubeChallenge3D.Editor
{
    public sealed class CubeChallengeUIAssetImporter : AssetPostprocessor
    {
        private const string SpriteRoot = "Assets/Resources/UI/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteRoot, System.StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
        }
    }
}
#endif
