using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Save;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class CachedRankingEntry
    {
        public string challengeId;
        public string fetchedAtUtc;
        public List<RankingSubmission> topList = new List<RankingSubmission>();
    }

    [Serializable]
    public sealed class CachedRankingData
    {
        public int saveVersion;
        public List<CachedRankingEntry> entries = new List<CachedRankingEntry>();
    }

    public sealed class CachedRankingStore
    {
        private const string FileName = "cached_ranking_records.json";
        private const int RankingAvatarSaveVersion = 3;

        private CachedRankingData data;

        public CachedRankingStore()
        {
            Load();
        }

        public void SaveCache(string challengeId, List<RankingSubmission> topList)
        {
            if (string.IsNullOrWhiteSpace(challengeId))
            {
                return;
            }

            data.entries.RemoveAll(entry => entry.challengeId == challengeId);
            data.entries.Add(new CachedRankingEntry
            {
                challengeId = challengeId,
                fetchedAtUtc = DateTime.UtcNow.ToString("o"),
                topList = topList ?? new List<RankingSubmission>()
            });
            Save();
        }

        public IReadOnlyList<RankingSubmission> GetCache(string challengeId)
        {
            CachedRankingEntry entry = data.entries.FirstOrDefault(item => item.challengeId == challengeId);
            return (entry?.topList ?? new List<RankingSubmission>()).AsReadOnly();
        }

        public void ClearCache()
        {
            data.entries.Clear();
            Save();
        }

        public void Load()
        {
            data = SaveService.LoadJson(FileName, new CachedRankingData());
            bool migrateMissingAvatarIds = data.saveVersion < RankingAvatarSaveVersion;
            bool changed = data.saveVersion < RankingAvatarSaveVersion;
            data.saveVersion = Math.Max(SaveDataValidator.CurrentSaveVersion, RankingAvatarSaveVersion);
            List<CachedRankingEntry> source = data.entries ?? new List<CachedRankingEntry>();
            data.entries = source
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.challengeId))
                .GroupBy(entry => entry.challengeId)
                .Select(group => group.OrderByDescending(entry => entry.fetchedAtUtc).First())
                .ToList();
            foreach (CachedRankingEntry entry in data.entries)
            {
                entry.topList = (entry.topList ?? new List<RankingSubmission>())
                    .Where(item => item != null)
                    .ToList();
                changed |= NormalizeAvatarIds(entry.topList, migrateMissingAvatarIds);
            }
            if (changed || data.entries.Count != source.Count)
            {
                Save();
            }
        }

        public void Save()
        {
            SaveService.SaveJson(FileName, data);
        }

        private static bool NormalizeAvatarIds(IEnumerable<RankingSubmission> submissions, bool migrateMissingAvatarIds)
        {
            bool changed = false;
            foreach (RankingSubmission submission in submissions)
            {
                if (submission == null)
                {
                    continue;
                }

                if (migrateMissingAvatarIds)
                {
                    submission.avatarId = -1;
                    changed = true;
                }
                else if (submission.avatarId < -1 || submission.avatarId > 3)
                {
                    submission.avatarId = -1;
                    changed = true;
                }
            }

            return changed;
        }
    }
}
