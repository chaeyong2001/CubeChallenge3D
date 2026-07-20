using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Save;
using CubeChallenge3D.Stages.Generation;
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

            bool changed = false;
            foreach (IGrouping<StageType, StageData> group in stages
                .Where(stage => stage != null)
                .GroupBy(stage => stage.stageType))
            {
                int maxLocalStageNumber = group.Max(GetLocalStageNumber);
                int maxBlock = (maxLocalStageNumber - 1) / BlockSize;
                for (int block = 0; block <= maxBlock; block++)
                {
                    StageMilestoneReward existingReward = Data.rewards.FirstOrDefault(reward => reward.stageType == group.Key && reward.blockIndex == block);
                    if (existingReward != null)
                    {
                        if (existingReward.requiredStars != EconomyBalanceConfig.BlockRequiredStars
                            || existingReward.rewardGems != EconomyBalanceConfig.BlockRewardGems)
                        {
                            existingReward.requiredStars = EconomyBalanceConfig.BlockRequiredStars;
                            existingReward.rewardGems = EconomyBalanceConfig.BlockRewardGems;
                            changed = true;
                        }

                        continue;
                    }

                    Data.rewards.Add(new StageMilestoneReward
                    {
                        stageType = group.Key,
                        blockIndex = block,
                        startStageNumber = (block * BlockSize) + 1,
                        endStageNumber = (block * BlockSize) + BlockSize,
                        requiredStars = EconomyBalanceConfig.BlockRequiredStars,
                        rewardGems = EconomyBalanceConfig.BlockRewardGems
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                Save();
            }

            return Data.rewards.AsReadOnly();
        }

        public bool TryClaimBlock(int blockIndex, StageType stageType, IReadOnlyList<StageData> stages, StageProgressStore progressStore, WalletStore walletStore)
        {
            if (stages == null || progressStore == null || walletStore == null)
            {
                return false;
            }

            EnsureMilestones(stages);
            StageMilestoneReward reward = Data.rewards.FirstOrDefault(item => item.stageType == stageType && item.blockIndex == blockIndex);
            if (reward == null || reward.claimedByUser)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log($"[BlockRewardClaimStore] block={blockIndex + 1} result=blocked reason={(reward == null ? "missing-reward" : "already-claimed-by-user")} claimedAtUtc={reward?.claimedAtUtc}");
#endif
                return false;
            }

            int totalStars = GetBlockStars(reward, stages, progressStore);
            if (totalStars < reward.requiredStars)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log($"[BlockRewardClaimStore] block={blockIndex + 1} result=blocked reason=not-enough-stars stars={totalStars}/{reward.requiredStars}");
#endif
                return false;
            }

            bool wasAlreadyMarkedClaimed = reward.isClaimed;
            reward.isClaimed = true;
            reward.claimedByUser = true;
            reward.claimedAtUtc = DateTime.UtcNow.ToString("o");
            if (!wasAlreadyMarkedClaimed)
            {
                walletStore.AddGems(reward.rewardGems);
            }
            Save();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log($"[BlockRewardClaimStore] block={blockIndex + 1} result=claimed rewardGems={(wasAlreadyMarkedClaimed ? 0 : reward.rewardGems)} legacyAlreadyPaid={wasAlreadyMarkedClaimed} claimedAtUtc={reward.claimedAtUtc}");
#endif
            return true;
        }

        public int GetBlockStars(StageMilestoneReward reward, IReadOnlyList<StageData> stages, StageProgressStore progressStore)
        {
            if (reward == null || stages == null || progressStore == null)
            {
                return 0;
            }

            return stages
                .Where(stage => stage.stageType == reward.stageType
                    && GetLocalStageNumber(stage) >= reward.startStageNumber
                    && GetLocalStageNumber(stage) <= reward.endStageNumber)
                .Sum(stage => progressStore.GetProgress(stage.stageId).stars);
        }

        private static int GetLocalStageNumber(StageData stage)
        {
            if (stage == null)
            {
                return 0;
            }

            string id = stage.stageId ?? string.Empty;
            int separator = id.LastIndexOf('_');
            if (separator >= 0
                && separator < id.Length - 1
                && int.TryParse(id.Substring(separator + 1), out int parsed)
                && parsed > 0)
            {
                return parsed;
            }

            if (stage.stageType == StageType.ReverseTargetStage)
            {
                return stage.stageNumber - StagePackGenerator.NormalStageCount;
            }

            if (stage.stageType == StageType.InfinityStage)
            {
                return stage.stageNumber - (StagePackGenerator.NormalStageCount + StagePackGenerator.HardStageCount);
            }

            if (stage.stageType == StageType.TutorialStage)
            {
                return stage.stageNumber - StagePackGenerator.TutorialFirstStageNumber + 1;
            }

            return stage.stageNumber;
        }

        public void ClearClaimedForDebug()
        {
            EnsureMilestones(null);
            bool changed = false;
            foreach (StageMilestoneReward reward in Data.rewards)
            {
                if (reward == null || (!reward.isClaimed && !reward.claimedByUser && string.IsNullOrWhiteSpace(reward.claimedAtUtc)))
                {
                    continue;
                }

                reward.isClaimed = false;
                reward.claimedByUser = false;
                reward.claimedAtUtc = null;
                changed = true;
            }

            if (changed)
            {
                Save();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log($"[BlockRewardDebug] resetClaimed={changed}");
#endif
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
