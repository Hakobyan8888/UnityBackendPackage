using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace BackendPackage.Runtime.Core
{
    [DisallowMultipleComponent]
    public sealed class BackendBootstrapper : MonoBehaviour
    {
        [SerializeField] private BackendPackageConfig _config;

        [Header("Events")]
        [SerializeField] private UnityEvent _onBootstrapStarted;
        [SerializeField] private UnityEvent<bool> _onStartupAuthenticationComplete;
        [SerializeField] private UnityEvent _onBootstrapComplete;
        [SerializeField] private UnityEvent<bool> _onBootstrapCompleteWithAuthentication;

        public event Action BootstrapStarted;
        public event Action<bool> StartupAuthenticationCompleted;
        public event Action<bool> BootstrapCompleted;

        public BackendPackageConfig Config => _config;
        public bool IsBootstrapping { get; private set; }
        public bool IsBootstrapComplete { get; private set; }
        public bool StartupAuthenticationSucceeded { get; private set; }

        private void Awake()
        {
            if (_config != null && _config.dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            StartBootstrap();
        }

        public void StartBootstrap()
        {
            if (IsBootstrapping || IsBootstrapComplete)
            {
                return;
            }

            if (_config == null)
            {
                Debug.LogWarning("[BackendBootstrapper] Missing BackendPackageConfig.");
                return;
            }

            IsBootstrapping = true;
            BootstrapStarted?.Invoke();
            _onBootstrapStarted?.Invoke();

            foreach (var initializable in GetComponents<MonoBehaviour>().OfType<IBackendPackageInitializable>())
            {
                initializable.Initialize(_config);
            }

            if (_config.authenticateOnStartup)
            {
                AuthenticateOnStartup();
                return;
            }

            CompleteBootstrap(false);
        }

        private void AuthenticateOnStartup()
        {
            var authenticators = GetComponents<MonoBehaviour>()
                .OfType<IBackendPackageAuthenticator>()
                .ToArray();

            if (authenticators.Length == 0)
            {
                Debug.LogWarning("[BackendBootstrapper] Startup authentication is enabled, but no authenticator component was found.");
                CompleteStartupAuthentication(false);
                CompleteBootstrap(false);
                return;
            }

            var remaining = authenticators.Length;
            var anySucceeded = false;

            foreach (var authenticator in authenticators)
            {
                authenticator.Authenticate(success =>
                {
                    anySucceeded |= success;
                    remaining--;

                    if (remaining > 0)
                    {
                        return;
                    }

                    CompleteStartupAuthentication(anySucceeded);

                    if (anySucceeded ||
                        _config.completeBootstrapWhenAuthenticationFails ||
                        !_config.waitForAuthenticationOnStartup)
                    {
                        CompleteBootstrap(anySucceeded);
                    }
                });
            }

            if (!_config.waitForAuthenticationOnStartup)
            {
                CompleteBootstrap(false);
            }
        }

        private void CompleteStartupAuthentication(bool success)
        {
            StartupAuthenticationSucceeded = success;
            StartupAuthenticationCompleted?.Invoke(success);
            _onStartupAuthenticationComplete?.Invoke(success);
        }

        private void CompleteBootstrap(bool authenticationSucceeded)
        {
            if (IsBootstrapComplete)
            {
                return;
            }

            IsBootstrapping = false;
            IsBootstrapComplete = true;
            StartupAuthenticationSucceeded = authenticationSucceeded;
            BootstrapCompleted?.Invoke(authenticationSucceeded);
            _onBootstrapComplete?.Invoke();
            _onBootstrapCompleteWithAuthentication?.Invoke(authenticationSucceeded);
        }
    }
}
