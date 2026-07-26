using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Profile;
using UnityEngine;
using UnityEngine.Purchasing;

namespace CubeChallenge3D.IAP
{
    public sealed class PromotionPurchaseService
    {
        private const string FileName = "iap_purchases.json";
        private const string PurchaseUnavailableMessage = "Purchase is not available yet.\nPlease try again after updating from Google Play.";
        private const string ProductsUnavailableMessage = "Products are not available.\nPlease try again later.";
        private const string PackageName = "com.FAMLEE.CubeChallenge3D";

        private readonly WalletStore walletStore;
        private readonly PlayerProfileStore profileStore;
        private readonly IapServerApiClient iapApiClient;
        private readonly Dictionary<string, Product> fetchedProducts = new Dictionary<string, Product>();
        private PromotionPurchaseData purchaseData;
        private StoreController storeController;
        private bool isInitializing;
        private bool isStoreConnected;
        private bool productsFetched;
        private string unavailableMessage = PurchaseUnavailableMessage;

        public event Action<string> StateChanged;

        public PromotionPurchaseService(WalletStore wallet, PlayerProfileStore profile = null)
        {
            walletStore = wallet ?? new WalletStore();
            profileStore = profile ?? new PlayerProfileStore();
            SettingsStore settingsStore = new SettingsStore();
            iapApiClient = new IapServerApiClient(
                settingsStore.Current.rankingApiBaseUrl,
                settingsStore.Current.rankingRequestTimeoutSeconds);

            if (IsProductOwned(PromotionProductIds.RemoveAds))
            {
                AdManager.Instance.SetRemoveAdsPurchased(true);
            }

#if !UNITY_EDITOR
            WalletStore.GemsSpent += HandleGemsSpent;
            _ = SyncServerEntitlementsAsync();
            InitializeBilling();
#endif
        }

        public bool IsBillingAvailable => isStoreConnected && productsFetched && fetchedProducts.Count > 0;

        public bool CanUseEditorTestPurchases
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public bool CanPurchase(PromotionProductDefinition product)
        {
            if (product == null)
            {
                return false;
            }

            return IsBillingAvailable || CanUseEditorTestPurchases;
        }

        public string GetUnavailableMessage()
        {
            return unavailableMessage;
        }

        public string GetDisplayPrice(PromotionProductDefinition product)
        {
            if (product == null)
            {
                return string.Empty;
            }

            if (fetchedProducts.TryGetValue(product.productId, out Product fetched)
                && fetched?.metadata != null
                && !string.IsNullOrWhiteSpace(fetched.metadata.localizedPriceString))
            {
                return fetched.metadata.localizedPriceString;
            }

            return product.priceText;
        }

        public bool IsProductOwned(string productId)
        {
            PromotionPurchaseData data = Load();
            return data.purchasedNonConsumableProductIds != null
                && data.purchasedNonConsumableProductIds.Contains(productId);
        }

        public PromotionPurchaseResult Purchase(PromotionProductDefinition product)
        {
            if (product == null)
            {
                return PromotionPurchaseResult.Failed("No product selected.");
            }

            if (!product.isConsumable && IsProductOwned(product.productId))
            {
                return PromotionPurchaseResult.Success($"{product.displayName} is already owned.");
            }

            if (!CanPurchase(product))
            {
                Debug.LogWarning($"[IAP] Purchase blocked because billing is unavailable. productId={product.productId}");
                return PromotionPurchaseResult.Failed(GetUnavailableMessage());
            }

#if UNITY_EDITOR
            Debug.Log($"[IAP Editor Test Purchase] productId={product.productId}, price={product.priceText}, itemType={product.itemType}");

            if (string.Equals(product.itemType, "GemPack", StringComparison.OrdinalIgnoreCase))
            {
                int gems = GetGemReward(product.productId);
                if (gems <= 0)
                {
                    return PromotionPurchaseResult.Failed($"Unknown gem pack product: {product.productId}");
                }

                walletStore.AddGems(gems);
                return PromotionPurchaseResult.Success($"Purchased {product.displayName}. +{gems} Gems");
            }

            if (string.Equals(product.itemType, "RemoveAds", StringComparison.OrdinalIgnoreCase))
            {
                MarkNonConsumablePurchased(product.productId);
                AdManager.Instance.SetRemoveAdsPurchased(true);
                return PromotionPurchaseResult.Success("Remove Ads purchased.");
            }

            return PromotionPurchaseResult.Failed($"Unsupported promotion product type: {product.itemType}");
#else
            if (!fetchedProducts.TryGetValue(product.productId, out Product fetched) || fetched == null || !fetched.availableToPurchase)
            {
                return PromotionPurchaseResult.Failed(ProductsUnavailableMessage);
            }

            storeController.PurchaseProduct(fetched);
            return PromotionPurchaseResult.Pending("Opening Google Play purchase...");
#endif
        }

#if !UNITY_EDITOR
        private async void InitializeBilling()
        {
            await InitializeBillingAsync();
        }

