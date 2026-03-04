# Unity Game Architecture: Event-Driven Design for Physics Games

Best practices for real-time physics-based Unity games. Based on Unity's official design patterns guidance and lessons learned from this project.

---

## Core Principle: Loose Coupling via Events

Components should not hold direct references to each other. Instead, they communicate through **C# events** (`Action`/`EventHandler`). This means:

- A `BallController` fires `OnBallLanded` — it doesn't know or care who listens
- A `GameManager` fires `OnShotStateChanged` — cameras, UI, audio all subscribe independently
- Adding or removing a feature means adding/removing a subscriber, not modifying the source

```csharp
// Publisher (doesn't know about subscribers)
public event Action<Vector3> OnBallLanded;
OnBallLanded?.Invoke(transform.position);

// Subscriber (doesn't know about publisher's internals)
ballController.OnBallLanded += HandleBallLanded;
```

**Always unsubscribe in `OnDestroy`** to prevent memory leaks and ghost callbacks:
```csharp
void OnDestroy() => ballController.OnBallLanded -= HandleBallLanded;
```

---

## Assembly Structure: Dependency Direction

Organize scripts into assemblies (`.asmdef` files) with a clear one-way dependency tree:

```
UI  ──────────────────┐
Audio  ───────────────┤
Camera  ──────────────┼──▶  Core
Environment  ─────────┤
Golf (ball, shot)  ───┘
Multiplayer  ─────────┘
```

**Rules:**
- `Core` knows nothing about `Golf`, `Camera`, etc.
- `Golf` can reference `Core` but not `UI` or `Audio`
- Break cycles with events: if `Core` needs to trigger `Golf` behavior, fire an `event Action` that `Golf` subscribes to (see `GameManager.OnResetToTee`)
- Never add a reference that creates a cycle — compiler will catch it but it's hard to untangle

---

## State Machine Pattern

Use an explicit state machine (enum + events) for game flow. Avoid `bool` flags that multiply:

```csharp
public enum ShotState { Ready, Flying, Landed }

public event Action<ShotState> OnShotStateChanged;

private void SetShotState(ShotState newState) {
    currentShotState = newState;
    OnShotStateChanged?.Invoke(newState);
}
```

All systems (camera, input, UI, audio) react to state changes rather than polling or being explicitly told what to do. This scales cleanly — adding a wind indicator means just subscribing to `OnShotStateChanged`, nothing else changes.

---

## Physics Architecture

### Update vs FixedUpdate
- **Never apply forces in `Update`** — use `FixedUpdate` for consistent physics
- **Exception:** `ForceMode.VelocityChange` / `ForceMode.Impulse` on launch can be called from `Update` since they're instantaneous, but prefer `FixedUpdate` for continuous forces

### Kinematic vs Dynamic
- Ball at rest → `isKinematic = true` (immune to physics, no drift)
- Ball in flight → `isKinematic = false` (physics simulates)
- **Always set `isKinematic = false` before writing `linearVelocity`** — setting velocity on a kinematic body is a no-op + warning

### Launch Pattern
```csharp
rb.isKinematic = false;
rb.linearVelocity = Vector3.zero;
rb.angularVelocity = Vector3.zero;
rb.AddForce(direction * force, ForceMode.Impulse); // respects mass
```

Use `ForceMode.Impulse` for launch (realistic, mass-dependent). Use `ForceMode.VelocityChange` only when you want to bypass mass (debug/cheat codes).

### Stop Condition
Don't check stop condition until ball has had minimum air time, preventing immediate stop on spawn:
```csharp
flightTimer += Time.fixedDeltaTime;
if (flightTimer < 0.5f) return; // min flight time
bool slow = rb.linearVelocity.magnitude < 0.2f && rb.angularVelocity.magnitude < 0.1f;
```

### Physics Materials
Use `PhysicsMaterial` on colliders for realistic friction/bounce, not custom drag code:
- Fairway: `dynamicFriction=0.4`, `staticFriction=0.5`, `bounciness=0.1`
- Green: `dynamicFriction=0.6`, `staticFriction=0.7`, `bounciness=0.05`
- Sand: `dynamicFriction=0.8`, `staticFriction=0.9`, `bounciness=0.0`
- Rough: `dynamicFriction=0.7`, `staticFriction=0.8`, `bounciness=0.05`

Combine with `angularDamping` on the Rigidbody (2–5) to kill spin naturally.

---

## Component Wiring

**No `[SerializeField]` for component references.** Use `Awake()` lookups:

