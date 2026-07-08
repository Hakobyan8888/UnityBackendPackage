using System;
using BackendPackage.Runtime.Core;
using UnityEngine;

#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace BackendPackage.Runtime.Auth
{
    [DisallowMultipleComponent]
    public sealed class BackendAuthService : MonoBehaviour, IBackendPackageInitializable, IBackendPackageAuthenticator
    {
        public event Action<bool> AuthenticationCompleted;
        public event Action<bool, long> PlayerScoreLoaded;
        public event Action<bool> ScoreReported;

        [SerializeField] private BackendPackageConfig _configAsset;

        private BackendAuthConfiguration _config;
        private bool _initialized;
        private bool _authenticated;

#if UNITY_IOS
        private BackendGameCenterBridge _gameCenterBridge;
        private Action<bool> _pendingAuthCallback;
        private Action<bool, long> _pendingLoadScoreCallback;
        private Action<bool> _pendingReportScoreCallback;
#endif

        public bool IsInitialized => _initialized;
        public bool IsAuthenticated => _authenticated;

        private void Awake()
        {
#if UNITY_IOS
            EnsureGameCenterBridge();
#endif
        }

        public void Initialize(BackendAuthConfiguration config = null)
        {
            if (config == null && _configAsset != null)
            {
                config = _configAsset.auth;
            }

            _config = config ?? new BackendAuthConfiguration();
            _initialized = true;

#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
            if (_config.enableGooglePlayGames)
            {
                PlayGamesPlatform.Activate();
            }
#endif
        }

        public void Initialize(BackendPackageConfig config)
        {
            if (config == null || !config.initializeAuthOnStartup)
            {
                return;
            }

            Initialize(config.auth);
        }

        public void Authenticate(Action<bool> onComplete = null, bool? silentOverride = null)
        {
            if (!_initialized)
            {
                Initialize();
            }

            var silent = silentOverride ?? _config.silentAuthentication;

#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
            if (!_config.enableGooglePlayGames)
            {
                onComplete?.Invoke(false);
                AuthenticationCompleted?.Invoke(false);
                return;
            }

            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                var success = status == SignInStatus.Success;
                if (success)
                {
                    _authenticated = true;
                    onComplete?.Invoke(true);
                    AuthenticationCompleted?.Invoke(true);
                    return;
                }

                if (!silent)
                {
                    PlayGamesPlatform.Instance.ManuallyAuthenticate(manualStatus =>
                    {
                        var manualSuccess = manualStatus == SignInStatus.Success;
                        _authenticated = manualSuccess;
                        onComplete?.Invoke(manualSuccess);
                        AuthenticationCompleted?.Invoke(manualSuccess);
                    });
                    return;
                }

                _authenticated = false;
                onComplete?.Invoke(false);
                AuthenticationCompleted?.Invoke(false);
            });
#elif UNITY_IOS
            if (!_config.enableGameCenter)
            {
                onComplete?.Invoke(false);
                AuthenticationCompleted?.Invoke(false);
                return;
            }

            EnsureGameCenterBridge();
            _pendingAuthCallback = success =>
            {
                _authenticated = success;
                onComplete?.Invoke(success);
                AuthenticationCompleted?.Invoke(success);
            };
            _gameCenterBridge.Authenticate();
#else
            Debug.Log("[BackendAuthService] Auth SDK is not enabled for this platform.");
            onComplete?.Invoke(false);
            AuthenticationCompleted?.Invoke(false);
#endif
        }

        public void LoadPlayerScore(Action<bool, long> onComplete = null)
        {
            var leaderboardId = GetLeaderboardIdForCurrentPlatform();
            if (string.IsNullOrWhiteSpace(leaderboardId))
            {
                onComplete?.Invoke(false, 0);
                PlayerScoreLoaded?.Invoke(false, 0);
                return;
            }

#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
            if (!PlayGamesPlatform.Instance.localUser.authenticated)
            {
                Authenticate(success =>
                {
                    if (!success)
                    {
                        onComplete?.Invoke(false, 0);
                        PlayerScoreLoaded?.Invoke(false, 0);
                        return;
                    }

                    LoadAndroidPlayerScore(leaderboardId, onComplete);
                });
                return;
            }

            LoadAndroidPlayerScore(leaderboardId, onComplete);
#elif UNITY_IOS
            EnsureGameCenterBridge();
            _pendingLoadScoreCallback = (success, score) =>
            {
                onComplete?.Invoke(success, score);
                PlayerScoreLoaded?.Invoke(success, score);
            };

            if (_authenticated)
            {
                _gameCenterBridge.GetMyScore(leaderboardId);
                return;
            }

            Authenticate(success =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false, 0);
                    PlayerScoreLoaded?.Invoke(false, 0);
                    return;
                }

                _gameCenterBridge.GetMyScore(leaderboardId);
            });
#else
            onComplete?.Invoke(false, 0);
            PlayerScoreLoaded?.Invoke(false, 0);
#endif
        }

        public void ReportScore(long score, Action<bool> onComplete = null)
        {
            var leaderboardId = GetLeaderboardIdForCurrentPlatform();
            if (string.IsNullOrWhiteSpace(leaderboardId))
            {
                onComplete?.Invoke(false);
                ScoreReported?.Invoke(false);
                return;
            }

#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
            void PostScore()
            {
                PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, success =>
                {
                    onComplete?.Invoke(success);
                    ScoreReported?.Invoke(success);
                });
            }

            if (PlayGamesPlatform.Instance.localUser.authenticated)
            {
                PostScore();
                return;
            }

            Authenticate(success =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false);
                    ScoreReported?.Invoke(false);
                    return;
                }

                PostScore();
            });