        private async Task InitializeBillingAsync()
        {
            if (isInitializing || IsBillingAvailable)
            {
                return;
            }

            isInitializing = true;
            unavailableMessage = PurchaseUnavailableMessage;

            try
            {
                storeController = UnityIAPServices.StoreController();
                storeController.OnStoreDisconnected += OnStoreDisconnected;
                storeController.OnProductsFetched += OnProductsFetched;
                storeController.OnProductsFetchFailed += OnProductsFetchFailed;
                storeController.OnPurchasePending += OnPurchasePending;
                storeController.OnPurchaseFailed += OnPurchaseFailed;
                storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                storeController.OnPurchasesFetched += OnPurchasesFetched;
                storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

                await storeController.Connect();
                isStoreConnected = true;

                storeController.FetchProducts(new List<ProductDefinition>
                {
                    new ProductDefinition(PromotionProductIds.SmallGemPack, ProductType.Consumable),
                    new ProductDefinition(PromotionProductIds.MediumGemPack, ProductType.Consumable),
                    new ProductDefinition(PromotionProductIds.LargeGemPack, ProductType.Consumable),
                    new ProductDefinition(PromotionProductIds.RemoveAds, ProductType.NonConsumable)
                });
            }
            catch (Exception exception)
            {
                unavailableMessage = PurchaseUnavailableMessage;
                Debug.LogWarning($"[IAP] Google Play Billing initialization failed: {exception.Message}");
                NotifyStateChanged(unavailableMessage);
            }
            finally
            {
                isInitializing = false;
            }
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            isStoreConnected = false;
            productsFetched = false;
            fetchedProducts.Clear();
            unavailableMessage = PurchaseUnavailableMessage;
            Debug.LogWarning($"[IAP] Store disconnected: {failure}");
            NotifyStateChanged(unavailableMessage);
        }

        private void OnProductsFetched(List<Product> products)
        {
            fetchedProducts.Clear();

            foreach (Product product in products ?? new List<Product>())
            {
                if (product?.definition == null || string.IsNullOrEmpty(product.definition.id))
                {
                    continue;
                }

                fetchedProducts[product.definition.id] = product;
            }

            productsFetched = fetchedProducts.Count > 0;
            unavailableMessage = productsFetched ? string.Empty : ProductsUnavailableMessage;

            storeController.FetchPurchases();
            _ = SyncServerEntitlementsAsync();
            NotifyStateChanged(productsFetched ? "Products loaded from Google Play." : ProductsUnavailableMessage);
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            productsFetched = false;
            fetchedProducts.Clear();
            unavailableMessage = ProductsUnavailableMessage;
            Debug.LogWarning($"[IAP] Product fetch failed: {failure}");
            NotifyStateChanged(ProductsUnavailableMessage);
        }

        private async void OnPurchasePending(PendingOrder order)
        {
            string productId = GetProductId(order);
            if (string.IsNullOrEmpty(productId))
            {
                NotifyStateChanged("Purchase failed: unknown product.");
                return;
            }

            PromotionPurchaseResult result = await VerifyAndApplyPurchasedProductAsync(productId, order, false);
            if (!result.success)
            {
                NotifyStateChanged(result.message);
                return;
            }

            storeController.ConfirmPurchase(order);
            NotifyStateChanged(result.message);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
            {
                NotifyStateChanged($"Purchase failed: {failed.FailureReason}");
                return;
            }

            NotifyStateChanged("Purchase confirmed.");
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            string productId = GetProductId(failed);
            Debug.LogWarning($"[IAP] Purchase failed. productId={productId}, reason={failed.FailureReason}, details={failed.Details}");
            NotifyStateChanged("Purchase failed. Please try again later.");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            _ = RestoreFetchedPurchasesAsync(orders);
        }

