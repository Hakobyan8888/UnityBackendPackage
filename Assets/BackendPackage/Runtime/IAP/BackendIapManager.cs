using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendPackage.Runtime.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace BackendPackage.Runtime.IAP
{
    [DisallowMultipleComponent]
    public sealed class BackendIapManager : MonoBehaviour, IBackendPackageInitializable
    {
        [SerializeField] private BackendPackageConfig _configAsset;

        public event Action<IReadOnlyList<Product>> ProductsLoaded;
        public event Action<string> ProductsLoadFailed;
        public event Action<string> PurchasePendingShown;
        public event Action<string> PurchaseSucceeded;
        public event Action<string, string> PurchaseFailed;
        public event Action<string> PurchaseDeferred;

        private CatalogProvider _catalogProvider = new();
        private StoreController _storeController;
        private readonly Dictionary<string, Product> _products = new();
        private BackendIapConfiguration _config;
        private bool _initialized;
        private bool _connected;

        public bool IsInitialized => _initialized;
        public bool IsConnected => _connected;
        public IReadOnlyList<Product> Products => _products.Values.ToList().AsReadOnly();

        public void Initialize(BackendIapConfiguration config = null)
        {
            if (_initialized)
            {
                return;
            }

            if (config == null && _configAsset != null)
            {
                config = _configAsset.iap;
            }

            _config = config ?? new BackendIapConfiguration();
            _initialized = true;
            _ = InitializeAsync();
        }

        public void Initialize(BackendPackageConfig config)
        {
            if (config == null || !config.initializeIapOnStartup || !config.iap.autoInitialize)
            {
                return;
            }

            Initialize(config.iap);
        }

        public Product GetProduct(string productId)
        {
            _products.TryGetValue(productId, out var product);
            return product;
        }

        public void Purchase(string productId)
        {
            if (!_connected)
            {
                PurchaseFailed?.Invoke(productId, "Not connected");
                return;
            }

            if (!_products.ContainsKey(productId))
            {
                PurchaseFailed?.Invoke(productId, "Unknown product");
                return;
            }

            _storeController.PurchaseProduct(productId);
        }

        public void RestorePurchases()
        {
            if (!_connected || _storeController == null)
            {
                return;
            }

            _storeController.RestoreTransactions((_, _) => { });
            _storeController.FetchPurchases();
        }

        private async Task InitializeAsync()
        {
            try
            {
                BuildCatalogProviderFromProductCatalog();

                _storeController = UnityIAPServices.StoreController();
                AttachStoreEventHandlers();

                await _storeController.Connect();

                var storeName = DetermineStoreNameForPlatform();
                var productDefinitions = _catalogProvider.GetProducts(storeName);
                _storeController.FetchProducts(productDefinitions);

                if (_config.autoFetchPurchases)
                {
                    _storeController.FetchPurchases();
                }

                _connected = true;
            }
            catch (Exception ex)
            {
                _connected = false;
                Debug.LogError($"[BackendIapManager] Initialization failed: {ex}");
            }
        }

        private void BuildCatalogProviderFromProductCatalog()
        {
            _catalogProvider = new CatalogProvider();
            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog == null)
            {
                Debug.LogWarning("[BackendIapManager] No ProductCatalog found.");
                return;
            }

            foreach (var item in catalog.allValidProducts)
            {
                var storeIds = new StoreSpecificIds();
                if (item.allStoreIDs != null)
                {
                    foreach (var sid in item.allStoreIDs)
                    {
                        if (!string.IsNullOrWhiteSpace(sid.id) && !string.IsNullOrWhiteSpace(sid.store))
                        {
                            storeIds.Add(sid.id, sid.store);
                        }
                    }
                }

                List<PayoutDefinition> payouts = null;
                if (item.Payouts is { Count: > 0 })
                {
                    payouts = item.Payouts
                        .Select(p => new PayoutDefinition(p.typeString, p.subtype, p.quantity, p.data))
                        .ToList();
                }

                _catalogProvider.AddProduct(item.id, item.type, storeIds, payouts);
            }
        }

        private void AttachStoreEventHandlers()
        {
            _storeController.OnStoreDisconnected += desc =>
            {
                _connected = false;
                Debug.LogError($"[BackendIapManager] Store disconnected: {desc.Message}");
            };

            _storeController.OnProductsFetched += products =>
            {
                _products.Clear();
                foreach (var product in products)
                {
                    _products[product.definition.id] = product;
                }

                ProductsLoaded?.Invoke(products.AsReadOnly());
            };

            _storeController.OnProductsFetchFailed += failure =>
            {
                ProductsLoadFailed?.Invoke(failure.FailureReason.ToString());
            };

            _storeController.OnPurchasePending += HandlePurchasePending;
            _storeController.OnPurchaseDeferred += order =>
            {
                var productId = GetFirstProductInOrder(order)?.definition?.id ?? "unknown";
                PurchaseDeferred?.Invoke(productId);
            };

            _storeController.OnPurchaseFailed += order =>
            {
                var productId = GetFirstProductInOrder(order)?.definition?.id ?? "unknown";
                var reason = order?.FailureReason.ToString() ?? "Unknown";
                PurchaseFailed?.Invoke(productId, reason);
            };
        }

        private void HandlePurchasePending(PendingOrder order)
        {
            var product = GetFirstProductInOrder(order);
            var productId = product?.definition?.id ?? "unknown";
            PurchasePendingShown?.Invoke(productId);

            if (_config.autoConfirmPurchases)
            {
                try
                {
                    _storeController.ConfirmPurchase(order);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BackendIapManager] ConfirmPurchase failed: {ex.Message}");
                    PurchaseFailed?.Invoke(productId, ex.Message);
                    return;
                }
            }

            PurchaseSucceeded?.Invoke(productId);
        }

        private Product GetFirstProductInOrder(Order order)
        {
            return order?.CartOrdered?.Items()?.FirstOrDefault()?.Product;
        }

        private string DetermineStoreNameForPlatform()
        {
#if UNITY_ANDROID
            try { return GooglePlay.Name; } catch { return null; }
#elif UNITY_IOS
            try { return AppleAppStore.Name; } catch { return null; }
#else
            return null;
#endif
        }
    }
}
