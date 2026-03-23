# Feature Landscape: CI/CD and Deployment

**Domain:** Unity 6 WebGL deployment pipeline (GitLab CI + Cloudflare Pages)
**Researched:** 2026-03-22

## Table Stakes

Features required for the pipeline to function at all. Missing = builds fail or deploy is broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| `UNITY_LICENSE` configured in GitLab CI/CD variables | Build stage cannot activate Unity Editor without a license. Pipeline fails immediately. | Low (manual step) | Copy `.ulf` content from local machine to GitLab variable. |
| `CLOUDFLARE_API_TOKEN` configured in GitLab CI/CD variables | Deploy stage cannot authenticate with Cloudflare. Deployment fails. | Low (manual step) | Create API token in Cloudflare dashboard with Pages:Edit permission. |
| Correct Docker image tag in `.gitlab-ci.yml` | Wrong tag means Docker pull fails. Build never starts. | None (already correct) | `unityci/editor:6000.3.10f1-webgl-3` verified to exist. |
| `decompressionFallback = true` in WebGL build settings | Without this, Cloudflare double-compresses and WebGL fails to load in browser. | None (already set) | Set in `WebGLBuildScript.cs`. No change needed. |
| Git LFS installed and configured | Unity projects use LFS for binary assets. Without LFS pull, assets are pointer files. Build fails or produces broken output. | None (already in CI) | `.gitlab-ci.yml` has `git lfs install && git lfs pull`. |

## Differentiators

Features that improve pipeline reliability and deployment quality. Not strictly required but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| `_headers` file with cache policy | Aggressive caching on Build/ assets (immutable, 1yr). No-cache on HTML. Faster repeat loads. | Low | Create during deploy stage. Content-hashed filenames make immutable caching safe. |
| Pinned wrangler version (`^4`) | Prevents surprise breakage from major version bumps. Reproducible deploys. | Low | Change `npm install -g wrangler` to `npm install -g wrangler@^4`. |
| Documentation corrections | Prevents future developers from "fixing" Gzip to Brotli based on wrong docs. | Low | Update 5 files with correct compression format and Unity version. |
| Deploy preview URLs per branch | Every MR gets its own Cloudflare preview URL for testing before merge. | Low | Already partially supported. Cloudflare Pages auto-creates preview URLs per branch. Need to update `deploy` stage to pass `--branch` flag on non-main branches. |

## Anti-Features

Features to explicitly NOT build in this milestone.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Brotli compression | Docs incorrectly reference Brotli, but switching would not help. Same Cloudflare double-compression problem. Longer build times. | Keep Gzip + decompressionFallback. |
| Content-Encoding headers in `_headers` | Would tell browsers to decompress before passing to Unity's JS loader, causing double-decompression failure. | Let Unity's embedded decompressor handle it. Only set cache headers. |
| Self-hosted GitLab runner | Adds infrastructure complexity (Mac mini, Docker, maintenance). Shared runners work. | Use GitLab shared runners. If disk space becomes an issue, add runner tags. |
| Automated deploy on push to main | Production deploys should be deliberate. A bad build auto-deploying breaks the live game. | Keep `when: manual` trigger on deploy stage. |
| Custom Cloudflare Worker for compression | Overkill for serving static files. Pages is the correct product. | Use Cloudflare Pages with `_headers` file. |
| Unity Pro license for CI | Avoids 30-day expiry of Personal licenses, but costs money. Only worth it if CI runs frequently. | Use Personal license. Renew `.ulf` every 30 days. Evaluate Pro if renewal becomes burdensome. |

## Feature Dependencies

```
UNITY_LICENSE configured --> webgl-build stage can run
CLOUDFLARE_API_TOKEN configured --> deploy stage can run
webgl-build stage passes --> deploy stage has artifacts to deploy
_headers file created --> deploy uploads it alongside build
Documentation corrections --> no dependencies, can be done anytime
```

## MVP Recommendation

Prioritize:
1. Configure `UNITY_LICENSE` in GitLab CI (blocks everything else)
2. Configure `CLOUDFLARE_API_TOKEN` in GitLab CI (blocks deploy)
3. Pin wrangler version in deploy stage (prevents future breakage)
4. Add `_headers` file creation in deploy stage (improves caching)
5. Correct documentation (prevents future confusion)

Defer:
- Preview URLs per branch: nice-to-have, not needed for initial deploy
- Automated testing of WebGL in browser: would require Playwright/Cypress setup, significant scope

## Sources

- Project codebase analysis (`.gitlab-ci.yml`, `WebGLBuildScript.cs`, docs)
- [Cloudflare Pages Headers](https://developers.cloudflare.com/pages/configuration/headers/)
- [Unity Manual: Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html)
- [GameCI GitLab Activation](https://game.ci/docs/gitlab/activation/)

---

*Features research: 2026-03-22*
