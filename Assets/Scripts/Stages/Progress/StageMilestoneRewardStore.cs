using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Save;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Progress
{
    public sealed class StageMilestoneRewardStore
    {
        private const string FileName = "stage_milestone_rewards.json";
        private const int BlockSize = 10;
        private StageMilestoneRewardData data;

        public StageMilestoneRewardData Data => data ?? (data = Load());

        public IReadOnlyList<StageMilestoneReward> EnsureMilestones(IReadOnlyList<StageData> stages)
        {
            if (Data.rewards == null)
            {
                Data.rewards = new List<StageMilestoneReward>();
            }

            if (stages == null || stages.Count == 0)
            {
                return Data.rewards.AsReadOnly();
            }

            int maxStageNumber = stages.Max(stage => stage.stageNumber);
            int maxBlock = (maxStageNumber - 1) / BlockSize;
            bool changed = false;
            for (int block = 0; block <= maxBlock; block++)
            {
                if (Data.rewards.Any(reward => reward.blockIndex == block))
                {
                    continue;
                }

                Data.rewards.Add(new StageMilestoneReward
                {
                    blockIndex = block,
                    startStageNumber = (block * BlockSize) + 1,
                    endStageNumber = (block * BlockSize) + BlockSize,
                    requiredStars = 30,
                    rewardGems = 5
                });
                changed = true;
            }

            if (changed)
            {
                Save();
            }

            return Data.rewards.AsReadOnly();
        }

        public bool TryClaimBlock(int blockIndex, IReadOnlyList<StageData> stages, StageProgressStore progressStore, WalletStore walletStore)
        {
            if (stages == null || progressStore == null || walletStore == null)
            {
                return false;
            }

            EnsureMilestones(stages);
            StageMilestoneReward reward = Data.rewards.FirstOrDefault(item => item.blockIndex == blockIndex);
            if (reward == null || reward.isClaimed)
            {
                return false;
            }

            int totalStars = GetBlockStars(reward, stages, progressStore);
            if (totalStars < reward.requiredStars)
            {
                return false;
            }

            reward.isClaimed = true;
            reward.claimedAtUtc = DateTime.UtcNow.ToString("o");
            walletStore.AddGems(reward.rewardGems);
            Save();
            return true;
        }

        public int GetBlockStars(StageMilestoneReward reward, IReadOnlyList<StageData> stages, StageProgressStore progressStore)
        {
            if (reward == null || stages == null || progressStore == null)
            {
                return 0;
            }

            return stages
                .Where(stage => stage.stageNumber >= reward.startStageNumber && stage.stageNumber <= reward.endStageNumber)
                .Sum(stage => progressStore.GetProgress(stage.stageId).stars);
        }

        private void Save()
        {
            SaveDataValidator.Normalize(Data);
            SaveService.SaveJson(FileName, Data);
        }

        private static StageMilestoneRewardData Load()
        {
            StageMilestoneRewardData loaded = SaveService.LoadJson(FileName, new StageMilestoneRewardData());
            if (SaveDataValidator.Normalize(loaded))
            {
                SaveService.SaveJson(FileName, loaded);
            }

            return loaded;
        }
    }
}
