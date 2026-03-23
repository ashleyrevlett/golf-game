# Technology Stack: CI/CD and Deployment

**Project:** Golf Game -- Unity 6 WebGL on Cloudflare Pages via GitLab CI
**Researched:** 2026-03-22
**Focus:** Compression, _headers, Docker image, license activation, wrangler

---

## Recommended Stack

### Docker Image (CI Build)

| Technology | Version/Tag | Purpose | Why |
|------------|-------------|---------|-----|
| unityci/editor | `6000.3.10f1-webgl-3` | Headless Unity WebGL builds in GitLab CI | Matches local editor version (`6000.3.10f1` per `ProjectSettings/ProjectVersion.txt`). Confirmed exists on Docker Hub via API query. The `-webgl-3` suffix means GameCI image series 3 with the WebGL build module installed. |

**Confidence:** HIGH -- verified via Docker Hub API. Tag exists alongside ubuntu-prefixed variant.

**Version mismatch note:** `.planning/PROJECT.md` and `.planning/codebase/STACK.md` reference `6000.0.23f1` but the actual project version is `6000.3.10f1`. Unity 6.3 LTS (`6000.3.x`) is a newer LTS release supported through December 2027. This documentation discrepancy should be corrected but does not affect the build.

The existing `.gitlab-ci.yml` already uses this correct tag. No change needed to the image reference.

---

### Unity WebGL Compression Strategy

| Setting | Value | Purpose | Why |
|---------|-------|---------|-----|
| `compressionFormat` | **Gzip** | Pre-compress .wasm, .js, .data | Gzip is natively supported by all browsers over both HTTP and HTTPS. Faster builds than Brotli. |
| `decompressionFallback` | **true** | Embed JS decompressor in loader | **CRITICAL for Cloudflare Pages.** Sidesteps Cloudflare's double-compression problem entirely. |
| `nameFilesAsHashes` | **true** | Content-hashed filenames | Busts CDN caches on new deploys. Already set in `WebGLBuildScript.cs`. |

**Confidence:** HIGH -- verified via Unity official docs, Cloudflare community reports, and multiple deployment guides.

#### The Cloudflare Double-Compression Problem (Why This Matters)

When Unity builds with Gzip compression (`compressionFormat = Gzip`), it produces files like `Build/<hash>.wasm.gz`. These files are already gzip-compressed on disk. For a browser to use them correctly, the server must respond with `Content-Encoding: gzip` so the browser knows to decompress transparently before passing the data to WebAssembly.

**The problem with Cloudflare Pages:**

1. Cloudflare Pages does not let you serve pre-compressed files with reliable `Content-Encoding` headers
2. Even when you set `Content-Encoding: gzip` in a `_headers` file, Cloudflare's edge may:
   - Apply its own compression on top (double-compression), producing unreadable garbage
   - Strip or ignore the `Content-Encoding` header entirely
   - Behave differently on `.pages.dev` domains vs custom domains
3. The `cache-control: no-transform` workaround has been reported to work on `.pages.dev` but fail on custom domains
4. Cloudflare Compression Rules (which could disable compression per path) are only available on paid plans

**How `decompressionFallback = true` solves this:**

1. Unity renames output files from `.wasm.gz` / `.js.gz` / `.data.gz` to `.wasm.unityweb` / `.js.unityweb` / `.data.unityweb`
2. Unity's `loader.js` includes a built-in JavaScript gzip decompressor (~15KB overhead)
3. The browser fetches `.unityweb` files as opaque binary blobs -- no `Content-Encoding` header needed
4. Unity's JS loader decompresses them client-side before handing data to WebAssembly/framework
5. Cloudflare cannot interfere because it does not recognize `.unityweb` files as pre-compressed content
6. This approach is explicitly documented by Unity as the solution for hosting providers where server configuration is not available

**Trade-off:** The loader.js is slightly larger (embeds the decompressor) and decompression happens in JavaScript rather than natively in the browser. For a small game like this, the performance difference is negligible.

#### Why NOT Use Other Approaches

