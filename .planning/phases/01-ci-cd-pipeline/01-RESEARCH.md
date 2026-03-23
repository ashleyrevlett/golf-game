# Phase 01: CI/CD Pipeline - Research

**Researched:** 2026-03-22
**Domain:** GitHub Actions, GameCI, Cloudflare Pages, Unity 6 WebGL builds
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Switch from GitLab CI (`.gitlab-ci.yml`) to GitHub Actions (`.github/workflows/build.yml`). The existing `.gitlab-ci.yml` can be left in place or removed — planner's discretion.
- **D-02:** Use GameCI (`game-ci/unity-builder`) for Unity WebGL builds. GameCI handles Unity license activation via online activation (not manual .ulf file).
- **D-03:** Unity secrets required: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` — all three needed by GameCI. These must be documented as required GitHub repository secrets.
- **D-04:** WebGL build configuration already correct in `Assets/Editor/WebGLBuildScript.cs` — Gzip compression + `decompressionFallback = true` + `nameFilesAsHashes = true`. Do not change these settings.
- **D-05:** Deploy automatically on push to `main` branch — no manual approval step.
- **D-06:** Deploy to Cloudflare Pages via `wrangler pages deploy`. Wrangler must be pinned to `^4` (not unpinned `npm install -g wrangler`).
- **D-07:** Deploy requires `CLOUDFLARE_API_TOKEN` secret in GitHub repository secrets.
- **D-08:** Cloudflare Pages project name: `golf-game` (URL `golf-game-amm.pages.dev`).
- **D-09:** A `_headers` file must be created in the build output directory (or copied there by CI) with Cache-Control headers only. No `Content-Encoding` header.
- **D-10:** Header values:
  ```
  /index.html
    Cache-Control: no-cache

  /Build/*
    Cache-Control: public, max-age=31536000, immutable
  ```
- **D-11:** The `_headers` file should be committed to the repo and copied into the build output by the CI pipeline (not generated at build time), so it's version-controlled.
- **D-12:** `docs/ci-cd-gotchas.md` must be updated: replace all Brotli references with Gzip, update secrets list to UNITY_LICENSE + UNITY_EMAIL + UNITY_PASSWORD, update platform references from GitLab to GitHub Actions.
- **D-13:** `docs/deployment.md` must be updated: replace Brotli with Gzip, replace GitLab CI references with GitHub Actions, document the automatic deploy on push to main, remove the "TODO: GitLab runner" section.

### Claude's Discretion

- Whether to delete `.gitlab-ci.yml` or leave it in place
- GameCI action version to pin to (latest stable)
- Whether to add a PR preview deploy job (separate from main deploy)
- Exact workflow job structure (job names, runner OS)
- Unity build timeout values

### Deferred Ideas (OUT OF SCOPE)

- PR preview deployments to Cloudflare Pages (separate preview URL per PR)
- Self-hosted GitHub runner for faster Unity builds
- Unity serial-based license activation (doesn't expire every 30 days) — v2 requirement PERF-02
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CI-01 | Lint and cloud-code-test stages pass on merge requests | Verified: existing `ci.yml` has lint (dotnet format) and cloud-code-tests jobs — both are sound. Lint job needs fixing (see Gap Analysis). |
| CI-02 | Unity license activation succeeds in CI (UNITY_LICENSE secret documented and configured) | GameCI personal license: UNITY_LICENSE = contents of `.ulf` file, UNITY_EMAIL, UNITY_PASSWORD all required. Pattern confirmed in GameCI docs. |
| CI-03 | WebGL build stage completes without errors (no silent `|| true` masking failures) | GameCI `unity-builder@v4` handles license activation internally and exits non-zero on failure — no `|| true` masking needed. Must call `buildMethod: WebGLBuildScript.Build`. |
| CI-04 | Deploy stage deploys to Cloudflare Pages (`golf-game-amm.pages.dev`) | Use `wrangler pages deploy` via npx; project name `golf-game`; triggered on push to `main`. Existing `deploy.yml` uses wrong trigger (tags) and wrong wrangler action version. |
| CI-05 | Wrangler is pinned to `^4` to prevent silent breakage from major version changes | Current wrangler stable is 4.76.0. Use `npx wrangler@^4` or `npm install wrangler@^4` in CI. Do NOT use `cloudflare/pages-action` (uses wrangler v3). |
| CI-06 | `_headers` file in build output sets cache-control headers (no Content-Encoding) | Commit `_headers` to repo root, CI copies it into `build/WebGL/golf-game/`. Cloudflare Pages `_headers` format confirmed. Wildcard `/Build/*` needed because `nameFilesAsHashes=true`. |
| DOC-01 | `docs/ci-cd-gotchas.md` corrected (Brotli references replaced with Gzip, decompressionFallback documented) | Current doc says "Brotli" and "`.br`" files — contradicts actual build script (Gzip). Fix by replacing compression section entirely. |
| DOC-02 | `docs/deployment.md` corrected (reflects actual compression approach and secret requirements) | Current doc says "Brotli-compressed" and references "Future: GitLab CI". Replace with GitHub Actions + Gzip + push-to-main deploy. |
</phase_requirements>

---

## Summary

The GitHub repository already has two workflow files: `.github/workflows/ci.yml` (lint, cloud-code-tests, unity-tests, unity-build) and `.github/workflows/deploy.yml` (build + deploy on tag push). Neither workflow is correct as-is. The CI workflow's build job gates on C# tests that don't exist yet and uses the wrong Unity version (`6000.0.23f1` vs `6000.3.10f1`). The deploy workflow uses `cloudflare/pages-action@v1` with wrangler v3 (not v4 as required), triggers on git tags rather than push to main, generates a `_headers` file with wrong Brotli `Content-Encoding` headers, and references the wrong production URL.

The solution is to replace both workflows with a single consolidated `build.yml` that: (1) runs lint and cloud-code-tests on PRs and push to main, (2) builds Unity WebGL via `game-ci/unity-builder@v4` with `buildMethod: WebGLBuildScript.Build` on push to main, (3) copies the committed `_headers` file into the build output, and (4) deploys to Cloudflare Pages using `npx wrangler@^4 pages deploy` on push to main automatically. Documentation must be updated to replace all Brotli references with Gzip and all GitLab CI references with GitHub Actions.

The existing build script (`WebGLBuildScript.cs`) is already correct — it uses Gzip compression, `decompressionFallback = true`, and `nameFilesAsHashes = true`. These must not change. The `decompressionFallback` approach means Cloudflare Pages does not need `Content-Encoding` headers — the Unity JS loader handles decompression in-browser.

**Primary recommendation:** Consolidate into a single `.github/workflows/build.yml` triggered on push to main and PRs, replacing both existing workflows. Pin `game-ci/unity-builder@v4` and use `npx wrangler@^4` for deploy. Commit a `_headers` file with Cache-Control only.

---

## Existing Workflow Gap Analysis

### Current `.github/workflows/ci.yml` — Issues

| Issue | Detail |
|-------|--------|
| Unity version wrong | Uses `6000.0.23f1` but project is `6000.3.10f1` (see `.gitlab-ci.yml` and `docs/deployment.md`) |
| Build gated on C# tests | `unity-build` job has `needs: [unity-tests, unity-playmode-tests]` but no C# tests exist yet (v2 requirement) — this blocks builds |
| Lint uses dotnet format | Uses `.ci/FormatCheck.csproj` and `dotnet format whitespace` — different from GitLab's `grep -P "\t"` approach. The `.ci/FormatCheck.csproj` exists and is valid. |
| Build job missing buildMethod | Does not pass `buildMethod: WebGLBuildScript.Build` — without this GameCI uses its own build method and the Gzip/decompressionFallback settings won't apply |
| No `_headers` handling | Build job has no step to copy `_headers` into build output |
| No deploy step | CI workflow has no deploy step; deploy is in separate `deploy.yml` |

### Current `.github/workflows/deploy.yml` — Issues

| Issue | Detail |
|-------|--------|
| Trigger is tags only | Triggers on `v*.*.*` tags and `workflow_dispatch` — not push to main (D-05 requires push to main) |
| Uses `cloudflare/pages-action@v1` | This action uses wrangler v3 (via `wranglerVersion: '3'`) — D-06 requires wrangler v4 |
| Wrong `_headers` content | Generates `_headers` with `Content-Encoding: br` for `.wasm.br` files — build uses Gzip, not Brotli; and Content-Encoding must not be set (D-09) |
| Wrong production URL | Summary step references `golf-game.pages.dev` — actual URL is `golf-game-amm.pages.dev` (D-08) |
| Unity version wrong | Same `6000.0.23f1` mismatch |
| Missing `CLOUDFLARE_ACCOUNT_ID` | Passes `accountId` to `pages-action` but if switching to wrangler CLI, need this as env var |
| COEP/COOP headers included | `_headers` includes `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: require-corp` — locked decisions specify Cache-Control only (D-10) |

---

## Standard Stack

### Core

| Tool | Version | Purpose | Why Standard |
|------|---------|---------|--------------|
| `game-ci/unity-builder` | `@v4` (v4.8.1 latest) | Unity WebGL build in GitHub Actions | Official GameCI action; handles license, Docker image selection, LFS |
| `wrangler` | `^4` (4.76.0 current) | Cloudflare Pages deploy CLI | Official Cloudflare tool; pinned to v4 per D-06 |
| `actions/checkout` | `@v4` | Repo checkout with LFS support | Standard GHA action |
| `actions/cache` | `@v4` | Unity Library folder caching | Standard GHA action |
| `actions/upload-artifact` | `@v4` | Pass build output between jobs | Standard GHA action |
| `actions/download-artifact` | `@v4` | Retrieve build artifact in deploy job | Standard GHA action |
| `actions/setup-node` | `@v4` | Node.js for cloud-code-tests and wrangler | Standard GHA action |
| `actions/setup-dotnet` | `@v4` | .NET SDK for dotnet format lint | Standard GHA action |
| `jlumbroso/free-disk-space` | `@v1.3.1` | Free 20+ GB before Unity image pull | Already in deploy.yml; essential for shared runners |

### GameCI License Secrets

| Secret Name | Value | Where to Set |
|-------------|-------|--------------|
| `UNITY_LICENSE` | Full contents of `Unity_lic.ulf` (Personal) | GitHub repo Settings > Secrets |
| `UNITY_EMAIL` | Unity account email | GitHub repo Settings > Secrets |
| `UNITY_PASSWORD` | Unity account password | GitHub repo Settings > Secrets |

Personal license `.ulf` location on Mac: `/Library/Application Support/Unity/Unity_lic.ulf`. Copy full file contents into the secret — no base64 encoding needed for GitHub secrets (they support multi-line values).

### Cloudflare Pages Secrets

| Secret Name | Value | Where to Set |
|-------------|-------|--------------|
| `CLOUDFLARE_API_TOKEN` | API token with "Cloudflare Pages: Edit" permission | GitHub repo Settings > Secrets |
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare account ID from dashboard | GitHub repo Settings > Secrets (or env var) |

### Installation for deploy step

```yaml
- name: Deploy to Cloudflare Pages
  run: npx wrangler@^4 pages deploy build/WebGL/golf-game/ --project-name=golf-game
  env:
    CLOUDFLARE_API_TOKEN: ${{ secrets.CLOUDFLARE_API_TOKEN }}
    CLOUDFLARE_ACCOUNT_ID: ${{ secrets.CLOUDFLARE_ACCOUNT_ID }}
```

No global install needed — `npx wrangler@^4` fetches and pins to the `^4` range.

---

## Architecture Patterns

### Recommended Workflow Structure

One consolidated file `.github/workflows/build.yml` with three jobs:

```
build.yml
├── lint          (on: push/PR — runs always)
├── cloud-code-tests  (on: push/PR — runs always)
└── build-and-deploy  (on: push to main only)
    ├── Free disk space
    ├── Checkout (with lfs: true)
    ├── LFS cache + pull
    ├── Unity Library cache
    ├── game-ci/unity-builder@v4
    ├── Copy _headers into build output
    ├── Upload artifact (optional, for debugging)
    └── npx wrangler@^4 pages deploy
```

### Pattern 1: GameCI Build with Custom buildMethod

```yaml
- uses: game-ci/unity-builder@v4
  env:
    UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
    UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
    UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
  with:
    targetPlatform: WebGL
    unityVersion: 6000.3.10f1
    buildName: golf-game
    buildMethod: WebGLBuildScript.Build
```

`buildMethod` must be specified to invoke `WebGLBuildScript.Build` which sets Gzip compression, `decompressionFallback`, and `nameFilesAsHashes`. Without `buildMethod`, GameCI uses its own default build method and these settings will not apply.

Output path: `build/WebGL/golf-game/` (controlled by `buildName: golf-game` and the default `buildsPath: build`).

### Pattern 2: LFS Caching (from existing deploy.yml — correct approach)

```yaml
- uses: actions/checkout@v4
  with:
    lfs: true

- name: Create LFS file list
  run: git lfs ls-files -l | cut -d' ' -f1 | sort > .lfs-assets-id

- uses: actions/cache@v4
  with:
    path: .git/lfs
    key: lfs-${{ hashFiles('.lfs-assets-id') }}

- name: Pull LFS files
  run: git lfs pull && rm -f .lfs-assets-id
```

The `.lfs-assets-id` file causes "dirty build" errors — always `rm -f` it after pulling.

### Pattern 3: `_headers` File Approach

Commit `_headers` to repo root. CI copies it into the build output directory before deploying:

```yaml
- name: Copy _headers into build output
  run: cp _headers build/WebGL/golf-game/_headers
```

The committed `_headers` content (D-10):

```
/index.html
  Cache-Control: no-cache

/Build/*
  Cache-Control: public, max-age=31536000, immutable
```

Note: `/Build/*` uses wildcard because `nameFilesAsHashes=true` produces hashed filenames like `Build/abc123def.data.gz` — specific filename patterns would not match.

### Pattern 4: Disk Space Cleanup (essential for shared runners)

The `unityci/editor` images for Unity 6 are 15+ GB. GitHub Actions hosted runners have ~14 GB free by default, which is insufficient. Always free disk space before pulling the Unity image:

```yaml
- uses: jlumbroso/free-disk-space@v1.3.1
  with:
    tool-cache: false
    android: true
    dotnet: true
    haskell: true
    large-packages: true
    docker-images: true
    swap-storage: true
```

This frees 20-30 GB, giving the Unity image room to pull.

### Pattern 5: Unity Library Cache

```yaml
- uses: actions/cache@v4
  with:
    path: Library
    key: Library-WebGL-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
    restore-keys: |
      Library-WebGL-
```

Cache miss on first run adds ~10 minutes (full import). Cache hit saves ~8-10 minutes on subsequent runs. The `Library/` folder should be in `.gitignore` (standard Unity setup).

### Pattern 6: Lint Job (existing approach is correct)

The existing `ci.yml` lint job uses `dotnet format whitespace` with `.ci/FormatCheck.csproj`. This is more robust than the GitLab `grep -P "\t"` approach and the `.ci/FormatCheck.csproj` already exists. Keep this approach.

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
- name: Check whitespace formatting
  run: dotnet format whitespace .ci/FormatCheck.csproj --verify-no-changes --verbosity diagnostic
```

### Anti-Patterns to Avoid

- **Using `cloudflare/pages-action`:** This action uses wrangler v3 internally, violating D-06. Use `npx wrangler@^4` instead.
- **Generating `_headers` inline in CI:** Makes header config non-version-controlled. Commit the file and copy it instead (D-11).
- **Setting `Content-Encoding` in `_headers`:** Cloudflare Pages ignores/overrides `Content-Encoding` set via `_headers`. `decompressionFallback=true` already handles this in-browser.
- **`npm install -g wrangler` without version pin:** Fetches latest major version; a future v5 release would silently break the pipeline (D-06).
- **Triggering build+deploy on tags:** Tags require a manual `git tag && git push --tags` step. D-05 requires automatic deploy on push to main.
- **Omitting `buildMethod: WebGLBuildScript.Build`:** GameCI's default build method doesn't call the custom script. Gzip compression and `decompressionFallback` settings won't be applied.
- **Unity version mismatch:** Both existing workflows use `6000.0.23f1` but the project requires `6000.3.10f1` (the version in `.gitlab-ci.yml`'s image tag `unityci/editor:6000.3.10f1-webgl-3`). Wrong version = license activation failure or wrong build.
- **Gating build on C# unit tests that don't exist:** The current `ci.yml` has `needs: [unity-tests, unity-playmode-tests]` on the build job. No C# unit tests exist (v2 requirement). Drop the dependency or the build will never run.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Unity license activation | Custom Docker activation scripts | `game-ci/unity-builder@v4` | GameCI handles activation, Docker image selection, Unity install, exit code propagation |
| Disk space cleanup | Custom `rm -rf` scripts | `jlumbroso/free-disk-space@v1.3.1` | Tested action targeting specific large packages; safer and more complete than ad-hoc cleanup |
| LFS caching | Custom LFS pull scripts | `actions/cache@v4` on `.git/lfs` | Standard approach; hash-based cache key avoids stale objects |
| Unity Library caching | Re-running full import every build | `actions/cache@v4` on `Library/` | 10+ minute time savings per build on cache hit |
| Cloudflare Pages deploy | Custom `curl` API calls | `npx wrangler@^4 pages deploy` | Official CLI handles auth, upload, branch mapping, deployment status |

**Key insight:** GameCI exists precisely because Unity license activation in Docker is notoriously complex — version-specific `.ulf` files, `|| true` masking, encoding issues. Let GameCI own it entirely.

---

## Common Pitfalls

### Pitfall 1: Wrong Unity Version in Workflow

**What goes wrong:** Both existing workflows specify `unityVersion: 6000.0.23f1`. The actual project uses `6000.3.10f1` (confirmed in `.gitlab-ci.yml` Docker image tag). GameCI selects the Docker image based on `unityVersion`. Wrong version means different runtime behavior and potential license activation failure.

**Why it happens:** Workflows were written before the final Unity version was set, or copied from a template.

**How to avoid:** Always match the version in `ProjectSettings/ProjectVersion.txt`. Use `unityVersion: auto` to let GameCI detect it, or hardcode `6000.3.10f1`.

**Warning signs:** Build log shows Unity version mismatch warnings; builds succeed locally but fail in CI.

### Pitfall 2: Content-Encoding in `_headers` Breaks WebGL Load

**What goes wrong:** The existing `deploy.yml` (lines 94-107) generates a `_headers` file with `Content-Encoding: br` for `.wasm.br`, `.js.br`, and `.data.br` files. Two problems: (1) the build uses Gzip, not Brotli — these filenames don't exist; (2) Cloudflare Pages ignores custom `Content-Encoding` headers in `_headers` files anyway.

**Why it happens:** Documentation inconsistency — `docs/ci-cd-gotchas.md` says "Brotli" when build script uses Gzip.

**How to avoid:** `_headers` file should only set `Cache-Control`. The `decompressionFallback=true` in `WebGLBuildScript.cs` makes the Unity JS loader handle decompression — no server-side `Content-Encoding` needed.

**Warning signs:** Game fails to load; browser console shows `CompileError: WebAssembly.instantiateStreaming failed`; Network tab shows double-gzip or missing encoding.

### Pitfall 3: Build Job Blocked by Non-Existent Tests

**What goes wrong:** `ci.yml` has `needs: [unity-tests, unity-playmode-tests]` on `unity-build`. No C# unit tests exist (v2 requirement). The test runner job will fail with "no tests found" — blocking the build job.

**Why it happens:** Workflow was written anticipating future tests that haven't been written yet.

**How to avoid:** Remove the dependency on `unity-tests` and `unity-playmode-tests` for the build job. The C# test runner jobs can remain but should not block deployment until tests exist.

### Pitfall 4: wrangler v3 via `cloudflare/pages-action`

**What goes wrong:** `cloudflare/pages-action@v1` pins wrangler to v3 (confirmed: `wranglerVersion: '3'` in existing `deploy.yml`). D-06 requires wrangler v4. With the action approach, there's no clean way to force v4.

**Why it happens:** The `cloudflare/pages-action` action hasn't been updated to wrangler v4.

**How to avoid:** Remove `cloudflare/pages-action` entirely. Use `npx wrangler@^4 pages deploy` as a run step. Pass `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID` as environment variables.

### Pitfall 5: `_headers` Filename Patterns Don't Match Hashed Files

**What goes wrong:** `nameFilesAsHashes=true` in `WebGLBuildScript.cs` produces filenames like `Build/4a3b2c1d.wasm.gz` — not `Build/golf-game.wasm.gz`. Header patterns like `/Build/golf-game.*` won't match.

**Why it happens:** Standard documentation examples use specific filenames.

**How to avoid:** Always use wildcard patterns: `/Build/*` covers all hashed filenames. Already correct in D-10.

### Pitfall 6: Missing `free-disk-space` Step Causes Build Failure

**What goes wrong:** Unity 6 images are 15+ GB. GitHub Actions ubuntu-latest provides ~14 GB free. Without cleanup, the image pull fails with "no space left on device."

**Why it happens:** GitHub-hosted runners have limited disk space that Unity images exceed.

**How to avoid:** Always add `jlumbroso/free-disk-space@v1.3.1` as the first step before checkout in any job that uses GameCI.

### Pitfall 7: Personal License Expiry (30-Day Limit)

**What goes wrong:** Unity Personal license (`.ulf` file) activates for ~30 days. After expiry, the `UNITY_LICENSE` secret value becomes invalid. Builds fail with "no valid Unity license."

**Why it happens:** Personal license design — requires periodic reactivation.

**How to avoid:** Document the renewal process in `docs/ci-cd-gotchas.md`. When it fails: (1) re-generate `.ulf` locally via Unity Hub, (2) update the `UNITY_LICENSE` GitHub secret with new file contents. The v2 upgrade path is serial-based activation (PERF-02) which doesn't expire.

---

## Code Examples

Verified patterns from official sources:

### Complete Build + Deploy Job Structure

```yaml
# Source: GameCI docs (game.ci/docs/github/builder) + Cloudflare docs
build-and-deploy:
  name: Build WebGL + Deploy
  runs-on: ubuntu-latest
  if: github.ref == 'refs/heads/main'

  steps:
    - uses: jlumbroso/free-disk-space@v1.3.1
      with:
        tool-cache: false
        android: true
        dotnet: true
        haskell: true
        large-packages: true
        docker-images: true
        swap-storage: true

    - uses: actions/checkout@v4
      with:
        lfs: true

    - name: Create LFS file list
      run: git lfs ls-files -l | cut -d' ' -f1 | sort > .lfs-assets-id

    - uses: actions/cache@v4
      with:
        path: .git/lfs
        key: lfs-${{ hashFiles('.lfs-assets-id') }}

    - run: git lfs pull && rm -f .lfs-assets-id

    - uses: actions/cache@v4
      with:
        path: Library
        key: Library-WebGL-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
        restore-keys: Library-WebGL-

    - uses: game-ci/unity-builder@v4
      env:
        UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
        UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
      with:
        targetPlatform: WebGL
        unityVersion: 6000.3.10f1
        buildName: golf-game
        buildMethod: WebGLBuildScript.Build

    - name: Copy _headers into build output
      run: cp _headers build/WebGL/golf-game/_headers

    - name: Deploy to Cloudflare Pages
      run: npx wrangler@^4 pages deploy build/WebGL/golf-game/ --project-name=golf-game
      env:
        CLOUDFLARE_API_TOKEN: ${{ secrets.CLOUDFLARE_API_TOKEN }}
        CLOUDFLARE_ACCOUNT_ID: ${{ secrets.CLOUDFLARE_ACCOUNT_ID }}
```

### `_headers` File Content (to commit at repo root)

```
# Source: Cloudflare Pages _headers docs + D-10 locked decisions
/index.html
  Cache-Control: no-cache

/Build/*
  Cache-Control: public, max-age=31536000, immutable
```

### Lint Job

```yaml
# Source: existing .ci/FormatCheck.csproj (already correct)
lint:
  name: Lint (C# whitespace)
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    - run: dotnet format whitespace .ci/FormatCheck.csproj --verify-no-changes --verbosity diagnostic
```

### Cloud Code Tests Job

```yaml
# Source: existing ci.yml (already correct)
cloud-code-tests:
  name: Cloud Code Tests
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: 20
    - run: node --test Assets/CloudCode/validate-and-post-score.test.js
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `cloudflare/pages-action@v1` (wrangler v3) | `npx wrangler@^4 pages deploy` | Wrangler v4 released 2024 | `pages-action` hasn't updated; must use CLI directly |
| GitLab manual `.ulf` license activation | GameCI online activation via secrets | GameCI v4 | No `.ulf` generation needed; just use UNITY_LICENSE + UNITY_EMAIL + UNITY_PASSWORD |
| Trigger deploy on git tags | Trigger deploy on push to main | Project decision D-05 | Removes manual `git tag` step; every main push deploys |
| `npm install -g wrangler` (unversioned) | `npx wrangler@^4` (pinned range) | Project decision D-06 | Prevents silent breakage from wrangler major version bumps |
| `game-ci/unity-builder@v2` (older docs) | `game-ci/unity-builder@v4` | GameCI v4 current stable | v4.8.1 is latest; use `@v4` tag to get patches automatically |

**Deprecated/outdated in existing workflows:**
- `wranglerVersion: '3'` in `cloudflare/pages-action` — superseded by wrangler v4
- `unityVersion: 6000.0.23f1` — wrong; actual project version is `6000.3.10f1`
- Brotli `Content-Encoding` in `_headers` — build produces Gzip; `Content-Encoding` cannot be set via `_headers` anyway
- Tag-based deploy trigger — replaced by push-to-main trigger per D-05
- `game-ci/unity-test-runner` jobs blocking build — no C# tests exist, these jobs would fail and block the build

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js | Cloud code tests, wrangler deploy | On CI: ubuntu-latest via actions/setup-node | v20 (CI), v22.14.0 (local) | — |
| npm | wrangler via npx | On CI: bundled with Node | v10.9.2 (local) | — |
| wrangler | Cloudflare Pages deploy | Not globally installed | v4.76.0 via npx | npx wrangler@^4 fetches on demand |
| dotnet SDK | Lint (dotnet format) | On CI: via actions/setup-dotnet@v4 | v8.0.x (CI) | — |
| .ci/FormatCheck.csproj | Lint job | Exists at `/Users/ashleyrevlett1/Documents/apps/golf-game/.ci/FormatCheck.csproj` | — | — |
| UNITY_LICENSE secret | Unity build | Must be manually configured in GitHub repo secrets | — | No fallback — must be set before CI runs |
| CLOUDFLARE_API_TOKEN secret | Deploy | Must be manually configured in GitHub repo secrets | — | No fallback — must be set before deploy runs |
| CLOUDFLARE_ACCOUNT_ID | Deploy | Must be manually configured in GitHub repo secrets | — | No fallback — deploy will fail without it |

**Missing dependencies with no fallback:**
- GitHub repository secrets (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`) — must be manually configured by a human with access to the GitHub repo settings and Unity/Cloudflare accounts. The plan should document this as a prerequisite step.

**Missing dependencies with fallback:**
- None beyond the secrets.

---

## Open Questions

1. **Unity version: `6000.3.10f1` vs `auto` detection**
   - What we know: `.gitlab-ci.yml` uses Docker image `unityci/editor:6000.3.10f1-webgl-3`, confirming the project version is `6000.3.10f1`.
   - What's unclear: Whether `ProjectSettings/ProjectVersion.txt` has been updated to `6000.3.10f1` (GameCI `unityVersion: auto` reads from it).
   - Recommendation: Hardcode `unityVersion: 6000.3.10f1` to be explicit rather than relying on auto-detection.

2. **Should `.gitlab-ci.yml` be deleted?**
   - What we know: D-01 gives planner discretion.
   - What's unclear: Whether any team process depends on it.
   - Recommendation: Delete it. It causes confusion (contradicts GitHub Actions setup) and has known bugs (`|| true` masking on license activation). Document deletion in commit message.

3. **Should `ci.yml` and `deploy.yml` be replaced or modified?**
   - What we know: Both have multiple issues requiring changes throughout.
   - Recommendation: Replace both with a single `build.yml`. The fixes required are pervasive enough that a clean rewrite is clearer than patching. Delete `ci.yml` and `deploy.yml`, create `build.yml`.

4. **CLOUDFLARE_ACCOUNT_ID: required by wrangler v4?**
   - What we know: Wrangler `pages deploy` accepts either `CLOUDFLARE_API_TOKEN` alone (with account inferred from token scope) or both `CLOUDFLARE_API_TOKEN` + `CLOUDFLARE_ACCOUNT_ID`.
   - Recommendation: Include both secrets in documentation and CI to be safe. The token alone may work if scoped to one account, but adding the account ID eliminates ambiguity.

---

## Sources

### Primary (HIGH confidence)
- GameCI GitHub Actions docs (game.ci/docs/github/getting-started, /activation, /builder) — action syntax, secret names, output path conventions
- Cloudflare Pages `_headers` docs (developers.cloudflare.com/pages/configuration/headers/) — header format and limitations
- Cloudflare Pages CI integration docs (developers.cloudflare.com/pages/how-to/use-direct-upload-with-continuous-integration/) — wrangler-action usage pattern
- Direct codebase analysis — `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`, `.gitlab-ci.yml`, `Assets/Editor/WebGLBuildScript.cs`, `.ci/FormatCheck.csproj`
- `.planning/research/PITFALLS.md` — project-specific CI/CD pitfalls already researched

### Secondary (MEDIUM confidence)
- `npm view wrangler version` — confirmed current wrangler stable: 4.76.0
- `github.com/cloudflare/wrangler-action/releases` — confirmed latest: v3.14.1 (uses wrangler v3 internally — confirmed reason to avoid it)
- `github.com/game-ci/unity-builder/releases` — confirmed latest: v4.8.1

### Tertiary (LOW confidence)
- None — all critical findings verified against official sources or direct codebase analysis

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versions verified via npm registry and GitHub releases pages
- Architecture: HIGH — patterns derived from existing workflow files and official docs
- Pitfalls: HIGH — most from direct codebase analysis of existing workflow bugs

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (stable ecosystem; wrangler/GameCI updates are patch-level)

**nyquist_validation:** SKIPPED — `workflow.nyquist_validation` is explicitly `false` in `.planning/config.json`.
