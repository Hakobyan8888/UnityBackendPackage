# Backend Package

This folder is a standalone backend package candidate. The core package imports without vendor SDKs; SDK integrations are opt-in through scripting define symbols.

Included modules:

- `Runtime/Config`
- `Runtime/Core`
- `Runtime/Auth`
- `Runtime/IAP`
- `Runtime/Analytics/TikTok`
- `Runtime/Analytics/UnityAnalyticsAdapter`
- `Runtime/Ads/LevelPlayAdsAdapter`
- `Plugins/iOS`
- `Editor`

Current goals:

- Generic bootstrapper and config asset
- Android/iOS auth + leaderboard service wrapper
- Generic Unity IAP manager
- Generic analytics facade + optional Unity Analytics adapter
- Generic TikTok App Events manager

Package layout:

- `package.json` lets the folder work as a UPM-style package root.
- `Runtime/Core` contains the generic bootstrapper and `IBackendPackageInitializable`.
- Optional integrations compile only after their SDK is installed and their define symbol is enabled.

Editor setup:

- `Backend Package/Setup Integrations`
- A one-time import prompt asks whether to open the setup window.
- Installs UPM packages where they are self-contained, such as Unity IAP.
- Opens external SDK pages for SDKs that are not reliably installable from the Unity registry.
- Enables or disables the integration symbols for the selected build target group.

Integration symbols:

- `BACKENDPACKAGE_ENABLE_AUTH`
- `BACKENDPACKAGE_ENABLE_IAP`
- `BACKENDPACKAGE_ENABLE_LEVELPLAY`
- `BACKENDPACKAGE_ENABLE_TIKTOK`
- `BACKENDPACKAGE_ENABLE_UNITY_ANALYTICS`

Notes:

- `BackendAuthService` expects Google Play Games when Android auth is enabled, and the included native Game Center bridge on iOS.
- `BackendTikTokManager` expects the TikTok Unity SDK to be present in the project before `BACKENDPACKAGE_ENABLE_TIKTOK` is enabled.
- `BackendUnityAnalyticsManager` expects Unity Analytics to be installed and Unity Gaming Services to be connected before `BACKENDPACKAGE_ENABLE_UNITY_ANALYTICS` is enabled. Data collection is not started by default unless `startDataCollectionOnInitialize` is enabled, and events are ignored until data collection starts.
- `BackendIapManager` expects Unity IAP v5 to be installed and configured before `BACKENDPACKAGE_ENABLE_IAP` is enabled. Local receipt validation is intentionally not included.
- `BackendLevelPlayAdsManager` expects the full Unity LevelPlay/ironSource SDK to be installed before `BACKENDPACKAGE_ENABLE_LEVELPLAY` is enabled. Do not install only `com.unity.services.levelplay`; that editor package can throw `IronSourceSdk is not installed` until the native SDK is present.
- The package includes its own custom iOS Game Center native bridge files under `Plugins/iOS`.

Bundled in this package:

- Generic bootstrapper and config asset
- Generic auth service wrapper
- Generic analytics facade
- Generic Unity Analytics manager
- Generic TikTok manager
- Generic Unity IAP manager
- Generic LevelPlay ads wrapper
- Custom iOS Game Center native bridge files

Required external dependencies:

- Google Play Games Unity plugin
- TikTok Unity SDK
- Unity Analytics package
- Unity IAP package
- Unity LevelPlay SDK

Assembly setup:

- `Runtime/Config` and `Runtime/Core` are always available.
- `Runtime/Auth`, `Runtime/IAP`, `Runtime/Analytics/UnityAnalyticsAdapter`, and `Runtime/Ads/LevelPlayAdsAdapter` use `asmdef` define constraints so they do not compile until explicitly enabled.
- `Runtime/Analytics/TikTok` uses an asmdef define constraint and reflection so it can compile as a Git/UPM package even when the TikTok SDK does not expose asmdefs.

Analytics usage:

- Use `BackendAnalytics.TrackEvent(...)` from game code.
- Add `BackendUnityAnalyticsManager` to the bootstrap object when Unity Analytics is enabled.
- Call `BackendAnalytics.StartDataCollection()` after your consent/privacy flow, unless your app configuration allows data collection on startup.

Editor helpers:

- `Backend Package/Create Test Bootstrap In Scene`
- `Backend Package/Setup Integrations`