        private async Task RestoreFetchedPurchasesAsync(Orders orders)
        {
            bool restoredRemoveAds = false;
            foreach (ConfirmedOrder order in orders?.ConfirmedOrders ?? Array.Empty<ConfirmedOrder>())
            {
                foreach (string productId in GetProductIds(order))
                {
                    if (productId == PromotionProductIds.RemoveAds)
                    {
                        PromotionPurchaseResult result = await VerifyAndApplyPurchasedProductAsync(productId, order, true);
                        restoredRemoveAds |= result.success;
                    }
                }
            }

            if (restoredRemoveAds)
            {
                NotifyStateChanged("Remove Ads restored.");
            }
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning($"[IAP] Purchases fetch failed: {failure}");
        }

        private PromotionPurchaseResult ApplyPurchasedProduct(string productId, string transactionId, bool restoreOnly)
        {
            if (string.Equals(productId, PromotionProductIds.RemoveAds, StringComparison.Ordinal))
            {
                MarkNonConsumablePurchased(productId);
                AdManager.Instance.SetRemoveAdsPurchased(true);
                return PromotionPurchaseResult.Success(restoreOnly ? "Remove Ads restored." : "Remove Ads purchased.");
            }

            if (restoreOnly)
            {
                return PromotionPurchaseResult.Success("Consumable purchase restore skipped.");
            }

            int gems = GetGemReward(productId);
            if (gems <= 0)
            {
                return PromotionPurchaseResult.Failed($"Unknown gem pack product: {productId}");
            }

            if (WasTransactionProcessed(transactionId))
            {
                return PromotionPurchaseResult.Success("Purchase already applied.");
            }

            walletStore.AddGems(gems);
            MarkTransactionProcessed(transactionId);
            return PromotionPurchaseResult.Success($"+{gems} Gems purchased.");
        }

        private async Task SyncServerEntitlementsAsync()
        {
            if (!iapApiClient.HasServerUrl)
            {
                return;
            }

            PlayerProfile profile = profileStore.Current;
            if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
            {
                return;
            }

            IapEntitlementsResult result = await iapApiClient.GetEntitlementsAsync(profile.profileId);
            if (!result.success || result.profile == null)
            {
                Debug.LogWarning($"[IAP] Entitlement sync failed. message={result.message}");
                return;
            }

            if (result.profile.removeAdsPurchased)
            {
                MarkNonConsumablePurchased(PromotionProductIds.RemoveAds);
                AdManager.Instance.SetRemoveAdsPurchased(true);
            }
            else
            {
                ClearNonConsumablePurchased(PromotionProductIds.RemoveAds);
                AdManager.Instance.SetRemoveAdsPurchased(false);
            }

            Debug.Log($"[IAP] Entitlement sync success profileId={profile.profileId} removeAdsPurchased={result.profile.removeAdsPurchased} refundDebtGems={result.profile.refundDebtGems}");
        }

        private async void HandleGemsSpent(int amount)
        {
            if (amount <= 0 || !iapApiClient.HasServerUrl)
            {
                return;
            }

            PlayerProfile profile = profileStore.Current;
            if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
            {
                return;
            }

            IapGemSpendResult result = await iapApiClient.ReportGemSpendAsync(profile.profileId, amount, "local_gem_spend");
            if (!result.success || result.response == null)
            {
                Debug.LogWarning($"[IAP] Paid gem ledger spend report failed amount={amount} message={result.message}");
                return;
            }

            Debug.Log($"[IAP] Paid gem ledger spend reported amount={amount} paidUsed={result.response.paidGemsUsed} untracked={result.response.untrackedGemsUsed} remainingPaid={result.response.remainingPaidGems}");
        }

