using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Learn.Storage;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Stages.Progress;
using UnityEngine;

namespace CubeChallenge3D.Save
{
    public static class SaveMigrationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MigrateLocalSaves()
        {
            // Loading each store runs its idempotent validator and persists only
            // when an old or malformed value needs normalization.
            _ = new WalletStore().Data;
            _ = new InventoryStore().Data;
            _ = new StageProgressStore();
            _ = new StageMilestoneRewardStore().Data;
            _ = new LearnLessonProgressStore();
            _ = new DailyRewardStore().Data;
            _ = new AdsRewardLimitStore().DailyCoinsAdCount;
            _ = new QuickPlayRecordStore();
            _ = new LocalRankingStore();
            _ = new CachedRankingStore();
            _ = new PendingRankingSubmissionStore();
            _ = new SettingsStore();
        }
    }
}
