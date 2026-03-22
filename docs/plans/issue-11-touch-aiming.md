## Implementation Plan

> **Agent:** `software-architect`

**Approach**: Add a touch displacement threshold to `ShotInput` so that finger-down only counts as a "tap" (meter phase advance) when total displacement stays below ~10px. Touch taps register on finger-up instead of finger-down -- this is standard mobile tap behavior and naturally separates drags (aim) from taps (meter). Keyboard and mouse paths are unchanged.

**Key decisions**:
- Tap registers on `wasReleasedThisFrame` instead of `wasPressedThisFrame` for touch -- resolves the ambiguity between drag-to-aim and tap-to-advance without adding a dedicated aim control or gesture recognizer
- Threshold exposed as `[SerializeField] private float tapThresholdPx = 10f` -- allows tuning in inspector without recompile
- `touchStartPosition` tracked as a simple field on `ShotInput` -- one field, no new abstractions

### Files to modify
- `Assets/Scripts/Golf/ShotInput.cs` -- add touch displacement tracking, change touch detection from press-based to release-based with threshold

### Steps

**1. Add touch tracking fields to `ShotInput`**
- File: `Assets/Scripts/Golf/ShotInput.cs`
- Add after `private bool isActive;` (line 40):
  ```csharp
  [SerializeField] private float tapThresholdPx = 10f;
  private Vector2 touchStartPosition;
  ```

**2. Add `UpdateTouchTracking()` method**
- File: `Assets/Scripts/Golf/ShotInput.cs`
- Add new private method:
  ```csharp
  private void UpdateTouchTracking()
  {
      if (Touchscreen.current == null) return;
      var touch = Touchscreen.current.primaryTouch;
      if (touch.press.wasPressedThisFrame)
      {
          touchStartPosition = touch.position.ReadValue();
      }
  }
  ```
- Call it at the top of `Update()`, immediately after `if (!isActive) return;` and before the aim input / phase switch logic.

**3. Change touch detection in `WasActionPressed()` from press to release-with-threshold**
- File: `Assets/Scripts/Golf/ShotInput.cs`
- Replace lines 142-143:
  ```csharp
  // Before:
  if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
      return true;
  ```
  with:
  ```csharp
  if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
  {
      float displacement = Vector2.Distance(
          Touchscreen.current.primaryTouch.position.ReadValue(),
          touchStartPosition);
      if (displacement < tapThresholdPx)
          return true;
  }
  ```
  Touch taps now register on release. Aim drags (displacement >= threshold) don't trigger the meter.

**4. No changes to `SetInputActive()` needed**
- `touchStartPosition` is a value type (Vector2) defaulting to zero. It gets a fresh value on each `wasPressedThisFrame`, so no explicit reset is necessary.

### Testing

**Test strategy**: EditMode unit test for threshold default + manual verification for the touch behavior (touch input requires a real or simulated device).

**Test files**:
- `Assets/Tests/EditMode/ShotInputTests.cs` -- add threshold default test to existing file

**Test cases**:
- `TapThresholdPx_DefaultsTo10`: Assert the serialized field default is 10f via reflection. Guards against accidental threshold changes.

**Existing test helpers to reuse**: `ShotInputTests` setup/teardown pattern (create GameObject + AddComponent, DestroyImmediate in teardown).

**Manual verification** (required -- touch can't be unit-tested in EditMode):
1. Unity Play mode with Device Simulator or Unity Remote:
   - Horizontal drag during `MeterPhase.Idle` -> aim angle changes, meter stays Idle
   - Short tap during Idle -> meter advances to Power
   - Tap during Power -> locks power
   - Tap during Accuracy -> fires shot
2. Desktop input unchanged:
   - Space bar starts/advances meter
   - Left click starts/advances meter
   - Right-click drag changes aim
   - Arrow keys change aim

### Risks
- **Tap-on-release latency**: Moving from press-to-start to release-to-start adds ~50-100ms to meter activation. Acceptable for a casual golf game and matches standard mobile tap UX. If it feels sluggish, threshold can be lowered.
- **Release position drift**: On some devices the final touch position may differ slightly from the last move position. The 10px threshold is generous enough (standard mobile tap thresholds are 8-15px).

### Insights
- When the same physical gesture (finger-down) maps to two distinct game actions (aim drag vs meter tap), resolve the ambiguity with displacement thresholding on release rather than trying to predict intent on press. This is a standard mobile input pattern.

Skip Agents: visual-designer
