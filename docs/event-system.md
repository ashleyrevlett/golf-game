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
public event Action<int, bool> OnGameOver;
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
- Use `[SerializeField]` references wired in Inspector for stable references
- Use `FindFirstObjectByType<T>()` sparingly, cache results in `Awake()`
- Never use `GetComponent<T>()` in `Update()` — cache in `Start()`

## Data Flow Examples

### Shot Input → Ball Launch

```
UI Controller (sliders/input)
    ↓ ShotParameters
GameManager.TakeShot()
    ↓ ShotParameters
BallController.Launch()
```

### Ball Stop → Score

```
BallController detects velocity < threshold
    ↓ OnLanded(position)
GameManager.HandleBallLanded()
    ↓ CalculateScoreAtPosition()
Target.GetScoreForPosition()
    ↓ ShotResult
OnShotComplete event
    ↓
UI updates
```
