# Golf Game

## What This Is

A browser-playable WebGL golf game built in Unity 6 targeting mobile browsers. Players take up to 6 shots in a closest-to-the-pin format (125 yards, lowest distance wins) with a global leaderboard backed by Unity Gaming Services. Built by Claude, now being made actually playable and deployed.

## Core Value

A human can open the URL, take shots, and see their score on the leaderboard — without errors.

## Requirements

### Validated

- ✓ Ball physics with power/accuracy shot mechanic — existing
- ✓ Closest-to-pin scoring (6 shots, 125 yards, lowest distance wins) — existing
- ✓ UGS leaderboard (global, anonymous sign-in, Cloud Code server-side validation) — existing
- ✓ Player nickname for leaderboard display — existing
- ✓ Copy-to-clipboard score sharing on game over — existing
- ✓ Mock services fallback (editor + offline) — existing
- ✓ UI Toolkit screen-space UI — existing

### Validated

- ✓ GitHub Actions pipeline configured (lint + cloud-code-tests + WebGL build + deploy) — Validated in Phase 1: CI/CD Pipeline
- ✓ Unity license secrets (UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD) and Cloudflare secrets documented in build.yml and docs — Validated in Phase 1: CI/CD Pipeline
- ✓ WebGL build deploys to Cloudflare Pages via wrangler@^4, `_headers` with Cache-Control — Validated in Phase 1: CI/CD Pipeline (end-to-end run pending human test)
- ✓ docs/ci-cd-gotchas.md and docs/deployment.md accurate (GitHub Actions, Gzip, correct secrets) — Validated in Phase 1: CI/CD Pipeline

### Active

- [ ] Game plays without runtime crashes in browser (async void exceptions handled)
- [ ] Critical null reference bugs fixed (GameManager, BallController, camera)
- [ ] UI is clean and functional at mobile resolution

### Out of Scope

- Visual redesign / new art direction — fix not redesign
- Multiplayer real-time (game is async leaderboard only)
- Player accounts / social features — UGS anonymous is sufficient for now
- New gameplay modes — CTP format is fixed

## Context

- Claude built the entire codebase autonomously; conventions are consistent but runtime correctness is unverified
- Phase 1 complete: GitHub Actions pipeline in `.github/workflows/build.yml` replaces GitLab CI; `_headers` file created; docs updated. End-to-end pipeline run requires 5 GitHub secrets to be configured manually.
- Cloudflare Pages deployment target: `golf-game-amm.pages.dev`; Unity Gzip compression + `decompressionFallback=true` + `_headers` Cache-Control-only file
- Key known bugs (Phase 2 scope): async void event handlers with no exception safety, null refs in core component lookups (GameManager, BallController, CameraController), leaderboard retry queue unbounded, physics framerate hardcoded at 50Hz
- Cloud Code JS tests exist and pass (`node --test validate-and-post-score.test.js`)
- No C# unit tests — only integration tests via Play mode

## Constraints

- **Platform:** WebGL only, targeting mobile browser (no desktop-specific features)
- **Engine:** Unity 6.3 LTS (`6000.0.23f1`) — no engine upgrades
- **CI:** GitLab CI with `unityci/editor:6000.3.10f1-webgl-3` Docker image
- **Hosting:** Cloudflare Pages — fix the compression issue, don't switch providers
- **Services:** UGS Authentication + Leaderboards + Cloud Code — already configured in UGS dashboard
- **Visual scope:** Fix layout/polish only, no art overhaul or redesign

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Keep Gzip compression in Unity build | `decompressionFallback = true` already set; simpler than Brotli | ✓ Phase 1: Implemented in build.yml + _headers |
| Fix Cloudflare via `_headers` file | Avoids switching hosts; standard approach for Unity WebGL on Cloudflare | ✓ Phase 1: _headers committed, copied to build by CI |
| Switch from GitLab CI to GitHub Actions + GameCI | GameCI handles Unity license; GitHub Actions is standard for public repos | ✓ Phase 1: build.yml with game-ci/unity-builder@v4 |
| Fix async void with try-catch (not rewrite to async Task) | Minimal diff; Unity event handlers must be void | — Phase 2 |
| No C# unit tests in this milestone | Getting it running is priority; tests are a future milestone | — Ongoing |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition:**
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone:**
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-03-23 after Phase 1: CI/CD Pipeline completion*
