# Deployment

## Architecture

```
GitHub push to main → GitHub Actions (build.yml) → game-ci/unity-builder → npx wrangler@^4 → Cloudflare Pages → Users
```

## Cloudflare Pages

- Project name: `golf-game`
- Production URL: `https://golf-game-amm.pages.dev`
- Compression: Gzip (Unity WebGL with `decompressionFallback = true`)

## Automated Deploy (GitHub Actions)

Every push to `main` triggers the `build-and-deploy` job in `.github/workflows/build.yml`:

1. Frees disk space (Unity 6 images are 15+ GB)
2. Checks out repo with LFS files
3. Caches Unity Library folder (avoids ~10 min re-import on cache hit)
4. Builds WebGL via `game-ci/unity-builder@v4` using `WebGLBuildScript.Build`
5. Copies `_headers` into `build/WebGL/golf-game/`
6. Deploys to Cloudflare Pages via `npx wrangler@^4 pages deploy`

No manual tagging or intervention required — push to main and the deploy happens automatically.

## Required GitHub Secrets

Configure these in GitHub repo Settings > Secrets and variables > Actions before the pipeline will work:

| Secret | Purpose | Where to Get |
|--------|---------|--------------|
| `UNITY_LICENSE` | GameCI license activation | Full contents of `~/Library/Application Support/Unity/Unity_lic.ulf` |
| `UNITY_EMAIL` | GameCI license activation | Unity account email |
| `UNITY_PASSWORD` | GameCI license activation | Unity account password |
| `CLOUDFLARE_API_TOKEN` | wrangler deploy auth | Cloudflare dashboard > API Tokens (Pages:Edit permission) |
| `CLOUDFLARE_ACCOUNT_ID` | wrangler deploy target | Cloudflare dashboard right sidebar |

## Manual Deploy (Local)

For one-off deploys without pushing to main:

```bash
# 1. Build WebGL
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath ~/Documents/apps/golf-game \
  -executeMethod WebGLBuildScript.Build \
  -logFile /tmp/unity-build.log

# 2. Copy _headers into build output
cp _headers build/WebGL/golf-game/_headers

# 3. Deploy to Cloudflare Pages
export CLOUDFLARE_API_TOKEN=<your-token>
export CLOUDFLARE_ACCOUNT_ID=<your-account-id>

npx wrangler@^4 pages deploy build/WebGL/golf-game/ \
  --project-name=golf-game \
  --commit-dirty=true
```

## Compression

Unity WebGL builds use Gzip (`.gz` files). `WebGLBuildScript.cs` sets:
- `compressionFormat = WebGLCompressionFormat.Gzip`
- `decompressionFallback = true` — Unity JS loader decompresses files client-side
- `nameFilesAsHashes = true` — build files have hashed names like `Build/4a3b2c1d.wasm.gz`

Cloudflare Pages does NOT need to set `Content-Encoding` headers. The `_headers` file at the repo root sets `Cache-Control` only.
