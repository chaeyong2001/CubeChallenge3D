using CubeChallenge3D.Stages.Assist;

namespace CubeChallenge3D.Economy
{
    public static class EconomyBalanceConfig
    {
        public const int UndoCoinPrice = 80;
        public const int MovePlus1CoinPrice = 90;
        public const int MovePlus2CoinPrice = 150;
        public const int MovePlus3CoinPrice = 220;
        public const int SolverTicketCoinPrice = 250;
        public const int NicknameTicketCoinPrice = 500;

        public const int SmallHeartPackHearts = 3;
        public const int SmallHeartPackGemPrice = 15;
        public const int MediumHeartPackHearts = 10;
        public const int MediumHeartPackGemPrice = 30;
        public const int LargeHeartPackHearts = 30;
        public const int LargeHeartPackGemPrice = 65;

        public const int SmallCoinPackCoins = 500;
        public const int SmallCoinPackGemPrice = 40;
        public const int MediumCoinPackCoins = 1500;
        public const int MediumCoinPackGemPrice = 100;
        public const int LargeCoinPackCoins = 4500;
        public const int LargeCoinPackGemPrice = 260;

        public const int SmallGemPackGems = 80;
        public const int MediumGemPackGems = 450;
        public const int LargeGemPackGems = 800;
        public const string SmallGemPackFallbackPrice = "KRW 1,500";
        public const string MediumGemPackFallbackPrice = "KRW 5,900";
        public const string LargeGemPackFallbackPrice = "KRW 12,000";
        public const string RemoveAdsFallbackPrice = "KRW 1,900";

        public const int OneStarClearCoins = 15;
        public const int TwoStarClearCoins = 20;
        public const int ThreeStarClearCoins = 25;

        public const int BlockRequiredStars = 30;
        public const int BlockRewardGems = 5;

        public const int DailySolverTicketBonus = 3;

        public static int GetCoinPrice(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    return UndoCoinPrice;
                case StageAssistItemType.MovePlus1:
                    return MovePlus1CoinPrice;
                case StageAssistItemType.MovePlus2:
                    return MovePlus2CoinPrice;
                case StageAssistItemType.MovePlus3:
                    return MovePlus3CoinPrice;
                case StageAssistItemType.SolverTicket:
                    return SolverTicketCoinPrice;
                case StageAssistItemType.NicknameTicket:
                    return NicknameTicketCoinPrice;
                default:
                    return 0;
            }
        }

        public static int GetStageClearCoinsForStars(int stars)
        {
            switch (stars)
            {
                case 3:
                    return ThreeStarClearCoins;
                case 2:
                    return TwoStarClearCoins;
                case 1:
                    return OneStarClearCoins;
                default:
                    return 0;
            }
        }
    }
}
