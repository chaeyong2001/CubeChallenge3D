using System;
using UnityEngine;

namespace CubeChallenge3D.Economy
{
    [Serializable]
    public sealed class ThemeData
    {
        public string themeId;
        public string displayName;
        public string description;
        public int priceGems;
        public bool isDefaultUnlocked;
        public Color backgroundColor;
        public Color panelColor;
    }
}
