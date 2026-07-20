using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CubeChallenge3D.UI.Stages
{
    [Serializable]
    public sealed class StageSlotLayout
    {
        public int slotIndex;
        public Vector2 normalizedPosition;
        public float scale;

        public StageSlotLayout(int slotIndex, Vector2 normalizedPosition, float scale)
        {
            this.slotIndex = slotIndex;
            this.normalizedPosition = normalizedPosition;
            this.scale = scale;
        }
    }

    [Serializable]
    public sealed class StageSlotLayoutConfig
    {
        public List<StageSlotLayout> slots = new List<StageSlotLayout>();
        public bool forceLastSlotAsLockPreview = true;

        public static string SavePath => Path.Combine(Application.persistentDataPath, "stage_slot_layout.json");
        private const string ResourcesLayoutPath = "stage_slot_layout";

        [NonSerialized] public string loadedSource = "unknown";
        [NonSerialized] public string loadedPath = string.Empty;
        [NonSerialized] public bool loadedFileExists;
        [NonSerialized] public bool usingDefaultLayout;

        public static StageSlotLayoutConfig LoadOrDefault()
        {
            string path = SavePath;
            bool exists = File.Exists(path);
            try
            {
                if (exists)
                {
                    StageSlotLayoutConfig loaded = JsonUtility.FromJson<StageSlotLayoutConfig>(File.ReadAllText(path));
                    if (loaded != null && loaded.slots != null && loaded.slots.Count >= 8)
                    {
                        loaded.NormalizeSlotOrder();
                        loaded.loadedSource = "persistent json";
                        loaded.loadedPath = path;
                        loaded.loadedFileExists = true;
                        loaded.usingDefaultLayout = false;
                        return loaded;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[StageSlotLayout] Failed to load saved layout. Trying Resources fallback. {exception.Message}");
            }

            try
            {
                TextAsset resource = Resources.Load<TextAsset>(ResourcesLayoutPath);
                if (resource != null)
                {
                    StageSlotLayoutConfig loaded = JsonUtility.FromJson<StageSlotLayoutConfig>(resource.text);
                    if (loaded != null && loaded.slots != null && loaded.slots.Count >= 8)
                    {
                        loaded.NormalizeSlotOrder();
                        loaded.loadedSource = "resources";
                        loaded.loadedPath = $"Resources/{ResourcesLayoutPath}.json";
                        loaded.loadedFileExists = exists;
                        loaded.usingDefaultLayout = false;
                        return loaded;
                    }

                    Debug.LogWarning("[StageSlotLayout] Resources layout exists but could not be parsed. Using defaults.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[StageSlotLayout] Failed to load Resources layout. Using defaults. {exception.Message}");
            }

            StageSlotLayoutConfig fallback = CreateDefault();
            fallback.loadedSource = "default fallback";
            fallback.loadedPath = path;
            fallback.loadedFileExists = exists;
            fallback.usingDefaultLayout = true;
            return fallback;
        }

        public static StageSlotLayoutConfig CreateDefault()
        {
            return new StageSlotLayoutConfig
            {
                forceLastSlotAsLockPreview = true,
                slots = new List<StageSlotLayout>
                {
                    new StageSlotLayout(0, new Vector2(0.50f, 0.185f), 1.15f),
                    new StageSlotLayout(1, new Vector2(0.46f, 0.340f), 1.05f),
                    new StageSlotLayout(2, new Vector2(0.43f, 0.465f), 0.98f),
                    new StageSlotLayout(3, new Vector2(0.52f, 0.575f), 0.90f),
                    new StageSlotLayout(4, new Vector2(0.47f, 0.675f), 0.82f),
                    new StageSlotLayout(5, new Vector2(0.60f, 0.760f), 0.74f),
                    new StageSlotLayout(6, new Vector2(0.49f, 0.835f), 0.68f),
                    new StageSlotLayout(7, new Vector2(0.62f, 0.875f), 0.62f)
                }
            };
        }

        public StageSlotLayout GetSlot(int index)
        {
            NormalizeSlotOrder();
            return slots[Mathf.Clamp(index, 0, slots.Count - 1)];
        }

        public void Save()
        {
            NormalizeSlotOrder();
            File.WriteAllText(SavePath, JsonUtility.ToJson(this, true));
            Debug.Log($"[StageSlotLayout] Saved layout: {SavePath}");
            Debug.Log(ToCSharpArrayLog());
        }

        public string ToCSharpArrayLog()
        {
            NormalizeSlotOrder();
            List<string> lines = new List<string>
            {
                "[StageSlotLayout] Copy layout:",
                "new StageSlotLayoutConfig",
                "{",
                $"    forceLastSlotAsLockPreview = {forceLastSlotAsLockPreview.ToString().ToLowerInvariant()},",
                "    slots = new List<StageSlotLayout>",
                "    {"
            };

            foreach (StageSlotLayout slot in slots)
            {
                lines.Add($"        new StageSlotLayout({slot.slotIndex}, new Vector2({slot.normalizedPosition.x:0.0000}f, {slot.normalizedPosition.y:0.0000}f), {slot.scale:0.0000}f),");
            }

            lines.Add("    }");
            lines.Add("};");
            return string.Join("\n", lines);
        }

        private void NormalizeSlotOrder()
        {
            if (slots == null)
            {
                slots = new List<StageSlotLayout>();
            }

            slots.Sort((left, right) => left.slotIndex.CompareTo(right.slotIndex));
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].slotIndex = i;
                slots[i].normalizedPosition = new Vector2(
                    Mathf.Clamp01(slots[i].normalizedPosition.x),
                    Mathf.Clamp01(slots[i].normalizedPosition.y));
                slots[i].scale = Mathf.Clamp(slots[i].scale, 0.35f, 1.6f);
            }

            while (slots.Count < 8)
            {
                StageSlotLayout fallback = CreateDefault().slots[slots.Count];
                slots.Add(fallback);
            }
        }
    }
}
