# Phase 1: CI/CD Pipeline - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Migrate from GitLab CI to GitHub Actions with GameCI, fix all pipeline issues, deploy to Cloudflare Pages on push to main, and update documentation to be accurate.

This phase delivers:
- A working `.github/workflows/build.yml` that builds Unity WebGL and deploys to `golf-game-amm.pages.dev`
- Correct GitHub Actions secrets configuration documented
- A `_headers` file in the build output with correct Cache-Control headers
- `docs/ci-cd-gotchas.md` and `docs/deployment.md` updated to reflect Gzip, GameCI, and GitHub Actions

**Out of scope:** Runtime stability fixes (Phase 2), mobile browser testing (Phase 3).

</domain>

<decisions>
## Implementation Decisions

### CI Platform
- **D-01:** Switch from GitLab CI (`.gitlab-ci.yml`) to GitHub Actions (`.github/workflows/build.yml`). The existing `.gitlab-ci.yml` can be left in place or removed — planner's discretion.

### Build Approach
- **D-02:** Use GameCI (`game-ci/unity-builder`) for Unity WebGL builds. GameCI handles Unity license activation via online activation (not manual .ulf file).
- **D-03:** Unity secrets required: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` — all three needed by GameCI. These must be documented as required GitHub repository secrets.
- **D-04:** WebGL build configuration already correct in `Assets/Editor/WebGLBuildScript.cs` — Gzip compression + `decompressionFallback = true` + `nameFilesAsHashes = true`. Do not change these settings.

### Deployment
- **D-05:** Deploy automatically on push to `main` branch — no manual approval step.
- **D-06:** Deploy to Cloudflare Pages via `wrangler pages deploy`. Wrangler must be pinned to `^4` (not unpinned `npm install -g wrangler`).
- **D-07:** Deploy requires `CLOUDFLARE_API_TOKEN` secret in GitHub repository secrets.
- **D-08:** Cloudflare Pages project name: `golf-game` (URL `golf-game-amm.pages.dev`).

### `_headers` File
- **D-09:** A `_headers` file must be created in the build output directory (or copied there by CI) with Cache-Control headers only. No `Content-Encoding` header (decompressionFallback handles decompression at runtime — Cloudflare must not set Content-Encoding).
- **D-10:** Header values:
  ```
  /index.html
    Cache-Control: no-cache

  /Build/*
    Cache-Control: public, max-age=31536000, immutable
  ```
- **D-11:** The `_headers` file should be committed to the repo and copied into the build output by the CI pipeline (not generated at build time), so it's version-controlled.

### Documentation
- **D-12:** `docs/ci-cd-gotchas.md` must be updated: replace all Brotli references with Gzip, update secrets list to UNITY_LICENSE + UNITY_EMAIL + UNITY_PASSWORD, update platform references from GitLab to GitHub Actions.
- **D-13:** `docs/deployment.md` must be updated: replace Brotli with Gzip, replace GitLab CI references with GitHub Actions, document the automatic deploy on push to main, remove the "TODO: GitLab runner" section.

### Claude's Discretion
- Whether to delete `.gitlab-ci.yml` or leave it in place
- GameCI action version to pin to (latest stable)
- Whether to add a PR preview deploy job (separate from main deploy)
- Exact workflow job structure (job names, runner OS)
- Unity build timeout values

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing CI/CD Files
- `.gitlab-ci.yml` — Current pipeline (being replaced — read to understand what jobs exist)
- `Assets/Editor/WebGLBuildScript.cs` — Unity build entry point, build path env vars, compression config
- `docs/ci-cd-gotchas.md` — Current docs (being updated — read to understand what needs fixing)
- `docs/deployment.md` — Current deployment docs (being updated)

### Project Docs
- `CLAUDE.md` — Project conventions, CI/CD section, Unity MCP workflow
- `.planning/REQUIREMENTS.md` — CI-01 through CI-06, DOC-01, DOC-02 (exact acceptance criteria)
- `.planning/research/PITFALLS.md` — 17 known CI/CD pitfalls including Cloudflare Content-Encoding limitation

### External References
- GameCI GitHub Actions docs: https://game.ci/docs/github/getting-started (for action syntax and secret names)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/Editor/WebGLBuildScript.cs` — Already correct. Reads `BUILD_PATH` or `CUSTOM_BUILD_PATH` env var for output path. Calls `EditorApplication.Exit(0)` on success, `Exit(1)` on failure — CI will correctly detect failures.
- `Assets/CloudCode/validate-and-post-score.test.js` — Cloud Code tests, already runnable via `node --test`. The CI cloud-code-test stage just needs `node --test Assets/CloudCode/validate-and-post-score.test.js`.
- `_headers` file does NOT exist yet — must be created.

### Established Patterns
- The existing lint job checks for tab characters in `.cs` files: `find . -name "*.cs" -exec grep -P "\t" {} + && exit 1 || true` — this logic is correct (note: the `|| true` here is intentional — grep exits 1 when no match, which is the success case)
- Build output path: `build/WebGL/golf-game/` — used consistently in `.gitlab-ci.yml` and `WebGLBuildScript.cs`

### Integration Points
- GitHub Actions workflow will need to upload build artifacts between jobs (build → deploy)
- The `_headers` file needs to land inside `build/WebGL/golf-game/` before wrangler deploy runs

</code_context>

<specifics>
## Specific Ideas

- User explicitly wants GameCI (`game-ci/unity-builder`) — not custom Docker-based headless Unity
- Wrangler must be pinned to `^4` specifically
- No Content-Encoding in `_headers` — this was a key finding from research (Cloudflare Pages cannot set Content-Encoding via `_headers` anyway)

</specifics>

<deferred>
## Deferred Ideas

- PR preview deployments to Cloudflare Pages (separate preview URL per PR) — possible but not in scope for this phase
- Self-hosted GitHub runner for faster Unity builds — would require infrastructure setup
- Unity serial-based license activation (doesn't expire every 30 days) — v2 requirement PERF-02

</deferred>

---

*Phase: 01-ci-cd-pipeline*
*Context gathered: 2026-03-22*
