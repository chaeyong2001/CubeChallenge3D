using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CubeChallenge3D.Economy
{
    public static class VisualCustomizationCatalog
    {
        private static readonly List<CubeSkinData> Skins = new List<CubeSkinData>
        {
            Skin("classic", "Classic", "Original high-contrast cube colors.", 0, true, "Common",
                C(0.025f, 0.03f, 0.035f), C(.95f,.95f,.95f), C(1f,.85f,.05f), C(.05f,.65f,.25f), C(.05f,.25f,.9f), C(.9f,.05f,.08f), C(1f,.35f,.03f)),
            Skin("soft_pastel", "Soft Pastel", "Softer colors for relaxed practice.", 0, true, "Common",
                C(.12f,.13f,.15f), C(.96f,.95f,.92f), C(1f,.88f,.45f), C(.45f,.78f,.58f), C(.45f,.62f,.9f), C(.93f,.45f,.5f), C(1f,.65f,.4f), null, 1f, 0f, .45f),
            PremiumStickerSkin(
                "royal_enamel",
                "Royal Enamel",
                "Glossy enamel tiles with polished gold trim.",
                420,
                "Premium",
                "CubeSkins/royal_enamel",
                C(.72f, .43f, .12f),
                .82f,
                .92f,
                0f,
                false,
                1.12f,
                .008f,
                "CubeSkins/royal_enamel/frame"),
            PremiumStickerSkin(
                "cyber_core",
                "Cyber Core",
                "Bright neon tech panels with dark cyber borders.",
                380,
                "Premium",
                "CubeSkins/cyber_core",
                C(.002f, .004f, .008f),
                .18f,
                .94f,
                .58f,
                false,
                1.17f,
                .012f),
            PremiumStickerSkin(
                "crystal_candy",
                "Crystal Candy",
                "Translucent candy crystal stickers with soft swirl highlights.",
                360,
                "Premium",
                "CubeSkins/crystal_candy",
                C(.055f, .012f, .115f),
                .28f,
                .98f,
                .20f,
                false,
                1.14f,
                .011f,
                "CubeSkins/crystal_candy/frame"),
            StickerSkin(
                "animal",
                "Animal Friends",
                "Cheerful animal stickers on soft pastel tiles.",
                150,
                "Epic",
                "CubeSkins/animal",
                C(.98f, .96f, .90f),
                C(1f, .90f, .48f),
                C(.54f, .82f, .62f),
                C(.50f, .67f, .94f),
                C(.95f, .52f, .58f),
                C(1f, .70f, .46f),
                .48f),
            StickerSkin(
                "national_flag",
                "National Flag",
                "National flag stickers on soft pastel tiles.",
                160,
                "Epic",
                "CubeSkins/national_flag",
                C(.97f, .96f, .90f),
                C(1f, .88f, .42f),
                C(.50f, .80f, .60f),
                C(.48f, .66f, .95f),
                C(.94f, .48f, .54f),
                C(1f, .68f, .42f),
                .44f)
        };

        private static readonly List<ThemeData> Themes = new List<ThemeData>
        {
            Theme("default", "Default", "Original neutral play background.", 0, true, C(.32f,.3f,.29f), C(.06f,.075f,.09f)),
            Theme("minimal_white", "Minimal White", "Bright, clean practice environment.", 0, true, C(.82f,.85f,.87f), C(.9f,.92f,.94f)),
            Theme("dark_room", "Dark Room", "Focused dark play environment.", 80, false, C(.025f,.03f,.045f), C(.055f,.065f,.085f)),
            Theme("space", "Space", "Deep indigo space-inspired background.", 150, false, C(.025f,.02f,.1f), C(.06f,.045f,.15f)),
            Theme("wood_desk", "Wood Desk", "Warm desktop-inspired background.", 120, false, C(.22f,.12f,.065f), C(.17f,.085f,.04f))
        };

        public static IReadOnlyList<CubeSkinData> GetSkins() => Skins;
        public static IReadOnlyList<ThemeData> GetThemes() => Themes;
        public static CubeSkinData GetSkin(string id) => Skins.FirstOrDefault(item => item.skinId == id) ?? Skins[0];
        public static bool HasSkin(string id) => Skins.Any(item => item.skinId == id);
        public static ThemeData GetTheme(string id) => Themes.FirstOrDefault(item => item.themeId == id) ?? Themes[0];
        public static bool HasTheme(string id) => Themes.Any(item => item.themeId == id);

        private static CubeSkinData Skin(string id, string name, string description, int price, bool unlocked, string rarity,
            Color body, Color white, Color yellow, Color green, Color blue, Color red, Color orange,
            string texturePath = null, float textureScale = 1f, float metallic = 0f, float smoothness = .35f,
            float emission = 0f, float textureVisibility = 0f, bool useTextureEmission = false)
        {
            return new CubeSkinData
            {
                skinId = id, displayName = name, description = description, priceGems = price,
                isDefaultUnlocked = unlocked, rarity = rarity, purchaseType = unlocked ? "free" : "premium", bodyColor = body,
                whiteColor = white, yellowColor = yellow, greenColor = green,
                blueColor = blue, redColor = red, orangeColor = orange,
                textureResourcePath = texturePath, textureScale = textureScale,
                metallic = metallic, smoothness = smoothness, emissionStrength = emission,
                textureVisibility = textureVisibility, useTextureEmission = useTextureEmission
            };
        }

        private static ThemeData Theme(string id, string name, string description, int price, bool unlocked, Color background, Color panel)
        {
            return new ThemeData
            {
                themeId = id, displayName = name, description = description, priceGems = price,
                isDefaultUnlocked = unlocked, backgroundColor = background, panelColor = panel
            };
        }

        private static CubeSkinData StickerSkin(
            string id,
            string name,
            string description,
            int price,
            string rarity,
            string textureRoot,
            Color? white = null,
            Color? yellow = null,
            Color? green = null,
            Color? blue = null,
            Color? red = null,
            Color? orange = null,
            float textureVisibility = 1f)
        {
            Color pale = C(.94f, .94f, .94f);
            return new CubeSkinData
            {
                skinId = id,
                displayName = name,
                description = description,
                priceGems = price,
                isDefaultUnlocked = false,
                rarity = rarity,
                purchaseType = "premium",
                bodyColor = C(.025f, .03f, .035f),
                whiteColor = white ?? pale,
                yellowColor = yellow ?? pale,
                greenColor = green ?? pale,
                blueColor = blue ?? pale,
                redColor = red ?? pale,
                orangeColor = orange ?? pale,
                stickerTextureRoot = textureRoot,
                textureScale = 1f,
                metallic = 0f,
                smoothness = .55f,
                textureVisibility = textureVisibility
            };
        }

        private static CubeSkinData PremiumStickerSkin(
            string id,
            string name,
            string description,
            int price,
            string rarity,
            string textureRoot,
            Color bodyColor,
            float metallic,
            float smoothness,
            float emission,
            bool useTextureEmission = false,
            float stickerSizeMultiplier = 1.06f,
            float stickerOffsetAdd = .006f,
            string bodyTexturePath = null)
        {
            return new CubeSkinData
            {
                skinId = id,
                displayName = name,
                description = description,
                priceGems = price,
                isDefaultUnlocked = false,
                rarity = rarity,
                purchaseType = "premium",
                bodyColor = bodyColor,
                whiteColor = C(.95f, .96f, .98f),
                yellowColor = C(1f, .82f, .03f),
                greenColor = C(.05f, .82f, .32f),
                blueColor = C(.05f, .32f, 1f),
                redColor = C(1f, .08f, .1f),
                orangeColor = C(1f, .42f, .03f),
                textureResourcePath = bodyTexturePath,
                stickerTextureRoot = textureRoot,
                textureScale = 1f,
                metallic = metallic,
                smoothness = smoothness,
                emissionStrength = emission,
                textureVisibility = 1f,
                useTextureEmission = useTextureEmission,
                stickerSizeMultiplier = stickerSizeMultiplier,
                stickerOffsetAdd = stickerOffsetAdd
            };
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);
    }
}
