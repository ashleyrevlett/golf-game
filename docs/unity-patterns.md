# Unity Patterns

## File Rules

- **Never edit `.meta` files** — Unity manages GUIDs automatically
- **Never edit `.prefab`, `.unity`, `.asset` directly** — use Unity Editor; YAML serialization is fragile
- **Always commit `.meta` files** — broken GUIDs break references on other machines

## Component Wiring

### SerializeField + Inspector (preferred)

```csharp
[SerializeField] private BallController ball;
```

Group related fields with `[Header]`:
```csharp
[Header("References")]
[SerializeField] private GameManager gameManager;

[Header("Settings")]
[SerializeField] private float transitionDuration = 2f;
```

### Runtime Lookup (sparingly)

Cache results — never call in `Update()`:
```csharp
private Target[] targets;

void Start()
{
    targets = FindObjectsByType<Target>(FindObjectsSortMode.None);
}
```

## Coroutines

Cache `WaitForSeconds` if reused:
```csharp
private WaitForSeconds waitOneSecond = new WaitForSeconds(1f);

IEnumerator ShowThenHide()
{
    gameObject.SetActive(true);
    yield return waitOneSecond;
    gameObject.SetActive(false);
}
```

## Transform Scale

- Verify Transform scale after creating/modifying GameObjects — default `(1,1,1)`
- When parenting, check scale didn't inherit incorrectly
- After duplicating, verify correct scale on the copy

## Prefabs

- Search for existing prefabs before creating new ones
- Use **Prefab Variants** for visual variations, not new files
- Before deleting/replacing instances, check serialized references
- After swapping, verify no `MissingReferenceException` in Console

## Debug Entry Points

Allow skipping game flow for faster iteration:
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

Use `[ContextMenu("Test X")]` for right-click test actions without Play mode.

## ScriptableObject Configs

Use ScriptableObjects for swappable configurations (physics settings, difficulty curves). Change data without code changes.
