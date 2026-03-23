# Architecture Patterns for Safe Bug Fixing

**Domain:** Unity 6 WebGL golf game -- fixing a Claude-built codebase without introducing regressions
**Researched:** 2026-03-22
**Overall Confidence:** HIGH (based on direct codebase analysis of every component)

## Recommended Architecture for Fixes

The codebase has a well-structured event-driven architecture with two state machines. The fix strategy must preserve this architecture while hardening its weak points. The three bug classes (async void, null references, state machine gaps) are interconnected -- they share root causes in how the codebase was generated rather than designed.

### Core Principle: Harden, Don't Restructure

The architecture is sound. The bugs are implementation-level, not design-level. Every fix should be the minimum change that prevents the runtime failure while preserving the existing pub/sub contract and initialization order.

---

## Component Map and Risk Assessment

### Tier 1: HIGH RISK -- Touch With Extreme Care

These components are event hubs. A broken change here cascades to everything.

| Component | File | Why High Risk | Subscriber Count |
|-----------|------|---------------|------------------|
| **GameManager** | `Assets/Scripts/Core/GameManager.cs` | Central event hub; `OnShotStateChanged` has 6 subscribers, `OnGameOver` has 2, `OnResetToTee` has 1 | 9 total subscriptions |
| **AppManager** | `Assets/Scripts/Core/AppManager.cs` | Singleton; `OnAppStateChanged` has 5 subscribers; controls scene loading | 5 total subscriptions |
| **Bootstrap** | `Assets/Scripts/Multiplayer/Bootstrap.cs` | First code to run (order -100); registers ALL services; `async void Awake()` is the single most dangerous method in the codebase | 0 events, but everything depends on it |

**What NOT to change in these files:**
- Do not change the event signatures (`Action<ShotState>`, `Action<AppState>`, etc.)
- Do not change the `AppManager` singleton pattern or `DontDestroyOnLoad` behavior
- Do not change `Bootstrap`'s execution order (-100) or the mock-first registration strategy
- Do not change `GameManager`'s state guard clauses (the `if (!isActive || currentShotState != ShotState.Ready)` patterns are correct and protective)
- Do not change the `ServiceLocator` API surface (`Register<T>`, `Get<T>`, `Clear`)

### Tier 2: MEDIUM RISK -- Fix Carefully

These components subscribe to Tier 1 events and have their own downstream subscribers.

| Component | File | Why Medium Risk |
|-----------|------|-----------------|
| **BallController** | `Assets/Scripts/Golf/BallController.cs` | Publishes `OnBallLanded`, `OnBallBounced`, `OnBallLaunched` (3 subscribers); subscribes to `GameManager.OnResetToTee` |
| **ScoringManager** | `Assets/Scripts/Environment/ScoringManager.cs` | Publishes 4 events; subscribes to `BallController` (2) and `GameManager` (2); references `PinController` and `Camera.main` |
| **LeaderboardManager** | `Assets/Scripts/Multiplayer/LeaderboardManager.cs` | Has `async void Start()`, `async void HandleBestDistanceUpdated`, `async void HandleGameOver`; subscribes to `ScoringManager` and `GameManager` |
| **ShotInput** | `Assets/Scripts/Golf/ShotInput.cs` | Publishes 6 events; calls `BallController.Launch()` and `GameManager.LaunchShot()` directly (not via events) |

### Tier 3: LOW RISK -- Safe to Fix

These are leaf-node subscribers. They consume events but publish nothing downstream (or only UI callbacks).

