# Project Research Summary

**Project:** Golf Game — Unity 6 WebGL CI/CD, Deployment, and Runtime Stability
**Domain:** Unity 6 WebGL game, GitLab CI pipeline, Cloudflare Pages hosting
**Researched:** 2026-03-22
**Confidence:** HIGH

## Executive Summary

This project has a well-built Unity 6 WebGL golf game codebase and a partially-configured CI/CD pipeline. The pipeline builds correctly in Docker via GameCI but has two blocking gaps preventing live deployment: the required GitLab CI/CD secrets (`UNITY_LICENSE` and `CLOUDFLARE_API_TOKEN`) have not been configured, and the deploy stage lacks a pinned wrangler version. The game itself is architecturally sound — an event-driven pub/sub system with two state machines — but carries several runtime risks that need targeted hardening before widespread mobile browser testing.

The recommended approach is to unblock CI/CD first (configure secrets, pin wrangler, create a `_headers` cache policy file), then correct contradictory documentation that would lead future developers astray, and finally address the three runtime stability areas: async void destroyed-object gaps in `LeaderboardManager`, the unbounded retry queue that creates iOS memory pressure, and iOS Safari context loss handling. The existing `decompressionFallback = true` setting in `WebGLBuildScript.cs` is the correct mitigation for Cloudflare's double-compression problem — this is the single most important thing to preserve, and it requires no changes.

The critical risk is documentation: `docs/ci-cd-gotchas.md` and `docs/deployment.md` describe Brotli compression when the build actually uses Gzip. A developer following these docs would "fix" the compression setting and break deployment. iOS Safari on iOS 17+ and 18.4+ also presents silent failures (context loss, memory ceiling page reload) that only reproduce on real hardware, not in editor or desktop browser emulation.

---

## Key Findings

### Recommended Stack

The build pipeline uses a verified-correct Docker image and sound deployment approach. No stack changes are required — the work is configuration and hardening of what already exists.

**Core technologies:**
- `unityci/editor:6000.3.10f1-webgl-3`: Unity WebGL headless build in GitLab CI — tag confirmed on Docker Hub; matches actual project version (`6000.3.10f1`, not `6000.0.23f1` as some docs incorrectly state)
- Gzip + `decompressionFallback = true`: WebGL compression for Cloudflare Pages — `.unityweb` file extension bypasses Cloudflare's compression interference; already set in `WebGLBuildScript.cs`, requires no change
- `wrangler@^4` (currently 4.76.0): Cloudflare Pages deployment — pin major version to prevent surprise breakage on future v5; currently unpinned in `.gitlab-ci.yml`
- Node.js 20 LTS: Deploy stage and Cloud Code tests — wrangler 4.x requires Node 18+; already used correctly
- Manual `.ulf` license file (`UNITY_LICENSE` variable): Unity license activation — simpler than GameCI serial activation; one variable vs three; Personal licenses expire every ~30 days

**Critical version note:** The project is Unity `6000.3.10f1` (Unity 6.3 LTS, supported through December 2027), not `6000.0.23f1` as stated in `.planning/PROJECT.md` and `.planning/codebase/STACK.md`. These docs need correcting but the actual CI image tag is already correct.

See `.planning/research/STACK.md` for full details including exact `gitlab-ci.yml` recommended changes.

### Expected Features

The pipeline work is infrastructure. "Table stakes" here means the pipeline must function at all.

**Must have (table stakes — pipeline non-functional without these):**
- `UNITY_LICENSE` configured in GitLab CI/CD variables — build stage cannot start without it; copy `.ulf` XML from `~/Library/Unity/Unity_lic.ulf` into the variable
- `CLOUDFLARE_API_TOKEN` configured in GitLab CI/CD variables — deploy stage cannot authenticate; create token with `Cloudflare Pages:Edit` permission
- `decompressionFallback = true` in `WebGLBuildScript.cs` — already set; must not be changed; Cloudflare cannot serve pre-compressed files with `Content-Encoding` headers
- Git LFS pull in CI — already handled in `.gitlab-ci.yml`; binary assets would arrive as pointer stubs without it

**Should have (reliability and performance):**
- `_headers` file with cache policy — `Cache-Control: immutable` on `Build/` assets (safe because `nameFilesAsHashes = true`); no-cache on HTML; do NOT set `Content-Encoding` headers
- Pinned wrangler version (`^4`) — one-line change in `.gitlab-ci.yml`
- Documentation corrections across 5 files — prevents future compression-setting mistakes
- Destroyed-object null guards in `async void` event handlers (`LeaderboardManager`, `GameOverController`)
- Bounded retry queue in `LeaderboardManager` — cap at 10 entries to prevent iOS memory pressure
- iOS Safari `webglcontextlost` handler in `index.html`

