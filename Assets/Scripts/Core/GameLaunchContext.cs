namespace CubeChallenge3D.Core
{
    public enum GameLaunchMode
    {
        QuickPlay,
        RankingChallenge
    }

    public static class GameLaunchContext
    {
        public static GameLaunchMode Mode { get; private set; } = GameLaunchMode.QuickPlay;

        public static void SetMode(GameLaunchMode mode)
        {
            Mode = mode;
        }

        public static void Reset()
        {
            Mode = GameLaunchMode.QuickPlay;
        }
    }
}
