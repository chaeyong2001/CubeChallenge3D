using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Save;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class PendingRankingSubmissionData
    {
        public List<RankingSubmission> submissions = new List<RankingSubmission>();
    }

    public sealed class PendingRankingSubmissionStore
    {
        private const string FileName = "pending_ranking_submissions.json";

        private readonly int maxPending;
        private PendingRankingSubmissionData data;

        public PendingRankingSubmissionStore(int maxPending = 100)
        {
            this.maxPending = maxPending;
            Load();
        }

        public void AddPending(RankingSubmission submission)
        {
            if (submission == null || data.submissions.Any(item => item.submissionId == submission.submissionId))
            {
                return;
            }

            submission.isSynced = false;
            submission.syncStatus = RankingSyncStatus.Pending;
            data.submissions.Add(submission);
            Trim();
            Save();
        }

        public IReadOnlyList<RankingSubmission> GetPending()
        {
            return data.submissions
                .Where(item => item != null && item.syncStatus == RankingSyncStatus.Pending)
                .ToList()
                .AsReadOnly();
        }

        public void MarkSynced(string submissionId)
        {
            RankingSubmission submission = Find(submissionId);
            if (submission == null)
            {
                return;
            }

            submission.isSynced = true;
            submission.syncStatus = RankingSyncStatus.Synced;
            Save();
        }

        public void MarkFailed(string submissionId, string reason)
        {
            RankingSubmission submission = Find(submissionId);
            if (submission == null)
            {
                return;
            }

            submission.isSynced = false;
            submission.syncStatus = RankingSyncStatus.Failed;
            Save();
        }

        public void Remove(string submissionId)
        {
            data.submissions.RemoveAll(item => item != null && item.submissionId == submissionId);
            Save();
        }

        public void ClearAll()
        {
            data.submissions.Clear();
            Save();
        }

        public void Load()
        {
            data = SaveService.LoadJson(FileName, new PendingRankingSubmissionData());
            if (data.submissions == null)
            {
                data.submissions = new List<RankingSubmission>();
            }

            Trim();
        }

        public void Save()
        {
            SaveService.SaveJson(FileName, data);
        }

        private RankingSubmission Find(string submissionId)
        {
            return data.submissions.FirstOrDefault(item => item != null && item.submissionId == submissionId);
        }

        private void Trim()
        {
            data.submissions = data.submissions
                .Where(item => item != null)
                .OrderByDescending(item => item.completedAtUtc)
                .Take(maxPending)
                .ToList();
        }
    }
}
