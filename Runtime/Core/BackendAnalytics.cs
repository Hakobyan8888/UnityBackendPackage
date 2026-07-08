using System.Collections.Generic;

namespace BackendPackage.Runtime.Core
{
    public static class BackendAnalytics
    {
        private static IBackendAnalyticsProvider _provider;

        public static bool HasProvider => _provider != null;
        public static bool IsInitialized => _provider?.IsInitialized == true;
        public static bool IsDataCollectionStarted => _provider?.IsDataCollectionStarted == true;

        public static void SetProvider(IBackendAnalyticsProvider provider)
        {
            _provider = provider;
        }

        public static void ClearProvider(IBackendAnalyticsProvider provider)
        {
            if (_provider == provider)
            {
                _provider = null;
            }
        }

        public static void StartDataCollection()
        {
            _provider?.StartDataCollection();
        }

        public static void StopDataCollection()
        {
            _provider?.StopDataCollection();
        }

        public static void TrackEvent(string eventName)
        {
            _provider?.TrackEvent(eventName);
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            _provider?.TrackEvent(eventName, parameters);
        }

        public static void TrackLevelStarted(int levelIndex, string levelName = null)
        {
            TrackEvent("level_started", new Dictionary<string, object>
            {
                { "level_index", levelIndex },
                { "level_name", levelName ?? string.Empty }
            });
        }

        public static void TrackLevelCompleted(int levelIndex, string levelName = null)
        {
            TrackEvent("level_completed", new Dictionary<string, object>
            {
                { "level_index", levelIndex },
                { "level_name", levelName ?? string.Empty }
            });
        }

        public static void TrackPurchase(string productId, double amount, string currencyCode)
        {
            TrackEvent("purchase_completed", new Dictionary<string, object>
            {
                { "product_id", productId ?? string.Empty },
                { "amount", amount },
                { "currency", currencyCode ?? string.Empty }
            });
        }

        public static void TrackRewardedAdCompleted(string placement, string rewardName, double rewardAmount)
        {
            TrackEvent("ad_rewarded_completed", new Dictionary<string, object>
            {
                { "placement", placement ?? string.Empty },
                { "reward_name", rewardName ?? string.Empty },
                { "reward_amount", rewardAmount }
            });
        }

        public static void Flush()
        {
            _provider?.Flush();
        }
    }
}
