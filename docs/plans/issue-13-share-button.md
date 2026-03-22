## Implementation Plan

> **Agent:** `software-architect`

**Approach**: Add a share button to the game over screen that calls the Web Share API via a jslib bridge, falling back to clipboard copy. Follows the existing jslib pattern (`Haptics.jslib`) and the existing button layout in `GameOver.uxml`. A static helper method formats the share text, keeping it unit-testable in EditMode.

**Key decisions**:
- **Separate jslib file** (`ShareBridge.jslib`): Keeps sharing logic isolated from haptics/touch, matching the one-concern-per-jslib pattern.
- **Callback from JS to C#**: JS calls `SendMessage` on the Unity game object to report success/failure, so the button text can update to "Shared!" or "Copied!" without polling.
- **Static `FormatShareText` helper**: Extracted as a `public static` method on `GameOverController` for direct EditMode unit testing -- matches the existing pattern (`FormatFinalScore`, `FormatBestScoreLabel`).
- **No coroutine for reset timer**: Use `VisualElement.schedule.Execute` (already used in `AnimateShotRows`) to reset button text after 2s -- avoids mixing coroutines with UI-only logic.

### Files to modify
- `Assets/Scripts/UI/GameOverController.cs` -- add share button field, click handler, jslib extern, text reset logic
- `Assets/UI/Screens/GameOver.uxml` -- add share button element between leaderboard and menu buttons
- `Assets/Tests/EditMode/GameOverControllerTests.cs` -- add tests for `FormatShareText`

### Files to create
- `Assets/Plugins/WebGL/ShareBridge.jslib` -- JS interop for Web Share API with clipboard fallback

### Steps

**1. Create `ShareBridge.jslib`**
- File: `Assets/Plugins/WebGL/ShareBridge.jslib`
- Follow `Haptics.jslib` pattern: `mergeInto(LibraryManager.library, { ... })`
- One exported function: `ShareScore(textPtr, gameObjectNamePtr)`
- Calls `navigator.share()` if available, else falls back to `navigator.clipboard.writeText()`
- On success, calls `SendMessage(gameObjectName, 'OnShareResult', 'shared')` or `'copied'`
- On failure or dismiss, calls with `'failed'`
- Dual rejection handling per issue spec: try/catch around the whole block for sync throws (API absent), `.catch()` on the promise for async rejections (user dismiss, permission denied)
- Key interface:
  ```js
  ShareScore: function(textPtr, gameObjectNamePtr) {
      var text = UTF8ToString(textPtr);
      var goName = UTF8ToString(gameObjectNamePtr);
      var url = window.location.href;
      var fullText = text + " " + url;
      try {
          if (navigator.share) {
              navigator.share({ text: fullText }).then(function() {
                  SendMessage(goName, 'OnShareResult', 'shared');
              }).catch(function() {
                  SendMessage(goName, 'OnShareResult', 'failed');
              });
          } else if (navigator.clipboard) {
              navigator.clipboard.writeText(fullText).then(function() {
                  SendMessage(goName, 'OnShareResult', 'copied');
              }).catch(function() {
                  SendMessage(goName, 'OnShareResult', 'failed');
              });
          } else {
              SendMessage(goName, 'OnShareResult', 'failed');
          }
      } catch(e) {
          SendMessage(goName, 'OnShareResult', 'failed');
      }
  }
  ```

**2. Add share button to `GameOver.uxml`**
- File: `Assets/UI/Screens/GameOver.uxml`
- Add `<ui:Button name="share-button" text="SHARE" class="btn-secondary" style="margin-bottom: 8px;" />` after `view-leaderboard-button`, before `menu-button`
- This places it between leaderboard and menu per acceptance criteria

