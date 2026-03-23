# Research Summary: CI/CD and Cloudflare Pages Deployment

**Domain:** Unity 6 WebGL deployment pipeline (GitLab CI + Cloudflare Pages)
**Researched:** 2026-03-22
**Overall confidence:** HIGH

## Executive Summary

The existing CI/CD pipeline is structurally sound but has three categories of issues: (1) a known-broken interaction between Unity's Gzip compression and Cloudflare Pages' automatic compression, (2) missing documentation and incorrect references in deployment docs, and (3) unpinned dependency versions in the deploy stage.

The core compression problem is well-understood and already partially solved in the codebase. The `WebGLBuildScript.cs` correctly sets `decompressionFallback = true`, which makes Unity rename compressed files to `.unityweb` and embed a JavaScript decompressor in the loader. This completely sidesteps Cloudflare's double-compression problem because Cloudflare does not recognize `.unityweb` files as pre-compressed content. However, the project lacks a `_headers` file for cache control, and the documentation incorrectly claims the build uses Brotli when it actually uses Gzip.

The Docker image (`unityci/editor:6000.3.10f1-webgl-3`) was verified to exist on Docker Hub. The license activation approach using `-manualLicenseFile` with a `UNITY_LICENSE` environment variable is simpler and more reliable than GameCI v4's serial-based activation for Unity Personal licenses.

The deploy stage needs minor hardening: pin wrangler to `^4`, add a `_headers` file for cache optimization (not compression), and ensure `CLOUDFLARE_API_TOKEN` is configured in GitLab CI variables. The total CI/CD fix scope is small -- primarily documentation corrections, one `_headers` file, and verifying that GitLab CI secrets are properly configured.

## Key Findings

**Stack:** Docker image tag is correct. WebGL compression settings are correct. Wrangler needs version pinning.
**Architecture:** Pipeline stages (lint -> test -> build -> deploy) are correctly ordered. The manual deploy trigger on main is appropriate.
**Critical pitfall:** The docs say "Brotli" but the code says "Gzip" -- this mismatch could cause someone to "fix" the compression to Brotli and break the pipeline.

## Implications for Roadmap

Based on research, suggested phase structure:

1. **Configure CI Secrets** - Set `UNITY_LICENSE` and `CLOUDFLARE_API_TOKEN` in GitLab CI/CD variables
   - Addresses: Pipeline cannot run without license activation; cannot deploy without Cloudflare token
   - Avoids: The #1 blocker (pipeline will fail immediately without these)
   - Prerequisite for all other phases

2. **Fix Documentation** - Correct Brotli-to-Gzip references, update Unity version numbers
   - Addresses: Incorrect docs in `ci-cd-gotchas.md`, `deployment.md`, `PROJECT.md`, `codebase/STACK.md`
   - Avoids: Future confusion where someone "fixes" compression to match wrong docs

3. **Harden Deploy Stage** - Pin wrangler version, add `_headers` for caching, verify deploy works
   - Addresses: Unpinned wrangler, missing cache headers, untested deploy path
   - Avoids: Build breakage from wrangler major version bump; slow loads from no cache policy

4. **Verify End-to-End** - Run full pipeline, confirm WebGL loads in browser without compression errors
   - Addresses: The fundamental question "does it work?"
   - Avoids: Shipping a broken deploy

**Phase ordering rationale:**
- Secrets first because nothing else can be tested without them
- Docs second because they are zero-risk and prevent future confusion
- Deploy hardening third because it requires a working build to test
- End-to-end verification last because it validates everything

**Research flags for phases:**
- Phase 1: Needs manual action (Unity Hub license activation, Cloudflare dashboard token creation). Cannot be automated.
- Phase 2: Standard file edits, no research needed
- Phase 3: LOW risk -- `_headers` syntax is straightforward, wrangler `pages deploy` is stable
- Phase 4: May need deeper research if WebGL fails to load. The `decompressionFallback` approach should work but has not been verified with this specific project's build output.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Docker image | HIGH | Verified tag exists on Docker Hub via API query |
| Compression strategy | HIGH | `decompressionFallback = true` is the documented solution for Cloudflare Pages. Multiple community reports confirm. |
| `_headers` file | MEDIUM | The caching headers are standard. The "do not set Content-Encoding" guidance is HIGH confidence. Whether any `_headers` file is needed at all with decompressionFallback is uncertain -- it may work with no `_headers` at all. |
| License activation | MEDIUM | Manual license file approach works but licenses expire ~30 days. Serial-based approach avoids expiry but adds complexity. |
| Wrangler deployment | HIGH | `pages deploy` command is stable across v3 and v4. `CLOUDFLARE_API_TOKEN` authentication is well-documented. |
| Documentation corrections | HIGH | All errors verified by cross-referencing code with docs |

## Gaps to Address

- Unity Personal license expiry (~30 days) means CI will periodically break and need a fresh `.ulf` file. Consider upgrading to Unity Pro or setting up a floating license server for long-term CI stability.
- The interaction between `nameFilesAsHashes = true` and `_headers` wildcard patterns has not been tested. Content-hashed filenames may not match extension-based patterns.
- Whether Cloudflare Pages auto-compresses `.unityweb` files during delivery (which would be fine -- it is standard transparent compression, not double-compression of pre-compressed content) is unverified.
- The `include` directive in `.gitlab-ci.yml` references `project: roborev/claude-plugin` with `file: pipeline/agent-pipeline.yml`. This external pipeline inclusion may add stages or variables not visible in the project's CI file. Its impact on the build is unknown.

---

*Summary: 2026-03-22*
