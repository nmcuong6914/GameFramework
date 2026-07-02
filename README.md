# BlockSort Game Framework

## Overview

This framework is a production-ready, modular architecture designed for mobile (Android/iOS) and desktop Unity games. It brings together core utility libraries, SDK integrations, and gameplay systems using a decoupled, event-driven, and service-oriented structure.

---

## 🚀 Getting Started & Initialization

The entry point of your game should be a bootstrapping scene (e.g., `StartupScene`) containing the **`GameInitFlow`** component.

### 1. The Bootstrapper: `GameInitFlow`

The `GameInitFlow` component (located in `Runtime/Core/Bootstrap/GameInitFlow.cs`) coordinates the initialization of all systems in the correct order, resolving asynchronous dependencies via `UniTask`.

#### Initialization Phases:
1. **Core Service Registration (Awake):**
   Registers lightweight, non-dependent services directly in the `ServiceLocator`:
   - `SignalBus` (Event Bus)
   - `TimerManager` (Async countdowns)
   - `ActionViewManager` (Decoupled gameplay-to-view pipeline)
   - Monobehaviour-based services (like `AssetManager`, `PoolManager`, `PopupManager`, `SoundManager`, etc.)
2. **Device FPS Profiling:**
   Detects device system RAM and locks the target frame rate to keep low-end mobile devices cool:
   - `< 2GB RAM` $\rightarrow$ 30 FPS
   - `< 4GB RAM` $\rightarrow$ 45 FPS
   - `4GB+ RAM` $\rightarrow$ 60 FPS
3. **Async Services Bootstrapping (Start):**
   Runs asynchronous initialization routines sequentially:
   - **Firebase SDK** initialization $\rightarrow$ **Facebook SDK** initialization.
   - **Analytics & RemoteConfig** loading (checks for version updates).
   - **PlayerDataManager** (loads inventory, currencies, and save file).
   - **Dependent features** (like `LivesManager`, `AdsManager`, and `PurchaseManager`).
4. **Transition to Gameplay:**
   Loads the target scene (e.g., `HomeScene` or `GameplayScene` depending on user level or status).

---

## 🛠️ Folder 1: Core & Architecture (`Runtime/Core/`)

These are foundational scripts with zero external dependencies:

*   **`Dependency/` (`ServiceLocator`):**
    A type-safe service registry. Supports lazy factory bindings and automatic constructor injection (Auto-Wiring).
    ```csharp
    // Registering a service
    ServiceLocator.Register<IMyService>(new MyService());
    // Resolving a service
    var myService = ServiceLocator.Resolve<IMyService>();
    ```
*   **`SignalSystem` (`SignalBus`):**
    A lightweight, GC-free type-safe event bus.
    ```csharp
    // Subscribe
    SignalBus.Subscribe<LevelCompleteSignal>(OnLevelCompleted);
    // Fire
    SignalBus.Fire(new LevelCompleteSignal(levelIndex));
    ```
*   **`SaveSystem`:**
    Atomic JSON saving for Android, iOS, and PC. Writes to a temporary file first before replacing the main save to prevent data corruption. Automatically maintains backups.
*   **`Timer`:**
    Async UniTask-based countdown/countup timer.
*   **`Pool`:**
    Preload and recycle GameObjects to prevent GC spikes.
*   **`ActionView`:**
    Decouples gameplay logic (`GameAction`) from visual feedback (`ViewResponse`) using async task pipelines.

---

## 📱 Folder 2: SDK & Mobile Services (`Runtime/SDKs/`)

This folder manages integrations for mobile-ready features:

*   **`RemoteConfig` (Generic):**
    Loads a unified JSON structure from Firebase Remote Config, parses it into a strongly-typed data class, caches it offline, and validates the app version.
*   **`Ads` (IronSource LevelPlay):**
    Shows Interstitial and Rewarded ads, tracks cooldowns, and automates loading.
*   **`Analytics` (Multiplexer):**
    Allows sending logs simultaneously to Firebase, Facebook, and Amplitude with a single call.
*   **`Purchase` (Unity IAP):**
    Decoupled IAP manager tracking transactions, validating receipts, and granting rewards.
*   **`Notification`:**
    Schedules local push notifications for retention reminders.

---

## 🎮 Folder 3: Game Systems (`Runtime/Systems/`)

Common mechanics for level-based games:

*   **`Shop`:**
    Configure purchasable shop packages (IAP, coin-based, ad-supported, or free) with loot rewards.
*   **`Popup`:**
    Manage popup UI stack utilizing Unity Addressables (`AssetReference`) to keep startup memory low.
*   **`Inventory & Currency`:**
    Generic wallet tracking gold, gems, and consumables with consumption validations.
*   **`Rewards`:**
    Set level-based completion rewards.
*   **`Audio`:**
    Decoupled background music (BGM) and sound effects (SFX) playing via ScriptableObjects.
*   **`VFX`:**
    Play particles/effects linked to the `PoolManager` with automatic pool-return timeouts.
