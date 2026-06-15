using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using UnityEngine;

namespace CubeChallenge3D.Economy
{
    [Serializable]
    public sealed class CubeSkinData
    {
        public string skinId;
        public string displayName;
        public string description;
        public int priceGems;
        public bool isDefaultUnlocked;
        public string rarity;
        public Color bodyColor;
        public Color whiteColor;
        public Color yellowColor;
        public Color greenColor;
        public Color blueColor;
        public Color redColor;
        public Color orangeColor;
        public string textureResourcePath;
        public float textureScale = 1f;
        public float metallic;
        public float smoothness = 0.35f;
        public float emissionStrength;
        public float textureVisibility;
        public bool useTextureEmission;
        public string stickerTextureRoot;

        public Color GetColor(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.White: return whiteColor;
                case CubeColor.Yellow: return yellowColor;
                case CubeColor.Green: return greenColor;
                case CubeColor.Blue: return blueColor;
                case CubeColor.Red: return redColor;
                case CubeColor.Orange: return orangeColor;
                default: return Color.magenta;
            }
        }

        public IReadOnlyList<Color> GetColorChips()
        {
            return new[] { whiteColor, yellowColor, redColor, orangeColor, blueColor, greenColor };
        }

        public string GetStickerTexturePath(CubeColor color)
        {
            if (string.IsNullOrWhiteSpace(stickerTextureRoot))
            {
                return textureResourcePath;
            }

            switch (color)
            {
                case CubeColor.White: return $"{stickerTextureRoot}/u";
                case CubeColor.Red: return $"{stickerTextureRoot}/r";
                case CubeColor.Green: return $"{stickerTextureRoot}/f";
                case CubeColor.Yellow: return $"{stickerTextureRoot}/d";
                case CubeColor.Orange: return $"{stickerTextureRoot}/l";
                case CubeColor.Blue: return $"{stickerTextureRoot}/b";
                default: return null;
            }
        }
    }
}
