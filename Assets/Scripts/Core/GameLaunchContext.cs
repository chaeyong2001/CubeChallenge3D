namespace CubeChallenge3D.Core
{
    public enum GameLaunchMode
    {
        QuickPlay,
        RankingChallenge,
        StagePlay
    }

    public static class GameLaunchContext
    {
        public static GameLaunchMode Mode { get; private set; } = GameLaunchMode.QuickPlay;
        public static string StageId { get; private set; } = string.Empty;
        private static bool openStageListOnMainMenu;
        private static bool openShopOnMainMenu;

        public static void SetMode(GameLaunchMode mode)
        {
            Mode = mode;
        }

        public static void SetStagePlay(string stageId)
        {
            Mode = GameLaunchMode.StagePlay;
            StageId = stageId ?? string.Empty;
        }

        public static void RequestStageListOnMainMenu()
        {
            openStageListOnMainMenu = true;
        }

        public static bool ConsumeStageListOnMainMenuRequest()
        {
            if (!openStageListOnMainMenu)
            {
                return false;
            }

            openStageListOnMainMenu = false;
            return true;
        }

        public static void RequestShopOnMainMenu()
        {
            openShopOnMainMenu = true;
        }

        public static bool ConsumeShopOnMainMenuRequest()
        {
            if (!openShopOnMainMenu)
            {
                return false;
            }

            openShopOnMainMenu = false;
            return true;
        }

        public static void Reset()
        {
            Mode = GameLaunchMode.QuickPlay;
            StageId = string.Empty;
            openStageListOnMainMenu = false;
            openShopOnMainMenu = false;
        }
    }
}
