using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Economy
{
    [Serializable]
    public sealed class PremiumShopItemDefinition
    {
        public string itemId;
        public string displayName;
        public string description;
        public int gemPrice;
        public string category;

        public static IReadOnlyList<PremiumShopItemDefinition> CreatePlaceholders()
        {
            return new List<PremiumShopItemDefinition>
            {
                new PremiumShopItemDefinition
                {
                    itemId = "skin_galaxy",
                    displayName = "Galaxy Cube",
                    description = "Sun, moon, planets, stars, and galaxy stickers.",
                    gemPrice = 200,
                    category = "CubeSkin"
                },
                new PremiumShopItemDefinition
                {
                    itemId = "skin_national_flag",
                    displayName = "National Flag Cube",
                    description = "Six national flag stickers.",
                    gemPrice = 160,
                    category = "CubeSkin"
                },
                new PremiumShopItemDefinition
                {
                    itemId = "skin_animal",
                    displayName = "Animal Friends Cube",
                    description = "Six cute animal face stickers.",
                    gemPrice = 150,
                    category = "CubeSkin"
                },
                new PremiumShopItemDefinition
                {
                    itemId = "skin_sports",
                    displayName = "Sports Cube",
                    description = "Six iconic sports stickers.",
                    gemPrice = 140,
                    category = "CubeSkin"
                }
            };
        }
    }
}
