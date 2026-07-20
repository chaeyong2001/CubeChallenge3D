using UnityEngine;
using UnityEngine.SceneManagement;

namespace CubeChallenge3D.Cube.Debugging
{
    public sealed class CubeStateDebugSceneBootstrap : MonoBehaviour
    {
        public const string SceneName = "CubeStateDebug";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName || FindAnyObjectByType<CubeStateDebugSceneBootstrap>() != null)
            {
                return;
            }

            new GameObject(nameof(CubeStateDebugSceneBootstrap))
                .AddComponent<CubeStateDebugSceneBootstrap>();
        }

        private void Start()
        {
            ConfigureCamera();
            EnsureLight();
            gameObject.AddComponent<CubeStateDebugUI>();
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(4f, 4f, -6f);
            camera.transform.LookAt(Vector3.zero);
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f);
        }

        private static void EnsureLight()
        {
            if (FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