| Approach | Why Not |
|----------|---------|
| **Gzip without fallback + `_headers`** | Cloudflare Pages does not reliably serve pre-compressed files. Multiple community reports confirm `Content-Encoding: gzip` in `_headers` does not prevent double-compression. |
| **Brotli + fallback** | Same outcome as Gzip + fallback (Unity handles decompression client-side), but Brotli builds take significantly longer. No benefit for this use case. |
| **Brotli without fallback + `_headers`** | Same Cloudflare double-compression problem. Brotli also requires HTTPS (Cloudflare provides this, so not a blocker, but adds unnecessary constraints). |
| **Disabled compression (let Cloudflare auto-compress)** | Cloudflare auto-compresses `application/wasm` and `application/javascript`, but `.data` files (served as `application/octet-stream`) may not be compressed. Uncompressed builds are 2-5x larger on disk, increasing upload time and artifact storage. Loses control over compression ratios. |

---

### Cloudflare Pages `_headers` File

| Item | Recommendation | Why |
|------|---------------|-----|
| `_headers` file needed? | **Optional but recommended** | With `decompressionFallback = true`, Unity's loader does not depend on MIME types or Content-Encoding. But correct MIME types improve browser caching behavior. |
| Content-Encoding headers? | **Do NOT set** | Unity handles decompression via JS. Setting `Content-Encoding: gzip` would tell the browser to decompress *before* passing to Unity's loader, causing double-decompression failure. |
| File placement | Root of build output (`build/WebGL/golf-game/_headers`) | Must be in the directory passed to `wrangler pages deploy`. Created as a CI pipeline step after the Unity build. |

**Confidence:** HIGH for the "do not set Content-Encoding" recommendation. MEDIUM for whether `_headers` is needed at all (it is belt-and-suspenders).

**Recommended minimal `_headers` content:**

```
/*
  X-Content-Type-Options: nosniff

/Build/*
  Cache-Control: public, max-age=31536000, immutable

/*.html
  Cache-Control: no-cache
```

This sets aggressive caching on Build assets (safe because `nameFilesAsHashes = true` means filenames change on new builds) and prevents caching of the HTML entry point (so users always get the latest loader reference). The `nosniff` header prevents MIME type sniffing attacks.

**Do NOT include patterns like:**
```
# WRONG -- do not use these with decompressionFallback
/Build/*.wasm.unityweb
  Content-Encoding: gzip
  Content-Type: application/wasm
```

With `nameFilesAsHashes = true`, the actual filenames are hex hashes (e.g., `Build/a1b2c3d4e5f6.unityweb`), so extension-based patterns like `*.wasm.unityweb` will not match anyway.

---

### Wrangler (Cloudflare Pages Deployment)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| wrangler | `^4` (pin major version) | Deploy static build to Cloudflare Pages via `pages deploy` | Current latest is 4.76.0. The `pages deploy` command has been stable across v3 and v4. Pinning to `^4` prevents surprise breakage from a future v5. |

**Confidence:** HIGH -- verified via npm registry and Cloudflare docs.

**Exact deploy command:**
```bash
npx wrangler@^4 pages deploy build/WebGL/golf-game/ --project-name=golf-game
```

**Authentication:** Requires `CLOUDFLARE_API_TOKEN` environment variable set in GitLab CI/CD variables. This is an API token (not API key) created in the Cloudflare dashboard with `Cloudflare Pages:Edit` permission.

**Current CI issue:** The deploy stage uses `npm install -g wrangler` (no version pin). This should be changed to `npm install -g wrangler@^4` or better, use `npx wrangler@^4` to avoid global install.

---

### License Activation (GameCI + Unity 6)

| Approach | Variables Required | How It Works |
|----------|-------------------|--------------|
| **Manual license file (recommended)** | `UNITY_LICENSE` | Writes .ulf content to file, passes to `-manualLicenseFile` flag. Simple, no external scripts. |
| Serial-based (GameCI v4) | `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD` | Uses `unity-editor -serial` with retry logic. More complex, requires GameCI helper scripts. |
| Floating license server | `UNITY_LICENSING_SERVER` | For organizations with Unity license servers. Not applicable here. |

**Confidence:** MEDIUM -- GameCI v4 docs describe serial-based activation as the primary method, but the manual license file approach is a Unity Editor feature (not GameCI-specific) that works in any Docker image containing `unity-editor`.

