# Coding Conventions

**Analysis Date:** 2026-03-22

## Naming Patterns

**Files:**
- Filename matches class name exactly (one MonoBehaviour per file)
- Example: `BallController.cs` contains class `BallController`

**Functions/Methods:**
- Public methods: PascalCase (`LaunchShot`, `ResetToTee`, `GetPlayerTokenAsync`)
- Private methods: camelCase (`HandleShotStateChanged`, `UpdatePowerMeter`)
- Event names: PascalCase with `On` prefix (`OnBallLanded`, `OnShotStateChanged`, `OnGameOver`)

**Variables:**
- Private/local variables: camelCase (`currentShot`, `ballController`, `lockedPower`)
- Parameters: camelCase (`displayName`, `projectId`, `distance`)

**Types/Classes:**
- PascalCase (`GameManager`, `BallController`, `ShotParameters`, `ShotState`)
- Enums: PascalCase (`MeterPhase`, `ShotState`, `AppState`)

**Constants:**
- PascalCase (`MaxShots`, `MAX_SUBMISSIONS`, `DefaultLoftAngle`)

**Properties:**
- Public properties: PascalCase (`IsFlying`, `CurrentShot`, `TotalCtpDistance`, `BestDistance`)
- Use expression-bodied syntax: `public int TotalScore => totalScore;`

## Namespaces

**Pattern:**
- `GolfGame.<Folder>` (e.g., `GolfGame.Core`, `GolfGame.Golf`, `GolfGame.Multiplayer`, `GolfGame.UI`, `GolfGame.Environment`)
- Test namespaces: `GolfGame.Tests.EditMode` or `GolfGame.Tests.PlayMode`

**Assembly Definitions:**
- One `.asmdef` per folder for compile-time isolation
- Located alongside scripts (`Assets/Scripts/Core/GolfGame.Core.asmdef`)

## SerializeField Usage

**Standard pattern:**
```csharp
[Header("References")]
[SerializeField] private GameManager gameManager;

[Header("Settings")]
[SerializeField] private float transitionDuration = 2f;
[SerializeField] private int maxAttempts = 3;
```

**Organization:**
- Group related fields with `[Header]` attributes
- Header groups: "References", "Settings", "Debug", "Configuration"
- Always private `[SerializeField]`, never public fields

**Reserve SerializeField for:**
- Assets without runtime equivalents (ScriptableObjects, Prefabs, AudioClips)
- Configuration values set in Inspector
- Component references that are drag-and-drop wired

**Use code-based lookups for:**
- Components on same GameObject: `GetComponent<T>()` in `Awake()`
- Singletons: `FindFirstObjectByType<T>()` or `GameObject.FindWithTag("TagName")`
- Scene queries: Cache result in `Awake()` or `Start()`, never call in `Update()`

## Events

**Declaration:**
```csharp
public event Action<GameState> OnStateChanged;
public event Action<int> OnGameOver;
public event Action OnResetToTee;
```

**Firing:**
```csharp
OnStateChanged?.Invoke(newState);
```

**Subscription (in Start):**
```csharp
gameManager.OnStateChanged += HandleStateChanged;
```

**Unsubscription (in OnDestroy):**
```csharp
gameManager.OnStateChanged -= HandleStateChanged;
```

**Handler naming:**
- Private event handlers: `Handle<EventName>` (e.g., `HandleShotStateChanged`, `HandleBallLanded`)

**Event documentation:**
- Include XML doc comment explaining payload and when event fires
- Example in `GameManager.cs`:
  ```csharp
  /// <summary>
  /// Fires when the shot state changes. Payload is the new state.
  /// </summary>
  public event Action<ShotState> OnShotStateChanged;
  ```

## Expression-Bodied Properties

**Use for read-only or simple computed properties:**
```csharp
public int CurrentShot => currentShot;
public bool IsPlaying => currentState == GameState.Playing;
public float TotalScore => totalScore;
```

**Use standard getter/setter only for complex logic:**
```csharp
public int Health
{
    get { return _health; }
    set { _health = Mathf.Clamp(value, 0, maxHealth); }
}
```

## Null-Conditional Operators

**Use for optional components/references:**
```csharp
animator?.SetTrigger("Show");
button?.RegisterCallback<ClickEvent>(HandleClick);
gameManager?.BallLanded();
windSystem?.GetCurrentWind();
```

**Avoid null coalescing for method calls; instead:**
```csharp
if (ballController != null)
{
    ballController.Launch(parameters);
}
```

## Import Organization

**Order (enforced by linting):**
1. System imports (`using System;`, `using System.Collections;`)
2. UnityEngine imports (`using UnityEngine;`, `using UnityEngine.UIElements;`)
3. Third-party/package imports (`using Unity.Services.Core;`)
4. Game namespace imports (`using GolfGame.Core;`)

**Example from `GameOverController.cs`:**
```csharp
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Environment;
using GolfGame.Multiplayer;
```

## Error Handling