| Component | File | Why Low Risk |
|-----------|------|--------------|
| **CameraController** | `Assets/Scripts/Camera/CameraController.cs` | Subscribes to `GameManager` and `BallController`; camera tag lookups may return null |
| **GameplayHUDController** | `Assets/Scripts/UI/GameplayHUDController.cs` | Subscribes to `AppManager`, `GameManager`, `ScoringManager`, `WindSystem` |
| **GameOverController** | `Assets/Scripts/UI/GameOverController.cs` | Has `async void UpdateFinalScore`; subscribes to `AppManager` and `ScoringManager` |
| **PowerMeterController** | `Assets/Scripts/UI/PowerMeterController.cs` | Subscribes to `ShotInput` only |
| **LeaderboardController** | `Assets/Scripts/UI/LeaderboardController.cs` | Subscribes to `AppManager` and `LeaderboardManager` |
| **MainMenuController** | `Assets/Scripts/UI/MainMenuController.cs` | Subscribes to `AppManager` and `NicknamePromptController` |
| **PauseMenuController** | `Assets/Scripts/UI/PauseMenuController.cs` | Subscribes to `AppManager` only |
| **WindSystem** | `Assets/Scripts/Golf/WindSystem.cs` | Subscribes to `GameManager.OnShotStateChanged`; publishes `OnWindChanged` (1 subscriber) |
| **NicknamePromptController** | `Assets/Scripts/UI/NicknamePromptController.cs` | Has fire-and-forget `_ = UpdateDisplayNameAsync()` (properly wrapped in try-catch) |

---

## Event Subscription Graph

This is the complete pub/sub wiring. Every fix must verify it does not break any of these connections.

```
Bootstrap (Awake, order -100)
  --> ServiceLocator.Register<IAuthService>()
  --> ServiceLocator.Register<ILeaderboardService>()
  --> ServiceLocator.Register<IBestScoreService>()

AppManager.OnAppStateChanged
  --> GameManager.HandleAppStateChanged
  --> GameplayHUDController.HandleAppStateChanged
  --> GameOverController.HandleAppStateChanged
  --> LeaderboardController.HandleAppStateChanged
  --> MainMenuController.HandleAppStateChanged
  --> PauseMenuController.HandleAppStateChanged

GameManager.OnShotStateChanged
  --> ShotInput.HandleShotStateChanged
  --> ScoringManager.HandleShotStateChanged
  --> CameraController.HandleShotStateChanged
  --> WindSystem.HandleShotStateChanged
  --> GameplayHUDController.HandleShotStateChanged
  --> LeaderboardManager.HandleShotStateChanged

GameManager.OnGameOver
  --> ScoringManager.HandleGameOver
  --> LeaderboardManager.HandleGameOver (async void)

GameManager.OnResetToTee
  --> BallController.ResetToTee

BallController.OnBallLanded
  --> ScoringManager.HandleBallLanded
  --> CameraController.HandleBallLanded

BallController.OnBallBounced
  --> ScoringManager.HandleBallBounced

BallController.OnBallLaunched
  (no subscribers found -- event exists but appears unused beyond haptics call in Launch())

ScoringManager.OnShotScored
  (no subscribers found in current code)

ScoringManager.OnShotRecorded
  --> GameplayHUDController.HandleShotRecorded

ScoringManager.OnBestDistanceUpdated
  --> LeaderboardManager.HandleBestDistanceUpdated (async void)

ScoringManager.OnAllShotsComplete
  --> GameOverController.HandleAllShotsComplete

ShotInput.OnShotReady
  (no subscribers found -- ShotInput directly calls BallController.Launch instead)

ShotInput.OnMeterPhaseChanged
  --> PowerMeterController.HandlePhaseChanged

ShotInput.OnMeterValueChanged
  --> PowerMeterController.HandleMeterValueChanged

ShotInput.OnAccuracyValueChanged
  --> PowerMeterController.HandleAccuracyValueChanged

ShotInput.OnPowerLocked
  --> PowerMeterController.HandlePowerLocked

ShotInput.OnAimAngleChanged
  (no subscribers found in current code)

WindSystem.OnWindChanged
  --> GameplayHUDController.HandleWindChanged

LeaderboardManager.OnLeaderboardUpdated
  --> LeaderboardController.HandleLeaderboardUpdated

NicknamePromptController.OnPromptDismissed
  --> MainMenuController.OnNicknamePromptDismissed
```

---

## The Three Bug Classes: Detailed Analysis

### Bug Class 1: Async Void Handlers

**Every `async void` method in the codebase, with current protection status:**

| Location | Method | Has try-catch? | Risk |
|----------|--------|----------------|------|
| `Bootstrap.cs:21` | `Awake()` | YES | LOW -- properly wrapped |
| `AppManager.cs:81` | `HandleStateTransition()` | YES | LOW -- properly wrapped |
| `LeaderboardManager.cs:50` | `Start()` | YES | LOW -- properly wrapped |
| `LeaderboardManager.cs:142` | `HandleBestDistanceUpdated()` | YES | LOW -- properly wrapped |
| `LeaderboardManager.cs:156` | `HandleGameOver()` | YES | LOW -- properly wrapped |
| `GameOverController.cs:132` | `UpdateFinalScore()` | YES | LOW -- properly wrapped |

