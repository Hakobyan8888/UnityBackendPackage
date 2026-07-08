using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendPackage.Runtime.Core;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

namespace BackendPackage.Runtime.Analytics.UnityAnalyticsAdapter
{
    [DisallowMultipleComponent]
    public sealed class BackendUnityAnalyticsManager : MonoBehaviour, IBackendPackageInitializable, IBackendAnalyticsProvider
    {
        [SerializeField] private BackendPackageConfig _configAsset;

        public event Action<bool, string> InitializationCompleted;
        public event Action<string> EventTracked;
        public event Action<string, string> EventTrackFailed;
        public event Action DataCollectionStarted;
        public event Action DataCollectionStopped;

        private BackendUnityAnalyticsConfiguration _config;
        private bool _initializing;

        public bool IsInitialized { get; private set; }
        public bool IsDataCollectionStarted { get; private set; }

        public void Initialize(BackendUnityAnalyticsConfiguration config = null)
        {
            if (IsInitialized || _initializing)
            {
                return;
            }

            if (config == null && _configAsset != null)
            {
                config = _configAsset.unityAnalytics;
            }

            _config = config ?? new BackendUnityAnalyticsConfiguration();
            _ = InitializeAsync();
        }

        public void Initialize(BackendPackageConfig config)
        {
            if (config == null || !config.initializeUnityAnalyticsOnStartup || !config.unityAnalytics.autoInitialize)
            {
                return;
            }

            Initialize(config.unityAnalytics);
        }

        public void StartDataCollection()
        {
            if (!IsInitialized)
            {
                Log("StartDataCollection ignored because Unity Analytics is not initialized.");
                return;
            }

            try
            {
                AnalyticsService.Instance.StartDataCollection();
                IsDataCollectionStarted = true;
                DataCollectionStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackendUnityAnalyticsManager] StartDataCollection failed: {ex}");
            }
        }

        public void StopDataCollection()
        {
            if (!IsInitialized)
            {
                return;
            }

            try
            {
                AnalyticsService.Instance.StopDataCollection();
                IsDataCollectionStarted = false;
                DataCollectionStopped?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackendUnityAnalyticsManager] StopDataCollection failed: {ex}");
            }
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (!IsDataCollectionStarted)
            {
                Log($"TrackEvent ignored because data collection has not started. eventName={eventName}");
                return;
            }

            try
            {
                if (parameters == null || parameters.Count == 0)
                {
                    AnalyticsService.Instance.RecordEvent(eventName);
                }
                else
                {
                    var customEvent = new CustomEvent(eventName);
                    foreach (var pair in parameters)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                        {
                            continue;
                        }

                        AddParameter(customEvent, pair.Key, pair.Value);
                    }

                    AnalyticsService.Instance.RecordEvent(customEvent);
                }

                EventTracked?.Invoke(eventName);
            }
            catch (Exception ex)
            {
                EventTrackFailed?.Invoke(eventName, ex.Message);
                Debug.LogError($"[BackendUnityAnalyticsManager] Event send failed. eventName={eventName}, error={ex}");
            }
        }

        public void Flush()
        {
            if (!IsInitialized)
            {
                return;
            }

            try
            {
                AnalyticsService.Instance.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackendUnityAnalyticsManager] Flush failed: {ex}");
            }
        }

        public void RequestDataDeletion()
        {
            if (!IsInitialized)
            {
                return;
            }

            AnalyticsService.Instance.RequestDataDeletion();
            IsDataCollectionStarted = false;
        }

        private async Task InitializeAsync()
        {
            _initializing = true;

            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }
                else
                {
                    while (UnityServices.State == ServicesInitializationState.Initializing)
                    {
                        await Task.Yield();
                    }
                }

                IsInitialized = UnityServices.State == ServicesInitializationState.Initialized;
                _initializing = false;

                if (!IsInitialized)
                {
                    InitializationCompleted?.Invoke(false, UnityServices.State.ToString());
                    return;
                }

                if (_config.registerAsDefaultProvider)
                {
                    BackendAnalytics.SetProvider(this);
                }

                if (_config.startDataCollectionOnInitialize)
                {
                    StartDataCollection();
                }

                if (_config.trackAppStartedOnInitialize)
                {
                    TrackEvent("app_started");
                }

                InitializationCompleted?.Invoke(true, string.Empty);
                Log("Unity Analytics initialized.");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                _initializing = false;
                InitializationCompleted?.Invoke(false, ex.Message);
                Debug.LogError($"[BackendUnityAnalyticsManager] Initialization failed: {ex}");
            }
        }

        private static void AddParameter(CustomEvent customEvent, string key, object value)
        {
            switch (value)
            {
                case string stringValue:
                    customEvent[key] = stringValue;
                    break;
                case int intValue:
                    customEvent[key] = intValue;
                    break;
                case long longValue:
                    customEvent[key] = longValue;
                    break;
                case float floatValue:
                    customEvent[key] = floatValue;
                    break;
                case double doubleValue:
                    customEvent[key] = doubleValue;
                    break;
                case decimal decimalValue:
                    customEvent[key] = Convert.ToDouble(decimalValue);
                    break;
                case bool boolValue:
                    customEvent[key] = boolValue;
                    break;
                case DateTime dateTimeValue:
                    customEvent[key] = dateTimeValue;
                    break;
                default:
                    customEvent[key] = value.ToString();
                    break;
            }
        }

        private void OnDestroy()
        {
            BackendAnalytics.ClearProvider(this);
        }

        private void Log(string message)
        {
            if (_config != null && _config.debugLogs)
            {
                Debug.Log($"[BackendUnityAnalyticsManager] {message}");
            }
        }
    }
}