**Exception Strategy:**
- Catch exceptions at service boundaries (Bootstrap, Auth, Leaderboard)
- Log error with context prefix: `Debug.LogError($"[ComponentName] Message: {ex}")`
- Catch and rethrow in public APIs to preserve stack trace
- Use try-catch in `async void` to prevent unhandled exceptions

**Pattern in Bootstrap.cs:**
```csharp
try
{
    await RegisterServicesAsync();
    ConfigurePlatform();
    Debug.Log("[Bootstrap] Initialization complete");
}
catch (Exception ex)
{
    Debug.LogError($"[Bootstrap] Initialization failed: {ex}");
}
```

**Pattern in async services:**
```csharp
catch (System.Exception ex)
{
    Debug.LogError($"[UgsAuth] Failed to get player token: {ex.Message}");
    throw; // Preserve stack trace for upstream handling
}
```

**Fallback pattern:**
- Register mock implementations before trying real services
- If real service initialization fails, mocks already in place (see `Bootstrap.cs`)

## Logging

**Framework:** `Debug.Log()`, `Debug.LogWarning()`, `Debug.LogError()`

**Pattern:**
- Prefix with component name in brackets: `[ComponentName]`
- Use string interpolation for context
- Log at key state transitions and errors

**Examples:**
```csharp
Debug.Log("[GameManager] Shot 1: Ready -> Flying");
Debug.LogWarning("[GameManager] Cannot change shot state while inactive");
Debug.LogError("[UgsAuth] Failed to get player token: connection timeout");
Debug.Log("[Bootstrap] UGS services registered");
```

## Comments

**When to comment:**
- Complex physics/math calculations
- Non-obvious design decisions
- Workarounds or temporary solutions
- Cross-cutting concerns (e.g., platform-specific code)

**XML Documentation (on public APIs):**
```csharp
/// <summary>
/// Transition to a new shot state.
/// </summary>
public void SetShotState(ShotState newState)
```

```csharp
/// <summary>
/// Best distance to pin across all shots this game.
/// float.MaxValue if no shots taken.
/// </summary>
public float BestDistance => bestDistance;
```

**Avoid comments for:**
- Self-explanatory code (`int shotCount = 0;` needs no comment)
- Obvious loop/conditional logic

## Function Design

**Size:**
- Keep methods under 50 lines where practical
- Extract complex state machines into private helper methods
- Example: `UpdatePowerMeter()` in `ShotInput.cs` is 3 lines

**Parameters:**
- Maximum 4 parameters; use data classes for more complex scenarios
- Example: `ShotParameters` struct bundles power, aim, spin data

**Return Values:**
- Prefer returning data/objects over output parameters
- Use nullable types and expression-bodied properties

**Async Methods:**
- Add `Async` suffix: `GetPlayerTokenAsync()`, `SignInAsync()`
- Return `Task<T>` or `Task` (never `Task.Run()` in WebGL)
- Use `async void` only for event handlers and one-time initializers (`Bootstrap.Awake()`)

## Conditional Compilation

**Platform-specific code:**
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL-only code
#else
    // Editor/other platform fallback
#endif
```

**Debug entry points (Editor-only):**
```csharp
#if UNITY_EDITOR
[Header("Debug")]
[SerializeField] private bool skipToPlaying = false;

void Start()
{
    if (skipToPlaying) SetState(GameState.Playing);
}
#endif
```

## Field Initialization

**Use lazy initialization for expensive operations:**
```csharp
private BallPhysicsConfig physicsConfig;

private void Awake()
{
    if (physicsConfig == null)
    {
        physicsConfig = Resources.Load<BallPhysicsConfig>("BallPhysicsConfig");
        if (physicsConfig == null)
            Debug.LogWarning("[BallController] No BallPhysicsConfig found — using defaults");
    }
}
```

## Module Design

**Exports:**
- Public properties and methods are exported API
- Keep public surface small and well-documented
- Use `internal` for methods shared within assembly only

**Barrel Files:**
- Not used in this project; use namespace imports instead

**Component Wiring:**
- Resolve references in `Awake()` (before other GameObjects' `Start()`)
- Only use `[SerializeField]` drag-drop for assets, not component references
- See Reference Wiring section in CLAUDE.md for specific patterns

## Coroutines

**Reused WaitForSeconds:**
```csharp
private WaitForSeconds waitOneSecond = new WaitForSeconds(1f);

IEnumerator ShowThenHide()
{
    gameObject.SetActive(true);
    yield return waitOneSecond;
    gameObject.SetActive(false);
}
```

**Inline yield when one-time:**
```csharp
StartCoroutine(ResetAfterDelay(3f));

private System.Collections.IEnumerator ResetAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    OnResetToTee?.Invoke();
    SetShotState(ShotState.Ready);
}
```

## General Principles

- Simplest solution that works
- DRY — extract shared logic
- Follow event-driven patterns — managers fire events, subscribers react
- Avoid SendMessage; use C# events instead
- One MonoBehaviour per file, filename matches class name
- Use ScriptableObjects for configuration data, not manager singletons

---

*Convention analysis: 2026-03-22*