**Defer:**
- Deploy preview URLs per branch (low priority for initial deploy)
- Automated WebGL browser testing via Playwright/Cypress (significant scope)
- Unity Pro license (only escalate if monthly Personal license renewal becomes burdensome)
- Self-hosted GitLab runner (only if disk space fails on shared runners)

See `.planning/research/FEATURES.md` for full details and feature dependency graph.

### Architecture Approach

The codebase has a clean event-driven pub/sub architecture around two state machines: `AppManager` (app state: Title, Instructions, TransitionToGame, Playing, Paused, GameOver, Leaderboard) and `GameManager` (shot state: Ready, Aiming, Charging, InFlight, Landed). The fix philosophy is "harden, don't restructure" — the architecture is correct, the bugs are implementation-level. Every fix should be the minimum change that prevents runtime failure while preserving existing event contracts and initialization order.

Components fall into three tiers by blast radius: Tier 1 (Bootstrap, AppManager, GameManager) are event hubs with 5-9 subscribers each — touch with extreme care, do not change event signatures or initialization order. Tier 2 (BallController, ScoringManager, LeaderboardManager, ShotInput) have medium blast radius and are where the async void risks and retry queue issue actually live. Tier 3 (all UI controllers, WindSystem) are leaf-node subscribers — safest to modify first.

**Major components:**
1. **Bootstrap** (execution order -100) — registers mock services synchronously before the first `await`, then upgrades to real UGS services; this ordering is the critical safety property and must not change
2. **AppManager** — `DontDestroyOnLoad` singleton; controls scene loading; 5 subscribers on `OnAppStateChanged`
3. **GameManager** — shot lifecycle hub; 6 subscribers on `OnShotStateChanged`, 2 on `OnGameOver`, 1 on `OnResetToTee`
4. **LeaderboardManager** — three `async void` event handlers (all try-catch wrapped, but need `this == null` destroyed-object guards after `await` points) plus an unbounded `Queue<>` retry structure
5. **ServiceLocator** — DI container; all callers null-check the result; safe throughout

**Key constraint:** The event subscription graph has 20+ connections. Do not add events, change event signatures, or reorder initialization. Fix bugs within existing handler methods.

See `.planning/research/ARCHITECTURE.md` for the full event subscription graph, complete async method inventory, and exact code patterns for each fix type.

### Critical Pitfalls

1. **Cloudflare double-compression (Pitfall 1, HIGH)** — Cloudflare Pages cannot honor `Content-Encoding` headers in `_headers` files. Do NOT set `Content-Encoding: gzip`. The existing `decompressionFallback = true` is the complete mitigation. A `_headers` file should only contain `Cache-Control` and security headers.

2. **Unity license activation silent failure (Pitfall 2, HIGH)** — The `|| true` on the activation command in `.gitlab-ci.yml:41` swallows failure exit codes. The license must be generated from the exact Docker image version. Personal licenses expire every ~30 days — the pipeline will silently fail to activate when this happens.

3. **`async void` exceptions freeze WebGL (Pitfall 3, HIGH)** — Unhandled exceptions in `async void` methods terminate the WASM instance in WebGL (game freezes with no error shown to player). All current `async void` methods have try-catch, but `LeaderboardManager` needs `this == null` destroyed-object guards after `await` points to handle mid-await scene transitions.

4. **`System.Threading` silently fails in WebGL (Pitfall 4, HIGH)** — `Task.Delay`, `CancellationTokenSource` timeouts, and `Task.Run` are non-functional stubs in WebGL. Compiles without error; fails silently at runtime. Use `Awaitable.WaitForSecondsAsync()` and coroutines instead.

5. **iOS Safari context loss and memory ceiling (Pitfalls 5 and 7, HIGH)** — iOS 17+ and 18.4 aggressively reclaim GPU resources causing black screens. WebAssembly memory ceiling (~256MB) causes silent page reloads. Fix: add `webglcontextlost` handler to `index.html`, cap the retry queue at 10 entries, consider WebGL 1 over WebGL 2. Only detectable on real iOS hardware.

6. **Contradictory compression documentation (Pitfall 10, HIGH)** — `docs/ci-cd-gotchas.md` and `docs/deployment.md` describe Brotli; `WebGLBuildScript.cs` uses Gzip. A developer following the docs would break the pipeline by "correcting" the compression format.

See `.planning/research/PITFALLS.md` for all 17 pitfalls with detection methods, prevention strategies, and codebase file references.

---

## Implications for Roadmap

Based on combined research, the work divides into four phases ordered by dependency and blast radius.

