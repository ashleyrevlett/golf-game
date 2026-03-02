# Performance Guidelines

## Target: Mobile WebGL (Safari + Chrome)

## Budgets

| Metric | Target | How to Check |
|--------|--------|--------------|
| Triangles (visible) | ≤ 300k | Game view → Stats |
| Draw calls (batches) | ≤ 50 | Game view → Stats |
| Memory (release build) | ≤ 450 MB | iOS Safari safe zone: 300–500 MB |

### Asset Budgets

| Asset Type | Triangle Budget |
|------------|-----------------|
| Hero asset (1–2 per scene) | 5k–15k |
| Medium props | 500–2k |
| Background/distant objects | < 500 |
| Ball | < 500 |

## Rules

1. **Draw calls > polycount** on mobile WebGL — fewer materials = fewer draw calls
2. **Static batch** all environment objects
3. **Shared palette material** — multiple objects sharing one material enables static batching and GPU instancing
4. **No alpha in textures** — transparency causes sorting overhead
5. **Texture atlasing** to reduce material count
6. **LOD for distant objects** if needed

## Memory

- iOS Safari crashes above ~500 MB
- Single scene = no runtime scene loading = no memory spikes
- Disable (not just hide) inactive UI panels
- Test on actual iOS device before shipping

## WebGL-Specific

- Use release builds for both staging and production (same behavior)
- Hashed filenames (`webGLNameFilesAsHashes: 1`) for cache-safe deployments
- Brotli compression (`.br`) is the default with GameCI
