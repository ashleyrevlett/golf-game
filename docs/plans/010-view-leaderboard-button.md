## 🏗️ Implementation Plan

> **Agent:** `software-architect`

**Approach**: Add a `view-leaderboard-button` to the game over UXML between the existing PLAY AGAIN and MENU buttons, wire it in `GameOverController` using the same `Q<Button>` / `RegisterCallback` / `UnregisterCallback` pattern, and call `AppManager.Instance.ShowLeaderboard()` on click. No new styles needed — `btn-secondary` from `Common.uss` covers everything per the visual design spec.

**Key decisions**:
- Button placed between PLAY AGAIN and MENU in UXML source order — this matches the visual designer's priority hierarchy (primary CTA → competitive hook → exit)
- No loading/error states on this button — the Leaderboard screen handles those after navigation (per design spec)

### Files to modify
- `Assets/UI/Screens/GameOver.uxml` — insert `view-leaderboard-button` element between play-again and menu buttons
- `Assets/Scripts/UI/GameOverController.cs` — query, register, unregister, and handle click for the new button

### Steps

**1. Add the button element to GameOver.uxml**
- File: `Assets/UI/Screens/GameOver.uxml`
- Insert a new `<ui:Button>` between the `play-again-button` (line 17) and `menu-button` (line 18):
  ```xml
  <ui:Button name="view-leaderboard-button" text="VIEW LEADERBOARD" class="btn-secondary" style="margin-bottom: 8px;" />
  ```
- The `btn-secondary` class provides: ghost background (`rgba(255,255,255,0.1)`), white 2px border, 44px min tap target, 200px min width, 8px radius, bold 20px text — all matching the MENU button
- `margin-bottom: 8px` inline style matches the play-again button's spacing, keeping consistent vertical rhythm before the MENU button below

**2. Wire the button in GameOverController.cs**
- File: `Assets/Scripts/UI/GameOverController.cs`
- Add field: `private Button viewLeaderboardButton;`
- In `Start()`, after the `menuButton` query (line 49), add:
  ```csharp
  viewLeaderboardButton = root.Q<Button>("view-leaderboard-button");
  ```
- Register callback after the `menuButton` registration (line 52):
  ```csharp
  viewLeaderboardButton?.RegisterCallback<ClickEvent>(OnViewLeaderboardClicked);
  ```
- In `OnDestroy()`, after the `menuButton` unregister (line 69), add:
  ```csharp
  viewLeaderboardButton?.UnregisterCallback<ClickEvent>(OnViewLeaderboardClicked);
  ```
- Add click handler method (after `OnMenuClicked`, ~line 197):
  ```csharp
  private void OnViewLeaderboardClicked(ClickEvent evt)
  {
      if (AppManager.Instance != null)
      {
          AppManager.Instance.ShowLeaderboard();
      }
  }
  ```
- Reuse: `AppManager.Instance.ShowLeaderboard()` from `Assets/Scripts/Core/AppManager.cs:166` — already calls `SetState(AppState.Leaderboard)` which handles scene loading to MainMenu if needed

### Testing

**Test strategy**: Manual verification — this is a UI wiring change with no new logic. The click handler delegates to an existing, tested method (`ShowLeaderboard`).

**Manual verification**:
1. Play a full round (6 shots) → game over screen appears
2. Verify three buttons visible in order: PLAY AGAIN (green), VIEW LEADERBOARD (ghost), MENU (ghost)
3. Tap VIEW LEADERBOARD → navigates to leaderboard screen showing scores
4. Play another round → verify game over screen reappears correctly (no stale state)
5. Verify button participates in panel fade-in animation (appears with panel, not before)
6. Verify OnDestroy cleanup: navigate away and back — no console errors about leaked callbacks

**Existing test helpers to reuse**: Cloud Code tests (`node --test Assets/CloudCode/validate-and-post-score.test.js`) are unrelated. No existing UI test infrastructure for this screen.

### Risks
- None significant. This is a two-file additive change touching no existing logic. The call site (`ShowLeaderboard`) is proven and handles its own scene transitions.

### Insights
- The game over panel uses inline `style` on UXML elements for spacing rather than USS classes — this is the established pattern for per-element margin overrides in this project.

Skip Agents: visual-designer
