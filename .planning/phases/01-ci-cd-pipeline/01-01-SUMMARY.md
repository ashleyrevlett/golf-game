---
phase: 01-ci-cd-pipeline
plan: 01
subsystem: infra
tags: [github-actions, gameci, unity-webgl, cloudflare-pages, wrangler, ci-cd]

# Dependency graph
requires: []
provides:
  - Consolidated GitHub Actions workflow (.github/workflows/build.yml) with lint, cloud-code-tests, and build-and-deploy jobs
  - _headers file at repo root with correct Cache-Control rules for Cloudflare Pages
  - Removal of broken ci.yml, deploy.yml, and .gitlab-ci.yml
affects: [02-runtime-stability, 03-mobile-browser]

# Tech tracking
tech-stack:
  added: [game-ci/unity-builder@v4, wrangler@^4 via npx, jlumbroso/free-disk-space@v1.3.1]
  patterns: [consolidated-ci-workflow, repo-root-headers-file, lfs-cache-pattern]

key-files:
  created:
    - .github/workflows/build.yml
    - _headers
  modified: []

key-decisions:
  - "Consolidated two broken workflows (ci.yml + deploy.yml) into single build.yml — avoids split maintenance and fixes all issues in one file"
  - "Deleted .gitlab-ci.yml — project is on GitHub Actions; keeping it causes confusion and it has known bugs (|| true masking on license)"
  - "Used npx wrangler@^4 pages deploy instead of cloudflare/pages-action@v1 — pages-action pins wrangler v3, violating D-06"
  - "_headers committed to repo root and copied by CI — version-controlled per D-11, not generated inline in workflow"
  - "build-and-deploy has no needs: dependency on lint/cloud-code-tests — runs in parallel; faster pipeline"

patterns-established:
  - "Pattern: Free disk space before Unity image pull — jlumbroso/free-disk-space@v1.3.1 is first step in any GameCI job"
  - "Pattern: LFS cache with rm -f .lfs-assets-id after pull — prevents dirty build error in Unity"
  - "Pattern: Cloudflare _headers at repo root, copied into build output by CI — not generated inline"

requirements-completed: [CI-01, CI-02, CI-03, CI-04, CI-05, CI-06]

# Metrics
duration: 2min
completed: 2026-03-23
---

# Phase 01 Plan 01: CI/CD Pipeline — Consolidated GitHub Actions Workflow Summary

**Replaced two broken GitHub Actions workflows and an obsolete GitLab CI pipeline with a single correct build.yml: GameCI unity-builder@v4 with unityVersion 6000.3.10f1, wrangler@^4 deploy to Cloudflare Pages, and a version-controlled _headers file with Cache-Control only.**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-03-23T02:56:19Z
- **Completed:** 2026-03-23T02:57:37Z
- **Tasks:** 2
- **Files modified:** 5 (2 created, 3 deleted)

## Accomplishments
- Created `.github/workflows/build.yml` with three correct jobs: lint (dotnet format), cloud-code-tests (node --test), build-and-deploy (GameCI + wrangler)
- Created `_headers` at repo root with tab-indented Cache-Control rules: no-cache for index.html, immutable for /Build/* (wildcard for hashed filenames)
- Deleted three obsolete/broken pipeline files: ci.yml (wrong Unity version, gated on non-existent C# tests), deploy.yml (wrangler v3, tag trigger, wrong _headers with Brotli Content-Encoding, wrong URL), .gitlab-ci.yml (migrated to GitHub Actions)

## Task Commits

Each task was committed atomically:

1. **Task 1: Create _headers file at repo root** - `ac65cee` (chore)
2. **Task 2: Create build.yml and remove obsolete pipeline files** - `3582904` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `.github/workflows/build.yml` - Consolidated CI/CD workflow: lint + cloud-code-tests on push/PR, build-and-deploy on main only
- `_headers` - Cloudflare Pages headers file: Cache-Control: no-cache for /index.html, Cache-Control: public, max-age=31536000, immutable for /Build/*
- `.github/workflows/ci.yml` - DELETED (broken: wrong Unity version 6000.0.23f1, build gated on non-existent C# tests, no deploy)
- `.github/workflows/deploy.yml` - DELETED (broken: tag-only trigger, wrangler v3 via cloudflare/pages-action, Brotli Content-Encoding in _headers, wrong project URL)
- `.gitlab-ci.yml` - DELETED (obsolete: project migrated to GitHub Actions; pipeline had known bugs including || true masking on license activation)

## Decisions Made

- Used `unityVersion: 6000.3.10f1` (not 6000.0.23f1 from old workflows) — confirmed from .gitlab-ci.yml Docker image tag `unityci/editor:6000.3.10f1-webgl-3`
- `buildMethod: WebGLBuildScript.Build` specified — without it, GameCI uses its own build method and Gzip/decompressionFallback settings from WebGLBuildScript.cs are not applied
- build-and-deploy job has NO `needs:` on lint/cloud-code-tests — parallel execution; lint failures do not block deploy (acceptable for this project)
- Deleted .gitlab-ci.yml entirely rather than leaving in place — D-01 gave discretion; keeping it causes confusion

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

**External services require manual configuration before the pipeline can succeed end-to-end.**

The following 5 GitHub repository secrets must be set in Settings > Secrets and variables > Actions:

| Secret | Source |
|--------|--------|
| `UNITY_LICENSE` | Full contents of `~/Library/Application Support/Unity/Unity_lic.ulf` (Mac) |
| `UNITY_EMAIL` | Unity account email address |
| `UNITY_PASSWORD` | Unity account password |
| `CLOUDFLARE_API_TOKEN` | Cloudflare dashboard > My Profile > API Tokens > Create Token with "Cloudflare Pages: Edit" permission |
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare dashboard right sidebar "Account ID" |

Note: Unity Personal license (`UNITY_LICENSE`) expires every ~30 days and must be renewed by re-generating `.ulf` locally and updating the secret.

## Next Phase Readiness

- Pipeline structure is complete and correct. Pushing to main will trigger build-and-deploy once secrets are configured.
- Lint and cloud-code-tests jobs will run on every push and PR immediately (no secrets required for these jobs).
- Phase 02 (runtime stability) can begin — it does not depend on the pipeline succeeding, only on the code being correct.
- Blocker for end-to-end verification: 5 GitHub secrets must be manually configured by a human with access to the GitHub repo, Unity account, and Cloudflare account.

## Self-Check: PASSED

- FOUND: `.github/workflows/build.yml`
- FOUND: `_headers`
- FOUND: `.planning/phases/01-ci-cd-pipeline/01-01-SUMMARY.md`
- FOUND commit: `ac65cee` (Task 1)
- FOUND commit: `3582904` (Task 2)
- ci.yml absent (deleted)
- deploy.yml absent (deleted)
- .gitlab-ci.yml absent (deleted)

---
*Phase: 01-ci-cd-pipeline*
*Completed: 2026-03-23*
