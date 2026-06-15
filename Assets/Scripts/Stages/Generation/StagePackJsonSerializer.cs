using System.Text;
using CubeChallenge3D.Stages.Model;

namespace CubeChallenge3D.Stages.Generation
{
    public static class StagePackJsonSerializer
    {
        public static string ToJson(StageDataCollection collection)
        {
            var builder = new StringBuilder(256 * 1024);
            builder.AppendLine("{");
            builder.AppendLine("  \"stages\": [");
            if (collection?.stages != null)
            {
                for (int i = 0; i < collection.stages.Count; i++)
                {
                    AppendStage(builder, collection.stages[i], i < collection.stages.Count - 1);
                }
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendStage(StringBuilder builder, StageData stage, bool hasNext)
        {
            builder.AppendLine("    {");
            Append(builder, "stageId", stage.stageId);
            Append(builder, "stageNumber", stage.stageNumber);
            Append(builder, "stageType", (int)stage.stageType);
            Append(builder, "difficulty", (int)stage.difficulty);
            Append(builder, "title", stage.title);
            Append(builder, "description", stage.description);
            Append(builder, "startStateFacelets", stage.startStateFacelets);
            Append(builder, "targetStateFacelets", stage.targetStateFacelets);
            Append(builder, "scrambleNotation", stage.scrambleNotation);
            Append(builder, "solutionNotation", stage.solutionNotation);
            Append(builder, "generatedSeed", stage.generatedSeed);
            Append(builder, "generationGroup", stage.generationGroup);
            Append(builder, "minimumMoves", stage.minimumMoves);
            Append(builder, "minMoveCount", stage.minMoveCount);
            Append(builder, "moveLimit", stage.moveLimit);
            Append(builder, "starMoveLimit1", stage.starMoveLimit1);
            Append(builder, "starMoveLimit2", stage.starMoveLimit2);
            Append(builder, "starMoveLimit3", stage.starMoveLimit3);
            Append(builder, "isUnlockedByDefault", stage.isUnlockedByDefault);
            Append(builder, "unlockAfterStageId", stage.unlockAfterStageId);
            Append(builder, "rewardCoins", stage.rewardCoins, false);
            builder.Append("    }");
            builder.AppendLine(hasNext ? "," : string.Empty);
        }

        private static void Append(StringBuilder builder, string name, string value, bool comma = true)
        {
            builder.Append("      \"").Append(name).Append("\": \"")
                .Append(Escape(value)).Append('"');
            builder.AppendLine(comma ? "," : string.Empty);
        }

        private static void Append(StringBuilder builder, string name, int value, bool comma = true)
        {
            builder.Append("      \"").Append(name).Append("\": ").Append(value);
            builder.AppendLine(comma ? "," : string.Empty);
        }

        private static void Append(StringBuilder builder, string name, bool value, bool comma = true)
        {
            builder.Append("      \"").Append(name).Append("\": ").Append(value ? "true" : "false");
            builder.AppendLine(comma ? "," : string.Empty);
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
