#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace CubeChallenge3D.EditorTools
{
    [InitializeOnLoad]
    public static class BuildSceneListBootstrap
    {
        private static readonly string[] RequiredScenes =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/Stage.unity",
            "Assets/Scenes/Solver.unity"
        };

        static BuildSceneListBootstrap()
        {
            EditorApplication.delayCall += EnsureRequiredScenes;
        }

        private static void EnsureRequiredScenes()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            bool changed = false;

            foreach (string path in RequiredScenes)
            {
                if (scenes.Any(scene => scene.path == path))
                {
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
            }

            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
#endif
