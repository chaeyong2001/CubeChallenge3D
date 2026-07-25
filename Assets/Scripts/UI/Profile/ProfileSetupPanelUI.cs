using System;
using System.Reflection;
using System.Threading.Tasks;
using CubeChallenge3D.Networking;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Profile
{
    public sealed class ProfileSetupPanelUI
    {
        private static readonly string[] AvatarSpritePaths =
        {
            "UI/Profile/Avatars/profile_avatar_1",
            "UI/Profile/Avatars/profile_avatar_2",
            "UI/Profile/Avatars/profile_avatar_3",
            "UI/Profile/Avatars/profile_avatar_4"
        };

        private readonly PlayerProfileStore profileStore;
        private readonly PlayerProfileApiClient apiClient;
        private readonly Action<PlayerProfile> createdCallback;
        private readonly Button[] avatarButtons = new Button[4];
        private readonly Outline[] avatarOutlines = new Outline[4];
        private readonly Text errorText;
        private readonly InputField nicknameInput;
        private readonly RectTransform panel;
        private readonly CanvasGroup rootGroup;
        private readonly Button startButton;
        private readonly Text startButtonLabel;
        private int selectedAvatarId;
        private bool isSubmitting;
        private string googlePlayGamesPlayerId = string.Empty;
        private string googlePlayDisplayName = string.Empty;

        public ProfileSetupPanelUI(RectTransform parent, PlayerProfileStore store, PlayerProfileApiClient client, Action<PlayerProfile> onCreated)
        {
            profileStore = store;
            apiClient = client;
            createdCallback = onCreated;
            Root = CreateRoot(parent);
            rootGroup = Root.GetComponent<CanvasGroup>();

            panel = CasualUIFactory.CreatePanel(
                Root,
                "CreateProfilePanel",
                new Color(0.035f, 0.07f, 0.1f, 0.96f),
                28);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -255f);
            panel.sizeDelta = new Vector2(850f, 690f);
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(1f, 0.66f, 0.18f, 0.85f);
            panelOutline.effectDistance = new Vector2(4f, -4f);
            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.62f);
            panelShadow.effectDistance = new Vector2(0f, -12f);

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Create Profile", 52, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, -34f, 72f, -80f);
            title.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(title, true);

            Text guide = RuntimeUiFactory.CreateText(panel, "Guide", "Choose your nickname and avatar.", 27, TextAnchor.MiddleCenter);
            SetTopRect(guide.rectTransform, -108f, 42f, -90f);
            guide.color = new Color(1f, 1f, 1f, 0.88f);
            CasualUIStyle.ApplyTextDepth(guide, false);

            nicknameInput = CreateNicknameInput(panel);

            Text avatarLabel = RuntimeUiFactory.CreateText(panel, "AvatarLabel", "Pick your avatar", 28, TextAnchor.MiddleCenter);
            SetTopRect(avatarLabel.rectTransform, -245f, 44f, -100f);
            avatarLabel.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(avatarLabel, false);

            CreateAvatarButtons(panel);

            errorText = RuntimeUiFactory.CreateText(panel, "ErrorText", string.Empty, 24, TextAnchor.MiddleCenter);
            SetTopRect(errorText.rectTransform, -494f, 42f, -90f);
            errorText.color = new Color(1f, 0.48f, 0.36f, 1f);
            errorText.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(errorText, false);

            startButton = RuntimeUiFactory.CreateButton(panel, "CreateProfileButton", "Start", new Vector2(0f, 46f), new Vector2(390f, 86f));
            startButtonLabel = startButton.transform.Find("Label")?.GetComponent<Text>();
            CasualUIStyle.ApplyButton(startButton, CasualUIColor.Blue);
            startButton.onClick.AddListener(CreateProfile);

            ProfileSetupKeyboardGuard keyboardGuard = Root.gameObject.AddComponent<ProfileSetupKeyboardGuard>();
            keyboardGuard.Configure(panel, nicknameInput);

            Text note = RuntimeUiFactory.CreateText(panel, "RankingNote", "This name will appear in rankings.", 22, TextAnchor.MiddleCenter);
            note.rectTransform.anchorMin = new Vector2(0f, 0f);
            note.rectTransform.anchorMax = new Vector2(1f, 0f);
            note.rectTransform.pivot = new Vector2(0.5f, 0f);
            note.rectTransform.anchoredPosition = new Vector2(0f, 148f);
            note.rectTransform.sizeDelta = new Vector2(-100f, 34f);
            note.color = new Color(1f, 0.82f, 0.48f, 0.9f);
            CasualUIStyle.ApplyTextDepth(note, false);

            SelectAvatar(0);
            SetVisible(false);
        }

        public RectTransform Root { get; }

        public void SetGooglePlayContext(string playerId, string displayName)
        {
            googlePlayGamesPlayerId = playerId ?? string.Empty;
            googlePlayDisplayName = displayName ?? string.Empty;
        }

        public void SetVisible(bool visible)
        {
            Root.gameObject.SetActive(visible);
            if (rootGroup != null)
            {
                rootGroup.interactable = visible;
                rootGroup.blocksRaycasts = visible;
            }

            if (visible)
            {
                FocusNicknameInput();
            }
        }

        private static RectTransform CreateRoot(RectTransform parent)
        {
            GameObject rootObject = new GameObject("ProfileSetupRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(parent, false);
            RectTransform rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private InputField CreateNicknameInput(RectTransform parent)
        {
            GameObject inputObject = new GameObject("NicknameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -172f);
            rect.sizeDelta = new Vector2(620f, 72f);

            Image image = inputObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, new Color(0.015f, 0.035f, 0.055f, 0.94f), 22);
            image.raycastTarget = true;
            Outline outline = inputObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.62f, 0.82f, 1f, 0.62f);
            outline.effectDistance = new Vector2(2f, -2f);

            Text text = RuntimeUiFactory.CreateText(rect, "Text", string.Empty, 30, TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(28f, 0f);
            text.rectTransform.offsetMax = new Vector2(-28f, 0f);
            text.color = Color.white;
            text.raycastTarget = false;
            text.supportRichText = false;

            Text placeholder = RuntimeUiFactory.CreateText(rect, "Placeholder", "Nickname", 30, TextAnchor.MiddleLeft);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(28f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-28f, 0f);
            placeholder.color = new Color(1f, 1f, 1f, 0.42f);
            placeholder.raycastTarget = false;

            InputField input = inputObject.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = image;
            input.interactable = true;
            input.enabled = true;
            input.characterLimit = 15;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Standard;
            input.shouldHideMobileInput = true;
            input.customCaretColor = true;
            input.caretColor = new Color(1f, 0.86f, 0.32f, 1f);
            input.caretBlinkRate = 0.85f;
            input.caretWidth = 5;
            input.selectionColor = new Color(0.28f, 0.58f, 1f, 0.55f);
            return input;
        }

        private void CreateAvatarButtons(RectTransform panel)
        {
            string[] labels = { "A1", "A2", "A3", "A4" };
            for (int i = 0; i < avatarButtons.Length; i++)
            {
                int index = i;
                Button button = RuntimeUiFactory.CreateButton(
                    panel,
                    $"Avatar{index + 1}Button",
                    labels[i],
                    new Vector2(-300f + (index * 200f), 232f),
                    new Vector2(150f, 132f));
                CasualUIStyle.ApplyButton(button, CasualUIColor.Slate);
                Sprite avatarSprite = LoadAvatarSprite(index);
                if (avatarSprite != null)
                {
                    CreateAvatarPortrait(button.GetComponent<RectTransform>(), avatarSprite);
                }

                Text label = button.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.fontSize = 34;
                    label.fontStyle = FontStyle.Bold;
                    label.text = labels[i];
                    label.gameObject.SetActive(avatarSprite == null);
                }

                avatarButtons[i] = button;
                avatarOutlines[i] = button.GetComponent<Outline>();
                button.onClick.AddListener(() => SelectAvatar(index));
            }
        }

        private void SelectAvatar(int avatarId)
        {
            selectedAvatarId = Mathf.Clamp(avatarId, 0, avatarButtons.Length - 1);
            for (int i = 0; i < avatarButtons.Length; i++)
            {
                if (avatarOutlines[i] == null)
                {
                    continue;
                }

                avatarOutlines[i].effectColor = i == selectedAvatarId
                    ? new Color(1f, 0.95f, 0.36f, 1f)
                    : new Color(1f, 0.74f, 0.34f, 0.55f);
                avatarOutlines[i].effectDistance = i == selectedAvatarId
                    ? new Vector2(6f, -6f)
                    : new Vector2(3f, -3f);
            }
        }

        private static Sprite LoadAvatarSprite(int index)
        {
            if (index < 0 || index >= AvatarSpritePaths.Length)
            {
                return null;
            }

            return Resources.Load<Sprite>(AvatarSpritePaths[index]);
        }

        private static void CreateAvatarPortrait(RectTransform parent, Sprite sprite)
        {
            GameObject imageObject = new GameObject("AvatarPortrait", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(112f, 112f);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private async void CreateProfile()
        {
            if (isSubmitting)
            {
                return;
            }

            NicknameValidationResult validation = NicknameValidator.Validate(nicknameInput.text);
            if (!validation.IsValid)
            {
                SetError(validation.Message);
                return;
            }

            SetBusy(true, "Checking...");
            string profileId = Guid.NewGuid().ToString();
            PlayerProfile profile = await TryCreateServerProfile(profileId, validation.NormalizedNickname);
            if (profile == null)
            {
                SetBusy(false, "Start");
                return;
            }

            SetStatus(string.Empty);
            createdCallback?.Invoke(profile);
        }

        private async Task<PlayerProfile> TryCreateServerProfile(string profileId, string nickname)
        {
            if (apiClient == null || !apiClient.HasServerUrl)
            {
                return CreateLocalFallback(nickname, profileId, "Server URL is empty.");
            }

            NicknameCheckResult check = await apiClient.CheckNicknameAsync(nickname);
            if (check.IsUnavailable)
            {
                return CreateLocalFallback(nickname, profileId, check.message);
            }

            if (!check.valid || !check.available)
            {
                SetError(string.IsNullOrWhiteSpace(check.message) ? "Nickname is not available." : check.message);
                return null;
            }

            SetBusy(true, "Creating...");
            PlayerProfileCreateResult create = string.IsNullOrWhiteSpace(googlePlayGamesPlayerId)
                ? await apiClient.CreateProfileAsync(profileId, nickname, selectedAvatarId)
                : await apiClient.CreateProfileWithGooglePlayAsync(profileId, nickname, selectedAvatarId, googlePlayGamesPlayerId);
            if (create.IsUnavailable)
            {
                return CreateLocalFallback(nickname, profileId, create.message);
            }

            if (!create.success || create.profile == null)
            {
                SetError(string.IsNullOrWhiteSpace(create.message) ? "Could not create profile." : create.message);
                return null;
            }

            PlayerProfile profile = profileStore.CreateProfile(
                create.profile.nickname,
                create.profile.avatarId,
                create.profile.profileId,
                true,
                false,
                string.Empty,
                create.profile.createdAt,
                create.profile.updatedAt,
                FirstNonEmpty(create.profile.googlePlayGamesPlayerId, create.profile.googlePlayPlayerId, googlePlayGamesPlayerId),
                googlePlayDisplayName);
            if (profile == null)
            {
                SetError("Could not save profile.");
            }

            return profile;
        }

        private PlayerProfile CreateLocalFallback(string nickname, string profileId, string reason)
        {
            Debug.LogWarning($"[PlayerProfile] Server profile sync unavailable. Creating local pending profile. reason={reason}");
            PlayerProfile profile = profileStore.CreateProfile(
                nickname,
                selectedAvatarId,
                profileId,
                false,
                true,
                reason ?? "Server unavailable.",
                string.Empty,
                string.Empty,
                googlePlayGamesPlayerId,
                googlePlayDisplayName);
            if (profile == null)
            {
                SetError("Could not save local profile.");
                return null;
            }

            SetStatus("Saved locally. Online sync will be tried later.");
            return profile;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private void SetBusy(bool busy, string buttonLabel)
        {
            isSubmitting = busy;
            if (startButton != null)
            {
                startButton.interactable = !busy;
            }

            if (startButtonLabel != null)
            {
                startButtonLabel.text = buttonLabel;
            }

            if (busy)
            {
                SetStatus(buttonLabel == "Checking..." ? "Checking nickname..." : "Creating profile...");
            }
        }

        private void SetError(string message)
        {
            errorText.color = new Color(1f, 0.48f, 0.36f, 1f);
            errorText.text = message;
        }

        private void SetStatus(string message)
        {
            errorText.color = new Color(1f, 0.82f, 0.48f, 0.95f);
            errorText.text = message;
        }

        private void FocusNicknameInput()
        {
            if (nicknameInput == null || !nicknameInput.gameObject.activeInHierarchy)
            {
                return;
            }

            nicknameInput.Select();
            nicknameInput.ActivateInputField();
        }

        private static void SetTopRect(RectTransform rect, float y, float height, float horizontalPadding)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(horizontalPadding, height);
        }

        private sealed class ProfileSetupKeyboardGuard : MonoBehaviour
        {
            private const float KeyboardLift = 360f;

            private static readonly FieldInfo KeyboardField = typeof(InputField).GetField(
                "m_Keyboard",
                BindingFlags.Instance | BindingFlags.NonPublic);

            private RectTransform panel;
            private InputField nicknameInput;
            private Vector2 originalPanelPosition;
            private string latestKeyboardText = string.Empty;
            private bool wasKeyboardVisible;
            private bool configured;

            public void Configure(RectTransform panelRect, InputField input)
            {
                panel = panelRect;
                nicknameInput = input;
                originalPanelPosition = panel != null ? panel.anchoredPosition : Vector2.zero;
                latestKeyboardText = nicknameInput != null ? nicknameInput.text : string.Empty;
                wasKeyboardVisible = false;
                configured = panel != null && nicknameInput != null;
            }

            private void OnEnable()
            {
                if (panel != null)
                {
                    panel.anchoredPosition = originalPanelPosition;
                }

                CommitLatestKeyboardText();
                wasKeyboardVisible = false;
            }

            private void OnDisable()
            {
                if (panel != null)
                {
                    panel.anchoredPosition = originalPanelPosition;
                }
            }

            private void Update()
            {
                if (!configured)
                {
                    return;
                }

#if UNITY_ANDROID || UNITY_IOS
                bool keyboardVisible = TouchScreenKeyboard.visible;
                CaptureLatestKeyboardText(keyboardVisible);
                if (wasKeyboardVisible && !keyboardVisible)
                {
                    CommitLatestKeyboardText();
                }

                wasKeyboardVisible = keyboardVisible;
                bool shouldLift = nicknameInput.isFocused && keyboardVisible;
                Vector2 target = shouldLift
                    ? originalPanelPosition + new Vector2(0f, KeyboardLift)
                    : originalPanelPosition;
                panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, target, Time.unscaledDeltaTime * 18f);
#else
                panel.anchoredPosition = originalPanelPosition;
#endif
            }

            private void CaptureLatestKeyboardText(bool keyboardVisible)
            {
                if (!keyboardVisible)
                {
                    return;
                }

                TouchScreenKeyboard keyboard = KeyboardField?.GetValue(nicknameInput) as TouchScreenKeyboard;
                if (keyboard == null)
                {
                    return;
                }

                latestKeyboardText = keyboard.text ?? string.Empty;
            }

            private void CommitLatestKeyboardText()
            {
                if (nicknameInput == null || latestKeyboardText == null || nicknameInput.text == latestKeyboardText)
                {
                    return;
                }

                nicknameInput.text = latestKeyboardText;
                nicknameInput.caretPosition = latestKeyboardText.Length;
                nicknameInput.selectionAnchorPosition = latestKeyboardText.Length;
                nicknameInput.selectionFocusPosition = latestKeyboardText.Length;
            }
        }
    }
}
