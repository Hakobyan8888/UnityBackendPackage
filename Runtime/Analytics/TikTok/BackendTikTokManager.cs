using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BackendPackage.Runtime.Core;
using UnityEngine;

namespace BackendPackage.Runtime.Analytics.TikTok
{
    [DisallowMultipleComponent]
    public sealed class BackendTikTokManager : MonoBehaviour, IBackendPackageInitializable
    {
        private const string TikTokBusinessSdkTypeName = "SDK.TikTokBusinessSDK";
        private const string TikTokConfigTypeName = "SDK.TikTokConfig";
        private const string TikTokBaseEventTypeName = "SDK.TikTokBaseEvent";
        private const string TikTokPurchaseEventTypeName = "SDK.TikTokPurchaseEvent";
        private const string TikTokCurrencyTypeName = "SDK.TTCurrency";
        private const string TikTokLogLevelTypeName = "SDK.TiktokLogLevel";

        [SerializeField] private BackendPackageConfig _configAsset;

        public event Action<bool, int, string> InitializationCompleted;

        private BackendTikTokConfiguration _config;
        private Type _businessSdkType;
        private Type _baseEventType;
        private Type _purchaseEventType;
        private Type _currencyType;

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

            _config = config;

            if (!ResolveSdkTypes())
            {
                InitializationCompleted?.Invoke(false, -1, "TikTok SDK types were not found.");
                return;
            }

            var sdkConfig = CreateSdkConfig(config);
            if (sdkConfig == null)
            {
                InitializationCompleted?.Invoke(false, -1, "TikTokConfig could not be created.");
                return;
            }

            InvokeInstance(sdkConfig, "DisablePayTrack");

            if (config.debugMode)
            {
                SetLogLevel(sdkConfig, "Debug");
                InvokeInstance(sdkConfig, "OpenDebugMode");
            }
            else
            {
                SetLogLevel(sdkConfig, "None");
            }

            if (config.iosTrackingAuthorizationDelaySeconds > 0)
            {
                InvokeInstance(
                    sdkConfig,
                    "IOS_SetDelayForATTUserAuthorizationInSeconds",
                    config.iosTrackingAuthorizationDelaySeconds);
            }

            InvokeInitializeSdk(sdkConfig);
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
            if (!IsInitialized || _purchaseEventType == null)
            {
                return;
            }

            var purchaseEvent = Activator.CreateInstance(_purchaseEventType, CreateEventId());
            InvokeInstance(purchaseEvent, "SetContentId", productId);
            InvokeInstance(purchaseEvent, "SetValue", Convert.ToDouble(amount));

            if (TryParseCurrency(currencyCode, out var currency))
            {
                InvokeInstance(purchaseEvent, "SetCurrency", currency);
            }
            else if (!string.IsNullOrWhiteSpace(currencyCode))
            {
                InvokeInstance(purchaseEvent, "AddProperty", "currency", currencyCode.ToUpperInvariant());
            }

            TrackInternalEvent(purchaseEvent);
        }

