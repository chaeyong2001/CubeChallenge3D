using System;
using CubeChallenge3D.Save;
using UnityEngine;

namespace CubeChallenge3D.Economy
{
    public sealed class WalletStore
    {
        private const string FileName = "economy_wallet.json";
        public const int MaxNaturalHearts = 10;
        public const int HeartRegenIntervalSeconds = 120;
        private EconomyWallet wallet;

        public static event Action Changed;
        public static event Action<string, int> CurrencyChanged;

        public int Coins => ReloadAndRefresh().coins;
        public int Gems => ReloadAndRefresh().gems;
        public int Hearts => ReloadAndRefresh().hearts;
        public int SecondsUntilNextHeart
        {
            get
            {
                EconomyWallet data = ReloadAndRefresh();
                if (data.hearts >= MaxNaturalHearts)
                {
                    return 0;
                }

                DateTime last = ParseUtc(data.lastHeartRegenTimeUtc, DateTime.UtcNow);
                double elapsed = Math.Max(0d, (DateTime.UtcNow - last).TotalSeconds);
                return Math.Max(1, HeartRegenIntervalSeconds - (int)Math.Floor(elapsed));
            }
        }
        public EconomyWallet Data => wallet ?? (wallet = Load());

        public void AddCoins(int amount)
        {
            EconomyWallet data = ReloadAndRefresh();
            data.coins = ClampCurrency((long)data.coins + amount);
            Save("coins", data.coins);
        }

        public void AddGems(int amount)
        {
            EconomyWallet data = ReloadAndRefresh();
            data.gems = ClampCurrency((long)data.gems + amount);
            Save("gems", data.gems);
        }

        public void AddHearts(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EconomyWallet data = ReloadAndRefresh();
            data.hearts = ClampCurrency((long)data.hearts + amount);
            if (data.hearts >= MaxNaturalHearts)
            {
                data.lastHeartRegenTimeUtc = DateTime.UtcNow.ToString("o");
            }
            Save("hearts", data.hearts);
        }

        public bool SpendCoins(int amount)
        {
            EconomyWallet data = ReloadAndRefresh();
            if (amount < 0 || data.coins < amount)
            {
                return false;
            }

            data.coins -= amount;
            Save("coins", data.coins);
            return true;
        }

        public bool SpendGems(int amount)
        {
            EconomyWallet data = ReloadAndRefresh();
            if (amount < 0 || data.gems < amount)
            {
                return false;
            }

            data.gems -= amount;
            Save("gems", data.gems);
            return true;
        }

        public bool TrySpendHeart()
        {
            EconomyWallet data = ReloadAndRefresh();
            if (data.hearts <= 0)
            {
                return false;
            }

            bool wasAtOrAboveNaturalCap = data.hearts >= MaxNaturalHearts;
            data.hearts--;
            if (wasAtOrAboveNaturalCap && data.hearts < MaxNaturalHearts)
            {
                data.lastHeartRegenTimeUtc = DateTime.UtcNow.ToString("o");
            }
            Save("hearts", data.hearts);
            return true;
        }

        public bool RefreshHearts()
        {
            wallet = LoadRaw();
            bool changed = ApplyHeartRegen(wallet, DateTime.UtcNow);
            if (changed)
            {
                Save("hearts", wallet.hearts);
            }
            return changed;
        }

        private static EconomyWallet Load()
        {
            EconomyWallet loaded = LoadRaw();
            if (SaveDataValidator.Normalize(loaded))
            {
                SaveService.SaveJson(FileName, loaded);
            }
            if (ApplyHeartRegen(loaded, DateTime.UtcNow))
            {
                SaveService.SaveJson(FileName, loaded);
            }

            return loaded;
        }

        private static EconomyWallet LoadRaw()
        {
            return SaveService.LoadJson(
                FileName,
                new EconomyWallet
                {
                    coins = 500,
                    gems = 30,
                    hearts = MaxNaturalHearts,
                    lastHeartRegenTimeUtc = DateTime.UtcNow.ToString("o")
                });
        }

        private EconomyWallet ReloadAndRefresh()
        {
            wallet = Load();
            return wallet;
        }

        private void Save(string currencyId = null, int value = 0)
        {
            SaveDataValidator.Normalize(Data);
            SaveService.SaveJson(FileName, Data);
            if (!string.IsNullOrWhiteSpace(currencyId))
            {
                CurrencyChanged?.Invoke(currencyId, value);
                Debug.Log($"[Wallet] Currency changed currency={currencyId} count={value}");
            }

            Changed?.Invoke();
            Debug.Log("[Wallet] WalletChanged event raised");
        }

        private static bool ApplyHeartRegen(EconomyWallet data, DateTime utcNow)
        {
            if (data == null)
            {
                return false;
            }

            DateTime last = ParseUtc(data.lastHeartRegenTimeUtc, utcNow);
            if (data.hearts >= MaxNaturalHearts)
            {
                return false;
            }

            double elapsed = Math.Max(0d, (utcNow - last).TotalSeconds);
            int recovered = (int)Math.Floor(elapsed / HeartRegenIntervalSeconds);
            if (recovered <= 0)
            {
                return false;
            }

            data.hearts = Math.Min(MaxNaturalHearts, data.hearts + recovered);
            data.lastHeartRegenTimeUtc = data.hearts >= MaxNaturalHearts
                ? utcNow.ToString("o")
                : last.AddSeconds(recovered * HeartRegenIntervalSeconds).ToString("o");
            return true;
        }

        private static DateTime ParseUtc(string value, DateTime fallback)
        {
            return DateTime.TryParse(value, out DateTime parsed)
                ? parsed.ToUniversalTime()
                : fallback;
        }

        private static int ClampCurrency(long value)
        {
            return value <= 0L ? 0 : value >= int.MaxValue ? int.MaxValue : (int)value;
        }
    }
}
