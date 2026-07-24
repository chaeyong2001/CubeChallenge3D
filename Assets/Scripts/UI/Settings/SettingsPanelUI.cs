using System;
using CubeChallenge3D.Auth;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Core;
using CubeChallenge3D.Cube.Debugging;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Networking;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Settings
{
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        private static readonly string[] AvatarSpritePaths =
        {
            "UI/Profile/Avatars/profile_avatar_1",
            "UI/Profile/Avatars/profile_avatar_2",
            "UI/Profile/Avatars/profile_avatar_3",
            "UI/Profile/Avatars/profile_avatar_4"
        };

        private const string SettingsIconRoot = "UI/Settings/Icons/";
        private const float SettingsContentHeight = 1210f;

        private SettingsStore settingsStore;
        private PlayerProfileStore profileStore;
        private PlayerProfileApiClient profileApiClient;
        private AccountLinkApiClient accountLinkApiClient;
        private GooglePlayGamesAuthService googlePlayGames;

        private GameObject root;
        private Image avatarImage;
        private Text nicknameValue;
        private Text googlePlayValue;
        private Text googlePlayButtonLabel;
        private Button avatarEditButton;
        private Image vibrationIcon;
        private Image soundIcon;
        private Image pushIcon;
        private Image languageIcon;
        private Text vibrationValue;
        private Text soundValue;
        private Text pushValue;
        private Text languageValue;
        private Text messageText;
        private GameObject nicknamePopupRoot;
        private InputField nicknameInput;
        private Button nicknameOkButton;
        private Text nicknamePopupMessage;
        private GameObject avatarPopupRoot;
        private Text avatarPopupMessage;
        private GameObject languagePopupRoot;
        private bool isSubmittingNickname;
        private bool isSubmittingAvatar;
        private readonly NicknamePopupKeyboardGuard nicknameKeyboardGuard = new NicknamePopupKeyboardGuard();

        public void Initialize(
            SettingsStore store,
            CubeControlModeController controlController,
            CubeRuntimeDiagnostics runtimeDiagnostics)
        {
            settingsStore = store ?? new SettingsStore();
            LocalizationManager.Instance?.SetLanguageFromCode(settingsStore.Current.languageCode);
            profileStore = new PlayerProfileStore();
            profileApiClient = new PlayerProfileApiClient(
                settingsStore.Current.rankingApiBaseUrl,
                settingsStore.Current.rankingRequestTimeoutSeconds);
            accountLinkApiClient = new AccountLinkApiClient(
                settingsStore.Current.rankingApiBaseUrl,
                settingsStore.Current.rankingRequestTimeoutSeconds);
            googlePlayGames = new GooglePlayGamesAuthService();
            BuildUi();
            RefreshLabels();
        }

        public void Show()
        {
            if (root == null)
            {
                BuildUi();
            }

            RefreshLabels();
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            GameManager.Instance?.SetState(CubeChallenge3D.Core.AppState.MainMenu);
        }

        private void Update()
        {
            nicknameKeyboardGuard.Tick();
        }

        private void BuildUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = RuntimeUiFactory.CreateCanvas(transform, "SettingsCanvas", 1540, 0f);
            root = canvas.gameObject;
            CasualUIFactory.CreateBackdrop(root.transform, "Background");

            RectTransform screen = CreateSafeArea(root.transform);
            CreateHeader(screen);

            RectTransform panel = CasualUIFactory.CreatePanel(
                screen,
                "SettingsPanel",
                new Color(0.028f, 0.085f, 0.18f, 0.96f),
                30);
            panel.anchorMin = new Vector2(0.055f, 0.135f);
            panel.anchorMax = new Vector2(0.945f, 0.765f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            AddGoldOutline(panel, new Vector2(4f, -4f));
            RectTransform content = CreateScrollableSettingsContent(panel);

            Text profileTitle = RuntimeUiFactory.CreateText(content, "ProfileTitle", T("profile"), 34, TextAnchor.MiddleLeft);
            profileTitle.fontStyle = FontStyle.Bold;
            profileTitle.color = new Color(1f, 0.82f, 0.24f, 1f);
            profileTitle.rectTransform.anchorMin = new Vector2(0.055f, 0.925f);
            profileTitle.rectTransform.anchorMax = new Vector2(0.4f, 1f);
            profileTitle.rectTransform.offsetMin = Vector2.zero;
            profileTitle.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(profileTitle, true);

            CreateProfileSection(content);
            CreateOptionRows(content);
            CreateBottom(screen);
            CreateNicknamePopup(screen);
            CreateAvatarPopup(screen);
            Hide();
        }

        private static RectTransform CreateSafeArea(Transform parent)
        {
            GameObject safeObject = new GameObject("SettingsSafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(parent, false);
            return safeObject.GetComponent<RectTransform>();
        }

        private void CreateHeader(RectTransform screen)
        {
            Text title = RuntimeUiFactory.CreateText(screen, "SettingsTitle", T("settings"), 72, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.78f, 0.18f, 1f);
            title.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -222f);
            title.rectTransform.sizeDelta = new Vector2(0f, 106f);
            CasualUIStyle.ApplyTextDepth(title, true);

            Text subtitle = RuntimeUiFactory.CreateText(screen, "SettingsSubtitle", T("customize_experience"), 28, TextAnchor.MiddleCenter);
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.color = new Color(1f, 0.78f, 0.25f, 1f);
            subtitle.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -364f);
            subtitle.rectTransform.sizeDelta = new Vector2(0f, 44f);
            CasualUIStyle.ApplyTextDepth(subtitle, false);
            CreateAccent(screen, new Vector2(-270f, -356f));
            CreateAccent(screen, new Vector2(270f, -356f));
        }

        private void CreateProfileSection(RectTransform panel)
        {
            RectTransform section = CreateRowPanel(panel, "ProfileSection", new Vector2(0.045f, 0.61f), new Vector2(0.955f, 0.91f));
            avatarImage = CreateAvatar(section);
            avatarEditButton = RuntimeUiFactory.CreateButton(section, "AvatarEditButton", T("edit"), Vector2.zero, new Vector2(116f, 42f));
            Place(avatarEditButton.GetComponent<RectTransform>(), new Vector2(0.105f, 0.04f), new Vector2(0.245f, 0.19f));
            Text avatarEditLabel = avatarEditButton.GetComponentInChildren<Text>();
            if (avatarEditLabel != null)
            {
                avatarEditLabel.fontSize = 20;
                avatarEditLabel.fontStyle = FontStyle.Bold;
            }

            avatarEditButton.onClick.AddListener(ShowAvatarPopup);

            CreateSmallLabel(section, "NicknameLabel", T("nickname"), new Vector2(0.34f, 0.63f), new Vector2(0.68f, 0.85f));
            nicknameValue = RuntimeUiFactory.CreateText(section, "NicknameValue", string.Empty, 40, TextAnchor.MiddleLeft);
            nicknameValue.fontStyle = FontStyle.Bold;
            nicknameValue.color = new Color(1f, 0.95f, 0.78f, 1f);
            nicknameValue.rectTransform.anchorMin = new Vector2(0.34f, 0.43f);
            nicknameValue.rectTransform.anchorMax = new Vector2(0.69f, 0.68f);
            nicknameValue.rectTransform.offsetMin = Vector2.zero;
            nicknameValue.rectTransform.offsetMax = Vector2.zero;
            nicknameValue.resizeTextForBestFit = true;
            nicknameValue.resizeTextMinSize = 24;
            nicknameValue.resizeTextMaxSize = 40;
            CasualUIStyle.ApplyTextDepth(nicknameValue, true);

            Button edit = RuntimeUiFactory.CreateButton(section, "NicknameEditButton", T("edit"), Vector2.zero, new Vector2(190f, 76f));
            Place(edit.GetComponent<RectTransform>(), new Vector2(0.78f, 0.58f), new Vector2(0.96f, 0.83f));
            edit.onClick.AddListener(ShowNicknamePopup);

            CreateDivider(section, 0.47f);
            CreateSmallLabel(section, "GooglePlayLabel", T("google_play"), new Vector2(0.34f, 0.25f), new Vector2(0.68f, 0.43f));
            googlePlayValue = RuntimeUiFactory.CreateText(section, "GooglePlayValue", string.Empty, 28, TextAnchor.MiddleLeft);
            googlePlayValue.fontStyle = FontStyle.Bold;
            googlePlayValue.color = new Color(1f, 0.95f, 0.80f, 1f);
            googlePlayValue.rectTransform.anchorMin = new Vector2(0.34f, 0.06f);
            googlePlayValue.rectTransform.anchorMax = new Vector2(0.70f, 0.28f);
            googlePlayValue.rectTransform.offsetMin = Vector2.zero;
            googlePlayValue.rectTransform.offsetMax = Vector2.zero;
            googlePlayValue.resizeTextForBestFit = true;
            googlePlayValue.resizeTextMinSize = 18;
            googlePlayValue.resizeTextMaxSize = 28;
            CasualUIStyle.ApplyTextDepth(googlePlayValue, false);

            Button connect = RuntimeUiFactory.CreateButton(section, "GooglePlayConnectButton", string.Empty, Vector2.zero, new Vector2(218f, 68f));
            Place(connect.GetComponent<RectTransform>(), new Vector2(0.73f, 0.08f), new Vector2(0.96f, 0.33f));
            googlePlayButtonLabel = connect.GetComponentInChildren<Text>();
            connect.onClick.AddListener(ConnectGooglePlay);
        }

        private void CreateOptionRows(RectTransform panel)
        {
            CreateToggleRow(panel, "Vibration", T("vibration"), 0.49f, ToggleVibration, out vibrationValue, out vibrationIcon);
            CreateToggleRow(panel, "Sound", T("sound"), 0.38f, ToggleSound, out soundValue, out soundIcon);
            CreateToggleRow(panel, "Push", T("push_notifications"), 0.27f, TogglePush, out pushValue, out pushIcon);
            CreateLanguageRow(panel, 0.16f);
        }

        private void CreateToggleRow(
            RectTransform panel,
            string name,
            string label,
            float y,
            UnityEngine.Events.UnityAction action,
            out Text valueText,
            out Image iconImage)
        {
            RectTransform row = CreateRowPanel(panel, $"{name}Row", new Vector2(0.045f, y), new Vector2(0.955f, y + 0.09f));
            iconImage = CreateSettingsIcon(row, null);
            Text labelText = RuntimeUiFactory.CreateText(row, "Label", label, 32, TextAnchor.MiddleLeft);
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = new Color(1f, 0.93f, 0.78f, 1f);
            labelText.rectTransform.anchorMin = new Vector2(0.16f, 0f);
            labelText.rectTransform.anchorMax = new Vector2(0.58f, 1f);
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(labelText, true);

            Button toggle = RuntimeUiFactory.CreateButton(row, $"{name}Toggle", string.Empty, Vector2.zero, new Vector2(168f, 60f));
            Place(toggle.GetComponent<RectTransform>(), new Vector2(0.77f, 0.18f), new Vector2(0.96f, 0.82f));
            toggle.onClick.AddListener(action);
            valueText = RuntimeUiFactory.CreateText(toggle.GetComponent<RectTransform>(), "Value", string.Empty, 27, TextAnchor.MiddleCenter);
            valueText.fontStyle = FontStyle.Bold;
            valueText.rectTransform.offsetMin = new Vector2(12f, 0f);
            valueText.rectTransform.offsetMax = new Vector2(-62f, 0f);
            CasualUIStyle.ApplyTextDepth(valueText, true);
            CreateToggleKnob(toggle.GetComponent<RectTransform>());
        }

        private void CreateLanguageRow(RectTransform panel, float y)
        {
            RectTransform row = CreateRowPanel(panel, "LanguageRow", new Vector2(0.045f, y), new Vector2(0.955f, y + 0.09f));
            languageIcon = CreateSettingsIcon(row, LoadSettingsIcon("settings_language_on"));
            Text label = RuntimeUiFactory.CreateText(row, "Label", T("language"), 32, TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 0.93f, 0.78f, 1f);
            label.rectTransform.anchorMin = new Vector2(0.16f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.55f, 1f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(label, true);

            RectTransform selector = CasualUIFactory.CreatePanel(row, "LanguageSelector", new Color(0.02f, 0.22f, 0.60f, 1f), 24);
            selector.anchorMin = new Vector2(0.58f, 0.15f);
            selector.anchorMax = new Vector2(0.96f, 0.85f);
            selector.offsetMin = Vector2.zero;
            selector.offsetMax = Vector2.zero;
            AddGoldOutline(selector, new Vector2(2f, -2f));
            languageValue = RuntimeUiFactory.CreateText(selector, "Value", "English", 28, TextAnchor.MiddleCenter);
            languageValue.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(languageValue, true);
            Button selectorButton = selector.gameObject.AddComponent<Button>();
            selectorButton.targetGraphic = selector.GetComponent<Image>();
            selectorButton.onClick.AddListener(ShowLanguagePopup);
        }

        private void CreateBottom(RectTransform screen)
        {
            messageText = RuntimeUiFactory.CreateText(screen, "SettingsMessage", string.Empty, 22, TextAnchor.MiddleCenter);
            messageText.color = new Color(1f, 0.82f, 0.36f, 1f);
            messageText.rectTransform.anchorMin = new Vector2(0.06f, 0.105f);
            messageText.rectTransform.anchorMax = new Vector2(0.94f, 0.135f);
            messageText.rectTransform.offsetMin = Vector2.zero;
            messageText.rectTransform.offsetMax = Vector2.zero;

            Button back = RuntimeUiFactory.CreateButton(screen, "BackButton", T("back"), Vector2.zero, new Vector2(440f, 82f));
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 88f);
            back.onClick.AddListener(Hide);

            Text version = RuntimeUiFactory.CreateText(screen, "VersionText", $"Version {Application.version}", 22, TextAnchor.MiddleCenter);
            version.color = new Color(0.76f, 0.78f, 0.88f, 1f);
            version.rectTransform.anchorMin = new Vector2(0f, 0f);
            version.rectTransform.anchorMax = new Vector2(1f, 0f);
            version.rectTransform.pivot = new Vector2(0.5f, 0f);
            version.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            version.rectTransform.sizeDelta = new Vector2(-80f, 34f);
        }

        private void CreateNicknamePopup(RectTransform screen)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(root.transform, "NicknameChangePopupCanvas", 1580, 0f);
            nicknamePopupRoot = canvas.gameObject;
            Image blocker = nicknamePopupRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                nicknamePopupRoot.transform,
                "Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -170f),
                new Vector2(700f, 520f));
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.10f, 0.25f, 0.98f);
            AddGoldOutline(panel, new Vector2(4f, -4f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", T("change_nickname"), 38, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.82f, 0.30f, 1f);
            title.rectTransform.anchorMin = new Vector2(0.12f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(0.88f, 0.96f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(title, true);

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "X", Vector2.zero, new Vector2(76f, 76f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.86f, 0.84f), new Vector2(0.97f, 0.97f));
            close.onClick.AddListener(HideNicknamePopup);

            nicknameInput = CreateInput(panel, "NicknameInput", new Vector2(0.10f, 0.64f), new Vector2(0.90f, 0.78f));
            Text info = RuntimeUiFactory.CreateText(
                panel,
                "Info",
                T("nickname_ticket_info"),
                24,
                TextAnchor.MiddleCenter);
            info.color = new Color(0.88f, 0.92f, 1f, 1f);
            info.rectTransform.anchorMin = new Vector2(0.09f, 0.42f);
            info.rectTransform.anchorMax = new Vector2(0.91f, 0.61f);
            info.rectTransform.offsetMin = Vector2.zero;
            info.rectTransform.offsetMax = Vector2.zero;

            nicknamePopupMessage = RuntimeUiFactory.CreateText(panel, "Message", string.Empty, 23, TextAnchor.MiddleCenter);
            nicknamePopupMessage.color = new Color(1f, 0.74f, 0.34f, 1f);
            nicknamePopupMessage.rectTransform.anchorMin = new Vector2(0.08f, 0.24f);
            nicknamePopupMessage.rectTransform.anchorMax = new Vector2(0.92f, 0.40f);
            nicknamePopupMessage.rectTransform.offsetMin = Vector2.zero;
            nicknamePopupMessage.rectTransform.offsetMax = Vector2.zero;

            Button cancel = RuntimeUiFactory.CreateButton(panel, "CancelButton", T("cancel"), Vector2.zero, new Vector2(250f, 78f));
            Place(cancel.GetComponent<RectTransform>(), new Vector2(0.10f, 0.06f), new Vector2(0.46f, 0.21f));
            ApplyCancelButtonStyle(cancel);
            cancel.onClick.AddListener(HideNicknamePopup);

            nicknameOkButton = RuntimeUiFactory.CreateButton(panel, "OkButton", T("ok"), Vector2.zero, new Vector2(250f, 78f));
            Place(nicknameOkButton.GetComponent<RectTransform>(), new Vector2(0.54f, 0.06f), new Vector2(0.90f, 0.21f));
            nicknameOkButton.onClick.AddListener(SubmitNicknameChange);
            nicknameKeyboardGuard.Configure(panel, nicknameInput);
            nicknamePopupRoot.SetActive(false);
        }

        private void CreateAvatarPopup(RectTransform screen)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(root.transform, "AvatarChangePopupCanvas", 1580, 0f);
            avatarPopupRoot = canvas.gameObject;
            Image blocker = avatarPopupRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                avatarPopupRoot.transform,
                "Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -130f),
                new Vector2(720f, 560f));
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.10f, 0.25f, 0.98f);
            AddGoldOutline(panel, new Vector2(4f, -4f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", T("choose_avatar"), 40, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.82f, 0.30f, 1f);
            title.rectTransform.anchorMin = new Vector2(0.12f, 0.84f);
            title.rectTransform.anchorMax = new Vector2(0.88f, 0.97f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(title, true);

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "X", Vector2.zero, new Vector2(76f, 76f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.86f, 0.85f), new Vector2(0.97f, 0.98f));
            close.onClick.AddListener(HideAvatarPopup);

            for (int i = 0; i < AvatarSpritePaths.Length; i++)
            {
                int avatarId = i;
                bool left = i % 2 == 0;
                bool top = i < 2;
                float xMin = left ? 0.14f : 0.54f;
                float xMax = left ? 0.38f : 0.78f;
                float yMin = top ? 0.48f : 0.19f;
                float yMax = top ? 0.75f : 0.46f;
                Button button = CreateAvatarChoiceButton(panel, avatarId, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                button.onClick.AddListener(() => SelectAvatar(avatarId));
            }

            avatarPopupMessage = RuntimeUiFactory.CreateText(panel, "Message", "Avatar can be changed anytime.", 23, TextAnchor.MiddleCenter);
            avatarPopupMessage.color = new Color(1f, 0.82f, 0.36f, 1f);
            avatarPopupMessage.rectTransform.anchorMin = new Vector2(0.08f, 0.06f);
            avatarPopupMessage.rectTransform.anchorMax = new Vector2(0.92f, 0.15f);
            avatarPopupMessage.rectTransform.offsetMin = Vector2.zero;
            avatarPopupMessage.rectTransform.offsetMax = Vector2.zero;
            avatarPopupRoot.SetActive(false);
        }

        private Button CreateAvatarChoiceButton(RectTransform panel, int avatarId, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = new GameObject($"AvatarChoice{avatarId + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(panel, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = LoadAvatarSprite(avatarId);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.30f, 0.90f);
            outline.effectDistance = new Vector2(4f, -4f);

            Button button = buttonObject.GetComponent<Button>();
            return button;
        }

        private Image CreateAvatar(RectTransform section)
        {
            GameObject avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Outline));
            avatarObject.transform.SetParent(section, false);
            RectTransform avatar = avatarObject.GetComponent<RectTransform>();
            avatar.anchorMin = new Vector2(0.065f, 0.20f);
            avatar.anchorMax = new Vector2(0.285f, 0.82f);
            avatar.offsetMin = Vector2.zero;
            avatar.offsetMax = Vector2.zero;
            Image image = avatarObject.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            Outline outline = avatarObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.84f, 0.44f, 0.90f);
            outline.effectDistance = new Vector2(4f, -4f);
            return image;
        }

        private static RectTransform CreateRowPanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform row = CasualUIFactory.CreatePanel(parent, name, new Color(0.028f, 0.10f, 0.22f, 0.94f), 24);
            row.anchorMin = anchorMin;
            row.anchorMax = anchorMax;
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            return row;
        }

        private static RectTransform CreateScrollableSettingsContent(RectTransform panel)
        {
            Mask mask = panel.gameObject.GetComponent<Mask>() ?? panel.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            ScrollRect scroll = panel.gameObject.GetComponent<ScrollRect>() ?? panel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject contentObject = new GameObject("SettingsPanelContent", typeof(RectTransform));
            contentObject.transform.SetParent(panel, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, SettingsContentHeight);
            scroll.viewport = panel;
            scroll.content = content;
            return content;
        }

        private static void CreateSmallLabel(RectTransform parent, string name, string label, Vector2 min, Vector2 max)
        {
            Text text = RuntimeUiFactory.CreateText(parent, name, label, 24, TextAnchor.MiddleLeft);
            text.color = new Color(0.78f, 0.84f, 1f, 1f);
            text.fontStyle = FontStyle.Bold;
            text.rectTransform.anchorMin = min;
            text.rectTransform.anchorMax = max;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }

        private static Image CreateSettingsIcon(RectTransform row, Sprite sprite)
        {
            GameObject iconObject = new GameObject("SettingIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(row, false);
            RectTransform holder = iconObject.GetComponent<RectTransform>();
            holder.anchorMin = new Vector2(0.035f, 0.16f);
            holder.anchorMax = new Vector2(0.135f, 0.84f);
            holder.offsetMin = Vector2.zero;
            holder.offsetMax = Vector2.zero;
            Image image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private static void CreateToggleKnob(RectTransform toggle)
        {
            RectTransform knob = CasualUIFactory.CreatePanel(toggle, "Knob", new Color(0.92f, 0.94f, 0.92f, 1f), 26);
            knob.anchorMin = new Vector2(0.68f, 0.09f);
            knob.anchorMax = new Vector2(0.98f, 0.91f);
            knob.offsetMin = Vector2.zero;
            knob.offsetMax = Vector2.zero;
            knob.GetComponent<Image>().raycastTarget = false;
        }

        private static InputField CreateInput(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform inputRoot = CasualUIFactory.CreatePanel(parent, name, new Color(0.012f, 0.04f, 0.11f, 1f), 20);
            inputRoot.anchorMin = anchorMin;
            inputRoot.anchorMax = anchorMax;
            inputRoot.offsetMin = Vector2.zero;
            inputRoot.offsetMax = Vector2.zero;
            InputField input = inputRoot.gameObject.AddComponent<InputField>();

            Text text = RuntimeUiFactory.CreateText(inputRoot, "Text", string.Empty, 30, TextAnchor.MiddleLeft);
            text.rectTransform.offsetMin = new Vector2(26f, 0f);
            text.rectTransform.offsetMax = new Vector2(-46f, 0f);
            text.color = Color.white;
            input.textComponent = text;

            Text placeholder = RuntimeUiFactory.CreateText(inputRoot, "Placeholder", T("nickname_placeholder"), 30, TextAnchor.MiddleLeft);
            placeholder.rectTransform.offsetMin = new Vector2(26f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-46f, 0f);
            placeholder.color = new Color(0.72f, 0.78f, 0.90f, 0.70f);
            input.placeholder = placeholder;
            input.characterLimit = 15;
            input.interactable = true;
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

        private static void CreateAccent(RectTransform parent, Vector2 position)
        {
            GameObject accentObject = new GameObject("TitleAccent", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(parent, false);
            RectTransform accent = accentObject.GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0.5f, 1f);
            accent.anchorMax = new Vector2(0.5f, 1f);
            accent.pivot = new Vector2(0.5f, 0.5f);
            accent.anchoredPosition = position;
            accent.sizeDelta = new Vector2(150f, 3f);
            Image image = accentObject.GetComponent<Image>();
            image.color = new Color(1f, 0.65f, 0.12f, 0.58f);
            image.raycastTarget = false;
        }

        private static void CreateDivider(RectTransform parent, float y)
        {
            GameObject dividerObject = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerObject.transform.SetParent(parent, false);
            RectTransform divider = dividerObject.GetComponent<RectTransform>();
            divider.anchorMin = new Vector2(0.32f, y);
            divider.anchorMax = new Vector2(0.93f, y);
            divider.sizeDelta = new Vector2(0f, 2f);
            Image image = dividerObject.GetComponent<Image>();
            image.color = new Color(0.36f, 0.58f, 0.88f, 0.28f);
            image.raycastTarget = false;
        }

        private static void AddGoldOutline(RectTransform rect, Vector2 distance)
        {
            Outline outline = rect.GetComponent<Outline>() ?? rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.70f, 0.20f, 0.90f);
            outline.effectDistance = distance;
            Shadow shadow = rect.GetComponent<Shadow>() ?? rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -8f);
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void RefreshLabels()
        {
            if (settingsStore?.Current == null)
            {
                return;
            }

            settingsStore.Load();
            LocalizationManager.Instance?.SetLanguageFromCode(settingsStore.Current.languageCode);
            profileStore.ReloadFromDisk();
            PlayerProfile profile = profileStore.Current;
            nicknameValue.text = profile != null ? profile.nickname : "Player";
            int avatarId = profile != null ? Mathf.Clamp(profile.avatarId, 0, AvatarSpritePaths.Length - 1) : 0;
            avatarImage.sprite = LoadAvatarSprite(avatarId);

            bool linkedGooglePlay = profile != null && profile.linkedGooglePlay;
            googlePlayValue.text = linkedGooglePlay
                ? FirstNonEmpty(profile.googlePlayPlayerId, T("connected"))
                : T("not_connected");
            googlePlayButtonLabel.text = linkedGooglePlay ? T("connected") : T("connect");

            vibrationValue.text = settingsStore.Current.vibrationEnabled ? T("on") : T("off");
            soundValue.text = settingsStore.Current.soundEnabled ? T("on") : T("off");
            pushValue.text = settingsStore.Current.pushNotificationsEnabled ? T("on") : T("off");
            languageValue.text = GetLanguageDisplayName(settingsStore.Current.languageCode);
            SetSettingIcon(vibrationIcon, "settings_vibration_on", "settings_vibration_off", settingsStore.Current.vibrationEnabled);
            SetSettingIcon(soundIcon, "settings_sound_on", "settings_sound_off", settingsStore.Current.soundEnabled);
            SetSettingIcon(pushIcon, "settings_push_on", "settings_push_off", settingsStore.Current.pushNotificationsEnabled);
            SetStaticIcon(languageIcon, "settings_language_on");
            AudioFeedbackManager.RefreshSettings();
        }

        private static Sprite LoadSettingsIcon(string spriteName)
        {
            return Resources.Load<Sprite>(SettingsIconRoot + spriteName);
        }

        private static void SetStaticIcon(Image icon, string spriteName)
        {
            if (icon == null)
            {
                return;
            }

            icon.sprite = LoadSettingsIcon(spriteName);
            icon.enabled = icon.sprite != null;
            icon.color = Color.white;
        }

        private static void SetSettingIcon(Image icon, string onSpriteName, string offSpriteName, bool isOn)
        {
            SetStaticIcon(icon, isOn ? onSpriteName : offSpriteName);
        }

        private void ToggleVibration()
        {
            settingsStore.Current.vibrationEnabled = !settingsStore.Current.vibrationEnabled;
            settingsStore.Save();
            AudioFeedbackManager.RefreshSettings();
            RefreshLabels();
        }

        private void ToggleSound()
        {
            settingsStore.Current.soundEnabled = !settingsStore.Current.soundEnabled;
            settingsStore.Save();
            AudioFeedbackManager.RefreshSettings();
            RefreshLabels();
        }

        private void TogglePush()
        {
            settingsStore.Current.pushNotificationsEnabled = !settingsStore.Current.pushNotificationsEnabled;
            settingsStore.Save();
            RefreshLabels();
        }

        private async void ConnectGooglePlay()
        {
            Debug.Log("[GPGS] Manual connect clicked.");
            PlayerProfile profile = profileStore.Current;
            if (profile == null)
            {
                Debug.LogWarning("[Profile] Link guest profile to google failed: no local profile.");
                SetMessage(T("create_profile_first"));
                return;
            }

            Debug.Log("[GPGS] Manual sign-in request.");
            AccountLinkState result = await googlePlayGames.SignInAsync();
            if (!result.success)
            {
                Debug.LogWarning($"[GPGS] Manual sign-in failed: message={result.message}");
                profileStore.UpdateGooglePlayLink(string.Empty, string.IsNullOrWhiteSpace(result.message) ? T("google_play_failed") : result.message);
                SetMessage(T("google_play_failed"));
                RefreshLabels();
                return;
            }

            Debug.Log($"[GPGS] Manual sign-in success: playerId={MaskId(result.providerUserId)}");
            Debug.Log($"[Profile] Link guest profile to google start: profileId={profile.profileId} googlePlayPlayerId={MaskId(result.providerUserId)}");
            AccountLinkResult linkResult = accountLinkApiClient != null
                ? await accountLinkApiClient.LinkGooglePlayAsync(profile.profileId, result.providerUserId, result.displayName)
                : AccountLinkResult.Unavailable("Account link API is not initialized.");

            if (!linkResult.success)
            {
                string message = string.IsNullOrWhiteSpace(linkResult.message) ? T("google_play_failed") : linkResult.message;
                Debug.LogWarning($"[Profile] Link guest profile to google failed: status={linkResult.statusCode} message={message}");
                profileStore.UpdateGooglePlayLink(string.Empty, message);
                SetMessage(message);
                RefreshLabels();
                return;
            }

            Debug.Log("[Profile] Link guest profile to google success.");
            profileStore.UpdateGooglePlayLink(result.providerUserId, string.Empty);
            SetMessage(T("google_play_connected"));
            RefreshLabels();
        }

        private static string MaskId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(empty)";
            }

            string trimmed = value.Trim();
            if (trimmed.Length <= 8)
            {
                return "***";
            }

            return $"{trimmed.Substring(0, 4)}...{trimmed.Substring(trimmed.Length - 4)}";
        }

        private void ShowNicknamePopup()
        {
            PlayerProfile profile = profileStore.Current;
            nicknameInput.text = profile != null ? profile.nickname : string.Empty;
            int tickets = profile != null ? Mathf.Max(0, profile.nicknameChangeTickets) : 0;
            nicknamePopupMessage.text = profile != null
                ? string.Format(T("tickets_available"), tickets)
                : T("create_profile_first");
            SetNicknameOkInteractable(profile != null && tickets > 0);
            nicknameKeyboardGuard.ResetToCurrentText();
            nicknamePopupRoot.SetActive(true);
            nicknameInput.Select();
            nicknameInput.ActivateInputField();
        }

        private void HideNicknamePopup()
        {
            nicknameKeyboardGuard.CommitLatestKeyboardText();
            nicknamePopupRoot.SetActive(false);
            isSubmittingNickname = false;
        }

        private void ShowAvatarPopup()
        {
            if (profileStore.Current == null)
            {
                SetMessage(T("create_profile_first"));
                return;
            }

            avatarPopupMessage.text = T("avatar_anytime");
            avatarPopupRoot.SetActive(true);
        }

        private void HideAvatarPopup()
        {
            avatarPopupRoot.SetActive(false);
            isSubmittingAvatar = false;
        }

        private void ShowLanguagePopup()
        {
            EnsureLanguagePopup();
            languagePopupRoot.SetActive(true);
        }

        private void HideLanguagePopup()
        {
            if (languagePopupRoot != null)
            {
                languagePopupRoot.SetActive(false);
            }
        }

        private void EnsureLanguagePopup()
        {
            if (languagePopupRoot != null)
            {
                return;
            }

            Canvas canvas = RuntimeUiFactory.CreateCanvas(root.transform, "LanguagePopupCanvas", 1590, 0f);
            languagePopupRoot = canvas.gameObject;
            Image blocker = languagePopupRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                languagePopupRoot.transform,
                "Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -80f),
                new Vector2(680f, 420f));
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.10f, 0.25f, 0.98f);
            AddGoldOutline(panel, new Vector2(4f, -4f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", T("choose_language"), 40, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.82f, 0.30f, 1f);
            title.rectTransform.anchorMin = new Vector2(0.10f, 0.76f);
            title.rectTransform.anchorMax = new Vector2(0.90f, 0.94f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(title, true);

            Button english = RuntimeUiFactory.CreateButton(panel, "EnglishButton", "English", Vector2.zero, new Vector2(500f, 78f));
            Place(english.GetComponent<RectTransform>(), new Vector2(0.13f, 0.49f), new Vector2(0.87f, 0.68f));
            english.onClick.AddListener(() => SelectLanguage(AppLanguage.English));

            Button korea = RuntimeUiFactory.CreateButton(panel, "KoreaButton", "Korea", Vector2.zero, new Vector2(500f, 78f));
            Place(korea.GetComponent<RectTransform>(), new Vector2(0.13f, 0.28f), new Vector2(0.87f, 0.47f));
            korea.onClick.AddListener(() => SelectLanguage(AppLanguage.Korean));

            Button cancel = RuntimeUiFactory.CreateButton(panel, "CancelButton", T("cancel"), Vector2.zero, new Vector2(300f, 72f));
            Place(cancel.GetComponent<RectTransform>(), new Vector2(0.28f, 0.07f), new Vector2(0.72f, 0.24f));
            ApplyCancelButtonStyle(cancel);
            cancel.onClick.AddListener(HideLanguagePopup);
            languagePopupRoot.SetActive(false);
        }

        private void SelectLanguage(AppLanguage language)
        {
            if (settingsStore?.Current == null)
            {
                return;
            }

            settingsStore.Current.languageCode = LocalizationManager.ToLanguageCode(language);
            settingsStore.Save();
            LocalizationManager.Instance?.SetLanguage(language);
            HideLanguagePopup();
            RebuildUi();
            SetMessage(T("language_changed"));
        }

        private void RebuildUi()
        {
            bool showAfterRebuild = root != null && root.activeSelf;
            if (root != null)
            {
                Destroy(root);
                root = null;
            }

            nicknamePopupRoot = null;
            avatarPopupRoot = null;
            languagePopupRoot = null;
            BuildUi();
            RefreshLabels();
            root.SetActive(showAfterRebuild);
        }

        private async void SelectAvatar(int avatarId)
        {
            if (isSubmittingAvatar)
            {
                return;
            }

            PlayerProfile profile = profileStore.Current;
            if (profile == null)
            {
                avatarPopupMessage.text = T("create_profile_first");
                return;
            }

            int normalizedAvatarId = Mathf.Clamp(avatarId, 0, AvatarSpritePaths.Length - 1);
            isSubmittingAvatar = true;
            profileStore.UpdateAvatar(normalizedAvatarId);
            profileStore.SyncAppSettings(settingsStore);
            UpdateLocalRankingAvatars(profile.profileId, profile.nickname, normalizedAvatarId);
            RefreshLabels();

            string message = T("avatar_changed");
            if (profileApiClient.HasServerUrl && !string.IsNullOrWhiteSpace(profile.profileId))
            {
                PlayerProfileCreateResult result = await profileApiClient.UpdateAvatarAsync(profile.profileId, normalizedAvatarId);
                if (result.requestSucceeded && result.success)
                {
                    profileStore.MarkServerSyncResult(true, string.Empty);
                    message = T("avatar_changed_synced");
                }
                else
                {
                    profileStore.MarkServerSyncResult(false, result.message);
                    message = T("avatar_changed_local");
                }
            }

            avatarPopupMessage.text = message;
            SetMessage(message);
            avatarPopupRoot.SetActive(false);
            isSubmittingAvatar = false;
        }

        private async void SubmitNicknameChange()
        {
            if (isSubmittingNickname)
            {
                return;
            }

            nicknameKeyboardGuard.CommitLatestKeyboardText();
            PlayerProfile currentProfile = profileStore.Current;
            if (currentProfile == null || currentProfile.nicknameChangeTickets <= 0)
            {
                nicknamePopupMessage.text = currentProfile == null
                    ? T("create_profile_first")
                    : T("need_nickname_ticket");
                SetNicknameOkInteractable(false);
                return;
            }

            isSubmittingNickname = true;
            string nickname = nicknameInput.text;
            NicknameValidationResult validation = NicknameValidator.Validate(nickname);
            if (!validation.IsValid)
            {
                nicknamePopupMessage.text = validation.Message;
                isSubmittingNickname = false;
                return;
            }

            if (profileApiClient.HasServerUrl)
            {
                NicknameCheckResult check = await profileApiClient.CheckNicknameAsync(validation.NormalizedNickname);
                if (check.requestSucceeded && (!check.valid || !check.available))
                {
                    nicknamePopupMessage.text = string.IsNullOrWhiteSpace(check.message) ? "This nickname is already taken." : check.message;
                    isSubmittingNickname = false;
                    return;
                }
            }

            if (!profileStore.TryChangeNicknameWithTicket(validation.NormalizedNickname, out string message))
            {
                nicknamePopupMessage.text = message;
                isSubmittingNickname = false;
                return;
            }

            profileStore.SyncAppSettings(settingsStore);
            nicknamePopupMessage.text = message;
            SetMessage(message);
            RefreshLabels();
            SetNicknameOkInteractable(profileStore.Current != null && profileStore.Current.nicknameChangeTickets > 0);
            nicknamePopupRoot.SetActive(false);
            isSubmittingNickname = false;
        }

        private void SetNicknameOkInteractable(bool interactable)
        {
            if (nicknameOkButton == null)
            {
                return;
            }

            nicknameOkButton.interactable = interactable && !isSubmittingNickname;
        }

        private static void ApplyCancelButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                CasualUIStyle.ApplyPanel(image, new Color(0.86f, 0.10f, 0.11f, 1f), 26);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.78f, 0.78f, 1f);
            colors.pressedColor = new Color(0.58f, 0.04f, 0.05f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        private void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        private static string T(string key)
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key;
        }

        private static string GetLanguageDisplayName(string languageCode)
        {
            AppLanguage language = string.Equals(languageCode, "ko", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.Korean
                : AppLanguage.English;
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetLanguageDisplayName(language)
                : language == AppLanguage.Korean ? "Korea" : "English";
        }

        private static string FirstNonEmpty(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static Sprite LoadAvatarSprite(int avatarId)
        {
            return Resources.Load<Sprite>(AvatarSpritePaths[Mathf.Clamp(avatarId, 0, AvatarSpritePaths.Length - 1)]);
        }

        private static void UpdateLocalRankingAvatars(string profileId, string nickname, int avatarId)
        {
            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(nickname))
            {
                return;
            }

            CubeChallenge3D.Ranking.LocalRankingStore store = new CubeChallenge3D.Ranking.LocalRankingStore();
            store.UpdateAvatarForPlayer(profileId, nickname, avatarId);
        }

        private sealed class NicknamePopupKeyboardGuard
        {
            private static readonly FieldInfo KeyboardField = typeof(InputField).GetField(
                "m_Keyboard",
                BindingFlags.Instance | BindingFlags.NonPublic);

            private RectTransform panel;
            private InputField input;
            private Vector2 originalPanelPosition;
            private string latestKeyboardText = string.Empty;
            private bool configured;
            private bool lifted;
            private bool wasKeyboardVisible;

            public void Configure(RectTransform panelRect, InputField inputField)
            {
                panel = panelRect;
                input = inputField;
                originalPanelPosition = panel != null ? panel.anchoredPosition : Vector2.zero;
                latestKeyboardText = input != null ? input.text : string.Empty;
                configured = panel != null && input != null;
                lifted = false;
                wasKeyboardVisible = false;
            }

            public void ResetToCurrentText()
            {
                latestKeyboardText = input != null ? input.text : string.Empty;
                wasKeyboardVisible = false;
                if (panel != null)
                {
                    panel.anchoredPosition = originalPanelPosition;
                }

                lifted = false;
            }

            public void Tick()
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
                bool shouldLift = input.isFocused && keyboardVisible;
                Vector2 target = shouldLift
                    ? originalPanelPosition + new Vector2(0f, 210f)
                    : originalPanelPosition;

                if (lifted != shouldLift || panel.anchoredPosition != target)
                {
                    panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, target, 0.5f);
                    if ((panel.anchoredPosition - target).sqrMagnitude < 1f)
                    {
                        panel.anchoredPosition = target;
                    }
                }

                lifted = shouldLift;
#endif
            }

            private void CaptureLatestKeyboardText(bool keyboardVisible)
            {
#if UNITY_ANDROID || UNITY_IOS
                if (!keyboardVisible || input == null)
                {
                    return;
                }

                TouchScreenKeyboard keyboard = KeyboardField?.GetValue(input) as TouchScreenKeyboard;
                if (keyboard != null)
                {
                    latestKeyboardText = keyboard.text ?? string.Empty;
                }
                else
                {
                    latestKeyboardText = input.text ?? string.Empty;
                }
#endif
            }

            public void CommitLatestKeyboardText()
            {
                if (input == null || latestKeyboardText == null || input.text == latestKeyboardText)
                {
                    return;
                }

                input.text = latestKeyboardText;
                input.caretPosition = latestKeyboardText.Length;
                input.selectionAnchorPosition = latestKeyboardText.Length;
                input.selectionFocusPosition = latestKeyboardText.Length;
            }
        }
    }
}