### Phase 1: Unblock the Pipeline
**Rationale:** The two CI/CD secrets are the single blockers for everything downstream. Nothing can be tested, deployed, or validated until the pipeline runs end-to-end. This is pure configuration — no code changes needed, zero regression risk.
**Delivers:** A working GitLab CI pipeline that builds Unity WebGL and deploys to Cloudflare Pages on manual trigger from `main`.
**Addresses:** `UNITY_LICENSE` configuration, `CLOUDFLARE_API_TOKEN` configuration, wrangler version pin, `_headers` cache policy file creation.
**Avoids:** License silent failure (Pitfall 2), missing Wrangler auth (Pitfall 14), CDN cache misses on repeated loads.
**Research flag:** Standard patterns — all steps are documented in STACK.md with exact values and commands.

### Phase 2: Fix the Documentation
**Rationale:** The documentation actively misleads future contributors. Fixing it immediately after Phase 1 ensures all subsequent work is based on accurate information. Zero code risk — markdown files only. Must happen before any other developer touches the pipeline.
**Delivers:** Internally consistent documentation that accurately describes the Gzip + `decompressionFallback` approach and the correct Unity version (`6000.3.10f1`).
**Files:** `docs/ci-cd-gotchas.md` (Brotli references + three-secret requirement), `docs/deployment.md` (Brotli reference), `.planning/PROJECT.md` (Unity version), `.planning/codebase/STACK.md` (Unity version).
**Research flag:** No research needed — all corrections are factual, verified against `WebGLBuildScript.cs` and `ProjectSettings/ProjectVersion.txt`.

### Phase 3: Runtime Stability Hardening
**Rationale:** The game is feature-complete but carries latent runtime failures that only manifest in WebGL (not editor), on mobile (not desktop), or during network failures (not typical manual testing). These are invisible during development but will cause real player-facing freezes and data loss.
**Delivers:** A game that survives network failures without freezing, handles iOS Safari context loss gracefully, caps memory usage, and surfaces component initialization failures as visible errors rather than silent functional breakage.
**Addresses:** Async void destroyed-object guards (Pitfall 3), unbounded retry queue and iOS memory ceiling (Pitfalls 7), iOS Safari black screen handler (Pitfall 5), `FindFirstObjectByType` silent null failures (Pitfall 13).
**Fix order:** Tier 3 UI components first, then Tier 2 (`LeaderboardManager`, `ScoringManager`, `CameraController`, `GameOverController`), avoid Tier 1 (GameManager, AppManager, Bootstrap) unless required.
**Research flag:** No deeper research needed. ARCHITECTURE.md provides exact code patterns for each fix type (Pattern 1: null-guard-then-subscribe, Pattern 2: async void destroyed-object guard, Pattern 4: bounded resource queue).

### Phase 4: Mobile Browser Polish
**Rationale:** After runtime stability is established and a live deployment exists (Phase 1), address the moderate-confidence mobile browser UX issues. These degrade the experience but do not cause data loss or freezes — they are lower priority than Phase 3's stability work.
**Delivers:** Reliable touch input on iOS/Android, no viewport overscroll disruption, proper address-bar resize handling.
**Addresses:** UI Toolkit erratic tap registration (Pitfall 8), mobile viewport bounce and overscroll (Pitfall 9).
**Requires:** Testing on real iOS and Android hardware. Desktop Safari and Chrome DevTools mobile emulation do not reproduce these issues.
**Research flag:** The `touch-action: none` fix in `index.html:13` already exists. CSS `overscroll-behavior: none` is well-documented. If UI Toolkit tap issues persist after large touch targets, may need deeper investigation — community reports suggest no clean upstream fix exists in Unity 6 + WebGL. Could require switching critical menu input to Input System pointer events.

### Phase Ordering Rationale

- Phases 1-2 are pure configuration and markdown with zero code risk. They unblock all validation and eliminate misleading documentation before any C# is touched.
- Phase 3 follows the ARCHITECTURE.md explicit recommendation: low-blast-radius to high-blast-radius (Tier 3 → Tier 2 components), containing regression risk through ordering.
- Phase 4 requires a live deployment to test (Phase 1 prerequisite) and is lower urgency than stability fixes.
- Do not run Phase 3 and Phase 4 in parallel — changes to `LeaderboardManager` and `GameOverController` could interact with Phase 4 input handling changes in the same Gameplay scene.

### Research Flags

Phases with standard, well-documented patterns (no `/gsd:research-phase` needed):
- **Phase 1:** Exact commands and variable names documented in STACK.md with verified sources.
- **Phase 2:** All corrections are factual, no design decisions required.
- **Phase 3:** Fix patterns fully specified in ARCHITECTURE.md. Implementation is mechanical.

