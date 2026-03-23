# Requirements: Golf Game

**Defined:** 2026-03-22
**Core Value:** A human can open the URL, take shots, and see their score on the leaderboard — without errors.

## v1 Requirements

### CI/CD Pipeline

- [x] **CI-01**: GitLab CI lint and cloud-code-test stages pass on merge requests
- [x] **CI-02**: Unity license activation succeeds in CI (UNITY_LICENSE secret documented and configured)
- [x] **CI-03**: WebGL build stage completes without errors (no silent `|| true` masking failures)
- [x] **CI-04**: Deploy stage deploys to Cloudflare Pages (`golf-game-amm.pages.dev`)
- [x] **CI-05**: Wrangler is pinned to `^4` to prevent silent breakage from major version changes
- [x] **CI-06**: `_headers` file in build output sets cache-control and security headers (no Content-Encoding — decompressionFallback handles decompression)

### Runtime Stability

- [ ] **STAB-01**: Leaderboard retry queue is capped at 10 entries (prevents iOS Safari memory crash)
- [ ] **STAB-02**: Fire-and-forget async calls in LeaderboardManager are wrapped with outer exception handling
- [ ] **STAB-03**: `Camera.main` null dereference in `ScoringManager.HandleBallLanded()` is guarded
- [ ] **STAB-04**: Camera tag lookup failures in `CameraController` log an error instead of silently failing
- [ ] **STAB-05**: Physics fixed timestep is 60Hz (not 50Hz) to match WebGL's assumed frame rate
- [ ] **STAB-06**: Post-`await` MonoBehaviour lifecycle guards added in `LeaderboardManager` and `GameOverController`

### WebGL + Mobile

- [ ] **WEB-01**: Game loads and runs in Chrome, Firefox, and Safari on desktop
- [ ] **WEB-02**: Game loads and runs on mobile Safari (iOS) without black screen or context loss
- [ ] **WEB-03**: Touch input works correctly for shot power and accuracy on mobile browsers
- [ ] **WEB-04**: UI renders correctly at 390×844 (iPhone 14) resolution

### Documentation

- [ ] **DOC-01**: `docs/ci-cd-gotchas.md` corrected (Brotli references replaced with Gzip, decompressionFallback approach documented)
- [ ] **DOC-02**: `docs/deployment.md` corrected (reflects actual compression approach and secret requirements)

## v2 Requirements

### Testing

- **TEST-01**: C# unit tests for ScoringManager distance calculations
- **TEST-02**: C# unit tests for GameManager state transitions
- **TEST-03**: Play mode integration test for shot → land → score flow

### Observability

- **OBS-01**: Error tracking service (Sentry or equivalent) captures unhandled exceptions in WebGL
- **OBS-02**: Leaderboard error distinguishes network vs auth vs rate-limit failures in logs

### Performance

- **PERF-01**: WebGL initial load time under 10 seconds on mobile LTE
- **PERF-02**: Unity license uses serial-based activation (doesn't expire every 30 days)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Visual redesign / new art | Fix not redesign — scope is functional polish only |
| Content-Encoding headers in Cloudflare | decompressionFallback=true already handles this; cannot be set via _headers anyway |
| Switching hosting provider | Cloudflare Pages is the target — fix the setup, don't switch |
| Real-time multiplayer | Async leaderboard is the design — not a bug |
| New gameplay modes | CTP format is locked in this milestone |
| C# unit tests | v2 — getting it running is the priority |
| Full exception support mode in Unity | Confirmed performance regression, fix null refs in code instead |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CI-01 | Phase 1 | Complete |
| CI-02 | Phase 1 | Complete |
| CI-03 | Phase 1 | Complete |
| CI-04 | Phase 1 | Complete |
| CI-05 | Phase 1 | Complete |
| CI-06 | Phase 1 | Complete |
| STAB-01 | Phase 2 | Pending |
| STAB-02 | Phase 2 | Pending |
| STAB-03 | Phase 2 | Pending |
| STAB-04 | Phase 2 | Pending |
| STAB-05 | Phase 2 | Pending |
| STAB-06 | Phase 2 | Pending |
| WEB-01 | Phase 3 | Pending |
| WEB-02 | Phase 3 | Pending |
| WEB-03 | Phase 3 | Pending |
| WEB-04 | Phase 3 | Pending |
| DOC-01 | Phase 1 | Pending |
| DOC-02 | Phase 1 | Pending |

**Coverage:**
- v1 requirements: 18 total
- Mapped to phases: 18
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-22*
*Last updated: 2026-03-22 after initial definition*
