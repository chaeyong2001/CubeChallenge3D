using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Stages.Model;
using UnityEngine;

namespace CubeChallenge3D.Stages.Services
{
    public sealed class StageDataLoader
    {
        private const string GeneratedResourcePath = "Stages/stages_generated";
        private const string SampleResourcePath = "Stages/stages_sample";

        private List<StageData> cachedStages;

        public IReadOnlyList<StageData> LoadAllStages()
        {
            if (cachedStages != null)
            {
                return cachedStages.AsReadOnly();
            }

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
    }
}
