## Implementation Plan

> **Agent:** `software-architect`

**Approach**: No implementation needed. The issue requests EditMode tests for AudioManager, BallAudioController, and AmbientAudioController. All three are already covered by `Assets/Tests/EditMode/AudioTests.cs` (628 lines, 29 tests). This file was committed across several earlier PRs and provides the exact coverage the issue asks for.

**Key decisions**:
- No new code: the work described in the issue acceptance criteria already exists in the codebase

### Existing coverage (verified)

| Class | Tests | Coverage |
|-------|-------|----------|
| `AudioManager` | 13 | Singleton lifecycle, pool allocation, PlaySFX/PlayLoop/StopSource, volume config, OnFirstUserGesture |
| `BallAudioController` | 3 | SetLaunchPower clamping, HandleBallBounced volume scaling, config fields |
| `AmbientAudioController` | 5 | HandleWindChanged volume scaling, HandleShotScored threshold logic (both directions + boundary), GetAmbientVolume fallback and config |
| Other audio tests | 8 | AudioConfig validation, integration scenarios |

### Steps

**1. Close the issue**
- No implementation steps. Every acceptance criterion from the issue is already met by existing tests.

### Testing

**Test strategy**: Already complete — 29 EditMode tests in `Assets/Tests/EditMode/AudioTests.cs`.

**Manual verification**: Run Unity Test Runner (EditMode) and confirm all 29 audio tests pass.

### Risks
- None. No code changes required.

### Recommendation

This issue should be closed as already-done. The `/improve` scan that generated it likely ran before the audio test coverage was committed.

Skip Agents: visual-designer,doc-writer
