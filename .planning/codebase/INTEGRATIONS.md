# External Integrations

**Analysis Date:** 2026-03-22

## APIs & External Services

**Unity Gaming Services (UGS):**
- Authentication Service - Player identity and session management
  - SDK: `Unity.Services.Authentication` (3.3.3)
  - Auth: Anonymous sign-in via `AuthenticationService.Instance.SignInAnonymouslyAsync()`
  - Used in: `Assets/Scripts/Multiplayer/UgsAuthService.cs`

- Cloud Code Service - Server-side validation and scoring logic
  - SDK: `Unity.Services.CloudCode` (2.10.2)
  - Endpoint: `validate-and-post-score` (JavaScript function)
  - Used in: `Assets/Scripts/Multiplayer/UgsLeaderboardService.cs`
  - Parameters: `distance` (numeric, required, 0-115 meters)
  - Response: `{ success: bool, reason: string }`

- Leaderboards Service - Global closest-to-pin leaderboard
  - SDK: `Unity.Services.Leaderboards` (2.1.0)
  - Leaderboard ID: `closest-to-pin` (configured server-side)
  - Score type: Distance in meters (ascending — lowest wins)
  - Max submissions per session: 6 shots
  - Used in: `Assets/Scripts/Multiplayer/UgsLeaderboardService.cs`

## Data Storage

**Cloud Services:**
- **Leaderboards** - UGS Leaderboards API
  - Stores: Player ID, distance, timestamp, rank
  - Sort order: Ascending (closest distance = best)
  - Read via: `LeaderboardsService.Instance.GetScoresAsync(leaderboardId, limit)`
  - Write via: Cloud Code `validate-and-post-score` function

- **Cloud Save** - UGS Cloud Save API
  - Purpose: Track submission count per player per session
  - Key: `submission_count`
  - Used in: Cloud Code validation (enforces 6-shot limit)
  - Client SDK: `@unity-services/cloud-save-1.4` (Cloud Code only)

**Local Persistence:**
- **PlayerPrefs** - Unity's local storage
  - Keys: `nickname` (player display name), `best_score` (best CTP distance)
  - Used in: `Assets/Scripts/Multiplayer/PlayerPrefsBestScoreService.cs`
  - Location: Browser localStorage (WebGL) or system preference storage

**File Storage:**
- Local filesystem only — no external file storage service
- Assets served via Cloudflare Pages CDN

**Caching:**
- Cloudflare Pages edge caching (HTTP cache headers)
- Content-addressed filenames for cache busting on WebGL builds

## Authentication & Identity

**Auth Provider:**
- Unity Gaming Services (UGS) Authentication
  - Implementation: Anonymous sign-in → server-assigned Player ID
  - Session token: Access token renewed via UGS SDK
  - Flow:
    1. `UgsAuthService.SignInAsync()` calls `AuthenticationService.Instance.SignInAnonymouslyAsync()`
    2. Server returns `PlayerId` and `AccessToken`
    3. Token cached in `AuthenticationService.Instance` singleton
  - Display name: Optional, set via `AuthenticationService.Instance.UpdatePlayerNameAsync(nickname)` and persisted in PlayerPrefs
  - Used in: `Assets/Scripts/Multiplayer/UgsAuthService.cs`, `Assets/Scripts/Multiplayer/Bootstrap.cs`

**Fallback (Editor & Offline):**
- `MockAuthService` - Synthetic player with fixed ID when UGS unavailable
  - Used in: Editor mode, offline testing, if UGS init fails

## Monitoring & Observability

**Error Tracking:**
- Not detected — errors logged to console via `Debug.LogError()`, `Debug.LogWarning()`
- Cloud Code errors return `{ success: false, reason: "error message" }` to client

**Logs:**
- **Client:** Unity Console (browser DevTools Console via WebGL)
- **Server:** Cloud Code logs visible in UGS dashboard (function execution logs)
- **CI:** GitLab CI build logs + WebGL build output
- Logging framework: Standard `Debug.Log()` / `Debug.LogError()` throughout codebase

## CI/CD & Deployment

**Hosting:**
- Cloudflare Pages (Primary)
  - Domain: `golf-game-amm.pages.dev` (production)
  - Deployment via `wrangler pages deploy` command
  - Auto-preview URLs per branch/MR

**Build Pipeline:**
- **Local build:** `File > Build Settings > WebGL > Build`
- **CI Pipeline (GitLab):**
  1. **Lint stage:** Checks C# files for tabs (fail if found)
  2. **Cloud Code test stage:** `node --test Assets/CloudCode/validate-and-post-score.test.js`
  3. **WebGL build stage:**
     - Image: `unityci/editor:6000.3.10f1-webgl-3`
     - Executes: `unity-editor -executeMethod WebGLBuildScript.Build`
     - Sets: Gzip compression, content-hashed filenames, decompression fallback
     - Output: `build/WebGL/golf-game/` (artifacts expire in 1 day)
  4. **Deploy stage (manual):**
     - Triggered on `main` branch only
     - Command: `wrangler pages deploy build/WebGL/golf-game/ --project-name=golf-game`
     - Requires: `CLOUDFLARE_API_TOKEN` (secret) set in GitLab CI variables

**Deployment Triggers:**
- Preview: Any push to branch → auto-deployed to preview URL
- Production: Manual trigger on main after webgl-build succeeds

## Environment Configuration

**Required env vars (CI/CD):**
- `UNITY_LICENSE` - Base64-encoded Unity license file (required for headless builds)
- `CLOUDFLARE_API_TOKEN` - Wrangler authentication token for Pages deployment

**Secrets location:**
- GitLab CI: Protected variables in project settings (not in repo)
- Local development: Set via shell `export` or `.env` files (never commit)
- Cloudflare: API token stored in GitLab CI protected variable

**UGS Configuration:**
- Project ID: `9e06cda0-e65a-42bd-b45f-9f58add1bfda` (in `ProjectSettings/ProjectSettings.asset`)
- Organization: `ar-87c9d102-c74f-4720-86c1-d0ea2088888e`
- Services: Initialize via `UnityServices.InitializeAsync()` on Bootstrap
- Fallback to mocks if init fails

## Webhooks & Callbacks

**Incoming:**
- None — game is pull-only (reads leaderboards, submits scores via REST)

**Outgoing:**
- Cloud Code function calls (via UGS REST API)
  - POST to: `cloud-code-service/projects/{projectId}/fn/{functionName}` (SDK-wrapped)
  - Body: `{ distance: number }` (validated by Cloud Code)
  - Response: `{ success: bool, reason: string }`

**WebGL-Specific:**
- No server push — game polls leaderboard on demand
- Service calls are `async/await` (no threads in WebGL)

---

*Integration audit: 2026-03-22*
