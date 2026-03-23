# Phase 2: Runtime Stability - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix null reference exceptions, unhandled async exceptions, and physics timestep issues so the game completes a full 6-shot round without crashes or silent failures. No new features, no visual changes — stability only.

This phase delivers:
- Leaderboard retry queue capped (prevents iOS Safari memory crash)
- Async fire-and-forget calls exception-safe
- Camera.main and tagged object lookups null-guarded with diagnostic logging
- Post-await MonoBehaviour lifecycle guards
- Physics fixed timestep corrected to 60Hz

**Out of scope:** UI redesign, new gameplay features, performance profiling, C# unit tests (v2).

</domain>

<decisions>
## Implementation Decisions

### Leaderboard failure feedback
- **D-01:** When score submission fails and the retry queue is exhausted, show an inline text notice on the existing Game Over screen — e.g. "Score couldn't be submitted". No new UI components; add a label to the existing Game Over UXML/controller.
- **D-02:** Retry queue is capped at 10 entries (per STAB-01). Entries beyond the cap are silently dropped after the inline notice is shown.

### Null check logging
- **D-03:** All null guards for `Camera.main` and tagged object lookups (`FindWithTag`, `FindFirstObjectByType`) must emit `Debug.LogWarning` with a descriptive message when the lookup returns null, before returning/skipping. This applies to STAB-03 (ScoringManager) and STAB-04 (CameraController).

### Async exception handling
- **D-04:** All `async void` fire-and-forget methods in `LeaderboardManager` and `GameOverController` must have an outer try-catch wrapping the entire method body. Exceptions are logged with `Debug.LogError`, not rethrown (per STAB-02).
- **D-05:** Post-`await` lifecycle guards use the `if (this == null) return;` pattern — simple, minimal diff, no CancellationToken refactor (per STAB-06).

### Physics timestep
- **D-06:** Set `Time.fixedDeltaTime` to `1f / 60f` (60Hz) in `ProjectSettings/ProjectSettings.asset`. This is a global change — no per-script override needed.

### Claude's Discretion
- Exact wording of the Game Over failure notice
- Whether to log the retry queue cap hit at Warning or Error level
- Where in the Game Over UXML to place the failure notice (below scores, above share button, etc.)

</decisions>

<specifics>
## Specific Ideas

- "Inline notice on Game Over screen" — a small text label, not a modal or toast. Should feel like a secondary status line, not an alarm.
- Keep it minimal: fix the listed bugs exactly, no opportunistic refactors of surrounding code.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Runtime bug locations (confirmed by codebase scout)
- `Assets/Scripts/Multiplayer/LeaderboardManager.cs` — unbounded retry queue (STAB-01), async void handlers (STAB-02), missing post-await guards (STAB-06)
- `Assets/Scripts/Environment/ScoringManager.cs` — `Camera.main` unguarded at line 179 in `HandleBallLanded` (STAB-03)
- `Assets/Scripts/Camera/CameraController.cs` — silent tag lookup failures, no error log (STAB-04)
- `Assets/Scripts/UI/GameOverController.cs` — async void with try-catch but missing `this == null` guards after awaits (STAB-06); also receives the leaderboard failure notice (D-01)

### Requirements
- `.planning/REQUIREMENTS.md` §Runtime Stability — STAB-01 through STAB-06 exact acceptance criteria
- `CLAUDE.md` §Async Services — fire-and-forget pattern with `_ = MethodAsync()`, exceptions caught inside method
- `CLAUDE.md` §Reference Wiring — preferred null-check patterns, tags to use

### UI files (for Game Over failure notice)
- `Assets/UI/` — UXML/USS files for Game Over screen (confirm exact file names before editing)
- `docs/ui-patterns.md` — UI Toolkit patterns for this project

### Physics
- `ProjectSettings/ProjectSettings.asset` — `fixedDeltaTime` field (STAB-05)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `LeaderboardManager.cs` already has try-catch in some async void handlers — extend the pattern, don't rewrite
- `GameOverController.cs` already has try-catch in `UpdateFinalScore` — add `this == null` guard after each await
- Game Over UXML exists — add a label element for the failure notice without restructuring the layout

### Established Patterns
- Null-conditional tag lookups: `GameObject.FindWithTag("Tag")?.GetComponent<T>()` — extend with `Debug.LogWarning` on null
- `_ = MethodAsync()` fire-and-forget pattern already used in codebase
- All C# files use spaces (no tabs) — lint will now enforce this

### Integration Points
- `GameOverController` receives leaderboard result and will need to show/hide the failure notice label
- `LeaderboardManager` retry queue needs a cap and must signal failure to `GameOverController` (via event or return value from the submit method)

</code_context>

<deferred>
## Deferred Ideas

- Sentry/error tracking for unhandled WebGL exceptions — v2 (OBS-01)
- Distinguishing network vs auth vs rate-limit failures in logs — v2 (OBS-02)
- CancellationToken-based async lifecycle management — cleaner than `this == null` guards but larger diff, not worth it this milestone

</deferred>

---

*Phase: 02-runtime-stability*
*Context gathered: 2026-03-23*
