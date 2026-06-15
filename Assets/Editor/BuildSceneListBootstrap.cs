#if UNITY_EDITOR
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
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/Stage.unity",
            "Assets/Scenes/Solver.unity",
            "Assets/Scenes/MainMenu.unity"
        };

        static BuildSceneListBootstrap()
        {
            EditorApplication.delayCall += EnsureRequiredScenes;
        }

        private static void EnsureRequiredScenes()
        {
            EditorBuildSettingsScene[] expected = RequiredScenes
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            bool matches = current.Length == expected.Length
                && current.Select(scene => scene.path).SequenceEqual(RequiredScenes)
                && current.All(scene => scene.enabled);
            if (!matches)
            {
                EditorBuildSettings.scenes = expected;
            }
        }
    }
}
#endif
