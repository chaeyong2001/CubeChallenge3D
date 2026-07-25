using System;
using System.Collections.Generic;
using System.Text;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Core;
using CubeChallenge3D.Learn.Model;
using CubeChallenge3D.Learn.Playback;
using CubeChallenge3D.Learn.Services;
using CubeChallenge3D.Learn.Storage;
using CubeChallenge3D.Solver.Model;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Solver.Playback;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Learn
{
    public sealed class LearnLessonBrowserUI
    {
        private static readonly Color LearnPanelBackgroundColor = new Color32(7, 21, 31, 255);
        private const float ReferenceCanvasHeight = 1920f;
        private const float DetailViewportDefaultPadding = 38f;
        private const float DetailFooterViewportBottomInset = 38f;
        private const float CaseNavigationViewportBottomInset = 108f;
        private const float LessonDetailGuideBoxBottomY = 360f;
        private const float LessonDetailGuideBoxMinHeight = 760f;
        private const float CaseNavigationButtonBottomY = 386f;
        private const float CaseNavigationLabelBottomY = 392f;
        private const float SolverLearnHeaderTitleY = -278f;
        private const float SolverLearnHeaderSubtitleY = -350f;
        private const float LessonListBaseTitleY = -300f;
        private const float LessonListBaseSubtitleY = -380f;
        private const float LessonListPanelTopY = -520f;
        private const float LessonListPanelBaseHeight = 950f;
        private const float LessonListBackButtonY = 150f;
        private const float LessonListBackButtonHeight = 78f;
        private const float LessonListPanelBackGap = 32f;
        private const float LessonDetailPanelTopY = -455f;
        private const float LessonDetailPanelHeight = 1040f;
        private const float LessonDemoContentHeight = 1020f;

        private readonly GameObject root;
        private readonly GameObject listRoot;
        private readonly GameObject detailRoot;
        private readonly Text listTitle;
        private readonly Text listDescription;
        private readonly RectTransform lessonContent;
        private readonly RectTransform lessonFrame;
        private readonly Text detailTitle;
        private readonly Text detailBody;
        private readonly RectTransform detailContent;
        private readonly RectTransform detailFrame;
        private readonly RectTransform lessonTextRoot;
        private readonly RectTransform lessonDemoRoot;
        private readonly Text detailStatus;
        private readonly Text substepText;
        private readonly Button demoButton;
        private readonly Button completeButton;
        private readonly Button previousSubstepButton;
        private readonly Button nextSubstepButton;
        private readonly Text listBackButtonText;
        private readonly Text detailBackButtonText;
        private readonly LearnContentProvider contentProvider;
        private readonly LearnLessonProgressStore progressStore;
        private readonly SolverPlaybackPanelUI playbackPanel;
        private LearnCategoryData currentCategory;
        private LearnLessonData currentLesson;
        private int currentSubstepIndex;

        public event Action Closed;

        public LearnLessonBrowserUI(
            Transform parent,
            LearnContentProvider provider,
            LearnLessonProgressStore progress)
        {
            contentProvider = provider ?? new LearnContentProvider();
            progressStore = progress ?? new LearnLessonProgressStore();
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "LearnLessonsCanvas", 1510, 0f);
            root = canvas.gameObject;
            CasualUIFactory.CreateBackdrop(root.transform, "LearnLessonsBackdrop", true, false);

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "LearnLessonsPanel",
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = Color.clear;
                panelImage.raycastTarget = false;
            }

            listRoot = CreateFullRoot(panel, "LessonListRoot");
            detailRoot = CreateFullRoot(panel, "LessonDetailRoot");

            listTitle = CreateTopText(listRoot.transform, "Title", T("lessons"), 58, LessonListBaseTitleY, 74f);
            listDescription = CreateTopText(listRoot.transform, "Description", string.Empty, 28, LessonListBaseSubtitleY, 48f);
            lessonContent = CreateScrollArea(listRoot.transform, "LessonScroll", LessonListPanelTopY, LessonListPanelBaseHeight, true);
            lessonFrame = GetScrollFrame(lessonContent);

            Button listBack = RuntimeUiFactory.CreateButton(
                listRoot.GetComponent<RectTransform>(),
                "BackButton",
                T("back"),
                new Vector2(0f, 150f),
                new Vector2(360f, 78f));
            CasualUIStyle.ApplyButton(listBack, CasualUIColor.Blue);
            StyleButtonText(listBack, 34);
            listBackButtonText = listBack.GetComponentInChildren<Text>();
            listBack.onClick.AddListener(CloseToHub);

            detailTitle = CreateTopText(detailRoot.transform, "Title", T("lesson"), 58, -305f, 78f);
            detailContent = CreateScrollArea(detailRoot.transform, "DetailScroll", LessonDetailPanelTopY, LessonDetailPanelHeight, true);
            detailFrame = GetScrollFrame(detailContent);
            lessonTextRoot = CreateContentRoot(detailContent, "LessonTextRoot");
            lessonDemoRoot = CreateContentRoot(detailContent, "LessonDemoRoot");

            detailBody = RuntimeUiFactory.CreateText(lessonTextRoot, "DetailText", string.Empty, 36, TextAnchor.UpperLeft);
            detailBody.rectTransform.anchorMin = new Vector2(0f, 1f);
            detailBody.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailBody.rectTransform.pivot = new Vector2(0.5f, 1f);
            detailBody.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            detailBody.rectTransform.sizeDelta = new Vector2(-110f, 940f);
            detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailBody.verticalOverflow = VerticalWrapMode.Overflow;
            detailBody.lineSpacing = 1.18f;
            CasualUIStyle.ApplyTextDepth(detailBody, false);
            detailBody.gameObject.SetActive(false);

            detailStatus = RuntimeUiFactory.CreateText(
                detailRoot.GetComponent<RectTransform>(),
                "Status",
                string.Empty,
                24,
                TextAnchor.MiddleCenter);
            detailStatus.rectTransform.anchorMin = new Vector2(0f, 0f);
            detailStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
            detailStatus.rectTransform.pivot = new Vector2(0.5f, 0f);
            detailStatus.rectTransform.anchoredPosition = new Vector2(0f, 306f);
            detailStatus.rectTransform.sizeDelta = new Vector2(-180f, 46f);

            substepText = RuntimeUiFactory.CreateText(
                detailRoot.GetComponent<RectTransform>(),
                "Substep",
                string.Empty,
                20,
                TextAnchor.MiddleCenter);
            substepText.rectTransform.anchorMin = new Vector2(0f, 0f);
            substepText.rectTransform.anchorMax = new Vector2(1f, 0f);
            substepText.rectTransform.pivot = new Vector2(0.5f, 0f);
            substepText.rectTransform.anchoredPosition = new Vector2(0f, 344f);
            substepText.rectTransform.sizeDelta = new Vector2(-210f, 44f);

            previousSubstepButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "PreviousSubstepButton",
                T("previous_edge"),
                new Vector2(-194f, 292f),
                new Vector2(220f, 50f));
            previousSubstepButton.onClick.AddListener(PreviousSubstep);

            nextSubstepButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "NextSubstepButton",
                T("next_edge"),
                new Vector2(194f, 292f),
                new Vector2(220f, 50f));
            nextSubstepButton.onClick.AddListener(NextSubstep);

            StyleSmallButton(previousSubstepButton);
            StyleSmallButton(nextSubstepButton);

            completeButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "CompleteButton",
                T("mark_complete"),
                new Vector2(-235f, 172f),
                new Vector2(430f, 86f));
            CasualUIStyle.ApplyButton(completeButton, CasualUIColor.Blue);
            StyleButtonText(completeButton, 34);
            completeButton.onClick.AddListener(MarkComplete);

            demoButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "Play3DDemoButton",
                T("play_demo"),
                new Vector2(235f, 172f),
                new Vector2(430f, 86f));
            CasualUIStyle.ApplyButton(demoButton, CasualUIColor.Blue);
            StyleButtonText(demoButton, 34);
            demoButton.onClick.AddListener(PlayDemo);

            Button detailBack = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "BackToLessonsButton",
                T("back_to_lessons"),
                new Vector2(235f, 62f),
                new Vector2(430f, 76f));
            CasualUIStyle.ApplyButton(detailBack, CasualUIColor.Blue);
            StyleButtonText(detailBack, 32);
            detailBackButtonText = detailBack.GetComponentInChildren<Text>();
            detailBack.onClick.AddListener(ShowLessonList);

            playbackPanel = new SolverPlaybackPanelUI(
                lessonDemoRoot,
                ExitLessonDemoMode,
                T("learn_3d_demo"),
                T("learn_3d_demo_subtitle"),
                true);
            lessonDemoRoot.gameObject.SetActive(false);

            Hide();
        }

        public void ShowCategory(string categoryId)
        {
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.LearnDetailBgmReason, true);
            IReadOnlyList<LearnCategoryData> categories = contentProvider.GetCategories();
            currentCategory = null;
            foreach (LearnCategoryData category in categories)
            {
                if (string.Equals(category.categoryId, categoryId, StringComparison.OrdinalIgnoreCase))
                {
                    currentCategory = category;
                    break;
                }
            }

            root.SetActive(true);
            detailRoot.SetActive(false);
            listRoot.SetActive(true);
            RefreshStaticLocalizedText();
            BuildLessonList();
            ApplyLessonListDeviceLayout();
        }

        public void Hide()
        {
            playbackPanel?.Hide();
            root.SetActive(false);
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.LearnDetailBgmReason, false);
        }

        private void BuildLessonList()
        {
            ClearChildren(lessonContent);
            if (currentCategory == null)
            {
                listTitle.text = T("lessons");
                listDescription.text = T("no_lesson_category");
                lessonContent.sizeDelta = new Vector2(0f, 100f);
                return;
            }

            listTitle.text = L(currentCategory.title);
            listDescription.text = L(currentCategory.description);
            IReadOnlyList<LearnLessonData> lessons = contentProvider.GetLessons(currentCategory.categoryId);
            const float rowHeight = 250f;
            const float spacing = 34f;
            float totalHeight = Mathf.Max(880f, lessons.Count * rowHeight + Mathf.Max(0, lessons.Count - 1) * spacing);
            lessonContent.sizeDelta = new Vector2(0f, totalHeight);

            for (int i = 0; i < lessons.Count; i++)
            {
                LearnLessonData lesson = lessons[i];
                CreateLessonRow(lesson, i, rowHeight, spacing);
            }
        }

        private void ApplyLessonListDeviceLayout()
        {
            float canvasHeight = GetCanvasHeight();
            float extraHeight = Mathf.Max(0f, canvasHeight - ReferenceCanvasHeight);
            float phoneT = extraHeight > 0.5f ? 1f : 0f;
            float phoneSafeTopInset = phoneT > 0f ? GetSafeAreaTopInsetInCanvas(canvasHeight) : 0f;

            SetTopTextY(listTitle, Mathf.Lerp(LessonListBaseTitleY, SolverLearnHeaderTitleY - phoneSafeTopInset, phoneT));
            SetTopTextY(listDescription, Mathf.Lerp(LessonListBaseSubtitleY, SolverLearnHeaderSubtitleY - phoneSafeTopInset, phoneT));

            if (lessonFrame == null)
            {
                return;
            }

            float panelHeight = LessonListPanelBaseHeight;
            if (extraHeight > 0.5f)
            {
                float targetBottomFromBottom = LessonListBackButtonY + LessonListBackButtonHeight + LessonListPanelBackGap;
                panelHeight = Mathf.Max(
                    LessonListPanelBaseHeight,
                    canvasHeight - Mathf.Abs(LessonListPanelTopY) - targetBottomFromBottom);
            }

            lessonFrame.anchoredPosition = new Vector2(lessonFrame.anchoredPosition.x, LessonListPanelTopY);
            lessonFrame.sizeDelta = new Vector2(lessonFrame.sizeDelta.x, panelHeight);
        }

        private void CreateLessonRow(LearnLessonData lesson, int index, float height, float spacing)
        {
            GameObject rowObject = new GameObject(
                lesson.lessonId + "Row",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            rowObject.transform.SetParent(lessonContent, false);
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -index * (height + spacing));
            row.sizeDelta = new Vector2(-72f, height);
            Image rowImage = rowObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(rowImage, new Color(0.035f, 0.105f, 0.19f, 0.98f), 28);
            Outline rowOutline = rowObject.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0.45f, 0.65f, 0.88f, 0.54f);
            rowOutline.effectDistance = new Vector2(2f, -2f);
            Shadow rowShadow = rowObject.AddComponent<Shadow>();
            rowShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            rowShadow.effectDistance = new Vector2(0f, -6f);

            CreateLessonIcon(row, lesson.lessonId);

            Text title = RuntimeUiFactory.CreateText(
                row,
                "Title",
                L(lesson.title),
                36,
                TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(238f, -60f);
            title.rectTransform.sizeDelta = new Vector2(-490f, 56f);
            CasualUIStyle.ApplyTextDepth(title, true);

            Text body = RuntimeUiFactory.CreateText(
                row,
                "Description",
                L(lesson.shortDescription),
                27,
                TextAnchor.UpperLeft);
            body.color = new Color(0.88f, 0.91f, 0.98f, 1f);
            body.rectTransform.anchorMin = new Vector2(0f, 1f);
            body.rectTransform.anchorMax = new Vector2(1f, 1f);
            body.rectTransform.pivot = new Vector2(0f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(238f, -118f);
            body.rectTransform.sizeDelta = new Vector2(-490f, 96f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            CasualUIStyle.ApplyTextDepth(body, false);

            Button open = RuntimeUiFactory.CreateButton(row, "OpenButton", T("open"), Vector2.zero, new Vector2(190f, 78f));
            CasualUIStyle.ApplyButton(open, CasualUIColor.Blue);
            RectTransform openRect = open.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(1f, 0.5f);
            openRect.anchorMax = new Vector2(1f, 0.5f);
            openRect.pivot = new Vector2(1f, 0.5f);
            openRect.anchoredPosition = new Vector2(-36f, 0f);
            StyleButtonText(open, 34);
            open.onClick.AddListener(() => ShowLesson(lesson));
            rowObject.GetComponent<Button>().onClick.AddListener(() => ShowLesson(lesson));
        }

        private void ShowLesson(LearnLessonData lesson)
        {
            currentLesson = lesson;
            currentSubstepIndex = 0;
            listRoot.SetActive(false);
            detailRoot.SetActive(true);
            ShowLessonTextMode();
            RefreshLessonDetail();
        }

        private void RefreshLessonDetail()
        {
            RefreshStaticLocalizedText();
            LearnStepDemoData substep = GetCurrentSubstep();
            detailTitle.text = currentLesson != null ? L(currentLesson.title) : T("lesson_unavailable");
            bool demoValid = LearnPlaybackAdapter.TryCreateSolution(
                currentLesson,
                substep,
                out _,
                out _);
            float preferredHeight = RebuildDetailContent(currentLesson, substep, demoValid);
            detailContent.sizeDelta = new Vector2(0f, preferredHeight);
            lessonTextRoot.sizeDelta = new Vector2(0f, preferredHeight);
            lessonDemoRoot.sizeDelta = new Vector2(0f, LessonDemoContentHeight);
            bool hasCaseNavigation = HasCaseNavigation(currentLesson);
            ApplyDetailViewportBottomInset(hasCaseNavigation);
            ScrollRect detailScroll = detailContent.GetComponentInParent<ScrollRect>();
            if (detailScroll != null)
            {
                detailScroll.verticalNormalizedPosition = 1f;
            }
            demoButton.interactable = demoValid;
            CasualUIStyle.ApplyButton(demoButton, demoValid ? CasualUIColor.Blue : CasualUIColor.Slate);
            StyleButtonText(demoButton, 34);
            completeButton.interactable = currentLesson != null && !progressStore.IsCompleted(currentLesson.lessonId);
            CasualUIStyle.ApplyButton(completeButton, completeButton.interactable ? CasualUIColor.Blue : CasualUIColor.Slate);
            StyleButtonText(completeButton, 34);
            if (currentLesson != null && progressStore.IsCompleted(currentLesson.lessonId))
            {
                detailStatus.text = T("completed");
            }
            else if (!demoValid)
            {
                detailStatus.text = currentLesson != null && currentLesson.isExpandedContent
                    ? T("no_3d_demo_yet")
                    : T("no_3d_demo");
            }
            else
            {
                detailStatus.text = string.Empty;
            }

            int substepCount = currentLesson?.demoSubsteps?.Length ?? 0;
            bool hasSubsteps = substepCount > 0;
            substepText.gameObject.SetActive(hasSubsteps);
            previousSubstepButton.gameObject.SetActive(hasSubsteps);
            nextSubstepButton.gameObject.SetActive(hasSubsteps);
            ApplySubstepNavigationLayout(hasSubsteps);
            if (hasSubsteps)
            {
                substepText.text = hasCaseNavigation
                    ? L(substep.substepTitle)
                    : $"{L(substep.substepTitle)}  ({currentSubstepIndex + 1}/{substepCount})";
                string itemName = string.Equals(currentLesson.demoType, "BeginnerMultiStep", StringComparison.Ordinal)
                    ? "edge"
                    : "case";
                previousSubstepButton.GetComponentInChildren<Text>().text = T(itemName == "edge" ? "previous_edge" : "previous_case");
                nextSubstepButton.GetComponentInChildren<Text>().text = T(itemName == "edge" ? "next_edge" : "next_case");
                previousSubstepButton.interactable = currentSubstepIndex > 0;
                nextSubstepButton.interactable = currentSubstepIndex < substepCount - 1;
            }
        }

        private void ApplySubstepNavigationLayout(bool hasSubsteps)
        {
            if (!hasSubsteps)
            {
                return;
            }

            SetBottomAnchored(previousSubstepButton.GetComponent<RectTransform>(), new Vector2(-365f, CaseNavigationButtonBottomY), new Vector2(220f, 70f));
            SetBottomAnchored(nextSubstepButton.GetComponent<RectTransform>(), new Vector2(365f, CaseNavigationButtonBottomY), new Vector2(220f, 70f));
            SetBottomAnchored(substepText.rectTransform, new Vector2(0f, CaseNavigationLabelBottomY), new Vector2(420f, 58f));
            SetBottomAnchored(detailStatus.rectTransform, new Vector2(0f, 306f), new Vector2(-180f, 46f), true);
            StyleButtonText(previousSubstepButton, 24);
            StyleButtonText(nextSubstepButton, 24);
            substepText.fontSize = 24;
            substepText.resizeTextForBestFit = true;
            substepText.resizeTextMinSize = 17;
            substepText.resizeTextMaxSize = 24;
            detailStatus.fontSize = 24;
        }

        private void ApplyDetailViewportBottomInset(bool reserveCaseNavigationArea)
        {
            if (detailFrame != null)
            {
                float canvasHeight = GetCanvasHeight();
                float topDistanceFromCanvasTop = Mathf.Abs(LessonDetailPanelTopY);
                float guideHeight = Mathf.Max(
                    LessonDetailGuideBoxMinHeight,
                    canvasHeight - topDistanceFromCanvasTop - LessonDetailGuideBoxBottomY);
                detailFrame.anchoredPosition = new Vector2(detailFrame.anchoredPosition.x, LessonDetailPanelTopY);
                detailFrame.sizeDelta = new Vector2(detailFrame.sizeDelta.x, guideHeight);
            }

            RectTransform viewport = detailContent.parent as RectTransform;
            if (viewport == null)
            {
                return;
            }

            float bottomInset = reserveCaseNavigationArea
                ? CaseNavigationViewportBottomInset
                : DetailFooterViewportBottomInset;
            viewport.offsetMin = new Vector2(DetailViewportDefaultPadding, bottomInset);
            viewport.offsetMax = new Vector2(-DetailViewportDefaultPadding, -DetailViewportDefaultPadding);
        }

        private float RebuildDetailContent(LearnLessonData lesson, LearnStepDemoData substep, bool hasDemo)
        {
            ClearGeneratedChildren(lessonTextRoot);
            if (detailBody != null)
            {
                detailBody.gameObject.SetActive(false);
            }

            float y = -10f;
            AddDetailBullet(lessonTextRoot, ref y, L(lesson?.shortDescription));
            AddBodyParagraphs(lessonTextRoot, ref y, L(lesson?.bodyText));

            if (substep != null)
            {
                AddDetailBullet(lessonTextRoot, ref y, L(substep.substepTitle));
                AddDetailBullet(lessonTextRoot, ref y, L(substep.instructionText));
            }

            string[] demoMoves = substep?.demoMoves != null && substep.demoMoves.Length > 0
                ? substep.demoMoves
                : GetDemoMoves(lesson);
            if (demoMoves.Length > 0)
            {
                AddDetailBullet(lessonTextRoot, ref y, T("moves_label") + ": " + string.Join(" ", demoMoves));
            }

            if (!string.IsNullOrWhiteSpace(lesson?.demoType))
            {
                AddDetailBullet(lessonTextRoot, ref y, T("demo_type_label") + ": " + GetDemoTypeLabel(lesson.demoType));
            }

            if (!string.IsNullOrWhiteSpace(lesson?.demoPurpose))
            {
                AddDetailBullet(lessonTextRoot, ref y, T("demo_purpose_label") + ": " + L(lesson.demoPurpose));
            }

            if (!string.IsNullOrWhiteSpace(lesson?.demoGoalDescription))
            {
                AddDetailBullet(lessonTextRoot, ref y, T("demo_goal_label") + ": " + L(lesson.demoGoalDescription));
            }

            string pieceHint = !string.IsNullOrWhiteSpace(substep?.highlightedCubieHint)
                ? substep.highlightedCubieHint
                : lesson?.highlightedCubieHint;
            if (!string.IsNullOrWhiteSpace(pieceHint))
            {
                AddDetailBullet(lessonTextRoot, ref y, T("target_piece_label") + ": " + L(pieceHint));
            }

            string slotHint = !string.IsNullOrWhiteSpace(substep?.targetSlotHint)
                ? substep.targetSlotHint
                : lesson?.targetSlotHint;
            if (!string.IsNullOrWhiteSpace(slotHint))
            {
                AddDetailBullet(lessonTextRoot, ref y, T("target_slot_label") + ": " + L(slotHint));
            }

            if (lesson?.keyPoints != null && lesson.keyPoints.Length > 0)
            {
                AddDetailSectionHeader(lessonTextRoot, ref y, T("key_points"));
                foreach (string point in lesson.keyPoints)
                {
                    AddDetailBullet(lessonTextRoot, ref y, L(point), 31, new Color(0.94f, 0.95f, 0.99f, 1f));
                }
            }

            if (lesson != null && !hasDemo)
            {
                AddDetailNote(lessonTextRoot, ref y, T("no_3d_demo"));
            }

            if (lesson != null && lesson.isExpandedContent)
            {
                AddDetailNote(lessonTextRoot, ref y, T("learn_more_later"));
            }

            if (HasCaseNavigation(lesson))
            {
                y -= 32f;
            }

            return Mathf.Max(940f, -y + 28f);
        }

        private void ShowLessonList()
        {
            playbackPanel?.Hide();
            currentLesson = null;
            detailRoot.SetActive(false);
            listRoot.SetActive(true);
            BuildLessonList();
        }

        private void PlayDemo()
        {
            LearnStepDemoData substep = GetCurrentSubstep();
            if (!LearnPlaybackAdapter.TryCreateSolution(currentLesson, substep, out SolverSolution solution, out string error))
            {
                detailStatus.text = L(error);
                return;
            }

            LocalizePlaybackSolution(solution);
            detailStatus.text = string.Empty;
            string goal = !string.IsNullOrWhiteSpace(substep?.targetDescription)
                ? substep.targetDescription
                : currentLesson.demoGoalDescription;
            string guide = string.IsNullOrWhiteSpace(goal)
                ? T("learn_default_demo_guide")
                : L(goal);
            if (currentLesson.demoType != null
                && currentLesson.demoType.StartsWith("Beginner", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(false);
                guide += " " + T("learn_white_bottom_guide");
                if (string.Equals(currentLesson.demoType, "BeginnerMultiStep", StringComparison.Ordinal))
                {
                    guide += " " + T("learn_yellow_edge_cyan_slot");
                    playbackPanel.SetInitialViewEuler(Vector3.zero);
                    playbackPanel.SetCameraLocalPosition(new Vector3(0f, 3.2f, 6.5f));
                    playbackPanel.SetCompletionCameraLocalPosition(new Vector3(0f, -5.2f, 4.8f));
                    playbackPanel.SetLearningHighlights(
                        true,
                        new Vector3Int(
                            substep?.targetStartX ?? 0,
                            substep?.targetStartY ?? 1,
                            substep?.targetStartZ ?? 1),
                        new Vector3Int(0, -1, 1));
                }
                else if (string.Equals(currentLesson.demoType, "BeginnerCornerStep", StringComparison.Ordinal))
                {
                    guide += " " + T("learn_yellow_corner_cyan_slot");
                    float cameraX = (substep?.targetGoalX ?? 1) > 0 ? 4f : -4f;
                    playbackPanel.SetInitialViewEuler(Vector3.zero);
                    playbackPanel.SetCameraLocalPosition(new Vector3(cameraX, 3.2f, 6.5f));
                    playbackPanel.SetCompletionCameraLocalPosition(new Vector3(cameraX * 0.45f, -5.2f, 4.8f));
                    playbackPanel.SetLearningHighlights(
                        true,
                        new Vector3Int(
                            substep?.targetStartX ?? 1,
                            substep?.targetStartY ?? 1,
                            substep?.targetStartZ ?? 1),
                        new Vector3Int(
                            substep?.targetGoalX ?? 1,
                            substep?.targetGoalY ?? -1,
                            substep?.targetGoalZ ?? 1));
                }
                else if (string.Equals(currentLesson.demoType, "BeginnerSecondLayer", StringComparison.Ordinal))
                {
                    playbackPanel.SetInitialViewEuler(Vector3.zero);
                    playbackPanel.SetCameraLocalPosition(new Vector3(4f, 2.4f, 6.5f));
                    playbackPanel.SetCompletionCameraLocalPosition(new Vector3(5.2f, 0.8f, 5.2f));
                    playbackPanel.SetLearningHighlights(
                        true,
                        new Vector3Int(substep.targetStartX, substep.targetStartY, substep.targetStartZ),
                        new Vector3Int(substep.targetGoalX, substep.targetGoalY, substep.targetGoalZ));
                }
                else if (string.Equals(currentLesson.demoType, "BeginnerYellowCross", StringComparison.Ordinal)
                    || string.Equals(currentLesson.demoType, "BeginnerYellowFace", StringComparison.Ordinal))
                {
                    playbackPanel.SetInitialViewEuler(Vector3.zero);
                    playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                    playbackPanel.SetCompletionCameraLocalPosition(new Vector3(0f, 5.8f, 4.2f));
                    playbackPanel.SetLearningHighlights(false, Vector3Int.zero, Vector3Int.zero);
                }
                else if (string.Equals(currentLesson.demoType, "BeginnerLastCorners", StringComparison.Ordinal))
                {
                    playbackPanel.SetInitialViewEuler(Vector3.zero);
                    playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                    playbackPanel.SetCompletionCameraLocalPosition(new Vector3(5.2f, 4.6f, 5.2f));
                    playbackPanel.SetLearningHighlights(
                        true,
                        new Vector3Int(substep.targetStartX, substep.targetStartY, substep.targetStartZ),
                        new Vector3Int(substep.targetGoalX, substep.targetGoalY, substep.targetGoalZ));
                }
                else if (string.Equals(currentLesson.demoType, "BeginnerLastEdges", StringComparison.Ordinal))
                {
                    playbackPanel.SetInitialViewEuler(Vector3.zero);
                    playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                    playbackPanel.SetCompletionCameraLocalPosition(new Vector3(4.8f, 4.8f, 5.4f));
                    playbackPanel.SetLearningHighlights(false, Vector3Int.zero, Vector3Int.zero);
                }
                else
                {
                    playbackPanel.SetInitialViewEuler(new Vector3(-22f, 0f, 0f));
                    playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                    playbackPanel.ClearCompletionCameraPosition();
                    playbackPanel.SetLearningHighlights(false, Vector3Int.zero, Vector3Int.zero);
                }
            }
            else if (string.Equals(currentLesson.demoType, "FormulaRightTrigger", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(false);
                guide += " " + T("learn_keep_white_corner_slot");
                playbackPanel.SetInitialViewEuler(Vector3.zero);
                playbackPanel.SetCameraLocalPosition(new Vector3(4f, 3.2f, 6.5f));
                playbackPanel.SetCompletionCameraLocalPosition(new Vector3(3.2f, -4.6f, 5.4f));
                playbackPanel.SetLearningHighlights(
                    true,
                    new Vector3Int(
                        substep?.targetStartX ?? -1,
                        substep?.targetStartY ?? 1,
                        substep?.targetStartZ ?? 1),
                    new Vector3Int(
                        substep?.targetGoalX ?? -1,
                        substep?.targetGoalY ?? -1,
                        substep?.targetGoalZ ?? 1));
            }
            else if (string.Equals(currentLesson.demoType, "FormulaLeftTrigger", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(false);
                guide += " " + T("learn_keep_white_corner_slot");
                playbackPanel.SetInitialViewEuler(Vector3.zero);
                playbackPanel.SetCameraLocalPosition(new Vector3(-4f, 3.2f, 6.5f));
                playbackPanel.SetCompletionCameraLocalPosition(new Vector3(-3.2f, -4.6f, 5.4f));
                playbackPanel.SetLearningHighlights(
                    true,
                    new Vector3Int(substep.targetStartX, substep.targetStartY, substep.targetStartZ),
                    new Vector3Int(substep.targetGoalX, substep.targetGoalY, substep.targetGoalZ));
            }
            else if (string.Equals(currentLesson.demoType, "FormulaSledgehammer", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(false);
                guide += " " + T("learn_prepared_pair_slot");
                playbackPanel.SetInitialViewEuler(Vector3.zero);
                playbackPanel.SetCameraLocalPosition(new Vector3(4f, 3.2f, 6.5f));
                playbackPanel.SetCompletionCameraLocalPosition(new Vector3(5.2f, 0.8f, 5.2f));
                playbackPanel.SetLearningHighlights(
                    true,
                    new Vector3Int(substep.targetStartX, substep.targetStartY, substep.targetStartZ),
                    new Vector3Int(substep.targetGoalX, substep.targetGoalY, substep.targetGoalZ));
            }
            else if (string.Equals(currentLesson.demoType, "FormulaYellowCross", StringComparison.Ordinal)
                || string.Equals(currentLesson.demoType, "FormulaRightAlgorithm", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(false);
                guide += " " + T("learn_solved_layers_top_result");
                playbackPanel.SetInitialViewEuler(Vector3.zero);
                playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                playbackPanel.SetCompletionCameraLocalPosition(new Vector3(0f, 5.8f, 4.2f));
                playbackPanel.SetLearningHighlights(false, Vector3Int.zero, Vector3Int.zero);
            }
            else if (string.Equals(currentLesson.demoType, "FormulaPattern", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(true);
                guide += " " + T("learn_pattern_demo_only");
                playbackPanel.SetInitialViewEuler(Vector3.zero);
                playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                playbackPanel.ClearCompletionCameraPosition();
                playbackPanel.SetLearningHighlights(false, Vector3Int.zero, Vector3Int.zero);
            }
            else
            {
                playbackPanel.SetShowFaceLabels(true);
                playbackPanel.SetInitialViewEuler(Vector3.zero);
                playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                playbackPanel.ClearCompletionCameraPosition();
                playbackPanel.SetLearningHighlights(false, Vector3Int.zero, Vector3Int.zero);
            }

            playbackPanel.SetPresentation(L(currentLesson.title) + " " + T("demo"), guide);
            ShowLessonDemoMode();
            playbackPanel.Show(solution);
        }

        private static void LocalizePlaybackSolution(SolverSolution solution)
        {
            if (solution == null
                || LocalizationManager.Instance == null
                || LocalizationManager.Instance.CurrentLanguage != AppLanguage.Korean)
            {
                return;
            }

            solution.completionMessage = L(solution.completionMessage);
            solution.completionGuideMessage = L(solution.completionGuideMessage);
            if (solution.moveDescriptions == null)
            {
                return;
            }

            for (int i = 0; i < solution.moveDescriptions.Length; i++)
            {
                solution.moveDescriptions[i] = TranslateMoveDescription(solution.moveDescriptions[i]);
            }
        }

        private static string TranslateMoveDescription(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            const string moveMarker = "\nMove ";
            int markerIndex = text.IndexOf(moveMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return L(text);
            }

            string instruction = text.Substring(0, markerIndex);
            string moveLine = text.Substring(markerIndex + 1);
            return L(instruction) + "\n" + TranslateMoveLine(moveLine);
        }

        private static string TranslateMoveLine(string moveLine)
        {
            if (string.IsNullOrWhiteSpace(moveLine) || !moveLine.StartsWith("Move ", StringComparison.Ordinal))
            {
                return L(moveLine);
            }

            return "\ub3d9\uc791 " + moveLine.Substring("Move ".Length);
        }

        private void RefreshStaticLocalizedText()
        {
            if (listBackButtonText != null)
            {
                listBackButtonText.text = T("back");
            }

            if (detailBackButtonText != null)
            {
                detailBackButtonText.text = T("back_to_lessons");
            }

            SetButtonText(completeButton, T("mark_complete"));
            SetButtonText(demoButton, T("play_demo"));
            SetButtonText(previousSubstepButton, T("previous_edge"));
            SetButtonText(nextSubstepButton, T("next_edge"));
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private void ShowLessonTextMode()
        {
            playbackPanel?.Hide();
            lessonDemoRoot.gameObject.SetActive(false);
            lessonTextRoot.gameObject.SetActive(true);
            ApplyDetailViewportBottomInset(HasCaseNavigation(currentLesson));
            SetCaseNavigationVisible(HasCaseNavigation(currentLesson));
            if (lessonTextRoot.sizeDelta.y > 0f)
            {
                detailContent.sizeDelta = new Vector2(0f, lessonTextRoot.sizeDelta.y);
            }
        }

        private void ShowLessonDemoMode()
        {
            lessonTextRoot.gameObject.SetActive(false);
            lessonDemoRoot.gameObject.SetActive(true);
            ApplyDetailViewportBottomInset(false);
            SetCaseNavigationVisible(false);
            detailContent.sizeDelta = new Vector2(0f, LessonDemoContentHeight);
            ScrollRect detailScroll = detailContent.GetComponentInParent<ScrollRect>();
            if (detailScroll != null)
            {
                detailScroll.verticalNormalizedPosition = 1f;
            }
        }

        private void ExitLessonDemoMode()
        {
            ShowLessonTextMode();
            ScrollRect detailScroll = detailContent.GetComponentInParent<ScrollRect>();
            if (detailScroll != null)
            {
                detailScroll.verticalNormalizedPosition = 1f;
            }
        }

        private void SetCaseNavigationVisible(bool visible)
        {
            substepText.gameObject.SetActive(visible);
            previousSubstepButton.gameObject.SetActive(visible);
            nextSubstepButton.gameObject.SetActive(visible);
            detailStatus.gameObject.SetActive(visible || lessonDemoRoot == null || !lessonDemoRoot.gameObject.activeSelf);
        }

        private void MarkComplete()
        {
            if (currentLesson == null)
            {
                return;
            }

            progressStore.MarkCompleted(currentLesson.lessonId);
            completeButton.interactable = false;
            CasualUIStyle.ApplyButton(completeButton, CasualUIColor.Slate);
            StyleButtonText(completeButton, 34);
            detailStatus.text = T("lesson_completed");
        }

        private void CloseToHub()
        {
            Hide();
            Closed?.Invoke();
        }

        private static string BuildDetailText(LearnLessonData lesson, LearnStepDemoData substep)
        {
            if (lesson == null)
            {
                return T("lesson_unavailable");
            }

            var builder = new StringBuilder();
            AppendBullet(builder, L(lesson.shortDescription));
            builder.AppendLine();
            AppendBulletParagraphs(builder, L(lesson.bodyText));

            if (substep != null)
            {
                builder.AppendLine();
                AppendBullet(builder, L(substep.substepTitle));
                AppendBullet(builder, L(substep.instructionText));
            }

            string[] demoMoves = substep?.demoMoves != null && substep.demoMoves.Length > 0
                ? substep.demoMoves
                : GetDemoMoves(lesson);
            if (demoMoves.Length > 0)
            {
                builder.AppendLine();
                AppendBullet(builder, T("moves_label") + ": " + string.Join(" ", demoMoves));
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoType))
            {
                builder.AppendLine();
                AppendBullet(builder, T("demo_type_label") + ": " + GetDemoTypeLabel(lesson.demoType));
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoPurpose))
            {
                builder.AppendLine();
                AppendBullet(builder, T("demo_purpose_label") + ": " + L(lesson.demoPurpose));
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoGoalDescription))
            {
                builder.AppendLine();
                AppendBullet(builder, T("demo_goal_label") + ": " + L(lesson.demoGoalDescription));
            }

            string pieceHint = !string.IsNullOrWhiteSpace(substep?.highlightedCubieHint)
                ? substep.highlightedCubieHint
                : lesson.highlightedCubieHint;
            if (!string.IsNullOrWhiteSpace(pieceHint))
            {
                builder.AppendLine();
                AppendBullet(builder, T("target_piece_label") + ": " + L(pieceHint));
            }

            string slotHint = !string.IsNullOrWhiteSpace(substep?.targetSlotHint)
                ? substep.targetSlotHint
                : lesson.targetSlotHint;
            if (!string.IsNullOrWhiteSpace(slotHint))
            {
                AppendBullet(builder, T("target_slot_label") + ": " + L(slotHint));
            }

            if (lesson.keyPoints != null && lesson.keyPoints.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine(T("key_points"));
                foreach (string point in lesson.keyPoints)
                {
                    builder.AppendLine("  - " + L(point));
                }
            }

            if (lesson.isExpandedContent)
            {
                builder.AppendLine();
                builder.AppendLine(T("learn_more_later"));
            }

            return builder.ToString();
        }

        private static void AppendBulletParagraphs(StringBuilder builder, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string normalized = text.Replace("\r\n", "\n");
            string[] paragraphs = normalized.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string paragraph in paragraphs)
            {
                AppendBullet(builder, paragraph.Trim());
                builder.AppendLine();
            }
        }

        private static void AppendBullet(StringBuilder builder, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string formatted = text.Replace("\r\n", "\n").Replace("\n", "\n  ");
            builder.AppendLine("- " + formatted);
        }

        private static string[] GetDemoMoves(LearnLessonData lesson)
        {
            if (lesson == null)
            {
                return Array.Empty<string>();
            }

            return lesson.demoMoves != null && lesson.demoMoves.Length > 0
                ? lesson.demoMoves
                : lesson.moveNotations ?? Array.Empty<string>();
        }

        private static string GetDemoTypeLabel(string demoType)
        {
            switch (demoType)
            {
                case "NotationMove": return T("demo_type_move");
                case "FormulaPattern": return T("demo_type_pattern");
                case "FormulaRightTrigger": return T("demo_type_right_trigger");
                case "FormulaLeftTrigger": return T("demo_type_left_trigger");
                case "FormulaSledgehammer": return T("demo_type_sledgehammer");
                case "FormulaYellowCross": return T("demo_type_yellow_cross");
                case "FormulaRightAlgorithm": return T("demo_type_right_algorithm");
                case "BeginnerStep": return T("demo_type_beginner_step");
                case "BeginnerMultiStep": return T("demo_type_beginner_multi");
                case "BeginnerCornerStep": return T("demo_type_beginner_corner");
                case "BeginnerSecondLayer": return T("demo_type_beginner_second_layer");
                case "BeginnerYellowCross": return T("demo_type_beginner_yellow_cross");
                case "BeginnerYellowFace": return T("demo_type_beginner_yellow_face");
                case "BeginnerLastCorners": return T("demo_type_beginner_last_corners");
                case "BeginnerLastEdges": return T("demo_type_beginner_last_edges");
                default: return T("demo_type_explanation");
            }
        }

        private LearnStepDemoData GetCurrentSubstep()
        {
            LearnStepDemoData[] substeps = currentLesson?.demoSubsteps;
            if (substeps == null || substeps.Length == 0)
            {
                return null;
            }

            currentSubstepIndex = Mathf.Clamp(currentSubstepIndex, 0, substeps.Length - 1);
            return substeps[currentSubstepIndex];
        }

        private void PreviousSubstep()
        {
            if (currentSubstepIndex <= 0)
            {
                return;
            }

            currentSubstepIndex--;
            RefreshLessonDetail();
        }

        private void NextSubstep()
        {
            int count = currentLesson?.demoSubsteps?.Length ?? 0;
            if (currentSubstepIndex >= count - 1)
            {
                return;
            }

            currentSubstepIndex++;
            RefreshLessonDetail();
        }

        private static string T(string key)
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key;
        }

        private static string L(string text)
        {
            if (string.IsNullOrWhiteSpace(text)
                || LocalizationManager.Instance == null
                || LocalizationManager.Instance.CurrentLanguage != AppLanguage.Korean)
            {
                return text;
            }

            if (KoreanLearnText.TryGetValue(text, out string translated))
            {
                return translated;
            }

            return TranslateGeneratedLearnText(text);
        }

        private static string TranslateGeneratedLearnText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            if (text.StartsWith("Find the white-", StringComparison.Ordinal)
                && text.Contains("then use F2 for the final insertion."))
            {
                foreach (KeyValuePair<string, string> color in KoreanColorNames)
                {
                    string token = "white-" + color.Key;
                    if (text.Contains(token))
                    {
                        return "\ud770\uc0c9-" + color.Value + " \uc5e3\uc9c0\ub97c \ucc3e\uace0 " + color.Value + " \uc13c\ud130\uac00 \uc815\uba74\uc744 \ubcf4\uac8c \ud558\uc138\uc694. "
                            + "\uc5e3\uc9c0\ub97c \uc704\uce35\uc5d0 \uc900\ube44\ud558\uace0 \uc606 \uc0c9\uc0c1\uc744 \uc13c\ud130\uc640 \ub9de\ucd98 \ub4a4, \ub9c8\uc9c0\ub9c9 \uc0bd\uc785\uc740 F2\ub97c \uc0ac\uc6a9\ud569\ub2c8\ub2e4.";
                    }
                }
            }

            if (text.StartsWith("Place the white-", StringComparison.Ordinal)
                && text.Contains("edge into the bottom-"))
            {
                foreach (KeyValuePair<string, string> color in KoreanColorNames)
                {
                    if (text.Contains("white-" + color.Key))
                    {
                        return "\ud770\uc0c9-" + color.Value + " \uc5e3\uc9c0\ub97c \uc544\ub798-" + color.Value + " \uc2ed\uc790 \uc2ac\ub86f\uc5d0 \ub123\uc2b5\ub2c8\ub2e4.";
                    }
                }
            }

            if (text.StartsWith("White-", StringComparison.Ordinal) && text.EndsWith(" edge", StringComparison.Ordinal))
            {
                foreach (KeyValuePair<string, string> color in KoreanColorNames)
                {
                    if (text.Contains("-" + color.Key))
                    {
                        return "\ud770\uc0c9-" + color.Value + " \uc5e3\uc9c0";
                    }
                }
            }

            if (text.StartsWith("Bottom-", StringComparison.Ordinal) && text.EndsWith(" cross slot", StringComparison.Ordinal))
            {
                foreach (KeyValuePair<string, string> color in KoreanColorNames)
                {
                    if (text.Contains("-" + color.Key))
                    {
                        return "\uc544\ub798-" + color.Value + " \uc2ed\uc790 \uc2ac\ub86f";
                    }
                }
            }

            if (text.StartsWith("Turn the ", StringComparison.Ordinal)
                && text.Contains(" face ")
                && text.EndsWith(".", StringComparison.Ordinal))
            {
                foreach (KeyValuePair<string, string> face in KoreanFaceNames)
                {
                    string faceToken = "the " + face.Key.ToLowerInvariant() + " face ";
                    if (text.Contains(faceToken))
                    {
                        string direction = text.Contains("counter-clockwise", StringComparison.Ordinal)
                            ? "\ubc18\uc2dc\uacc4\ubc29\ud5a5"
                            : text.Contains("180 degrees", StringComparison.Ordinal)
                                ? "180\ub3c4"
                                : "\uc2dc\uacc4\ubc29\ud5a5";
                        return face.Value + " \uba74\uc744 " + direction + "\uc73c\ub85c \ub3cc\ub9bd\ub2c8\ub2e4.";
                    }
                }
            }

            if (text.StartsWith("Look directly at the ", StringComparison.Ordinal)
                && text.Contains("Clockwise and counter-clockwise are measured from that view.", StringComparison.Ordinal))
            {
                foreach (KeyValuePair<string, string> face in KoreanFaceNames)
                {
                    if (text.Contains("the " + face.Key.ToLowerInvariant() + " face"))
                    {
                        return face.Value + " \uba74\uc744 \uc815\uba74\uc73c\ub85c \ubc14\ub77c\ubcf4\uc138\uc694. \uc2dc\uacc4\ubc29\ud5a5\uacfc \ubc18\uc2dc\uacc4\ubc29\ud5a5\uc740 \uadf8 \uc2dc\uc810 \uae30\uc900\uc785\ub2c8\ub2e4.";
                    }
                }
            }

            if (text.StartsWith("Watch the ", StringComparison.Ordinal) && text.EndsWith(" move on a solved cube.", StringComparison.Ordinal))
            {
                string move = text.Substring("Watch the ".Length, text.Length - "Watch the ".Length - " move on a solved cube.".Length);
                return "\uc644\uc131\ub41c \ud050\ube0c\uc5d0\uc11c " + move + " \ub3d9\uc791\uc744 \ud655\uc778\ud558\uc138\uc694.";
            }

            return text;
        }

        private static readonly Dictionary<string, string> KoreanColorNames = new Dictionary<string, string>
        {
            { "green", "\ucd08\ub85d" },
            { "red", "\ube68\uac15" },
            { "blue", "\ud30c\ub791" },
            { "orange", "\uc8fc\ud669" }
        };

        private static readonly Dictionary<string, string> KoreanFaceNames = new Dictionary<string, string>
        {
            { "Right", "\uc624\ub978\ucabd" },
            { "Top", "\uc704\ucabd" },
            { "Front", "\uc55e\ucabd" },
            { "Left", "\uc67c\ucabd" },
            { "Bottom", "\uc544\ub798\ucabd" },
            { "Back", "\ub4a4\ucabd" }
        };

        private static readonly Dictionary<string, string> KoreanLearnText = new Dictionary<string, string>
        {
            { "Learn Basics", "\uae30\ucd08 \ubc30\uc6b0\uae30" },
            { "Orientation, cube faces, and turn direction.", "\ud050\ube0c \ubc29\ud5a5, \uba74, \ud68c\uc804 \ubc29\ud5a5\uc744 \uc775\ud799\ub2c8\ub2e4." },
            { "Cube Orientation", "\ud050\ube0c \ubc29\ud5a5 \uc7a1\uae30" },
            { "Set a stable Front, Top, and Right before following moves.", "\uacf5\uc2dd\uc744 \ub530\ub77c \ud558\uae30 \uc804\uc5d0 \uc55e\uba74(F), \uc717\uba74(U), \uc624\ub978\ucabd\uba74(R)\uc744 \uace0\uc815\ud569\ub2c8\ub2e4." },
            { "Front is the face you are looking at. Top is the face on top. Right is the face on your right.\n\nKeep this orientation while following a move sequence.", "\uc55e\uba74(F)\uc740 \uc9c0\uae08 \ubc14\ub77c\ubcf4\ub294 \uba74, \uc717\uba74(U)\uc740 \uc704\ucabd \uba74, \uc624\ub978\ucabd\uba74(R)\uc740 \uc624\ub978\ucabd \uba74\uc785\ub2c8\ub2e4.\n\n\uacf5\uc2dd\uc744 \ub530\ub77c\uac00\ub294 \ub3d9\uc548 \uc774 \ubc29\ud5a5\uc744 \uc720\uc9c0\ud558\uc138\uc694." },
            { "Do not rotate the whole cube unless instructed.", "\ubcc4\ub3c4 \uc548\ub0b4\uac00 \uc5c6\uc73c\uba74 \ud050\ube0c \uc804\uccb4\ub97c \ub3cc\ub9ac\uc9c0 \ub9c8\uc138\uc694." },
            { "Moves use the current Front, Top, and Right orientation.", "\ub3d9\uc791\uc740 \ud604\uc7ac \uc55e\uba74(F), \uc717\uba74(U), \uc624\ub978\ucabd\uba74(R) \uae30\uc900\uc73c\ub85c \uc77d\uc2b5\ub2c8\ub2e4." },
            { "Cube Faces", "\ud050\ube0c\uc758 \uba74" },
            { "Learn the six standard face letters.", "\uc5ec\uc12f \uac1c \ud45c\uc900 \uba74 \uae30\ud638\ub97c \uc775\ud799\ub2c8\ub2e4." },
            { "U = Top\nD = Bottom\nR = Right\nL = Left\nF = Front\nB = Back", "U = \uc717\uba74\nD = \uc544\ub798\uba74\nR = \uc624\ub978\ucabd\uba74\nL = \uc67c\ucabd\uba74\nF = \uc55e\uba74\nB = \ub4b7\uba74" },
            { "Face letters describe physical faces, not screen movement.", "\uba74 \uae30\ud638\ub294 \ud654\uba74 \ubc29\ud5a5\uc774 \uc544\ub2c8\ub77c \uc2e4\uc81c \ud050\ube0c\uc758 \uba74\uc744 \ub73b\ud569\ub2c8\ub2e4." },
            { "Center stickers identify each face.", "\uc13c\ud130 \uc2a4\ud2f0\ucee4\uac00 \uac01 \uba74\uc758 \uc0c9\uacfc \uae30\uc900\uc744 \uc54c\ub824\uc90d\ub2c8\ub2e4." },
            { "Clockwise and Counter-clockwise", "\uc2dc\uacc4\ubc29\ud5a5\uacfc \ubc18\uc2dc\uacc4\ubc29\ud5a5" },
            { "Read normal, prime, and double turns.", "\uc77c\ubc18 \ud68c\uc804, \ud504\ub77c\uc784 \ud68c\uc804, 2\ud68c\uc804\uc744 \uc77d\ub294 \ubc95\uc744 \uc775\ud799\ub2c8\ub2e4." },
            { "A move without ' is clockwise. A move with ' is counter-clockwise. A move with 2 turns 180 degrees.\n\nClockwise always means clockwise when looking directly at that face.", "' \ud45c\uc2dc\uac00 \uc5c6\uc73c\uba74 \uc2dc\uacc4\ubc29\ud5a5, ' \ud45c\uc2dc\uac00 \uc788\uc73c\uba74 \ubc18\uc2dc\uacc4\ubc29\ud5a5, 2\uac00 \ubd99\uc73c\uba74 180\ub3c4 \ud68c\uc804\uc785\ub2c8\ub2e4.\n\n\uc2dc\uacc4\ubc29\ud5a5\uc740 \ud56d\uc0c1 \ud574\ub2f9 \uba74\uc744 \uc815\uba74\uc5d0\uc11c \ubc14\ub77c\ubcf8 \uae30\uc900\uc785\ub2c8\ub2e4." },
            { "Compare a normal, prime, and double right-face turn.", "\uc624\ub978\ucabd \uba74\uc758 \uc77c\ubc18 \ud68c\uc804, \ud504\ub77c\uc784 \ud68c\uc804, 2\ud68c\uc804\uc744 \ube44\uad50\ud569\ub2c8\ub2e4." },
            { "Learn how normal, prime, and double notation changes one face.", "\uc77c\ubc18, \ud504\ub77c\uc784, 2\ud68c\uc804 \ud45c\uae30\uac00 \ud55c \uba74\uc744 \uc5b4\ub5bb\uac8c \ubc14\uafb8\ub294\uc9c0 \uc775\ud799\ub2c8\ub2e4." },
            { "Look directly at the moving face.", "\uc6c0\uc9c1\uc774\ub294 \uba74\uc744 \uc815\uba74\uc5d0\uc11c \ubc14\ub77c\ubcf8\ub2e4\uace0 \uc0dd\uac01\ud558\uc138\uc694." },
            { "R2 and R2' describe the same 180-degree result.", "R2\uc640 R2'\ub294 \uac19\uc740 180\ub3c4 \uacb0\uacfc\ub97c \ub73b\ud569\ub2c8\ub2e4." },
            { "Notation", "\ud45c\uae30\ubc95" },
            { "Practice all 18 basic face moves.", "\uae30\ubcf8 18\uac00\uc9c0 \uba74 \ud68c\uc804\uc744 \uc5f0\uc2b5\ud569\ub2c8\ub2e4." },
            { "Beginner Method", "\ucd08\uae09 \uacf5\uc2dd" },
            { "A seven-step path for solving a 3x3 cube.", "3x3 \ud050\ube0c\ub97c \ud478\ub294 7\ub2e8\uacc4 \uacfc\uc815\uc785\ub2c8\ub2e4." },
            { "Formula Practice", "\uacf5\uc2dd \uc5f0\uc2b5" },
            { "Practice move patterns and use them in the correct case.", "\uacf5\uc2dd \ud328\ud134\uc744 \uc5f0\uc2b5\ud558\uace0 \ub9de\ub294 \uc0c1\ud669\uc5d0 \uc0ac\uc6a9\ud569\ub2c8\ub2e4." },
            { "Right Trigger", "\uc624\ub978\ucabd \ud2b8\ub9ac\uac70" },
            { "Open the right slot, move a top corner into place, then close the slot.", "\uc624\ub978\ucabd \uc2ac\ub86f\uc744 \uc5f4\uace0 \uc704\uce35 \ucf54\ub108\ub97c \ub123\uc740 \ub4a4 \uc2ac\ub86f\uc744 \ub2eb\uc2b5\ub2c8\ub2e4." },
            { "The beginner Right Trigger is R U R'.\n\nUse it when the target corner is in the top layer above its matching slot and must enter from the right side.\n\n1. R opens the right-side slot.\n2. U moves the target corner over the open slot.\n3. R' closes the slot and inserts the corner.\n\nR U R' U' is also commonly practised as a repeating four-move trigger, but the final U' is not required for this basic corner insertion case.", "\ucd08\uae09 \uc624\ub978\ucabd \ud2b8\ub9ac\uac70\ub294 R U R'\uc785\ub2c8\ub2e4.\n\n\ubaa9\ud45c \ucf54\ub108\uac00 \ub9de\ub294 \uc2ac\ub86f \uc704\uc5d0 \uc788\uace0 \uc624\ub978\ucabd\uc5d0\uc11c \ub4e4\uc5b4\uac00\uc57c \ud560 \ub54c \uc0ac\uc6a9\ud569\ub2c8\ub2e4.\n\n1. R\ub85c \uc624\ub978\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4.\n2. U\ub85c \ubaa9\ud45c \ucf54\ub108\ub97c \uc5f4\ub9b0 \uc2ac\ub86f \uc704\ub85c \uc62e\uae41\ub2c8\ub2e4.\n3. R'\ub85c \uc2ac\ub86f\uc744 \ub2eb\uc544 \ucf54\ub108\ub97c \ub123\uc2b5\ub2c8\ub2e4.\n\nR U R' U'\ub3c4 \ubc18\ubcf5 \uc5f0\uc2b5\uc6a9 4\ub3d9\uc791 \ud2b8\ub9ac\uac70\ub85c \uc790\uc8fc \uc4f0\uc774\uc9c0\ub9cc, \uc774 \uae30\ubcf8 \ucf54\ub108 \uc0bd\uc785\uc5d0\uc11c\ub294 \ub9c8\uc9c0\ub9c9 U'\uac00 \ud544\uc218\ub294 \uc544\ub2d9\ub2c8\ub2e4." },
            { "Show a real first-layer corner insertion instead of applying moves to a solved cube.", "\uc644\uc131\ub41c \ud050\ube0c\uc5d0 \uacf5\uc2dd\uc744 \uc801\uc6a9\ud558\ub294 \ub300\uc2e0 \uc2e4\uc81c 1\uce35 \ucf54\ub108 \uc0bd\uc785\uc744 \ubcf4\uc5ec\uc90d\ub2c8\ub2e4." },
            { "Insert the highlighted white corner into its matching first-layer slot.", "\ud45c\uc2dc\ub41c \ud770\uc0c9 \ucf54\ub108\ub97c \ub9de\ub294 1\uce35 \uc2ac\ub86f\uc5d0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Right-side Corner Insertion", "\uc624\ub978\ucabd \ucf54\ub108 \uc0bd\uc785" },
            { "The highlighted corner is above its matching slot. Keep white on the bottom and use R U R' to insert it from the right.", "\ud45c\uc2dc\ub41c \ucf54\ub108\uac00 \ub9de\ub294 \uc2ac\ub86f \uc704\uc5d0 \uc788\uc2b5\ub2c8\ub2e4. \ud770\uc0c9\uc744 \uc544\ub798\uc5d0 \ub450\uace0 R U R'\ub85c \uc624\ub978\ucabd\uc5d0\uc11c \ub123\uc2b5\ub2c8\ub2e4." },
            { "R: Open the right-side slot.", "R: \uc624\ub978\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "U: Move the highlighted corner over the open slot.", "U: \ud45c\uc2dc\ub41c \ucf54\ub108\ub97c \uc5f4\ub9b0 \uc2ac\ub86f \uc704\ub85c \uc62e\uae41\ub2c8\ub2e4." },
            { "R': Close the slot and insert the corner.", "R': \uc2ac\ub86f\uc744 \ub2eb\uace0 \ucf54\ub108\ub97c \ub123\uc2b5\ub2c8\ub2e4." },
            { "Yellow marks the target white corner; cyan marks its matching first-layer slot.", "\ub178\ub780\uc0c9\uc740 \ubaa9\ud45c \ud770\uc0c9 \ucf54\ub108, \ud558\ub298\uc0c9\uc740 \ub9de\ub294 1\uce35 \uc2ac\ub86f\uc744 \ud45c\uc2dc\ud569\ub2c8\ub2e4." },
            { "Target white corner", "\ubaa9\ud45c \ud770\uc0c9 \ucf54\ub108" },
            { "Right-side first-layer corner slot", "\uc624\ub978\ucabd 1\uce35 \ucf54\ub108 \uc2ac\ub86f" },
            { "Match all three corner colors with the surrounding centers before inserting.", "\ub123\uae30 \uc804\uc5d0 \ucf54\ub108\uc758 \uc138 \uc0c9\uc774 \uc8fc\ubcc0 \uc13c\ud130\uc640 \ub9de\ub294\uc9c0 \ud655\uc778\ud558\uc138\uc694." },
            { "Keep the solved white cross on the bottom.", "\uc644\uc131\ub41c \ud770\uc0c9 \uc2ed\uc790\ub294 \uc544\ub798\uc5d0 \uc720\uc9c0\ud569\ub2c8\ub2e4." },
            { "R opens the slot and R' restores it.", "R\uc740 \uc2ac\ub86f\uc744 \uc5f4\uace0 R'\ub294 \ub2e4\uc2dc \ub2eb\uc2b5\ub2c8\ub2e4." },
            { "Use the mirrored Left Trigger when the corner must enter from the left.", "\ucf54\ub108\uac00 \uc67c\ucabd\uc5d0\uc11c \ub4e4\uc5b4\uac00\uc57c \ud558\uba74 \ubc18\ub300 \ud615\ud0dc\uc778 \uc67c\ucabd \ud2b8\ub9ac\uac70\ub97c \uc0ac\uc6a9\ud569\ub2c8\ub2e4." },
            { "Left Trigger", "\uc67c\ucabd \ud2b8\ub9ac\uac70" },
            { "Insert a matching top corner through the left-side slot.", "\ub9de\ub294 \uc704\uce35 \ucf54\ub108\ub97c \uc67c\ucabd \uc2ac\ub86f\uc73c\ub85c \ub123\uc2b5\ub2c8\ub2e4." },
            { "The Left Trigger is L' U' L.\n\nUse it when a matched corner is above its target and must enter from the left side.\n\n1. L' opens the left-side slot.\n2. U' moves the target corner over the open slot.\n3. L closes the slot and inserts the corner.", "\uc67c\ucabd \ud2b8\ub9ac\uac70\ub294 L' U' L\uc785\ub2c8\ub2e4.\n\n\ub9de\ub294 \ucf54\ub108\uac00 \ubaa9\ud45c \uc2ac\ub86f \uc704\uc5d0 \uc788\uace0 \uc67c\ucabd\uc5d0\uc11c \ub4e4\uc5b4\uac00\uc57c \ud560 \ub54c \uc0ac\uc6a9\ud569\ub2c8\ub2e4.\n\n1. L'\ub85c \uc67c\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4.\n2. U'\ub85c \ubaa9\ud45c \ucf54\ub108\ub97c \uc5f4\ub9b0 \uc2ac\ub86f \uc704\ub85c \uc62e\uae41\ub2c8\ub2e4.\n3. L\ub85c \uc2ac\ub86f\uc744 \ub2eb\uc544 \ucf54\ub108\ub97c \ub123\uc2b5\ub2c8\ub2e4." },
            { "Left-side Corner Insertion", "\uc67c\ucabd \ucf54\ub108 \uc0bd\uc785" },
            { "Keep white on the bottom. Insert the highlighted corner into the matching left-side first-layer slot.", "\ud770\uc0c9\uc744 \uc544\ub798\uc5d0 \ub461\ub2c8\ub2e4. \ud45c\uc2dc\ub41c \ucf54\ub108\ub97c \ub9de\ub294 \uc67c\ucabd 1\uce35 \uc2ac\ub86f\uc5d0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "L': Open the left-side slot.", "L': \uc67c\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "U': Move the highlighted corner over the open slot.", "U': \ud45c\uc2dc\ub41c \ucf54\ub108\ub97c \uc5f4\ub9b0 \uc2ac\ub86f \uc704\ub85c \uc62e\uae41\ub2c8\ub2e4." },
            { "L: Close the slot and insert the corner.", "L: \uc2ac\ub86f\uc744 \ub2eb\uace0 \ucf54\ub108\ub97c \ub123\uc2b5\ub2c8\ub2e4." },
            { "Left-side first-layer corner slot", "\uc67c\ucabd 1\uce35 \ucf54\ub108 \uc2ac\ub86f" },
            { "Match all three corner colors before inserting.", "\uc0bd\uc785 \uc804\uc5d0 \ucf54\ub108\uc758 \uc138 \uc0c9\uc744 \ubaa8\ub450 \ub9de\ucda5\ub2c8\ub2e4." },
            { "Keep the white cross on the bottom.", "\ud770\uc0c9 \uc2ed\uc790\ub97c \uc544\ub798\uc5d0 \uc720\uc9c0\ud569\ub2c8\ub2e4." },
            { "The Left Trigger mirrors the Right Trigger.", "\uc67c\ucabd \ud2b8\ub9ac\uac70\ub294 \uc624\ub978\ucabd \ud2b8\ub9ac\uac70\uc758 \ubc18\ub300 \ud615\ud0dc\uc785\ub2c8\ub2e4." },
            { "Sledgehammer", "\uc2ac\ub808\uc9c0\ud574\uba38" },
            { "Insert or reorient a prepared corner-edge pair without rotating the whole cube.", "\ud050\ube0c \uc804\uccb4\ub97c \ub3cc\ub9ac\uc9c0 \uc54a\uace0 \uc900\ube44\ub41c \ucf54\ub108-\uc5e3\uc9c0 \ud398\uc5b4\ub97c \ub123\uac70\ub098 \ubc29\ud5a5\uc744 \ubc14\uafc9\ub2c8\ub2e4." },
            { "The Sledgehammer is R' F R F'.\n\nIt is commonly used in F2L and last-layer cases because it moves a corner-edge pair while changing edge orientation.\n\nIn this demo, the highlighted corner and its matching edge are prepared together above their slot. The four moves place the pair into the first two layers.", "\uc2ac\ub808\uc9c0\ud574\uba38\ub294 R' F R F'\uc785\ub2c8\ub2e4.\n\n\uc5e3\uc9c0 \ubc29\ud5a5\uc744 \ubc14\uafb8\uba74\uc11c \ucf54\ub108-\uc5e3\uc9c0 \ud398\uc5b4\ub97c \uc6c0\uc9c1\uc77c \uc218 \uc788\uc5b4 F2L\uacfc \ub9c8\uc9c0\ub9c9 \uce35\uc5d0\uc11c \uc790\uc8fc \uc0ac\uc6a9\ub429\ub2c8\ub2e4.\n\n\uc774 \ub370\ubaa8\uc5d0\uc11c\ub294 \ud45c\uc2dc\ub41c \ucf54\ub108\uc640 \ub9de\ub294 \uc5e3\uc9c0\uac00 \uc2ac\ub86f \uc704\uc5d0 \ud568\uaed8 \uc900\ube44\ub418\uc5b4 \uc788\uc2b5\ub2c8\ub2e4. \ub124 \ub3d9\uc791\uc73c\ub85c \ud398\uc5b4\ub97c \uccab \ub450 \uce35\uc5d0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Prepared F2L Pair Insertion", "\uc900\ube44\ub41c F2L \ud398\uc5b4 \uc0bd\uc785" },
            { "Watch the prepared corner-edge pair enter the highlighted front-side F2L slot.", "\uc900\ube44\ub41c \ucf54\ub108-\uc5e3\uc9c0 \ud398\uc5b4\uac00 \ud45c\uc2dc\ub41c \uc55e\ucabd F2L \uc2ac\ub86f\uc5d0 \ub4e4\uc5b4\uac00\ub294 \ubaa8\uc2b5\uc744 \ud655\uc778\ud569\ub2c8\ub2e4." },
            { "R': Move the prepared pair away from the slot.", "R': \uc900\ube44\ub41c \ud398\uc5b4\ub97c \uc2ac\ub86f\uc5d0\uc11c \uc7a0\uc2dc \ube7c\ub0c5\ub2c8\ub2e4." },
            { "F: Open the front-side slot.", "F: \uc55e\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "R: Bring the pair into alignment.", "R: \ud398\uc5b4\ub97c \uc815\ub82c \uc704\uce58\ub85c \uac00\uc838\uc635\ub2c8\ub2e4." },
            { "F': Close the slot with the pair inserted.", "F': \ud398\uc5b4\uac00 \ub4e4\uc5b4\uac04 \uc0c1\ud0dc\ub85c \uc2ac\ub86f\uc744 \ub2eb\uc2b5\ub2c8\ub2e4." },
            { "Prepared corner-edge pair", "\uc900\ube44\ub41c \ucf54\ub108-\uc5e3\uc9c0 \ud398\uc5b4" },
            { "Front-side F2L slot", "\uc55e\ucabd F2L \uc2ac\ub86f" },
            { "Treat the corner and edge as one pair.", "\ucf54\ub108\uc640 \uc5e3\uc9c0\ub97c \ud558\ub098\uc758 \ud398\uc5b4\ub85c \ubd05\ub2c8\ub2e4." },
            { "The completed white cross remains on the bottom.", "\uc644\uc131\ub41c \ud770\uc0c9 \uc2ed\uc790\ub294 \uc544\ub798\uc5d0 \ub0a8\uc544 \uc788\uc2b5\ub2c8\ub2e4." },
            { "The result restores the first two layers.", "\uacb0\uacfc\uc801\uc73c\ub85c \uccab \ub450 \uce35\uc774 \ubcf5\uad6c\ub429\ub2c8\ub2e4." },
            { "Yellow Cross Formula", "\ub178\ub780 \uc2ed\uc790 \uacf5\uc2dd" },
            { "Turn a yellow edge line into a yellow cross.", "\ub178\ub780 \uc5e3\uc9c0 \ub77c\uc778\uc744 \ub178\ub780 \uc2ed\uc790\ub85c \ub9cc\ub4ed\ub2c8\ub2e4." },
            { "Use F R U R' U' F' to orient the last-layer edges.\n\nIgnore the yellow corners. In this example the top face begins with a yellow line. Hold the line in the demonstrated orientation and perform the formula once.\n\nThe result is a yellow cross while the first two layers remain solved.", "F R U R' U' F'\ub85c \ub9c8\uc9c0\ub9c9 \uce35 \uc5e3\uc9c0\uc758 \ubc29\ud5a5\uc744 \ub9de\ucda5\ub2c8\ub2e4.\n\n\ub178\ub780 \ucf54\ub108\ub294 \uc77c\ub2e8 \ubb34\uc2dc\ud569\ub2c8\ub2e4. \uc774 \uc608\uc2dc\ub294 \uc717\uba74\uc774 \ub178\ub780 \ub77c\uc778\uc778 \uc0c1\ud0dc\uc5d0\uc11c \uc2dc\uc791\ud569\ub2c8\ub2e4. \ub370\ubaa8\ucc98\ub7fc \ub77c\uc778\uc744 \uc7a1\uace0 \uacf5\uc2dd\uc744 \ud55c \ubc88 \uc218\ud589\ud569\ub2c8\ub2e4.\n\n\uccab \ub450 \uce35\uc740 \uc720\uc9c0\ub418\uba74\uc11c \ub178\ub780 \uc2ed\uc790\uac00 \ub9cc\ub4e4\uc5b4\uc9d1\ub2c8\ub2e4." },
            { "Yellow Line to Cross", "\ub178\ub780 \ub77c\uc778\uc5d0\uc11c \uc2ed\uc790\ub85c" },
            { "Orient the four yellow top edges to form a cross.", "\uc717\uba74\uc758 \ub178\ub780 \uc5e3\uc9c0 4\uac1c \ubc29\ud5a5\uc744 \ub9de\ucdb0 \uc2ed\uc790\ub97c \ub9cc\ub4ed\ub2c8\ub2e4." },
            { "F: Open the front face for the edge-orientation sequence.", "F: \uc5e3\uc9c0 \ubc29\ud5a5 \ub9de\ucda4\uc744 \uc704\ud574 \uc55e\uba74\uc744 \uc5fd\ub2c8\ub2e4." },
            { "R: Raise the right layer.", "R: \uc624\ub978\ucabd \uce35\uc744 \uc62c\ub9bd\ub2c8\ub2e4." },
            { "U: Move the top edges through the open area.", "U: \uc717\uce35 \uc5e3\uc9c0\ub97c \uc5f4\ub9b0 \uacf5\uac04\uc73c\ub85c \uc774\ub3d9\uc2dc\ud0b5\ub2c8\ub2e4." },
            { "R': Restore the right layer.", "R': \uc624\ub978\ucabd \uce35\uc744 \ubcf5\uad6c\ud569\ub2c8\ub2e4." },
            { "U': Restore the top alignment.", "U': \uc717\uce35 \uc815\ub82c\uc744 \ubcf5\uad6c\ud569\ub2c8\ub2e4." },
            { "F': Close the front face and reveal the yellow cross.", "F': \uc55e\uba74\uc744 \ub2eb\uc544 \ub178\ub780 \uc2ed\uc790\ub97c \uc644\uc131\ud569\ub2c8\ub2e4." },
            { "Yellow top edges", "\uc717\uba74 \ub178\ub780 \uc5e3\uc9c0" },
            { "Top yellow cross", "\uc717\uba74 \ub178\ub780 \uc2ed\uc790" },
            { "Only the four yellow edge stickers matter.", "\uc5ec\uae30\uc11c\ub294 \ub178\ub780 \uc5e3\uc9c0 \uc2a4\ud2f0\ucee4 4\uac1c\ub9cc \ubd05\ub2c8\ub2e4." },
            { "Keep the first two layers solved.", "\uccab \ub450 \uce35\uc740 \uc644\uc131 \uc0c1\ud0dc\ub85c \uc720\uc9c0\ud569\ub2c8\ub2e4." },
            { "Use the correct line or L orientation before applying the formula.", "\uacf5\uc2dd\uc744 \uc4f0\uae30 \uc804\uc5d0 \ub77c\uc778\uc774\ub098 L \ubaa8\uc591 \ubc29\ud5a5\uc744 \ub9de\ucda5\ub2c8\ub2e4." },
            { "Right Algorithm", "\uc624\ub978\uc190 \uacf5\uc2dd" },
            { "Orient the final yellow corners with the Sune algorithm.", "Sune \uacf5\uc2dd\uc73c\ub85c \ub9c8\uc9c0\ub9c9 \ub178\ub780 \ucf54\ub108\uc758 \ubc29\ud5a5\uc744 \ub9de\ucda5\ub2c8\ub2e4." },
            { "The Right Algorithm, often called Sune, is R U R' U R U2 R'.\n\nUse it after the yellow cross is complete. Hold the yellow-corner pattern in the demonstrated orientation, then perform all seven moves without rotating the cube.\n\nThis example finishes the full yellow face. In another case, reposition the top layer and repeat as instructed.", "\uc624\ub978\uc190 \uacf5\uc2dd\uc740 \ud754\ud788 Sune\uc774\ub77c\uace0 \ubd80\ub974\uba70 R U R' U R U2 R'\uc785\ub2c8\ub2e4.\n\n\ub178\ub780 \uc2ed\uc790\uac00 \uc644\uc131\ub41c \ub4a4 \uc0ac\uc6a9\ud569\ub2c8\ub2e4. \ub178\ub780 \ucf54\ub108 \ud328\ud134\uc744 \ub370\ubaa8 \ubc29\ud5a5\uc73c\ub85c \uc7a1\uace0 \ud050\ube0c \uc804\uccb4\ub97c \ub3cc\ub9ac\uc9c0 \uc54a\uc740 \ucc44 7\ub3d9\uc791\uc744 \uc218\ud589\ud569\ub2c8\ub2e4.\n\n\uc774 \uc608\uc2dc\ub294 \uc717\uba74 \uc804\uccb4\ub97c \ub178\ub780\uc0c9\uc73c\ub85c \uc644\uc131\ud569\ub2c8\ub2e4. \ub2e4\ub978 \uacbd\uc6b0\uc5d0\ub294 \uc717\uce35 \uc704\uce58\ub97c \ub2e4\uc2dc \ub9de\ucd94\uace0 \uc548\ub0b4\ub300\ub85c \ubc18\ubcf5\ud569\ub2c8\ub2e4." },
            { "Sune to Yellow Face", "Sune\uc73c\ub85c \ub178\ub780 \uba74 \uc644\uc131" },
            { "Orient the remaining yellow corners so all nine top stickers become yellow.", "\ub0a8\uc740 \ub178\ub780 \ucf54\ub108 \ubc29\ud5a5\uc744 \ub9de\ucdb0 \uc717\uba74 9\uce78\uc744 \ubaa8\ub450 \ub178\ub780\uc0c9\uc73c\ub85c \ub9cc\ub4ed\ub2c8\ub2e4." },
            { "R: Begin the right-hand corner cycle.", "R: \uc624\ub978\uc190 \ucf54\ub108 \uc21c\ud658\uc744 \uc2dc\uc791\ud569\ub2c8\ub2e4." },
            { "U: Move the next yellow corner into the working area.", "U: \ub2e4\uc74c \ub178\ub780 \ucf54\ub108\ub97c \uc791\uc5c5 \uc704\uce58\ub85c \uc62e\uae41\ub2c8\ub2e4." },
            { "R': Restore the right side.", "R': \uc624\ub978\ucabd \uba74\uc744 \ubcf5\uad6c\ud569\ub2c8\ub2e4." },
            { "U: Continue the top-corner cycle.", "U: \uc717\uce35 \ucf54\ub108 \uc21c\ud658\uc744 \uc774\uc5b4\uac11\ub2c8\ub2e4." },
            { "R: Reopen the right side.", "R: \uc624\ub978\ucabd\uc744 \ub2e4\uc2dc \uc5fd\ub2c8\ub2e4." },
            { "U2: Move the final corners through the working area.", "U2: \ub9c8\uc9c0\ub9c9 \ucf54\ub108\ub4e4\uc744 \uc791\uc5c5 \uc704\uce58\ub85c \ud1b5\uacfc\uc2dc\ud0b5\ub2c8\ub2e4." },
            { "R': Restore the cube and complete the yellow face.", "R': \ud050\ube0c\ub97c \ubcf5\uad6c\ud558\uace0 \ub178\ub780 \uba74\uc744 \uc644\uc131\ud569\ub2c8\ub2e4." },
            { "Unoriented yellow corners", "\ubc29\ud5a5\uc774 \ub9de\uc9c0 \uc54a\uc740 \ub178\ub780 \ucf54\ub108" },
            { "Complete yellow top face", "\uc644\uc131\ub41c \ub178\ub780 \uc717\uba74" },
            { "The yellow cross must already be complete.", "\ub178\ub780 \uc2ed\uc790\uac00 \uba3c\uc800 \uc644\uc131\ub418\uc5b4 \uc788\uc5b4\uc57c \ud569\ub2c8\ub2e4." },
            { "Keep the cube orientation fixed for all seven moves.", "7\ub3d9\uc791 \ub3d9\uc548 \ud050\ube0c \ubc29\ud5a5\uc744 \uace0\uc815\ud569\ub2c8\ub2e4." },
            { "This algorithm orients corners; their positions are handled separately.", "\uc774 \uacf5\uc2dd\uc740 \ucf54\ub108 \ubc29\ud5a5\uc744 \ub9de\ucda5\ub2c8\ub2e4. \ucf54\ub108 \uc704\uce58\ub294 \ubcc4\ub3c4\ub85c \ucc98\ub9ac\ud569\ub2c8\ub2e4." },
            { "Step 1: White Cross", "1\ub2e8\uacc4: \ud770\uc0c9 \uc2ed\uc790" },
            { "Place all four white edges into the bottom cross.", "\ud770\uc0c9 \uc5e3\uc9c0 4\uac1c\ub97c \uc544\ub798 \uc2ed\uc790\uc5d0 \ubc30\uce58\ud569\ub2c8\ub2e4." },
            { "The goal is to build a white cross on the bottom.\n\n1. Find a white edge piece.\n2. If needed, move it to the top layer without losing pieces already solved.\n3. Turn the top layer until the edge's side color matches its center.\n4. Keep that matching center facing you.\n5. Turn the front face 180 degrees to send the white edge to the bottom.\n6. Repeat until four white edges form a cross.\n\nThe four demos are representative cases, not one fixed formula. The preparation moves change depending on where and how the edge starts.", "\ubaa9\ud45c\ub294 \uc544\ub798\uba74\uc5d0 \ud770\uc0c9 \uc2ed\uc790\ub97c \ub9cc\ub4dc\ub294 \uac83\uc785\ub2c8\ub2e4.\n\n1. \ud770\uc0c9 \uc5e3\uc9c0 \uc870\uac01\uc744 \ucc3e\uc2b5\ub2c8\ub2e4.\n2. \ud544\uc694\ud558\uba74 \uc774\ubbf8 \ub9de\ucd98 \uc870\uac01\uc744 \ud750\ud2b8\ub7ec\ub728\ub9ac\uc9c0 \uc54a\uace0 \uc717\uce35\uc73c\ub85c \ubcf4\ub0c5\ub2c8\ub2e4.\n3. \uc5e3\uc9c0\uc758 \uc606 \uc0c9\uc774 \uac19\uc740 \uc0c9 \uc13c\ud130\uc640 \ub9de\uc744 \ub54c\uae4c\uc9c0 \uc717\uce35\uc744 \ub3cc\ub9bd\ub2c8\ub2e4.\n4. \ub9de\uc740 \uc13c\ud130\uac00 \uc815\uba74\uc744 \ubcf4\uac8c \uc7a1\uc2b5\ub2c8\ub2e4.\n5. \uc55e\uba74\uc744 180\ub3c4 \ub3cc\ub824 \ud770\uc0c9 \uc5e3\uc9c0\ub97c \uc544\ub798\ub85c \ubcf4\ub0c5\ub2c8\ub2e4.\n6. \ud770\uc0c9 \uc5e3\uc9c0 4\uac1c\uac00 \uc2ed\uc790\ub97c \ub9cc\ub4e4 \ub54c\uae4c\uc9c0 \ubc18\ubcf5\ud569\ub2c8\ub2e4.\n\n\ub124 \ub370\ubaa8\ub294 \ub300\ud45c \uc0ac\ub840\uc774\uba70 \ud558\ub098\uc758 \uace0\uc815 \uacf5\uc2dd\uc774 \uc544\ub2d9\ub2c8\ub2e4. \uc900\ube44 \ub3d9\uc791\uc740 \uc5e3\uc9c0\uac00 \uc5b4\ub514\uc11c \uc5b4\ub5a4 \ubc29\ud5a5\uc73c\ub85c \uc2dc\uc791\ud558\ub290\ub0d0\uc5d0 \ub530\ub77c \ub2ec\ub77c\uc9d1\ub2c8\ub2e4." },
            { "Build the complete bottom white cross one edge case at a time.", "\uc5e3\uc9c0 \uc0ac\ub840\ub97c \ud558\ub098\uc529 \ucc98\ub9ac\ud574 \uc544\ub798 \ud770\uc0c9 \uc2ed\uc790\ub97c \uc644\uc131\ud569\ub2c8\ub2e4." },
            { "Complete all four white edge placements on the bottom.", "\ud770\uc0c9 \uc5e3\uc9c0 4\uac1c\ub97c \ubaa8\ub450 \uc544\ub798\uc5d0 \ubc30\uce58\ud569\ub2c8\ub2e4." },
            { "Case 1 / 4: White-Green Already Aligned", "\uc0ac\ub840 1 / 4: \ud770\uc0c9-\ucd08\ub85d \uc774\ubbf8 \uc815\ub82c\ub428" },
            { "The green side already matches the green center. Turn the front face 180 degrees to insert the edge.", "\ucd08\ub85d \uba74\uc774 \uc774\ubbf8 \ucd08\ub85d \uc13c\ud130\uc640 \ub9de\uc2b5\ub2c8\ub2e4. \uc55e\uba74\uc744 180\ub3c4 \ub3cc\ub824 \uc5e3\uc9c0\ub97c \ub123\uc2b5\ub2c8\ub2e4." },
            { "Case 2 / 4: White-Red Needs Alignment", "\uc0ac\ub840 2 / 4: \ud770\uc0c9-\ube68\uac15 \uc815\ub82c \ud544\uc694" },
            { "Turn the top layer until the red side of the white-red edge matches the red center.", "\ud770\uc0c9-\ube68\uac15 \uc5e3\uc9c0\uc758 \ube68\uac15 \uba74\uc774 \ube68\uac15 \uc13c\ud130\uc640 \ub9de\uc744 \ub54c\uae4c\uc9c0 \uc717\uce35\uc744 \ub3cc\ub9bd\ub2c8\ub2e4." },
            { "The edge is aligned. Turn the front face 180 degrees to insert it into the bottom cross.", "\uc5e3\uc9c0\uac00 \uc815\ub82c\ub418\uc5c8\uc2b5\ub2c8\ub2e4. \uc55e\uba74\uc744 180\ub3c4 \ub3cc\ub824 \uc544\ub798 \uc2ed\uc790\uc5d0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Case 3 / 4: White-Blue in the Middle Layer", "\uc0ac\ub840 3 / 4: \uc911\uac04\uce35\uc758 \ud770\uc0c9-\ud30c\ub791" },
            { "Move the white-blue edge out of the middle layer and onto the top-front position.", "\ud770\uc0c9-\ud30c\ub791 \uc5e3\uc9c0\ub97c \uc911\uac04\uce35\uc5d0\uc11c \ube7c\ub0b4 \uc717\uce35 \uc55e \uc704\uce58\ub85c \ubcf4\ub0c5\ub2c8\ub2e4." },
            { "The blue side now matches the blue center. Turn the front face 180 degrees to insert it.", "\uc774\uc81c \ud30c\ub791 \uba74\uc774 \ud30c\ub791 \uc13c\ud130\uc640 \ub9de\uc2b5\ub2c8\ub2e4. \uc55e\uba74\uc744 180\ub3c4 \ub3cc\ub824 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Case 4 / 4: White-Orange Flipped on Top", "\uc0ac\ub840 4 / 4: \uc717\uce35\uc5d0\uc11c \ub4a4\uc9d1\ud78c \ud770\uc0c9-\uc8fc\ud669" },
            { "Move the flipped white-orange edge away from the front slot.", "\ub4a4\uc9d1\ud78c \ud770\uc0c9-\uc8fc\ud669 \uc5e3\uc9c0\ub97c \uc55e \uc2ac\ub86f\uc5d0\uc11c \ube7c\ub0c5\ub2c8\ub2e4." },
            { "Use the side face to reorient the edge.", "\uc606\uba74\uc744 \uc0ac\uc6a9\ud574 \uc5e3\uc9c0 \ubc29\ud5a5\uc744 \ub2e4\uc2dc \ub9de\ucda5\ub2c8\ub2e4." },
            { "Turn the top layer until the orange side matches the orange center.", "\uc8fc\ud669 \uba74\uc774 \uc8fc\ud669 \uc13c\ud130\uc640 \ub9de\uc744 \ub54c\uae4c\uc9c0 \uc717\uce35\uc744 \ub3cc\ub9bd\ub2c8\ub2e4." },
            { "The edge is aligned. Turn the front face 180 degrees to complete the placement.", "\uc5e3\uc9c0\uac00 \uc815\ub82c\ub418\uc5c8\uc2b5\ub2c8\ub2e4. \uc55e\uba74\uc744 180\ub3c4 \ub3cc\ub824 \ubc30\uce58\ub97c \uc644\ub8cc\ud569\ub2c8\ub2e4." },
            { "White stickers alone are not enough; every side color must match its center.", "\ud770\uc0c9 \uc2a4\ud2f0\ucee4\ub9cc \ub9de\uc73c\uba74 \ucda9\ubd84\ud558\uc9c0 \uc54a\uc2b5\ub2c8\ub2e4. \uc606 \uc0c9\ub3c4 \uc13c\ud130\uc640 \ub9de\uc544\uc57c \ud569\ub2c8\ub2e4." },
            { "Use top-layer turns to align an edge before inserting it.", "\uc5e3\uc9c0\ub97c \ub123\uae30 \uc804\uc5d0 \uc717\uce35 \ud68c\uc804\uc73c\ub85c \uc815\ub82c\ud569\ub2c8\ub2e4." },
            { "The final insertion is F2 only after the matching center faces you.", "\ub9de\ub294 \uc13c\ud130\uac00 \uc815\uba74\uc744 \ubcfc \ub54c\ub9cc \ub9c8\uc9c0\ub9c9\uc5d0 F2\ub85c \ub123\uc2b5\ub2c8\ub2e4." },
            { "Previously solved cross edges should remain in place.", "\uc774\ubbf8 \ub9de\ucd98 \uc2ed\uc790 \uc5e3\uc9c0\ub294 \uc81c\uc790\ub9ac\uc5d0 \uc720\uc9c0\ud574\uc57c \ud569\ub2c8\ub2e4." },
            { "Step 2: White Corners", "2\ub2e8\uacc4: \ud770\uc0c9 \ucf54\ub108" },
            { "Complete the white first layer without breaking the cross.", "\uc2ed\uc790\ub97c \uae68\uc9c0 \uc54a\uace0 \ud770\uc0c9 1\uce35\uc744 \uc644\uc131\ud569\ub2c8\ub2e4." },
            { "Keep the completed white cross on the bottom.\n\n1. Find a corner containing white.\n2. Read its other two colors to identify its correct slot.\n3. Turn the top layer until the corner is above that slot.\n4. If white faces a side, insert from that side with a three-move trigger.\n5. If white faces up, first turn it to the side, realign it, then insert.\n6. If a white corner is twisted or in the wrong bottom slot, move it to the top and insert it again.\n\nRepeat for all four corners. The result is a complete white face and a matching first row on every side.", "\uc644\uc131\ub41c \ud770\uc0c9 \uc2ed\uc790\ub97c \uc544\ub798\uc5d0 \ub461\ub2c8\ub2e4.\n\n1. \ud770\uc0c9\uc774 \ud3ec\ud568\ub41c \ucf54\ub108\ub97c \ucc3e\uc2b5\ub2c8\ub2e4.\n2. \ub098\uba38\uc9c0 \ub450 \uc0c9\uc744 \ubcf4\uace0 \ub9de\ub294 \uc2ac\ub86f\uc744 \ud655\uc778\ud569\ub2c8\ub2e4.\n3. \ucf54\ub108\uac00 \uadf8 \uc2ac\ub86f \uc704\uc5d0 \uc62c \ub54c\uae4c\uc9c0 \uc717\uce35\uc744 \ub3cc\ub9bd\ub2c8\ub2e4.\n4. \ud770\uc0c9\uc774 \uc606\uc744 \ubcf4\uba74 \uadf8\ucabd\uc5d0\uc11c 3\ub3d9\uc791 \ud2b8\ub9ac\uac70\ub85c \ub123\uc2b5\ub2c8\ub2e4.\n5. \ud770\uc0c9\uc774 \uc704\ub97c \ubcf4\uba74 \uba3c\uc800 \uc606\uc73c\ub85c \ub3cc\ub9ac\uace0 \ub2e4\uc2dc \uc815\ub82c\ud55c \ub4a4 \ub123\uc2b5\ub2c8\ub2e4.\n6. \ud770\uc0c9 \ucf54\ub108\uac00 \uc544\ub798\uc5d0\uc11c \uaf2c\uc600\uac70\ub098 \ud2c0\ub9b0 \uc2ac\ub86f\uc5d0 \uc788\uc73c\uba74 \uc717\uce35\uc73c\ub85c \ube7c\ub0b8 \ub4a4 \ub2e4\uc2dc \ub123\uc2b5\ub2c8\ub2e4.\n\n\ucf54\ub108 4\uac1c\uc5d0 \ubc18\ubcf5\ud569\ub2c8\ub2e4. \uacb0\uacfc\ub294 \uc644\uc131\ub41c \ud770\uc0c9 \uba74\uacfc \uac01 \uc606\uba74\uc758 \ub9de\ub294 \uccab \uc904\uc785\ub2c8\ub2e4." },
            { "Learn the four common white-corner insertion situations.", "\uc790\uc8fc \ub098\uc624\ub294 \ud770\uc0c9 \ucf54\ub108 \uc0bd\uc785 \uc0c1\ud669 4\uac00\uc9c0\ub97c \uc775\ud799\ub2c8\ub2e4." },
            { "Place one white corner correctly while keeping the bottom cross solved.", "\uc544\ub798 \uc2ed\uc790\ub97c \uc720\uc9c0\ud558\uba74\uc11c \ud770\uc0c9 \ucf54\ub108 \ud558\ub098\ub97c \uc815\ud655\ud788 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Case 1 / 4: White Faces Right", "\uc0ac\ub840 1 / 4: \ud770\uc0c9\uc774 \uc624\ub978\ucabd\uc744 \ubd04" },
            { "The corner is above the front-right slot and white faces the right side. Insert it with the right-side trigger.", "\ucf54\ub108\uac00 \uc55e-\uc624\ub978\ucabd \uc2ac\ub86f \uc704\uc5d0 \uc788\uace0 \ud770\uc0c9\uc774 \uc624\ub978\ucabd\uc744 \ubd05\ub2c8\ub2e4. \uc624\ub978\ucabd \ud2b8\ub9ac\uac70\ub85c \ub123\uc2b5\ub2c8\ub2e4." },
            { "Open the right-side slot without disturbing the white cross.", "\ud770\uc0c9 \uc2ed\uc790\ub97c \ud750\ud2b8\ub7ec\ub728\ub9ac\uc9c0 \uc54a\uace0 \uc624\ub978\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "Move the corner over the open slot.", "\ucf54\ub108\ub97c \uc5f4\ub9b0 \uc2ac\ub86f \uc704\ub85c \uc62e\uae41\ub2c8\ub2e4." },
            { "Close the slot to insert the corner into the first layer.", "\uc2ac\ub86f\uc744 \ub2eb\uc544 \ucf54\ub108\ub97c 1\uce35\uc5d0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Case 2 / 4: White Faces Left", "\uc0ac\ub840 2 / 4: \ud770\uc0c9\uc774 \uc67c\ucabd\uc744 \ubd04" },
            { "The corner is above the front-left slot and white faces the left side. Use the mirrored trigger.", "\ucf54\ub108\uac00 \uc55e-\uc67c\ucabd \uc2ac\ub86f \uc704\uc5d0 \uc788\uace0 \ud770\uc0c9\uc774 \uc67c\ucabd\uc744 \ubd05\ub2c8\ub2e4. \ubc18\ub300 \ud2b8\ub9ac\uac70\ub97c \uc0ac\uc6a9\ud569\ub2c8\ub2e4." },
            { "Open the left-side slot while preserving the cross.", "\uc2ed\uc790\ub97c \uc720\uc9c0\ud558\uba74\uc11c \uc67c\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "Close the slot to complete the mirrored insertion.", "\uc2ac\ub86f\uc744 \ub2eb\uc544 \ubc18\ub300 \ubc29\ud5a5 \uc0bd\uc785\uc744 \uc644\ub8cc\ud569\ub2c8\ub2e4." },
            { "Case 3 / 4: White Faces Up", "\uc0ac\ub840 3 / 4: \ud770\uc0c9\uc774 \uc704\ub97c \ubd04" },
            { "White points upward, so a direct three-move insert will not solve the corner. Turn it to the side, realign it, then insert.", "\ud770\uc0c9\uc774 \uc704\ub97c \ubcf4\uace0 \uc788\uc5b4 \ubc14\ub85c 3\ub3d9\uc791\uc73c\ub85c \ub123\uc73c\uba74 \ud574\uacb0\ub418\uc9c0 \uc54a\uc2b5\ub2c8\ub2e4. \uc606\uc73c\ub85c \ub3cc\ub9ac\uace0 \ub2e4\uc2dc \uc815\ub82c\ud55c \ub4a4 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Move the top-facing white corner away from the slot.", "\uc704\ub97c \ubcf4\ub294 \ud770\uc0c9 \ucf54\ub108\ub97c \uc2ac\ub86f\uc5d0\uc11c \ube7c\ub0c5\ub2c8\ub2e4." },
            { "Rotate the top layer to create space for reorientation.", "\ubc29\ud5a5\uc744 \ubc14\uafc0 \uacf5\uac04\uc744 \ub9cc\ub4e4\uae30 \uc704\ud574 \uc717\uce35\uc744 \ub3cc\ub9bd\ub2c8\ub2e4." },
            { "Return the side face so white now points sideways.", "\uc606\uba74\uc744 \ub418\ub3cc\ub824 \ud770\uc0c9\uc774 \uc606\uc744 \ubcf4\uac8c \ub9cc\ub4ed\ub2c8\ub2e4." },
            { "Realign the corner above its correct slot.", "\ucf54\ub108\ub97c \ub9de\ub294 \uc2ac\ub86f \uc704\uc5d0 \ub2e4\uc2dc \uc815\ub82c\ud569\ub2c8\ub2e4." },
            { "Open the right-side slot.", "\uc624\ub978\ucabd \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "Move the corner over the slot.", "\ucf54\ub108\ub97c \uc2ac\ub86f \uc704\ub85c \uc62e\uae41\ub2c8\ub2e4." },
            { "Close the slot to finish the insertion.", "\uc2ac\ub86f\uc744 \ub2eb\uc544 \uc0bd\uc785\uc744 \ub9c8\uce69\ub2c8\ub2e4." },
            { "Case 4 / 4: Corner Twisted in Bottom", "\uc0ac\ub840 4 / 4: \uc544\ub798\uce35\uc5d0\uc11c \uaf2c\uc778 \ucf54\ub108" },
            { "The corner is in the bottom layer but is twisted. Take it out to the top, realign it, and insert it correctly.", "\ucf54\ub108\uac00 \uc544\ub798\uce35\uc5d0 \uc788\uc9c0\ub9cc \ubc29\ud5a5\uc774 \uaf2c\uc600\uc2b5\ub2c8\ub2e4. \uc717\uce35\uc73c\ub85c \ube7c\ub0b4 \ub2e4\uc2dc \uc815\ub82c\ud558\uace0 \uc815\ud655\ud788 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Open the slot to remove the twisted corner from the bottom layer.", "\uc544\ub798\uce35\uc758 \uaf2c\uc778 \ucf54\ub108\ub97c \ube7c\ub0b4\uae30 \uc704\ud574 \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "Move the corner into the top layer.", "\ucf54\ub108\ub97c \uc717\uce35\uc73c\ub85c \uc62e\uae41\ub2c8\ub2e4." },
            { "Close the slot; the white cross remains solved.", "\uc2ac\ub86f\uc744 \ub2eb\uc2b5\ub2c8\ub2e4. \ud770\uc0c9 \uc2ed\uc790\ub294 \uc720\uc9c0\ub429\ub2c8\ub2e4." },
            { "Turn the top layer to realign the corner.", "\uc717\uce35\uc744 \ub3cc\ub824 \ucf54\ub108\ub97c \ub2e4\uc2dc \uc815\ub82c\ud569\ub2c8\ub2e4." },
            { "Open the slot for the correct insertion.", "\uc815\ud655\ud55c \uc0bd\uc785\uc744 \uc704\ud574 \uc2ac\ub86f\uc744 \uc5fd\ub2c8\ub2e4." },
            { "Close the slot with the corner correctly oriented.", "\ucf54\ub108 \ubc29\ud5a5\uc774 \ub9de\uc740 \uc0c1\ud0dc\ub85c \uc2ac\ub86f\uc744 \ub2eb\uc2b5\ub2c8\ub2e4." },
            { "A corner belongs where all three of its colors match the surrounding centers.", "\ucf54\ub108\ub294 \uc138 \uc0c9\uc774 \uc8fc\ubcc0 \uc13c\ud130\uc640 \ubaa8\ub450 \ub9de\ub294 \uc704\uce58\uc5d0 \ub4e4\uc5b4\uac11\ub2c8\ub2e4." },
            { "Keep the white cross on the bottom throughout this step.", "\uc774 \ub2e8\uacc4 \ub0b4\ub0b4 \ud770\uc0c9 \uc2ed\uc790\ub97c \uc544\ub798\uc5d0 \uc720\uc9c0\ud569\ub2c8\ub2e4." },
            { "Right-facing and left-facing white stickers use mirrored three-move inserts.", "\uc624\ub978\ucabd/\uc67c\ucabd\uc744 \ubcf4\ub294 \ud770\uc0c9 \uc2a4\ud2f0\ucee4\ub294 \uc11c\ub85c \ubc18\ub300 \ud615\ud0dc\uc758 3\ub3d9\uc791 \uc0bd\uc785\uc744 \uc501\ub2c8\ub2e4." },
            { "Top-facing or incorrectly placed corners must be reoriented before insertion.", "\uc704\ub97c \ubcf4\uac70\ub098 \uc798\ubabb \ub4e4\uc5b4\uac04 \ucf54\ub108\ub294 \ub123\uae30 \uc804\uc5d0 \ubc29\ud5a5\uc744 \ub2e4\uc2dc \uc7a1\uc544\uc57c \ud569\ub2c8\ub2e4." },
            { "After all four corners, the entire first layer must match, not only the white face.", "\ucf54\ub108 4\uac1c \ub4a4\uc5d0\ub294 \ud770\uc0c9 \uba74\ubfd0 \uc544\ub2c8\ub77c 1\uce35 \uc804\uccb4 \uc0c9\uc774 \ub9de\uc544\uc57c \ud569\ub2c8\ub2e4." },
            { "Match all three corner colors, then insert the corner without breaking the white cross.", "\ucf54\ub108\uc758 \uc138 \uc0c9\uc744 \ub9de\ucd98 \ub4a4 \ud770\uc0c9 \uc2ed\uc790\ub97c \uae68\uc9c0 \uc54a\uace0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Bottom-front-right corner slot", "\uc544\ub798-\uc55e-\uc624\ub978\ucabd \ucf54\ub108 \uc2ac\ub86f" },
            { "Bottom-front-left corner slot", "\uc544\ub798-\uc55e-\uc67c\ucabd \ucf54\ub108 \uc2ac\ub86f" },
            { "Step 3: Second Layer", "3\ub2e8\uacc4: \ub450 \ubc88\uc9f8 \uce35" },
            { "Insert the four non-yellow edges into the middle layer.", "\ub178\ub780\uc0c9\uc774 \uc5c6\ub294 \uc5e3\uc9c0 4\uac1c\ub97c \uc911\uac04\uce35\uc5d0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Keep the completed white layer on the bottom.\n\n1. Find a top-layer edge with no yellow sticker.\n2. Match its front color with the same-color center.\n3. Look at the edge's top color to decide whether it belongs left or right.\n4. Use the matching eight-move insertion.\n5. If no usable edge is on top, eject an incorrect middle edge first.\n\nRepeat until the first two layers are solved around all four sides.", "\uc644\uc131\ub41c \ud770\uc0c9 \uce35\uc744 \uc544\ub798\uc5d0 \ub461\ub2c8\ub2e4.\n\n1. \ub178\ub780 \uc2a4\ud2f0\ucee4\uac00 \uc5c6\ub294 \uc717\uce35 \uc5e3\uc9c0\ub97c \ucc3e\uc2b5\ub2c8\ub2e4.\n2. \uc55e\ucabd \uc0c9\uc744 \uac19\uc740 \uc0c9 \uc13c\ud130\uc640 \ub9de\ucda5\ub2c8\ub2e4.\n3. \uc5e3\uc9c0\uc758 \uc717\uc0c9\uc744 \ubcf4\uace0 \uc67c\ucabd\uc73c\ub85c \uac08\uc9c0 \uc624\ub978\ucabd\uc73c\ub85c \uac08\uc9c0 \uacb0\uc815\ud569\ub2c8\ub2e4.\n4. \ub9de\ub294 8\ub3d9\uc791 \uc0bd\uc785\uc744 \uc0ac\uc6a9\ud569\ub2c8\ub2e4.\n5. \uc717\uce35\uc5d0 \uc4f8 \uc218 \uc788\ub294 \uc5e3\uc9c0\uac00 \uc5c6\uc73c\uba74 \uba3c\uc800 \uc798\ubabb \ub4e4\uc5b4\uac04 \uc911\uac04\uce35 \uc5e3\uc9c0\ub97c \ube7c\ub0c5\ub2c8\ub2e4.\n\n\ub124 \uc606\uba74\uc758 \uccab \ub450 \uce35\uc774 \ubaa8\ub450 \ub9de\uc744 \ub54c\uae4c\uc9c0 \ubc18\ubcf5\ud569\ub2c8\ub2e4." },
            { "Complete the middle ring while preserving the white first layer.", "\ud770\uc0c9 1\uce35\uc744 \uc720\uc9c0\ud558\uba74\uc11c \uc911\uac04 \ub9c1\uc744 \uc644\uc131\ud569\ub2c8\ub2e4." },
            { "Case 1 / 3: Edge Goes Right", "\uc0ac\ub840 1 / 3: \uc5e3\uc9c0\uac00 \uc624\ub978\ucabd\uc73c\ub85c \uac10" },
            { "Match the front sticker to its center. The top sticker matches the right center, so insert the edge to the right.", "\uc55e \uc2a4\ud2f0\ucee4\ub97c \uc13c\ud130\uc640 \ub9de\ucda5\ub2c8\ub2e4. \uc717 \uc2a4\ud2f0\ucee4\uac00 \uc624\ub978\ucabd \uc13c\ud130\uc640 \ub9de\uc73c\ubbc0\ub85c \uc5e3\uc9c0\ub97c \uc624\ub978\ucabd\uc5d0 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Target middle-layer edge", "\ubaa9\ud45c \uc911\uac04\uce35 \uc5e3\uc9c0" },
            { "Front-right middle slot", "\uc55e-\uc624\ub978\ucabd \uc911\uac04 \uc2ac\ub86f" },
            { "Case 2 / 3: Edge Goes Left", "\uc0ac\ub840 2 / 3: \uc5e3\uc9c0\uac00 \uc67c\ucabd\uc73c\ub85c \uac10" },
            { "Match the front sticker to its center. The top sticker matches the left center, so use the mirrored insertion.", "\uc55e \uc2a4\ud2f0\ucee4\ub97c \uc13c\ud130\uc640 \ub9de\ucda5\ub2c8\ub2e4. \uc717 \uc2a4\ud2f0\ucee4\uac00 \uc67c\ucabd \uc13c\ud130\uc640 \ub9de\uc73c\ubbc0\ub85c \ubc18\ub300 \uc0bd\uc785\uc744 \uc0ac\uc6a9\ud569\ub2c8\ub2e4." },
            { "Front-left middle slot", "\uc55e-\uc67c\ucabd \uc911\uac04 \uc2ac\ub86f" },
            { "Case 3 / 3: Wrong Edge in Middle", "\uc0ac\ub840 3 / 3: \uc911\uac04\uce35\uc5d0 \uc798\ubabb \ub4e4\uc5b4\uac04 \uc5e3\uc9c0" },
            { "When every usable edge is trapped in the middle layer, eject the incorrect edge, align it on top, then insert it correctly.", "\uc4f8 \uc218 \uc788\ub294 \uc5e3\uc9c0\uac00 \ubaa8\ub450 \uc911\uac04\uce35\uc5d0 \uac07\ud600 \uc788\uc73c\uba74 \uc798\ubabb\ub41c \uc5e3\uc9c0\ub97c \ube7c\ub0b4 \uc717\uce35\uc5d0\uc11c \uc815\ub82c\ud55c \ub4a4 \uc815\ud655\ud788 \ub123\uc2b5\ub2c8\ub2e4." },
            { "Incorrect middle-layer edge", "\uc798\ubabb \ub4e4\uc5b4\uac04 \uc911\uac04\uce35 \uc5e3\uc9c0" },
            { "Eject and refill the front-right middle slot", "\uc55e-\uc624\ub978\ucabd \uc911\uac04 \uc2ac\ub86f \ube7c\ub0b4\uace0 \ub2e4\uc2dc \ucc44\uc6b0\uae30" },
            { "Use only top-layer edges without yellow.", "\ub178\ub780\uc0c9\uc774 \uc5c6\ub294 \uc717\uce35 \uc5e3\uc9c0\ub9cc \uc0ac\uc6a9\ud569\ub2c8\ub2e4." },
            { "Match the front color before choosing left or right.", "\uc67c\ucabd/\uc624\ub978\ucabd\uc744 \uace0\ub974\uae30 \uc804\uc5d0 \uc55e \uc0c9\uc744 \uba3c\uc800 \ub9de\ucda5\ub2c8\ub2e4." },
            { "A correct insertion restores the white layer automatically.", "\uc815\ud655\ud788 \uc0bd\uc785\ud558\uba74 \ud770\uc0c9 \uce35\uc740 \uc790\ub3d9\uc73c\ub85c \ubcf5\uad6c\ub429\ub2c8\ub2e4." },
            { "The second layer is complete when the bottom two rows of every side match their centers.", "\uac01 \uc606\uba74\uc758 \uc544\ub798 \ub450 \uc904\uc774 \uc13c\ud130\uc640 \ub9de\uc73c\uba74 \ub450 \ubc88\uc9f8 \uce35\uc774 \uc644\uc131\uc785\ub2c8\ub2e4." },
            { "Step 4: Yellow Cross", "4\ub2e8\uacc4: \ub178\ub780 \uc2ed\uc790" },
            { "Orient the four yellow edges on the top face.", "\uc717\uba74\uc758 \ub178\ub780 \uc5e3\uc9c0 4\uac1c \ubc29\ud5a5\uc744 \ub9de\ucda5\ub2c8\ub2e4." },
            { "Keep the solved first two layers underneath.\n\nIgnore the yellow corners for now. Look only at the four top edge stickers.\nUse F R U R' U' F' to progress from dot to L, from L to line, and from line to cross.\nFor the line, hold it vertical. For the L, hold yellow edges at the back and left positions.\n\nThe step is complete when the yellow center and all four yellow edge stickers form a cross.", "\uc644\uc131\ub41c \uccab \ub450 \uce35\uc744 \uc544\ub798\uc5d0 \ub461\ub2c8\ub2e4.\n\n\ub178\ub780 \ucf54\ub108\ub294 \uc9c0\uae08 \ubb34\uc2dc\ud558\uace0 \uc717\uba74 \uc5e3\uc9c0 \uc2a4\ud2f0\ucee4 4\uac1c\ub9cc \ubd05\ub2c8\ub2e4.\nF R U R' U' F'\ub85c \uc810\uc5d0\uc11c L, L\uc5d0\uc11c \ub77c\uc778, \ub77c\uc778\uc5d0\uc11c \uc2ed\uc790\ub85c \uc9c4\ud589\ud569\ub2c8\ub2e4.\n\ub77c\uc778\uc740 \uc138\ub85c\ub85c \uc7a1\uace0, L\uc740 \ub178\ub780 \uc5e3\uc9c0\uac00 \ub4a4\uc640 \uc67c\ucabd\uc5d0 \uc624\uac8c \uc7a1\uc2b5\ub2c8\ub2e4.\n\n\ub178\ub780 \uc13c\ud130\uc640 \ub178\ub780 \uc5e3\uc9c0 \uc2a4\ud2f0\ucee4 4\uac1c\uac00 \uc2ed\uc790\ub97c \ub9cc\ub4e4\uba74 \uc644\ub8cc\uc785\ub2c8\ub2e4." },
            { "Create a yellow cross while keeping the first two layers solved.", "\uccab \ub450 \uce35\uc744 \uc720\uc9c0\ud558\uba74\uc11c \ub178\ub780 \uc2ed\uc790\ub97c \ub9cc\ub4ed\ub2c8\ub2e4." },
            { "Case 1 / 3: Yellow Line", "\uc0ac\ub840 1 / 3: \ub178\ub780 \ub77c\uc778" },
            { "Hold the yellow line horizontally across the top face, then apply the yellow-cross algorithm once.", "\uc717\uba74\uc758 \ub178\ub780 \ub77c\uc778\uc744 \uac00\ub85c\ub85c \uc7a1\uace0 \ub178\ub780 \uc2ed\uc790 \uacf5\uc2dd\uc744 \ud55c \ubc88 \uc801\uc6a9\ud569\ub2c8\ub2e4." },
            { "Case 2 / 3: Yellow L", "\uc0ac\ub840 2 / 3: \ub178\ub780 L" },
            { "Hold the yellow L in the top-face corner shown by the demo, then apply the algorithm.", "\ub370\ubaa8\ucc98\ub7fc \uc717\uba74 \ucf54\ub108\uc5d0 \ub178\ub780 L\uc744 \uc7a1\uace0 \uacf5\uc2dd\uc744 \uc801\uc6a9\ud569\ub2c8\ub2e4." },
            { "Yellow L pattern", "\ub178\ub780 L \ud328\ud134" },
            { "Case 3 / 3: Yellow Dot", "\uc0ac\ub840 3 / 3: \ub178\ub780 \uc810" },
            { "With only the yellow center visible, repeat the algorithm through the intermediate edge patterns until a cross appears.", "\ub178\ub780 \uc13c\ud130\ub9cc \ubcf4\uc774\ub294 \uc0c1\ud0dc\ub77c\uba74 \uc911\uac04 \uc5e3\uc9c0 \ud328\ud134\uc744 \uac70\uccd0 \uc2ed\uc790\uac00 \ub098\uc62c \ub54c\uae4c\uc9c0 \uacf5\uc2dd\uc744 \ubc18\ubcf5\ud569\ub2c8\ub2e4." },
            { "Four unoriented yellow edges", "\ubc29\ud5a5\uc774 \ub9de\uc9c0 \uc54a\uc740 \ub178\ub780 \uc5e3\uc9c0 4\uac1c" },
            { "Only the four yellow edge stickers matter in this step.", "\uc774 \ub2e8\uacc4\uc5d0\uc11c\ub294 \ub178\ub780 \uc5e3\uc9c0 \uc2a4\ud2f0\ucee4 4\uac1c\ub9cc \uc911\uc694\ud569\ub2c8\ub2e4." },
            { "Do not try to solve the yellow corners yet.", "\uc544\uc9c1 \ub178\ub780 \ucf54\ub108\ub97c \ub9de\ucd94\ub824 \ud558\uc9c0 \ub9c8\uc138\uc694." },
            { "Repeat the same algorithm with the correct line or L orientation.", "\ub77c\uc778\uc774\ub098 L \ubc29\ud5a5\uc744 \ub9de\ucd98 \ub4a4 \uac19\uc740 \uacf5\uc2dd\uc744 \ubc18\ubcf5\ud569\ub2c8\ub2e4." },
            { "The first two layers must remain solved.", "\uccab \ub450 \uce35\uc740 \uc644\uc131 \uc0c1\ud0dc\ub85c \uc720\uc9c0\ub418\uc5b4\uc57c \ud569\ub2c8\ub2e4." },
            { "Step 5: Yellow Face", "5\ub2e8\uacc4: \ub178\ub780 \uba74" },
            { "Orient all four top corners so the entire top becomes yellow.", "\uc717\uce35 \ucf54\ub108 4\uac1c \ubc29\ud5a5\uc744 \ub9de\ucdb0 \uc717\uba74 \uc804\uccb4\ub97c \ub178\ub780\uc0c9\uc73c\ub85c \ub9cc\ub4ed\ub2c8\ub2e4." },
            { "Start with the yellow cross complete.\n\nPlace an unsolved yellow corner at the front-right of the top layer.\nApply R U R' U R U2 R'. Recheck the top face, rotate only U as needed, and repeat.\nCorner positions may still be wrong after this step; only their yellow orientation matters.\n\nThe step is complete when all nine stickers on the top face are yellow.", "\ub178\ub780 \uc2ed\uc790\uac00 \uc644\uc131\ub41c \uc0c1\ud0dc\uc5d0\uc11c \uc2dc\uc791\ud569\ub2c8\ub2e4.\n\n\uc544\uc9c1 \ub9de\uc9c0 \uc54a\uc740 \ub178\ub780 \ucf54\ub108\ub97c \uc717\uce35 \uc55e-\uc624\ub978\ucabd\uc5d0 \ub461\ub2c8\ub2e4.\nR U R' U R U2 R'\ub97c \uc801\uc6a9\ud569\ub2c8\ub2e4. \uc717\uba74\uc744 \ub2e4\uc2dc \ud655\uc778\ud558\uace0 \ud544\uc694\ud558\uba74 U\ub9cc \ub3cc\ub824 \ubc18\ubcf5\ud569\ub2c8\ub2e4.\n\uc774 \ub2e8\uacc4 \ub4a4\uc5d0\ub3c4 \ucf54\ub108 \uc704\uce58\ub294 \ud2c0\ub9b4 \uc218 \uc788\uc2b5\ub2c8\ub2e4. \uc5ec\uae30\uc11c\ub294 \ub178\ub780 \ubc29\ud5a5\ub9cc \uc911\uc694\ud569\ub2c8\ub2e4.\n\n\uc717\uba74 9\uac1c \uc2a4\ud2f0\ucee4\uac00 \ubaa8\ub450 \ub178\ub780\uc0c9\uc774\uba74 \uc644\ub8cc\uc785\ub2c8\ub2e4." },
            { "Turn the yellow cross into a complete yellow face.", "\ub178\ub780 \uc2ed\uc790\ub97c \uc644\uc131\ub41c \ub178\ub780 \uba74\uc73c\ub85c \ubc14\uafc9\ub2c8\ub2e4." },
            { "Case 1 / 2: One Sune", "\uc0ac\ub840 1 / 2: Sune \ud55c \ubc88" },
            { "Keep an unsolved yellow corner at top-front-right and perform the seven-move orientation algorithm.", "\ub9de\uc9c0 \uc54a\uc740 \ub178\ub780 \ucf54\ub108\ub97c \uc704-\uc55e-\uc624\ub978\ucabd\uc5d0 \ub450\uace0 7\ub3d9\uc791 \ubc29\ud5a5 \ub9de\ucda4 \uacf5\uc2dd\uc744 \uc218\ud589\ud569\ub2c8\ub2e4." },
            { "Unoriented yellow corner", "\ubc29\ud5a5\uc774 \ub9de\uc9c0 \uc54a\uc740 \ub178\ub780 \ucf54\ub108" },
            { "Top yellow face", "\uc717\uba74 \ub178\ub780 \uba74" },
            { "Case 2 / 2: Anti-Sune Direction", "\uc0ac\ub840 2 / 2: Anti-Sune \ubc29\ud5a5" },
            { "Use the mirrored corner orientation when the yellow stickers point in the opposite pattern.", "\ub178\ub780 \uc2a4\ud2f0\ucee4\uac00 \ubc18\ub300 \ud328\ud134\uc744 \ubcf4\uc774\uba74 \ubc18\ub300 \ubc29\ud5a5 \ucf54\ub108 \uacf5\uc2dd\uc744 \uc0ac\uc6a9\ud569\ub2c8\ub2e4." },
            { "Opposite yellow corner pattern", "\ubc18\ub300 \ub178\ub780 \ucf54\ub108 \ud328\ud134" },
            { "The yellow cross must stay intact.", "\ub178\ub780 \uc2ed\uc790\ub294 \uc720\uc9c0\ub418\uc5b4\uc57c \ud569\ub2c8\ub2e4." },
            { "This step orients corners; it does not necessarily position them.", "\uc774 \ub2e8\uacc4\ub294 \ucf54\ub108 \ubc29\ud5a5\uc744 \ub9de\ucd94\uba70, \uc704\uce58\uae4c\uc9c0 \ub9de\ucd98\ub2e4\ub294 \ub73b\uc740 \uc544\ub2d9\ub2c8\ub2e4." },
            { "Rotate only the top layer between repetitions.", "\ubc18\ubcf5 \uc0ac\uc774\uc5d0\ub294 \uc717\uce35\ub9cc \ub3cc\ub9bd\ub2c8\ub2e4." },
            { "Finish with all nine top stickers yellow.", "\uc717\uba74 9\uac1c \uc2a4\ud2f0\ucee4\uac00 \ubaa8\ub450 \ub178\ub780\uc0c9\uc774\uba74 \ub05d\uc785\ub2c8\ub2e4." },
            { "Step 6: Position Last Layer Corners", "6\ub2e8\uacc4: \ub9c8\uc9c0\ub9c9 \uce35 \ucf54\ub108 \uc704\uce58" },
            { "Move the yellow corners into their correct locations.", "\ub178\ub780 \ucf54\ub108\ub97c \uc62c\ubc14\ub978 \uc704\uce58\ub85c \uc62e\uae41\ub2c8\ub2e4." },
            { "Keep the full yellow face on top.\n\nA corner is correctly positioned when its three colors match the three surrounding centers, even if you imagine its orientation separately.\nIf one corner is correct, keep it at the front-right and apply the corner-positioning algorithm.\nIf none are correct, apply it once from any angle, then place the newly correct corner at front-right and repeat.\n\nThe step is complete when all four yellow corners belong in their current locations.", "\uc644\uc131\ub41c \ub178\ub780 \uba74\uc744 \uc704\uc5d0 \ub461\ub2c8\ub2e4.\n\n\ucf54\ub108\uc758 \uc138 \uc0c9\uc774 \uc8fc\ubcc0 \uc13c\ud130 3\uac1c\uc640 \ub9de\ub294 \uc704\uce58\ub77c\uba74, \ubc29\ud5a5\uc744 \ub530\ub85c \uc0dd\uac01\ud558\ub354\ub77c\ub3c4 \uc704\uce58\ub294 \ub9de\uc740 \uac83\uc785\ub2c8\ub2e4.\n\ub9de\uc740 \ucf54\ub108\uac00 \ud558\ub098 \uc788\uc73c\uba74 \uc55e-\uc624\ub978\ucabd\uc5d0 \ub450\uace0 \ucf54\ub108 \uc704\uce58 \uacf5\uc2dd\uc744 \uc801\uc6a9\ud569\ub2c8\ub2e4.\n\ud558\ub098\ub3c4 \ub9de\uc9c0 \uc54a\uc73c\uba74 \uc544\ubb34 \uac01\ub3c4\uc5d0\uc11c \ud55c \ubc88 \uc801\uc6a9\ud55c \ub4a4 \uc0c8\ub85c \ub9de\uc740 \ucf54\ub108\ub97c \uc55e-\uc624\ub978\ucabd\uc5d0 \ub450\uace0 \ubc18\ubcf5\ud569\ub2c8\ub2e4.\n\n\ub178\ub780 \ucf54\ub108 4\uac1c\uac00 \ubaa8\ub450 \uc790\uae30 \uc704\uce58\uc5d0 \uc788\uc73c\uba74 \uc644\ub8cc\uc785\ub2c8\ub2e4." },
            { "Position every yellow corner while preserving the yellow face and first two layers.", "\ub178\ub780 \uba74\uacfc \uccab \ub450 \uce35\uc744 \uc720\uc9c0\ud558\uba74\uc11c \ubaa8\ub4e0 \ub178\ub780 \ucf54\ub108 \uc704\uce58\ub97c \ub9de\ucda5\ub2c8\ub2e4." },
            { "Corner Positioning Demo", "\ucf54\ub108 \uc704\uce58 \ub9de\ucda4 \ub370\ubaa8" },
            { "Cycle the top corners into their matching center-color locations. Repeat from the instructed reference angle when a real cube needs another cycle.", "\uc717\uce35 \ucf54\ub108\ub4e4\uc744 \ub9de\ub294 \uc13c\ud130 \uc0c9 \uc704\uce58\ub85c \uc21c\ud658\uc2dc\ud0b5\ub2c8\ub2e4. \uc2e4\uc81c \ud050\ube0c\uc5d0\uc11c \ud55c \ubc88 \ub354 \ud544\uc694\ud558\uba74 \uc548\ub0b4\ub41c \uae30\uc900 \uac01\ub3c4\uc5d0\uc11c \ubc18\ubcf5\ud569\ub2c8\ub2e4." },
            { "Three misplaced yellow corners", "\uc704\uce58\uac00 \ud2c0\ub9b0 \ub178\ub780 \ucf54\ub108 3\uac1c" },
            { "Correct top-corner positions", "\uc62c\ubc14\ub978 \uc717\uce35 \ucf54\ub108 \uc704\uce58" },
            { "Judge a corner by all three colors, not only yellow.", "\ub178\ub780\uc0c9\ub9cc \ubcf4\uc9c0 \ub9d0\uace0 \ucf54\ub108\uc758 \uc138 \uc0c9 \ubaa8\ub450\ub85c \ud310\ub2e8\ud569\ub2c8\ub2e4." },
            { "The yellow face should remain oriented.", "\ub178\ub780 \uba74 \ubc29\ud5a5\uc740 \uc720\uc9c0\ub418\uc5b4\uc57c \ud569\ub2c8\ub2e4." },
            { "Use a correct corner as the reference at top-front-right.", "\ub9de\uc740 \ucf54\ub108\ub97c \uc704-\uc55e-\uc624\ub978\ucabd \uae30\uc900\uc73c\ub85c \ub461\ub2c8\ub2e4." },
            { "Side-face corner colors must match after completion.", "\uc644\ub8cc \ud6c4 \uc606\uba74 \ucf54\ub108 \uc0c9\ub3c4 \ub9de\uc544\uc57c \ud569\ub2c8\ub2e4." },
            { "Step 7: Position Last Layer Edges", "7\ub2e8\uacc4: \ub9c8\uc9c0\ub9c9 \uce35 \uc5e3\uc9c0 \uc704\uce58" },
            { "Cycle the final yellow edges to solve the cube.", "\ub9c8\uc9c0\ub9c9 \ub178\ub780 \uc5e3\uc9c0\ub97c \uc21c\ud658\uc2dc\ucf1c \ud050\ube0c\ub97c \uc644\uc131\ud569\ub2c8\ub2e4." },
            { "All corners are now correct. Only the four top-layer edges may remain out of position.\n\nIf one side is already solved, hold that solved side at the back.\nChoose the clockwise or counter-clockwise edge cycle according to where the remaining edges must move.\nIf no side is solved, perform one cycle, find the solved side, place it at the back, and finish.\n\nThe cube is complete when every face has one uniform color.", "\uc774\uc81c \ubaa8\ub4e0 \ucf54\ub108\ub294 \ub9de\uc558\uc2b5\ub2c8\ub2e4. \uc717\uce35 \uc5e3\uc9c0 4\uac1c\ub9cc \uc704\uce58\uac00 \ud2c0\ub9b4 \uc218 \uc788\uc2b5\ub2c8\ub2e4.\n\n\uc774\ubbf8 \ub9de\uc740 \uc606\uba74\uc774 \ud558\ub098 \uc788\uc73c\uba74 \uadf8 \uba74\uc744 \ub4a4\uc5d0 \ub461\ub2c8\ub2e4.\n\ub0a8\uc740 \uc5e3\uc9c0\uac00 \uac00\uc57c \ud558\ub294 \ubc29\ud5a5\uc5d0 \ub530\ub77c \uc2dc\uacc4\ubc29\ud5a5 \ub610\ub294 \ubc18\uc2dc\uacc4\ubc29\ud5a5 \uc5e3\uc9c0 \uc21c\ud658\uc744 \uc120\ud0dd\ud569\ub2c8\ub2e4.\n\ub9de\uc740 \uba74\uc774 \ud558\ub098\ub3c4 \uc5c6\uc73c\uba74 \ud55c \ubc88 \uc21c\ud658\ud55c \ub4a4 \ub9de\uc740 \uba74\uc744 \ucc3e\uc544 \ub4a4\uc5d0 \ub450\uace0 \ub9c8\ubb34\ub9ac\ud569\ub2c8\ub2e4.\n\n\ubaa8\ub4e0 \uba74\uc774 \ud55c \uac00\uc9c0 \uc0c9\uc73c\ub85c \uc644\uc131\ub418\uba74 \ud050\ube0c\uac00 \uc644\uc131\uc785\ub2c8\ub2e4." },
            { "Position the final four edges and finish the cube.", "\ub9c8\uc9c0\ub9c9 \uc5e3\uc9c0 4\uac1c\uc758 \uc704\uce58\ub97c \ub9de\ucdb0 \ud050\ube0c\ub97c \uc644\uc131\ud569\ub2c8\ub2e4." },
            { "Case 1 / 2: Clockwise Edge Cycle", "\uc0ac\ub840 1 / 2: \uc2dc\uacc4\ubc29\ud5a5 \uc5e3\uc9c0 \uc21c\ud658" },
            { "Keep the solved side at the back and cycle the remaining top edges clockwise.", "\ub9de\uc740 \uba74\uc744 \ub4a4\uc5d0 \ub450\uace0 \ub0a8\uc740 \uc717\uce35 \uc5e3\uc9c0\ub97c \uc2dc\uacc4\ubc29\ud5a5\uc73c\ub85c \uc21c\ud658\ud569\ub2c8\ub2e4." },
            { "Three misplaced top edges", "\uc704\uce58\uac00 \ud2c0\ub9b0 \uc717\uce35 \uc5e3\uc9c0 3\uac1c" },
            { "Solved cube", "\uc644\uc131\ub41c \ud050\ube0c" },
            { "Case 2 / 2: Counter-clockwise Edge Cycle", "\uc0ac\ub840 2 / 2: \ubc18\uc2dc\uacc4\ubc29\ud5a5 \uc5e3\uc9c0 \uc21c\ud658" },
            { "Keep the solved side at the back and cycle the remaining top edges counter-clockwise.", "\ub9de\uc740 \uba74\uc744 \ub4a4\uc5d0 \ub450\uace0 \ub0a8\uc740 \uc717\uce35 \uc5e3\uc9c0\ub97c \ubc18\uc2dc\uacc4\ubc29\ud5a5\uc73c\ub85c \uc21c\ud658\ud569\ub2c8\ub2e4." },
            { "Do not rotate the whole cube after choosing the solved back side.", "\ub9de\uc740 \ub4b7\uba74\uc744 \uc815\ud55c \ub4a4\uc5d0\ub294 \ud050\ube0c \uc804\uccb4\ub97c \ub3cc\ub9ac\uc9c0 \ub9c8\uc138\uc694." },
            { "Use the cycle direction that sends each edge toward its matching center.", "\uac01 \uc5e3\uc9c0\uac00 \ub9de\ub294 \uc13c\ud130\ub85c \uac00\ub294 \uc21c\ud658 \ubc29\ud5a5\uc744 \uc0ac\uc6a9\ud569\ub2c8\ub2e4." },
            { "All corners must remain solved.", "\ubaa8\ub4e0 \ucf54\ub108\ub294 \ub9de\uc740 \uc0c1\ud0dc\ub85c \uc720\uc9c0\ub418\uc5b4\uc57c \ud569\ub2c8\ub2e4." },
            { "Completion means every face is a single color.", "\uc644\ub8cc\ub294 \ubaa8\ub4e0 \uba74\uc774 \ud558\ub098\uc758 \uc0c9\uc774 \ub418\ub294 \uac83\uc744 \ub73b\ud569\ub2c8\ub2e4." },
            { "Lesson explanation is available; the dedicated 3D case is next.", "\uac15\uc758 \uc124\uba85\uc740 \uc900\ube44\ub418\uc5b4 \uc788\uc2b5\ub2c8\ub2e4. \uc804\uc6a9 3D \uc0ac\ub840\ub294 \ub2e4\uc74c \uc5c5\ub370\uc774\ud2b8\uc5d0\uc11c \ucd94\uac00\ub429\ub2c8\ub2e4." },
            { "This step remains part of the complete beginner method. Its dedicated start state and guided 3D demo will be added in the next Learn expansion.", "\uc774 \ub2e8\uacc4\ub294 \uc804\uccb4 \ucd08\uae09 \uacf5\uc2dd\uc758 \uc77c\ubd80\uc785\ub2c8\ub2e4. \uc804\uc6a9 \uc2dc\uc791 \uc0c1\ud0dc\uc640 3D \uc548\ub0b4 \ub370\ubaa8\ub294 \ub2e4\uc74c Learn \ud655\uc7a5\uc5d0\uc11c \ucd94\uac00\ub429\ub2c8\ub2e4." },
            { "Review the completed earlier steps before continuing.", "\uacc4\uc18d\ud558\uae30 \uc804\uc5d0 \uc55e \ub2e8\uacc4\uac00 \uc644\ub8cc\ub418\uc5c8\ub294\uc9c0 \ud655\uc778\ud558\uc138\uc694." },
            { "This demo shows the move pattern only. Use this formula when your cube has the correct case.", "\uc774 \ub370\ubaa8\ub294 \ub3d9\uc791 \ud328\ud134\ub9cc \ubcf4\uc5ec\uc90d\ub2c8\ub2e4. \uc2e4\uc81c \ud050\ube0c\uac00 \ub9de\ub294 \ucf00\uc774\uc2a4\uc77c \ub54c \uc774 \uacf5\uc2dd\uc744 \uc0ac\uc6a9\ud558\uc138\uc694." },
            { "Watch the complete algorithm, or step through one move at a time.", "\uc804\uccb4 \uacf5\uc2dd\uc744 \ubcf4\uac70\ub098 \ud55c \ub3d9\uc791\uc529 \ub530\ub77c\uac00\uc138\uc694." },
            { "Pattern demo only. This is not a full solving case.", "\ud328\ud134 \ub370\ubaa8 \uc804\uc6a9\uc785\ub2c8\ub2e4. \uc804\uccb4 \ud480\uc774 \ucf00\uc774\uc2a4\ub294 \uc544\ub2d9\ub2c8\ub2e4." },
            { "Keep the same orientation for the whole sequence.", "\uc804\uccb4 \uc21c\uc11c \ub3d9\uc548 \uac19\uc740 \ubc29\ud5a5\uc744 \uc720\uc9c0\ud558\uc138\uc694." }
        };
        private static GameObject CreateFullRoot(RectTransform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return child;
        }

        private static bool HasCaseNavigation(LearnLessonData lesson)
        {
            return lesson != null
                && lesson.demoSubsteps != null
                && lesson.demoSubsteps.Length > 0;
        }

        private static void SetBottomAnchored(RectTransform rect, Vector2 position, Vector2 size, bool stretchX = false)
        {
            rect.anchorMin = stretchX ? new Vector2(0f, 0f) : new Vector2(0.5f, 0f);
            rect.anchorMax = stretchX ? new Vector2(1f, 0f) : new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static RectTransform CreateContentRoot(RectTransform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 940f);
            return rect;
        }

        private static RectTransform GetScrollFrame(RectTransform content)
        {
            return content != null && content.parent != null
                ? content.parent.parent as RectTransform
                : null;
        }

        private float GetCanvasHeight()
        {
            RectTransform rootRect = root != null ? root.GetComponent<RectTransform>() : null;
            return rootRect != null && rootRect.rect.height > 0f
                ? rootRect.rect.height
                : ReferenceCanvasHeight;
        }

        private static float GetSafeAreaTopInsetInCanvas(float canvasHeight)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return 0f;
            }

            Rect safeArea = Screen.safeArea;
            float minimumTopInset = Mathf.Min(Screen.height * 0.012f, 56f);
            safeArea.yMax = Mathf.Min(safeArea.yMax, Screen.height - minimumTopInset);
            float topInsetPixels = Mathf.Max(0f, Screen.height - safeArea.yMax);
            return topInsetPixels * (canvasHeight / Screen.height);
        }

        private static void SetTopTextY(Text text, float y)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        private static Text CreateTopText(Transform parent, string name, string text, int size, float y, float height)
        {
            Text label = RuntimeUiFactory.CreateText(parent.GetComponent<RectTransform>(), name, text, size, TextAnchor.UpperCenter);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, y);
            label.rectTransform.sizeDelta = new Vector2(-80f, height);
            label.fontStyle = name == "Title" ? FontStyle.Bold : FontStyle.Normal;
            label.color = name == "Title"
                ? new Color(0.96f, 0.97f, 1f, 1f)
                : new Color(0.82f, 0.86f, 0.94f, 1f);
            CasualUIStyle.ApplyTextDepth(label, name == "Title");
            return label;
        }

        private static RectTransform CreateScrollArea(Transform parent, string name, float topY, float height, bool framed = false)
        {
            GameObject scrollObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);
            RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 1f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = new Vector2(0f, topY);
            scrollRect.sizeDelta = new Vector2(-70f, height);
            Image scrollImage = scrollObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(scrollImage, LearnPanelBackgroundColor, framed ? 32 : 20);
            if (framed)
            {
                Outline outline = scrollObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.66f, 0.12f, 0.95f);
                outline.effectDistance = new Vector2(3f, -3f);
                Shadow shadow = scrollObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.64f);
                shadow.effectDistance = new Vector2(0f, -8f);
            }

            GameObject viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            float padding = framed ? 38f : 12f;
            viewport.offsetMin = new Vector2(padding, padding);
            viewport.offsetMax = new Vector2(-padding, -padding);
            viewportObject.GetComponent<Image>().color = Color.white;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, height);

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;
            return content;
        }

        private static void AddBodyParagraphs(RectTransform parent, ref float y, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string normalized = text.Replace("\r\n", "\n");
            string[] paragraphs = normalized.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string paragraph in paragraphs)
            {
                AddDetailBullet(parent, ref y, paragraph.Trim());
            }
        }

        private static void AddDetailBullet(RectTransform parent, ref float y, string text)
        {
            AddDetailBullet(parent, ref y, text, 34, new Color(0.96f, 0.97f, 1f, 1f));
        }

        private static void AddDetailBullet(RectTransform parent, ref float y, string text, int fontSize, Color bodyColor)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            GameObject rowObject = new GameObject("GeneratedBullet", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, y);
            row.sizeDelta = new Vector2(0f, 64f);

            Text bullet = RuntimeUiFactory.CreateText(row, "Bullet", "-", fontSize, TextAnchor.UpperCenter);
            bullet.color = new Color(0.94f, 0.95f, 1f, 1f);
            bullet.rectTransform.anchorMin = new Vector2(0f, 1f);
            bullet.rectTransform.anchorMax = new Vector2(0f, 1f);
            bullet.rectTransform.pivot = new Vector2(0f, 1f);
            bullet.rectTransform.anchoredPosition = new Vector2(30f, 0f);
            bullet.rectTransform.sizeDelta = new Vector2(34f, 48f);
            CasualUIStyle.ApplyTextDepth(bullet, false);

            Text body = RuntimeUiFactory.CreateText(row, "Body", NormalizeInlineText(text), fontSize, TextAnchor.UpperLeft);
            body.color = bodyColor;
            body.rectTransform.anchorMin = new Vector2(0f, 1f);
            body.rectTransform.anchorMax = new Vector2(1f, 1f);
            body.rectTransform.pivot = new Vector2(0f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(84f, 0f);
            body.rectTransform.sizeDelta = new Vector2(-124f, 240f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.lineSpacing = 1.05f;
            CasualUIStyle.ApplyTextDepth(body, false);

            float rowHeight = Mathf.Max(48f, body.preferredHeight) + 18f;
            row.sizeDelta = new Vector2(0f, rowHeight);
            body.rectTransform.sizeDelta = new Vector2(-124f, rowHeight);
            y -= rowHeight + 8f;
        }

        private static void AddDetailSectionHeader(RectTransform parent, ref float y, string text)
        {
            y -= 10f;
            Text header = RuntimeUiFactory.CreateText(parent, "GeneratedSectionHeader", text, 38, TextAnchor.MiddleCenter);
            header.fontStyle = FontStyle.Bold;
            header.color = new Color(1f, 0.78f, 0.22f, 1f);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, y);
            header.rectTransform.sizeDelta = new Vector2(-80f, 58f);
            CasualUIStyle.ApplyTextDepth(header, true);
            y -= 72f;
        }

        private static void AddDetailNote(RectTransform parent, ref float y, string text)
        {
            y -= 8f;
            GameObject noteObject = new GameObject("GeneratedNote", typeof(RectTransform), typeof(Image), typeof(Outline));
            noteObject.transform.SetParent(parent, false);
            RectTransform note = noteObject.GetComponent<RectTransform>();
            note.anchorMin = new Vector2(0f, 1f);
            note.anchorMax = new Vector2(1f, 1f);
            note.pivot = new Vector2(0.5f, 1f);
            note.anchoredPosition = new Vector2(0f, y);
            note.sizeDelta = new Vector2(-160f, 70f);
            CasualUIStyle.ApplyPanel(noteObject.GetComponent<Image>(), new Color(0.08f, 0.14f, 0.22f, 0.52f), 18);
            Outline outline = noteObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.72f, 0.92f, 0.42f);
            outline.effectDistance = new Vector2(2f, -2f);

            Text label = RuntimeUiFactory.CreateText(note, "NoteText", text, 26, TextAnchor.MiddleCenter);
            label.color = new Color(0.86f, 0.9f, 0.98f, 1f);
            label.rectTransform.offsetMin = new Vector2(26f, 0f);
            label.rectTransform.offsetMax = new Vector2(-26f, 0f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            CasualUIStyle.ApplyTextDepth(label, false);
            y -= 88f;
        }

        private static string NormalizeInlineText(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Replace("\r\n", "\n").Replace("\n", " ");
        }

        private static void CreateLessonIcon(RectTransform row, string lessonId)
        {
            GameObject badgeObject = new GameObject("IconBadge", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow));
            badgeObject.transform.SetParent(row, false);
            RectTransform badge = badgeObject.GetComponent<RectTransform>();
            badge.anchorMin = new Vector2(0f, 0.5f);
            badge.anchorMax = new Vector2(0f, 0.5f);
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition = new Vector2(112f, 0f);
            badge.sizeDelta = new Vector2(132f, 132f);
            Image badgeImage = badgeObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(badgeImage, new Color(0.09f, 0.19f, 0.31f, 0.98f), 30);
            badgeObject.GetComponent<Outline>().effectColor = new Color(0.92f, 0.72f, 0.34f, 0.74f);
            badgeObject.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            badgeObject.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);
            badgeObject.GetComponent<Shadow>().effectDistance = new Vector2(0f, -5f);
            badgeImage.raycastTarget = false;

            if (string.Equals(lessonId, "orientation", StringComparison.OrdinalIgnoreCase))
            {
                CreateCompassIcon(badge);
            }
            else if (string.Equals(lessonId, "faces", StringComparison.OrdinalIgnoreCase))
            {
                CreateCubeFaceIcon(badge);
            }
            else
            {
                CreateTurnIcon(badge);
            }
        }

        private static void CreateCompassIcon(RectTransform parent)
        {
            CreateIconLine(parent, "NorthNeedle", new Vector2(0f, 12f), new Vector2(18f, 78f), -34f, new Color(1f, 0.78f, 0.18f, 1f));
            CreateIconLine(parent, "SouthNeedle", new Vector2(0f, -18f), new Vector2(14f, 54f), 146f, new Color(0.38f, 0.62f, 0.95f, 0.92f));
            CreateIconDot(parent, "CompassCenter", new Vector2(0f, 0f), new Vector2(20f, 20f), new Color(1f, 0.9f, 0.45f, 1f), 10);
        }

        private static void CreateCubeFaceIcon(RectTransform parent)
        {
            CreateIconPanel(parent, "TopFace", new Vector2(0f, 28f), new Vector2(54f, 42f), new Color(1f, 0.78f, 0.25f, 1f), 8);
            CreateIconPanel(parent, "LeftFace", new Vector2(-26f, -10f), new Vector2(48f, 56f), new Color(0.25f, 0.55f, 0.95f, 1f), 8);
            CreateIconPanel(parent, "RightFace", new Vector2(26f, -10f), new Vector2(48f, 56f), new Color(0.95f, 0.58f, 0.16f, 1f), 8);
        }

        private static void CreateTurnIcon(RectTransform parent)
        {
            Text arrow = RuntimeUiFactory.CreateText(parent, "TurnArrow", "R", 70, TextAnchor.MiddleCenter);
            arrow.fontStyle = FontStyle.Bold;
            arrow.color = new Color(1f, 0.74f, 0.16f, 1f);
            arrow.rectTransform.anchorMin = Vector2.zero;
            arrow.rectTransform.anchorMax = Vector2.one;
            arrow.rectTransform.offsetMin = Vector2.zero;
            arrow.rectTransform.offsetMax = Vector2.zero;
            CasualUIStyle.ApplyTextDepth(arrow, true);
            CreateIconLine(parent, "TopArc", new Vector2(0f, 30f), new Vector2(72f, 12f), -14f, new Color(0.28f, 0.58f, 1f, 0.9f));
            CreateIconLine(parent, "BottomArc", new Vector2(0f, -30f), new Vector2(72f, 12f), -14f, new Color(0.28f, 0.58f, 1f, 0.9f));
        }

        private static void CreateIconLine(RectTransform parent, string name, Vector2 position, Vector2 size, float angle, Color color)
        {
            GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(parent, false);
            RectTransform line = lineObject.GetComponent<RectTransform>();
            line.anchorMin = new Vector2(0.5f, 0.5f);
            line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = position;
            line.sizeDelta = size;
            line.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image image = lineObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, 7);
            image.raycastTarget = false;
        }

        private static void CreateIconPanel(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, int radius)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            panelObject.transform.SetParent(parent, false);
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = position;
            panel.sizeDelta = size;
            Image image = panelObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, radius);
            panelObject.GetComponent<Outline>().effectColor = new Color(0.02f, 0.02f, 0.02f, 0.56f);
            panelObject.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            image.raycastTarget = false;
        }

        private static void CreateIconDot(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, int radius)
        {
            GameObject dotObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(parent, false);
            RectTransform dot = dotObject.GetComponent<RectTransform>();
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.anchoredPosition = position;
            dot.sizeDelta = size;
            Image image = dotObject.GetComponent<Image>();
            CasualUIStyle.ApplyPanel(image, color, radius);
            image.raycastTarget = false;
        }

        private static void StyleSmallButton(Button button)
        {
            CasualUIStyle.ApplyButton(button, CasualUIColor.Blue);
            StyleButtonText(button, 22);
        }

        private static void StyleButtonText(Button button, int fontSize)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(16, fontSize - 12);
            label.resizeTextMaxSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            CasualUIStyle.ApplyTextDepth(label, true);
        }

        private static void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static void ClearGeneratedChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name.StartsWith("Generated", StringComparison.Ordinal))
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }

        private static void AddDragBar(RectTransform parent)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image), typeof(PanelDragHandle));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 52f);
            barObject.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.14f, 1f);
            barObject.GetComponent<PanelDragHandle>().Initialize(parent);
        }
    }
}
