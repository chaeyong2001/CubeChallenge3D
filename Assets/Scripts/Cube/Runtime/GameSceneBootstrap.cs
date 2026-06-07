using UnityEngine;
using UnityEngine.SceneManagement;
using CubeChallenge3D.Core;
using CubeChallenge3D.Cube.Debugging;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.GameModes.QuickPlay;
using CubeChallenge3D.GameModes.RankingChallenge;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Save;
using CubeChallenge3D.UI.Game;
using CubeChallenge3D.UI.Ranking;
using CubeChallenge3D.UI.Settings;

namespace CubeChallenge3D.Cube.Runtime
{
    public sealed class GameSceneBootstrap : MonoBehaviour
    {
        private const string GameSceneName = "Game";
        private const string BootstrapName = "GameSceneBootstrap";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedHandler()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name != GameSceneName || FindAnyObjectByType<GameSceneBootstrap>() != null)
            {
                return;
            }

            new GameObject(BootstrapName).AddComponent<GameSceneBootstrap>();
        }

        private void Start()
        {
            SettingsStore settingsStore = new SettingsStore();
            GameLaunchMode launchMode = GameLaunchContext.Mode;

            CubeController controller = GetComponent<CubeController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<CubeController>();
            }

            controller.InitializeSolved();

            CubeViewOrbitController orbitController = GetComponent<CubeViewOrbitController>();
            if (orbitController == null)
            {
                orbitController = gameObject.AddComponent<CubeViewOrbitController>();
            }

            orbitController.Initialize(controller);
            orbitController.SetOrbitSensitivity(settingsStore.Current.viewSensitivity);

            CubeControlModeController controlModeController = GetComponent<CubeControlModeController>();
            if (controlModeController == null)
            {
                controlModeController = gameObject.AddComponent<CubeControlModeController>();
            }

            controlModeController.Initialize(controller, orbitController, settingsStore);

            CubeFaceDragInput faceDragInput = GetComponent<CubeFaceDragInput>();
            if (faceDragInput == null)
            {
                faceDragInput = gameObject.AddComponent<CubeFaceDragInput>();
            }

            faceDragInput.Initialize(controller, orbitController, controlModeController);
            faceDragInput.SetDragThreshold(settingsStore.Current.faceDragThreshold);

            GameMobileInteractionUI mobileUi = GetComponent<GameMobileInteractionUI>();
            if (mobileUi == null)
            {
                mobileUi = gameObject.AddComponent<GameMobileInteractionUI>();
            }

            mobileUi.Initialize(orbitController, controlModeController, controller);

            QuickPlayGameMode quickPlay = null;
            RankingChallengeGameMode rankingMode = null;

            CubeDevInput devInput = GetComponent<CubeDevInput>();
            if (devInput == null)
            {
                devInput = gameObject.AddComponent<CubeDevInput>();
            }

            CubeRuntimeDiagnostics diagnostics = GetComponent<CubeRuntimeDiagnostics>();
            if (diagnostics == null)
            {
                diagnostics = gameObject.AddComponent<CubeRuntimeDiagnostics>();
            }

            diagnostics.Initialize(controller, orbitController, controlModeController, faceDragInput);
            diagnostics.SetDebugPanelVisible(settingsStore.Current.showDebugPanel);

            SettingsPanelUI settingsPanel = GetComponent<SettingsPanelUI>();
            if (settingsPanel == null)
            {
                settingsPanel = gameObject.AddComponent<SettingsPanelUI>();
            }

            settingsPanel.Initialize(settingsStore, controlModeController, diagnostics);

            if (launchMode == GameLaunchMode.RankingChallenge)
            {
                LocalRankingStore rankingStore = new LocalRankingStore();
                rankingMode = GetComponent<RankingChallengeGameMode>();
                if (rankingMode == null)
                {
                    rankingMode = gameObject.AddComponent<RankingChallengeGameMode>();
                }

                rankingMode.Initialize(controller, controlModeController, settingsStore, rankingStore);

                RankingChallengeUI rankingUi = GetComponent<RankingChallengeUI>();
                if (rankingUi == null)
                {
                    rankingUi = gameObject.AddComponent<RankingChallengeUI>();
                }

                rankingUi.Initialize(rankingMode);
            }
            else
            {
                QuickPlayRecordStore recordStore = new QuickPlayRecordStore();
                quickPlay = GetComponent<QuickPlayGameMode>();
                if (quickPlay == null)
                {
                    quickPlay = gameObject.AddComponent<QuickPlayGameMode>();
                }

                quickPlay.Initialize(controller, controlModeController, recordStore);

                QuickPlayUI quickPlayUi = GetComponent<QuickPlayUI>();
                if (quickPlayUi == null)
                {
                    quickPlayUi = gameObject.AddComponent<QuickPlayUI>();
                }

                quickPlayUi.Initialize(quickPlay, mobileUi, diagnostics);
            }

            GameUtilityUI utilityUi = GetComponent<GameUtilityUI>();
            if (utilityUi == null)
            {
                utilityUi = gameObject.AddComponent<GameUtilityUI>();
            }

            utilityUi.Initialize(controller, settingsPanel);
            devInput.Initialize(controller, diagnostics, orbitController, controlModeController, quickPlay, rankingMode);
            ConfigureCamera();
            EnsureDirectionalLight();
        }

        private static void ConfigureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.transform.position = new Vector3(4f, 4f, -6f);
            mainCamera.transform.LookAt(Vector3.zero);
        }

        private static void EnsureDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    return;
                }
            }

            GameObject lightObject = new GameObject("Directional Light");
            Light directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
