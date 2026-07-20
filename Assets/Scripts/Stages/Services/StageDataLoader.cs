using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Stages.Generation;
using CubeChallenge3D.Stages.Model;
using UnityEngine;

namespace CubeChallenge3D.Stages.Services
{
    public sealed class StageDataLoader
    {
        private const string GeneratedResourcePath = "Stages/stages_generated";
        private const string SampleResourcePath = "Stages/stages_sample";

        private static List<StageData> sharedStages;
        private static bool sharedLoadAttempted;
        private List<StageData> cachedStages;

        public IReadOnlyList<StageData> LoadAllStages()
        {
            if (cachedStages != null)
            {
                return cachedStages.AsReadOnly();
            }

            if (sharedLoadAttempted && sharedStages != null)
            {
                cachedStages = sharedStages;
                return cachedStages.AsReadOnly();
            }

            sharedLoadAttempted = true;
            cachedStages = new List<StageData>();
            TextAsset asset = Resources.Load<TextAsset>(GeneratedResourcePath);
            string resourcePath = GeneratedResourcePath;
            if (asset == null)
            {
                asset = Resources.Load<TextAsset>(SampleResourcePath);
                resourcePath = SampleResourcePath;
            }

            if (asset == null)
            {
                Debug.LogWarning($"Stage data not found in Resources/{GeneratedResourcePath}.json or {SampleResourcePath}.json");
                return cachedStages.AsReadOnly();
            }

            try
            {
                StageDataCollection collection = JsonUtility.FromJson<StageDataCollection>(asset.text);
                if (collection?.stages != null)
                {
                    cachedStages = collection.stages
                        .Where(stage => stage != null)
                        .OrderBy(stage => stage.stageNumber)
                        .ToList();
                }

                EnsureCanonicalStagePack();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Stage data parse failed: {exception.Message}");
                cachedStages.Clear();
            }

            StageValidationResult result = StageDataValidator.ValidateAll(cachedStages);
            Debug.Log(result.isValid
                ? $"Loaded stages: {cachedStages.Count} from {resourcePath}. Stage validation: OK"
                : $"Loaded stages: {cachedStages.Count} from {resourcePath}. Stage validation warnings: {string.Join(" | ", result.messages)}");
            sharedStages = cachedStages;
            return cachedStages.AsReadOnly();
        }

        public StageData GetStageById(string stageId)
        {
            return LoadAllStages().FirstOrDefault(stage => stage.stageId == stageId);
        }

        public IReadOnlyList<StageData> GetStagesByType(StageType type)
        {
            return LoadAllStages()
                .Where(stage => stage.stageType == type)
                .ToList()
                .AsReadOnly();
        }

        private void EnsureCanonicalStagePack()
        {
            int solveCount = cachedStages.Count(stage => stage.stageType == StageType.SolveStage);
            int targetCount = cachedStages.Count(stage => stage.stageType == StageType.ReverseTargetStage);
            int infinityCount = cachedStages.Count(stage => stage.stageType == StageType.InfinityStage);
            int tutorialCount = cachedStages.Count(stage => stage.stageType == StageType.TutorialStage);
            bool baseModesCanonical = solveCount == StagePackGenerator.NormalStageCount
                && targetCount == StagePackGenerator.HardStageCount
                && infinityCount == StagePackGenerator.InfinityStageCount;
            bool tutorialCanonical = tutorialCount == StagePackGenerator.TutorialStageCount;
            if (baseModesCanonical && tutorialCanonical)
            {
                return;
            }

            var generator = new StagePackGenerator();
            cachedStages.RemoveAll(stage => stage.stageType == StageType.TutorialStage);
            cachedStages.AddRange(generator.GenerateTutorialStages(StagePackGenerator.TutorialStageCount, StagePackGenerator.DefaultTutorialSeed));
            if (!baseModesCanonical)
            {
                cachedStages.RemoveAll(stage =>
                    stage.stageType == StageType.SolveStage
                    || stage.stageType == StageType.ReverseTargetStage
                    || stage.stageType == StageType.InfinityStage);
                cachedStages.AddRange(generator.GenerateSolveStages(StagePackGenerator.NormalStageCount, StagePackGenerator.DefaultSolveSeed));
                cachedStages.AddRange(generator.GenerateReverseTargetStages(StagePackGenerator.HardStageCount, StagePackGenerator.DefaultTargetSeed));
                cachedStages.AddRange(generator.GenerateInfinityStages(StagePackGenerator.InfinityStageCount, StagePackGenerator.DefaultInfinitySeed));
            }
            cachedStages = cachedStages
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            Debug.Log($"Generated runtime canonical stages: tutorial={StagePackGenerator.TutorialStageCount}, normal={StagePackGenerator.NormalStageCount}, hard={StagePackGenerator.HardStageCount}, infinity={StagePackGenerator.InfinityStageCount}");
        }
    }
}