        public void TrackEvent(string eventName, Dictionary<string, object> properties = null, string eventId = null)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(eventName) || _baseEventType == null)
            {
                return;
            }

            var tikTokEvent = Activator.CreateInstance(
                _baseEventType,
                eventName,
                properties,
                eventId ?? CreateEventId());

            TrackInternalEvent(tikTokEvent);
        }

        private bool ResolveSdkTypes()
        {
            _businessSdkType = FindType(TikTokBusinessSdkTypeName);
            _baseEventType = FindType(TikTokBaseEventTypeName);
            _purchaseEventType = FindType(TikTokPurchaseEventTypeName);
            _currencyType = FindType(TikTokCurrencyTypeName);

            return _businessSdkType != null &&
                   FindType(TikTokConfigTypeName) != null &&
                   _baseEventType != null;
        }

        private object CreateSdkConfig(BackendTikTokConfiguration config)
        {
            var configType = FindType(TikTokConfigTypeName);
            if (configType == null)
            {
                return null;
            }

            return Activator.CreateInstance(
                configType,
                config.iosAccessToken,
                config.iosAppId,
                config.iosTikTokAppId,
                config.androidAccessToken,
                config.androidAppId,
                config.androidTikTokAppId);
        }

        private void SetLogLevel(object sdkConfig, string levelName)
        {
            var logLevelType = FindType(TikTokLogLevelTypeName);
            if (logLevelType == null || !Enum.TryParse(logLevelType, levelName, out var level))
            {
                return;
            }

            InvokeInstance(sdkConfig, "SetLogLevel", level);
        }

        private void InvokeInitializeSdk(object sdkConfig)
        {
            var initializeMethod = _businessSdkType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "InitializeSdk" && method.GetParameters().Length == 2);

            if (initializeMethod == null)
            {
                InitializationCompleted?.Invoke(false, -1, "TikTokBusinessSDK.InitializeSdk was not found.");
                return;
            }

            var callbackType = initializeMethod.GetParameters()[1].ParameterType;
            var callback = CreateDelegate(callbackType, nameof(HandleInitializeCallback));
            initializeMethod.Invoke(null, new[] { sdkConfig, callback });
        }

        private void HandleInitializeCallback(bool success, int code, string message)
        {
            IsInitialized = success;
            InitializationCompleted?.Invoke(success, code, message);

            if (!success)
            {
                return;
            }

            if (_config.trackLaunchOnInitialize)
            {
                TrackLaunchApp();
            }

#if UNITY_IOS
            if (_config.requestIosTrackingAuthorization)
            {
                InvokeRequestTrackingAuthorization();
            }
#endif
        }

        private void InvokeRequestTrackingAuthorization()
        {
            var method = _businessSdkType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(candidate => candidate.Name == "IOS_requestTrackingAuthorization");

            if (method == null)
            {
                return;
            }

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                method.Invoke(null, null);
                return;
            }

            method.Invoke(null, new[] { CreateNoopDelegate(parameters[0].ParameterType) });
        }

        private void TrackInternalEvent(object tikTokEvent)
        {
            if (!IsInitialized || tikTokEvent == null || _businessSdkType == null)
            {
                return;
            }

            try
            {
                var method = _businessSdkType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(candidate => candidate.Name == "TrackTTEvent" && candidate.GetParameters().Length == 1);

                method?.Invoke(null, new[] { tikTokEvent });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackendTikTokManager] Event send failed. error={ex}");
            }
        }

        private bool TryParseCurrency(string currencyCode, out object currency)
        {
            currency = null;
            if (_currencyType == null || string.IsNullOrWhiteSpace(currencyCode))
            {
                return false;
            }

            return Enum.TryParse(
                _currencyType,
                "TTCurrency" + currencyCode.Trim().ToUpperInvariant(),
                out currency);
        }

        private object InvokeInstance(object instance, string methodName, params object[] arguments)
        {
            if (instance == null)
            {
                return null;
            }

            var method = instance
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate =>
                    candidate.Name == methodName &&
                    candidate.GetParameters().Length == arguments.Length);

            return method?.Invoke(instance, arguments);
        }

        private Delegate CreateDelegate(Type delegateType, string methodName)
        {
            var invokeMethod = delegateType.GetMethod("Invoke");
            var parameterTypes = invokeMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
            return Delegate.CreateDelegate(delegateType, this, method);
        }

        private static Delegate CreateNoopDelegate(Type delegateType)
        {
            var invoke = delegateType.GetMethod("Invoke");
            var parameters = invoke.GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType))
                .ToArray();
            var body = invoke.ReturnType == typeof(void)
                ? Expression.Empty()
                : Expression.Default(invoke.ReturnType);

            return Expression.Lambda(delegateType, body, parameters).Compile();
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string CreateEventId() => Guid.NewGuid().ToString();
    }
}