**Recommendation: Keep the manual license file approach** (Approach B in the current `.gitlab-ci.yml`). Rationale:

1. **Simpler:** One environment variable (`UNITY_LICENSE`) vs three (`UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`)
2. **No external dependencies:** Does not require cloning GameCI helper scripts or running `before_script.sh` from the example repo
3. **Works with the raw Docker image:** `-manualLicenseFile` is a Unity Editor CLI flag, not a GameCI abstraction
4. **Already implemented:** The current `.gitlab-ci.yml` already uses this approach correctly

**How to obtain the `UNITY_LICENSE` value:**

1. Open Unity Hub on your local machine, sign in, and activate a Unity Personal license
2. Locate the license file:
   - **macOS:** `~/Library/Unity/Unity_lic.ulf`
   - **Linux:** `~/.local/share/unity3d/Unity/Unity_lic.ulf`
   - **Windows:** `C:\ProgramData\Unity\Unity_lic.ulf`
3. Copy the **entire XML content** of the `.ulf` file
4. In GitLab: Settings > CI/CD > Variables > Add Variable
   - Key: `UNITY_LICENSE`
   - Value: paste the XML content
   - Type: Variable (not File)
   - Protected: **unchecked** (needed for merge request pipelines)
   - Masked: unchecked (XML content is too long to mask in GitLab)

**License expiry:** Unity Personal licenses activated locally typically expire after ~30 days. When the CI starts failing with license errors, repeat the steps above with a fresh `.ulf` file. Unity Pro serial-based licenses do not have this limitation.

**GameCI v4 breaking changes (for reference, not recommended for this project):**

- `UNITY_USERNAME` renamed to `UNITY_EMAIL`
- `UNITY_SERIAL` is now mandatory (was optional in v3)
- The old `get_activation_file.sh` script was removed
- Activation uses `before_script.sh` and `after_script.sh` from the example repo
- Serial must be extracted from `.ulf` file's `DeveloperData` field via base64 decode

---

### Node.js (Deploy Stage)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Node.js | `20` (LTS) | Run wrangler for deployment + Cloud Code tests | Node 20 is current LTS. Wrangler 4.x requires Node 18+. Already used in deploy and test stages. |

**Confidence:** HIGH.

---

## GitLab CI/CD Variables Summary

### Required Variables

| Variable | Value | Protected? | Purpose |
|----------|-------|------------|---------|
| `UNITY_LICENSE` | Raw XML content of `Unity_lic.ulf` | No | Unity license activation in Docker |
| `CLOUDFLARE_API_TOKEN` | API token from Cloudflare dashboard | Yes (main only is fine) | Wrangler authentication for Pages deploy |

### Optional Variables (Not Currently Needed)

| Variable | Value | When Needed |
|----------|-------|-------------|
| `UNITY_EMAIL` | Unity account email | Only if switching to serial-based activation |
| `UNITY_PASSWORD` | Unity account password | Only if switching to serial-based activation |
| `UNITY_SERIAL` | License serial from .ulf | Only if switching to serial-based activation |

---

## Current vs Recommended `.gitlab-ci.yml` Changes

### Current `webgl-build` stage (keep mostly as-is)

The existing build stage is correct. The before_script correctly:
- Cleans up disk space (`rm -rf /usr/share/dotnet ...`)
- Writes license file and activates via `-manualLicenseFile`
- Cleans up the license file after activation

The script correctly:
- Installs and pulls Git LFS
- Removes `.lfs-assets-id` to avoid dirty build errors
- Runs `unity-editor` with `-executeMethod WebGLBuildScript.Build`

### Current `deploy` stage (needs changes)

```yaml
# CURRENT (has issues)
deploy:
  stage: deploy
  image: node:20
  needs: [webgl-build]
  script:
    - npm install -g wrangler                    # No version pin
    - wrangler pages deploy build/WebGL/golf-game/ --project-name=golf-game
  # Missing: _headers file creation

# RECOMMENDED
deploy:
  stage: deploy
  image: node:20
  needs: [webgl-build]
  script:
    - npm install -g wrangler@^4                 # Pin major version
    # Create _headers file for caching (not compression)
    - |
      cat > build/WebGL/golf-game/_headers << 'HEADERS'
      /*
        X-Content-Type-Options: nosniff
      /Build/*
        Cache-Control: public, max-age=31536000, immutable
      /*.html
        Cache-Control: no-cache
      HEADERS
    - wrangler pages deploy build/WebGL/golf-game/ --project-name=golf-game
  environment:
    name: production
  only:
    - main
  when: manual
```

