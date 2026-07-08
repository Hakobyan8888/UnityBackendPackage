#if BACKENDPACKAGE_ENABLE_TIKTOK
using System;
using System.Collections.Generic;
using BackendPackage.Runtime.Core;
using SDK;
using UnityEngine;

namespace BackendPackage.Runtime.Analytics.TikTok
{
    [DisallowMultipleComponent]
    public sealed class BackendTikTokManager : MonoBehaviour, IBackendPackageInitializable
    {
        [SerializeField] private BackendPackageConfig _configAsset;

        public event Action<bool, int, string> InitializationCompleted;

        public bool IsInitialized { get; private set; }

        public void Initialize(BackendTikTokConfiguration config = null)
        {
            if (config == null && _configAsset != null)
            {
                config = _configAsset.tikTok;
            }

            if (config == null)
            {
                Debug.LogWarning("[BackendTikTokManager] Missing configuration.");
                return;
            }

            if (IsInitialized)
            {
                return;
            }

            var sdkConfig = new TikTokConfig(
                config.iosAccessToken,
                config.iosAppId,
                config.iosTikTokAppId,
                config.androidAccessToken,
                config.androidAppId,
                config.androidTikTokAppId);

            sdkConfig.DisablePayTrack();

            if (config.debugMode)
            {
                sdkConfig.SetLogLevel(TiktokLogLevel.Debug);
                sdkConfig.OpenDebugMode();
            }
            else
            {
                sdkConfig.SetLogLevel(TiktokLogLevel.None);
            }

            if (config.iosTrackingAuthorizationDelaySeconds > 0)
            {
                sdkConfig.IOS_SetDelayForATTUserAuthorizationInSeconds(
                    config.iosTrackingAuthorizationDelaySeconds);
            }

            TikTokBusinessSDK.InitializeSdk(sdkConfig, (success, code, message) =>
            {
                IsInitialized = success;
                InitializationCompleted?.Invoke(success, code, message);

                if (!success)
                {
                    return;
                }

                if (config.trackLaunchOnInitialize)
                {
                    TrackLaunchApp();
                }

#if UNITY_IOS
                if (config.requestIosTrackingAuthorization)
                {
                    TikTokBusinessSDK.IOS_requestTrackingAuthorization(_ => { });
                }
#endif
            });
        }

        public void Initialize(BackendPackageConfig config)
        {
            if (config == null || !config.initializeTikTokOnStartup)
            {
                return;
            }

            Initialize(config.tikTok);
        }

        public void TrackLaunchApp()
        {
            TrackEvent("LaunchAPP");
        }

        public void TrackCompleteTutorial()
        {
            TrackEvent("CompleteTutorial");
        }

        public void TrackAchieveLevel(int level)
        {
            TrackEvent("AchieveLevel", new Dictionary<string, object> { { "level", level } });
        }

        public void TrackRate()
        {
            TrackEvent("Rate");
        }

        public void TrackPurchase(string productId, decimal amount, string currencyCode)
        {
            if (!IsInitialized)
            {
                return;
            }

            var purchaseEvent = new TikTokPurchaseEvent(CreateEventId());
            purchaseEvent.SetContentId(productId);
            purchaseEvent.SetValue(Convert.ToDouble(amount));

            if (TryParseCurrency(currencyCode, out var currency))
            {
                purchaseEvent.SetCurrency(currency);
            }
            else if (!string.IsNullOrWhiteSpace(currencyCode))
            {
                purchaseEvent.AddProperty("currency", currencyCode.ToUpperInvariant());
            }

            TrackInternalEvent(purchaseEvent);
        }

        public void TrackEvent(string eventName, Dictionary<string, object> properties = null, string eventId = null)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            TrackInternalEvent(new TikTokBaseEvent(eventName, properties, eventId ?? CreateEventId()));
        }

        private void TrackInternalEvent(TikTokBaseEvent tikTokEvent)
        {
            if (!IsInitialized || tikTokEvent == null)
            {
                return;
            }

            try
            {
                TikTokBusinessSDK.TrackTTEvent(tikTokEvent);
            }
            catch (Exception ex)
            {
                var eventParams = tikTokEvent.getEventParams();
                eventParams.TryGetValue("eventName", out var eventName);
                Debug.LogError($"[BackendTikTokManager] Event send failed. eventName={eventName}, error={ex}");
            }
        }

        private static string CreateEventId() => Guid.NewGuid().ToString();

        private static bool TryParseCurrency(string currencyCode, out TTCurrency currency)
        {
            currency = default;
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return false;
            }

            return Enum.TryParse("TTCurrency" + currencyCode.Trim().ToUpperInvariant(), out currency);
        }
    }
}
#endif