**Verdict:** All `async void` methods already have try-catch wrapping. The PROJECT.md flags "async void event handlers with no exception safety" but the code analysis shows they are wrapped. This may have been fixed in a prior commit, or the PROJECT.md description may be slightly stale.

**However, there is a subtle remaining risk:** The `async void` methods in `LeaderboardManager` are event handlers subscribed to C# events. If the `LeaderboardManager` MonoBehaviour is destroyed mid-await (e.g., during scene transition from Gameplay back to MainMenu), the `catch` block will execute but any subsequent Unity API calls (`Debug.Log`, `StartCoroutine`, accessing `gameObject`) will throw `MissingReferenceException`. The try-catch prevents a hard crash but the error still logs.

**The real async gap is fire-and-forget calls in coroutines:**
- `LeaderboardManager.PollLoop()` line 113: `_ = PollLeaderboardAsync()`
- `LeaderboardManager.RetryLoop()` line 123: `_ = ProcessRetryQueueAsync()`

Both `PollLeaderboardAsync()` and `ProcessRetryQueueAsync()` have internal try-catch blocks, so exceptions within the try body are caught. But if an exception occurs BEFORE reaching the try block (e.g., during method entry or first await setup), it becomes an unobserved Task exception. On WebGL, this can silently corrupt state.

**Safe fix pattern for async void event handlers:**
```csharp
private async void HandleBestDistanceUpdated(float distance)
{
    try
    {
        if (this == null) return;  // destroyed check
        if (leaderboardService == null) return;
        await PostScoreWithRetryAsync(playerId, distance);
        if (this == null) return;  // check again after await
        await PollLeaderboardAsync();
    }
    catch (Exception ex)
    {
        if (this != null)  // only log if not destroyed
            Debug.LogError($"[LeaderboardManager] HandleBestDistanceUpdated failed: {ex}");
    }
}
```

**What NOT to do:** Do not convert these to `async Task` return types -- Unity event handlers and MonoBehaviour lifecycle methods (Awake, Start) must be `void`. The fire-and-forget pattern with `_ = MethodAsync()` used in `LeaderboardManager.PollLoop()` (line 113) and `RetryLoop()` (line 123) is the correct Unity pattern.

### Bug Class 2: Null Reference Chains

**Every `FindFirstObjectByType<T>()` call in the codebase and its null-safety status:**

| Caller | Looking For | Null-checked before use? | Scene |
|--------|-------------|--------------------------|-------|
| `BallController.Start()` | `GameManager` | YES (null-conditional `gameManager?.BallLanded()`) | Gameplay |
| `BallController.Start()` | `WindSystem` | YES (`if (windSystem != null)` in FixedUpdate) | Gameplay |
| `ScoringManager.Start()` | `GameManager` | YES (`if (gameManager != null)`) | Gameplay |
| `ScoringManager.Start()` | `BallController` | YES (`if (ballController != null)`) | Gameplay |
| `ScoringManager.Start()` | `PinController` | YES (`if (pinController != null)`) | Gameplay |
| `ShotInput.Start()` | `GameManager` | YES (`if (gameManager != null)`) | Gameplay |
| `ShotInput.Start()` | `BallController` | PARTIAL -- warns if null, but `LockAccuracyAndFire()` checks before `.Launch()` | Gameplay |
| `CameraController.Start()` | `GameManager` | YES | Gameplay |
| `CameraController.Start()` | `BallController` | YES | Gameplay |
| `WindSystem.Start()` | `GameManager` | YES | Gameplay |
| `GameplayHUDController.Start()` | `GameManager` | YES | Gameplay |
| `GameplayHUDController.Start()` | `ScoringManager` | YES | Gameplay |
| `GameplayHUDController.Start()` | `WindSystem` | YES | Gameplay |
| `GameOverController.Start()` | `ScoringManager` | YES | Gameplay |
| `LeaderboardManager.Start()` | `ScoringManager` | YES | Gameplay |
| `LeaderboardManager.Start()` | `GameManager` | YES | Gameplay |
| `LeaderboardController.Start()` | `LeaderboardManager` | YES | Gameplay |
| `PowerMeterController.Start()` | `ShotInput` | YES | Gameplay |
| `MainMenuController.Start()` | `SettingsController` | YES | MainMenu |
| `MainMenuController.Start()` | `NicknamePromptController` | YES | MainMenu |
| `PauseMenuController.Start()` | `SettingsController` | YES | Gameplay |

