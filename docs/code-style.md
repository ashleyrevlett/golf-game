# C# Code Style

## Naming

| Element | Convention | Example |
|---------|------------|---------|
| Public members, methods, properties, events | PascalCase | `OnShotComplete`, `TotalScore` |
| Private fields, local variables, parameters | camelCase | `currentShot`, `ballController` |
| Constants | PascalCase | `MaxShots` |

## Namespaces

Use `GolfGame.<Folder>` (e.g., `GolfGame.Core`, `GolfGame.Golf`). Assembly definitions per folder.

## SerializeField

```csharp
[Header("References")]
[SerializeField] private GameManager gameManager;

[Header("Settings")]
[SerializeField] private float transitionDuration = 2f;
```

Never use public fields for Inspector exposure.

## Events

```csharp
// Declaration
public event Action<GameState> OnStateChanged;

// Firing
OnStateChanged?.Invoke(newState);

// Subscribe in Start, unsubscribe in OnDestroy
gameManager.OnStateChanged += HandleStateChanged;
gameManager.OnStateChanged -= HandleStateChanged;
```

## Expression-Bodied Properties

```csharp
public int TotalScore => totalScore;
public bool IsPlaying => currentState == GameState.Playing;
```

## Null-Conditional for Optional Components

```csharp
animator?.SetTrigger("Show");
button?.RegisterCallback<ClickEvent>(HandleClick);
```

## General

- Simplest solution that works
- DRY — extract shared logic
- Follow existing event-driven patterns
- One MonoBehaviour per file, filename matches class name
