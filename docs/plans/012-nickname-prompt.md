## Implementation Plan

> **Agent:** `software-architect`

**Approach**: Add a `NicknamePromptController` that intercepts the Title state on first launch (no `PlayerPrefs` nickname), collects a display name, persists it, and passes it to `IAuthService.UpdateDisplayNameAsync()`. The nickname flows into UGS via `UpdatePlayerNameAsync()` so leaderboard entries show real names. The prompt is a new UI Toolkit screen (modal layer) that gates the main menu — simple overlay, no new `AppState` needed.

**Key decisions**:
- **No new `AppState`**: The nickname prompt shows as an overlay on the Title state rather than adding a new enum value. This avoids auditing every `AppState` subscriber for implicit else-branches. `MainMenuController` checks `PlayerPrefs` and shows/hides the prompt accordingly.
- **`IUgsAuthProvider` gains `UpdatePlayerNameAsync`**: Keeps the provider abstraction consistent — all UGS auth calls go through the provider interface, not direct SDK access in `UgsAuthService`.
- **`PlayerPrefs` as source of truth**: The nickname is read from `PlayerPrefs` in both `UgsAuthService.GetPlayerInfoAsync()` and `MockAuthService`, keeping the two paths symmetric and testable.

### Files to modify
- `Assets/Scripts/Multiplayer/IAuthService.cs` — add `Task UpdateDisplayNameAsync(string name)` to the interface
- `Assets/Scripts/Multiplayer/UgsAuthService.cs` — implement `UpdateDisplayNameAsync` via provider, update `GetPlayerInfoAsync` to read `PlayerPrefs` nickname
- `Assets/Scripts/Multiplayer/MockAuthService.cs` — implement `UpdateDisplayNameAsync` (stores in `PlayerInfo` struct), read `PlayerPrefs` nickname in no-arg constructor
- `Assets/Scripts/Multiplayer/UgsSdkProviders.cs` — add `Task UpdatePlayerNameAsync(string name)` to `IUgsAuthProvider` and implement in `DefaultUgsAuthProvider`
- `Assets/Scripts/Multiplayer/Bootstrap.cs` — call `UpdateDisplayNameAsync` after sign-in when a nickname exists in `PlayerPrefs`
- `Assets/Scripts/UI/MainMenuController.cs` — on `Title` state, check `PlayerPrefs` for nickname; if missing, find and show `NicknamePromptController`; subscribe to its completion event

### Files to create
- `Assets/Scripts/UI/NicknamePromptController.cs` — UI Toolkit controller for the nickname input overlay
- `Assets/UI/Screens/NicknamePrompt.uxml` — UXML layout for the nickname prompt (text field, Save button, Skip button)

### Steps

**1. Add `UpdateDisplayNameAsync` to `IAuthService`**
- File: `Assets/Scripts/Multiplayer/IAuthService.cs`
- Add method signature:
  ```csharp
  Task UpdateDisplayNameAsync(string displayName);
  ```

**2. Add `UpdatePlayerNameAsync` to `IUgsAuthProvider` and implement in `DefaultUgsAuthProvider`**
- File: `Assets/Scripts/Multiplayer/UgsSdkProviders.cs`
- Add to `IUgsAuthProvider`:
  ```csharp
  Task UpdatePlayerNameAsync(string name);
  ```
- Implement in `DefaultUgsAuthProvider`:
  ```csharp
  public Task UpdatePlayerNameAsync(string name)
  {
      return AuthenticationService.Instance.UpdatePlayerNameAsync(name);
  }
  ```

**3. Implement `UpdateDisplayNameAsync` in `UgsAuthService` and update `GetPlayerInfoAsync`**
- File: `Assets/Scripts/Multiplayer/UgsAuthService.cs`
- `UpdateDisplayNameAsync`:
  ```csharp
  public async Task UpdateDisplayNameAsync(string displayName)
  {
      if (!IsSignedIn) await SignInAsync();
      await _auth.UpdatePlayerNameAsync(displayName);
      Debug.Log($"[UgsAuth] Display name updated: {displayName}");
  }
  ```
- Update `GetPlayerInfoAsync` to read `PlayerPrefs`:
  ```csharp
  var nickname = PlayerPrefs.GetString("nickname", "");
  DisplayName = string.IsNullOrEmpty(nickname) ? $"Player_{_auth.PlayerId[..6]}" : nickname
  ```

**4. Implement `UpdateDisplayNameAsync` in `MockAuthService` and read `PlayerPrefs` nickname**
- File: `Assets/Scripts/Multiplayer/MockAuthService.cs`
- No-arg constructor: read `PlayerPrefs.GetString("nickname", "")` and use it for `DisplayName` if non-empty, otherwise keep `"You"`.
- `UpdateDisplayNameAsync`: replace `playerInfo` struct with updated `DisplayName` and return `Task.CompletedTask`.
- Note: `playerInfo` must become non-readonly to support mutation via struct replacement.
  ```csharp
  public Task UpdateDisplayNameAsync(string displayName)
  {
      playerInfo = new PlayerInfo
      {
          PlayerId = playerInfo.PlayerId,
          DisplayName = displayName,
          Token = playerInfo.Token
      };
      return Task.CompletedTask;
  }
  ```

**5. Call `UpdateDisplayNameAsync` in Bootstrap after sign-in**
- File: `Assets/Scripts/Multiplayer/Bootstrap.cs`
- In the `#if UNITY_WEBGL && !UNITY_EDITOR` block, after `await authService.SignInAsync()`:
  ```csharp
  var nickname = PlayerPrefs.GetString("nickname", "");
  if (!string.IsNullOrEmpty(nickname))
  {
      await authService.UpdateDisplayNameAsync(nickname);
  }
  ```