**The actual null reference problems are not from FindFirstObjectByType returning null.** They are:

1. **`ScoringManager.HandleBallLanded()` line 179: `Camera.main`** -- `ShotPopup.Create(landPosition + Vector3.up * 1.5f, distanceToPin, Camera.main)` passes `Camera.main` which can be null if no camera is tagged "MainCamera". The `ShotPopup.LateUpdate()` does null-check `targetCamera`, but the `Camera.main` lookup itself could return null in edge cases (scene transition, camera destroyed).

2. **`CameraController.Start()` tag lookups** -- Lines 40-42 use `FindWithTag("TeeCamera")`, `FindWithTag("FlightCamera")`, `FindWithTag("LandingCamera")`. If any tag is missing or the GameObjects are not in the scene, these return null silently. The `SetCameraPriorities` method null-checks each, so this fails silently (no camera switching) rather than crashing. But it means **the game would have no camera transitions with no error message** -- a silent functional failure.

3. **`AppManager.Instance` during scene transitions** -- Multiple components check `AppManager.Instance != null` in `Start()` and `OnDestroy()`. During the `Gameplay -> MainMenu` scene transition, `AppManager.OnDestroy()` sets `Instance = null` (line 176). If another component's `OnDestroy()` runs after AppManager's, it will fail the null check and silently skip unsubscription. However, since AppManager is `DontDestroyOnLoad`, it should only be destroyed when the entire application shuts down. The real risk is **if a duplicate AppManager exists** -- the Awake guard (line 33-38) destroys the duplicate, but any components that cached a reference to the duplicate in the brief window before destruction would hold a dead reference.

4. **`ServiceLocator.Get<T>()` returning null** -- Used in `LeaderboardManager.Start()`, `LeaderboardController.Start()`, `GameOverController.UpdateFinalScore()`, and `NicknamePromptController.Dismiss()`. All callers null-check the result. This is safe.

**Safe fix pattern for the real problems:**
- For `Camera.main`: Cache in `Awake()` or `Start()`, null-check before passing to `ShotPopup.Create()`
- For camera tags: Add `Debug.LogWarning` when a tag lookup returns null so failures are visible
- For `AppManager.Instance` in OnDestroy: The existing pattern is already safe (null-check before unsubscribe)

### Bug Class 3: State Machine Gaps

**The state machines are simple and correct, but have timing gaps:**

**AppManager state machine:**
```
Title -> Instructions -> TransitionToGame -> Playing <-> Paused
                                               |
                                            GameOver -> Leaderboard -> Title
```

**Known issue:** `HandleStateTransition()` is `async void`. When `TransitionToGame` triggers `SceneLoader.LoadSceneAsync("Gameplay")`, the scene load is asynchronous. During the load, `AppManager.CurrentState` is already `TransitionToGame`, but the Gameplay scene's components haven't initialized yet. After the load completes, `SetState(AppState.Playing)` fires `OnAppStateChanged(Playing)`, which GameManager receives in `HandleAppStateChanged`. But GameManager's `Start()` may have already run and checked `AppManager.Instance.CurrentState == Playing` -- which was false at that time (it was `TransitionToGame`).

**The code handles this correctly:** GameManager.Start() checks the current state AND subscribes to future changes. If AppManager is already `Playing` when GameManager starts, it activates. If not, it waits for the event. The state machine is safe.

