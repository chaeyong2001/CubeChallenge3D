using System;
using System.Collections.Generic;
using CubeChallenge3D.IAP;

namespace CubeChallenge3D.Economy
{
    [Serializable]
    public sealed class PromotionProductDefinition
    {
        public string id;
        public string productId;
        public string displayName;
        public string description;
        public string rewardText;
        public string priceText;
        public string iconKey;
        public string buttonLabel;
        public bool isRealMoneyPurchase;
        public bool isOwned;
        public bool isConsumable;
        public string itemType;

        public static IReadOnlyList<PromotionProductDefinition> CreateDefaults()
        {
            return new List<PromotionProductDefinition>
            {
                new PromotionProductDefinition
                {
                    id = "promo_gems_small",
                    productId = PromotionProductIds.SmallGemPack,
                    displayName = "Small Gem Pack",
                    description = "Get 80 Gems.",
                    rewardText = $"+{EconomyBalanceConfig.SmallGemPackGems} Gems",
                    priceText = EconomyBalanceConfig.SmallGemPackFallbackPrice,
                    iconKey = "gem_pack_small",
                    buttonLabel = "BUY",
                    isRealMoneyPurchase = true,
                    isOwned = false,
                    isConsumable = true,
                    itemType = "GemPack"
                },
                new PromotionProductDefinition
                {
                    id = "promo_gems_medium",
                    productId = PromotionProductIds.MediumGemPack,
                    displayName = "Medium Gem Pack",
                    description = "Get 450 Gems.",
                    rewardText = $"+{EconomyBalanceConfig.MediumGemPackGems} Gems",
                    priceText = EconomyBalanceConfig.MediumGemPackFallbackPrice,
                    iconKey = "gem_pack_medium",
                    buttonLabel = "BUY",
                    isRealMoneyPurchase = true,
                    isOwned = false,
                    isConsumable = true,
                    itemType = "GemPack"
                },
                new PromotionProductDefinition
                {
                    id = "promo_gems_large",
                    productId = PromotionProductIds.LargeGemPack,
                    displayName = "Large Gem Pack",
                    description = "Get 800 Gems.",
                    rewardText = $"+{EconomyBalanceConfig.LargeGemPackGems:N0} Gems",
                    priceText = EconomyBalanceConfig.LargeGemPackFallbackPrice,
                    iconKey = "gem_pack_large",
                    buttonLabel = "BUY",
                    isRealMoneyPurchase = true,
                    isOwned = false,
                    isConsumable = true,
                    itemType = "GemPack"
                },
                new PromotionProductDefinition
                {
                    id = "promo_remove_ads",
                    productId = PromotionProductIds.RemoveAds,
                    displayName = "Remove Ads Forever",
                    description = "Remove forced ads. Rewarded ads remain available.",
                    rewardText = "Forced ads removed permanently.",
                    priceText = EconomyBalanceConfig.RemoveAdsFallbackPrice,
                    iconKey = "remove_ads",
                    buttonLabel = "BUY",
                    isRealMoneyPurchase = true,
                    isOwned = false,
                    isConsumable = false,
                    itemType = "RemoveAds"
                }
            };
        }
    }
}
