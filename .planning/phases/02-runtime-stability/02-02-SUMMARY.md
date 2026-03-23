---
phase: 02-runtime-stability
plan: 02
subsystem: golf
tags: [unity, csharp, physics, camera, cinemachine, null-safety]

# Dependency graph
requires:
  - phase: 02-runtime-stability-01
    provides: async exception safety and leaderboard queue cap (parallel plan)
provides:
  - Camera.main null guard in ScoringManager.HandleBallLanded with diagnostic warning
  - Tag-lookup failure warnings (Debug.LogWarning) for TeeCamera, FlightCamera, LandingCamera in CameraController.Start
  - Physics fixed timestep corrected to 60Hz (0.01666667s) in TimeManager.asset
affects: [02-runtime-stability, gameplay, physics, camera]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Null-guard Camera.main before use; log Debug.LogWarning and skip operation if null"
    - "After FindWithTag, null-check each result and emit Debug.LogWarning with tag name and scene fix hint"

key-files:
  created: []
  modified:
    - Assets/Scripts/Environment/ScoringManager.cs
    - Assets/Scripts/Camera/CameraController.cs
    - ProjectSettings/TimeManager.asset

key-decisions:
  - "Used var mainCamera = Camera.main; then null-check to avoid double property access (D-03)"
  - "Three separate if-blocks for camera null checks rather than single chained condition — matches plan spec and D-03 directive for per-tag warnings"
  - "TimeManager.asset targeted (not ProjectSettings.asset) — correct file for physics timestep in Unity 6"

patterns-established:
  - "Camera.main null guard: cache to local var, null-check with Debug.LogWarning, skip operation if null"
  - "FindWithTag null guard: assign then check with Debug.LogWarning('[Component] XTag not found — check scene tag assignment')"

requirements-completed:
  - STAB-03
  - STAB-04
  - STAB-05

# Metrics
duration: 8min
completed: 2026-03-22
---

# Phase 02 Plan 02: Runtime Stability — Camera Guards and Physics Timestep Summary

**Camera.main null-guarded in ScoringManager with warning log; CameraController emits Debug.LogWarning for each missing camera tag; physics fixed timestep changed from 50Hz to 60Hz (WebGL-aligned)**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-22T00:00:00Z
- **Completed:** 2026-03-22T00:08:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- ScoringManager.HandleBallLanded no longer dereferences Camera.main without a null check — crash on camera-less scenes is eliminated
- CameraController.Start logs a targeted Debug.LogWarning for each of TeeCamera, FlightCamera, and LandingCamera if FindWithTag returns null — silent lookup failures are now visible
- Physics fixed timestep updated to 0.01666667 (60Hz) in TimeManager.asset — aligns physics simulation with WebGL's target frame rate

## Task Commits

Each task was committed atomically:

1. **Task 1: Guard Camera.main in ScoringManager and add tag-lookup warnings in CameraController** - `8e902ad` (fix)
2. **Task 2: Set physics fixed timestep to 60Hz in TimeManager.asset** - `b60ca32` (chore)

**Plan metadata:** `173fe08` (docs: complete plan)

## Files Created/Modified

- `Assets/Scripts/Environment/ScoringManager.cs` - Added Camera.main null guard with Debug.LogWarning in HandleBallLanded; ShotPopup.Create now only called when camera is non-null
- `Assets/Scripts/Camera/CameraController.cs` - Added three Debug.LogWarning calls after FindWithTag assignments for TeeCamera, FlightCamera, LandingCamera
- `ProjectSettings/TimeManager.asset` - Changed m_FixedTimestep from 0.02 (50Hz) to 0.01666667 (60Hz)

## Decisions Made

- Used `var mainCamera = Camera.main;` (cache to local) then null-check, per plan spec and D-03 diagnostic logging directive
- Kept three separate null-check if-blocks in CameraController rather than a single compound condition — each tag gets its own named warning message for precise diagnosis
- Targeted `ProjectSettings/TimeManager.asset` for physics timestep (not `ProjectSettings.asset`) — plan correctly identifies TimeManager.asset as the right file despite CONTEXT.md D-06 naming the wrong asset

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- STAB-03, STAB-04, and STAB-05 requirements complete
- Three of six stability requirements addressed (STAB-01, STAB-02, STAB-06 addressed by plan 02-01)
- Phase 02 runtime stability goals fully addressed across both plans
- Ready for Phase 03 or deployment testing

## Self-Check: PASSED

- FOUND: Assets/Scripts/Environment/ScoringManager.cs
- FOUND: Assets/Scripts/Camera/CameraController.cs
- FOUND: ProjectSettings/TimeManager.asset
- FOUND: .planning/phases/02-runtime-stability/02-02-SUMMARY.md
- FOUND commit 8e902ad (Task 1 — camera guards)
- FOUND commit b60ca32 (Task 2 — physics timestep)
- FOUND commit 173fe08 (plan metadata)

---
*Phase: 02-runtime-stability*
*Completed: 2026-03-23*
