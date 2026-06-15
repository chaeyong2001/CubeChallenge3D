using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Inventory
{
    [Serializable]
    public sealed class InventoryData
    {
        public int saveVersion;
        public int undoItems;
        public int movePlus1Items;
        public int movePlus2Items;
        public int movePlus3Items;
        public int solverTickets;
        public List<string> ownedSkinIds;
        public string selectedSkinId;
        public List<string> ownedThemeIds;
        public string selectedThemeId;
    }
}
