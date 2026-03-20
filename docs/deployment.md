# Deployment

## Architecture

```
Local Unity build (Mac mini) → wrangler pages deploy → Cloudflare Pages → Users
```

## Cloudflare Pages

- Project name: `golf-game`
- Production URL: `https://golf-game-amm.pages.dev`
- Production branch: `production`
- Brotli-compressed Unity WebGL assets

## Build & Deploy (Manual)

```bash
# 1. Build WebGL
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath ~/Documents/apps/golf-game \
  -executeMethod WebGLBuildScript.Build \
  -logFile /tmp/unity-build.log

# 2. Deploy to Cloudflare Pages
export CLOUDFLARE_API_TOKEN=$(python3 -c "import json; print(json.load(open('$HOME/.config/cloudflare/credentials.json'))['api_key'])")
export CLOUDFLARE_ACCOUNT_ID=$(python3 -c "import json; print(json.load(open('$HOME/.config/cloudflare/credentials.json'))['account_id'])")

npx wrangler pages deploy build/WebGL/golf-game/ \
  --project-name=golf-game \
  --branch=production \
  --commit-dirty=true
```

## Future: GitLab CI Deploy

TODO: Set up self-hosted GitLab runner on Mac mini with Unity installed for automated tag-based deploys.

## Versioning

- Tag on main after merge: `git tag -a v1.1.0 -m "summary" && git push origin v1.1.0`
- Deploy after tagging (currently manual, future: CI triggered by tags)
