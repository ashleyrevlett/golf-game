# UI Patterns

This project uses **UI Toolkit** for all screen-space UI. **UGUI (World Space Canvas)** is allowed only for UI elements placed in the 3D environment (e.g., distance markers, pin labels, in-world indicators).

## Architecture

- **AppManager** fires `OnAppStateChanged` — screen-level visibility
- **GameManager** fires `OnStateChanged`, `OnShotComplete`, `OnGameOver` — gameplay HUD
- **UI Controllers** subscribe to events, update visibility and content

## Controller Pattern (UI Toolkit)

```csharp
namespace GolfGame.UI
{
    public class TitleScreenController : MonoBehaviour
    {
        private AppManager appManager;
        private UIDocument uiDocument;
        private VisualElement root;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            appManager = FindFirstObjectByType<AppManager>();
        }

        private void Start()
        {
            root = uiDocument.rootVisualElement;
            root.Q<Button>("start-button")?.RegisterCallback<ClickEvent>(OnStartClicked);
            appManager.OnAppStateChanged += HandleAppStateChanged;
        }

        private void OnDestroy()
        {
            appManager.OnAppStateChanged -= HandleAppStateChanged;
        }

        private void HandleAppStateChanged(AppState state)
        {
            root.style.display = state == AppState.Title
                ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
```

## UIDocument Architecture

One UIDocument per logical layer:

| Layer | Content | Sort Order |
|-------|---------|------------|
| HUD | Score, shot history, stats | 1 |
| Menus | Title, settings, pause | 10 |
| Modals | Dialogs, confirmations | 20 |
| Overlays | Tooltips, notifications | 30 |

**Why**: Independent z-ordering, independent visibility, manageable complexity.

## File Structure

```
Assets/UI/
├── Styles/
│   ├── Common.uss          # Shared variables, base styles
│   └── TitleScreen.uss     # Screen-specific overrides
├── Screens/
│   ├── TitleScreen.uxml
│   └── GameplayHUD.uxml
└── Settings/
    └── DefaultPanelSettings.asset
```

## Visibility Pattern

```csharp
private void HandleAppStateChanged(AppState state)
{
    root.style.display = state == AppState.Playing
        ? DisplayStyle.Flex : DisplayStyle.None;
}
```

## USS Limitations

- **No `line-height`** — use rich text: `<line-height=80%>text</line-height>`
- **Style specificity** — later stylesheets override earlier ones in UXML
- **Clearing overrides** — in UI Builder, right-click property → Unset

## Testing UI

- Test with shortest and longest expected content
- Verify at target mobile resolution
- Check that panels properly show/hide on state transitions
