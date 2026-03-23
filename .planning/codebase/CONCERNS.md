# Codebase Concerns

**Analysis Date:** 2026-03-22

## Async Void Event Handlers

**Fire-and-forget async operations without exception safety:**
- Issue: Multiple event handlers are declared as `async void`, which means exceptions thrown inside them are not caught at the call site and will crash the game with unhandled exceptions
- Files:
  - `Assets/Scripts/UI/GameOverController.cs:132` - `UpdateFinalScore()`
  - `Assets/Scripts/Core/AppManager.cs:81` - `HandleStateTransition()`
  - `Assets/Scripts/Multiplayer/Bootstrap.cs:20` - `Awake()`
  - `Assets/Scripts/Multiplayer/LeaderboardManager.cs:50` - `Start()`
  - `Assets/Scripts/Multiplayer/LeaderboardManager.cs:142` - `HandleBestDistanceUpdated()`
  - `Assets/Scripts/Multiplayer/LeaderboardManager.cs:156` - `HandleGameOver()`
- Impact: If any async operation inside these methods fails (network request, service lookup, etc.), the exception propagates to Unity's event system and crashes the game. No error recovery possible.
- Fix approach: Wrap all async work in try-catch blocks inside the async void methods. Only use `async void` for event handlers; ensure all awaited operations have error handling before returning.

## Potential Null Reference: Camera.main

**Unchecked null dereference:**
- Issue: `ScoringManager.cs:179` calls `Camera.main` directly without null checking before passing to `ShotPopup.Create()`
- Files: `Assets/Scripts/Environment/ScoringManager.cs:179`
- Impact: If the main camera is not found or set up, `Camera.main` returns null. This is passed to `ShotPopup.Create()` which then tries to use it in `LateUpdate()`. While the code checks `if (targetCamera != null)`, the null is still stored, causing silent failure of billboard rotation.
- Fix approach: Add null check: `if (Camera.main != null) ShotPopup.Create(..., Camera.main)` or cache `Camera.main` in `Awake()` of the component that needs it.

## Runtime Component Lookups Without Fallback

**FindFirstObjectByType calls may return null:**
- Issue: Numerous start/awake methods find components at runtime without handling the null case. If a scene is loaded incorrectly or components are missing, the game silently continues with null references.
- Files:
  - `Assets/Scripts/Camera/CameraController.cs:37-40` - Missing GameManager/BallController handled with null coalesce but TeeCamera lookup can fail
  - `Assets/Scripts/Golf/BallController.cs:52-53` - GameManager/WindSystem not null-checked
  - `Assets/Scripts/Golf/ShotInput.cs:89-90` - GameManager/BallController not null-checked (though warning is logged)
  - `Assets/Scripts/UI/GameplayHUDController.cs:38-40` - Multiple finds not null-checked
  - `Assets/Scripts/UI/LeaderboardController.cs:38` - LeaderboardManager not null-checked
  - `Assets/Scripts/Multiplayer/LeaderboardManager.cs:54-55` - ScoringManager/GameManager not null-checked
- Impact: Silent failures in gameplay. Components try to call methods on null objects, causing NullReferenceException at runtime (e.g., `gameManager?.BallLanded()` hides errors when gameManager is null).
- Fix approach: Either (1) Add explicit error logging and return early if critical components not found, or (2) Use tags consistently with null-checks, or (3) Add scene validation in editor.

## Unvalidated Best Score State

**float.MaxValue sentinel value creates logic gaps:**
- Issue: `GameOverController.cs:159` uses `float.MaxValue / 2f` as a threshold to distinguish "no score" from "valid score", which is fragile and confusing
- Files: `Assets/Scripts/UI/GameOverController.cs:159`, `Assets/Scripts/Environment/ScoringManager.cs:20`, `Assets/Scripts/Multiplayer/PlayerPrefsBestScoreService.cs`
- Impact: If a player somehow ends up with a score between `float.MaxValue / 2f` and `float.MaxValue`, it will be treated as "no best score". The logic is also non-obvious to maintainers.
- Fix approach: Replace sentinel value with a nullable `float?` or a dedicated struct like `ScoreState { HasScore, Score }`. Or document the invariant clearly.

