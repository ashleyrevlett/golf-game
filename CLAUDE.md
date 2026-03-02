# Golf Game

## Stack
- **Engine:** Unity 6 (6000.x LTS)
- **Platform:** WebGL (mobile browser)
- **Language:** C# (.NET Standard 2.1)
- **UI:** UI Toolkit (no UGUI)
- **Camera:** Cinemachine 3.x
- **Multiplayer:** Server-authoritative scoring (external API)
- **Physics:** Unity built-in physics (Rigidbody, colliders)

## Project Structure
```
Assets/
  Scripts/
    Core/          # Game managers, state machine, config
    Golf/          # Ball physics, shot mechanics, scoring
    Camera/        # Cinemachine setup, camera transitions
    UI/            # UI Toolkit documents, controllers
    Multiplayer/   # Leaderboard API, player auth
    Environment/   # Course generation, placeholder geometry
  Scenes/
    MainMenu.unity
    Gameplay.unity
  UI/              # UXML and USS files
  Settings/        # Cinemachine profiles, physics materials
  Prefabs/
  Materials/
ProjectSettings/
Packages/
```

## Conventions
- One MonoBehaviour per file, filename matches class name
- Use `[SerializeField]` for inspector-exposed fields, never public fields
- Namespace: `GolfGame.<Folder>` (e.g., `GolfGame.Core`, `GolfGame.Golf`)
- Events via C# events or UnityEvents, not SendMessage
- ScriptableObjects for configuration data
- Assembly definitions per folder for compile-time isolation

## Test Commands
- Unity test runner: Window > General > Test Runner (requires Unity Editor)
- CI: GameCI unity-builder action (requires Unity license secret)
- Manual: Open project in Unity 6, run Play mode in Gameplay scene

## Key Design Decisions
- Closest-to-the-pin format: 6 shots, 125 yards, lowest distance wins
- No player/club models — ball + environment + pin only
- Placeholder geometry (cubes, cylinders, planes) for 3D environment
- External leaderboard API (mocked interface for now)
- External auth API (token-based, mocked interface for now)
