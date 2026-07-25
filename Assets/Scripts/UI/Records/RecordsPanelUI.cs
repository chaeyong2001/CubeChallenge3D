using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CubeChallenge3D.Core;
using CubeChallenge3D.Networking;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Profile;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Records
{
    public sealed class RecordsPanelUI
    {
        private enum RecordsMode
        {
            Normal,
            Hard,
            Infinity
        }

        private sealed class ProgressRecord
        {
            public int rank;
            public bool firstInRankGroup;
            public string playerId;
            public string playerName;
            public int avatarId;
            public int clearedStage;
            public int totalStars;
            public bool isLocalPlayer;
        }

        private sealed class ModeProgress
        {
            public int clearedStage;
            public int totalStars;
            public int maxStage;
            public int maxStars;
        }

        private sealed class HeaderRefs
        {
            public RectTransform title;
            public RectTransform subtitle;
        }

        private static readonly Color BackgroundColor = new Color(0.020f, 0.035f, 0.095f, 1f);
        private static readonly Color PanelColor = new Color32(0x0A, 0x2C, 0x5D, 0xFA);
        private static readonly Color InnerPanelColor = new Color32(0x01, 0x1A, 0x3B, 0xFA);
        private static readonly Color RowColor = new Color32(0x0C, 0x23, 0x44, 0xF2);
        private static readonly Color RowHighlightColor = new Color32(0x14, 0x36, 0x5F, 0xF5);
        private static readonly Color RowOutlineColor = new Color32(0x3A, 0x5E, 0x8E, 0xB8);
        private static readonly Color SelectedTabColor = new Color32(0x16, 0x75, 0xF5, 0xFF);
        private static readonly Color InactiveTabColor = new Color32(0x0B, 0x23, 0x48, 0xFF);
        private static readonly Color InactiveTabOutlineColor = new Color32(0x2E, 0x4D, 0x78, 0xD8);
        private static readonly Color GoldColor = new Color32(0xE3, 0xA2, 0x1A, 0xFF);
        private static readonly Color GoldHighlightColor = new Color32(0xFF, 0xD1, 0x5A, 0xFF);
        private static readonly Color CreamColor = new Color(1.000f, 0.925f, 0.730f, 1f);
        private static readonly Color MutedTextColor = new Color32(0xC7, 0xD8, 0xF2, 0xFF);

        private readonly GameObject root;
        private readonly RectTransform rowsContent;
        private readonly ScrollRect rowsScrollRect;
        private readonly VerticalLayoutGroup rowsLayout;
        private readonly RectTransform stickyRow;
        private readonly Text emptyText;
        private readonly Text statusText;
        private readonly Text localSummaryText;
        private readonly Button normalTab;
        private readonly Button hardTab;
        private readonly Button infinityTab;
        private readonly StageDataLoader stageLoader = new StageDataLoader();
        private readonly StageProgressStore progressStore = new StageProgressStore();
        private readonly PlayerProfileStore profileStore = new PlayerProfileStore();
        private readonly StageProgressRecordsApiClient recordsApiClient;
        private readonly Dictionary<string, RectTransform> rowsByPlayerId = new Dictionary<string, RectTransform>();

        private RecordsMode selectedMode = RecordsMode.Normal;
        private int refreshRequestId;
        private string stickyPlayerId = string.Empty;

        public RecordsPanelUI(Transform parent)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "WorldRecordsCanvas", 1470);
            root = canvas.gameObject;
            SettingsStore settingsStore = new SettingsStore();
            recordsApiClient = new StageProgressRecordsApiClient(
                settingsStore.Current.rankingApiBaseUrl,
                settingsStore.Current.rankingRequestTimeoutSeconds);

            RectTransform screen = root.GetComponent<RectTransform>();
            CreateBackdrop(screen);
            TopCurrencyBar.Attach(canvas, null, true);
            RectTransform panel = CreateMainPanel(screen);
            HeaderRefs header = CreateHeader(screen);
            ConfigureMobileTitleAlignment(header, panel);

            normalTab = CreateTabButton(panel, "NormalTab", "Normal", new Vector2(-304f, -66f), true);
            hardTab = CreateTabButton(panel, "HardTab", "Hard", new Vector2(0f, -66f), false);
            infinityTab = CreateTabButton(panel, "InfinityTab", "Infinity", new Vector2(304f, -66f), false);
            normalTab.onClick.AddListener(() => SelectMode(RecordsMode.Normal));
            hardTab.onClick.AddListener(() => SelectMode(RecordsMode.Hard));
            infinityTab.onClick.AddListener(() => SelectMode(RecordsMode.Infinity));

            CreateColumnHeader(panel);
            rowsContent = CreateRowsArea(panel, out rowsScrollRect, out rowsLayout);
            stickyRow = CreateStickyRow(panel);
            stickyRow.gameObject.SetActive(false);

            emptyText = RuntimeUiFactory.CreateText(panel, "EmptyText", T("no_world_records"), 34, TextAnchor.MiddleCenter);
            emptyText.fontStyle = FontStyle.Bold;
            emptyText.color = MutedTextColor;
            SetFixedTopRect(emptyText.rectTransform, 70f, -500f, 820f, 120f);

            statusText = RuntimeUiFactory.CreateText(screen, "StatusText", string.Empty, 25, TextAnchor.MiddleCenter);
            statusText.color = MutedTextColor;
            SetTopRect(statusText.rectTransform, -618f, 40f, -156f);
            statusText.gameObject.SetActive(false);

            localSummaryText = RuntimeUiFactory.CreateText(screen, "LocalSummary", string.Empty, 24, TextAnchor.MiddleCenter);
            localSummaryText.color = CreamColor;
            SetTopRect(localSummaryText.rectTransform, -1500f, 58f, -120f);
            localSummaryText.gameObject.SetActive(false);

            Button close = RuntimeUiFactory.CreateButton(screen, "BackButton", T("back"), new Vector2(0f, 72f), new Vector2(336f, 74f));
            close.onClick.AddListener(Hide);
            Hide();
        }

        public void Show()
        {
            Refresh();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private void SelectMode(RecordsMode mode)
        {
            if (selectedMode == mode)
            {
                return;
            }

            selectedMode = mode;
            Refresh();
        }

        private void Refresh()
        {
            StyleTabs();

            ModeProgress progress = BuildLocalProgress(selectedMode);
            List<ProgressRecord> records = BuildLocalRecords(progress);
            ApplyCompetitionRanks(records);
            string initialStatus = recordsApiClient != null && recordsApiClient.HasServerUrl
                ? $"Loading World Records - {GetModeName(selectedMode)}"
                : $"Local Progress - {GetModeName(selectedMode)}";
            RenderRecords(records, progress, initialStatus, FindLocalRecord(records));
            _ = RefreshServerRecordsAsync(selectedMode, progress, ++refreshRequestId);
        }

        private async Task RefreshServerRecordsAsync(RecordsMode mode, ModeProgress progress, int requestId)
        {
            if (recordsApiClient == null || !recordsApiClient.HasServerUrl)
            {
                return;
            }

            PlayerProfile profile = profileStore.Current;
            StageProgressRecordsResult submittedRank = null;
            if (profile != null
                && !string.IsNullOrWhiteSpace(profile.profileId)
                && (progress.clearedStage > 0 || progress.totalStars > 0))
            {
                StageProgressRecordSubmitDto payload = new StageProgressRecordSubmitDto
                {
                    playerId = profile.profileId,
                    nickname = string.IsNullOrWhiteSpace(profile.nickname) ? "Player" : profile.nickname,
                    profileImageId = Mathf.Max(0, profile.avatarId),
                    mode = GetApiMode(mode),
                    clearedStage = progress.clearedStage,
                    totalStars = progress.totalStars,
                    clientUpdatedAtUtc = DateTime.UtcNow.ToString("o")
                };
                submittedRank = await recordsApiClient.SubmitAsync(payload);
            }

            StageProgressRecordsResult result = await recordsApiClient.GetLeaderboardAsync(GetApiMode(mode), 50);
            StageProgressRecordsResult myRank = profile != null && !string.IsNullOrWhiteSpace(profile.profileId)
                ? await recordsApiClient.GetMyRankAsync(GetApiMode(mode), profile.profileId)
                : null;
            if (requestId != refreshRequestId || selectedMode != mode)
            {
                return;
            }

            if (!result.success)
            {
                List<ProgressRecord> fallback = BuildLocalRecords(progress);
                ApplyCompetitionRanks(fallback);
                RenderRecords(
                    fallback,
                    progress,
                    fallback.Count > 0 ? "Server unavailable - Showing local records" : "Server unavailable. Try again later",
                    FindLocalRecord(fallback));
                return;
            }

            List<ProgressRecord> serverRecords = result.records
                .Select(dto => new ProgressRecord
                {
                    rank = dto.rank,
                    firstInRankGroup = !dto.tied,
                    playerId = dto.playerId,
                    playerName = string.IsNullOrWhiteSpace(dto.nickname) ? "Player" : dto.nickname,
                    avatarId = Mathf.Clamp(dto.profileImageId, 0, 3),
                    clearedStage = dto.clearedStage,
                    totalStars = dto.totalStars,
                    isLocalPlayer = profile != null && dto.playerId == profile.profileId
                })
                .OrderByDescending(record => record.clearedStage)
                .ThenByDescending(record => record.totalStars)
                .ThenBy(record => record.playerName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ApplyCompetitionRanks(serverRecords);
            ProgressRecord stickyRecord = ConvertFirstServerRecord(myRank, profile)
                ?? ConvertFirstServerRecord(submittedRank, profile)
                ?? FindLocalRecord(serverRecords);
            RenderRecords(serverRecords, progress, $"Server Ranking - {GetModeName(mode)}", stickyRecord);
        }

        private void RenderRecords(List<ProgressRecord> records, ModeProgress progress, string statusPrefix, ProgressRecord stickyRecord)
        {
            ClearRows();

            int visibleCount = Math.Min(50, records.Count);
            for (int i = 0; i < visibleCount; i++)
            {
                RectTransform row = CreateRecordRow(rowsContent, records[i], i);
                if (!string.IsNullOrWhiteSpace(records[i]?.playerId))
                {
                    rowsByPlayerId[records[i].playerId] = row;
                }
            }

            SetStickyRow(stickyRecord);
            Canvas.ForceUpdateCanvases();
            UpdateStickyVisibility();
            emptyText.gameObject.SetActive(records.Count == 0 && stickyRecord == null);
            statusText.text = string.Empty;
            localSummaryText.text = string.Empty;
        }

        private ModeProgress BuildLocalProgress(RecordsMode mode)
        {
            StageType type = GetStageType(mode);
            IReadOnlyList<StageData> stages = stageLoader.GetStagesByType(type)
                .OrderBy(stage => stage.stageNumber)
                .ToList();

            int clearedOrdinal = 0;
            int totalStars = 0;
            for (int i = 0; i < stages.Count; i++)
            {
                StageProgress progress = progressStore.GetProgress(stages[i].stageId);
                if (progress == null)
                {
                    continue;
                }

                if (progress.isCleared)
                {
                    clearedOrdinal = Math.Max(clearedOrdinal, i + 1);
                }

                totalStars += Mathf.Clamp(progress.stars, 0, 3);
            }

            return new ModeProgress
            {
                clearedStage = clearedOrdinal,
                totalStars = totalStars,
                maxStage = stages.Count,
                maxStars = stages.Count * 3
            };
        }

        private List<ProgressRecord> BuildLocalRecords(ModeProgress progress)
        {
            var records = new List<ProgressRecord>();
            PlayerProfile profile = profileStore.Current;
            if (profile != null && (!string.IsNullOrWhiteSpace(profile.nickname) || progress.clearedStage > 0 || progress.totalStars > 0))
            {
                records.Add(new ProgressRecord
                {
                    playerId = profile.profileId,
                    playerName = string.IsNullOrWhiteSpace(profile.nickname) ? "Player" : profile.nickname,
                    avatarId = Mathf.Clamp(profile.avatarId, 0, 3),
                    clearedStage = progress.clearedStage,
                    totalStars = progress.totalStars,
                    isLocalPlayer = true
                });
            }

            return records
                .OrderByDescending(record => record.clearedStage)
                .ThenByDescending(record => record.totalStars)
                .ThenBy(record => record.playerName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static ProgressRecord ConvertFirstServerRecord(StageProgressRecordsResult result, PlayerProfile profile)
        {
            StageProgressRecordDto dto = result?.records?.FirstOrDefault();
            if (dto == null)
            {
                return null;
            }

            return new ProgressRecord
            {
                rank = dto.rank,
                firstInRankGroup = !dto.tied,
                playerId = dto.playerId,
                playerName = string.IsNullOrWhiteSpace(dto.nickname) ? "Player" : dto.nickname,
                avatarId = Mathf.Clamp(dto.profileImageId, 0, 3),
                clearedStage = dto.clearedStage,
                totalStars = dto.totalStars,
                isLocalPlayer = profile != null && dto.playerId == profile.profileId
            };
        }

        private static ProgressRecord FindLocalRecord(List<ProgressRecord> records)
        {
            return records?.FirstOrDefault(record => record != null && record.isLocalPlayer);
        }

        private static void ApplyCompetitionRanks(List<ProgressRecord> records)
        {
            int previousStage = -1;
            int previousStars = -1;
            int currentRank = 0;
            for (int i = 0; i < records.Count; i++)
            {
                ProgressRecord record = records[i];
                bool sameRank = i > 0
                    && record.clearedStage == previousStage
                    && record.totalStars == previousStars;
                if (!sameRank)
                {
                    currentRank = i + 1;
                }

                record.rank = currentRank;
                record.firstInRankGroup = !sameRank;
                previousStage = record.clearedStage;
                previousStars = record.totalStars;
            }
        }

        private static void CreateBackdrop(RectTransform parent)
        {
            GameObject backdropObject = new GameObject("WorldRecordsBackdrop", typeof(RectTransform), typeof(Image));
            backdropObject.transform.SetParent(parent, false);
            RectTransform rect = backdropObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = backdropObject.GetComponent<Image>();
            Sprite backgroundSprite = LoadResourceSprite("UI/Records/world_records_background");
            if (backgroundSprite != null)
            {
                image.sprite = backgroundSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }
            else
            {
                image.color = BackgroundColor;
            }

            image.raycastTarget = false;
            rect.SetAsFirstSibling();
        }

        private static HeaderRefs CreateHeader(RectTransform parent)
        {
            Text title = RuntimeUiFactory.CreateText(parent, "WorldRecordsTitle", T("world_records_title"), 76, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -240f);
            title.rectTransform.sizeDelta = new Vector2(0f, 106f);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.84f, 0.25f, 1f);
            CasualUIStyle.ApplyTextDepth(title, true);

            Text subtitle = RuntimeUiFactory.CreateText(parent, "WorldRecordsSubtitle", T("world_records_subtitle"), 28, TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -364f);
            subtitle.rectTransform.sizeDelta = new Vector2(0f, 44f);
            subtitle.color = new Color(1f, 0.93f, 0.55f, 0.98f);
            CasualUIStyle.ApplyTextDepth(subtitle, false);

            return new HeaderRefs
            {
                title = title.rectTransform,
                subtitle = subtitle.rectTransform
            };
        }

        private void ConfigureMobileTitleAlignment(HeaderRefs header, RectTransform panel)
        {
            if (header == null || header.title == null)
            {
                return;
            }

            MobileTitleSectionAligner aligner = root.GetComponent<MobileTitleSectionAligner>()
                ?? root.AddComponent<MobileTitleSectionAligner>();
            aligner.Configure(
                "WorldRecords",
                header.title,
                new[] { header.subtitle, panel },
                -222f,
                false);
        }

        private static Button CreateTabButton(RectTransform parent, string name, string label, Vector2 position, bool selected)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(276f, 76f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            Text text = RuntimeUiFactory.CreateText(rect, "Label", label, 30, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(10f, 0f);
            text.rectTransform.offsetMax = new Vector2(-10f, 0f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = 30;

            SetTabVisual(button, selected);
            return button;
        }

        private static RectTransform CreateMainPanel(RectTransform parent)
        {
            GameObject panelObject = new GameObject("WorldRecordsPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -500f);
            rect.sizeDelta = new Vector2(1008f, 1258f);

            Image image = panelObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, PanelColor, 34);
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = GoldColor;
            outline.effectDistance = new Vector2(5f, -5f);
            Shadow shadow = panelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(0xA9, 0x6B, 0x09, 0x82);
            shadow.effectDistance = new Vector2(0f, -12f);
            return rect;
        }

        private static void CreateColumnHeader(RectTransform panel)
        {
            RectTransform header = CreatePlainRect(panel, "ColumnHeader", new Vector2(0f, -128f), new Vector2(930f, 60f));
            CreateHeaderText(header, "RankHeader", T("rank"), 28, 0f, 0f, 136f, TextAnchor.MiddleCenter);
            CreateHeaderText(header, "PlayerHeader", T("player"), 28, 160f, 0f, 380f, TextAnchor.MiddleLeft);
            CreateHeaderText(header, "StageHeader", T("stage"), 28, 590f, 0f, 140f, TextAnchor.MiddleCenter);
            CreateHeaderText(header, "StarsHeader", T("stars"), 28, 744f, 0f, 170f, TextAnchor.MiddleCenter);
        }

        private RectTransform CreateRowsArea(RectTransform parent, out ScrollRect scroll, out VerticalLayoutGroup layout)
        {
            GameObject scrollObject = new GameObject("WorldRecordsRows", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);
            RectTransform rect = scrollObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -198f);
            rect.sizeDelta = new Vector2(940f, 1038f);

            Image image = scrollObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, InnerPanelColor, 26);
            image.raycastTarget = true;
            Outline outline = scrollObject.AddComponent<Outline>();
            outline.effectColor = new Color32(0x02, 0x21, 0x4A, 0xD0);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(rect, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(14f, 14f);
            viewport.offsetMax = new Vector2(-14f, -14f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = false;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38f;
            scroll.onValueChanged.AddListener(_ => UpdateStickyVisibility());
            return content;
        }

        private static RectTransform CreateStickyRow(RectTransform panel)
        {
            RectTransform row = CasualUIFactory.CreatePanel(panel, "LocalRecordStickyRow", new Color32(0x08, 0x30, 0x56, 0xFC), 18);
            row.anchorMin = new Vector2(0.5f, 0f);
            row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, 32f);
            row.sizeDelta = new Vector2(940f, 108f);
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(0xFF, 0xD1, 0x5A, 0xE6);
            outline.effectDistance = new Vector2(3f, -3f);
            Shadow shadow = row.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(0xE3, 0xA2, 0x1A, 0x55);
            shadow.effectDistance = new Vector2(0f, -6f);
            return row;
        }

        private static RectTransform CreateRecordRow(RectTransform parent, ProgressRecord record, int rowIndex)
        {
            GameObject rowObject = new GameObject($"RecordRow{rowIndex + 1}", typeof(RectTransform), typeof(Image));
            rowObject.transform.SetParent(parent, false);
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, 108f);

            LayoutElement layout = rowObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 108f;
            layout.minHeight = 108f;

            Image rowImage = rowObject.GetComponent<Image>();
            Color baseColor = record.isLocalPlayer ? RowHighlightColor : RowColor;
            CasualUIStyle.ApplyPanel(rowImage, baseColor, 18);
            Outline outline = rowObject.AddComponent<Outline>();
            outline.effectColor = record.isLocalPlayer
                ? new Color32(0xFF, 0xD1, 0x5A, 0xCC)
                : RowOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            CreateRank(row, record);
            CreateAvatar(row, record.avatarId);
            CreateValueText(row, "Player", record.playerName, 31, 252f, 0f, 300f, TextAnchor.MiddleLeft, Color.white, true);
            CreateValueText(row, "Stage", record.clearedStage.ToString(), 36, 590f, 0f, 140f, TextAnchor.MiddleCenter, GoldColor, true);
            CreateStarValue(row, record.totalStars);
            return row;
        }

        private static void CreateRank(RectTransform row, ProgressRecord record)
        {
            if (record.rank <= 3 && record.firstInRankGroup && CreateRankMedal(row, record.rank))
            {
                return;
            }

            string text = record.firstInRankGroup ? FormatRank(record.rank) : "=";
            Text rank = CreateValueText(row, "Rank", text, record.rank <= 3 && record.firstInRankGroup ? 35 : 32, 0f, 0f, 132f, TextAnchor.MiddleCenter, GetRankColor(record.rank), true);
            if (record.rank <= 3 && record.firstInRankGroup)
            {
                Outline outline = rank.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        private static bool CreateRankMedal(RectTransform row, int rank)
        {
            Sprite medal = LoadResourceSprite($"UI/Records/world_rank_{rank}");
            if (medal == null)
            {
                return false;
            }

            GameObject medalObject = new GameObject($"RankMedal{rank}", typeof(RectTransform), typeof(Image));
            medalObject.transform.SetParent(row, false);
            RectTransform rect = medalObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(66f, 0f);
            rect.sizeDelta = rank == 1 ? new Vector2(104f, 104f) : new Vector2(98f, 98f);

            Image image = medalObject.GetComponent<Image>();
            image.sprite = medal;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return true;
        }

        private static void CreateAvatar(RectTransform row, int avatarId)
        {
            GameObject avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Outline));
            avatarObject.transform.SetParent(row, false);
            RectTransform avatar = avatarObject.GetComponent<RectTransform>();
            avatar.anchorMin = new Vector2(0f, 0.5f);
            avatar.anchorMax = new Vector2(0f, 0.5f);
            avatar.pivot = new Vector2(0.5f, 0.5f);
            avatar.anchoredPosition = new Vector2(198f, 0f);
            avatar.sizeDelta = new Vector2(72f, 72f);
            Image image = avatarObject.GetComponent<Image>();
            int normalizedAvatarId = Mathf.Clamp(avatarId, 0, 3);
            Sprite avatarSprite = LoadAvatarSprite(normalizedAvatarId);
            if (avatarSprite != null)
            {
                image.sprite = avatarSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                CasualUIStyle.ApplyPanel(image, GetAvatarColor(normalizedAvatarId), 22);
            }
            Outline outline = avatarObject.GetComponent<Outline>();
            outline.effectColor = CreamColor;
            outline.effectDistance = new Vector2(2f, -2f);

            if (avatarSprite == null)
            {
                Text label = RuntimeUiFactory.CreateText(avatar, "AvatarLabel", $"A{normalizedAvatarId + 1}", 22, TextAnchor.MiddleCenter);
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                CasualUIStyle.ApplyTextDepth(label, true);
            }
        }

        private static void CreateStarValue(RectTransform row, int totalStars)
        {
            Sprite star = Resources.Load<Sprite>("UI/Stages/Generated/star_row_1")
                ?? Resources.Load<Sprite>("UI/Stages/Generated/icon_star_large");
            if (star != null)
            {
                GameObject starObject = new GameObject("StarIcon", typeof(RectTransform), typeof(Image));
                starObject.transform.SetParent(row, false);
                RectTransform rect = starObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(772f, 0f);
                rect.sizeDelta = new Vector2(58f, 52f);
                Image image = starObject.GetComponent<Image>();
                image.sprite = star;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
                image.raycastTarget = false;
            }

            CreateValueText(row, "Stars", totalStars.ToString(), 34, 806f, 0f, 112f, TextAnchor.MiddleLeft, Color.white, true);
        }

        private void StyleTabs()
        {
            SetTabVisual(normalTab, selectedMode == RecordsMode.Normal);
            SetTabVisual(hardTab, selectedMode == RecordsMode.Hard);
            SetTabVisual(infinityTab, selectedMode == RecordsMode.Infinity);
        }

        private void ClearRows()
        {
            for (int i = rowsContent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(rowsContent.GetChild(i).gameObject);
            }

            rowsByPlayerId.Clear();
        }

        private void SetStickyRow(ProgressRecord record)
        {
            if (stickyRow == null)
            {
                return;
            }

            for (int i = stickyRow.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(stickyRow.GetChild(i).gameObject);
            }

            bool hasRecord = record != null && record.rank > 0 && !string.IsNullOrWhiteSpace(record.playerId);
            stickyPlayerId = hasRecord ? record.playerId : string.Empty;
            stickyRow.gameObject.SetActive(hasRecord);
            SetRowsBottomPadding(hasRecord ? 128 : 0);
            if (!hasRecord)
            {
                return;
            }

            RectTransform labelPanel = CasualUIFactory.CreatePanel(stickyRow, "StickyLabelPanel", new Color32(0x03, 0x18, 0x31, 0xF2), 14);
            labelPanel.anchorMin = new Vector2(0.014f, 0.140f);
            labelPanel.anchorMax = new Vector2(0.205f, 0.860f);
            labelPanel.offsetMin = Vector2.zero;
            labelPanel.offsetMax = Vector2.zero;

            Text prefix = RuntimeUiFactory.CreateText(labelPanel, "StickyPrefix", "Your record", 20, TextAnchor.MiddleCenter);
            prefix.rectTransform.anchorMin = Vector2.zero;
            prefix.rectTransform.anchorMax = Vector2.one;
            prefix.rectTransform.offsetMin = Vector2.zero;
            prefix.rectTransform.offsetMax = Vector2.zero;
            prefix.color = GoldHighlightColor;
            prefix.fontStyle = FontStyle.Bold;
            CasualUIStyle.ApplyTextDepth(prefix, true);

            Text rank = CreateValueText(stickyRow, "StickyRank", FormatRank(record.rank), 28, 220f, 0f, 112f, TextAnchor.MiddleCenter, GoldHighlightColor, true);
            rank.fontStyle = FontStyle.Bold;
            CreateValueText(stickyRow, "StickyPlayer", record.playerName, 28, 350f, 0f, 210f, TextAnchor.MiddleLeft, Color.white, true);
            CreateValueText(stickyRow, "StickyStage", record.clearedStage.ToString(), 34, 590f, 0f, 140f, TextAnchor.MiddleCenter, GoldColor, true);
            CreateStarValue(stickyRow, record.totalStars);
        }

        private void SetRowsBottomPadding(int bottomPadding)
        {
            if (rowsLayout != null)
            {
                rowsLayout.padding = new RectOffset(0, 0, 0, bottomPadding);
            }
        }

        private void UpdateStickyVisibility()
        {
            if (stickyRow == null || string.IsNullOrWhiteSpace(stickyPlayerId))
            {
                return;
            }

            if (!rowsByPlayerId.TryGetValue(stickyPlayerId, out RectTransform row))
            {
                stickyRow.gameObject.SetActive(true);
                return;
            }

            stickyRow.gameObject.SetActive(!IsRowVisibleInViewport(row));
        }

        private bool IsRowVisibleInViewport(RectTransform row)
        {
            if (row == null || rowsScrollRect == null || rowsScrollRect.viewport == null)
            {
                return false;
            }

            Vector3[] rowCorners = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            row.GetWorldCorners(rowCorners);
            rowsScrollRect.viewport.GetWorldCorners(viewportCorners);
            return rowCorners[1].y <= viewportCorners[1].y && rowCorners[0].y >= viewportCorners[0].y;
        }

        private static void SetTabVisual(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, selected ? SelectedTabColor : InactiveTabColor, 24);
            Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? GoldHighlightColor : InactiveTabOutlineColor;
            outline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontStyle = FontStyle.Bold;
                label.color = selected ? Color.white : new Color(0.84f, 0.86f, 0.93f, 1f);
                label.fontSize = 30;
                CasualUIStyle.ApplyTextDepth(label, true);
            }
        }

        private static Text CreateHeaderText(RectTransform parent, string name, string text, int size, float x, float y, float width, TextAnchor anchor)
        {
            Text label = CreateValueText(parent, name, text, size, x, y, width, anchor, CreamColor, true);
            label.fontStyle = FontStyle.Bold;
            return label;
        }

        private static Text CreateValueText(RectTransform parent, string name, string text, int size, float x, float y, float width, TextAnchor anchor, Color color, bool depth)
        {
            Text label = RuntimeUiFactory.CreateText(parent, name, text, size, anchor);
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, size - 10);
            label.resizeTextMaxSize = size;
            SetCenterLeftRect(label.rectTransform, x, y, width, 64f);
            if (depth)
            {
                CasualUIStyle.ApplyTextDepth(label, true);
            }

            return label;
        }

        private static RectTransform CreatePlainRect(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static StageType GetStageType(RecordsMode mode)
        {
            switch (mode)
            {
                case RecordsMode.Hard:
                    return StageType.ReverseTargetStage;
                case RecordsMode.Infinity:
                    return StageType.InfinityStage;
                default:
                    return StageType.SolveStage;
            }
        }

        private static string GetModeName(RecordsMode mode)
        {
            switch (mode)
            {
                case RecordsMode.Hard:
                    return "Hard";
                case RecordsMode.Infinity:
                    return "Infinity";
                default:
                    return "Normal";
            }
        }

        private static string GetApiMode(RecordsMode mode)
        {
            switch (mode)
            {
                case RecordsMode.Hard:
                    return "hard";
                case RecordsMode.Infinity:
                    return "infinity";
                default:
                    return "normal";
            }
        }

        private static string FormatRank(int rank)
        {
            if (rank > 999)
            {
                return "???";
            }

            switch (rank)
            {
                case 1: return "1";
                case 2: return "2";
                case 3: return "3";
                default: return rank.ToString();
            }
        }

        private static Color GetRankColor(int rank)
        {
            switch (rank)
            {
                case 1: return new Color(1f, 0.76f, 0.16f, 1f);
                case 2: return new Color(0.86f, 0.90f, 0.96f, 1f);
                case 3: return new Color(0.92f, 0.48f, 0.20f, 1f);
                default: return Color.white;
            }
        }

        private static Color GetAvatarColor(int avatarId)
        {
            switch (Mathf.Clamp(avatarId, 0, 3))
            {
                case 1: return new Color(0.92f, 0.35f, 0.55f, 1f);
                case 2: return new Color(0.55f, 0.34f, 0.96f, 1f);
                case 3: return new Color(0.15f, 0.62f, 0.58f, 1f);
                default: return new Color(0.24f, 0.47f, 0.94f, 1f);
            }
        }

        private static Sprite LoadAvatarSprite(int avatarId)
        {
            int normalizedAvatarId = Mathf.Clamp(avatarId, 0, 3);
            return Resources.Load<Sprite>($"UI/Profile/Avatars/profile_avatar_{normalizedAvatarId + 1}");
        }

        private static Sprite LoadResourceSprite(string path)
        {
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void SetTopRect(RectTransform rect, float y, float height, float horizontalMargin)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(horizontalMargin, height);
        }

        private static void SetFixedTopRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetCenterLeftRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static string T(string key)
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key;
        }
    }
}
