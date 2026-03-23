# CI/CD Gotchas (GameCI + WebGL + Cloudflare Pages)

## Unity Version

The project uses Unity `6000.3.10f1`. The `build.yml` workflow hardcodes `unityVersion: 6000.3.10f1`. Using the wrong version (`6000.0.23f1` appeared in old workflows) causes GameCI to pull the wrong Docker image and may cause license activation failure.

## Gzip, Not Brotli

Unity WebGL builds use Gzip compression (`.gz` files). `WebGLBuildScript.cs` sets `compressionFormat = WebGLCompressionFormat.Gzip` and `decompressionFallback = true`. This means the Unity JS loader decompresses files in-browser — Cloudflare does NOT need to serve `Content-Encoding: gz` headers.

The `_headers` file at the repo root sets `Cache-Control` headers only. Do not add `Content-Encoding` — Cloudflare Pages ignores it via `_headers` anyway, and `decompressionFallback` already handles decompression client-side.

Build output uses hashed filenames (`nameFilesAsHashes = true`): files are named like `Build/4a3b2c1d.wasm.gz`, not `Build/golf-game.wasm.gz`. Use wildcard patterns in `_headers`: `/Build/*` not `/Build/golf-game.*`.

## GitHub Actions Secrets

Set these as repository secrets in GitHub repo Settings > Secrets and variables > Actions:

- `UNITY_LICENSE` — Full contents of `Unity_lic.ulf` (Personal license). On Mac: `~/Library/Application Support/Unity/Unity_lic.ulf`. Copy the entire XML file content into the secret (no base64 encoding needed — GitHub secrets support multi-line values).
- `UNITY_EMAIL` — Unity account email address
- `UNITY_PASSWORD` — Unity account password
- `CLOUDFLARE_API_TOKEN` — API token with "Cloudflare Pages: Edit" permission (Cloudflare dashboard > My Profile > API Tokens)
- `CLOUDFLARE_ACCOUNT_ID` — Cloudflare account ID (Cloudflare dashboard right sidebar)

GameCI uses online activation (not manual `.ulf` file activation). All three Unity secrets are required — GameCI handles the activation process automatically.

**Personal license expiry:** Unity Personal licenses activate for ~30 days. When builds fail with "no valid Unity license," generate a new `.ulf` in Unity Hub and update the `UNITY_LICENSE` secret.

## Disk Space (Unity 6 on GitHub Actions)

Unity 6 Docker images are 15+ GB. GitHub Actions ubuntu-latest provides ~14 GB free. The `build-and-deploy` job uses `jlumbroso/free-disk-space@v1.3.1` as its first step (before checkout) to free 20-30 GB:

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

This must be the first step — before checkout — to avoid "no space left on device" errors when Docker pulls the Unity image.

## GameCI Output Path

With `buildName: golf-game`, GameCI outputs to `build/WebGL/golf-game/`, not `build/WebGL/WebGL/`:

```yaml
# In build.yml deploy step
run: npx wrangler@^4 pages deploy build/WebGL/golf-game/ --project-name=golf-game
```

## LFS Dirty Build Error

`.lfs-assets-id` file (created to generate the LFS cache key) causes "dirty build" errors. It is deleted after LFS pull:

```yaml
- name: Pull LFS files
  run: git lfs pull && rm -f .lfs-assets-id
```

## Cloudflare Pages URL

Production URL: `https://golf-game-amm.pages.dev`

The `build-and-deploy` job deploys automatically on every push to `main`. No manual tagging or `workflow_dispatch` needed.

## Same Build Type for Staging and Production

Use release builds for both. Testing a dev build doesn't validate what ships.
