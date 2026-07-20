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
                new ShopItemDefinition { itemType = StageAssistItemType.Undo, coinPrice = EconomyBalanceConfig.UndoCoinPrice, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.MovePlus1, coinPrice = EconomyBalanceConfig.MovePlus1CoinPrice, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.MovePlus2, coinPrice = EconomyBalanceConfig.MovePlus2CoinPrice, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.MovePlus3, coinPrice = EconomyBalanceConfig.MovePlus3CoinPrice, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.SolverTicket, coinPrice = EconomyBalanceConfig.SolverTicketCoinPrice, quantity = 1 },
                new ShopItemDefinition { itemType = StageAssistItemType.NicknameTicket, coinPrice = EconomyBalanceConfig.NicknameTicketCoinPrice, quantity = 1 }
            };
        }
    }
}
