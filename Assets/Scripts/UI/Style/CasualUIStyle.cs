using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Style
{
    public static class CasualUIStyle
    {
        private static readonly Dictionary<int, Sprite> RoundedSprites = new Dictionary<int, Sprite>();
        private static Sprite gradientSprite;

        public static Sprite GetRoundedSprite(int radius = 22)
        {
            if (RoundedSprites.TryGetValue(radius, out Sprite cached))
            {
                return cached;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"RuntimeRounded_{radius}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            float r = Mathf.Clamp(radius, 4, 30);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(r - x, x - (size - 1 - r), 0f);
                    float dy = Mathf.Max(r - y, y - (size - 1 - r), 0f);
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(r + 0.5f - distance));
                    pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            sprite.name = texture.name;
            RoundedSprites[radius] = sprite;
            return sprite;
        }

        public static Sprite GetBackgroundGradient()
        {
            if (gradientSprite != null)
            {
                return gradientSprite;
            }

            const int width = 8;
            const int height = 256;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "RuntimeCasualBackground",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                Color color = Color.Lerp(CasualUIPalette.BackgroundBottom, CasualUIPalette.BackgroundTop, t);
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            gradientSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f);
            return gradientSprite;
        }

        public static void ApplyBackground(Image image)
        {
            image.sprite = GetBackgroundGradient();
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }

        public static void ApplyPanel(Image image, Color color, int radius = 22)
        {
            image.sprite = GetRoundedSprite(radius);
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        public static void ApplyTextDepth(Text text, bool strong)
        {
            Shadow shadow = text.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, strong ? 0.72f : 0.48f);
            shadow.effectDistance = strong ? new Vector2(2f, -3f) : new Vector2(1f, -2f);

            if (!strong)
            {
                return;
            }

            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.035f, 0.14f, 0.42f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        public static void ApplyButton(Button button, CasualUIColor theme)
        {
            Image image = button.GetComponent<Image>();
            ApplyPanel(image, CasualUIPalette.Get(theme), 26);

            Shadow shadow = button.GetComponent<Shadow>() ?? button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            shadow.effectDistance = new Vector2(0f, -10f);

            Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.74f, 0.34f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            Color baseColor = CasualUIPalette.Get(theme);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, baseColor, 0.7f);
            colors.pressedColor = Color.Lerp(Color.black, baseColor, 0.72f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.44f, 0.5f, 0.58f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (button.GetComponent<CasualUIButton>() == null)
            {
                button.gameObject.AddComponent<CasualUIButton>();
            }

            if (button.transform.Find("Depth") == null)
            {
                GameObject depth = new GameObject("Depth", typeof(RectTransform), typeof(Image));
                depth.transform.SetParent(button.transform, false);
                RectTransform depthRect = depth.GetComponent<RectTransform>();
                depthRect.anchorMin = new Vector2(0.015f, 0f);
                depthRect.anchorMax = new Vector2(0.985f, 0.18f);
                depthRect.offsetMin = Vector2.zero;
                depthRect.offsetMax = Vector2.zero;
                ApplyPanel(depth.GetComponent<Image>(), new Color(0.08f, 0.035f, 0.13f, 0.55f), 20);
                depth.GetComponent<Image>().raycastTarget = false;
                depth.transform.SetAsFirstSibling();
            }

            if (button.transform.Find("InnerStroke") == null)
            {
                GameObject stroke = new GameObject("InnerStroke", typeof(RectTransform), typeof(Image), typeof(Outline));
                stroke.transform.SetParent(button.transform, false);
                RectTransform strokeRect = stroke.GetComponent<RectTransform>();
                strokeRect.anchorMin = new Vector2(0.018f, 0.05f);
                strokeRect.anchorMax = new Vector2(0.982f, 0.97f);
                strokeRect.offsetMin = Vector2.zero;
                strokeRect.offsetMax = Vector2.zero;
                Image strokeImage = stroke.GetComponent<Image>();
                ApplyPanel(strokeImage, new Color(1f, 1f, 1f, 0.025f), 24);
                Outline strokeOutline = stroke.GetComponent<Outline>();
                strokeOutline.effectColor = new Color(1f, 1f, 1f, 0.28f);
                strokeOutline.effectDistance = new Vector2(2f, -2f);
                strokeImage.raycastTarget = false;
            }

            if (button.transform.Find("Gloss") == null)
            {
                GameObject gloss = new GameObject("Gloss", typeof(RectTransform), typeof(Image));
                gloss.transform.SetParent(button.transform, false);
                RectTransform rect = gloss.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.045f, 0.58f);
                rect.anchorMax = new Vector2(0.955f, 0.94f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Image glossImage = gloss.GetComponent<Image>();
                ApplyPanel(glossImage, new Color(1f, 1f, 1f, 0.21f), 20);
                glossImage.raycastTarget = false;
                gloss.transform.SetAsFirstSibling();
            }
        }
    }
}
