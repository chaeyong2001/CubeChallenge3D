using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Learn.Model;
using CubeChallenge3D.Learn.Playback;
using UnityEngine;

namespace CubeChallenge3D.Learn.Services
{
    public static class LearnDemoValidator
    {
        public static void ValidateBeginnerDemos(IReadOnlyList<LearnCategoryData> categories)
        {
            LearnLessonData lesson = categories?
                .SelectMany(category => category.lessons)
                .FirstOrDefault(item => item.lessonId == "beginner_white_cross");
            if (lesson?.demoSubsteps == null || lesson.demoSubsteps.Length != 4)
            {
                Debug.LogWarning("Learn White Cross must contain exactly four edge demo substeps.");
                return;
            }

            for (int i = 0; i < lesson.demoSubsteps.Length; i++)
            {
                if (!LearnPlaybackAdapter.TryCreateSolution(
                        lesson,
                        lesson.demoSubsteps[i],
                        out _,
                        out string error))
                {
                    Debug.LogWarning($"Learn White Cross edge demo {i + 1} is invalid: {error}");
                }
            }

            LearnLessonData corners = categories?
                .SelectMany(category => category.lessons)
                .FirstOrDefault(item => item.lessonId == "beginner_white_corners");
            if (corners?.demoSubsteps == null || corners.demoSubsteps.Length != 4)
            {
                Debug.LogWarning("Learn White Corners must contain exactly four case demos.");
                return;
            }

            for (int i = 0; i < corners.demoSubsteps.Length; i++)
            {
                if (!LearnPlaybackAdapter.TryCreateSolution(
                        corners,
                        corners.demoSubsteps[i],
                        out _,
                        out string error))
                {
                    Debug.LogWarning($"Learn White Corners case {i + 1} is invalid: {error}");
                }
            }

            string[] remainingLessonIds =
            {
                "beginner_second_layer",
                "beginner_yellow_cross",
                "beginner_yellow_face",
                "beginner_last_corners",
                "beginner_last_edges"
            };
            foreach (string lessonId in remainingLessonIds)
            {
                LearnLessonData remainingLesson = categories?
                    .SelectMany(category => category.lessons)
                    .FirstOrDefault(item => item.lessonId == lessonId);
                if (remainingLesson?.demoSubsteps == null || remainingLesson.demoSubsteps.Length == 0)
                {
                    Debug.LogWarning($"Learn lesson {lessonId} has no case demos.");
                    continue;
                }

                for (int i = 0; i < remainingLesson.demoSubsteps.Length; i++)
                {
                    if (!LearnPlaybackAdapter.TryCreateSolution(
                            remainingLesson,
                            remainingLesson.demoSubsteps[i],
                            out _,
                            out string error))
                    {
                        Debug.LogWarning($"Learn lesson {lessonId} case {i + 1} is invalid: {error}");
                    }
                }
            }

            LearnLessonData rightTrigger = categories?
                .SelectMany(category => category.lessons)
                .FirstOrDefault(item => item.lessonId == "formula_right_trigger");
            LearnStepDemoData rightTriggerCase = rightTrigger?.demoSubsteps?.FirstOrDefault();
            if (rightTriggerCase == null)
            {
                Debug.LogWarning("Learn Right Trigger demo is missing its insertion case.");
            }
            else if (!LearnPlaybackAdapter.TryCreateSolution(
                         rightTrigger,
                         rightTriggerCase,
                         out _,
                         out string rightTriggerError))
            {
                Debug.LogWarning($"Learn Right Trigger demo is invalid: {rightTriggerError}");
            }

            string[] formulaLessonIds =
            {
                "formula_left_trigger",
                "formula_sledgehammer",
                "formula_yellow_cross",
                "formula_right_algorithm"
            };
            foreach (string formulaLessonId in formulaLessonIds)
            {
                LearnLessonData formulaLesson = categories?
                    .SelectMany(category => category.lessons)
                    .FirstOrDefault(item => item.lessonId == formulaLessonId);
                LearnStepDemoData formulaCase = formulaLesson?.demoSubsteps?.FirstOrDefault();
                if (formulaCase == null)
                {
                    Debug.LogWarning($"Learn formula {formulaLessonId} is missing its guided case.");
                }
                else if (!LearnPlaybackAdapter.TryCreateSolution(
                             formulaLesson,
                             formulaCase,
                             out _,
                             out string formulaError))
                {
                    Debug.LogWarning($"Learn formula {formulaLessonId} is invalid: {formulaError}");
                }
            }
        }
    }
}
