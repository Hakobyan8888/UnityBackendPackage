# Backend Package

This folder is a standalone backend package candidate. The core package imports without vendor SDKs; SDK integrations are opt-in through scripting define symbols.

Included modules:

- `Runtime/Config`
- `Runtime/Core`
- `Runtime/Auth`
- `Runtime/IAP`
- `Runtime/Analytics/TikTok`
- `Runtime/Ads/LevelPlayAdsAdapter`
- `Plugins/iOS`
- `Editor`

Current goals:

- Generic bootstrapper and config asset
- Android/iOS auth + leaderboard service wrapper
- Generic Unity IAP manager
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

Notes:

- `BackendAuthService` expects Google Play Games when Android auth is enabled, and the included native Game Center bridge on iOS.
- `BackendTikTokManager` expects the TikTok Unity SDK to be present in the project before `BACKENDPACKAGE_ENABLE_TIKTOK` is enabled.
- `BackendIapManager` expects Unity IAP v5 to be installed and configured before `BACKENDPACKAGE_ENABLE_IAP` is enabled. Local receipt validation is intentionally not included.
- `BackendLevelPlayAdsManager` expects the full Unity LevelPlay/ironSource SDK to be installed before `BACKENDPACKAGE_ENABLE_LEVELPLAY` is enabled. Do not install only `com.unity.services.levelplay`; that editor package can throw `IronSourceSdk is not installed` until the native SDK is present.
- The package includes its own custom iOS Game Center native bridge files under `Plugins/iOS`.

Bundled in this package:

- Generic bootstrapper and config asset
- Generic auth service wrapper
- Generic TikTok manager
- Generic Unity IAP manager
- Generic LevelPlay ads wrapper
- Custom iOS Game Center native bridge files

Required external dependencies:

- Google Play Games Unity plugin
- TikTok Unity SDK
- Unity IAP package
- Unity LevelPlay SDK

Assembly setup:

- `Runtime/Config` and `Runtime/Core` are always available.
- `Runtime/Auth`, `Runtime/IAP`, and `Runtime/Ads/LevelPlayAdsAdapter` use `asmdef` define constraints so they do not compile until explicitly enabled.
- `Runtime/Analytics/TikTok` is wrapped in `#if BACKENDPACKAGE_ENABLE_TIKTOK` because some TikTok SDK imports do not expose asmdefs.

Editor helpers:

- `Backend Package/Create Test Bootstrap In Scene`
- `Backend Package/Setup Integrations`
