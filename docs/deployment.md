# Deployment

## Architecture

```
GitHub Actions (GameCI build) → S3 bucket → CloudFront CDN → Users
```

## S3 Bucket

- Public access blocked (CloudFront OAC only)
- Versioning disabled
- Path structure:
  ```
  /stable/              # Latest production release
  /staging/             # Latest pre-release (alpha, rc)
  /v1.0.0/              # Archived stable release
  /v1.0.0-alpha.1/      # Archived pre-release
  ```

## CloudFront

- Origin: S3 bucket with Origin Access Control (OAC)
- Default root object: `index.html` (only works at root `/`, not subdirectories)
- Cache policy: CachingOptimized
- SSL: Default CloudFront certificate
- Always use full path: `https://<dist>.cloudfront.net/stable/index.html`

## Caching Strategy

Three layers:

1. **Hashed filenames** (`webGLNameFilesAsHashes: 1`) — unique filenames per build, no cache collisions
2. **Service Worker** — network-first for `index.html`, cache-first for hashed build assets
3. **CloudFront invalidation** — deploy workflow invalidates `/stable/*` or `/staging/*` after upload

## GitHub Actions Workflow

### Build (GameCI)

```yaml
name: Build and Deploy
on:
  push:
    tags: ['v*']

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
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

      - uses: actions/checkout@v4
        with:
          lfs: true

      - name: Pull LFS and clean
        run: |
          git lfs pull
          rm -f .lfs-assets-id

      - name: Build WebGL
        uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          targetPlatform: WebGL

      - name: Upload to S3
        env:
          AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
          AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        run: |
          VERSION=${GITHUB_REF_NAME}

          # Upload to versioned path
          aws s3 sync build/WebGL/WebGL/ s3://${{ secrets.S3_BUCKET }}/${VERSION}/ \
            --delete --cache-control "max-age=31536000,immutable"

          # Upload compressed files with correct encoding
          aws s3 cp s3://${{ secrets.S3_BUCKET }}/${VERSION}/ s3://${{ secrets.S3_BUCKET }}/${VERSION}/ \
            --recursive --exclude "*" \
            --include "*.br" --content-encoding br --metadata-directive REPLACE

          # Determine alias (stable or staging)
          if [[ "$VERSION" == *"-"* ]]; then
            ALIAS="staging"
          else
            ALIAS="stable"
          fi

          # Copy to alias path
          aws s3 sync build/WebGL/WebGL/ s3://${{ secrets.S3_BUCKET }}/${ALIAS}/ \
            --delete --cache-control "max-age=60"

          aws s3 cp s3://${{ secrets.S3_BUCKET }}/${ALIAS}/ s3://${{ secrets.S3_BUCKET }}/${ALIAS}/ \
            --recursive --exclude "*" \
            --include "*.br" --content-encoding br --metadata-directive REPLACE

      - name: Invalidate CloudFront
        env:
          AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
          AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        run: |
          VERSION=${GITHUB_REF_NAME}
          if [[ "$VERSION" == *"-"* ]]; then
            ALIAS="staging"
          else
            ALIAS="stable"
          fi
          aws cloudfront create-invalidation \
            --distribution-id ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }} \
            --paths "/${ALIAS}/*"
```

## Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `UNITY_LICENSE` | Unity `.ulf` license file contents |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `AWS_ACCESS_KEY_ID` | IAM deploy user access key |
| `AWS_SECRET_ACCESS_KEY` | IAM deploy user secret key |
| `S3_BUCKET` | S3 bucket name |
| `CLOUDFRONT_DISTRIBUTION_ID` | CloudFront distribution ID |

## IAM Policy (Minimum)

The deploy user needs:
- `s3:PutObject`, `s3:DeleteObject`, `s3:ListBucket` on the bucket
- `cloudfront:CreateInvalidation` on the distribution

## Tagging and Releasing

Semver with `alpha` → `rc` → stable progression:

```bash
# Alpha (early testing)
git tag -a v1.0.0-alpha.1 -m "First alpha"
git push --tags

# Release candidate (feature-complete, bug fixes only)
git tag -a v1.0.0-rc.1 -m "Release candidate 1"
git push --tags

# Stable release
git tag -a v1.0.0 -m "Initial release"
git push --tags
```

Tags with a hyphen (`-`) deploy to `/staging/`. Clean semver tags deploy to `/stable/`.

### Version Precedence

```
v1.0.0-alpha.1 < v1.0.0-alpha.2 < v1.0.0-rc.1 < v1.0.0
```

## Rollback

Copy an archived version to `/stable/`, then invalidate:
```bash
aws s3 sync s3://bucket/v0.9.0/ s3://bucket/stable/ --delete
aws cloudfront create-invalidation --distribution-id DIST_ID --paths "/stable/*"
```

Prefer "fail forward" — tag and deploy a fix rather than rolling back.
