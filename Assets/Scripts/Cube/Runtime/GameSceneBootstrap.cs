using UnityEngine;
using UnityEngine.SceneManagement;
using CubeChallenge3D.Core;
using CubeChallenge3D.Cube.Debugging;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.GameModes.QuickPlay;
using CubeChallenge3D.GameModes.RankingChallenge;
using CubeChallenge3D.GameModes.Stages;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Save;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.UI.Game;
using CubeChallenge3D.UI.Settings;
using CubeChallenge3D.Economy;

namespace CubeChallenge3D.Cube.Runtime
{
    public sealed class GameSceneBootstrap : MonoBehaviour
    {
        private const string GameSceneName = "Game";
        private const string BootstrapName = "GameSceneBootstrap";
        private static readonly Vector3 StageGameplayViewOffset = new Vector3(0f, 0.24f, 0f);
        private static readonly Vector3 RankingChallengeViewOffset = new Vector3(0f, 0.10f, 0f);

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
            if (launchMode == GameLaunchMode.StagePlay
                || launchMode == GameLaunchMode.RankingChallenge
                || launchMode == GameLaunchMode.PracticeRanking)
            {
                orbitController.SetBaseScale(0.62f, 0.88f);
                orbitController.SetViewOffset(launchMode == GameLaunchMode.RankingChallenge
                    ? RankingChallengeViewOffset
                    : StageGameplayViewOffset);
            }

            CubeControlModeController controlModeController = GetComponent<CubeControlModeController>();
            if (controlModeController == null)
            {
                controlModeController = gameObject.AddComponent<CubeControlModeController>();
            }

            controlModeController.Initialize(controller, orbitController, settingsStore);
            controlModeController.SetDragControlMode();

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
            mobileUi.SetVisible(false);

            QuickPlayGameMode quickPlay = null;
            RankingChallengeGameMode rankingMode = null;
            StagePlayGameMode stageMode = null;

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
            diagnostics.SetDebugPanelVisible(false);

            SettingsPanelUI settingsPanel = GetComponent<SettingsPanelUI>();
            if (settingsPanel == null)
            {
                settingsPanel = gameObject.AddComponent<SettingsPanelUI>();
            }

            settingsPanel.Initialize(settingsStore, controlModeController, diagnostics);

            if (launchMode == GameLaunchMode.RankingChallenge
                || launchMode == GameLaunchMode.PracticeRanking)
            {
                LocalRankingStore rankingStore = new LocalRankingStore();
                rankingMode = GetComponent<RankingChallengeGameMode>();
                if (rankingMode == null)
                {
                    rankingMode = gameObject.AddComponent<RankingChallengeGameMode>();
                }

                rankingMode.Initialize(controller, controlModeController, settingsStore, rankingStore);

            }
            else if (launchMode == GameLaunchMode.StagePlay)
            {
                StageDataLoader stageLoader = new StageDataLoader();
                StageProgressStore stageProgressStore = new StageProgressStore();
                stageProgressStore.EnsureStageDefaults(stageLoader.LoadAllStages());

                stageMode = GetComponent<StagePlayGameMode>();
                if (stageMode == null)
                {
                    stageMode = gameObject.AddComponent<StagePlayGameMode>();
                }

                stageMode.Initialize(controller, controlModeController, stageLoader, stageProgressStore);
                StageData stage = stageLoader.GetStageById(GameLaunchContext.StageId);
                stageMode.LoadStage(stage);
                if (stage == null || !StagePlayGameMode.IsTargetPatternStage(stage))
                {
                    stageMode.StartStage();
                }

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

            }

            ConfigureCamera();
            EnsureDirectionalLight();
            EnsureCubeVisible(controller);

            MobileGameHudUI gameHud = GetComponent<MobileGameHudUI>();
            if (gameHud == null)
            {
                gameHud = gameObject.AddComponent<MobileGameHudUI>();
            }

            gameHud.Initialize(controller, orbitController, launchMode, quickPlay, rankingMode, stageMode);
            devInput.Initialize(
                controller,
                diagnostics,
                orbitController,
                controlModeController,
                quickPlay,
                rankingMode,
                stageMode,
                launchMode != GameLaunchMode.StagePlay);
            EnsureCubeVisible(controller);
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
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 48f;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = VisualCustomizationService.LoadSelectedTheme().backgroundColor;
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

        private static void EnsureCubeVisible(CubeController controller)
        {
            if (controller == null || controller.ViewRoot == null)
            {
                return;
            }

            controller.SetViewVisible(true);
            Renderer[] renderers = controller.ViewRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
    }
}
