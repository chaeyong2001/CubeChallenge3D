using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Save;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Progress
{
    public sealed class StageProgressStore
    {
        private const string FileName = "stage_progress.json";

        private StageProgressData data;

        public StageProgressStore()
        {
            Load();
        }

        public void Load()
        {
            data = SaveService.LoadJson(FileName, new StageProgressData());
            if (SaveDataValidator.Normalize(data))
            {
                Save();
            }
        }

        public void Save()
        {
            SaveDataValidator.Normalize(data);
            SaveService.SaveJson(FileName, data);
        }

        public void EnsureStageDefaults(IEnumerable<StageData> stages)
        {
            if (stages == null)
            {
                return;
            }

            foreach (StageData stage in stages.Where(item => item != null))
            {
                StageProgress progress = GetOrCreate(stage.stageId);
                if (stage.isUnlockedByDefault)
                {
                    progress.isUnlocked = true;
                }
            }

            Save();
        }

        public StageProgress GetProgress(string stageId)
        {
            return GetOrCreate(stageId);
        }

        public void MarkCleared(string stageId, int moves, float timeSeconds, int stars)
        {
            StageProgress progress = GetOrCreate(stageId);
            progress.isUnlocked = true;
            progress.isCleared = true;
            progress.clearCount = progress.clearCount == int.MaxValue ? int.MaxValue : progress.clearCount + 1;
            progress.stars = Math.Max(progress.stars, Math.Max(0, Math.Min(3, stars)));
            progress.lastClearedAtUtc = DateTime.UtcNow.ToString("o");
            if (progress.bestMoves < 0 || moves < progress.bestMoves)
            {
                progress.bestMoves = moves;
            }

            if (progress.bestTimeSeconds < 0f || timeSeconds < progress.bestTimeSeconds)
            {
                progress.bestTimeSeconds = timeSeconds;
            }

            Save();
        }

        public void UnlockStage(string stageId)
        {
            StageProgress progress = GetOrCreate(stageId);
            progress.isUnlocked = true;
            Save();
        }

        public bool IsUnlocked(string stageId)
        {
            return GetOrCreate(stageId).isUnlocked;
        }

        public void ClearAllForDebug()
        {
            data.stages.Clear();
            Save();
        }

        private StageProgress GetOrCreate(string stageId)
        {
            stageId = string.IsNullOrWhiteSpace(stageId) ? "unknown" : stageId;
            StageProgress progress = data.stages.FirstOrDefault(item => item.stageId == stageId);
            if (progress != null)
            {
                return progress;
            }

            progress = new StageProgress { stageId = stageId };
            data.stages.Add(progress);
            return progress;
        }
    }
}
