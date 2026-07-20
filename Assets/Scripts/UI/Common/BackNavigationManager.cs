using System;
using System.Reflection;
using CubeChallenge3D.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CubeChallenge3D.UI.Common
{
    public sealed class BackNavigationManager : MonoBehaviour
    {
        private static BackNavigationManager instance;
        private static Func<bool> currentHandler;
        private static string currentScreen = "Unknown";
        private static readonly FieldInfo InputFieldKeyboardField = typeof(InputField).GetField(
            "m_Keyboard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static GameObject lastSelectedInputObject;
        private static float lastTextInputInteractionTime;

        private GameObject exitPopupRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            EnsureExists();
            LockPortrait();
        }

        public static void EnsureExists()
        {
            if (instance != null)
            {
                return;
            }

            GameObject root = new GameObject("BackNavigationManager");
            instance = root.AddComponent<BackNavigationManager>();
            DontDestroyOnLoad(root);
        }

        public static void SetCurrentHandler(string screenName, Func<bool> handler)
        {
            EnsureExists();
            currentScreen = string.IsNullOrWhiteSpace(screenName) ? "Unknown" : screenName;
            currentHandler = handler;
        }

        public static void ClearCurrentHandler(Func<bool> handler)
        {
            if (currentHandler == handler)
            {
                currentHandler = null;
                currentScreen = "Unknown";
            }
        }

        public static void ShowExitConfirmation()
        {
            EnsureExists();
            instance.ShowExitConfirmationInternal();
        }

        private static void LockPortrait()
        {
#if UNITY_ANDROID
            // Android launch orientation is controlled by the manifest. Forcing Screen.orientation
            // before the first scene can create a visible startup rotation on some devices.
            Debug.Log("[Orientation] Android portrait is controlled by AndroidManifest.");
#else
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
            Debug.Log("[Orientation] locked=Portrait");
#endif
        }

        private void Update()
        {
            TrackActiveTextInput();

            if (!IsBackPressedThisFrame())
            {
                return;
            }

            bool popupOpen = exitPopupRoot != null && exitPopupRoot.activeInHierarchy;
            Debug.Log($"[BackNavigation] currentScreen={currentScreen} popupOpen={popupOpen} action=Pressed");

            if (TryConsumeBackForActiveTextInput())
            {
                Debug.Log("[BackNavigation] action=DismissTextInput");
                return;
            }

            if (popupOpen)
            {
                exitPopupRoot.SetActive(false);
                Debug.Log("[BackNavigation] Popup closed");
                return;
            }

            if (currentHandler != null && currentHandler.Invoke())
            {
                return;
            }

            Debug.Log("[BackNavigation] currentScreen=Fallback popupOpen=False action=MainMenu");
            SceneLoader.LoadMainMenu();
        }

        private static bool IsBackPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

            return false;
        }

        private static void TrackActiveTextInput()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selectedObject = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            InputField inputField = selectedObject != null ? selectedObject.GetComponent<InputField>() : null;
            if (inputField != null)
            {
                lastSelectedInputObject = selectedObject;
                lastTextInputInteractionTime = Time.unscaledTime;
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            if (TouchScreenKeyboard.visible)
            {
                lastTextInputInteractionTime = Time.unscaledTime;
            }
#endif
        }

        private static bool TryConsumeBackForActiveTextInput()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selectedObject = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            InputField inputField = selectedObject != null ? selectedObject.GetComponent<InputField>() : null;
            if (inputField == null && lastSelectedInputObject != null)
            {
                inputField = lastSelectedInputObject.GetComponent<InputField>();
            }

            bool recentlyEditingText = Time.unscaledTime - lastTextInputInteractionTime < 0.8f;
#if UNITY_ANDROID || UNITY_IOS
            recentlyEditingText = recentlyEditingText || TouchScreenKeyboard.visible;
#endif

            if (inputField == null)
            {
                return recentlyEditingText;
            }

            CommitMobileKeyboardText(inputField);
            inputField.DeactivateInputField();
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }

            lastSelectedInputObject = null;
            lastTextInputInteractionTime = 0f;
            return true;
        }

        private static void CommitMobileKeyboardText(InputField inputField)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (inputField == null)
            {
                return;
            }

            TouchScreenKeyboard keyboard = InputFieldKeyboardField?.GetValue(inputField) as TouchScreenKeyboard;
            if (keyboard == null)
            {
                return;
            }

            string keyboardText = keyboard.text ?? string.Empty;
            if (inputField.text == keyboardText)
            {
                return;
            }

            inputField.text = keyboardText;
            inputField.caretPosition = keyboardText.Length;
            inputField.selectionAnchorPosition = keyboardText.Length;
            inputField.selectionFocusPosition = keyboardText.Length;
#endif
        }

        private void ShowExitConfirmationInternal()
        {
            if (exitPopupRoot == null)
            {
                BuildExitPopup();
            }

            exitPopupRoot.SetActive(true);
            exitPopupRoot.transform.SetAsLastSibling();
            Debug.Log("[BackNavigation] currentScreen=MainMenu popupOpen=True action=ShowExitConfirm");
        }

        private void BuildExitPopup()
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(transform, "AndroidBackExitPopupCanvas", 5000, 0f);
            exitPopupRoot = canvas.gameObject;

            Image dim = exitPopupRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                exitPopupRoot.transform,
                "Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 430f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Exit Game?", 46, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.05f, 0.70f);
            title.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.88f, 0.55f, 1f);

            Text body = RuntimeUiFactory.CreateText(panel, "Body", "Do you want to quit the game?", 30, TextAnchor.MiddleCenter);
            body.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
            body.rectTransform.anchorMax = new Vector2(0.92f, 0.66f);
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = Vector2.zero;

            Button cancel = RuntimeUiFactory.CreateButton(panel, "CancelButton", "Cancel", new Vector2(-175f, -130f), new Vector2(260f, 76f));
            cancel.onClick.AddListener(() =>
            {
                exitPopupRoot.SetActive(false);
                Debug.Log("[BackNavigation] Popup closed");
            });

            Button exit = RuntimeUiFactory.CreateButton(panel, "ExitButton", "Exit", new Vector2(175f, -130f), new Vector2(260f, 76f));
            exit.onClick.AddListener(() =>
            {
                Debug.Log("[BackNavigation] currentScreen=MainMenu popupOpen=True action=Quit");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            exitPopupRoot.SetActive(false);
        }
    }
}
