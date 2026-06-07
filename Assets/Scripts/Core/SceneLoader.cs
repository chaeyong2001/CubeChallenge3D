using UnityEngine.SceneManagement;

namespace CubeChallenge3D.Core
{
    public static class SceneLoader
    {
        public const string BootScene = "Boot";
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";
        public const string StageScene = "Stage";
        public const string SolverScene = "Solver";

        public static void LoadBoot()
        {
            LoadScene(BootScene, AppState.Boot);
        }

        public static void LoadMainMenu()
        {
            LoadScene(MainMenuScene, AppState.MainMenu);
        }

        public static void LoadGame()
        {
            LoadScene(GameScene, AppState.Game);
        }

        public static void LoadStage()
        {
            LoadScene(StageScene, AppState.Stage);
        }

        public static void LoadSolver()
        {
            LoadScene(SolverScene, AppState.Solver);
        }

        public static void LoadScene(string sceneName, AppState nextState)
        {
            GameManager.Instance?.SetState(nextState);
            SceneManager.LoadScene(sceneName);
        }
    }
}
