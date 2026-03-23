# Architecture

**Analysis Date:** 2026-03-22

## Pattern Overview

**Overall:** Event-driven layered architecture with two state machines (application-level and gameplay-level).

**Key Characteristics:**
- Two-tier state management: `AppManager` (screen flow) and `GameManager` (shot loop)
- Event-based inter-component communication (C# events, no SendMessage)
- Service locator dependency injection for async services (auth, leaderboards)
- Separation of concerns: Core managers → Domain logic → UI Controllers
- Assembly definitions per folder for compile-time isolation and dependency clarity

## Layers

**Core/State Management Layer:**
- Purpose: Global application state, scene transitions, and gameplay loop coordination
- Location: `Assets/Scripts/Core/`
- Contains: `AppManager`, `GameManager`, `ServiceLocator`, `SceneLoader`, state enums, heuristics managers
- Depends on: Nothing (foundation layer)
- Used by: All other layers via event subscriptions and service lookups

**Domain Logic Layer:**
- Purpose: Physics simulation, input processing, scoring, and wind simulation
- Location: `Assets/Scripts/Golf/`, `Assets/Scripts/Environment/`
- Contains: Ball mechanics, shot input handling, scoring calculations, wind generation
- Depends on: Core layer (for state and events)
- Used by: UI layer, multiplayer layer

**Multiplayer/Service Layer:**
- Purpose: Authentication, leaderboard operations, score persistence
- Location: `Assets/Scripts/Multiplayer/`
- Contains: Service interfaces, implementations (UGS and mock), service registration
- Depends on: Core layer (ServiceLocator), Environment (ScoringManager)
- Used by: UI controllers, orchestration via LeaderboardManager

**UI Layer:**
- Purpose: Screen rendering, user input capture, state visualization
- Location: `Assets/Scripts/UI/`
- Contains: UI Toolkit controllers for each screen (menu, HUD, game over, leaderboard, etc.)
- Depends on: Core (AppManager, state), Domain (GameManager, ScoringManager, WindSystem), Multiplayer (auth, leaderboards)
- Used by: End user interactions

**Camera/Audio/Presentation Layer:**
- Purpose: Cinemachine camera switching, haptics, audio mixing
- Location: `Assets/Scripts/Camera/`, `Assets/Scripts/Audio/`
- Contains: Camera state-based switching, audio controller, haptics triggering
- Depends on: Core (GameManager state), Domain (BallController position)
- Used by: No downstream dependencies

## Data Flow

**Game Initialization:**

1. `Bootstrap` (Awake, execution order -100) registers all services before `AppManager` awakens
   - Conditional: UGS services in WebGL, mocks in editor or on failure
   - `ServiceLocator.Register()` stores implementations
2. `AppManager.Start()` initializes with Title state (or debug override)
3. Scene loads based on `AppState` transitions
4. On `Gameplay` scene load, `GameManager`, `BallController`, `ScoringManager`, `LeaderboardManager` all initialize via `Start()`

**Shot Loop (Primary Data Flow):**

1. **Ready State (Input Phase):**
   - `ShotInput.Update()` processes touch/mouse input
   - `ShotInput` maintains power meter (oscillating 0-1), accuracy meter (-1 to 1), aim angle (-45 to 45)
   - Events fired: `OnMeterValueChanged`, `OnAccuracyValueChanged`, `OnAimAngleChanged` → UI subscribes
   - UI: `PowerMeterController`, `GameplayHUDController` update visuals in response

2. **Power Lock → Accuracy Phase:**
   - User clicks second time: `ShotInput.OnPowerLocked` fired
   - Power meter freezes, accuracy meter becomes active
   - Events: `OnMeterPhaseChanged`, `OnAccuracyValueChanged`

3. **Shot Firing:**
   - Third click: `ShotInput` constructs `ShotParameters` (power, aim angle, spin)
   - Fires `ShotInput.OnShotReady(ShotParameters)`
   - `BallController.Launch(ShotParameters)` applies physics
   - `GameManager.LaunchShot()` transitions to `ShotState.Flying`
   - `BallController.OnBallLaunched` fired → haptics triggered

4. **Flight Phase:**
   - `BallController.FixedUpdate()` applies wind force each frame via `WindSystem.CurrentWind`
   - `BallController` checks collision and velocity decay
   - Camera switches to flight camera (`CameraController` watching `GameManager.OnShotStateChanged`)

5. **Ball Lands:**
   - `BallController.OnBallLanded(landingPosition)` fired
   - `ScoringManager.HandleBallLanded()` calculates distance to pin
   - `ScoringManager.OnShotScored(ShotResult)` → UI updates shot results
   - `ScoringManager.OnShotRecorded(distance)` → HUD updates score
   - If best distance improved: `ScoringManager.OnBestDistanceUpdated()`
   - `GameManager.BallLanded()` transitions to `ShotState.Landed`
   - After 3s delay: `GameManager.OnResetToTee()` → ball and cameras reset, back to Ready state

6. **Game Over (After 6 Shots):**
   - `GameManager.EndGame()` fires `OnGameOver(shotCount)`
   - `AppManager.EndGame()` transitions to `AppState.GameOver`
   - `GameOverController` subscribes to `ScoringManager.OnAllShotsComplete(totalCtpDistance)`
   - Simultaneously: `LeaderboardManager.HandleGameOver()` posts final score via `leaderboardService.PostScoreAsync()`
   - `LeaderboardManager` polls leaderboard via `leaderboardService.GetLeaderboardAsync()`
   - UI: `GameOverController`, `LeaderboardController` display results

**State Management:**

- `AppManager.CurrentState` controls which scene is active and which UI is visible
- `GameManager.CurrentShotState` controls gameplay loop phase
- Services persist across scene loads via `ServiceLocator` (registered once in `Bootstrap`)
- Best score cached locally in `PlayerPrefs` via `IBestScoreService`

## Key Abstractions

**State Machines:**
- **AppState enum** (Title, Instructions, TransitionToGame, Playing, Paused, GameOver, Leaderboard): Application-level screen flow
- **ShotState enum** (Ready, Flying, Landed): Gameplay loop phases
- Managed by `AppManager` and `GameManager` via `SetState()` and event dispatch

**Service Interfaces:**
- `IAuthService`: Player sign-in, token retrieval, display name updates
  - Implementations: `UgsAuthService` (server), `MockAuthService` (offline)
- `ILeaderboardService`: Score posting, leaderboard retrieval, rank calculation
  - Implementations: `UgsLeaderboardService` (server via Cloud Code), `MockLeaderboardService` (offline)
- `IBestScoreService`: Local best score persistence
  - Implementation: `PlayerPrefsBestScoreService`
- Resolved via `ServiceLocator.Get<T>()`

**Configuration Objects:**
- `BallPhysicsConfig` (ScriptableObject): Mass, max power, loft angle, wind sensitivity, physics tuning
- `CameraConfig`: Camera blend times, speeds, smooth damps
- `CourseConfig`: Hole length, pin position, tee position (procedurally generated per shot)
- Located in `Assets/Resources/` or injected via `[SerializeField]`

**Data Transfer Objects:**
- `ShotParameters`: Power (0-1), aim angle (-45 to 45), backspin/sidespin (for future use)
- `ShotResult`: Distance to pin, shot number, timestamp
- `LeaderboardEntry`: Player name, score, rank
- `PlayerInfo`: Player ID, display name, authentication token

## Entry Points

**Bootstrap (Initialization):**
- Location: `Assets/Scripts/Multiplayer/Bootstrap.cs`
- Triggers: Automatic (MonoBehaviour, Awake with execution order -100)
- Responsibilities: Register services, initialize UGS, configure physics/platform settings, fallback to mocks on failure

**AppManager (Screen Flow):**
- Location: `Assets/Scripts/Core/AppManager.cs`
- Triggers: Creates singleton after Bootstrap, responds to user actions (StartGame, ShowLeaderboard, ReturnToTitle)
- Responsibilities: Manage application state transitions, handle scene loading, orchestrate pause/resume

**GameManager (Gameplay Loop):**
- Location: `Assets/Scripts/Core/GameManager.cs`
- Triggers: Activated when `AppState == Playing`, responds to `OnResetToTee` events
- Responsibilities: Track shot count (0-6), manage shot state transitions, detect game over condition, fire events for UI/camera/scoring

**Main Scene Entry Points:**
- `MainMenu.unity`: Contains `AppManager`, `Bootstrap`, UI controllers for title/instructions/leaderboard
- `Gameplay.unity`: Contains `GameManager`, `BallController`, `ScoringManager`, `LeaderboardManager`, `WindSystem`, `CameraController`, gameplay UI

## Error Handling

**Strategy:** Try-catch wrapping async operations, fallback to mock services on UGS failure, debug logging with component tags.

**Patterns:**

**Service Registration Failure:**
```csharp
// Bootstrap.cs: RegisterServicesAsync()
try
{
    await UnityServices.InitializeAsync();
    // ... register real services
}
catch (Exception ex)
{
    Debug.LogWarning($"[Bootstrap] UGS init failed, using mocks: {ex.Message}");
    // Mocks already registered above -- game continues
}
```

**Scene Loading Error:**
```csharp
// AppManager.cs: HandleStateTransition()
try
{
    await SceneLoader.LoadSceneAsync(sceneName);
    SetState(AppState.Playing);
}
catch (Exception ex)
{
    Debug.LogError($"[AppManager] Scene transition to {newState} failed: {ex.Message}");
    // Game pauses in loading state, error visible in console
}
```

**Async Service Failures:**
- Fire-and-forget async calls in `Update()` use `_ = MethodAsync()` to suppress warning
- Exceptions caught inside service methods, logged, not propagated to caller
- Retry queue in `LeaderboardManager` for failed score posts

**State Guard Clauses:**
```csharp
// GameManager.cs: LaunchShot()
if (!isActive || currentShotState != ShotState.Ready)
{
    Debug.LogWarning("[GameManager] Cannot launch shot in current state");
    return;
}
```

## Cross-Cutting Concerns

**Logging:**
- All components log with `[ComponentName]` prefix: `Debug.Log("[GameManager] ...")`, `Debug.LogWarning()`, `Debug.LogError()`
- Scene loader logs progress
- Service initialization logs fallback state

**Validation:**
- `ShotInput` clamps aim angle to ±45 degrees
- Power meter clamped 0-1, accuracy meter clamped -1 to 1
- Guard clauses in state transitions prevent invalid state changes
- Scene loader skips reload if already in target scene

**Authentication:**
- `Bootstrap` handles sign-in (anonymous or cached nickname)
- `IAuthService.IsSignedIn` and `PlayerId` cached on first access
- Display name updated if `nickname` saved in `PlayerPrefs`
- Multiplayer layer gracefully degrades to mock if UGS unavailable

**Event Subscription Cleanup:**
- All controllers unsubscribe in `OnDestroy()` from managers they subscribed to in `Start()`
- Prevents null reference exceptions on scene transitions
- `OnDestroy()` checks null before unsubscribing (safe pattern)

---

*Architecture analysis: 2026-03-22*
