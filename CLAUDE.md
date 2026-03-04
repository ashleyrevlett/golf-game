# Golf Game

## Stack
- **Engine:** Unity 6.3 LTS
- **Platform:** WebGL (mobile browser)
- **Language:** C# (.NET Standard 2.1)
- **UI:** UI Toolkit (screen-space); UGUI World Space Canvas for in-world 3D UI only
- **Camera:** Cinemachine 3.x
- **Multiplayer:** Server-authoritative scoring (UGS Authentication, Leaderboards, Cloud Code)
- **Physics:** Unity built-in physics (Rigidbody, colliders)

## Project Structure
```
Assets/
  CloudCode/       # Cloud Code JS scripts (server-side validation)
  Scripts/
    Core/          # Game managers, state machine, config
    Golf/          # Ball physics, shot mechanics, scoring
    Camera/        # Cinemachine setup, camera transitions
    UI/            # UI Toolkit documents, controllers
    Multiplayer/   # Leaderboard API, player auth, UGS services
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
- Cloud Code JS tests: `node --test Assets/CloudCode/validate-and-post-score.test.js`
- CI: GameCI unity-builder action (requires Unity license secret)
- Manual: Open project in Unity 6, run Play mode in Gameplay scene

## Key Design Decisions
- Closest-to-the-pin format: 6 shots, 125 yards, lowest distance wins
- No player/club models — ball + environment + pin only
- Placeholder geometry (cubes, cylinders, planes) for 3D environment
- UGS-backed leaderboard and auth (anonymous sign-in, Cloud Code validation)
- Mock services as fallback in editor and when UGS init fails

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

### Async Services
- Service interfaces (`IAuthService`, `ILeaderboardService`) are async (`Task<T>`) — no `Task.Run` or threads in WebGL
- `Bootstrap.Awake()` is `async void` — mocks register synchronously before the first `await` so other components can resolve services immediately in their `Start()`
- Fire-and-forget async calls in `Update()` use `_ = MethodAsync()` — exceptions are caught inside the method, not at the call site

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

## Reference Wiring

Avoid `[SerializeField]` drag-and-drop wiring in the Inspector. Use code-based reference resolution instead:

- **Same GameObject:** `GetComponent<T>()` in `Awake()`
- **Tagged objects:** `GameObject.FindWithTag("TagName")` — use for singletons like `GameManager`, `BallController`
- **Simple lookups (no tag warranted):** `GameObject.Find("Name")` — cache result in `Awake()`, never call in `Update()`
- **Children:** `GetComponentInChildren<T>()`
- **Scene-wide search by type:** `Object.FindFirstObjectByType<T>()` (Unity 6 preferred over `FindObjectOfType`)

### Preferred Pattern
```csharp
private GameManager gameManager;

private void Awake()
{
    gameManager = GameObject.FindWithTag("GameManager")?.GetComponent<GameManager>();
    // or for same-object components:
    ballController = GetComponent<BallController>();
}
```

### Tags to Define
- `GameManager`
- `BallController`
- `ScoringManager`
- `WindSystem`
- `AudioManager`

Reserve `[SerializeField]` only for assets (ScriptableObjects, Prefabs, AudioClips) that have no runtime equivalent.

## Unity MCP Workflow

After making changes that trigger recompilation or generate `.meta` files (new scripts, moved assets, package changes):

1. **Refresh Unity:**
   ```
   mcporter call UnityMCP.refresh_unity
   ```

2. **Check console for errors:**
   ```
   mcporter call UnityMCP.read_console
   ```
   Fix any errors before proceeding.

3. **Commit refreshed assets** — Unity may generate or update `.meta` files after import. Always commit these alongside the triggering change:
   ```bash
   git add -A && git commit -m "..." && git push
   ```

Never commit code changes without also committing the resulting `.meta` files — missing metas cause broken references for other contributors.

## Compiler Errors — Hard Rule

**Never commit if there are compiler errors. Always check before committing.**

```bash
# Check for compiler errors via Unity MCP before any commit
mcporter call UnityMCP.read_console
# Look for "error CS" lines — if any exist, fix them first
```

C# compiler errors cause the entire project to fail to build — every other script stops working too. A single error breaks everyone. There is no acceptable reason to commit broken code.

**Workflow:**
1. Make changes
2. `mcporter call UnityMCP.refresh_unity`
3. `mcporter call UnityMCP.read_console` — scan for `error CS`
4. Fix any errors
5. Re-check console is clean
6. Only then: `git add -A && git commit -m "..." && git push`
