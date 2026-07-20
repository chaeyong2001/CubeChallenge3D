using System;
using System.Collections.Generic;
using System.Text;
using CubeChallenge3D.Audio;
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
        private const float DetailFooterViewportBottomInset = 300f;
        private const float CaseNavigationViewportBottomInset = 560f;
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
        private readonly RectTransform lessonTextRoot;
        private readonly RectTransform lessonDemoRoot;
        private readonly Text detailStatus;
        private readonly Text substepText;
        private readonly Button demoButton;
        private readonly Button completeButton;
        private readonly Button previousSubstepButton;
        private readonly Button nextSubstepButton;
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

            listTitle = CreateTopText(listRoot.transform, "Title", "Lessons", 58, LessonListBaseTitleY, 74f);
            listDescription = CreateTopText(listRoot.transform, "Description", string.Empty, 28, LessonListBaseSubtitleY, 48f);
            lessonContent = CreateScrollArea(listRoot.transform, "LessonScroll", LessonListPanelTopY, LessonListPanelBaseHeight, true);
            lessonFrame = GetScrollFrame(lessonContent);

            Button listBack = RuntimeUiFactory.CreateButton(
                listRoot.GetComponent<RectTransform>(),
                "BackButton",
                "Back",
                new Vector2(0f, 150f),
                new Vector2(360f, 78f));
            CasualUIStyle.ApplyButton(listBack, CasualUIColor.Blue);
            StyleButtonText(listBack, 34);
            listBack.onClick.AddListener(CloseToHub);

            detailTitle = CreateTopText(detailRoot.transform, "Title", "Lesson", 58, -305f, 78f);
            detailContent = CreateScrollArea(detailRoot.transform, "DetailScroll", LessonDetailPanelTopY, LessonDetailPanelHeight, true);
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
                "Previous Edge",
                new Vector2(-194f, 292f),
                new Vector2(220f, 50f));
            previousSubstepButton.onClick.AddListener(PreviousSubstep);

            nextSubstepButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "NextSubstepButton",
                "Next Edge",
                new Vector2(194f, 292f),
                new Vector2(220f, 50f));
            nextSubstepButton.onClick.AddListener(NextSubstep);

            StyleSmallButton(previousSubstepButton);
            StyleSmallButton(nextSubstepButton);

            completeButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "CompleteButton",
                "Mark Complete",
                new Vector2(-235f, 172f),
                new Vector2(430f, 86f));
            CasualUIStyle.ApplyButton(completeButton, CasualUIColor.Blue);
            StyleButtonText(completeButton, 34);
            completeButton.onClick.AddListener(MarkComplete);

            demoButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "Play3DDemoButton",
                "Play Demo",
                new Vector2(235f, 172f),
                new Vector2(430f, 86f));
            CasualUIStyle.ApplyButton(demoButton, CasualUIColor.Blue);
            StyleButtonText(demoButton, 34);
            demoButton.onClick.AddListener(PlayDemo);

            Button detailBack = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "BackToLessonsButton",
                "Back to Lessons",
                new Vector2(235f, 62f),
                new Vector2(430f, 76f));
            CasualUIStyle.ApplyButton(detailBack, CasualUIColor.Blue);
            StyleButtonText(detailBack, 32);
            detailBack.onClick.AddListener(ShowLessonList);

            playbackPanel = new SolverPlaybackPanelUI(
                lessonDemoRoot,
                ExitLessonDemoMode,
                "Learn 3D Demo",
                "Keep Front, Top, and Right fixed while watching the moves.",
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
                listTitle.text = "Lessons";
                listDescription.text = "No lesson category is available.";
                lessonContent.sizeDelta = new Vector2(0f, 100f);
                return;
            }

            listTitle.text = currentCategory.title;
            listDescription.text = currentCategory.description;
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
                lesson.title,
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
                lesson.shortDescription,
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

            Button open = RuntimeUiFactory.CreateButton(row, "OpenButton", "Open", Vector2.zero, new Vector2(190f, 78f));
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
            LearnStepDemoData substep = GetCurrentSubstep();
            detailTitle.text = currentLesson?.title ?? "Lesson unavailable";
            bool demoValid = LearnPlaybackAdapter.TryCreateSolution(
                currentLesson,
                substep,
                out _,
                out _);
            float preferredHeight = RebuildDetailContent(currentLesson, substep, demoValid);
            detailContent.sizeDelta = new Vector2(0f, preferredHeight);
            lessonTextRoot.sizeDelta = new Vector2(0f, preferredHeight);
            lessonDemoRoot.sizeDelta = new Vector2(0f, 940f);
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
                detailStatus.text = "Completed";
            }
            else if (!demoValid)
            {
                detailStatus.text = currentLesson != null && currentLesson.isExpandedContent
                    ? "This lesson does not include a 3D demo yet."
                    : "This lesson does not include a 3D demo.";
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
                    ? substep.substepTitle
                    : $"{substep.substepTitle}  ({currentSubstepIndex + 1}/{substepCount})";
                string itemName = string.Equals(currentLesson.demoType, "BeginnerMultiStep", StringComparison.Ordinal)
                    ? "Edge"
                    : "Case";
                previousSubstepButton.GetComponentInChildren<Text>().text = $"Previous {itemName}";
                nextSubstepButton.GetComponentInChildren<Text>().text = $"Next {itemName}";
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

            SetBottomAnchored(previousSubstepButton.GetComponent<RectTransform>(), new Vector2(-365f, 450f), new Vector2(220f, 86f));
            SetBottomAnchored(nextSubstepButton.GetComponent<RectTransform>(), new Vector2(365f, 450f), new Vector2(220f, 86f));
            SetBottomAnchored(substepText.rectTransform, new Vector2(0f, 456f), new Vector2(420f, 74f));
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
            AddDetailBullet(lessonTextRoot, ref y, lesson?.shortDescription);
            AddBodyParagraphs(lessonTextRoot, ref y, lesson?.bodyText);

            if (substep != null)
            {
                AddDetailBullet(lessonTextRoot, ref y, substep.substepTitle);
                AddDetailBullet(lessonTextRoot, ref y, substep.instructionText);
            }

            string[] demoMoves = substep?.demoMoves != null && substep.demoMoves.Length > 0
                ? substep.demoMoves
                : GetDemoMoves(lesson);
            if (demoMoves.Length > 0)
            {
                AddDetailBullet(lessonTextRoot, ref y, "Moves: " + string.Join(" ", demoMoves));
            }

            if (!string.IsNullOrWhiteSpace(lesson?.demoType))
            {
                AddDetailBullet(lessonTextRoot, ref y, "Demo Type: " + GetDemoTypeLabel(lesson.demoType));
            }

            if (!string.IsNullOrWhiteSpace(lesson?.demoPurpose))
            {
                AddDetailBullet(lessonTextRoot, ref y, "Demo Purpose: " + lesson.demoPurpose);
            }

            if (!string.IsNullOrWhiteSpace(lesson?.demoGoalDescription))
            {
                AddDetailBullet(lessonTextRoot, ref y, "Demo Goal: " + lesson.demoGoalDescription);
            }

            string pieceHint = !string.IsNullOrWhiteSpace(substep?.highlightedCubieHint)
                ? substep.highlightedCubieHint
                : lesson?.highlightedCubieHint;
            if (!string.IsNullOrWhiteSpace(pieceHint))
            {
                AddDetailBullet(lessonTextRoot, ref y, "Target Piece: " + pieceHint);
            }

            string slotHint = !string.IsNullOrWhiteSpace(substep?.targetSlotHint)
                ? substep.targetSlotHint
                : lesson?.targetSlotHint;
            if (!string.IsNullOrWhiteSpace(slotHint))
            {
                AddDetailBullet(lessonTextRoot, ref y, "Target Slot: " + slotHint);
            }

            if (lesson?.keyPoints != null && lesson.keyPoints.Length > 0)
            {
                AddDetailSectionHeader(lessonTextRoot, ref y, "Key Points");
                foreach (string point in lesson.keyPoints)
                {
                    AddDetailBullet(lessonTextRoot, ref y, point, 31, new Color(0.94f, 0.95f, 0.99f, 1f));
                }
            }

            if (lesson != null && !hasDemo)
            {
                AddDetailNote(lessonTextRoot, ref y, "This lesson does not include a 3D demo.");
            }

            if (lesson != null && lesson.isExpandedContent)
            {
                AddDetailNote(lessonTextRoot, ref y, "More guided cases will be expanded in the next Learn update.");
            }

            if (HasCaseNavigation(lesson))
            {
                y -= 128f;
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
                detailStatus.text = error;
                return;
            }

            detailStatus.text = string.Empty;
            string goal = !string.IsNullOrWhiteSpace(substep?.targetDescription)
                ? substep.targetDescription
                : currentLesson.demoGoalDescription;
            string guide = string.IsNullOrWhiteSpace(goal)
                ? "This is a demo. Follow the lesson explanation."
                : goal;
            if (currentLesson.demoType != null
                && currentLesson.demoType.StartsWith("Beginner", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(false);
                guide += " White is kept on the bottom in this beginner method.";
                if (string.Equals(currentLesson.demoType, "BeginnerMultiStep", StringComparison.Ordinal))
                {
                    guide += " Yellow marks the target edge; cyan marks the bottom slot.";
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
                    guide += " Yellow marks the target corner; cyan marks its first-layer slot.";
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
                guide += " Keep white on the bottom. Yellow marks the corner; cyan marks its slot.";
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
                guide += " Keep white on the bottom. Yellow marks the corner; cyan marks its slot.";
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
                guide += " Yellow marks the prepared pair reference; cyan marks the F2L slot.";
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
                guide += " Keep the solved layers underneath and watch the top-face result.";
                playbackPanel.SetInitialViewEuler(Vector3.zero);
                playbackPanel.SetCameraLocalPosition(new Vector3(4f, 4f, 6f));
                playbackPanel.SetCompletionCameraLocalPosition(new Vector3(0f, 5.8f, 4.2f));
                playbackPanel.SetLearningHighlights(false, Vector3Int.zero, Vector3Int.zero);
            }
            else if (string.Equals(currentLesson.demoType, "FormulaPattern", StringComparison.Ordinal))
            {
                playbackPanel.SetShowFaceLabels(true);
                guide += " Pattern demo only; use it in the correct solving case.";
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

            playbackPanel.SetPresentation(currentLesson.title + " Demo", guide);
            ShowLessonDemoMode();
            playbackPanel.Show(solution);
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
            detailContent.sizeDelta = new Vector2(0f, 940f);
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
            detailStatus.text = "Lesson completed.";
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
                return "Lesson data is unavailable.";
            }

            var builder = new StringBuilder();
            AppendBullet(builder, lesson.shortDescription);
            builder.AppendLine();
            AppendBulletParagraphs(builder, lesson.bodyText);

            if (substep != null)
            {
                builder.AppendLine();
                AppendBullet(builder, substep.substepTitle);
                AppendBullet(builder, substep.instructionText);
            }

            string[] demoMoves = substep?.demoMoves != null && substep.demoMoves.Length > 0
                ? substep.demoMoves
                : GetDemoMoves(lesson);
            if (demoMoves.Length > 0)
            {
                builder.AppendLine();
                AppendBullet(builder, "Moves: " + string.Join(" ", demoMoves));
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoType))
            {
                builder.AppendLine();
                AppendBullet(builder, "Demo Type: " + GetDemoTypeLabel(lesson.demoType));
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoPurpose))
            {
                builder.AppendLine();
                AppendBullet(builder, "Demo Purpose: " + lesson.demoPurpose);
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoGoalDescription))
            {
                builder.AppendLine();
                AppendBullet(builder, "Demo Goal: " + lesson.demoGoalDescription);
            }

            string pieceHint = !string.IsNullOrWhiteSpace(substep?.highlightedCubieHint)
                ? substep.highlightedCubieHint
                : lesson.highlightedCubieHint;
            if (!string.IsNullOrWhiteSpace(pieceHint))
            {
                builder.AppendLine();
                AppendBullet(builder, "Target Piece: " + pieceHint);
            }

            string slotHint = !string.IsNullOrWhiteSpace(substep?.targetSlotHint)
                ? substep.targetSlotHint
                : lesson.targetSlotHint;
            if (!string.IsNullOrWhiteSpace(slotHint))
            {
                AppendBullet(builder, "Target Slot: " + slotHint);
            }

            if (lesson.keyPoints != null && lesson.keyPoints.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Key Points");
                foreach (string point in lesson.keyPoints)
                {
                    builder.AppendLine("  - " + point);
                }
            }

            if (lesson.isExpandedContent)
            {
                builder.AppendLine();
                builder.AppendLine("More guided cases will be expanded in the next Learn update.");
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
                case "NotationMove": return "Move Demo";
                case "FormulaPattern": return "Pattern Demo - Not a full solving case";
                case "FormulaRightTrigger": return "Guided Demo - Insert a right-side corner";
                case "FormulaLeftTrigger": return "Guided Demo - Insert a left-side corner";
                case "FormulaSledgehammer": return "Guided Demo - Insert a prepared F2L pair";
                case "FormulaYellowCross": return "Guided Demo - Turn a yellow line into a cross";
                case "FormulaRightAlgorithm": return "Guided Demo - Complete the yellow face";
                case "BeginnerStep": return "Step Demo - Watch the target piece move into place";
                case "BeginnerMultiStep": return "Step-by-step Demo - Complete four white edges";
                case "BeginnerCornerStep": return "Case Demo - Insert white corners without breaking the cross";
                case "BeginnerSecondLayer": return "Case Demo - Complete the second layer";
                case "BeginnerYellowCross": return "Pattern Demo - Build the yellow cross";
                case "BeginnerYellowFace": return "Pattern Demo - Orient the yellow face";
                case "BeginnerLastCorners": return "Case Demo - Position the last-layer corners";
                case "BeginnerLastEdges": return "Case Demo - Position the final edges";
                default: return "Explanation Only";
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
