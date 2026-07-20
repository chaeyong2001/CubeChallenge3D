using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Style
{
    public static class CasualIconFactory
    {
        private static readonly Dictionary<string, Sprite> MainMenuKitSprites =
            new Dictionary<string, Sprite>();

        public static Sprite LoadMainMenuKitSprite(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (MainMenuKitSprites.TryGetValue(relativePath, out Sprite cached))
            {
                return cached;
            }

            string resourcePath = $"UI/MainMenu/{relativePath}";
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    sprite.name = texture.name;
                }
            }

            MainMenuKitSprites[relativePath] = sprite;
            return sprite;
        }

        public static Sprite LoadUiSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            if (MainMenuKitSprites.TryGetValue(resourcePath, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    sprite.name = texture.name;
                }
            }

            MainMenuKitSprites[resourcePath] = sprite;
            return sprite;
        }

        public static bool TryCreateMainMenuKitIcon(Transform parent, string key, out RectTransform root)
        {
            root = null;
            Sprite sprite = LoadMainMenuKitSprite($"Icons/{key}");
            if (sprite == null)
            {
                return false;
            }

            GameObject rootObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            rootObject.transform.SetParent(parent, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(4f, 4f);
            root.offsetMax = new Vector2(-4f, -4f);

            Image image = rootObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return true;
        }

        public static RectTransform Create(Transform parent, string key, Color color)
        {
            GameObject rootObject = new GameObject("Icon", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(8f, 8f);
            root.offsetMax = new Vector2(-8f, -8f);

            switch (key)
            {
                case "heart": CreateHeart(root, color); break;
                case "coin": CreateCoin(root, color); break;
                case "gem": CreateGem(root, color); break;
                case "shop": CreateShop(root, color); break;
                case "stages": CreateCube(root); break;
                case "ranking": CreateTrophy(root, color); break;
                case "solver":
                case "hint": CreateLightbulb(root, color); break;
                case "rewards": CreateGift(root, color); break;
                case "records": CreateRecords(root, color); break;
                case "settings": CreateGear(root, color); break;
                case "undo": CreateUndo(root, color); break;
                case "retry": CreateUndo(root, color); break;
                case "move": CreateMoveBoost(root, color); break;
                case "view": CreateEye(root, color); break;
                case "scramble": CreateShuffle(root, color); break;
                case "start": CreatePlay(root, color); break;
                case "menu": CreateHome(root, color); break;
                case "list": CreateList(root, color); break;
                case "chevron": CreateChevron(root, color); break;
                default: CreateDot(root, color); break;
            }

            return root;
        }

        private static Image Shape(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color,
            int radius = 8,
            float rotation = 0f)
        {
            GameObject shapeObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            shapeObject.transform.SetParent(parent, false);
            RectTransform rect = shapeObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            Image image = shapeObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, radius);
            image.raycastTarget = false;
            return image;
        }

        private static void CreateHeart(RectTransform root, Color color)
        {
            Shape(root, "Left", new Vector2(0.14f, 0.44f), new Vector2(0.58f, 0.88f), color, 28);
            Shape(root, "Right", new Vector2(0.42f, 0.44f), new Vector2(0.86f, 0.88f), color, 28);
            Shape(root, "Point", new Vector2(0.25f, 0.18f), new Vector2(0.75f, 0.68f), color, 8, 45f);
            Shape(root, "Shine", new Vector2(0.25f, 0.64f), new Vector2(0.4f, 0.77f), new Color(1f, 1f, 1f, 0.65f), 12);
        }

        private static void CreateCoin(RectTransform root, Color color)
        {
            Shape(root, "Coin", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), color, 30);
            Shape(root, "Inner", new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f), new Color(1f, 0.78f, 0.08f), 30);
            Shape(root, "MarkV", new Vector2(0.46f, 0.34f), new Vector2(0.54f, 0.66f), new Color(1f, 0.94f, 0.55f), 4);
            Shape(root, "MarkH", new Vector2(0.34f, 0.46f), new Vector2(0.66f, 0.54f), new Color(1f, 0.94f, 0.55f), 4);
            Shape(root, "Shine", new Vector2(0.25f, 0.67f), new Vector2(0.42f, 0.79f), new Color(1f, 1f, 1f, 0.7f), 10);
        }

        private static void CreateGem(RectTransform root, Color color)
        {
            Shape(root, "Gem", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), color, 7, 45f);
            Shape(root, "Facet", new Vector2(0.36f, 0.42f), new Vector2(0.65f, 0.75f), new Color(1f, 0.46f, 1f, 0.65f), 5, 45f);
            Shape(root, "Shine", new Vector2(0.29f, 0.65f), new Vector2(0.43f, 0.78f), new Color(1f, 1f, 1f, 0.72f), 8);
        }

        private static void CreateShop(RectTransform root, Color color)
        {
            Shape(root, "Basket", new Vector2(0.22f, 0.24f), new Vector2(0.83f, 0.62f), color, 8);
            Shape(root, "HandleA", new Vector2(0.16f, 0.66f), new Vector2(0.48f, 0.75f), color, 4, -36f);
            Shape(root, "HandleB", new Vector2(0.52f, 0.66f), new Vector2(0.84f, 0.75f), color, 4, 36f);
            Shape(root, "WheelL", new Vector2(0.28f, 0.09f), new Vector2(0.43f, 0.24f), color, 18);
            Shape(root, "WheelR", new Vector2(0.63f, 0.09f), new Vector2(0.78f, 0.24f), color, 18);
            Shape(root, "Shine", new Vector2(0.3f, 0.48f), new Vector2(0.73f, 0.56f), new Color(1f, 1f, 1f, 0.55f), 4);
        }

        private static void CreateCube(RectTransform root)
        {
            Color gold = new Color(1f, 0.82f, 0.15f);
            Color orange = new Color(1f, 0.35f, 0.05f);
            Color green = new Color(0.22f, 0.85f, 0.2f);
            Shape(root, "A", new Vector2(0.13f, 0.51f), new Vector2(0.42f, 0.8f), gold, 5);
            Shape(root, "B", new Vector2(0.46f, 0.51f), new Vector2(0.75f, 0.8f), orange, 5);
            Shape(root, "C", new Vector2(0.13f, 0.18f), new Vector2(0.42f, 0.47f), green, 5);
            Shape(root, "D", new Vector2(0.46f, 0.18f), new Vector2(0.75f, 0.47f), new Color(0.12f, 0.48f, 1f), 5);
            Shape(root, "Side", new Vector2(0.72f, 0.27f), new Vector2(0.88f, 0.72f), new Color(0.05f, 0.18f, 0.38f), 4, -12f);
        }

        private static void CreateTrophy(RectTransform root, Color color)
        {
            Shape(root, "Cup", new Vector2(0.25f, 0.46f), new Vector2(0.75f, 0.84f), color, 10);
            Shape(root, "LeftHandle", new Vector2(0.1f, 0.5f), new Vector2(0.3f, 0.72f), color, 18);
            Shape(root, "RightHandle", new Vector2(0.7f, 0.5f), new Vector2(0.9f, 0.72f), color, 18);
            Shape(root, "Stem", new Vector2(0.46f, 0.27f), new Vector2(0.54f, 0.48f), color, 4);
            Shape(root, "Base", new Vector2(0.25f, 0.16f), new Vector2(0.75f, 0.29f), color, 8);
        }

        private static void CreateLightbulb(RectTransform root, Color color)
        {
            Shape(root, "Bulb", new Vector2(0.23f, 0.4f), new Vector2(0.77f, 0.9f), color, 30);
            Shape(root, "Neck", new Vector2(0.4f, 0.28f), new Vector2(0.6f, 0.46f), color, 5);
            Shape(root, "Base", new Vector2(0.34f, 0.16f), new Vector2(0.66f, 0.3f), new Color(0.78f, 0.55f, 0.12f), 5);
            Shape(root, "Shine", new Vector2(0.34f, 0.66f), new Vector2(0.46f, 0.79f), new Color(1f, 1f, 1f, 0.75f), 10);
        }

        private static void CreateGift(RectTransform root, Color color)
        {
            Shape(root, "Box", new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.66f), color, 9);
            Shape(root, "Lid", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.76f), Color.Lerp(color, Color.white, 0.15f), 8);
            Shape(root, "RibbonV", new Vector2(0.46f, 0.18f), new Vector2(0.56f, 0.76f), new Color(1f, 0.83f, 0.12f), 4);
            Shape(root, "BowL", new Vector2(0.28f, 0.72f), new Vector2(0.5f, 0.9f), new Color(1f, 0.83f, 0.12f), 16, 22f);
            Shape(root, "BowR", new Vector2(0.5f, 0.72f), new Vector2(0.72f, 0.9f), new Color(1f, 0.83f, 0.12f), 16, -22f);
        }

        private static void CreateRecords(RectTransform root, Color color)
        {
            Shape(root, "Board", new Vector2(0.18f, 0.1f), new Vector2(0.82f, 0.88f), new Color(0.95f, 0.92f, 0.8f), 10);
            Shape(root, "Clip", new Vector2(0.36f, 0.82f), new Vector2(0.64f, 0.94f), color, 8);
            Shape(root, "Bar1", new Vector2(0.29f, 0.23f), new Vector2(0.4f, 0.48f), new Color(0.1f, 0.5f, 1f), 3);
            Shape(root, "Bar2", new Vector2(0.45f, 0.23f), new Vector2(0.56f, 0.66f), new Color(0.24f, 0.76f, 0.25f), 3);
            Shape(root, "Bar3", new Vector2(0.61f, 0.23f), new Vector2(0.72f, 0.75f), new Color(1f, 0.48f, 0.08f), 3);
        }

        private static void CreateGear(RectTransform root, Color color)
        {
            Shape(root, "Outer", new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f), color, 30);
            Shape(root, "Center", new Vector2(0.37f, 0.37f), new Vector2(0.63f, 0.63f), new Color(0.2f, 0.29f, 0.44f), 22);
            Shape(root, "Top", new Vector2(0.43f, 0.78f), new Vector2(0.57f, 0.96f), color, 4);
            Shape(root, "Bottom", new Vector2(0.43f, 0.04f), new Vector2(0.57f, 0.22f), color, 4);
            Shape(root, "Left", new Vector2(0.04f, 0.43f), new Vector2(0.22f, 0.57f), color, 4);
            Shape(root, "Right", new Vector2(0.78f, 0.43f), new Vector2(0.96f, 0.57f), color, 4);
        }

        private static void CreateUndo(RectTransform root, Color color)
        {
            Shape(root, "Top", new Vector2(0.28f, 0.67f), new Vector2(0.76f, 0.78f), color, 5);
            Shape(root, "Side", new Vector2(0.67f, 0.28f), new Vector2(0.78f, 0.73f), color, 5);
            Shape(root, "Bottom", new Vector2(0.34f, 0.22f), new Vector2(0.74f, 0.33f), color, 5);
            Shape(root, "HeadA", new Vector2(0.12f, 0.55f), new Vector2(0.38f, 0.66f), color, 5, 35f);
            Shape(root, "HeadB", new Vector2(0.12f, 0.69f), new Vector2(0.38f, 0.8f), color, 5, -35f);
        }

        private static void CreateMoveBoost(RectTransform root, Color color)
        {
            Shape(root, "Arc", new Vector2(0.12f, 0.31f), new Vector2(0.8f, 0.43f), color, 5, 15f);
            Shape(root, "ArrowA", new Vector2(0.68f, 0.42f), new Vector2(0.9f, 0.53f), color, 5, 38f);
            Shape(root, "ArrowB", new Vector2(0.68f, 0.27f), new Vector2(0.9f, 0.38f), color, 5, -38f);
            Shape(root, "PlusV", new Vector2(0.43f, 0.5f), new Vector2(0.55f, 0.86f), color, 4);
            Shape(root, "PlusH", new Vector2(0.31f, 0.62f), new Vector2(0.67f, 0.74f), color, 4);
        }

        private static void CreateEye(RectTransform root, Color color)
        {
            Shape(root, "Eye", new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f), color, 28);
            Shape(root, "Inner", new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.66f), new Color(0.1f, 0.46f, 0.92f), 28);
            Shape(root, "Pupil", new Vector2(0.4f, 0.31f), new Vector2(0.6f, 0.69f), Color.white, 24);
        }

        private static void CreateShuffle(RectTransform root, Color color)
        {
            Shape(root, "LineA", new Vector2(0.14f, 0.62f), new Vector2(0.76f, 0.72f), color, 5, -24f);
            Shape(root, "LineB", new Vector2(0.14f, 0.29f), new Vector2(0.76f, 0.39f), color, 5, 24f);
            Shape(root, "HeadA", new Vector2(0.7f, 0.62f), new Vector2(0.9f, 0.72f), color, 5, 35f);
            Shape(root, "HeadB", new Vector2(0.7f, 0.28f), new Vector2(0.9f, 0.38f), color, 5, -35f);
        }

        private static void CreatePlay(RectTransform root, Color color)
        {
            Shape(root, "Play", new Vector2(0.28f, 0.24f), new Vector2(0.73f, 0.76f), color, 6, -45f);
        }

        private static void CreateHome(RectTransform root, Color color)
        {
            Shape(root, "House", new Vector2(0.23f, 0.18f), new Vector2(0.77f, 0.62f), color, 8);
            Shape(root, "Roof", new Vector2(0.22f, 0.48f), new Vector2(0.78f, 0.86f), color, 7, 45f);
            Shape(root, "Door", new Vector2(0.45f, 0.18f), new Vector2(0.58f, 0.45f), new Color(0.3f, 0.18f, 0.12f), 3);
        }

        private static void CreateList(RectTransform root, Color color)
        {
            for (int i = 0; i < 3; i++)
            {
                float y = 0.68f - (i * 0.25f);
                Shape(root, $"Dot{i}", new Vector2(0.1f, y), new Vector2(0.23f, y + 0.13f), color, 12);
                Shape(root, $"Line{i}", new Vector2(0.3f, y + 0.02f), new Vector2(0.9f, y + 0.11f), color, 5);
            }
        }

        private static void CreateChevron(RectTransform root, Color color)
        {
            Shape(root, "Upper", new Vector2(0.28f, 0.5f), new Vector2(0.78f, 0.62f), color, 5, -42f);
            Shape(root, "Lower", new Vector2(0.28f, 0.38f), new Vector2(0.78f, 0.5f), color, 5, 42f);
        }

        private static void CreateDot(RectTransform root, Color color)
        {
            Shape(root, "Dot", new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), color, 30);
        }
    }
}
