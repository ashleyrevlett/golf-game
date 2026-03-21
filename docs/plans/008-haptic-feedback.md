## 🏗️ Implementation Plan

> **Agent:** `software-architect`

**Approach**: Add haptic feedback at ball launch via a JS bridge (`Haptics.jslib`) and a static `HapticsManager` in `GolfGame.Core`, following the exact pattern established by `TouchInputBlocker.cs` + `TouchBlocker.jslib`. The manager reads a PlayerPrefs toggle so users can disable vibration. BallController calls the manager after launch with the shot's power level to select pulse intensity.

**Key decisions**:
- **HapticsManager in Core, not Golf** — mirrors `TouchInputBlocker` placement; `GolfGame.Golf` already references `GolfGame.Core`, so no assembly def changes needed
- **Static class, not MonoBehaviour** — no per-frame work, no scene wiring, just a stateless bridge to JS. Simpler than a singleton.
- **PlayerPrefs toggle separate from volume** — haptics is a distinct concern from audio. New `"HapticsEnabled"` key, defaulting to `1` (on). Keeps the settings surface clean.
- **Feature detection in JS, not C#** — the jslib checks `'vibrate' in navigator` so the C# side doesn't need to know about browser capabilities. Graceful no-op on desktop.
- **Two JS functions** — `TriggerHaptic(int ms)` for simple pulses and `TriggerHapticPattern(int[] pattern, int length)` for multi-segment patterns. The C# side decides which to call based on power threshold.

### Files to modify
- `Assets/Scripts/Golf/BallController.cs` — add haptic call after `OnBallLaunched?.Invoke()`
- `Assets/Scripts/UI/SettingsController.cs` — wire haptics toggle to PlayerPrefs
- `Assets/UI/Screens/Settings.uxml` — add haptics toggle element

### Files to create
- `Assets/Plugins/WebGL/Haptics.jslib` — JS bridge exposing vibration API to C#
- `Assets/Scripts/Core/HapticsManager.cs` — static C# wrapper with platform guards and power-tiered logic

### Steps

**1. Create the JS bridge (`Haptics.jslib`)**
- File: `Assets/Plugins/WebGL/Haptics.jslib`
- Follow the `mergeInto(LibraryManager.library, {...})` pattern from `TouchBlocker.jslib`
- Two exported functions:
  - `TriggerHaptic(durationMs)` — calls `navigator.vibrate(durationMs)` if available
  - `TriggerHapticPattern(patternPtr, length)` — reads an Int32Array from the HEAP, calls `navigator.vibrate([...pattern])` if available
- Both guard with `if (typeof navigator !== 'undefined' && 'vibrate' in navigator)`
- Key interface:
  ```javascript
  TriggerHaptic: function(durationMs) {
      if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
          navigator.vibrate(durationMs);
      }
  },
  TriggerHapticPattern: function(patternPtr, length) {
      if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
          var pattern = [];
          for (var i = 0; i < length; i++) {
              pattern.push(HEAP32[(patternPtr >> 2) + i]);
          }
          navigator.vibrate(pattern);
      }
  }
  ```

**2. Create the C# haptics manager**
- File: `Assets/Scripts/Core/HapticsManager.cs`
- Namespace: `GolfGame.Core`
- Static class with `#if UNITY_WEBGL && !UNITY_EDITOR` guarded `[DllImport("__Internal")]` declarations (identical pattern to `TouchInputBlocker.cs:14-16`)
- Constants: `PlayerPrefsKey = "HapticsEnabled"`, `PowerThreshold = 0.7f`, `LowPowerDurationMs = 50`, `HighPowerPattern = new[] { 80, 30, 50 }`
- Public API:
  ```csharp
  public static bool IsEnabled { get; set; }  // reads/writes PlayerPrefs
  public static void TriggerShotHaptic(float powerNormalized)
  ```
- `TriggerShotHaptic` logic:
  - Early return if `!IsEnabled`
  - If `powerNormalized < PowerThreshold`: call `TriggerHaptic(50)`
  - Else: call `TriggerHapticPattern` with `[80, 30, 50]`
- On non-WebGL platforms, all methods are no-ops (empty method bodies inside `#else` blocks or conditional compilation)
- Load `IsEnabled` from `PlayerPrefs.GetInt("HapticsEnabled", 1) == 1` in a static initializer

**3. Wire haptics into BallController.Launch()**
- File: `Assets/Scripts/Golf/BallController.cs`
- After line 81 (`OnBallLaunched?.Invoke();`), add:
  ```csharp
  HapticsManager.TriggerShotHaptic(shot.PowerNormalized);
  ```
- Single line addition. No other changes to BallController.
- Reuse: `shot.PowerNormalized` already computed — no new plumbing needed.

**4. Add haptics toggle to Settings UI**
- File: `Assets/UI/Screens/Settings.uxml`
- After the quality toggle `VisualElement` (line 15), add a new section:
  ```xml
  <ui:VisualElement style="margin-bottom: 24px;">
      <ui:Label text="Haptic Feedback" class="text-body" style="margin-bottom: 8px;" />
      <ui:Toggle name="haptics-toggle" label="Vibration on Shot" style="min-height: 44px;" />
  </ui:VisualElement>
  ```

**5. Wire haptics toggle in SettingsController**
- File: `Assets/Scripts/UI/SettingsController.cs`
- Add `using GolfGame.Core;`
- Add field: `private Toggle hapticsToggle;`
- In `Start()`: query `root.Q<Toggle>("haptics-toggle")`, load from `PlayerPrefs.GetInt("HapticsEnabled", 1) == 1`, register `OnHapticsChanged` callback
- Add handler:
  ```csharp
  private void OnHapticsChanged(ChangeEvent<bool> evt)
  {
      PlayerPrefs.SetInt("HapticsEnabled", evt.newValue ? 1 : 0);
      HapticsManager.IsEnabled = evt.newValue;
  }
  ```
- In `OnDestroy()`: unregister callback

### Testing

**Test strategy**: Manual verification (Unity Editor play mode + WebGL build on mobile device). The jslib bridge is untestable in Editor; the C# logic is trivially simple (threshold comparison + PlayerPrefs read). No unit tests warranted — the functions are stateless one-liners behind platform guards.

**Manual verification**:
- **Editor play mode**: Confirm no errors/warnings in console. Confirm `HapticsManager.TriggerShotHaptic()` is called (add temporary `Debug.Log` or verify via breakpoint). No vibration expected — that's correct behavior.
- **WebGL build on mobile (Chrome Android)**: Trigger shots at varying power levels. Verify short pulse on low-power shots and distinct multi-pulse pattern on high-power shots. Toggle haptics off in settings — verify no vibration. Toggle back on — verify vibration resumes.
- **WebGL build on desktop browser**: Verify no console errors. `navigator.vibrate` is absent — the jslib should silently no-op.
- **Settings persistence**: Toggle haptics off, reload page, verify toggle state is restored from PlayerPrefs.

### Risks
- **Browser vibration API support**: Safari iOS does not support `navigator.vibrate()`. This is a known platform limitation — the feature detection guard means it silently no-ops. Android Chrome is the primary target and has full support. No mitigation needed beyond the existing guard.

### Insights
- The `TouchInputBlocker.cs` + `TouchBlocker.jslib` pair is a clean, reusable pattern for any browser API bridge in this project. Future browser-API integrations (e.g., fullscreen, clipboard, share) should follow the same structure.

---

Skip Agents: visual-designer
