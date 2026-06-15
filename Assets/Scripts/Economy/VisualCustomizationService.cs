using System;
using CubeChallenge3D.Inventory;

namespace CubeChallenge3D.Economy
{
    public sealed class VisualCustomizationService
    {
        private readonly WalletStore wallet;
        private readonly InventoryStore inventory;

        public VisualCustomizationService(WalletStore walletStore = null, InventoryStore inventoryStore = null)
        {
            wallet = walletStore ?? new WalletStore();
            inventory = inventoryStore ?? new InventoryStore();
        }

        public CubeSkinData SelectedSkin => VisualCustomizationCatalog.GetSkin(inventory.Data.selectedSkinId);
        public ThemeData SelectedTheme => VisualCustomizationCatalog.GetTheme(inventory.Data.selectedThemeId);
        public static CubeSkinData LoadSelectedSkin()
        {
            InventoryStore store = new InventoryStore();
            return VisualCustomizationCatalog.GetSkin(store.Data.selectedSkinId);
        }

        public static ThemeData LoadSelectedTheme()
        {
            InventoryStore store = new InventoryStore();
            return VisualCustomizationCatalog.GetTheme(store.Data.selectedThemeId);
        }
        public bool OwnsSkin(string id) => inventory.OwnsSkin(id);
        public bool OwnsTheme(string id) => inventory.OwnsTheme(id);

        public bool TryBuySkin(CubeSkinData skin, out string message)
        {
            if (skin == null) { message = "Skin is unavailable."; return false; }
            if (OwnsSkin(skin.skinId)) { message = "Already owned."; return false; }
            if (!wallet.SpendGems(skin.priceGems)) { message = "Not enough gems."; return false; }
            inventory.UnlockSkin(skin.skinId);
            message = "Purchased.";
            return true;
        }

        public bool TryBuyTheme(ThemeData theme, out string message)
        {
            if (theme == null) { message = "Theme is unavailable."; return false; }
            if (OwnsTheme(theme.themeId)) { message = "Already owned."; return false; }
            if (!wallet.SpendGems(theme.priceGems)) { message = "Not enough gems."; return false; }
            inventory.UnlockTheme(theme.themeId);
            message = "Purchased.";
            return true;
        }

        public bool EquipSkin(string id, out string message)
        {
            bool equipped = inventory.EquipSkin(id);
            message = equipped ? "Equipped." : "Purchase this skin first.";
            return equipped;
        }

        public bool EquipTheme(string id, out string message)
        {
            bool equipped = inventory.EquipTheme(id);
            message = equipped ? "Equipped." : "Purchase this theme first.";
            return equipped;
        }
    }
}
