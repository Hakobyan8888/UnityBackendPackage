using System;

namespace BackendPackage.Runtime.Core
{
    public interface IBackendPackageAuthenticator
    {
        bool IsAuthenticated { get; }

        void Authenticate(Action<bool> onComplete = null, bool? silentOverride = null);
    }
}