        private async Task<PromotionPurchaseResult> VerifyAndApplyPurchasedProductAsync(string productId, Order order, bool restoreOnly)
        {
            if (restoreOnly && productId != PromotionProductIds.RemoveAds)
            {
                return PromotionPurchaseResult.Success("Consumable purchase restore skipped.");
            }

            if (!iapApiClient.HasServerUrl)
            {
                Debug.LogWarning("[IAP] Server verification skipped. reason=Server URL is empty.");
                return PromotionPurchaseResult.Failed("Purchase verification server is not configured.");
            }

            PlayerProfile profile = profileStore.Current;
            if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
            {
                return PromotionPurchaseResult.Failed("Create a profile before purchasing.");
            }

            GoogleReceiptFields receipt = ExtractGoogleReceipt(order);
            string purchaseToken = !string.IsNullOrWhiteSpace(receipt.purchaseToken)
                ? receipt.purchaseToken
                : order?.Info?.TransactionID;
            string orderId = !string.IsNullOrWhiteSpace(receipt.orderId)
                ? receipt.orderId
                : string.Empty;
            string packageName = !string.IsNullOrWhiteSpace(receipt.packageName)
                ? receipt.packageName
                : PackageName;

            if (string.IsNullOrWhiteSpace(purchaseToken))
            {
                return PromotionPurchaseResult.Failed("Purchase token was not found.");
            }

            Debug.Log($"[IAP] Purchase received productId={productId} token={MaskToken(purchaseToken)}");
            Debug.Log($"[IAP] Server verification start productId={productId}");
            IapVerifyResult result = await iapApiClient.VerifyGooglePurchaseAsync(new IapGoogleVerifyRequestDto
            {
                profileId = profile.profileId,
                productId = productId,
                purchaseToken = purchaseToken,
                orderId = orderId,
                packageName = packageName
            });

            if (!result.success || result.response == null || !result.response.success)
            {
                string code = result.errorCode ?? result.response?.errorCode ?? "VERIFY_FAILED";
                Debug.LogWarning($"[IAP] Server verification failed productId={productId} errorCode={code} message={result.message}");
                return PromotionPurchaseResult.Failed("Purchase verification failed. It will retry next time you open the shop.");
            }

            IapGoogleVerifyResponseDto response = result.response;
            if (response.alreadyGranted)
            {
                Debug.Log($"[IAP] Duplicate purchase token. alreadyGranted=true productId={productId}");
                if (response.profile != null && response.profile.removeAdsPurchased)
                {
                    MarkNonConsumablePurchased(PromotionProductIds.RemoveAds);
                    AdManager.Instance.SetRemoveAdsPurchased(true);
                }
                return PromotionPurchaseResult.Success("Purchase already applied.");
            }

            if (response.purchase != null
                && string.Equals(response.purchase.grantedCurrencyType, "gems", StringComparison.OrdinalIgnoreCase)
                && response.purchase.grantedAmount > 0)
            {
                walletStore.AddGems(response.purchase.grantedAmount);
                MarkTransactionProcessed(purchaseToken);
                Debug.Log($"[IAP] Grant success productId={productId} amount={response.purchase.grantedAmount}");
                return PromotionPurchaseResult.Success($"+{response.purchase.grantedAmount} Gems purchased.");
            }

            if (response.profile != null && response.profile.removeAdsPurchased)
            {
                MarkNonConsumablePurchased(productId);
                AdManager.Instance.SetRemoveAdsPurchased(true);
                Debug.Log("[IAP] RemoveAds entitlement granted");
                return PromotionPurchaseResult.Success(restoreOnly ? "Remove Ads restored." : "Remove Ads purchased.");
            }

            return PromotionPurchaseResult.Failed("Purchase was verified but no reward was returned.");
        }

        private static string GetProductId(Order order)
        {
            return GetProductIds(order).FirstOrDefault();
        }

        private static IEnumerable<string> GetProductIds(Order order)
        {
            if (order?.CartOrdered == null)
            {
                yield break;
            }

            foreach (CartItem item in order.CartOrdered.Items())
            {
                string productId = item?.Product?.definition?.id;
                if (!string.IsNullOrEmpty(productId))
                {
                    yield return productId;
                }
            }
        }

        private static GoogleReceiptFields ExtractGoogleReceipt(Order order)
        {
            string receipt = order?.Info?.Receipt;
            if (string.IsNullOrWhiteSpace(receipt))
            {
                return new GoogleReceiptFields();
            }

            try
            {
                UnifiedReceiptDto unified = JsonUtility.FromJson<UnifiedReceiptDto>(receipt);
                string payload = !string.IsNullOrWhiteSpace(unified?.Payload) ? unified.Payload : receipt;
                GooglePayloadDto googlePayload = JsonUtility.FromJson<GooglePayloadDto>(payload);
                string purchaseJson = !string.IsNullOrWhiteSpace(googlePayload?.json) ? googlePayload.json : payload;
                GooglePurchaseJsonDto purchase = JsonUtility.FromJson<GooglePurchaseJsonDto>(purchaseJson);
                return new GoogleReceiptFields
                {
                    orderId = purchase?.orderId ?? string.Empty,
                    packageName = purchase?.packageName ?? string.Empty,
                    productId = purchase?.productId ?? string.Empty,
                    purchaseToken = purchase?.purchaseToken ?? string.Empty
                };
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[IAP] Receipt parse failed: {exception.Message}");
                return new GoogleReceiptFields();
            }
        }

        private static string MaskToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(empty)";
            }

            string trimmed = value.Trim();
            if (trimmed.Length <= 8)
            {
                return "***";
            }

