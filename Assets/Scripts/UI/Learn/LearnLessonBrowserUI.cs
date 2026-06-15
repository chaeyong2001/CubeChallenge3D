using System;
using System.Collections.Generic;
using System.Text;
using CubeChallenge3D.Learn.Model;
using CubeChallenge3D.Learn.Playback;
using CubeChallenge3D.Learn.Services;
using CubeChallenge3D.Learn.Storage;
using CubeChallenge3D.Solver.Model;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Solver.Playback;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Learn
{
    public sealed class LearnLessonBrowserUI
    {
        private readonly GameObject root;
        private readonly GameObject listRoot;
        private readonly GameObject detailRoot;
        private readonly Text listTitle;
        private readonly Text listDescription;
        private readonly RectTransform lessonContent;
        private readonly Text detailTitle;
        private readonly Text detailBody;
        private readonly RectTransform detailContent;
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
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "LearnLessonsCanvas", 1510);
            root = canvas.gameObject;

            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "LearnLessonsPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 920f));
            AddDragBar(panel);

            listRoot = CreateFullRoot(panel, "LessonListRoot");
            detailRoot = CreateFullRoot(panel, "LessonDetailRoot");

            listTitle = CreateTopText(listRoot.transform, "Title", "Lessons", 34, -40f, 52f);
            listDescription = CreateTopText(listRoot.transform, "Description", string.Empty, 20, -94f, 62f);
            lessonContent = CreateScrollArea(listRoot.transform, "LessonScroll", -168f, 610f);

            Button listBack = RuntimeUiFactory.CreateButton(
                listRoot.GetComponent<RectTransform>(),
                "BackButton",
                "Back",
                new Vector2(0f, 26f),
                new Vector2(300f, 58f));
            listBack.onClick.AddListener(CloseToHub);

            detailTitle = CreateTopText(detailRoot.transform, "Title", "Lesson", 34, -42f, 58f);
            detailContent = CreateScrollArea(detailRoot.transform, "DetailScroll", -118f, 520f);
            detailBody = RuntimeUiFactory.CreateText(detailContent, "DetailText", string.Empty, 22, TextAnchor.UpperLeft);
            detailBody.rectTransform.anchorMin = new Vector2(0f, 1f);
            detailBody.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailBody.rectTransform.pivot = new Vector2(0.5f, 1f);
            detailBody.rectTransform.anchoredPosition = Vector2.zero;
            detailBody.rectTransform.sizeDelta = new Vector2(-28f, 900f);
            detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailBody.verticalOverflow = VerticalWrapMode.Overflow;

            detailStatus = RuntimeUiFactory.CreateText(
                detailRoot.GetComponent<RectTransform>(),
                "Status",
                string.Empty,
                19,
                TextAnchor.MiddleCenter);
            detailStatus.rectTransform.anchorMin = new Vector2(0f, 0f);
            detailStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
            detailStatus.rectTransform.pivot = new Vector2(0.5f, 0f);
            detailStatus.rectTransform.anchoredPosition = new Vector2(0f, 136f);
            detailStatus.rectTransform.sizeDelta = new Vector2(-80f, 38f);

            substepText = RuntimeUiFactory.CreateText(
                detailRoot.GetComponent<RectTransform>(),
                "Substep",
                string.Empty,
                20,
                TextAnchor.MiddleCenter);
            substepText.rectTransform.anchorMin = new Vector2(0f, 0f);
            substepText.rectTransform.anchorMax = new Vector2(1f, 0f);
            substepText.rectTransform.pivot = new Vector2(0.5f, 0f);
            substepText.rectTransform.anchoredPosition = new Vector2(0f, 218f);
            substepText.rectTransform.sizeDelta = new Vector2(-210f, 44f);

            previousSubstepButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "PreviousSubstepButton",
                "Previous Edge",
                new Vector2(-194f, 166f),
                new Vector2(220f, 50f));
            previousSubstepButton.onClick.AddListener(PreviousSubstep);

            nextSubstepButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "NextSubstepButton",
                "Next Edge",
                new Vector2(194f, 166f),
                new Vector2(220f, 50f));
            nextSubstepButton.onClick.AddListener(NextSubstep);

            demoButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "Play3DDemoButton",
                "Play 3D Demo",
                new Vector2(-178f, 72f),
                new Vector2(290f, 58f));
            demoButton.onClick.AddListener(PlayDemo);

            completeButton = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "CompleteButton",
                "Mark Complete",
                new Vector2(178f, 72f),
                new Vector2(290f, 58f));
            completeButton.onClick.AddListener(MarkComplete);

            Button detailBack = RuntimeUiFactory.CreateButton(
                detailRoot.GetComponent<RectTransform>(),
                "BackToLessonsButton",
                "Back to Lessons",
                new Vector2(0f, 10f),
                new Vector2(320f, 56f));
            detailBack.onClick.AddListener(ShowLessonList);

            playbackPanel = new SolverPlaybackPanelUI(
                root.transform,
                "Learn 3D Demo",
                "Keep Front, Top, and Right fixed while watching the moves.");

            Hide();
        }

        public void ShowCategory(string categoryId)
        {
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
        }

        public void Hide()
        {
            playbackPanel?.Hide();
            root.SetActive(false);
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
            const float rowHeight = 104f;
            const float spacing = 12f;
            float totalHeight = Mathf.Max(610f, lessons.Count * (rowHeight + spacing));
            lessonContent.sizeDelta = new Vector2(0f, totalHeight);

            for (int i = 0; i < lessons.Count; i++)
            {
                LearnLessonData lesson = lessons[i];
                CreateLessonRow(lesson, i, rowHeight, spacing);
            }
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
            row.sizeDelta = new Vector2(-12f, height);
            rowObject.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.16f, 1f);

            string completion = progressStore.IsCompleted(lesson.lessonId) ? "Completed" : "Open";
            Text label = RuntimeUiFactory.CreateText(
                row,
                "Label",
                $"{lesson.title}\n{lesson.shortDescription}",
                20,
                TextAnchor.MiddleLeft);
            label.rectTransform.offsetMin = new Vector2(22f, 8f);
            label.rectTransform.offsetMax = new Vector2(-142f, -8f);

            Text state = RuntimeUiFactory.CreateText(row, "State", completion, 17, TextAnchor.MiddleCenter);
            state.rectTransform.anchorMin = new Vector2(1f, 0f);
            state.rectTransform.anchorMax = new Vector2(1f, 1f);
            state.rectTransform.pivot = new Vector2(1f, 0.5f);
            state.rectTransform.anchoredPosition = new Vector2(-12f, 0f);
            state.rectTransform.sizeDelta = new Vector2(120f, 0f);
            rowObject.GetComponent<Button>().onClick.AddListener(() => ShowLesson(lesson));
        }

        private void ShowLesson(LearnLessonData lesson)
        {
            currentLesson = lesson;
            currentSubstepIndex = 0;
            listRoot.SetActive(false);
            detailRoot.SetActive(true);
            RefreshLessonDetail();
        }

        private void RefreshLessonDetail()
        {
            LearnStepDemoData substep = GetCurrentSubstep();
            detailTitle.text = currentLesson?.title ?? "Lesson unavailable";
            detailBody.text = BuildDetailText(currentLesson, substep);
            float preferredHeight = Mathf.Max(590f, detailBody.preferredHeight + 36f);
            detailBody.rectTransform.sizeDelta = new Vector2(-28f, preferredHeight);
            detailContent.sizeDelta = new Vector2(0f, preferredHeight);
            bool demoValid = LearnPlaybackAdapter.TryCreateSolution(
                currentLesson,
                substep,
                out _,
                out string demoError);
            demoButton.interactable = demoValid;
            completeButton.interactable = currentLesson != null && !progressStore.IsCompleted(currentLesson.lessonId);
            if (currentLesson != null && progressStore.IsCompleted(currentLesson.lessonId))
            {
                detailStatus.text = "Completed";
            }
            else if (!demoValid)
            {
                detailStatus.text = currentLesson != null && currentLesson.isExpandedContent
                    ? "3D demo will be added later for this step."
                    : demoError;
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
            if (hasSubsteps)
            {
                substepText.text = $"{substep.substepTitle}  ({currentSubstepIndex + 1}/{substepCount})";
                string itemName = string.Equals(currentLesson.demoType, "BeginnerMultiStep", StringComparison.Ordinal)
                    ? "Edge"
                    : "Case";
                previousSubstepButton.GetComponentInChildren<Text>().text = $"Previous {itemName}";
                nextSubstepButton.GetComponentInChildren<Text>().text = $"Next {itemName}";
                previousSubstepButton.interactable = currentSubstepIndex > 0;
                nextSubstepButton.interactable = currentSubstepIndex < substepCount - 1;
            }
        }

        private void ShowLessonList()
        {
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
            playbackPanel.Show(solution);
        }

        private void MarkComplete()
        {
            if (currentLesson == null)
            {
                return;
            }

            progressStore.MarkCompleted(currentLesson.lessonId);
            completeButton.interactable = false;
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
            builder.AppendLine(lesson.shortDescription);
            builder.AppendLine();
            builder.AppendLine(lesson.bodyText);

            if (substep != null)
            {
                builder.AppendLine();
                builder.AppendLine(substep.substepTitle);
                builder.AppendLine(substep.instructionText);
            }

            string[] demoMoves = substep?.demoMoves != null && substep.demoMoves.Length > 0
                ? substep.demoMoves
                : GetDemoMoves(lesson);
            if (demoMoves.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Moves");
                builder.AppendLine(string.Join(" ", demoMoves));
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoType))
            {
                builder.AppendLine();
                builder.AppendLine($"Demo Type: {GetDemoTypeLabel(lesson.demoType)}");
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoPurpose))
            {
                builder.AppendLine();
                builder.AppendLine("Demo Purpose");
                builder.AppendLine(lesson.demoPurpose);
            }

            if (!string.IsNullOrWhiteSpace(lesson.demoGoalDescription))
            {
                builder.AppendLine();
                builder.AppendLine("Demo Goal");
                builder.AppendLine(lesson.demoGoalDescription);
            }

            string pieceHint = !string.IsNullOrWhiteSpace(substep?.highlightedCubieHint)
                ? substep.highlightedCubieHint
                : lesson.highlightedCubieHint;
            if (!string.IsNullOrWhiteSpace(pieceHint))
            {
                builder.AppendLine();
                builder.AppendLine($"Target Piece: {pieceHint}");
            }

            string slotHint = !string.IsNullOrWhiteSpace(substep?.targetSlotHint)
                ? substep.targetSlotHint
                : lesson.targetSlotHint;
            if (!string.IsNullOrWhiteSpace(slotHint))
            {
                builder.AppendLine($"Target Slot: {slotHint}");
            }

            if (lesson.keyPoints != null && lesson.keyPoints.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Key Points");
                foreach (string point in lesson.keyPoints)
                {
                    builder.AppendLine($"- {point}");
                }
            }

            if (lesson.isExpandedContent)
            {
                builder.AppendLine();
                builder.AppendLine("More guided cases will be expanded in the next Learn update.");
            }

            return builder.ToString();
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

        private static Text CreateTopText(Transform parent, string name, string text, int size, float y, float height)
        {
            Text label = RuntimeUiFactory.CreateText(parent.GetComponent<RectTransform>(), name, text, size, TextAnchor.UpperCenter);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, y);
            label.rectTransform.sizeDelta = new Vector2(-80f, height);
            return label;
        }

        private static RectTransform CreateScrollArea(Transform parent, string name, float topY, float height)
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
            scrollObject.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.9f);

            GameObject viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12f, 12f);
            viewport.offsetMax = new Vector2(-12f, -12f);
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

        private static void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
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
