using System;
using System.Text;

namespace CubeChallenge3D.Ranking
{
    public static class DailyChallengeSeed
    {
        public static int GetTodaySeed()
        {
            return GetSeedForDate(DateTime.UtcNow);
        }

        public static int GetSeedForDate(DateTime date)
        {
            // UTC date is used so every player gets the same daily challenge boundary.
            string key = date.ToUniversalTime().ToString("yyyy_MM_dd");
            unchecked
            {
                int hash = 17;
                foreach (byte value in Encoding.UTF8.GetBytes(key))
                {
                    hash = (hash * 31) + value;
                }

                return hash & 0x7fffffff;
            }
        }

        public static string GetChallengeIdForDate(DateTime date)
        {
            return $"daily_{date.ToUniversalTime():yyyy_MM_dd}";
        }
    }
}
