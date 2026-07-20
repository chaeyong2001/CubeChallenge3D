using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeChallenge3D.Audio
{
    public sealed class AudioFeedbackManager : MonoBehaviour
    {
        private const string RootName = "AudioFeedbackManager";
        private const string BgmPath = "Audio/Feedback/bgm_main";
        private const string CubeMovePath = "Audio/Feedback/cube_move";
        private const string CubeUndoPath = "Audio/Feedback/cube_undo";
        private const float BgmVolume = 0.38f;
        private const float CubeMoveVolume = 0.62f;
        private const float CubeUndoVolume = 0.68f;
        public const string GameplayBgmReason = "Gameplay";
        public const string StageGameplayBgmReason = "StageGameplay";
        public const string StageAdvanceBgmReason = "StageAdvance";
        public const string RankingChallengeBgmReason = "RankingChallenge";
        public const string LearnDetailBgmReason = "LearnDetail";
        public const string SolverDetailBgmReason = "SolverDetail";

        private static AudioFeedbackManager instance;

        private readonly HashSet<int> boundButtonIds = new HashSet<int>();
        private readonly HashSet<string> bgmSuppressReasons = new HashSet<string>();

        private SettingsStore settingsStore;
        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private AudioClip bgmClip;
        private AudioClip cubeMoveClip;
        private AudioClip cubeUndoClip;
        private CubeController boundCubeController;
        private bool pendingUndoSound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            AudioFeedbackManager manager = EnsureExists();
            manager.BindButtonsInScene();
            manager.ApplySoundSetting();
            SceneManager.sceneLoaded += (_, __) => EnsureExists().BindButtonsInScene();
        }

        public static AudioFeedbackManager EnsureExists()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindObjectOfType<AudioFeedbackManager>();
            if (instance == null)
            {
                GameObject root = new GameObject(RootName);
                instance = root.AddComponent<AudioFeedbackManager>();
            }

            DontDestroyOnLoad(instance.gameObject);
            instance.Initialize();
            return instance;
        }

        public static void RegisterButton(Button button)
        {
            EnsureExists().RegisterButtonInstance(button);
        }

        public static void RefreshSettings()
        {
            AudioFeedbackManager manager = EnsureExists();
            manager.settingsStore.Load();
            manager.ApplySoundSetting();
        }

        public static void SetBgmSuppressed(string reason, bool suppressed)
        {
            AudioFeedbackManager manager = EnsureExists();
            if (string.IsNullOrWhiteSpace(reason))
            {
                return;
            }

            if (suppressed)
            {
                manager.bgmSuppressReasons.Add(reason);
            }
            else
            {
                manager.bgmSuppressReasons.Remove(reason);
            }

            manager.ApplySoundSetting();
        }

        public static void ClearMenuBgmSuppressions()
        {
            AudioFeedbackManager manager = EnsureExists();
            manager.bgmSuppressReasons.Remove(GameplayBgmReason);
            manager.bgmSuppressReasons.Remove(StageGameplayBgmReason);
            manager.bgmSuppressReasons.Remove(StageAdvanceBgmReason);
            manager.bgmSuppressReasons.Remove(RankingChallengeBgmReason);
            manager.bgmSuppressReasons.Remove(LearnDetailBgmReason);
            manager.bgmSuppressReasons.Remove(SolverDetailBgmReason);
            manager.ApplySoundSetting();
        }

        public static void BindCubeController(CubeController controller)
        {
            EnsureExists().BindCubeControllerInstance(controller);
        }

        public static void UnbindCubeController(CubeController controller)
        {
            if (instance != null)
            {
                instance.UnbindCubeControllerInstance(controller);
            }
        }

        public static void PlayCubeRotation()
        {
            EnsureExists().PlayCubeRotationInstance();
        }

        public static void MarkNextCubeRotationAsUndo()
        {
            EnsureExists().pendingUndoSound = true;
        }

        public static void PlayButtonClick()
        {
            // Button tap audio is intentionally disabled. Keep this method as a no-op
            // so existing direct calls remain harmless.
        }

        public static void PlayClearVibration()
        {
            AudioFeedbackManager manager = EnsureExists();
            if (!manager.IsVibrationEnabled())
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#else
            if (Application.isMobilePlatform)
            {
                Handheld.Vibrate();
            }
#endif
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            if (sfxSource != null)
            {
                return;
            }

            settingsStore = new SettingsStore();
            bgmClip = Resources.Load<AudioClip>(BgmPath);
            cubeMoveClip = Resources.Load<AudioClip>(CubeMovePath);
            cubeUndoClip = Resources.Load<AudioClip>(CubeUndoPath);
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = BgmVolume;
            bgmSource.spatialBlend = 0f;
            bgmSource.clip = bgmClip;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;

            ApplySoundSetting();
        }

        private void ApplySoundSetting()
        {
            bool enabled = IsSoundEnabled();
            AudioListener.volume = enabled ? 1f : 0f;
            if (enabled && bgmSuppressReasons.Count == 0)
            {
                StartBgm();
            }
            else
            {
                StopBgm();
            }
        }

        private void StartBgm()
        {
            if (bgmSource == null || bgmClip == null || bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.Play();
        }

        private void StopBgm()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }

        private bool IsSoundEnabled()
        {
            return settingsStore == null
                || settingsStore.Current == null
                || settingsStore.Current.soundEnabled;
        }

        private bool IsVibrationEnabled()
        {
            return settingsStore == null
                || settingsStore.Current == null
                || settingsStore.Current.vibrationEnabled;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null || !IsSoundEnabled())
            {
                return;
            }

            sfxSource.PlayOneShot(clip, volume);
        }

        private void RegisterButtonInstance(Button button)
        {
            if (button == null)
            {
                return;
            }

            int id = button.GetInstanceID();
            if (!boundButtonIds.Add(id))
            {
                return;
            }
        }

        private void BindButtonsInScene()
        {
            Button[] buttons = FindObjectsOfType<Button>(true);
            foreach (Button button in buttons)
            {
                RegisterButtonInstance(button);
            }
        }

        private void BindCubeControllerInstance(CubeController controller)
        {
            if (boundCubeController == controller)
            {
                return;
            }

            UnbindCurrentCubeController();
            boundCubeController = controller;
            pendingUndoSound = false;
            if (boundCubeController == null)
            {
                return;
            }

            boundCubeController.UndoApplied += HandleUndoApplied;
        }

        private void UnbindCubeControllerInstance(CubeController controller)
        {
            if (boundCubeController != controller)
            {
                return;
            }

            UnbindCurrentCubeController();
        }

        private void UnbindCurrentCubeController()
        {
            if (boundCubeController == null)
            {
                return;
            }

            boundCubeController.UndoApplied -= HandleUndoApplied;
            boundCubeController = null;
            pendingUndoSound = false;
        }

        private void HandleUndoApplied(CubeMove move)
        {
            pendingUndoSound = true;
        }

        private void PlayCubeRotationInstance()
        {
            if (pendingUndoSound)
            {
                pendingUndoSound = false;
                PlayOneShot(cubeUndoClip, CubeUndoVolume);
                return;
            }

            PlayOneShot(cubeMoveClip, CubeMoveVolume);
        }
    }
}
