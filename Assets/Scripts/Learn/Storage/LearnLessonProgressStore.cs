using CubeChallenge3D.Learn.Model;
using CubeChallenge3D.Save;

namespace CubeChallenge3D.Learn.Storage
{
    public sealed class LearnLessonProgressStore
    {
        private const string FileName = "learn_progress.json";
        private LearnLessonProgress progress;

        private LearnLessonProgress Data => progress ?? (progress = Load());

        public LearnLessonProgressStore()
        {
            _ = Data;
        }

        public bool IsCompleted(string lessonId)
        {
            return !string.IsNullOrWhiteSpace(lessonId)
                && Data.completedLessonIds != null
                && Data.completedLessonIds.Contains(lessonId);
        }

        public void MarkCompleted(string lessonId)
        {
            if (string.IsNullOrWhiteSpace(lessonId))
            {
                return;
            }

            if (Data.completedLessonIds == null)
            {
                Data.completedLessonIds = new System.Collections.Generic.List<string>();
            }

            if (!Data.completedLessonIds.Contains(lessonId))
            {
                Data.completedLessonIds.Add(lessonId);
                SaveService.SaveJson(FileName, Data);
            }
        }

        private static LearnLessonProgress Load()
        {
            LearnLessonProgress loaded = SaveService.LoadJson(FileName, new LearnLessonProgress());
            if (SaveDataValidator.Normalize(loaded))
            {
                SaveService.SaveJson(FileName, loaded);
            }

            return loaded;
        }
    }
}
