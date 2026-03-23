# Roadmap: Golf Game

## Overview

This milestone takes a Claude-built Unity WebGL golf game from "code exists" to "playable in a browser." The pipeline must build and deploy without errors, the runtime must not crash, and the game must work on mobile browsers. Three phases in dependency order: pipeline first (nothing works without builds), stability second (fix crashes before verifying in browser), WebGL/mobile verification last (the final gate to "it works").

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: CI/CD Pipeline** - Get the GitHub Actions pipeline green and deploying to Cloudflare Pages with correct docs (completed 2026-03-23)
- [ ] **Phase 2: Runtime Stability** - Fix null refs, async exceptions, and physics issues so the game runs without crashes
- [ ] **Phase 3: WebGL + Mobile Verification** - Verify the game loads and plays correctly across browsers and mobile devices

## Phase Details

### Phase 1: CI/CD Pipeline
**Goal**: A push to main produces a working WebGL build deployed to Cloudflare Pages, with accurate documentation
**Depends on**: Nothing (first phase)
**Requirements**: CI-01, CI-02, CI-03, CI-04, CI-05, CI-06, DOC-01, DOC-02
**Success Criteria** (what must be TRUE):
  1. A push to main triggers lint and cloud-code-test jobs that pass without manual intervention
  2. The WebGL build-and-deploy job completes successfully using the configured Unity license
  3. The deploy step publishes the build to golf-game-amm.pages.dev with correct Cache-Control headers
  4. `docs/ci-cd-gotchas.md` and `docs/deployment.md` accurately describe the Gzip/decompressionFallback approach and CI secret requirements
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md — Replace ci.yml + deploy.yml + .gitlab-ci.yml with consolidated build.yml; create _headers file
- [x] 01-02-PLAN.md — Rewrite docs/ci-cd-gotchas.md and docs/deployment.md to reflect GitHub Actions + Gzip

### Phase 2: Runtime Stability
**Goal**: The game runs from start to game-over without null reference exceptions, unhandled async errors, or silent failures
**Depends on**: Phase 1
**Requirements**: STAB-01, STAB-02, STAB-03, STAB-04, STAB-05, STAB-06
**Success Criteria** (what must be TRUE):
  1. The game completes a full 6-shot round without any console errors or unhandled exceptions
  2. LeaderboardManager retries are capped and async calls cannot crash the game with unhandled exceptions
  3. Camera lookups and ScoringManager ball-landed handling work without null reference errors
  4. Physics runs at 60Hz fixed timestep and MonoBehaviour lifecycle is respected after await calls
**Plans**: TBD

Plans:
- [ ] 02-01: TBD
- [ ] 02-02: TBD

### Phase 3: WebGL + Mobile Verification
**Goal**: A human can open the deployed URL on a phone, play a round, and see their score on the leaderboard
**Depends on**: Phase 2
**Requirements**: WEB-01, WEB-02, WEB-03, WEB-04
**Success Criteria** (what must be TRUE):
  1. The game loads and runs without errors in Chrome, Firefox, and Safari on desktop
  2. The game loads and runs on mobile Safari (iOS) without black screen, context loss, or memory crashes
  3. Touch input correctly controls shot power and accuracy on mobile browsers
  4. UI renders correctly and is usable at 390x844 (iPhone 14) resolution
**Plans**: TBD
**UI hint**: yes

Plans:
- [ ] 03-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. CI/CD Pipeline | 2/2 | Complete   | 2026-03-23 |
| 2. Runtime Stability | 0/0 | Not started | - |
| 3. WebGL + Mobile Verification | 0/0 | Not started | - |
