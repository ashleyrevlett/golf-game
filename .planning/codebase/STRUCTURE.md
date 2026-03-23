# Codebase Structure

**Analysis Date:** 2026-03-22

## Directory Layout

```
Assets/
├── Scripts/
│   ├── Core/               # Application state, game state, service locator, scene loading
│   ├── Golf/               # Ball physics, shot input, wind system
│   ├── Environment/        # Course generation, scoring, pin controller, shot results
│   ├── Multiplayer/        # Auth, leaderboard services, Bootstrap
│   ├── UI/                 # UI Toolkit controllers for all screens
│   ├── Camera/             # Cinemachine-based camera switching
│   └── Audio/              # Audio management, sound effect triggers
├── Scenes/
│   ├── MainMenu.unity      # Title, instructions, leaderboard screens
│   └── Gameplay.unity      # Gameplay loop, HUD, game over screen
├── UI/
│   ├── Screens/            # UXML files for each screen/panel
│   └── Styles/             # USS stylesheets
├── Config/                 # ScriptableObjects: BallPhysicsConfig, CameraConfig, CourseConfig
├── Settings/
│   └── BuildProfiles/      # WebGL build profile configuration
├── Plugins/
│   └── WebGL/              # WebGL template, Unity wrapper scripts
├── CloudCode/              # Server-side JavaScript for score validation
└── Tests/
    ├── PlayMode/           # Integration tests
    └── EditMode/           # Unit tests
```

## Directory Purposes

**Core:**
- Purpose: Foundation layer—application state, gameplay loop state, dependency injection, scene transitions
- Contains: State enums, managers, service locator, scene loader utilities
- Key files: `AppManager.cs`, `GameManager.cs`, `ServiceLocator.cs`, `GameState.cs`, `SceneLoader.cs`

**Golf:**
- Purpose: Ball physics simulation, player input handling, wind system
- Contains: Ball controller with Rigidbody integration, shot input meter, wind vector generation
- Key files: `BallController.cs`, `ShotInput.cs`, `ShotParameters.cs`, `WindSystem.cs`, `BallPhysicsConfig.cs`

**Environment:**
- Purpose: Course geometry, scoring calculations, pin tracking, shot result tracking
- Contains: Course builder (placeholder geometry), scoring manager, pin controller, wind indicator UI
- Key files: `ScoringManager.cs`, `CourseBuilder.cs`, `PinController.cs`, `ShotResult.cs`, `CourseConfig.cs`

**Multiplayer:**
- Purpose: Server integration, player authentication, leaderboard management
- Contains: Service interfaces (IAuthService, ILeaderboardService), UGS implementations, mock implementations for offline play
- Key files: `Bootstrap.cs`, `IAuthService.cs`, `ILeaderboardService.cs`, `UgsAuthService.cs`, `UgsLeaderboardService.cs`, `LeaderboardManager.cs`

**UI:**
- Purpose: User interface controllers for all screens
- Contains: Screen controllers that respond to state changes and game events, update UI Toolkit documents
- Key files: `MainMenuController.cs`, `GameplayHUDController.cs`, `GameOverController.cs`, `LeaderboardController.cs`, `PauseMenuController.cs`, `SettingsController.cs`

**Camera:**
- Purpose: Cinemachine-based camera management
- Contains: Camera controller that switches cameras based on shot state (Ready/Flying/Landed)
- Key files: `CameraController.cs`, `CameraConfig.cs`

**Audio:**
- Purpose: Sound and haptics management
- Contains: Audio manager, sound trigger handlers for ball, environment, UI
- Key files: `AudioManager.cs`, `AmbientAudioController.cs`, `BallAudioController.cs`, `AudioConfig.cs`

**Scenes:**
- `MainMenu.unity`: Main entry point. Contains AppManager (singleton DontDestroyOnLoad), Bootstrap, UI controllers for Title/Instructions/Leaderboard screens
- `Gameplay.unity`: Gameplay scene. Contains GameManager, BallController, ScoringManager, LeaderboardManager, WindSystem, CameraController, HUD and GameOver UI controllers

**UI/ (UXML/USS):**
- `Screens/`: UXML documents for each screen (MainMenu.uxml, Gameplay.uxml, GameOver.uxml, Leaderboard.uxml, etc.)
- `Styles/`: Shared USS stylesheets for colors, fonts, spacing, responsive layout

**Config/:**
- `BallPhysicsConfig.asset`: Physics tuning—mass, max power, loft angle, wind sensitivity, bounce damping
- `CameraConfig.asset`: Camera transition speeds, blend times, follow distances
- `CourseConfig.asset`: Hole dimensions, wind range, tee/pin positions (supports randomization per shot)

**Plugins/WebGL/:**
- WebGL build template and glue code for browser APIs (clipboard sharing, fullscreen, etc.)

**CloudCode/:**
- Server-side JavaScript running on Unity Services—validates scores, prevents cheating, posts to leaderboards

**Tests/:**
- `PlayMode/`: Integration tests using GameManager, BallController, real scenes
- `EditMode/`: Unit tests for isolated components (windSystem, scoring logic, etc.)

## Key File Locations

**Entry Points:**
- `Assets/Scripts/Multiplayer/Bootstrap.cs`: Service initialization and registration (execution order -100)
- `Assets/Scripts/Core/AppManager.cs`: Application state machine and scene orchestration
- `Assets/Scenes/MainMenu.unity`: Game entry point (contains AppManager, Bootstrap)

