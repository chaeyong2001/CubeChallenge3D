#if UNITY_EDITOR
using System.IO;
using CubeChallenge3D.Stages.Generation;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Services;
using CubeChallenge3D.Stages.Validation;
using UnityEditor;
using UnityEngine;

namespace CubeChallenge3D.Editor.StageGeneration
{
    public static class StagePackGeneratorMenu
    {
        private const string OutputPath = "Assets/Resources/Stages/stages_generated.json";

        [MenuItem("Tools/CubeChallenge3D/Generate Stage Pack")]
        public static void GenerateStagePack()
        {
            try
            {
                StageDataCollection collection = new StagePackGenerator().GenerateFullStagePack();
                StageValidationResult validation = StagePackValidator.Validate(collection);
                if (!validation.isValid)
                {
                    Debug.LogError($"Stage pack generation failed validation: {string.Join(" | ", validation.messages)}");
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, StagePackJsonSerializer.ToJson(collection));
                AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Generated {collection.stages.Count} stages at {OutputPath}. Validation: OK");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Stage pack generation failed: {exception}");
            }
        }

        [MenuItem("Tools/CubeChallenge3D/Validate Stage Pack")]
        public static void ValidateStagePack()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(OutputPath);
            if (asset == null)
            {
                Debug.LogError($"Generated stage pack not found: {OutputPath}");
                return;
            }

            StageDataCollection collection = JsonUtility.FromJson<StageDataCollection>(asset.text);
            StageValidationResult validation = StagePackValidator.Validate(collection);
            Debug.Log(validation.isValid
                ? $"Stage pack validation: OK ({collection.stages.Count} stages)"
                : $"Stage pack validation failed: {string.Join(" | ", validation.messages)}");
        }

        [MenuItem("Tools/CubeChallenge3D/Validate Stage Balance")]
        public static void ValidateStageBalance()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(OutputPath);
            if (asset == null)
            {
                Debug.LogError($"Generated stage pack not found: {OutputPath}");
                return;
            }

            StageDataCollection collection = JsonUtility.FromJson<StageDataCollection>(asset.text);
            StageValidationResult validation = StageBalanceValidator.Validate(collection?.stages);
            Debug.Log(validation.isValid
                ? $"Stage balance validation: OK ({collection.stages.Count} stages)"
                : $"Stage balance validation failed: {string.Join(" | ", validation.messages)}");
        }
    }
}
#endif