## Memory Leak Potential: UIElements Schedule

**Scheduled callbacks not guaranteed to clean up:**
- Issue: `GameOverController.cs:250` and `PowerMeterController.cs` use `schedule.Execute(...).StartingIn()` for delayed UI updates, which schedules callbacks but doesn't verify they cancel if the element is destroyed
- Files: `Assets/Scripts/UI/GameOverController.cs:250-254`, `Assets/Scripts/UI/PowerMeterController.cs:26` (feedbackHideSchedule)
- Impact: If a scheduled callback references a destroyed VisualElement (e.g., user navigates away before delay), it may cause errors or memory leaks. In `PowerMeterController`, `feedbackHideSchedule` is never explicitly cancelled.
- Fix approach: Store `IVisualElementScheduledItem` and call `Pause()` or save reference for cleanup in `OnDestroy()`. Verify UIElements properly garbage-collect scheduled items.

## Leaderboard Retry Queue Unbounded Growth

**Retry queue can grow indefinitely on persistent network failure:**
- Issue: `LeaderboardManager.cs:29-30` maintains a retry queue that Enqueues failed posts but never has a max size or time-to-live
- Files: `Assets/Scripts/Multiplayer/LeaderboardManager.cs:29-30, 136-140, 177-180`
- Impact: If network is down for an extended period and the player keeps making shots, retry queue grows unbounded in memory. Extremely bad on WebGL/mobile with limited RAM.
- Fix approach: Add max queue size (e.g., 10), or add timestamp to retries and drop entries older than X minutes, or stop accepting new retries after N failures.

## Cloud Code Submission Limit Not Enforced Client-Side

**Client trusts server submission count entirely:**
- Issue: `Assets/CloudCode/validate-and-post-score.js` enforces MAX_SUBMISSIONS=6 server-side, but client has no visual feedback or client-side guard
- Files: `Assets/CloudCode/validate-and-post-score.js:6, 32-34`, `Assets/Scripts/Multiplayer/LeaderboardManager.cs`
- Impact: Player makes a 7th shot, submits, and gets rejection with no UI error message. Player sees no reason why score didn't post. No meter or UI warning prevents the attempt.
- Fix approach: After 6 submissions, either (1) disable leaderboard posting in UI with a message, or (2) catch and display the rejection error with a user-friendly message in GameOverController.

## Async Void Exception Handling Invisible

**Fire-and-forget async in Update() and event handlers:**
- Issue: `LeaderboardManager.cs:113` uses `_ = PollLeaderboardAsync()` (fire-and-forget) in a while loop with no exception handling inside the async method
- Files: `Assets/Scripts/Multiplayer/LeaderboardManager.cs:108-114, 117-126`
- Impact: If `PollLeaderboardAsync()` throws before the try-catch (line 201), the exception is swallowed silently. Only exceptions inside the try-catch are logged.
- Fix approach: Wrap the entire async method body in try-catch, or use `async Task` instead of fire-and-forget, then await with proper error handling.

## Missing Input Validation on Power Meter

**Power meter oscillator not bounds-checked:**
- Issue: `ShotInput.cs:37-38` creates oscillators with `-1 to 1` range for accuracy, but no clamp is applied to oscillator output before use
- Files: `Assets/Scripts/Golf/ShotInput.cs:37-38, 271`
- Impact: If oscillator math diverges (floating point error), accuracy deviation could exceed maxAccuracyDeviation, causing shots beyond intended aim range.
- Fix approach: Clamp accuracy meter output: `float accuracy = Mathf.Clamp(accuracyMeter.Value, -1f, 1f)` before multiplying by `maxAccuracyDeviation`.

## Ball Landing Detection Fragile