Phases that may need targeted investigation during planning:
- **Phase 4 (UI Toolkit touch input):** If large touch targets and `touch-action: none` don't resolve erratic tap registration, may require an architectural input approach change. Community reports rate this as a known Unity 6 + WebGL bug without a confirmed upstream fix.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Docker tag verified on Docker Hub API. Wrangler version verified on npm registry. Unity version verified against `ProjectSettings/ProjectVersion.txt`. |
| Features | HIGH | Based on direct analysis of `.gitlab-ci.yml` and `WebGLBuildScript.cs` — no inference required. |
| Architecture | HIGH | All findings verified against direct source code analysis of 25+ C# files. Event graph is exhaustive. Async void inventory is complete. |
| Pitfalls | HIGH | 13 of 17 pitfalls rated HIGH confidence, backed by official docs or direct code verification. 4 rated MEDIUM (community consensus, not experimentally verified against this codebase). |

**Overall confidence: HIGH**

### Gaps to Address

- **License renewal cadence:** Personal Unity licenses expire every ~30 days with no in-pipeline alert — the build will just start failing. Consider monitoring or evaluate Unity Pro if CI runs more than twice a month.
- **iOS Safari touch input root cause:** Pitfall 8 has no confirmed fix. If the `touch-action: none` + large touch target approach does not resolve it, may need to replace UI Toolkit button handling with Input System events for all menu interaction. Validate on real hardware in Phase 4 before committing to an approach.
- **Build Profile override verification:** `CLAUDE.md` warns that Unity 6 Build Profiles override `PlayerSettings` API calls. The compression settings in `WebGLBuildScript.cs` may be silently overridden if a Build Profile is active. Verify `Assets/Settings/` for Build Profile `.asset` files before Phase 1 pipeline run.
- **WebGL 1 vs WebGL 2 for iOS:** Pitfall 5 recommends WebGL 1 for iOS Safari compatibility. This is a Player Settings change with potential rendering implications. Validate on real iOS hardware before committing — do not make this change speculatively.
- **External pipeline include:** `.gitlab-ci.yml` includes `project: roborev/claude-plugin, file: pipeline/agent-pipeline.yml`. This external inclusion may add stages or variables. Its interaction with the build and deploy stages should be reviewed.

---

## Sources

### Primary (HIGH confidence)
- [Unity Manual: Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html) — compression formats, decompressionFallback behavior
- [Unity Server Configuration Code Samples](https://docs.unity.cn/Manual/webgl-server-configuration-code-samples.html) — header mappings per compression format and fallback setting
- [Cloudflare Pages Headers docs](https://developers.cloudflare.com/pages/configuration/headers/) — `_headers` syntax, 100-rule limit, splat patterns
- [Cloudflare Content Compression docs](https://developers.cloudflare.com/speed/optimization/content/compression/) — auto-compression behavior, compressible MIME types
- [Docker Hub: unityci/editor tags](https://hub.docker.com/r/unityci/editor/tags) — confirmed `6000.3.10f1-webgl-3` tag exists
- [wrangler on npm](https://www.npmjs.com/package/wrangler) — current version 4.76.0
- [Unity Manual: WebGL Technical Limitations](https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-technical-overview.html) — threading restrictions
- [GameCI: GitLab Activation](https://game.ci/docs/gitlab/activation/) — license activation variables and v4 breaking changes
- [Unity Issue Tracker: WebGL2 black screen in Safari](https://issuetracker.unity3d.com/issues/safari-webgl2-build-shows-black-screen-in-safari) — confirmed bug
- Direct codebase analysis of all 25+ C# source files — architecture, async inventory, event subscription graph

### Secondary (MEDIUM confidence)
- [Cloudflare Community: Pre-Compressed Assets in Pages](https://community.cloudflare.com/t/pre-compressed-assets-in-pages/300028) — confirms Content-Encoding unreliable on Pages
- [Cloudflare Community: End-to-end compression for Pages](https://community.cloudflare.com/t/end-to-end-compression-for-pages/744695) — confirms headers file insufficient
- [Unity WebGL Compression Done Right](https://miltoncandelero.github.io/unity-webgl-compression) — decompressionFallback mechanics explained
- [Unity Discussions: WebGL context lost iOS 17 Safari](https://discussions.unity.com/t/webgl-context-lost-ios-17-safari/930432) — context loss handling
- [Unity Discussions: UI Toolkit buttons erratic on mobile WebGL](https://discussions.unity.com/t/problem-with-ui-toolkit-buttons-with-webgl-on-mobile-browsers/1706183) — touch input issues

### Tertiary (LOW confidence / needs real-hardware validation)
- iOS Safari black screen on iOS 18.4+ — multiple community reports, no official Apple or Unity confirmed fix
- WebGL 1 recommendation for iOS Safari — community consensus, not verified against this specific codebase

---

*Research completed: 2026-03-22*
*Ready for roadmap: yes*
