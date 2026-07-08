using System.Collections.Generic;

namespace BackendPackage.Runtime.Core
{
    public interface IBackendAnalyticsProvider
    {
        bool IsInitialized { get; }
        bool IsDataCollectionStarted { get; }

        void StartDataCollection();
        void StopDataCollection();
        void TrackEvent(string eventName, Dictionary<string, object> parameters = null);
        void Flush();
    }
}
