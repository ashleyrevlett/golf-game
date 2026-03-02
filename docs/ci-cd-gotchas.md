# CI/CD Gotchas (GameCI + WebGL + S3/CloudFront)

## GameCI Output Path

GameCI outputs to `build/WebGL/WebGL/`, not `build/WebGL/`:
```yaml
# Correct
aws s3 sync build/WebGL/WebGL/ s3://bucket/...
```

## Brotli, Not Gzip

GameCI compresses to `.br` (Brotli):
```yaml
# Correct
--include "*.wasm.br" --content-encoding br
```

## S3-to-S3 Copy Ignores Cache Headers

`aws s3 sync --cache-control` only works for local→S3, not S3→S3. Upload directly from local build.

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

## CloudFront Default Root Object

Only works at root `/`, not subdirectories. Always use full path:
```
https://domain.cloudfront.net/stable/index.html
```

## SemVer Pre-release Tags

Use dot separator: `v1.0.0-rc.1` not `v1.0.0-rc1`.

## Naming

- `/stable/` for production releases
- `/staging/` for pre-releases
- Never "latest" — ambiguous

## Same Build Type for Staging and Production

Use release builds for both. Testing a dev build doesn't validate what ships.
