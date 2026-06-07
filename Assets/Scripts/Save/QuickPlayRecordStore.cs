using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Save.Records;

namespace CubeChallenge3D.Save
{
    [Serializable]
    public sealed class QuickPlayRecordData
    {
        public List<QuickPlayResult> records = new List<QuickPlayResult>();
    }

    public sealed class QuickPlayRecordStore
    {
        private const string FileName = "quick_play_records.json";

        private readonly int maxRecords;
        private QuickPlayRecordData data;

        public QuickPlayRecordStore(int maxRecords = 100)
        {
            this.maxRecords = maxRecords;
            Load();
        }

        public int Count => data.records.Count;

        public void AddResult(QuickPlayResult result)
        {
            if (result == null)
            {
                return;
            }

            data.records.Add(result);
            SortAndTrim();
            Save();
        }

        public IReadOnlyList<QuickPlayResult> GetRecent(int maxCount)
        {
            return data.records
                .OrderByDescending(record => record.completedAtUtc)
                .Take(Math.Max(0, maxCount))
                .ToList()
                .AsReadOnly();
        }

        public QuickPlayResult GetBestByTime()
        {
            return data.records
                .Where(record => record.completed)
                .OrderBy(record => record.elapsedSeconds)
                .ThenBy(record => record.moveCount)
                .FirstOrDefault();
        }

        public QuickPlayResult GetBestByMoves()
        {
            return data.records
                .Where(record => record.completed)
                .OrderBy(record => record.moveCount)
                .ThenBy(record => record.elapsedSeconds)
                .FirstOrDefault();
        }

        public void ClearAll()
        {
            data.records.Clear();
            Save();
        }

        public void Load()
        {
            data = SaveService.LoadJson(FileName, new QuickPlayRecordData());
            if (data.records == null)
            {
                data.records = new List<QuickPlayResult>();
            }

            SortAndTrim();
        }

        public void Save()
        {
            SaveService.SaveJson(FileName, data);
        }

        private void SortAndTrim()
        {
            data.records = data.records
                .Where(record => record != null)
                .OrderByDescending(record => record.completedAtUtc)
                .Take(maxRecords)
                .ToList();
        }
    }
}
