using System;
using System.Collections.Generic;
using CubeChallenge3D.Stages.Assist;

namespace CubeChallenge3D.Economy
{
    [Serializable]
    public sealed class ShopItemDefinition
    {
        public StageAssistItemType itemType;
        public int coinPrice;
        public int quantity;

        public static IReadOnlyList<ShopItemDefinition> CreateDefaults()
        {
            return new List<ShopItemDefinition>
            {
                new ShopItemDefinition { itemType = StageAssistItemType.Undo, coinPrice = 50, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.MovePlus1, coinPrice = 100, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.MovePlus2, coinPrice = 180, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.MovePlus3, coinPrice = 250, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.SolverTicket, coinPrice = 250, quantity = 1 }
            };
        }
    }
}
