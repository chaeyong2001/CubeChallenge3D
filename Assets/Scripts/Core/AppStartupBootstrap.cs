using UnityEngine;
using UnityEngine.SceneManagement;

namespace CubeChallenge3D.Core
{
    public static class AppStartupBootstrap
    {
        private static bool isRouting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            isRouting = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RouteActiveScene()
        {
            RouteIfBoot(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneLoader.BootScene)
            {
                isRouting = false;
            }
            RouteIfBoot(scene);
        }

        private static void RouteIfBoot(Scene scene)
        {
            if (isRouting || scene.name != SceneLoader.BootScene)
            {
                return;
            }

            isRouting = true;
            GameLaunchContext.Reset();
            SceneLoader.LoadMainMenu();
        }
    }
}
