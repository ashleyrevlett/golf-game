# Golf Game

## Stack
- **Engine:** Unity 6.3 LTS
- **Platform:** WebGL (mobile browser)
- **Language:** C# (.NET Standard 2.1)
- **UI:** UI Toolkit (screen-space); UGUI World Space Canvas for in-world 3D UI only
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

## Reference Docs

Read these before working on specific areas:

| Task | Read First |
|------|------------|
| Writing C# code | [`docs/code-style.md`](docs/code-style.md) |
| Event system, state machines | [`docs/event-system.md`](docs/event-system.md) |
| Unity-specific work (prefabs, components) | [`docs/unity-patterns.md`](docs/unity-patterns.md) |
| UI panels and controllers | [`docs/ui-patterns.md`](docs/ui-patterns.md) |
| Performance optimization | [`docs/performance.md`](docs/performance.md) |
| Ball physics | [`docs/physics.md`](docs/physics.md) |
| WebGL builds, templates, fullscreen | [`docs/webgl-gotchas.md`](docs/webgl-gotchas.md) |
| Unity MCP server | [`docs/unity-mcp.md`](docs/unity-mcp.md) |
| CI/CD, deployment | [`docs/deployment.md`](docs/deployment.md), [`docs/ci-cd-gotchas.md`](docs/ci-cd-gotchas.md) |

## Common Mistakes to Avoid

### Transform Scale
- Verify Transform scale after creating/modifying GameObjects — default `(1,1,1)`
- When parenting, check scale didn't inherit incorrectly
- After duplicating, verify correct scale

### .meta Files
- Always commit `.meta` files with new scripts/assets
- Missing `.meta` = broken scene/prefab references on other machines

### Build Profiles (Unity 6)
- Build Profiles **override** `ProjectSettings/ProjectSettings.asset`
- Always update settings in Build Profile `.asset` files, not just ProjectSettings

### UI Layout
- UI Toolkit for screen-space UI; UGUI only for world-space 3D elements
- Test layouts with shortest and longest expected content
- Verify at target mobile resolution

### Process
- Read relevant docs BEFORE making changes
- Update docs BEFORE committing if behavior changed
- Follow event-driven architecture — managers fire events, UI subscribes

## Pre-Commit Checklist

- [ ] Tested in Unity Play mode
- [ ] Console clean of errors/warnings
- [ ] Updated relevant docs if behavior/APIs changed
- [ ] No unintended scale or position changes
- [ ] All `.meta` files for new assets included
