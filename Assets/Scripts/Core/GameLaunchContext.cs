namespace CubeChallenge3D.Core
{
    public enum GameLaunchMode
    {
        QuickPlay,
        PracticeRanking,
        RankingChallenge,
        StagePlay
    }

    public static class GameLaunchContext
    {
        public static GameLaunchMode Mode { get; private set; } = GameLaunchMode.QuickPlay;
        public static string StageId { get; private set; } = string.Empty;
        private static bool openStageListOnMainMenu;
        private static bool openShopOnMainMenu;
        private static bool openSolverLearnOnMainMenu;
        private static bool stageAdvanceOnMainMenu;
        private static string stageAdvanceFromStageId = string.Empty;
        private static string stageAdvanceToStageId = string.Empty;
        private static bool stageAdvanceAutoStart;
        private static bool fadeInGameOnNextLoad;
        private static string requestedShopTab = string.Empty;

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

        public static void RequestStageAdvanceOnMainMenu(string fromStageId, string toStageId, bool autoStart)
        {
            openStageListOnMainMenu = true;
            stageAdvanceOnMainMenu = true;
            stageAdvanceFromStageId = fromStageId ?? string.Empty;
            stageAdvanceToStageId = toStageId ?? string.Empty;
            stageAdvanceAutoStart = autoStart;
        }

        public static bool ConsumeStageAdvanceOnMainMenuRequest(out string fromStageId, out string toStageId, out bool autoStart)
        {
            fromStageId = stageAdvanceFromStageId;
            toStageId = stageAdvanceToStageId;
            autoStart = stageAdvanceAutoStart;
            if (!stageAdvanceOnMainMenu)
            {
                return false;
            }

            stageAdvanceOnMainMenu = false;
            stageAdvanceFromStageId = string.Empty;
            stageAdvanceToStageId = string.Empty;
            stageAdvanceAutoStart = false;
            return true;
        }

        public static void RequestGameScreenFadeInOnNextLoad()
        {
            fadeInGameOnNextLoad = true;
        }

        public static bool ConsumeGameScreenFadeInRequest()
        {
            if (!fadeInGameOnNextLoad)
            {
                return false;
            }

            fadeInGameOnNextLoad = false;
            return true;
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

        public static void RequestSolverLearnOnMainMenu()
        {
            openSolverLearnOnMainMenu = true;
        }

        public static bool ConsumeSolverLearnOnMainMenuRequest()
        {
            if (!openSolverLearnOnMainMenu)
            {
                return false;
            }

            openSolverLearnOnMainMenu = false;
            return true;
        }

        public static void RequestShopOnMainMenu()
        {
            openShopOnMainMenu = true;
            requestedShopTab = string.Empty;
        }

        public static void RequestShopOnMainMenu(string tabId)
        {
            openShopOnMainMenu = true;
            requestedShopTab = tabId ?? string.Empty;
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

        public static string ConsumeRequestedShopTab()
        {
            string tab = requestedShopTab;
            requestedShopTab = string.Empty;
            return tab;
        }

        public static void Reset()
        {
            Mode = GameLaunchMode.QuickPlay;
            StageId = string.Empty;
            openStageListOnMainMenu = false;
            openShopOnMainMenu = false;
            openSolverLearnOnMainMenu = false;
            stageAdvanceOnMainMenu = false;
            stageAdvanceFromStageId = string.Empty;
            stageAdvanceToStageId = string.Empty;
            stageAdvanceAutoStart = false;
            fadeInGameOnNextLoad = false;
            requestedShopTab = string.Empty;
        }
    }
}