---

## WebGLBuildScript.cs (No Changes Needed)

The existing build script already has the correct settings:

```csharp
PlayerSettings.WebGL.nameFilesAsHashes = true;        // CDN cache busting
PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
PlayerSettings.WebGL.decompressionFallback = true;     // Critical for Cloudflare
```

These three settings together produce `.unityweb` files with content-hashed filenames that work on Cloudflare Pages without any server-side compression configuration.

---

## Documentation Corrections Needed

| File | Issue | Correction |
|------|-------|------------|
| `docs/ci-cd-gotchas.md` line 13-18 | "Brotli, Not Gzip" section is wrong | The build uses Gzip, not Brotli. The `_headers` example shows `.wasm.br` which is incorrect. Should document the Gzip + decompressionFallback approach. |
| `docs/ci-cd-gotchas.md` line 33-35 | "GameCI Requires All Three Secrets" | With manual license file approach, only `UNITY_LICENSE` is needed. The three-variable requirement is for GameCI v4 serial-based activation. |
| `docs/deployment.md` line 14 | "Brotli-compressed Unity WebGL assets" | Should say "Gzip-compressed with decompression fallback" |
| `.planning/PROJECT.md` line 56 | Unity version `6000.0.23f1` | Should be `6000.3.10f1` |
| `.planning/codebase/STACK.md` line 16 | Unity version `6000.0.23f1` | Should be `6000.3.10f1` |

---

## Sources

- [Unity Manual: Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html) -- compression formats, decompressionFallback behavior
- [Unity Server Configuration Code Samples](https://docs.unity.cn/Manual/webgl-server-configuration-code-samples.html) -- exact header mappings per compression format and fallback setting
- [Cloudflare Pages Headers Configuration](https://developers.cloudflare.com/pages/configuration/headers/) -- `_headers` file syntax, 100-rule limit, 2000-char line limit, splat patterns
- [Cloudflare Content Compression](https://developers.cloudflare.com/speed/optimization/content/compression/) -- auto-compression behavior, `cache-control: no-transform`, compressible MIME types including application/wasm
- [Cloudflare Community: Pre-Compressed Assets in Pages](https://community.cloudflare.com/t/pre-compressed-assets-in-pages/300028) -- confirms Pages does not reliably serve pre-compressed files
- [Cloudflare Community: End-to-end compression for Pages](https://community.cloudflare.com/t/end-to-end-compression-for-pages/744695) -- confirms Content-Encoding headers unreliable on Pages
- [Unity WebGL Compression Done Right](https://miltoncandelero.github.io/unity-webgl-compression) -- advocates Disabled compression + external pre-compression, documents decompressionFallback mechanics
- [GameCI GitLab Activation (v4)](https://game.ci/docs/gitlab/activation/) -- UNITY_SERIAL, UNITY_EMAIL, UNITY_PASSWORD variables
- [GameCI v4.0.0 Release Notes](https://gitlab.com/game-ci/unity3d-gitlab-ci-example/-/releases/v4.0.0) -- breaking changes to license activation
- [GameCI v4 Variable Changes (Issue #485)](https://github.com/game-ci/documentation/issues/485) -- UNITY_USERNAME renamed to UNITY_EMAIL, UNITY_SERIAL now mandatory
- [Docker Hub: unityci/editor tags](https://hub.docker.com/r/unityci/editor/tags) -- confirmed `6000.3.10f1-webgl-3` tag exists
- [wrangler on npm](https://www.npmjs.com/package/wrangler) -- current version 4.76.0
- [Cloudflare Pages Direct Upload](https://developers.cloudflare.com/pages/get-started/direct-upload/) -- `wrangler pages deploy` command, 20K file limit, 25MB per file
- [Wrangler Install/Update Docs](https://developers.cloudflare.com/workers/wrangler/install-and-update/) -- local vs global install guidance

---

*Stack research: 2026-03-22*