**Actual state machine risk:** The `ResetAfterDelay` coroutine in `GameManager.BallLanded()` (line 159) waits 3 seconds, then fires `OnResetToTee` and sets state to `Ready`. If the game ends during those 3 seconds (e.g., a rapid scene transition), the coroutine continues on a destroyed object. `StartCoroutine` is tied to the MonoBehaviour -- if the GameObject is destroyed, the coroutine stops. But if only `isActive` is set to false (via `Deactivate()`) while the coroutine is running, the `SetShotState(ShotState.Ready)` call will hit the guard clause `if (!isActive)` and warn. This is correct behavior.

**The unbounded retry queue in LeaderboardManager** is a resource management risk. `retryQueue` is a `Queue<(string, float)>` with no size limit. If the network is down, every best-distance update enqueues a retry. In a 6-shot game, maximum 6 entries would be queued in normal play -- but if the player replays multiple times without network recovery, the queue grows across sessions (it persists as long as the LeaderboardManager MonoBehaviour exists). Cap at 10 entries to prevent iOS Safari memory pressure.

---

## Bootstrap Initialization Order: Why It Matters

The initialization sequence is critical and must not be altered:

```
1. Bootstrap.Awake()  [execution order -100]
   |
   +--> Register mocks synchronously (lines 40-42)
   |    - MockAuthService
   |    - MockLeaderboardService
   |    - PlayerPrefsBestScoreService
   |
   +--> await RegisterServicesAsync()
   |    |
   |    +--> (WebGL only) await UnityServices.InitializeAsync()
   |    +--> (WebGL only) await authService.SignInAsync()
   |    +--> (WebGL only) Re-register real services, overwriting mocks
   |
   +--> ConfigurePlatform()
        - Physics.autoSyncTransforms = false
        - Time.fixedDeltaTime = 0.02f

2. AppManager.Awake()  [default execution order]
   +--> Singleton registration
   +--> DontDestroyOnLoad

3. AppManager.Start()
   +--> SetState(AppState.Title) --> fires OnAppStateChanged

4. [Scene loads Gameplay]

5. GameManager.Start()
   +--> Subscribes to AppManager.OnAppStateChanged
   +--> Checks if already Playing, activates if so

6. All other Start() methods run
   +--> Each does FindFirstObjectByType lookups
   +--> Each subscribes to events
```

**The key safety property:** Mocks are registered synchronously in step 1, before the first `await`. This means that even if UGS initialization fails or takes a long time, any component calling `ServiceLocator.Get<IAuthService>()` in their `Start()` will get a working mock. The `await` only upgrades to real services if possible.

**Do NOT change:**
- The mock-first registration order
- The `[DefaultExecutionOrder(-100)]` on Bootstrap
- The `static bool initialized` guard (prevents double initialization on scene reload)
- The synchronous mock registration happening before the first `await`

---

## Component Lookup Pattern

The codebase uses `FindFirstObjectByType<T>()` exclusively for cross-component references. This is correct for Unity 6 (it replaces the deprecated `FindObjectOfType<T>()`).

**Current pattern (consistent across all components):**
```csharp
private GameManager gameManager;

private void Start()
{
    gameManager = FindFirstObjectByType<GameManager>();
    if (gameManager != null)
    {
        gameManager.OnShotStateChanged += HandleShotStateChanged;
    }
}

private void OnDestroy()
{
    if (gameManager != null)
    {
        gameManager.OnShotStateChanged -= HandleShotStateChanged;
    }
}
```

**The `CameraController` is the exception** -- it mixes `FindFirstObjectByType` (for GameManager, BallController) with `FindWithTag` (for Cinemachine cameras). This is intentional: the cameras are scene-specific tagged GameObjects, not MonoBehaviour singletons.

**Safe fix approach for lookups:**
- Do NOT switch from `FindFirstObjectByType` to `FindWithTag` or vice versa -- keep the existing pattern
- Add `Debug.LogWarning` only where a null result is a genuine problem (not where it's expected, like in MainMenu scene where GameManager doesn't exist)
- Never add `[SerializeField]` wiring to replace runtime lookups -- the CLAUDE.md explicitly forbids this for component references

---

## Scene Boundary: What Lives Where

Understanding which components exist in which scene prevents fixes from breaking cross-scene assumptions.

**MainMenu scene objects:**
- AppManager (DontDestroyOnLoad -- persists)
- Bootstrap (DontDestroyOnLoad via `static bool initialized` -- runs once)
- MainMenuController
- NicknamePromptController
- SettingsController
- LeaderboardController (for standalone leaderboard viewing)

