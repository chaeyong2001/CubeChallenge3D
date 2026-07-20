using UnityEngine;

namespace CubeChallenge3D.UI.Style
{
    public enum CasualUIColor
    {
        Blue,
        Purple,
        Green,
        Orange,
        Pink,
        Teal,
        Slate,
        Cream
    }

    public static class CasualUIPalette
    {
        public static readonly Color BackgroundTop = new Color(0.075f, 0.07f, 0.15f, 1f);
        public static readonly Color BackgroundBottom = new Color(0.018f, 0.022f, 0.06f, 1f);
        public static readonly Color Cream = new Color(1f, 0.89f, 0.69f, 1f);
        public static readonly Color Gold = new Color(1f, 0.64f, 0.08f, 1f);

        public static Color Get(CasualUIColor color)
        {
            switch (color)
            {
                case CasualUIColor.Blue: return new Color(0.05f, 0.42f, 0.94f, 1f);
                case CasualUIColor.Purple: return new Color(0.48f, 0.18f, 0.86f, 1f);
                case CasualUIColor.Green: return new Color(0.28f, 0.67f, 0.08f, 1f);
                case CasualUIColor.Orange: return new Color(1f, 0.39f, 0.015f, 1f);
                case CasualUIColor.Pink: return new Color(0.9f, 0.08f, 0.36f, 1f);
                case CasualUIColor.Teal: return new Color(0.02f, 0.62f, 0.67f, 1f);
                case CasualUIColor.Slate: return new Color(0.2f, 0.32f, 0.5f, 1f);
                case CasualUIColor.Cream: return Cream;
                default: return Color.white;
            }
        }
    }
}