**6. Create `NicknamePrompt.uxml`**
- File: `Assets/UI/Screens/NicknamePrompt.uxml`
- Layout: centered panel (reuse `.screen-root`, `.panel` classes from `Common.uss`), containing:
  - Heading label: "ENTER NICKNAME"
  - `TextField` with name `nickname-field`, max-length 20, placeholder "Your name..."
  - Row with two buttons: `save-button` ("SAVE", `.btn-primary`) and `skip-button` ("SKIP", `.btn-secondary`)
- Reference `Common.uss` stylesheet. Use existing design tokens.

**7. Create `NicknamePromptController.cs`**
- File: `Assets/Scripts/UI/NicknamePromptController.cs`
- Namespace: `GolfGame.UI`
- Pattern: follows `SettingsController` — `MonoBehaviour` with `UIDocument`, self-managed visibility, starts hidden.
- Key interface:
  ```csharp
  namespace GolfGame.UI
  {
      public class NicknamePromptController : MonoBehaviour
      {
          // Fires when user saves or skips. Payload: saved nickname (empty string if skipped).
          public event Action<string> OnNicknameComplete;

          public void Show() { ... }
          private void OnSaveClicked(ClickEvent evt) { ... }
          private void OnSkipClicked(ClickEvent evt) { ... }
      }
  }
  ```
- `OnSaveClicked`: read `TextField` value, trim whitespace. If empty after trim, treat as skip. Otherwise save to `PlayerPrefs.SetString("nickname", value)` and `PlayerPrefs.SetInt("nickname_prompted", 1)`, call `PlayerPrefs.Save()`. Fire `OnNicknameComplete(value)`. Hide self. Call `UpdateDisplayNameAsync` on `ServiceLocator.Get<IAuthService>()` fire-and-forget (`_ = UpdateNameAsync(value)` with try/catch inside).
- `OnSkipClicked`: set `PlayerPrefs.SetInt("nickname_prompted", 1)` and `PlayerPrefs.Save()`. Fire `OnNicknameComplete("")`. Hide self.
- The prompt shows only when `PlayerPrefs.GetString("nickname", "")` is empty AND `PlayerPrefs.GetInt("nickname_prompted", 0) == 0`.

**8. Wire `MainMenuController` to show nickname prompt on first launch**
- File: `Assets/Scripts/UI/MainMenuController.cs`
- In `HandleAppStateChanged`, when `state == AppState.Title`:
  - Check `PlayerPrefs.GetString("nickname", "")` is empty AND `PlayerPrefs.GetInt("nickname_prompted", 0) == 0`
  - If so: find `NicknamePromptController` via `FindFirstObjectByType<NicknamePromptController>()`, subscribe to `OnNicknameComplete`, call `Show()`, and hide the main menu (`SetVisible(false)`).
  - On `OnNicknameComplete`: unsubscribe, show main menu (`SetVisible(true)`).
  - If nickname already set or already prompted: show main menu normally (existing behavior, no change).

### Testing

**Test strategy**: Manual verification in Unity Editor (mock path) and WebGL build (UGS path). No new automated test files — the feature is UI + `PlayerPrefs` + UGS SDK calls, all of which require Unity runtime.

**Manual verification**:
1. **First launch (editor)**: Clear `PlayerPrefs` (Edit > Clear All PlayerPrefs). Play. Verify nickname prompt appears before main menu. Enter name, click Save. Verify main menu appears. Check `PlayerPrefs` has `nickname` key.
2. **Skip path**: Clear `PlayerPrefs`. Play. Click Skip on nickname prompt. Verify main menu appears. Replay — verify prompt does NOT reappear. Verify leaderboard shows fallback `Player_` name format.
3. **Return launch**: After saving a nickname, stop and replay. Verify prompt does NOT appear. Verify `GetPlayerInfoAsync()` returns the saved nickname as `DisplayName`.
4. **Max length**: Attempt to type more than 20 characters in the field. Verify `TextField` max-length attribute enforces the limit.
5. **Empty save**: Click Save with empty/whitespace-only field. Verify it's treated as skip (no `PlayerPrefs` write for nickname, only `nickname_prompted` flag set).
6. **WebGL build**: Deploy, sign in anonymously, save nickname. Verify UGS player name updates (check via UGS dashboard or leaderboard display showing nickname).
7. **Leaderboard display**: After setting nickname, post a score. Verify leaderboard entry shows nickname instead of `Player_abc123`.

### Risks
- **UGS `UpdatePlayerNameAsync` rate limits**: UGS may throttle name updates. Mitigation: only call on save (not every launch if name hasn't changed). Bootstrap can compare `PlayerPrefs` value against a cached "last synced name" to skip redundant calls in future iterations if needed.
- **`PlayerPrefs` unavailable in WebGL private browsing**: `PlayerPrefs` uses `IndexedDB` in WebGL. Private/incognito may block it. Mitigation: acceptable degradation — prompt reappears each session, which is fine for the skip-friendly design.

### Insights
- The `IUgsAuthProvider` abstraction cleanly separates SDK calls from business logic, making it straightforward to add new UGS capabilities without touching `UgsAuthService` internals directly.
- `PlayerPrefs` is the established persistence mechanism in this project (`SettingsController`, `PlayerPrefsBestScoreService`), so using it for nickname is consistent with existing patterns.

Skip Agents: visual-designer