**3. Wire share button in `GameOverController.cs`**
- File: `Assets/Scripts/UI/GameOverController.cs`
- Add field: `private Button shareButton;`
- Add field to track current totalCtp: `private float lastTotalCtp;`
- In `Start()`: query `share-button`, register click callback `OnShareClicked`
- In `OnDestroy()`: unregister callback
- Store `totalCtp` in `HandleAllShotsComplete` so the share handler can access it
- Add `FormatShareText` static helper:
  ```csharp
  public static string FormatShareText(float totalCtp)
  {
      return $"I scored {totalCtp:F1} yds in Golf Game \u2014 beat me!";
  }
  ```
- Add DllImport extern (guarded):
  ```csharp
  #if UNITY_WEBGL && !UNITY_EDITOR
  [System.Runtime.InteropServices.DllImport("__Internal")]
  private static extern void ShareScore(string text, string gameObjectName);
  #endif
  ```
- Add click handler:
  ```csharp
  private void OnShareClicked(ClickEvent evt)
  {
      #if UNITY_WEBGL && !UNITY_EDITOR
      ShareScore(FormatShareText(lastTotalCtp), gameObject.name);
      #else
      Debug.Log($"[GameOverController] Share (editor): {FormatShareText(lastTotalCtp)}");
      OnShareResult("copied");
      #endif
  }
  ```
- Add `OnShareResult(string result)` callback (called by JS `SendMessage`):
  ```csharp
  public void OnShareResult(string result)
  {
      if (shareButton == null) return;
      if (result == "shared")
          shareButton.text = "SHARED!";
      else if (result == "copied")
          shareButton.text = "COPIED!";
      // "failed" -- no feedback, button stays as SHARE

      if (result == "shared" || result == "copied")
      {
          shareButton.schedule.Execute(() => shareButton.text = "SHARE").StartingIn(2000);
      }
  }
  ```
- In `SetVisible(false)` path: reset `shareButton.text = "SHARE"` to clean state when hiding

**4. Add unit tests for `FormatShareText`**
- File: `Assets/Tests/EditMode/GameOverControllerTests.cs`
- Follow existing pattern (static method tests, NUnit `[Test]`)
- Test cases listed below

### Testing

**Test strategy**: EditMode unit tests for the static `FormatShareText` helper. The jslib bridge and button wiring are integration-level (require WebGL runtime) -- covered by manual verification.

**Test files**:
- `Assets/Tests/EditMode/GameOverControllerTests.cs` -- add share text format tests

**Test cases**:
- `FormatShareText_ContainsScoreWithOneDecimal`: asserts `FormatShareText(45.3f)` contains `"45.3 yds"`
- `FormatShareText_ContainsBeatMePhrase`: asserts output contains `"beat me!"`
- `FormatShareText_FormatsZeroScore`: asserts `FormatShareText(0f)` produces `"I scored 0.0 yds in Golf Game -- beat me!"`

**Existing test helpers to reuse**: None needed -- follows the same direct static-method-call pattern as `FormatFinalScore` tests.

**Manual verification**:
- Build WebGL and test on mobile browser: tap Share, verify native share sheet appears (or clipboard copy + "Copied!" feedback on desktop)
- Verify "Shared!"/"Copied!" text resets to "SHARE" after 2 seconds
- Verify button placement between View Leaderboard and Menu
- Test in Unity Editor Play Mode: confirm no crash (extern is guarded), debug log appears

### Risks
- **`SendMessage` requires matching GameObject name**: The jslib passes `gameObject.name` to `SendMessage`. If the GameObject is renamed in the scene, the callback breaks silently. Mitigation: use `gameObject.name` dynamically (not hardcoded), document this coupling.
- **Web Share API availability**: Not available on all mobile browsers (notably Firefox Android has limited support). The clipboard fallback handles this. No risk of unhandled errors due to dual catch pattern.

### Insights
- The existing jslib files (`Haptics.jslib`, `TouchBlocker.jslib`) don't use `SendMessage` callbacks -- this is the first async JS-to-C# callback in the project. If more jslib callbacks emerge, consider a shared callback pattern, but for now one direct `SendMessage` is simplest.

---

Skip Agents: visual-designer
