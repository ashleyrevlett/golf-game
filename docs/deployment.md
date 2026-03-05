# Deployment

## Architecture

```
GitHub Actions (GameCI build) → Cloudflare Pages → Users
```

## Cloudflare Pages

- Project name: `golf-game`
- Production URL: `https://golf-game.pages.dev`
- Preview deployments: auto-generated per branch
- Brotli-compressed Unity WebGL assets served with `_headers` rules

## GitHub Actions Workflow

Trigger: push a semver tag (`v1.0.0` for production, `v1.0.0-rc.1` for preview).

Steps:
1. Free disk space (GameCI requirement)
2. Checkout with LFS
3. Determine version and environment from tag
4. Build WebGL with GameCI `unity-builder`
5. Write `_headers` file for Brotli content encoding
6. Deploy via `cloudflare/pages-action`

Manual trigger via `workflow_dispatch` is also supported (choose `preview` or `production`).

## Caching Strategy

Two layers:

1. **Hashed filenames** (`webGLNameFilesAsHashes: 1`) — unique filenames per build
2. **Cloudflare `_headers`** — sets `Content-Encoding: br` for `.wasm.br`, `.js.br`, `.data.br` and COOP/COEP headers for SharedArrayBuffer

## Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `UNITY_LICENSE` | Unity `.ulf` license file contents |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `CLOUDFLARE_API_TOKEN` | Cloudflare API token with Pages deploy permission |
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare account ID |

## Tagging and Releasing

Semver with `alpha` → `rc` → stable progression:

```bash
# Preview (pre-release)
git tag -a v1.0.0-rc.1 -m "Release candidate 1"
git push --tags

# Production (stable)
git tag -a v1.0.0 -m "Initial release"
git push --tags
```

Tags with a hyphen (`-`) deploy to preview. Clean semver tags deploy to production.

## Rollback

Re-deploy a previous tag or use Cloudflare Pages dashboard to roll back to a prior deployment. Prefer "fail forward" — tag and deploy a fix rather than rolling back.
