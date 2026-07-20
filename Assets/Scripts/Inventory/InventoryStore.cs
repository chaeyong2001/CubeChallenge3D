using CubeChallenge3D.Save;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.Economy;
using System.Collections.Generic;

namespace CubeChallenge3D.Inventory
{
    public sealed class InventoryStore
    {
        private const string FileName = "inventory.json";
        private InventoryData inventory;

        public int UndoItems => Data.undoItems;
        public int MovePlus1Items => Data.movePlus1Items;
        public int MovePlus2Items => Data.movePlus2Items;
        public int MovePlus3Items => Data.movePlus3Items;
        public int SolverTickets => Data.solverTickets;
        public InventoryData Data => inventory ?? (inventory = Load());

        public void Reload()
        {
            inventory = Load();
        }

        public bool TryConsume(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.Undo:
                    if (Data.undoItems <= 0)
                    {
                        return false;
                    }

                    Data.undoItems--;
                    Save();
                    return true;
                case StageAssistItemType.MovePlus1:
                    if (Data.movePlus1Items <= 0)
                    {
                        return false;
                    }

                    Data.movePlus1Items--;
                    Save();
                    return true;
                case StageAssistItemType.MovePlus2:
                    if (Data.movePlus2Items <= 0)
                    {
                        return false;
                    }

                    Data.movePlus2Items--;
                    Save();
                    return true;
                case StageAssistItemType.MovePlus3:
                    if (Data.movePlus3Items <= 0)
                    {
                        return false;
                    }

                    Data.movePlus3Items--;
                    Save();
                    return true;
                case StageAssistItemType.SolverTicket:
                    if (Data.solverTickets <= 0)
                    {
                        return false;
                    }

                    Data.solverTickets--;
                    Save();
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

            Save();
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

        private void Save()
        {
            SaveDataValidator.Normalize(Data);
            SaveService.SaveJson(FileName, Data);
        }

        private static int SafeAdd(int current, int amount)
        {
            long result = (long)current + amount;
            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }
    }
}
