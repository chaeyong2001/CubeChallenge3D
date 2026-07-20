using CubeChallenge3D.Auth;
using CubeChallenge3D.Cloud;
using CubeChallenge3D.Networking;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Settings
{
    public sealed class AccountManagementPanelUI
    {
        private readonly Transform parent;
        private readonly PlayerProfileStore profileStore;
        private readonly AccountLinkApiClient apiClient;
        private readonly CloudSyncService cloudSyncService;
        private readonly GooglePlayGamesAuthService googlePlayGames;
        private readonly GoogleSignInAuthService googleSignIn;

        private GameObject root;
        private Text profileText;
        private Text googlePlayStatus;
        private Text googleStatus;
        private Text cloudStatus;
        private Text messageText;
        private Button uploadButton;
        private Button downloadButton;
        private Button refreshCloudButton;

        public AccountManagementPanelUI(Transform parent, SettingsStore settingsStore)
        {
            this.parent = parent;
            profileStore = new PlayerProfileStore();
            apiClient = new AccountLinkApiClient(
                settingsStore?.Current?.rankingApiBaseUrl ?? string.Empty,
                settingsStore?.Current?.rankingRequestTimeoutSeconds ?? 8);
            cloudSyncService = new CloudSyncService(settingsStore);
            googlePlayGames = new GooglePlayGamesAuthService();
            googleSignIn = new GoogleSignInAuthService();
            Build();
        }

        public void Show()
        {
            Refresh();
            root.SetActive(true);
        }

        private void Hide()
        {
            root.SetActive(false);
        }

        private void Build()
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "AccountManagementModalCanvas", 1560);
            root = canvas.gameObject;

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "AccountManagementPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(820f, 1010f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Account Management", 40, TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(30f, 910f);
            title.rectTransform.offsetMax = new Vector2(-30f, -28f);

            Text subtitle = RuntimeUiFactory.CreateText(panel, "Subtitle", "Link an account to protect your progress.", 24, TextAnchor.UpperCenter);
            subtitle.rectTransform.offsetMin = new Vector2(40f, 858f);
            subtitle.rectTransform.offsetMax = new Vector2(-40f, -92f);

            profileText = AddSection(panel, "CurrentProfile", new Vector2(0f, 740f), "Current Profile", string.Empty);
            googlePlayStatus = AddSection(panel, "GooglePlay", new Vector2(0f, 590f), "Google Play Games", string.Empty);
            Button googlePlayButton = RuntimeUiFactory.CreateButton(panel, "LinkGooglePlayButton", "Link Google Play Games", new Vector2(0f, 520f), new Vector2(560f, 64f));
            googlePlayButton.onClick.AddListener(LinkGooglePlay);

            googleStatus = AddSection(panel, "GoogleAccount", new Vector2(0f, 405f), "Google Account", string.Empty);
            Button googleButton = RuntimeUiFactory.CreateButton(panel, "LinkGoogleButton", "Link Google Account", new Vector2(0f, 335f), new Vector2(560f, 64f));
            googleButton.onClick.AddListener(LinkGoogle);

            cloudStatus = AddSection(panel, "CloudSync", new Vector2(0f, 284f), "Cloud Sync", string.Empty);
            uploadButton = RuntimeUiFactory.CreateButton(panel, "UploadCloudButton", "Upload to Cloud", new Vector2(-196f, 206f), new Vector2(250f, 54f));
            uploadButton.onClick.AddListener(UploadCloud);
            downloadButton = RuntimeUiFactory.CreateButton(panel, "DownloadCloudButton", "Download from Cloud", new Vector2(196f, 206f), new Vector2(250f, 54f));
            downloadButton.onClick.AddListener(DownloadCloud);
            refreshCloudButton = RuntimeUiFactory.CreateButton(panel, "RefreshCloudButton", "Refresh Status", new Vector2(0f, 144f), new Vector2(360f, 52f));
            refreshCloudButton.onClick.AddListener(RefreshCloudStatus);

            Text note = RuntimeUiFactory.CreateText(panel, "Note", "Download replaces local progress after creating a local backup.", 20, TextAnchor.MiddleCenter);
            note.rectTransform.anchorMin = new Vector2(0.08f, 0f);
            note.rectTransform.anchorMax = new Vector2(0.92f, 0f);
            note.rectTransform.pivot = new Vector2(0.5f, 0f);
            note.rectTransform.anchoredPosition = new Vector2(0f, 108f);
            note.rectTransform.sizeDelta = new Vector2(0f, 34f);

            messageText = RuntimeUiFactory.CreateText(panel, "Message", string.Empty, 21, TextAnchor.MiddleCenter);
            messageText.color = new Color(1f, 0.82f, 0.38f, 1f);
            messageText.rectTransform.anchorMin = new Vector2(0.08f, 0f);
            messageText.rectTransform.anchorMax = new Vector2(0.92f, 0f);
            messageText.rectTransform.pivot = new Vector2(0.5f, 0f);
            messageText.rectTransform.anchoredPosition = new Vector2(0f, 74f);
            messageText.rectTransform.sizeDelta = new Vector2(0f, 32f);

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Back", new Vector2(0f, 8f), new Vector2(340f, 60f));
            close.onClick.AddListener(Hide);
            Hide();
        }

        private static Text AddSection(RectTransform panel, string name, Vector2 position, string title, string body)
        {
            RectTransform section = RuntimeUiFactory.CreatePanel(
                panel,
                name,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                position,
                new Vector2(640f, 92f));
            Text label = RuntimeUiFactory.CreateText(section, "Label", $"{title}\n{body}", 24, TextAnchor.MiddleCenter);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 24;
            return label;
        }

        private void Refresh()
        {
            PlayerProfile profile = profileStore.Current;
            if (profile == null)
            {
                profileText.text = "Current Profile\nNo local profile";
                googlePlayStatus.text = "Google Play Games\nStatus: Not linked";
                googleStatus.text = "Google Account\nStatus: Not linked";
                cloudStatus.text = "Cloud Sync\nCreate a profile first.";
                SetCloudButtons(false);
                return;
            }

            string profileStatus = profile.serverSyncPending ? "Pending" : profile.isServerSynced ? "Synced" : "Local";
            profileText.text = $"Current Profile\n{profile.nickname}  /  Avatar {profile.avatarId + 1}  /  {profileStatus}";
            googlePlayStatus.text = $"Google Play Games\nStatus: {(profile.linkedGooglePlay ? "Linked" : "Not linked")}";
            googleStatus.text = $"Google Account\nStatus: {(profile.linkedGoogle ? "Linked" : "Not linked")}";
            if (cloudSyncService.CanSync(out string reason))
            {
                cloudStatus.text = "Cloud Sync\nReady. Tap Refresh Status.";
                SetCloudButtons(true);
            }
            else
            {
                cloudStatus.text = $"Cloud Sync\n{reason}";
                SetCloudButtons(false);
                if (refreshCloudButton != null)
                {
                    refreshCloudButton.interactable = !string.IsNullOrWhiteSpace(profile.profileId);
                }
            }

            if (!string.IsNullOrWhiteSpace(profile.accountLinkError))
            {
                messageText.text = profile.accountLinkError;
            }
        }

        private async void LinkGooglePlay()
        {
            PlayerProfile profile = profileStore.Current;
            if (profile == null)
            {
                SetMessage("Create a profile first.");
                return;
            }

            AccountLinkState auth = await googlePlayGames.SignInAsync();
            if (!auth.success)
            {
                profileStore.UpdateGooglePlayLink(string.Empty, auth.message);
                SetMessage(auth.message);
                Refresh();
                return;
            }

            AccountLinkResult result = await apiClient.LinkGooglePlayAsync(profile.profileId, auth.providerUserId, auth.displayName);
            if (result.success)
            {
                profileStore.UpdateGooglePlayLink(auth.providerUserId, string.Empty);
                SetMessage("Google Play Games linked.");
            }
            else
            {
                profileStore.UpdateGooglePlayLink(string.Empty, result.message);
                SetMessage(result.message);
            }

            Refresh();
        }

        private async void LinkGoogle()
        {
            PlayerProfile profile = profileStore.Current;
            if (profile == null)
            {
                SetMessage("Create a profile first.");
                return;
            }

            AccountLinkState auth = await googleSignIn.SignInAsync();
            if (!auth.success)
            {
                profileStore.UpdateGoogleLink(string.Empty, string.Empty, auth.message);
                SetMessage(auth.message);
                Refresh();
                return;
            }

            AccountLinkResult result = await apiClient.LinkGoogleAsync(profile.profileId, auth.providerUserId, auth.emailHash);
            if (result.success)
            {
                profileStore.UpdateGoogleLink(auth.providerUserId, auth.emailHash, string.Empty);
                SetMessage("Google Account linked.");
            }
            else
            {
                profileStore.UpdateGoogleLink(string.Empty, string.Empty, result.message);
                SetMessage(result.message);
            }

            Refresh();
        }

        private void SetMessage(string message)
        {
            messageText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
        }

        private async void RefreshCloudStatus()
        {
            SetMessage("Checking cloud status...");
            CloudSaveStatusResult result = await cloudSyncService.GetCloudSaveStatusAsync();
            if (result.success && result.status != null)
            {
                string lastSync = string.IsNullOrWhiteSpace(result.status.serverUpdatedAt)
                    ? "Never"
                    : result.status.serverUpdatedAt;
                cloudStatus.text = $"Cloud Sync\nCloud save: {(result.status.exists ? "Exists" : "Not found")}  /  Last: {lastSync}";
                SetMessage(result.message);
            }
            else
            {
                SetMessage(result.message);
            }
        }

        private async void UploadCloud()
        {
            SetMessage("Uploading current save...");
            SetCloudButtons(false);
            CloudSaveUploadResult result = await cloudSyncService.UploadCurrentSaveAsync();
            SetMessage(result.message);
            Refresh();
        }

        private async void DownloadCloud()
        {
            SetMessage("Downloading cloud save...");
            SetCloudButtons(false);
            CloudSaveDownloadResult result = await cloudSyncService.DownloadCloudSaveAsync();
            SetMessage(result.success ? "Cloud save restored. Local backup was created." : result.message);
            if (result.success)
            {
                profileStore.ReloadFromDisk();
            }
            Refresh();
        }

        private void SetCloudButtons(bool interactable)
        {
            if (uploadButton != null)
            {
                uploadButton.interactable = interactable;
            }

            if (downloadButton != null)
            {
                downloadButton.interactable = interactable;
            }

            if (refreshCloudButton != null)
            {
                refreshCloudButton.interactable = interactable;
            }
        }
    }
}
