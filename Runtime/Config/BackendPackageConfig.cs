using System;
using UnityEngine;

namespace BackendPackage.Runtime.Core
{
    [CreateAssetMenu(
        fileName = "BackendPackageConfig",
        menuName = "Backend Package/Backend Package Config")]
    public sealed class BackendPackageConfig : ScriptableObject
    {
        [Header("Bootstrap")]
        public bool dontDestroyOnLoad = true;
        public bool initializeAuthOnStartup = true;
        public bool authenticateOnStartup = true;
        public bool waitForAuthenticationOnStartup = true;
        public bool completeBootstrapWhenAuthenticationFails = true;
        public bool initializeIapOnStartup = true;
        public bool initializeTikTokOnStartup = true;
        public bool initializeUnityAnalyticsOnStartup = true;

        [Header("Auth")]
        public BackendAuthConfiguration auth = new();

        [Header("IAP")]
        public BackendIapConfiguration iap = new();

        [Header("TikTok")]
        public BackendTikTokConfiguration tikTok = new();

        [Header("Unity Analytics")]
        public BackendUnityAnalyticsConfiguration unityAnalytics = new();

        [Header("Ads")]
        public BackendAdsConfiguration ads = new();
    }

    [Serializable]
    public sealed class BackendAuthConfiguration
    {
        [Header("Platform Toggles")]
        public bool enableGooglePlayGames = true;
        public bool enableGameCenter = true;
        public bool silentAuthentication = true;

        [Header("Leaderboard IDs")]
        public string androidLeaderboardId;
        public string iosLeaderboardId;
    }

    [Serializable]
    public sealed class BackendIapConfiguration
    {
        [Header("General")]
        public bool autoInitialize = true;
        public bool autoFetchPurchases = true;
        public bool autoConfirmPurchases = true;
    }

    [Serializable]
    public sealed class BackendTikTokConfiguration
    {
        [Header("General")]
        public bool debugMode;
        public bool trackLaunchOnInitialize = true;

        [Header("iOS")]
        public string iosAppId;
        public string iosTikTokAppId;
        public string iosAccessToken;
        public bool requestIosTrackingAuthorization = true;
        public long iosTrackingAuthorizationDelaySeconds;

        [Header("Android")]
        public string androidAppId;
        public string androidTikTokAppId;
        public string androidAccessToken;
    }

    [Serializable]
    public sealed class BackendUnityAnalyticsConfiguration
    {
        [Header("General")]
        public bool autoInitialize = true;
        public bool registerAsDefaultProvider = true;
        public bool startDataCollectionOnInitialize;
        public bool trackAppStartedOnInitialize = true;
        public bool debugLogs;
    }

    [Serializable]
    public sealed class BackendAdsConfiguration
    {
        [Header("General")]
        public bool autoInitialize = true;
        public bool enableRewarded = true;
        public bool enableInterstitial = true;
        public bool enableBanner = true;

        [Header("Android")]
        public string androidAppKey;
        public string androidRewardedAdUnitId;
        public string androidInterstitialAdUnitId;
        public string androidBannerAdUnitId;

        [Header("iOS")]
        public string iosAppKey;
        public string iosRewardedAdUnitId;
        public string iosInterstitialAdUnitId;
        public string iosBannerAdUnitId;

        [Header("Banner")]
        public bool autoLoadBanner = true;
        public bool showBannerOnLoad = true;
        public bool respectBannerSafeArea = true;
        public string bannerPlacementName;
        public BackendBannerSize bannerSize = BackendBannerSize.Banner;
        public BackendBannerPosition bannerPosition = BackendBannerPosition.BottomCenter;
        public Vector2 bannerCustomPosition;
        public int bannerCustomWidth;
        public int bannerCustomHeight;
        public int bannerAdaptiveWidth = -1;
    }

    public enum BackendBannerSize
    {
        Banner,
        Large,
        MediumRectangle,
        Leaderboard,
        Adaptive,
        Custom
    }

    public enum BackendBannerPosition
    {
        BottomCenter = 0,
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomRight,
        Custom
    }
}
