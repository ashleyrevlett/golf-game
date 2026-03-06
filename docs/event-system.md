# Event System Architecture

## Two-Tier State Machine

The game uses two state machines at different scopes:

### AppManager (Application-Level)

Controls screen flow:

```
Title → Instructions → TransitionToGame → Playing → GameOver → Leaderboard
  ↑                                                     │           │
  └─────────────────────────────────────────────────────┴───────────┘
```

| State | Description |
|-------|-------------|
| `Title` | Title screen, waiting for user to start |
| `Instructions` | Showing game instructions |
| `TransitionToGame` | Camera blending to tee position |
| `Playing` | Active gameplay (GameManager takes over) |
| `GameOver` | Showing final score |
| `Leaderboard` | Showing high scores |

### GameManager (Gameplay-Level)

Controls the shot loop. Only active when `AppManager.CurrentState == Playing`:

```
Ready → Flying → Landed → Ready (loop until max shots)
                    ↓
                GameOver (after final shot)
```

## Event Pattern

Use `System.Action<T>` for all events. Never use `SendMessage`.

### Declaration

```csharp
public event Action<GameState> OnStateChanged;
public event Action<int> OnGameOver;

// ScoringManager events
public event Action<ShotResult> OnShotScored;
public event Action<float> OnShotRecorded;        // per-shot distance to pin
public event Action<float> OnBestDistanceUpdated;  // new best distance
public event Action<float> OnAllShotsComplete;     // total CTP distance (end of game)

// BallController events
public event Action<Vector3> OnBallLanded;
public event Action<Vector3, float> OnBallBounced;
public event Action OnBallLaunched;                // fires on each launch
```

### Firing

```csharp
OnStateChanged?.Invoke(newState);
```

### Subscribing

Subscribe in `Start()` or `OnEnable()`, unsubscribe in `OnDestroy()` or `OnDisable()`:

```csharp
private void Start()
{
    gameManager.OnStateChanged += HandleStateChanged;
}

private void OnDestroy()
{
    gameManager.OnStateChanged -= HandleStateChanged;
}
```

**Always unsubscribe.** Leaked subscriptions cause null reference errors and memory leaks.

## Component Wiring

- Managers fire events, UI controllers subscribe
- Use `FindFirstObjectByType<T>()` for cross-object references, cache in `Start()` or `Awake()`
- Use `GetComponent<T>()` for same-GameObject components, cache in `Awake()`
- Reserve `[SerializeField]` for assets only (ScriptableObjects, Prefabs, AudioClips)
- Never call `GetComponent<T>()` or `FindFirstObjectByType<T>()` in `Update()`

## Data Flow Examples

### Shot Input → Ball Launch

```
UI Controller (sliders/input)
    ↓ ShotParameters
GameManager.TakeShot()
    ↓ ShotParameters
BallController.Launch()
```

### Shot Launch → Ball Flight

```
ShotTester / UI Controller
    ↓ ShotParameters
GameManager.LaunchShot()
    ↓
BallController.Launch()
    ↓ OnBallLaunched
BallVisualEffects.HandleLaunched()  (trail starts)
```

### Ball Stop → Score → CTP

```
BallController detects velocity < threshold
    ↓ OnBallLanded(position)
ScoringManager.HandleBallLanded()
    ↓ calculates distance to pin
    ↓ accumulates TotalCtpDistance
    ├─ OnShotRecorded(distanceToPin)       → HUD updates score pod
    ├─ OnShotScored(ShotResult)            → HUD updates shots remaining
    ├─ OnBestDistanceUpdated(best)         → (if new best)
    └─ ShotPopup.Create()                  → floating distance text
```

### Final Shot → Game Over

```
GameManager fires OnGameOver(shotCount)
    ↓
ScoringManager.HandleGameOver()
    └─ OnAllShotsComplete(totalCtpDistance) → GameOverController shows overlay
        ↓ async
        IBestScoreService comparison        → "NEW BEST!" or previous best display
```