            return $"{trimmed.Substring(0, 4)}***{trimmed.Substring(trimmed.Length - 4)}";
        }

        private bool WasTransactionProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return false;
            }

            PromotionPurchaseData data = Load();
            return data.processedTransactionIds != null && data.processedTransactionIds.Contains(transactionId);
        }

        private void MarkTransactionProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return;
            }

            PromotionPurchaseData data = Load();
            if (data.processedTransactionIds == null)
            {
                data.processedTransactionIds = new List<string>();
            }

            if (!data.processedTransactionIds.Contains(transactionId))
            {
                data.processedTransactionIds.Add(transactionId);
                SaveService.SaveJson(FileName, data);
            }
        }

        private void NotifyStateChanged(string message)
        {
            StateChanged?.Invoke(message);
        }
#else
        private void NotifyStateChanged(string message)
        {
            StateChanged?.Invoke(message);
        }
#endif

        private static int GetGemReward(string productId)
        {
            switch (productId)
            {
                case PromotionProductIds.SmallGemPack:
                    return EconomyBalanceConfig.SmallGemPackGems;
                case PromotionProductIds.MediumGemPack:
                    return EconomyBalanceConfig.MediumGemPackGems;
                case PromotionProductIds.LargeGemPack:
                    return EconomyBalanceConfig.LargeGemPackGems;
                default:
                    return 0;
            }
        }

        private void MarkNonConsumablePurchased(string productId)
        {
            PromotionPurchaseData data = Load();
            if (data.purchasedNonConsumableProductIds == null)
            {
                data.purchasedNonConsumableProductIds = new List<string>();
            }

            if (!data.purchasedNonConsumableProductIds.Contains(productId))
            {
                data.purchasedNonConsumableProductIds.Add(productId);
            }

            SaveService.SaveJson(FileName, data);
        }

        private void ClearNonConsumablePurchased(string productId)
        {
            PromotionPurchaseData data = Load();
            if (data.purchasedNonConsumableProductIds == null)
            {
                return;
            }

            if (data.purchasedNonConsumableProductIds.Remove(productId))
            {
                SaveService.SaveJson(FileName, data);
            }
        }

        private PromotionPurchaseData Load()
        {
            if (purchaseData == null)
            {
                purchaseData = SaveService.LoadJson(
                    FileName,
                    new PromotionPurchaseData
                    {
                        saveVersion = SaveDataValidator.CurrentSaveVersion,
                        purchasedNonConsumableProductIds = new List<string>(),
                        processedTransactionIds = new List<string>()
                    });
            }

            if (purchaseData.purchasedNonConsumableProductIds == null)
            {
                purchaseData.purchasedNonConsumableProductIds = new List<string>();
            }

            if (purchaseData.processedTransactionIds == null)
            {
                purchaseData.processedTransactionIds = new List<string>();
            }

            return purchaseData;
        }
    }

    public static class PromotionProductIds
    {
        public const string SmallGemPack = "gem_pack_small";
        public const string MediumGemPack = "gem_pack_medium";
        public const string LargeGemPack = "gem_pack_large";
        public const string RemoveAds = "remove_ads";
    }

    [Serializable]
    public sealed class PromotionPurchaseData
    {
        public int saveVersion;
        public List<string> purchasedNonConsumableProductIds;
        public List<string> processedTransactionIds;
    }

    [Serializable]
    internal sealed class UnifiedReceiptDto
    {
        public string Store = string.Empty;
        public string TransactionID = string.Empty;
        public string Payload = string.Empty;
    }

    [Serializable]
    internal sealed class GooglePayloadDto
    {
        public string json = string.Empty;
        public string signature = string.Empty;
    }

    [Serializable]
    internal sealed class GooglePurchaseJsonDto
    {
        public string orderId = string.Empty;
        public string packageName = string.Empty;
        public string productId = string.Empty;
        public string purchaseToken = string.Empty;
    }

    internal sealed class GoogleReceiptFields
    {
        public string orderId = string.Empty;
        public string packageName = string.Empty;
        public string productId = string.Empty;
        public string purchaseToken = string.Empty;
    }

    public struct PromotionPurchaseResult
    {
        public readonly bool success;
        public readonly string message;

        private PromotionPurchaseResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        public static PromotionPurchaseResult Success(string message)
        {
            return new PromotionPurchaseResult(true, message);
        }

        public static PromotionPurchaseResult Failed(string message)
        {
            return new PromotionPurchaseResult(false, message);
        }

        public static PromotionPurchaseResult Pending(string message)
        {
            return new PromotionPurchaseResult(false, message);
        }
    }
}
