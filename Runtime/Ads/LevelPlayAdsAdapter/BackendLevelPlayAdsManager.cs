using System;
using BackendPackage.Runtime.Core;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace BackendPackage.Runtime.Ads.LevelPlayAdsAdapter
{
    [DisallowMultipleComponent]
    public sealed class BackendLevelPlayAdsManager : MonoBehaviour, IBackendPackageInitializable
    {
        [SerializeField] private BackendPackageConfig _configAsset;

        public event Action<bool, string> InitializationCompleted;
        public event Action<BackendRewardedAdResult> RewardedCompleted;
        public event Action<string> RewardedClosed;
        public event Action<string, string> RewardedDisplayFailed;
        public event Action<string> RewardedLoaded;
        public event Action<string, string> RewardedLoadFailed;
        public event Action<string> InterstitialLoaded;
        public event Action<string, string> InterstitialLoadFailed;
        public event Action<string> InterstitialClosed;
        public event Action<string, string> InterstitialDisplayFailed;
        public event Action<string> BannerLoaded;
        public event Action<string, string> BannerLoadFailed;
        public event Action<string> BannerDisplayed;
        public event Action<string, string> BannerDisplayFailed;
        public event Action<string> BannerClicked;
        public event Action<string> BannerExpanded;
        public event Action<string> BannerCollapsed;
        public event Action<string> BannerLeftApplication;

        private LevelPlayRewardedAd _rewardedAd;
        private LevelPlayInterstitialAd _interstitialAd;
        private LevelPlayBannerAd _bannerAd;
        private BackendAdsConfiguration _config;
        private bool _bannerLoaded;
        private bool _initialized;
        private bool _initializing;

        public bool IsInitialized => _initialized;

        public void Initialize(BackendAdsConfiguration config = null)
        {
            if (_initialized || _initializing)
            {
                InitializationCompleted?.Invoke(_initialized, _initialized ? string.Empty : "Initialization already in progress");
                return;
            }

            if (config == null && _configAsset != null)
            {
                config = _configAsset.ads;
            }

            _config = config ?? new BackendAdsConfiguration();
            var appKey = GetAppKey();

            if (string.IsNullOrWhiteSpace(appKey))
            {
                Debug.LogWarning("[BackendLevelPlayAdsManager] Missing LevelPlay app key.");
                InitializationCompleted?.Invoke(false, "Missing app key");
                return;
            }

            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;
            LevelPlay.OnInitFailed += HandleInitFailed;

            _initializing = true;
            LevelPlay.ValidateIntegration();
            LevelPlay.Init(appKey);
        }

        public void Initialize(BackendPackageConfig config)
        {
            if (config == null || !config.ads.autoInitialize)
            {
                return;
            }

            Initialize(config.ads);
        }

        public bool IsRewardedReady()
        {
            return _rewardedAd != null && _rewardedAd.IsAdReady();
        }

        public bool IsInterstitialReady()
        {
            return _interstitialAd != null && _interstitialAd.IsAdReady();
        }

        public bool IsBannerLoaded()
        {
            return _bannerAd != null && _bannerLoaded;
        }

        public bool ShowRewarded(string placement = null, string context = null)
        {
            if (_rewardedAd == null || !_rewardedAd.IsAdReady())
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(placement) && LevelPlayRewardedAd.IsPlacementCapped(placement))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(placement))
            {
                _rewardedAd.ShowAd(placement);
            }
            else
            {
                _rewardedAd.ShowAd();
            }

            return true;
        }

        public bool ShowInterstitial(string placement = null)
        {
            if (_interstitialAd == null || !_interstitialAd.IsAdReady())
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(placement))
            {
                _interstitialAd.ShowAd(placement);
            }
            else
            {
                _interstitialAd.ShowAd();
            }

            return true;
        }

        public void LoadRewarded()
        {
            _rewardedAd?.LoadAd();
        }

        public void LoadInterstitial()
        {
            _interstitialAd?.LoadAd();
        }

        public void LoadBanner()
        {
            _bannerLoaded = false;
            _bannerAd?.LoadAd();
        }

        public bool ShowBanner()
        {
            if (_bannerAd == null)
            {
                return false;
            }

            _bannerAd.ShowAd();
            return true;
        }

        public void HideBanner()
        {
            _bannerAd?.HideAd();
        }

        public void DestroyBanner()
        {
            _bannerLoaded = false;
            _bannerAd?.DestroyAd();
            _bannerAd = null;
        }

        private void HandleInitSuccess(LevelPlayConfiguration configuration)
        {
            _initialized = true;
            _initializing = false;

            if (_config.enableRewarded)
            {
                SetupRewarded();
            }

            if (_config.enableInterstitial)
            {
                SetupInterstitial();
            }

            if (_config.enableBanner)
            {
                SetupBanner();
            }

            InitializationCompleted?.Invoke(true, string.Empty);
        }

        private void HandleInitFailed(LevelPlayInitError error)
        {
            _initialized = false;
            _initializing = false;
            InitializationCompleted?.Invoke(false, error.ToString());
        }

        private void SetupRewarded()
        {
            var rewardedUnitId = GetRewardedAdUnitId();
            if (string.IsNullOrWhiteSpace(rewardedUnitId))
            {
                return;
            }

            _rewardedAd = new LevelPlayRewardedAd(rewardedUnitId);
            _rewardedAd.OnAdLoaded += info => RewardedLoaded?.Invoke(info.AdUnitId);
            _rewardedAd.OnAdLoadFailed += error => RewardedLoadFailed?.Invoke(error.AdUnitId, error.ToString());
            _rewardedAd.OnAdDisplayFailed += (adInfo, error) =>
                RewardedDisplayFailed?.Invoke(adInfo.AdUnitId, error.ToString());
            _rewardedAd.OnAdRewarded += (info, reward) =>
                RewardedCompleted?.Invoke(new BackendRewardedAdResult(info.AdUnitId, info.AdId, reward.Name, reward.Amount));
            _rewardedAd.OnAdClosed += info =>
            {
                RewardedClosed?.Invoke(info.AdUnitId);
                _rewardedAd.LoadAd();
            };

            _rewardedAd.LoadAd();
        }

        private void SetupInterstitial()
        {
            var interstitialUnitId = GetInterstitialAdUnitId();
            if (string.IsNullOrWhiteSpace(interstitialUnitId))
            {
                return;
            }

            _interstitialAd = new LevelPlayInterstitialAd(interstitialUnitId);
            _interstitialAd.OnAdLoaded += info => InterstitialLoaded?.Invoke(info.AdUnitId);
            _interstitialAd.OnAdLoadFailed += error =>
                InterstitialLoadFailed?.Invoke(error.AdUnitId, error.ErrorMessage);
            _interstitialAd.OnAdDisplayFailed += (adInfo, error) =>
                InterstitialDisplayFailed?.Invoke(adInfo.AdUnitId, error.ToString());
            _interstitialAd.OnAdClosed += info =>
            {
                InterstitialClosed?.Invoke(info.AdUnitId);
                _interstitialAd.LoadAd();
            };

            _interstitialAd.LoadAd();
        }

        private void SetupBanner()
        {
            var bannerUnitId = GetBannerAdUnitId();
            if (string.IsNullOrWhiteSpace(bannerUnitId))
            {
                return;
            }

            DestroyBanner();

            var bannerConfig = new LevelPlayBannerAd.Config.Builder()
                .SetSize(GetBannerSize())
                .SetPosition(GetBannerPosition())
                .SetDisplayOnLoad(_config.showBannerOnLoad)
                .SetRespectSafeArea(_config.respectBannerSafeArea)
                .SetPlacementName(_config.bannerPlacementName)
                .Build();

            _bannerAd = new LevelPlayBannerAd(bannerUnitId, bannerConfig);
            _bannerAd.OnAdLoaded += info =>
            {
                _bannerLoaded = true;
                BannerLoaded?.Invoke(info.AdUnitId);
            };
            _bannerAd.OnAdLoadFailed += error =>
            {
                _bannerLoaded = false;
                BannerLoadFailed?.Invoke(error.AdUnitId, error.ErrorMessage);
            };
            _bannerAd.OnAdDisplayed += info => BannerDisplayed?.Invoke(info.AdUnitId);
            _bannerAd.OnAdDisplayFailed += (adInfo, error) =>
                BannerDisplayFailed?.Invoke(adInfo.AdUnitId, error.ToString());
            _bannerAd.OnAdClicked += info => BannerClicked?.Invoke(info.AdUnitId);
            _bannerAd.OnAdExpanded += info => BannerExpanded?.Invoke(info.AdUnitId);
            _bannerAd.OnAdCollapsed += info => BannerCollapsed?.Invoke(info.AdUnitId);
            _bannerAd.OnAdLeftApplication += info => BannerLeftApplication?.Invoke(info.AdUnitId);

            if (_config.autoLoadBanner)
            {
                _bannerAd.LoadAd();
            }
        }

        private string GetAppKey()
        {
#if UNITY_ANDROID
            return _config.androidAppKey;
#elif UNITY_IOS
            return _config.iosAppKey;
#else
            return string.Empty;
#endif
        }

        private string GetRewardedAdUnitId()
        {
#if UNITY_ANDROID
            return _config.androidRewardedAdUnitId;
#elif UNITY_IOS
            return _config.iosRewardedAdUnitId;
#else
            return string.Empty;
#endif
        }

        private string GetInterstitialAdUnitId()
        {
#if UNITY_ANDROID
            return _config.androidInterstitialAdUnitId;
#elif UNITY_IOS
            return _config.iosInterstitialAdUnitId;
#else
            return string.Empty;
#endif
        }

        private string GetBannerAdUnitId()
        {
#if UNITY_ANDROID
            return _config.androidBannerAdUnitId;
#elif UNITY_IOS
            return _config.iosBannerAdUnitId;
#else
            return string.Empty;
#endif
        }

        private LevelPlayAdSize GetBannerSize()
        {
            switch (_config.bannerSize)
            {
                case BackendBannerSize.Large:
                    return LevelPlayAdSize.LARGE;
                case BackendBannerSize.MediumRectangle:
                    return LevelPlayAdSize.MEDIUM_RECTANGLE;
                case BackendBannerSize.Leaderboard:
                    return LevelPlayAdSize.LEADERBOARD;
                case BackendBannerSize.Adaptive:
                    return LevelPlayAdSize.CreateAdaptiveAdSize(_config.bannerAdaptiveWidth);
                case BackendBannerSize.Custom:
                    if (_config.bannerCustomWidth > 0 && _config.bannerCustomHeight > 0)
                    {
                        return LevelPlayAdSize.CreateCustomBannerSize(_config.bannerCustomWidth, _config.bannerCustomHeight);
                    }

                    return LevelPlayAdSize.BANNER;
                case BackendBannerSize.Banner:
                default:
                    return LevelPlayAdSize.BANNER;
            }
        }

        private LevelPlayBannerPosition GetBannerPosition()
        {
            switch (_config.bannerPosition)
            {
                case BackendBannerPosition.TopLeft:
                    return LevelPlayBannerPosition.TopLeft;
                case BackendBannerPosition.TopCenter:
                    return LevelPlayBannerPosition.TopCenter;
                case BackendBannerPosition.TopRight:
                    return LevelPlayBannerPosition.TopRight;
                case BackendBannerPosition.CenterLeft:
                    return LevelPlayBannerPosition.CenterLeft;
                case BackendBannerPosition.Center:
                    return LevelPlayBannerPosition.Center;
                case BackendBannerPosition.CenterRight:
                    return LevelPlayBannerPosition.CenterRight;
                case BackendBannerPosition.BottomLeft:
                    return LevelPlayBannerPosition.BottomLeft;
                case BackendBannerPosition.BottomRight:
                    return LevelPlayBannerPosition.BottomRight;
                case BackendBannerPosition.Custom:
                    return new LevelPlayBannerPosition(_config.bannerCustomPosition);
                case BackendBannerPosition.BottomCenter:
                default:
                    return LevelPlayBannerPosition.BottomCenter;
            }
        }
    }

    [Serializable]
    public sealed class BackendRewardedAdResult
    {
        public string AdUnitId { get; }
        public string AdId { get; }
        public string RewardName { get; }
        public double RewardAmount { get; }

        public BackendRewardedAdResult(string adUnitId, string adId, string rewardName, double rewardAmount)
        {
            AdUnitId = adUnitId;
            AdId = adId;
            RewardName = rewardName;
            RewardAmount = rewardAmount;
        }
    }
}
