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
        public List<CachedRankingEntry> entries = new List<CachedRankingEntry>();
    }

    public sealed class CachedRankingStore
    {
        private const string FileName = "cached_ranking_records.json";

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
            if (data.entries == null)
            {
                data.entries = new List<CachedRankingEntry>();
            }
        }

        public void Save()
        {
            SaveService.SaveJson(FileName, data);
        }
    }
}