**Configuration:**
- `Assets/Config/BallPhysicsConfig.asset`: Ball mass, power, loft, wind tuning
- `Assets/Config/CameraConfig.asset`: Camera blend times and follow distances
- `Assets/Config/CourseConfig.asset`: Hole dimensions and environmental parameters

**Core Logic:**
- `Assets/Scripts/Core/GameManager.cs`: Gameplay loop state machine (Ready/Flying/Landed)
- `Assets/Scripts/Golf/BallController.cs`: Ball physics simulation and collision detection
- `Assets/Scripts/Environment/ScoringManager.cs`: Distance calculation and shot tracking
- `Assets/Scripts/Golf/ShotInput.cs`: 3-click power meter input system

**Services:**
- `Assets/Scripts/Multiplayer/IAuthService.cs`, `ILeaderboardService.cs`: Service contracts
- `Assets/Scripts/Multiplayer/UgsAuthService.cs`, `UgsLeaderboardService.cs`: Production implementations
- `Assets/Scripts/Multiplayer/MockAuthService.cs`, `MockLeaderboardService.cs`: Offline/debug implementations

**UI Controllers:**
- `Assets/Scripts/UI/MainMenuController.cs`: Title and instructions screens
- `Assets/Scripts/UI/GameplayHUDController.cs`: In-game HUD (score, shots remaining, wind)
- `Assets/Scripts/UI/GameOverController.cs`: Game over screen, score sharing, leaderboard access
- `Assets/Scripts/UI/LeaderboardController.cs`: Leaderboard display and ranking

**Testing:**
- `Assets/Tests/EditMode/GolfGame.Tests.EditMode.asmdef`: Unit test assembly definition
- `Assets/Tests/PlayMode/GolfGame.Tests.PlayMode.asmdef`: Integration test assembly definition

## Naming Conventions

**Files:**
- C# class files: PascalCase matching class name (e.g., `BallController.cs`, `ShotInput.cs`)
- ScriptableObjects: PascalCase with `Config` or `Data` suffix (e.g., `BallPhysicsConfig.asset`)
- UI documents: camelCase with screen name (e.g., `mainMenu.uxml`, `gameplayHUD.uxml`)
- Test files: ClassName + `Tests.cs` (e.g., `BallControllerTests.cs`, `ScoringManagerTests.cs`)

**Directories:**
- Domain folders match C# namespaces: `Scripts/Core/` → `GolfGame.Core`, `Scripts/Golf/` → `GolfGame.Golf`
- Plural for collections: `Screens/`, `Styles/`
- No underscores or hyphens (except in filenames when needed for web assets)

**Classes & Namespaces:**
- All game code: `GolfGame.<Folder>` (e.g., `GolfGame.Core`, `GolfGame.Golf`, `GolfGame.UI`)
- One MonoBehaviour per file
- Private fields: `camelCase` with `_` prefix optional
- Public properties: PascalCase
- Events: `On<Action>` (e.g., `OnShotStateChanged`, `OnBallLanded`)

## Where to Add New Code

**New Feature (e.g., new game mode):**
- Primary code: `Assets/Scripts/Core/` for state management, `Assets/Scripts/Golf/` for mechanics
- Tests: `Assets/Tests/PlayMode/` for integration tests
- UI: `Assets/Scripts/UI/` for new screen controller, new UXML in `Assets/UI/Screens/`
- Example: Adding "practice mode" → `GameState.cs` enum, `PracticeController.cs` in Core, `PracticeScreenController.cs` in UI

**New Component/Module:**
- Implementation: Match domain—physics → `Scripts/Golf/`, environment → `Scripts/Environment/`, services → `Scripts/Multiplayer/`
- Assembly definition: Create `.asmdef` in folder if none exists (maintains compile-time isolation)
- Namespace: `GolfGame.<FolderName>`
- Example: New wind effect → `Assets/Scripts/Golf/AdvancedWindSimulation.cs`, namespace `GolfGame.Golf`

**Utilities & Helpers:**
- Shared helpers: `Assets/Scripts/Core/` for singletons/managers, consider a `Utilities/` folder for pure functions
- Configuration: `Assets/Config/` for ScriptableObjects, `Assets/Resources/` for runtime-loaded defaults
- Example: Math helpers for trajectory → `Assets/Scripts/Core/MathUtils.cs` or `Assets/Scripts/Golf/TrajectoryCalculator.cs`

## Special Directories

**Build Profiles:**
- Location: `Assets/Settings/BuildProfiles/`
- Purpose: WebGL-specific build configuration overrides
- Generated: Automatically by Unity 6
- Committed: Yes—critical for reproducible builds

**CloudCode:**
- Location: `Assets/CloudCode/`
- Purpose: Server-side score validation and leaderboard posting
- Generated: No—hand-written JavaScript
- Committed: Yes—part of deployable artifact

**Resources:**
- Location: `Assets/Resources/`
- Purpose: Runtime-loaded ScriptableObjects and defaults
- Generated: No
- Committed: Yes—includes config defaults

**Packages:**
- Location: `Packages/`
- Purpose: Package manifests (manifest.json, lock files)
- Generated: Yes—by Package Manager
- Committed: Yes—lock files ensure reproducibility

**Temp & Library:**
- Location: `Temp/`, `Library/`
- Purpose: Build artifacts and editor cache
- Generated: Yes—auto-generated
- Committed: No—in .gitignore

---

*Structure analysis: 2026-03-22*
