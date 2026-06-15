using System;
using CubeChallenge3D.Save;

namespace CubeChallenge3D.Solver
{
    [Serializable]
    public sealed class SolverUsageData
    {
        public string usageDateUtc;
        public int dailyFreeUsed;
        public int adBonusUses;
        public int ticketUses;
    }

    public sealed class SolverUsageStore
    {
        private const string FileName = "solver_usage.json";
        private const int DailyFreeUses = 3;
        private SolverUsageData data;

        public SolverUsageData Data => data ?? (data = SaveService.LoadJson(FileName, new SolverUsageData()));
        public int RemainingFreeUses => Math.Max(0, DailyFreeUses - Data.dailyFreeUsed);

        public bool TryUseFree(DateTime utcNow)
        {
            ResetIfNewDay(utcNow);
            if (RemainingFreeUses <= 0)
            {
                return false;
            }

            Data.dailyFreeUsed++;
            Save();
            return true;
        }

        public void AddAdBonusUse(DateTime utcNow)
        {
            ResetIfNewDay(utcNow);
            Data.adBonusUses++;
            Save();
        }

        public void AddTicketUse(DateTime utcNow)
        {
            ResetIfNewDay(utcNow);
            Data.ticketUses++;
            Save();
        }

        private void ResetIfNewDay(DateTime utcNow)
        {
            string today = utcNow.ToString("yyyy-MM-dd");
            if (Data.usageDateUtc == today)
            {
                return;
            }

            Data.usageDateUtc = today;
            Data.dailyFreeUsed = 0;
            Data.adBonusUses = 0;
            Data.ticketUses = 0;
        }

        private void Save()
        {
            SaveService.SaveJson(FileName, Data);
        }
    }
}
