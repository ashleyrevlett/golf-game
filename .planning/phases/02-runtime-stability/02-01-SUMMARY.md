---
phase: 02-runtime-stability
plan: 02-01
subsystem: core-stability
tags: [stability, null-safety, async, physics, leaderboard]
dependency_graph:
  requires: []
  provides: [leaderboard-retry-capped, async-void-lifecycle-safe, camera-null-guarded, physics-60hz]
  affects: [LeaderboardManager, ScoringManager, CameraController, TimeManager]
tech_stack:
  added: []
  patterns: [this-null-post-await-guard, camera-main-null-check, retry-queue-cap]
key_files:
  created: []
  modified:
    - Assets/Scripts/Multiplayer/LeaderboardManager.cs
    - Assets/Scripts/Environment/ScoringManager.cs
    - Assets/Scripts/Camera/CameraController.cs
    - ProjectSettings/TimeManager.asset
decisions:
  - Cap retry queue at MaxRetryQueueSize=10 constant; drop with LogWarning when full
  - this==null post-await guard pattern chosen over CancellationToken (minimal diff)
  - Camera.main retrieved into local variable before null check (not double-access)
  - Debug.LogWarning (not LogError) for camera tag lookup null; warnings are diagnostic not fatal
metrics:
  duration: "2 minutes"
  completed: "2026-03-23"
  tasks_completed: 5
  files_modified: 4
---

# Phase 2 Plan 1: Core Stability Fixes Summary

## One-liner

Capped leaderboard retry queue at 10, added post-await lifecycle guards in async void handlers, null-guarded Camera.main in ScoringManager, added diagnostic warnings for camera tag lookup failures, and set physics to 60Hz.

## What Was Built

Five targeted stability fixes across four files:

1. **STAB-01 — Retry queue cap:** Added `MaxRetryQueueSize = 10` constant to `LeaderboardManager`. In `PostScoreWithRetryAsync`, entries are silently dropped with a `LogWarning` when the queue is already at capacity. Prevents unbounded memory growth on iOS Safari when network is flaky.

2. **STAB-02 — Post-await lifecycle guards:** Added `if (this == null) return;` after each `await` point in `LeaderboardManager.Start`, `HandleBestDistanceUpdated`, and `HandleGameOver`. Prevents accessing destroyed MonoBehaviour after async resumption. All three methods already had outer try-catch — guards were added inside the existing pattern.

3. **STAB-03 — Camera.main null guard in ScoringManager:** Line 179 of `HandleBallLanded` previously called `Camera.main` inline in a method argument, preventing any null check. Extracted to a local variable, null-checked with `Debug.LogWarning`, and skipped the `ShotPopup.Create` call if null.

4. **STAB-04 — Camera tag lookup diagnostics:** `CameraController.Start` now logs `Debug.LogWarning` for each of the three Cinemachine camera tag lookups (`TeeCamera`, `FlightCamera`, `LandingCamera`) when `FindWithTag` returns null. Previously these were silent failures.

5. **STAB-05 — Physics timestep 60Hz:** Changed `ProjectSettings/TimeManager.asset` `m_FixedTimestep` from `0.02` (50Hz) to `0.01666667` (60Hz). This matches WebGL's assumed frame rate and improves physics simulation accuracy for ball flight.

## Commits

| Task | Commit | Files |
|------|--------|-------|
| STAB-01 + STAB-02 (LeaderboardManager) | 52c8590 | Assets/Scripts/Multiplayer/LeaderboardManager.cs |
| STAB-03 (ScoringManager Camera.main guard) | b0f2b8b | Assets/Scripts/Environment/ScoringManager.cs |
| STAB-04 (CameraController tag diagnostics) | d6f245d | Assets/Scripts/Camera/CameraController.cs |
| STAB-05 (Physics 60Hz) | e609508 | ProjectSettings/TimeManager.asset |

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None. All changes are complete fixes with no placeholder implementations.

## Self-Check: PASSED

Files verified:
- `Assets/Scripts/Multiplayer/LeaderboardManager.cs` — FOUND
- `Assets/Scripts/Environment/ScoringManager.cs` — FOUND
- `Assets/Scripts/Camera/CameraController.cs` — FOUND
- `ProjectSettings/TimeManager.asset` — FOUND

Commits verified:
- 52c8590 — FOUND
- b0f2b8b — FOUND
- d6f245d — FOUND
- e609508 — FOUND
