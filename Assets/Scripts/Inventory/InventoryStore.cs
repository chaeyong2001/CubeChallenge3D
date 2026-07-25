using CubeChallenge3D.Save;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.Economy;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeChallenge3D.Inventory
{
    public sealed class InventoryStore
    {
        private const string FileName = "inventory.json";
        private static int globalVersion;
        private InventoryData inventory;
        private int loadedVersion = -1;

        public static event Action Changed;
        public static event Action<StageAssistItemType, int> ItemCountChanged;

        public int UndoItems => GetCount(StageAssistItemType.Undo);
        public int MovePlus1Items => GetCount(StageAssistItemType.MovePlus1);
        public int MovePlus2Items => GetCount(StageAssistItemType.MovePlus2);
        public int MovePlus3Items => GetCount(StageAssistItemType.MovePlus3);
        public int SolverTickets => GetCount(StageAssistItemType.SolverTicket);
        public InventoryData Data
        {
            get
            {
                if (inventory == null || loadedVersion != globalVersion)
                {
                    inventory = Load();
                    loadedVersion = globalVersion;
                }

                return inventory;
            }
        }

        public void Reload()
        {
            inventory = Load();
            loadedVersion = globalVersion;
        }

        public bool TryConsume(StageAssistItemType itemType)
        {
            int before = GetCount(itemType);
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    if (Data.undoItems <= 0)
                    {
                        return false;
                    }

                    Data.undoItems--;
                    Save(itemType);
                    LogConsume(itemType, before);
                    return true;
                case StageAssistItemType.MovePlus1:
                    if (Data.movePlus1Items <= 0)
                    {
                        return false;
                    }

                    Data.movePlus1Items--;
                    Save(itemType);
                    LogConsume(itemType, before);
                    return true;
                case StageAssistItemType.MovePlus2:
                    if (Data.movePlus2Items <= 0)
                    {
                        return false;
                    }

                    Data.movePlus2Items--;
                    Save(itemType);
                    LogConsume(itemType, before);
                    return true;
                case StageAssistItemType.MovePlus3:
                    if (Data.movePlus3Items <= 0)
                    {
                        return false;
                    }

                    Data.movePlus3Items--;
                    Save(itemType);
                    LogConsume(itemType, before);
                    return true;
                case StageAssistItemType.SolverTicket:
                    if (Data.solverTickets <= 0)
                    {
                        return false;
                    }

                    Data.solverTickets--;
                    Save(itemType);
                    LogConsume(itemType, before);
                    return true;
                default:
                    return false;
            }
        }

        public int GetCount(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    return Data.undoItems;
                case StageAssistItemType.MovePlus1:
                    return Data.movePlus1Items;
                case StageAssistItemType.MovePlus2:
                    return Data.movePlus2Items;
                case StageAssistItemType.MovePlus3:
                    return Data.movePlus3Items;
                case StageAssistItemType.SolverTicket:
                    return Data.solverTickets;
                default:
                    return 0;
            }
        }

        public void Add(StageAssistItemType itemType, int count)
        {
            if (count <= 0)
            {
                return;
            }

            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    Data.undoItems = SafeAdd(Data.undoItems, count);
                    break;
                case StageAssistItemType.MovePlus1:
                    Data.movePlus1Items = SafeAdd(Data.movePlus1Items, count);
                    break;
                case StageAssistItemType.MovePlus2:
                    Data.movePlus2Items = SafeAdd(Data.movePlus2Items, count);
                    break;
                case StageAssistItemType.MovePlus3:
                    Data.movePlus3Items = SafeAdd(Data.movePlus3Items, count);
                    break;
                case StageAssistItemType.SolverTicket:
                    Data.solverTickets = SafeAdd(Data.solverTickets, count);
                    break;
            }

            Save(itemType);
        }

        public bool OwnsSkin(string skinId) => Data.ownedSkinIds.Contains(skinId);
        public bool OwnsTheme(string themeId) => Data.ownedThemeIds.Contains(themeId);

        public bool UnlockSkin(string skinId)
        {
            if (string.IsNullOrWhiteSpace(skinId) || OwnsSkin(skinId))
            {
                return false;
            }

            Data.ownedSkinIds.Add(skinId);
            Save();
            return true;
        }

        public bool UnlockTheme(string themeId)
        {
            if (string.IsNullOrWhiteSpace(themeId) || OwnsTheme(themeId))
            {
                return false;
            }

            Data.ownedThemeIds.Add(themeId);
            Save();
            return true;
        }

        public bool EquipSkin(string skinId)
        {
            if (!OwnsSkin(skinId))
            {
                return false;
            }

            Data.selectedSkinId = skinId;
            Save();
            return true;
        }

        public bool EquipTheme(string themeId)
        {
            if (!OwnsTheme(themeId))
            {
                return false;
            }

            Data.selectedThemeId = themeId;
            Save();
            return true;
        }

        public void ResetVisualCustomizations()
        {
            Data.ownedSkinIds = new List<string> { "classic", "soft_pastel" };
            Data.selectedSkinId = "classic";
            Data.ownedThemeIds = new List<string> { "default", "minimal_white" };
            Data.selectedThemeId = "default";
            Save();
        }

        private static InventoryData Load()
        {
            InventoryData loaded = SaveService.LoadJson(FileName, new InventoryData
            {
                undoItems = 5,
                movePlus1Items = 3,
                movePlus2Items = 2,
                movePlus3Items = 1,
                solverTickets = 1,
                ownedSkinIds = new List<string> { "classic", "soft_pastel" },
                selectedSkinId = "classic",
                ownedThemeIds = new List<string> { "default", "minimal_white" },
                selectedThemeId = "default"
            });
            if (SaveDataValidator.Normalize(loaded))
            {
                SaveService.SaveJson(FileName, loaded);
            }
            return loaded;
        }

        private void Save(StageAssistItemType? changedItemType = null)
        {
            SaveDataValidator.Normalize(Data);
            SaveService.SaveJson(FileName, Data);
            globalVersion++;
            loadedVersion = globalVersion;
            if (changedItemType.HasValue)
            {
                int count = GetCount(changedItemType.Value);
                ItemCountChanged?.Invoke(changedItemType.Value, count);
                Debug.Log($"[Inventory] Item changed itemId={changedItemType.Value} count={count}");
            }

            Changed?.Invoke();
            Debug.Log("[Inventory] InventoryChanged event raised");
        }

        private static int SafeAdd(int current, int amount)
        {
            long result = (long)current + amount;
            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private void LogConsume(StageAssistItemType itemType, int before)
        {
            Debug.Log($"[Inventory] Consume item itemId={itemType} before={before} after={GetCount(itemType)}");
        }
    }
}
