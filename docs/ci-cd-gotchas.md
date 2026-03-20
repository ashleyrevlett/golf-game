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

Unity 6 Docker images are ~15GB+. Add cleanup in `before_script`:
```yaml
before_script:
  - rm -rf /usr/share/dotnet /usr/local/lib/android /opt/ghc || true
```
Note: This runs inside the container, not on the host runner. It won't help with image-pull disk pressure on shared runners. For persistent disk issues, use a self-hosted runner or GitLab runner tags to route to a larger machine.

## GameCI Requires All Three Secrets

Set these as CI/CD variables in GitLab project settings (Settings > CI/CD > Variables):
- `UNITY_LICENSE` — `.ulf` file content (base64 or raw)
- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

Reference them in `.gitlab-ci.yml` as `$UNITY_LICENSE`, `$UNITY_EMAIL`, `$UNITY_PASSWORD`:
```yaml
variables:
  BUILD_PATH: build/WebGL/golf-game
before_script:
  - echo "$UNITY_LICENSE" > /tmp/unity.ulf
  - unity-editor -batchmode -nographics -quit
      -manualLicenseFile /tmp/unity.ulf
      -logFile /dev/stdout || true
  - rm -f /tmp/unity.ulf
```

## LFS Dirty Build Error

`.lfs-assets-id` file causes "dirty build" errors. Delete after LFS pull:
```yaml
script:
  - git lfs install && git lfs pull
  - rm -f .lfs-assets-id
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