```csharp
private void Awake() {
    // FindFirstObjectByType for singletons/managers
    gameManager = FindFirstObjectByType<GameManager>();
    
    // FindWithTag for specific objects (faster than Find by name)
    ballController = GameObject.FindWithTag("BallController")
                               ?.GetComponent<BallController>();
}
```

**Only use `[SerializeField]` for assets:** ScriptableObjects, AudioClips, Prefabs, Materials — things that can't be found at runtime.

**Why:** Inspector drag-and-drop breaks when GameObjects are created via code/MCP and doesn't survive scene reload automation.

---

## ScriptableObject for Config

Game tuning values belong in ScriptableObjects, not hardcoded:

```csharp
[CreateAssetMenu(menuName = "Golf/Ball Physics Config")]
public class BallPhysicsConfig : ScriptableObject {
    public float launchForce = 3.2f;
    public float loftAngle = 25f;
    public float angularDamping = 2f;
}
```

Benefits: tweak values without recompile, share config across scenes, designer-friendly.

---

## Scene Management Pattern

When an AppManager loads scenes dynamically, always guard against reloading the current scene:

```csharp
public static async Task LoadSceneAsync(string sceneName) {
    if (SceneManager.GetActiveScene().name == sceneName) {
        OnSceneLoaded?.Invoke(sceneName); // already here
        return;
    }
    // ... normal load
}
```

Without this, running a scene directly in the editor causes double-initialization: all `Start()` methods run twice, creating duplicate subscriptions and ghost objects.

---

## Camera Architecture (Cinemachine 3.x)

- One `CinemachineBrain` on the Unity Camera
- Multiple `CinemachineCamera` virtual cams (Tee, Flight, Landing)
- Switch cameras by changing `Priority` (higher = active), not enabling/disabling
- `CinemachineFollow` for offset-based following (position)
- `CinemachineRotationComposer` for look-at tracking (rotation)
- Set `Follow`/`LookAt` targets in code at runtime, not in Inspector

```csharp
flightCamera.Follow = ballController.transform;
flightCamera.LookAt = ballController.transform;
flightCamera.Priority = 10; // activates this camera
```

---

## Input System (Unity 6)

Use the new Input System (`com.unity.inputsystem`):

```csharp
using UnityEngine.InputSystem;

// Keyboard
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }

// Mouse
if (Mouse.current.leftButton.wasPressedThisFrame) { }
var pos = Mouse.current.position.ReadValue();
```

Set `activeInputHandler: 1` in `ProjectSettings.asset` (new Input System only). Add `Unity.InputSystem` to the relevant `.asmdef` references.

---

## Coroutines for Timed Game Flow

For delayed state transitions (e.g., "wait 3s then reset"), use coroutines rather than `Update` timers:

```csharp
private IEnumerator ResetAfterDelay(float delay) {
    yield return new WaitForSeconds(delay);
    OnResetToTee?.Invoke();       // notify subscribers
    SetShotState(ShotState.Ready); // transition state
}

// Fire-and-forget from BallLanded:
StartCoroutine(ResetAfterDelay(3f));
```

---

## Anti-Patterns to Avoid

| Anti-Pattern | Problem | Fix |
|---|---|---|
| `FindObjectOfType` in `Update` | Expensive every frame | Cache in `Awake`/`Start` |
| Direct method calls between unrelated systems | Tight coupling | Use events |
| `[SerializeField]` for component refs | Breaks with procedural scene setup | `Awake()` lookups |
| `#if EDITOR` guards around gameplay code | Dead code in builds | Remove guards or use proper platform defines |
| Polling state with `bool` flags | Doesn't scale | State machine enum + events |
| Physics in `Update` | Non-deterministic | `FixedUpdate` for forces |
| Setting velocity on kinematic body | Silent no-op + warning | Set `isKinematic=false` first |
| Checking stop before min flight time | Ball stops at spawn | Add `flightTimer < 0.5f` guard |

---

## References

- [Unity: Observer Pattern](https://learn.unity.com/course/design-patterns-unity-6/tutorial/create-modular-and-maintainable-code-with-the-observer-pattern)
- [Unity: State Pattern](https://learn.unity.com/course/design-patterns-unity-6/tutorial/develop-a-modular-flexible-codebase-with-the-state-programming-pattern)
- [Unity: Game Programming Patterns (free ebook)](https://resources.unity.com/games/level-up-your-code-with-game-programming-patterns)
- [Cinemachine 3.x Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html)