**Gameplay scene objects:**
- GameManager
- BallController
- ScoringManager
- PinController
- CourseBuilder
- WindSystem
- ShotInput
- CameraController (+ TeeCamera, FlightCamera, LandingCamera tagged objects)
- LeaderboardManager
- GameplayHUDController
- GameOverController
- PauseMenuController
- PowerMeterController

**Cross-scene dependency:** AppManager persists from MainMenu into Gameplay. All Gameplay components check `AppManager.Instance != null` defensively. When transitioning back to MainMenu, all Gameplay objects are destroyed (standard Unity scene loading).

---

## Patterns to Follow When Fixing

### Pattern 1: Null-Guard-Then-Subscribe

**What:** Every event subscription must be preceded by a null check on the publisher, and every unsubscription must be preceded by the same null check.

**Example (already used everywhere):**
```csharp
private void Start()
{
    gameManager = FindFirstObjectByType<GameManager>();
    if (gameManager != null)
    {
        gameManager.OnShotStateChanged += HandleShotStateChanged;
    }
}

private void OnDestroy()
{
    if (gameManager != null)
    {
        gameManager.OnShotStateChanged -= HandleShotStateChanged;
    }
}
```

**When fixing:** If you add a new event subscription, always add the matching unsubscription in `OnDestroy()` with the null guard. If you see a subscription without a matching unsubscription, add the unsubscription.

### Pattern 2: Async Void With Destroyed-Object Guard

**What:** Any `async void` method on a MonoBehaviour must check `this == null` after every `await` point, because the object may have been destroyed during the await.

**When:** In `LeaderboardManager`'s event handlers and `GameOverController.UpdateFinalScore()`.

```csharp
private async void HandleGameOver(int shots)
{
    try
    {
        isPolling = false;
        if (this == null) return;
        await PollLeaderboardAsync();
    }
    catch (Exception ex)
    {
        if (this != null)
            Debug.LogError($"[LeaderboardManager] HandleGameOver failed: {ex}");
    }
}
```

### Pattern 3: Fire-and-Forget With Internal Exception Handling

**What:** When calling an async method from a synchronous context (like a coroutine or Update), use `_ = MethodAsync()` and ensure the async method has its own try-catch.

**Already correctly used in:**
- `LeaderboardManager.PollLoop()` line 113: `_ = PollLeaderboardAsync()`
- `LeaderboardManager.RetryLoop()` line 123: `_ = ProcessRetryQueueAsync()`
- `NicknamePromptController.Dismiss()` line 141: `_ = UpdateDisplayNameAsync(nickname)`

**Verify:** `PollLeaderboardAsync()` and `ProcessRetryQueueAsync()` both have internal try-catch. Correct.

### Pattern 4: Bounded Resource Queue

**What:** Any queue that grows from external input (network failures, user actions) must have a maximum size.

```csharp
private const int MaxRetryQueueSize = 10;

private void EnqueueRetry(string id, float dist)
{
    if (retryQueue.Count >= MaxRetryQueueSize)
    {
        retryQueue.Dequeue(); // drop oldest
        Debug.LogWarning("[LeaderboardManager] Retry queue full, dropping oldest entry");
    }
    retryQueue.Enqueue((id, dist));
}
```

---

## Anti-Patterns to Avoid When Fixing

### Anti-Pattern 1: Converting async void to async Task in Event Handlers

**What:** Changing `private async void HandleBestDistanceUpdated(float distance)` to return `Task`.

**Why bad:** C# events with `Action<float>` signatures require `void` return type. You would also need to change the event declaration from `Action<float>` to `Func<float, Task>`, which changes the subscription API for every subscriber. Massive ripple effect.

**Instead:** Keep `async void`, add destroyed-object guards after awaits.

### Anti-Pattern 2: Adding SerializeField References to Replace Runtime Lookups

**What:** Adding `[SerializeField] private GameManager gameManager;` and wiring it in the Inspector.

**Why bad:** CLAUDE.md explicitly states: "Reserve `[SerializeField]` only for assets (ScriptableObjects, Prefabs, AudioClips)." Inspector-wired references are fragile across scene changes and are easy to miss in reviews. The runtime lookup pattern is intentional and consistent.

