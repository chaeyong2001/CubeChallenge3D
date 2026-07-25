using System.Threading.Tasks;
using CubeChallenge3D.Auth;
using CubeChallenge3D.Core;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Learn.Services;
using CubeChallenge3D.Learn.Storage;
using CubeChallenge3D.Networking;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Settings;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Learn;
using CubeChallenge3D.UI.Profile;
using CubeChallenge3D.UI.Records;
using CubeChallenge3D.UI.Rewards;
using CubeChallenge3D.UI.Settings;
using CubeChallenge3D.UI.Shop;
using CubeChallenge3D.UI.Solver;
using CubeChallenge3D.UI.Stages;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeChallenge3D.UI
{
    public static class MainMenuBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryBuildMainMenu(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBuildMainMenu(scene);
        }

        private static void TryBuildMainMenu(Scene scene)
        {
            EnsurePersistentManagers();

            if (scene.name != SceneLoader.MainMenuScene || Object.FindFirstObjectByType<MainMenuController>() != null)
            {
                return;
            }

            GameObject root = new("MainMenu");
            MainMenuController controller = root.AddComponent<MainMenuController>();

            Canvas canvas = RuntimeUiFactory.CreateCanvas(root.transform, "Canvas", 100, 0f);
            controller.SetMainMenuCanvas(canvas);
            CreateBackground(canvas.transform);
            RectTransform safeArea = CreateSafeArea(canvas.transform);
            RectTransform menuPanel = CreateMenuPanel(safeArea);
            RectTransform logoRoot = CreateFullScreenRoot(menuPanel, "LogoRoot");
            CreateHeader(logoRoot);
            VerticalLayoutGroup layout = CreateButtonLayout(menuPanel);
            TopCurrencyBar.Attach(canvas, controller.Shop, true);
            SettingsStore settingsStore = new SettingsStore();
            LocalizationManager.Instance?.SetLanguageFromCode(settingsStore.Current.languageCode);

            Button stages = CreateButton(layout.transform, "stages", "stages", CasualUIColor.Blue, controller.Stages);
            Button ranking = CreateButton(layout.transform, "ranking_challenge", "ranking", CasualUIColor.Purple, controller.RankingChallenge);
            Button solverLearn = CreateButton(layout.transform, "solver_learn", "solver", CasualUIColor.Green, controller.SolverLearn);
            Button shop = CreateButton(layout.transform, "shop", "shop", CasualUIColor.Orange, controller.Shop);
            Button rewards = CreateButton(layout.transform, "rewards", "rewards", CasualUIColor.Pink, controller.Rewards);
            ApplyDailyRewardAttention(rewards);
            Button records = CreateButton(layout.transform, "records", "records", CasualUIColor.Teal, controller.Records);
            Button settings = CreateButton(layout.transform, "settings", "settings", CasualUIColor.Slate, controller.Settings);

            ModalPanel comingSoon = new ModalPanel(root.transform, "MainMenuModalCanvas");
            PlayerProfileStore profileStore = new PlayerProfileStore();
            ApplyWeeklyRankingAttention(ranking, settingsStore, profileStore);
            PlayerProfileApiClient profileApiClient = new PlayerProfileApiClient(
                settingsStore.Current.rankingApiBaseUrl,
                settingsStore.Current.rankingRequestTimeoutSeconds);
            AccountChoicePanelUI accountChoice = null;
            ProfileSetupPanelUI profileSetup = null;
            profileSetup = new ProfileSetupPanelUI(menuPanel, profileStore, profileApiClient, _ =>
            {
                profileStore.SyncAppSettings(settingsStore);
                accountChoice?.SetVisible(false);
                profileSetup.SetVisible(false);
                layout.gameObject.SetActive(true);
            });
            accountChoice = new AccountChoicePanelUI(
                menuPanel,
                () =>
                {
                    _ = SignInAndResolveGooglePlayProfileAsync(
                        profileStore,
                        settingsStore,
                        profileApiClient,
                        profileSetup,
                        accountChoice,
                        layout);
                },
                () =>
                {
                    Debug.Log("[ProfileFlow] Continue without sign-in selected");
                    Debug.Log("[ProfileFlow] Show guest nickname setup");
                    profileSetup.SetGooglePlayContext(string.Empty, string.Empty);
                    accountChoice.SetVisible(false);
                    layout.gameObject.SetActive(false);
                    profileSetup.SetVisible(true);
                });
            bool hasProfile = profileStore.Exists();
            if (hasProfile)
            {
                profileStore.SyncAppSettings(settingsStore);
                PlayerProfile profile = profileStore.Current;
                if (profile != null && (profile.linkedGooglePlay || !string.IsNullOrWhiteSpace(profile.googlePlayPlayerId)))
                {
                    Debug.Log("[GPGS] Linked profile found. Try silent sign-in.");
                    _ = RefreshLinkedGooglePlayProfileAsync(profileStore, settingsStore, profileApiClient);
                }

                Debug.Log("[ProfileFlow] Local profile found. Skip account choice.");
                Debug.Log($"[MainMenu] Profile exists. Showing menu buttons. nickname={profile?.nickname} avatarId={profile?.avatarId}");
            }
            else
            {
                Debug.Log("[ProfileFlow] No local profile found. Show account choice.");
            }

            layout.gameObject.SetActive(hasProfile);
            profileSetup.SetVisible(false);
            accountChoice.SetVisible(!hasProfile);
            if (!hasProfile)
            {
                accountChoice.SetMessage(string.Empty);
            }

            WalletStore walletStore = new WalletStore();
            InventoryStore inventoryStore = new InventoryStore();
            StageProgressStore progressStore = new StageProgressStore();
            StageMilestoneRewardStore milestoneStore = new StageMilestoneRewardStore();
            RewardedAdService rewardService = RewardedAdService.CreateDefault();
            ShopPanelUI shopPanel = null;
            TopCurrencyBar.SetDefaultActions(
                () => shopPanel?.Show(),
                () => shopPanel?.ShowGemItems(),
                () => shopPanel?.ShowGemItems(),
                () => shopPanel?.ShowPromotion());
            StageListPanelUI stageListPanel = new StageListPanelUI(root.transform);
            shopPanel = new ShopPanelUI(root.transform, walletStore, inventoryStore, rewardService, profileStore);
            TopCurrencyBar.Attach(
                canvas,
                controller.Shop,
                true,
                shopPanel.ShowGemItems,
                shopPanel.ShowGemItems,
                shopPanel.ShowPromotion);
            stageListPanel.SetShopAction(shopPanel.Show);
            RewardsPanelUI rewardsPanel = new RewardsPanelUI(root.transform, walletStore, inventoryStore, progressStore, milestoneStore, rewardService);
            rewardsPanel.SetTopHudActions(
                controller.Shop,
                shopPanel.ShowGemItems,
                shopPanel.ShowGemItems,
                shopPanel.ShowPromotion);
            SolverPanelUI solverPanel = TryCreateSolverPanel(root.transform);
            LearnModeHubView learnHub = new LearnModeHubView(root.transform);
            learnHub.SetTopHudActions(
                controller.Shop,
                shopPanel.ShowGemItems,
                shopPanel.ShowGemItems,
                shopPanel.ShowPromotion);
            LearnLessonBrowserUI learnBrowser = new LearnLessonBrowserUI(
                root.transform,
                new LearnContentProvider(),
                new LearnLessonProgressStore());
            RecordsPanelUI recordsPanel = new RecordsPanelUI(root.transform);
            SettingsPanelUI settingsPanel = root.AddComponent<SettingsPanelUI>();
            settingsPanel.Initialize(settingsStore, null, null);

            learnHub.SetManualSolverAction(() =>
            {
                if (solverPanel != null)
                {
                    solverPanel.Show();
                    BackNavigationManager.SetCurrentHandler("ManualSolver", () =>
                    {
                        solverPanel.Hide();
                        return true;
                    });
                }
                else
                {
                    comingSoon.Show("Solver unavailable", "The solver screen could not be loaded.");
                }
            });
            learnHub.SetCategoryAction(categoryId =>
            {
                learnBrowser.ShowCategory(categoryId);
                BackNavigationManager.SetCurrentHandler("LearnLessons", () =>
                {
                    learnBrowser.Hide();
                    learnHub.Show();
                    BackNavigationManager.SetCurrentHandler("SolverLearn", () =>
                    {
                        learnHub.Hide();
                        BackNavigationManager.SetCurrentHandler("MainMenu", () =>
                        {
                            BackNavigationManager.ShowExitConfirmation();
                            return true;
                        });
                        return true;
                    });
                    return true;
                });
            });
            learnHub.SetPracticeAction(controller.Practice);
            learnBrowser.Closed += () =>
            {
                learnHub.Show();
                BackNavigationManager.SetCurrentHandler("SolverLearn", () =>
                {
                    learnHub.Hide();
                    BackNavigationManager.SetCurrentHandler("MainMenu", () =>
                    {
                        BackNavigationManager.ShowExitConfirmation();
                        return true;
                    });
                    return true;
                });
            };
            if (solverPanel != null)
            {
                solverPanel.Closed += () =>
                {
                    learnHub.Show();
                    BackNavigationManager.SetCurrentHandler("SolverLearn", () =>
                    {
                        learnHub.Hide();
                        BackNavigationManager.SetCurrentHandler("MainMenu", () =>
                        {
                            BackNavigationManager.ShowExitConfirmation();
                            return true;
                        });
                        return true;
                    });
                };
            }
            controller.Initialize(
                comingSoon,
                settingsPanel,
                stageListPanel,
                shopPanel,
                rewardsPanel,
                solverPanel,
                learnHub,
                recordsPanel);
            controller.ApplyLocalizedLabels(ranking, stages, solverLearn, records, shop, rewards, settings);

            if (!hasProfile)
            {
                AudioFeedbackManager.ClearMenuBgmSuppressions();
                return;
            }

            if (GameLaunchContext.ConsumeStageListOnMainMenuRequest())
            {
                controller.Stages(GameLaunchContext.StageId);
            }
            else if (GameLaunchContext.ConsumeSolverLearnOnMainMenuRequest())
            {
                controller.SolverLearn();
            }
            else if (GameLaunchContext.ConsumeShopOnMainMenuRequest())
            {
                string requestedShopTab = GameLaunchContext.ConsumeRequestedShopTab();
                if (requestedShopTab == "GemItems")
                {
                    shopPanel.ShowGemItems();
                }
                else if (requestedShopTab == "Promotion")
                {
                    shopPanel.ShowPromotion();
                }
                else
                {
                    controller.Shop();
                }
            }
            else
            {
                AudioFeedbackManager.ClearMenuBgmSuppressions();
            }
        }

        private static void EnsurePersistentManagers()
        {
            if (GameManager.Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }

            if (LocalizationManager.Instance == null)
            {
                new GameObject("LocalizationManager").AddComponent<LocalizationManager>();
            }
        }

        private static SolverPanelUI TryCreateSolverPanel(Transform parent)
        {
            try
            {
                return new SolverPanelUI(parent);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                Transform partialCanvas = parent.Find("SolverCanvas");
                if (partialCanvas != null)
                {
                    Object.Destroy(partialCanvas.gameObject);
                }
                return null;
            }
        }

        private static void CreateBackground(Transform parent)
        {
            CasualUIFactory.CreateBackdrop(parent, "Background");
        }

        private static RectTransform CreateSafeArea(Transform parent)
        {
            GameObject safeObject = new("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeObject.transform.SetParent(parent, false);
            return safeObject.GetComponent<RectTransform>();
        }

        private static void CreateHeader(RectTransform parent)
        {
            Sprite titleSprite = CasualIconFactory.LoadMainMenuKitSprite("Title/title");
            Sprite subtitleSprite = CasualIconFactory.LoadMainMenuKitSprite("Title/subtitle");
            if (titleSprite != null && subtitleSprite != null)
            {
                CreateHeaderImage(parent, "AppTitle", titleSprite, new Vector2(0f, -265f), new Vector2(850f, 126f));
                CreateHeaderImage(parent, "AppSubtitle", subtitleSprite, new Vector2(0f, -394f), new Vector2(570f, 54f));
                return;
            }

            GameObject titleObject = new("TitleBlock", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleObject.transform.SetParent(parent, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -265f);
            titleRect.sizeDelta = new Vector2(-80f, 82f);

            HorizontalLayoutGroup titleLayout = titleObject.GetComponent<HorizontalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.MiddleCenter;
            titleLayout.spacing = 14f;
            titleLayout.childControlWidth = false;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = true;

            Text cubeTitle = CreateTitlePart(titleRect, "CubeTitle", "Cube", 252f, new Color(1f, 0.69f, 0.08f));
            Text challengeTitle = CreateTitlePart(
                titleRect,
                "ChallengeTitle",
                "Challenge 3D",
                500f,
                new Color(1f, 0.94f, 0.82f));
            cubeTitle.fontSize = 62;
            challengeTitle.fontSize = 58;

            Text subtitle = RuntimeUiFactory.CreateText(parent, "AppSubtitle", "Play  \u2022  Learn  \u2022  Improve", 25, TextAnchor.MiddleCenter);
            subtitle.color = new Color(1f, 0.7f, 0.18f, 1f);
            subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -386f);
            subtitle.rectTransform.sizeDelta = new Vector2(-60f, 38f);
            subtitle.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(subtitle, false);

            CreateTitleAccent(parent, new Vector2(-270f, -404f));
            CreateTitleAccent(parent, new Vector2(270f, -404f));
        }

        private static void CreateHeaderImage(
            RectTransform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 size)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static Text CreateTitlePart(
            RectTransform parent,
            string name,
            string value,
            float width,
            Color color)
        {
            Text text = RuntimeUiFactory.CreateText(parent, name, value, 58, TextAnchor.MiddleCenter);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            CasualUIStyle.ApplyTextDepth(text, true);
            return text;
        }

        private static void CreateTitleAccent(RectTransform parent, Vector2 position)
        {
            GameObject accentObject = new("TitleAccent", typeof(RectTransform), typeof(Image));
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

        private static RectTransform CreateMenuPanel(Transform parent)
        {
            GameObject panelObject = new("MenuPanel");
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = panelObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateFullScreenRoot(RectTransform parent, string name)
        {
            GameObject rootObject = new(name, typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            RectTransform rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static VerticalLayoutGroup CreateButtonLayout(Transform parent)
        {
            GameObject layoutObject = new("MenuButtonsRoot");
            layoutObject.transform.SetParent(parent, false);

            RectTransform rect = layoutObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.02f);
            rect.anchorMax = new Vector2(0.94f, 0.765f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = layoutObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 13f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return layout;
        }

        private static Button CreateButton(
            Transform parent,
            string key,
            string iconKey,
            CasualUIColor theme,
            UnityEngine.Events.UnityAction action)
        {
            string title = key == "records"
                ? "World Records"
                : LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetText(key)
                    : key;
            return CasualUIFactory.CreateLargeMenuCard(
                parent,
                $"{key}Button",
                title,
                GetMenuSubtitle(key),
                iconKey,
                theme,
                action);
        }

        private static void ApplyDailyRewardAttention(Button rewardsButton)
        {
            if (rewardsButton == null)
            {
                return;
            }

            if (rewardsButton.GetComponent<DailyRewardAttentionBinding>() == null)
            {
                rewardsButton.gameObject.AddComponent<DailyRewardAttentionBinding>();
            }
        }

        private static void ApplyWeeklyRankingAttention(
            Button rankingButton,
            SettingsStore settingsStore,
            PlayerProfileStore profileStore)
        {
            if (rankingButton == null || settingsStore == null || profileStore == null)
            {
                return;
            }

            WeeklyRankingRewardAttentionBinding binding = rankingButton.GetComponent<WeeklyRankingRewardAttentionBinding>();
            if (binding == null)
            {
                binding = rankingButton.gameObject.AddComponent<WeeklyRankingRewardAttentionBinding>();
            }

            binding.Initialize(settingsStore, profileStore);
        }

        private static async Task SignInAndResolveGooglePlayProfileAsync(
            PlayerProfileStore profileStore,
            SettingsStore settingsStore,
            PlayerProfileApiClient profileApiClient,
            ProfileSetupPanelUI profileSetup,
            AccountChoicePanelUI accountChoice,
            VerticalLayoutGroup layout)
        {
            if (profileStore == null || settingsStore == null || profileSetup == null || accountChoice == null || layout == null)
            {
                return;
            }

            if (profileStore.Exists())
            {
                Debug.Log("[ProfileFlow] Local profile found during sign-in flow. Skip account choice.");
                profileStore.SyncAppSettings(settingsStore);
                layout.gameObject.SetActive(true);
                accountChoice.SetVisible(false);
                profileSetup.SetVisible(false);
                return;
            }

            Debug.Log("[GPGS] Initial sign-in selected");
            accountChoice.SetBusy(true, "Signing in...");
            AccountLinkState auth = await new GooglePlayGamesAuthService().SignInAsync();
            if (!auth.success || string.IsNullOrWhiteSpace(auth.providerUserId))
            {
                Debug.LogWarning($"[Profile] Resolve Google Play profile skipped. reason={auth.message}");
                accountChoice.SetBusy(false, string.IsNullOrWhiteSpace(auth.message) ? "Google Play sign-in failed." : auth.message);
                layout.gameObject.SetActive(false);
                profileSetup.SetVisible(false);
                return;
            }

            if (profileApiClient == null || !profileApiClient.HasServerUrl)
            {
                Debug.LogWarning("[Profile] Resolve Google Play profile failed. reason=Server URL is empty.");
                profileSetup.SetGooglePlayContext(auth.providerUserId, auth.displayName);
                accountChoice.SetVisible(false);
                layout.gameObject.SetActive(false);
                profileSetup.SetVisible(true);
                return;
            }

            accountChoice.SetBusy(true, "Checking profile...");
            Debug.Log("[Profile] Resolve Google Play profile start");
            GooglePlayProfileResolveResult resolve = await profileApiClient.ResolveGooglePlayProfileAsync(auth.providerUserId);
            if (!resolve.success)
            {
                Debug.LogWarning($"[Profile] Resolve Google Play profile failed. status={resolve.statusCode} message={resolve.message}");
                accountChoice.SetBusy(false, string.IsNullOrWhiteSpace(resolve.message) ? "Could not check Google Play profile." : resolve.message);
                profileSetup.SetGooglePlayContext(auth.providerUserId, auth.displayName);
                layout.gameObject.SetActive(false);
                profileSetup.SetVisible(false);
                return;
            }

            if (!resolve.found || resolve.profile == null)
            {
                Debug.Log("[Profile] Google Play profile not found. Show nickname setup.");
                profileSetup.SetGooglePlayContext(auth.providerUserId, auth.displayName);
                accountChoice.SetVisible(false);
                layout.gameObject.SetActive(false);
                profileSetup.SetVisible(true);
                return;
            }

            Debug.Log($"[Profile] Google Play resolve success serverProfileId={MaskId(resolve.profile.profileId)} serverNickname={resolve.profile.nickname}");
            if (profileStore.Exists())
            {
                profileStore.SyncAppSettings(settingsStore);
                layout.gameObject.SetActive(true);
                accountChoice.SetVisible(false);
                profileSetup.SetVisible(false);
                return;
            }

            PlayerProfile restored = profileStore.CreateProfile(
                resolve.profile.nickname,
                resolve.profile.avatarId,
                resolve.profile.profileId,
                true,
                false,
                string.Empty,
                resolve.profile.createdAt,
                resolve.profile.updatedAt,
                FirstNonEmpty(resolve.profile.googlePlayGamesPlayerId, resolve.profile.googlePlayPlayerId, auth.providerUserId),
                auth.displayName);
            if (restored == null)
            {
                Debug.LogWarning("[Profile] Resolve Google Play profile failed. reason=Could not save restored profile.");
                accountChoice.SetBusy(false, "Could not save restored profile.");
                profileSetup.SetGooglePlayContext(auth.providerUserId, auth.displayName);
                layout.gameObject.SetActive(false);
                profileSetup.SetVisible(false);
                return;
            }

            profileStore.SyncAppSettings(settingsStore);
            Debug.Log($"[Profile] Google Play profile found nickname={restored.nickname}");
            accountChoice.SetVisible(false);
            profileSetup.SetVisible(false);
            layout.gameObject.SetActive(true);
        }

        private static async Task RefreshLinkedGooglePlayProfileAsync(
            PlayerProfileStore profileStore,
            SettingsStore settingsStore,
            PlayerProfileApiClient profileApiClient)
        {
            if (profileStore == null || settingsStore == null || profileApiClient == null || !profileStore.Exists())
            {
                return;
            }

            AccountLinkState auth = await new GooglePlayGamesAuthService().TrySilentSignInAsync();
            if (!auth.success || string.IsNullOrWhiteSpace(auth.providerUserId))
            {
                Debug.LogWarning($"[Profile] Linked Google Play refresh skipped. reason={auth.message}");
                return;
            }

            if (!profileApiClient.HasServerUrl)
            {
                Debug.LogWarning("[Profile] Linked Google Play refresh skipped. reason=Server URL is empty.");
                return;
            }

            Debug.Log("[Profile] Resolve Google Play profile start");
            GooglePlayProfileResolveResult resolve = await profileApiClient.ResolveGooglePlayProfileAsync(auth.providerUserId);
            if (!resolve.success || !resolve.found || resolve.profile == null)
            {
                Debug.LogWarning($"[Profile] Linked Google Play refresh skipped. status={resolve.statusCode} message={resolve.message}");
                return;
            }

            Debug.Log($"[Profile] Google Play resolve success serverProfileId={MaskId(resolve.profile.profileId)} serverNickname={resolve.profile.nickname}");
            PlayerProfile current = profileStore.Current;
            if (current == null || resolve.profile.profileId != current.profileId)
            {
                Debug.LogWarning("[Profile] Linked Google Play refresh conflict. Keep local profile.");
                Debug.LogWarning($"[Profile] Resolve profile compare localProfileId={MaskId(current?.profileId)} serverProfileId={MaskId(resolve.profile.profileId)}");
                return;
            }

            profileStore.UpdateGooglePlayLink(
                FirstNonEmpty(resolve.profile.googlePlayGamesPlayerId, resolve.profile.googlePlayPlayerId, auth.providerUserId),
                auth.displayName,
                string.Empty);
            profileStore.SyncAppSettings(settingsStore);
            Debug.Log($"[Profile] Linked Google Play profile refreshed nickname={current.nickname}");
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

        private static string MaskId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(empty)";
            }

            string trimmed = value.Trim();
            if (trimmed.Length <= 6)
            {
                return "***";
            }

            return $"{trimmed.Substring(0, 3)}***{trimmed.Substring(trimmed.Length - 3)}";
        }

        private static string GetMenuSubtitle(string key)
        {
            switch (key)
            {
                case "stages":
                    return "Complete stages and earn stars";
                case "ranking_challenge":
                    return "Compete and climb the leaderboard";
                case "solver_learn":
                    return "Solve step by step and learn";
                case "shop":
                    return "Browse items and power-ups";
                case "rewards":
                    return "Claim daily rewards and bonuses";
                case "records":
                    return "See who has cleared the most stages";
                case "settings":
                    return "Customize your experience";
                default:
                    return string.Empty;
            }
        }

        private sealed class WeeklyRankingRewardAttentionBinding : MonoBehaviour
        {
            private const float CheckIntervalSeconds = 12f;

            private SettingsStore settingsStore;
            private PlayerProfileStore profileStore;
            private RewardAttentionEffect attentionEffect;
            private float nextCheckTime;
            private bool isChecking;

            public void Initialize(SettingsStore settings, PlayerProfileStore profile)
            {
                settingsStore = settings;
                profileStore = profile;
                attentionEffect = GetComponent<RewardAttentionEffect>();
                nextCheckTime = 0f;
                Refresh();
            }

            private void Awake()
            {
                attentionEffect = GetComponent<RewardAttentionEffect>();
            }

            private void OnEnable()
            {
                nextCheckTime = 0f;
                Refresh();
            }

            private void Update()
            {
                if (Time.unscaledTime < nextCheckTime)
                {
                    return;
                }

                Refresh();
            }

            private async void Refresh()
            {
                if (isChecking)
                {
                    return;
                }

                nextCheckTime = Time.unscaledTime + CheckIntervalSeconds;
                if (settingsStore == null || profileStore == null || !profileStore.Exists())
                {
                    SetAttention(false);
                    return;
                }

                PlayerProfile profile = profileStore.Current;
                if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
                {
                    SetAttention(false);
                    return;
                }

                AppSettings settings = settingsStore.Current;
                if (settings == null || string.IsNullOrWhiteSpace(settings.rankingApiBaseUrl))
                {
                    SetAttention(false);
                    return;
                }

                isChecking = true;
                WeeklyRankingRewardDto reward = await new WeeklyRankingRewardService(
                    settings.rankingApiBaseUrl,
                    settings.rankingRequestTimeoutSeconds).GetClaimableAsync(profile.profileId);
                isChecking = false;

                bool claimableTopThree = reward != null
                    && reward.exists
                    && !reward.claimed
                    && reward.rank >= 1
                    && reward.rank <= 3
                    && reward.rewardAmount > 0;
                SetAttention(claimableTopThree);
            }

            private void SetAttention(bool active)
            {
                if (active)
                {
                    if (attentionEffect == null)
                    {
                        attentionEffect = gameObject.AddComponent<RewardAttentionEffect>();
                    }

                    attentionEffect.enabled = true;
                    return;
                }

                if (attentionEffect != null)
                {
                    attentionEffect.enabled = false;
                }
            }
        }

    }
}
