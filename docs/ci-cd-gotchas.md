# CI/CD Gotchas (GameCI + WebGL + Cloudflare Pages)

## GameCI Output Path

With `buildName: golf-game`, GameCI outputs to `build/WebGL/golf-game/`, not `build/WebGL/WebGL/`:
```yaml
# Correct — matches deploy.yml buildName param
directory: build/WebGL/golf-game
```

## Brotli, Not Gzip

GameCI compresses to `.br` (Brotli). Cloudflare Pages needs a `_headers` file to set `Content-Encoding: br`:
```yaml
# In _headers file
/Build/*.wasm.br
  Content-Encoding: br
  Content-Type: application/wasm
```

## Disk Space (Unity 6)

Unity 6 Docker images are ~15GB+. Add cleanup before checkout:
```yaml
- name: Free disk space
  uses: jlumbroso/free-disk-space@v1.3.1
  with:
    tool-cache: false
    android: true
    dotnet: true
    haskell: true
    large-packages: true
    docker-images: true
    swap-storage: true
```

## GameCI Requires All Three Secrets

```yaml
env:
  UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
  UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
  UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
```

## LFS Dirty Build Error

`.lfs-assets-id` file causes "dirty build" errors. Delete after cache lookup:
```yaml
- run: |
    git lfs pull
    rm -f .lfs-assets-id
```

## Cloudflare Pages Preview URLs

Preview deployments get auto-generated URLs per branch. Production is at `https://golf-game.pages.dev`.

## SemVer Pre-release Tags

Use dot separator: `v1.0.0-rc.1` not `v1.0.0-rc1`.

## Branch Naming (Cloudflare Pages)

- `production` branch → production deployment at `golf-game.pages.dev`
- `preview` branch → preview deployment at auto-generated URL
- Tags with a hyphen (`-rc`, `-alpha`) deploy to preview; clean semver to production

## Same Build Type for Staging and Production

Use release builds for both. Testing a dev build doesn't validate what ships.