**Fixed velocity threshold for landing may not work with varied terrain:**
- Issue: `BallController.cs:102` checks `linearVelocity.magnitude < 0.2f && angularVelocity.magnitude < 0.1f` to detect landing. No terrain slope compensation.
- Files: `Assets/Scripts/Golf/BallController.cs:100-106`
- Impact: On sloped terrain, ball may stop moving slightly above ground, or it may continue rolling very slowly and register landing late, affecting scoring accuracy and shot popup timing.
- Fix approach: Verify landing by checking raycast below ball or use rigidbody sleep state, or add terrain-aware threshold.

## Test Coverage Gap: Cloud Code Only

**No C# unit tests for game logic:**
- Issue: Only Cloud Code (`Assets/CloudCode/validate-and-post-score.test.js`) has test coverage. No tests for ScoringManager, GameManager, shot physics, UI transitions, or leaderboard retry logic.
- Files: No test files for `Assets/Scripts/`
- Risk: Scoring calculation bugs, state machine transitions, and physics calibration are not verified. Regression potential is high.
- Priority: High

## Weak Error Messages in LeaderboardManager

**Generic "using cached data" message hides real failures:**
- Issue: `LeaderboardManager.cs:209` logs "using cached data" but doesn't distinguish between network error, auth failure, rate limit, or malformed response
- Files: `Assets/Scripts/Multiplayer/LeaderboardManager.cs:198-212`
- Impact: Hard to debug why leaderboard isn't updating. Player sees no error. Devs can't triage without detailed logs.
- Fix approach: Catch specific exception types and log context: `ex is HttpRequestException`, `ex is TimeoutException`, etc.

## ServiceLocator.Get() Can Return Null

**No fallback when service not registered:**
- Issue: `GameOverController.cs:143`, `LeaderboardController.cs:40-41` call `ServiceLocator.Get<T>()` which may return null if service not registered
- Files: `Assets/Scripts/UI/GameOverController.cs:143-148`, `Assets/Scripts/UI/LeaderboardController.cs:40-41`
- Impact: Null checks are in place, but silent disabling of features (best score label hidden, rank not shown) with no user-facing error or warning.
- Fix approach: Log warning when service not found, or ensure Bootstrap always registers a fallback mock service.

## State Machine Implicit Transitions

**GameManager state transitions not fully validated:**
- Issue: `GameManager.cs:109-122` allows state changes without checking all preconditions. `SetShotState()` doesn't validate that transition is legal (e.g., can't go from Idle to Landed).
- Files: `Assets/Scripts/Core/GameManager.cs:109-137`
- Impact: Logic errors in callers (ShotInput, BallController) can corrupt game state. No assertion prevents invalid transitions.
- Fix approach: Add whitelist of valid transitions or use enum-based state machine with compile-time checks.

## Physics Framerate Hardcoded

**Fixed 50Hz physics may not match device refresh rate:**
- Issue: `Bootstrap.cs:87` hardcodes `Time.fixedDeltaTime = 0.02f` (50Hz) regardless of device refresh rate or build profile
- Files: `Assets/Scripts/Multiplayer/Bootstrap.cs:87`
- Impact: On 60Hz displays, physics runs slower than render, causing jitter. On 120Hz devices, frame skips. No adaptive adjustment.
- Fix approach: Query device refresh rate and match to nearest common physics rate (60Hz for most mobile), or make configurable in settings.

## Nickname Persistence Not Validated

**Nickname stored in PlayerPrefs without sanitization:**
- Issue: `UgsAuthService.cs:39` and `Bootstrap.cs:52` read nickname from PlayerPrefs and update UGS display name without validation
- Files: `Assets/Scripts/Multiplayer/UgsAuthService.cs:39-45`, `Assets/Scripts/Multiplayer/Bootstrap.cs:52-56`
- Impact: Extremely long or malicious strings could be sent to UGS, potentially causing API errors or display issues on leaderboard.
- Fix approach: Validate nickname length (max 32 chars), alphanumeric + space only, and trim whitespace before use.

---

*Concerns audit: 2026-03-22*
