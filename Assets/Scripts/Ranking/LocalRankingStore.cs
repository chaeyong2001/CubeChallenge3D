using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Save;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class LocalRankingData
    {
        public int saveVersion;
        public List<RankingSubmission> submissions = new List<RankingSubmission>();
    }

    public sealed class LocalRankingStore
    {
        private const string FileName = "local_ranking_records.json";

        private readonly int maxRecords;
        private LocalRankingData data;

        public LocalRankingStore(int maxRecords = 200)
        {
            this.maxRecords = maxRecords;
            Load();
        }

        public void AddSubmission(RankingSubmission submission)
        {
            if (submission == null)
            {
                return;
            }

            if (data.submissions.Any(record => record.submissionId == submission.submissionId))
            {
                return;
            }

            data.submissions.Add(submission);
            SortAndTrim();
            Save();
        }

        public IReadOnlyList<RankingSubmission> GetTopByTime(string challengeId, int maxCount)
        {
            return RankingDisplayHelper.TakeTop(FilterChallenge(challengeId), maxCount).AsReadOnly();
        }

        public IReadOnlyList<RankingSubmission> GetTopByMoves(string challengeId, int maxCount)
        {
            return FilterChallenge(challengeId)
                .OrderBy(record => record.moveCount)
                .ThenBy(record => record.elapsedSeconds)
                .Take(Math.Max(0, maxCount))
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<RankingSubmission> GetRecent(string challengeId, int maxCount)
        {
            return FilterChallenge(challengeId)
                .OrderByDescending(record => record.completedAtUtc)
                .Take(Math.Max(0, maxCount))
                .ToList()
                .AsReadOnly();
        }

        public void Clear()
        {
            data.submissions.Clear();
            Save();
        }

        public void Load()
        {
            data = SaveService.LoadJson(FileName, new LocalRankingData());
            bool changed = data.saveVersion < SaveDataValidator.CurrentSaveVersion;
            data.saveVersion = SaveDataValidator.CurrentSaveVersion;
            int originalCount = data.submissions?.Count ?? 0;
            data.submissions = (data.submissions ?? new List<RankingSubmission>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.submissionId))
                .GroupBy(item => item.submissionId)
                .Select(group => group.OrderByDescending(item => item.completedAtUtc).First())
                .ToList();
            SortAndTrim();
            if (changed || data.submissions.Count != originalCount)
            {
                Save();
            }
        }

        public void Save()
        {
            SaveService.SaveJson(FileName, data);
        }

        private IEnumerable<RankingSubmission> FilterChallenge(string challengeId)
        {
            return data.submissions.Where(record => record != null
                && record.completed
                && record.isVerified
                && !record.isDebugClear
                && record.challengeId == challengeId);
        }

        private void SortAndTrim()
        {
            data.submissions = data.submissions
                .Where(record => record != null)
                .OrderByDescending(record => record.completedAtUtc)
                .Take(maxRecords)
                .ToList();
        }
    }
}