#elif UNITY_IOS
            EnsureGameCenterBridge();
            _pendingReportScoreCallback = success =>
            {
                onComplete?.Invoke(success);
                ScoreReported?.Invoke(success);
            };

            if (_authenticated)
            {
                _gameCenterBridge.ReportScore(score, leaderboardId);
                return;
            }

            Authenticate(success =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false);
                    ScoreReported?.Invoke(false);
                    return;
                }

                _gameCenterBridge.ReportScore(score, leaderboardId);
            });
#else
            onComplete?.Invoke(false);
            ScoreReported?.Invoke(false);
#endif
        }

        public void ShowLeaderboard()
        {
            var leaderboardId = GetLeaderboardIdForCurrentPlatform();
            if (string.IsNullOrWhiteSpace(leaderboardId))
            {
                return;
            }

#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
            if (PlayGamesPlatform.Instance.localUser.authenticated)
            {
                PlayGamesPlatform.Instance.ShowLeaderboardUI();
                return;
            }

            Authenticate(success =>
            {
                if (success)
                {
                    PlayGamesPlatform.Instance.ShowLeaderboardUI();
                }
            }, false);
#elif UNITY_IOS
            EnsureGameCenterBridge();
            if (_authenticated)
            {
                _gameCenterBridge.ShowLeaderboard(leaderboardId);
                return;
            }

            Authenticate(success =>
            {
                if (success)
                {
                    _gameCenterBridge.ShowLeaderboard(leaderboardId);
                }
            }, false);
#endif
        }

        private string GetLeaderboardIdForCurrentPlatform()
        {
#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
            return _config.androidLeaderboardId;
#elif UNITY_IOS
            return _config.iosLeaderboardId;
#else
            return string.Empty;
#endif
        }

#if UNITY_ANDROID && BACKENDPACKAGE_ENABLE_AUTH
        private void LoadAndroidPlayerScore(string leaderboardId, Action<bool, long> onComplete)
        {
            PlayGamesPlatform.Instance.LoadScores(
                leaderboardId,
                LeaderboardStart.PlayerCentered,
                1,
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                data =>
                {
                    var success = data.Status == ResponseStatus.Success;
                    var score = success && data.PlayerScore != null ? data.PlayerScore.value : 0;
                    onComplete?.Invoke(success, score);
                    PlayerScoreLoaded?.Invoke(success, score);
                });
        }
#endif

#if UNITY_IOS
        private void EnsureGameCenterBridge()
        {
            if (_gameCenterBridge != null)
            {
                return;
            }

            _gameCenterBridge = GetComponent<BackendGameCenterBridge>();
            if (_gameCenterBridge == null)
            {
                _gameCenterBridge = gameObject.AddComponent<BackendGameCenterBridge>();
            }

            _gameCenterBridge.AuthSucceeded -= HandleGameCenterAuthSucceeded;
            _gameCenterBridge.AuthSucceeded += HandleGameCenterAuthSucceeded;
            _gameCenterBridge.AuthFailed -= HandleGameCenterAuthFailed;
            _gameCenterBridge.AuthFailed += HandleGameCenterAuthFailed;
            _gameCenterBridge.ScoreReported -= HandleGameCenterScoreReported;
            _gameCenterBridge.ScoreReported += HandleGameCenterScoreReported;
            _gameCenterBridge.ScoreReportFailed -= HandleGameCenterScoreReportFailed;
            _gameCenterBridge.ScoreReportFailed += HandleGameCenterScoreReportFailed;
            _gameCenterBridge.PlayerScoreLoaded -= HandleGameCenterPlayerScoreLoaded;
            _gameCenterBridge.PlayerScoreLoaded += HandleGameCenterPlayerScoreLoaded;
            _gameCenterBridge.PlayerScoreLoadFailed -= HandleGameCenterPlayerScoreLoadFailed;
            _gameCenterBridge.PlayerScoreLoadFailed += HandleGameCenterPlayerScoreLoadFailed;
        }

        private void HandleGameCenterAuthSucceeded()
        {
            _authenticated = true;
            _pendingAuthCallback?.Invoke(true);
            _pendingAuthCallback = null;
        }

        private void HandleGameCenterAuthFailed(string message)
        {
            _authenticated = false;
            _pendingAuthCallback?.Invoke(false);
            _pendingAuthCallback = null;
        }

        private void HandleGameCenterScoreReported()
        {
            _pendingReportScoreCallback?.Invoke(true);
            _pendingReportScoreCallback = null;
        }

        private void HandleGameCenterScoreReportFailed(string message)
        {
            _pendingReportScoreCallback?.Invoke(false);
            _pendingReportScoreCallback = null;
        }

        private void HandleGameCenterPlayerScoreLoaded(string score)
        {
            var success = long.TryParse(score, out var parsedScore);
            _pendingLoadScoreCallback?.Invoke(success, success ? parsedScore : 0);
            _pendingLoadScoreCallback = null;
        }

        private void HandleGameCenterPlayerScoreLoadFailed()
        {
            _pendingLoadScoreCallback?.Invoke(false, 0);
            _pendingLoadScoreCallback = null;
        }
#endif
    }
}