**Instead:** Keep `FindFirstObjectByType<T>()` in `Start()`, add null warnings where missing.

### Anti-Pattern 3: Adding Centralized Error Handling or Exception Middleware

**What:** Creating a global exception handler, wrapping all event dispatches in try-catch, or adding an error reporting service.

**Why bad:** Increases coupling, adds complexity, and may mask bugs rather than fix them. The per-component try-catch pattern is correct for this codebase size.

**Instead:** Fix each component's error handling individually. The existing pattern of component-tagged logging (`[ComponentName]`) is sufficient.

### Anti-Pattern 4: Changing Event Firing Order or Adding Events

**What:** Adding new events to GameManager, or changing when existing events fire.

**Why bad:** The event graph above shows 20+ subscriptions. Adding or reordering events can break subscribers that depend on the current order (e.g., `ScoringManager.HandleBallLanded` must run before `GameManager.BallLanded` is called by `BallController`, because `BallController.BallStopped()` fires `OnBallLanded` first, then calls `gameManager?.BallLanded()`).

**Instead:** Fix bugs within the existing event handlers. If new behavior is needed, add it to an existing handler rather than creating a new event.

---

## Scalability Considerations

Not relevant for this milestone -- this is a single-player WebGL game with 6 shots per session. No scaling concerns. The focus is purely on runtime correctness.

---

## Fix Ordering Recommendation

Based on the dependency analysis above, fixes should be ordered from lowest risk to highest:

1. **Tier 3 components first** (UI controllers) -- lowest blast radius, independent of each other
2. **Tier 2 components next** (ScoringManager, LeaderboardManager, CameraController) -- medium blast radius, but changes are isolated to their own event handling
3. **Tier 1 components last** (GameManager, AppManager, Bootstrap) -- highest blast radius, only if actually needed

Within each tier, fix in this order:
1. Add null-guard warnings (non-breaking, diagnostic)
2. Add destroyed-object guards to async void methods (non-breaking, protective)
3. Fix actual logic bugs (state machine corrections, if any found)

---

## Complete Async Method Inventory

For reference when fixing, every async method in the codebase:

| Method | Returns | Call Pattern | File |
|--------|---------|--------------|------|
| `Bootstrap.Awake` | void | lifecycle | Bootstrap.cs |
| `Bootstrap.RegisterServicesAsync` | Task | awaited in Awake | Bootstrap.cs |
| `AppManager.HandleStateTransition` | void | called from SetState | AppManager.cs |
| `SceneLoader.LoadSceneAsync` | Task | awaited in HandleStateTransition | SceneLoader.cs |
| `LeaderboardManager.Start` | void | lifecycle | LeaderboardManager.cs |
| `LeaderboardManager.HandleBestDistanceUpdated` | void | event handler | LeaderboardManager.cs |
| `LeaderboardManager.HandleGameOver` | void | event handler | LeaderboardManager.cs |
| `LeaderboardManager.PostScoreWithRetryAsync` | Task | awaited | LeaderboardManager.cs |
| `LeaderboardManager.ProcessRetryQueueAsync` | Task | fire-and-forget | LeaderboardManager.cs |
| `LeaderboardManager.PollLeaderboardAsync` | Task | awaited and fire-and-forget | LeaderboardManager.cs |
| `GameOverController.UpdateFinalScore` | void | called from coroutine | GameOverController.cs |
| `NicknamePromptController.UpdateDisplayNameAsync` | Task | fire-and-forget | NicknamePromptController.cs |

---

## Sources

- Direct codebase analysis of all 25+ C# source files
- Unity 6 documentation for `FindFirstObjectByType`, `DefaultExecutionOrder`, `DontDestroyOnLoad`
- [Unity Manual: Web performance considerations](https://docs.unity3d.com/Manual/webgl-performance.html)
- [Unity Manual: Debug and troubleshoot Web builds](https://docs.unity3d.com/Manual/webgl-debugging.html)
- [Unity Manual: Rigidbody interpolation](https://docs.unity3d.com/Manual/rigidbody-interpolation.html)
- Confidence: HIGH -- all findings verified against actual source code

---

*Architecture analysis: 2026-03-22*
