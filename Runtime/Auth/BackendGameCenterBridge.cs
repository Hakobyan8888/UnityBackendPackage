using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BackendPackage.Runtime.Auth
{
    public sealed class BackendGameCenterBridge : MonoBehaviour
    {
        private const string NativeCallbackObjectName = "BackendGameCenterListener";

        public event Action AuthSucceeded;
        public event Action<string> AuthFailed;
        public event Action ScoreReported;
        public event Action<string> ScoreReportFailed;
        public event Action LeaderboardClosed;
        public event Action<string> LeaderboardShowFailed;
        public event Action<string> PlayerScoreLoaded;
        public event Action PlayerScoreLoadFailed;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void BGC_Authenticate();

        [DllImport("__Internal")]
        private static extern int BGC_IsAuthenticated();

        [DllImport("__Internal")]
        private static extern void BGC_ReportScore(long score, string leaderboardID);

        [DllImport("__Internal")]
        private static extern void BGC_ShowLeaderboard(string leaderboardID);

        [DllImport("__Internal")]
        private static extern void BGC_GetMyScore(string leaderboardID, int timeScope);
#else
        private static void BGC_Authenticate() { }
        private static int BGC_IsAuthenticated() { return 0; }
        private static void BGC_ReportScore(long score, string leaderboardID) { }
        private static void BGC_ShowLeaderboard(string leaderboardID) { }
        private static void BGC_GetMyScore(string leaderboardID, int timeScope) { }
#endif

        private void Awake()
        {
            if (GameObject.Find(NativeCallbackObjectName) != null)
            {
                return;
            }

            var listenerObject = new GameObject(NativeCallbackObjectName);
            listenerObject.AddComponent<BackendGameCenterListener>();
            DontDestroyOnLoad(listenerObject);
        }

        private void OnEnable()
        {
            BackendGameCenterListener.AuthSuccess += HandleAuthSuccess;
            BackendGameCenterListener.AuthFailed += HandleAuthFailed;
            BackendGameCenterListener.ReportSuccess += HandleReportSuccess;
            BackendGameCenterListener.ReportFailed += HandleReportFailed;
            BackendGameCenterListener.LeaderboardDismissed += HandleLeaderboardClosed;
            BackendGameCenterListener.ShowLeaderboardFailed += HandleLeaderboardShowFailed;
            BackendGameCenterListener.GetScoreSuccess += HandleGetScoreSuccess;
            BackendGameCenterListener.GetScoreFailed += HandleGetScoreFailed;
        }

        private void OnDisable()
        {
            BackendGameCenterListener.AuthSuccess -= HandleAuthSuccess;
            BackendGameCenterListener.AuthFailed -= HandleAuthFailed;
            BackendGameCenterListener.ReportSuccess -= HandleReportSuccess;
            BackendGameCenterListener.ReportFailed -= HandleReportFailed;
            BackendGameCenterListener.LeaderboardDismissed -= HandleLeaderboardClosed;
            BackendGameCenterListener.ShowLeaderboardFailed -= HandleLeaderboardShowFailed;
            BackendGameCenterListener.GetScoreSuccess -= HandleGetScoreSuccess;
            BackendGameCenterListener.GetScoreFailed -= HandleGetScoreFailed;
        }

        public void Authenticate() => BGC_Authenticate();

        public bool IsAuthenticated() => BGC_IsAuthenticated() != 0;

        public void ReportScore(long score, string leaderboardId) => BGC_ReportScore(score, leaderboardId);

        public void ShowLeaderboard(string leaderboardId) => BGC_ShowLeaderboard(leaderboardId);

        public void GetMyScore(string leaderboardId, int timeScope = 2) => BGC_GetMyScore(leaderboardId, timeScope);

        private void HandleAuthSuccess() => AuthSucceeded?.Invoke();

        private void HandleAuthFailed(string message) => AuthFailed?.Invoke(message);

        private void HandleReportSuccess() => ScoreReported?.Invoke();

        private void HandleReportFailed(string message) => ScoreReportFailed?.Invoke(message);

        private void HandleLeaderboardClosed() => LeaderboardClosed?.Invoke();

        private void HandleLeaderboardShowFailed(string message) => LeaderboardShowFailed?.Invoke(message);

        private void HandleGetScoreSuccess(string score) => PlayerScoreLoaded?.Invoke(score);

        private void HandleGetScoreFailed() => PlayerScoreLoadFailed?.Invoke();
    }

    public sealed class BackendGameCenterListener : MonoBehaviour
    {
        public static event Action AuthSuccess;
        public static event Action<string> AuthFailed;
        public static event Action ReportSuccess;
        public static event Action<string> ReportFailed;
        public static event Action LeaderboardDismissed;
        public static event Action<string> ShowLeaderboardFailed;
        public static event Action<string> GetScoreSuccess;
        public static event Action GetScoreFailed;

        public void OnAuthSuccess(string message) => AuthSuccess?.Invoke();

        public void OnAuthFailed(string message) => AuthFailed?.Invoke(message);

        public void OnReportScoreSuccess(string message) => ReportSuccess?.Invoke();

        public void OnReportScoreFailed(string message) => ReportFailed?.Invoke(message);

        public void OnLeaderboardClosed(string message) => LeaderboardDismissed?.Invoke();

        public void OnShowLeaderboardFailed(string message) => ShowLeaderboardFailed?.Invoke(message);

        public void OnGetMyScoreSuccess(string score) => GetScoreSuccess?.Invoke(score);

        public void OnGetMyScoreFailed(string message) => GetScoreFailed?.Invoke();
    }
}
