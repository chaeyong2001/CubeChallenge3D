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
        private const int RankingAvatarSaveVersion = 3;

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

        public RankingRankResult GetRank(string challengeId, string submissionId)
        {
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                return new RankingRankResult
                {
                    success = false,
                    message = "Submission id is empty.",
                    rank = 0
                };
            }

            List<RankingSubmission> records = RankingDisplayHelper
                .SortByTimeThenMoves(FilterChallenge(challengeId))
                .ToList();
            int index = records.FindIndex(record => record.submissionId == submissionId);
            if (index < 0)
            {
                return new RankingRankResult
                {
                    success = false,
                    message = "Record is not available locally.",
                    rank = 0
                };
            }

            return new RankingRankResult
            {
                success = true,
                message = "Local rank.",
                rank = index + 1,
                record = records[index]
            };
        }

        public IReadOnlyList<RankingSubmission> GetPlayerRecords(string playerId, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return new List<RankingSubmission>().AsReadOnly();
            }

            return data.submissions
                .Where(record => record != null
                    && record.completed
                    && record.isVerified
                    && !record.isDebugClear
                    && record.playerId == playerId)
                .OrderBy(record => record.elapsedSeconds)
                .ThenBy(record => record.moveCount)
                .ThenBy(record => record.completedAtUtc)
                .Take(Math.Max(0, maxCount))
                .ToList()
                .AsReadOnly();
        }

        public void UpdateAvatarForPlayer(string playerId, string playerName, int avatarId)
        {
            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            int clampedAvatarId = Math.Max(0, Math.Min(3, avatarId));
            bool changed = false;
            foreach (RankingSubmission submission in data.submissions)
            {
                if (submission == null)
                {
                    continue;
                }

                if (submission.playerId == playerId
                    && string.Equals(submission.playerName, playerName, StringComparison.OrdinalIgnoreCase)
                    && submission.avatarId != clampedAvatarId)
                {
                    submission.avatarId = clampedAvatarId;
                    changed = true;
                }
            }

            if (changed)
            {
                Save();
            }
        }

        public void Clear()
        {
            data.submissions.Clear();
            Save();
        }

        public void Load()
        {
            data = SaveService.LoadJson(FileName, new LocalRankingData());
            bool migrateMissingAvatarIds = data.saveVersion < RankingAvatarSaveVersion;
            bool changed = data.saveVersion < RankingAvatarSaveVersion;
            data.saveVersion = Math.Max(SaveDataValidator.CurrentSaveVersion, RankingAvatarSaveVersion);
            int originalCount = data.submissions?.Count ?? 0;
            data.submissions = (data.submissions ?? new List<RankingSubmission>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.submissionId))
                .GroupBy(item => item.submissionId)
                .Select(group => group.OrderByDescending(item => item.completedAtUtc).First())
                .ToList();
            changed |= NormalizeAvatarIds(data.submissions, migrateMissingAvatarIds);
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
