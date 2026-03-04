# Add UGS integration for server-authoritative scoring

**Issue**: #20 | **Type**: feature

## What
Replace mock auth/leaderboard services with UGS Authentication, Leaderboards, and Cloud Code. Scores validated server-side before writing to shared leaderboard.

## Requirements
- Migrate `IAuthService` and `ILeaderboardService` from sync to async (`Task<T>`)
- `UgsAuthService`: anonymous sign-in via Unity Authentication SDK
- `UgsLeaderboardService`: reads from UGS Leaderboards, writes via Cloud Code only
- Cloud Code JS script: validates distance [0, 115m], enforces 6-submission cap, writes to leaderboard
- `Bootstrap` registers UGS services in WebGL builds, mocks in editor/offline
- Graceful fallback to mocks if UGS init fails at runtime
- Retry queue for failed score posts; stale cache for failed leaderboard reads
- Add `com.unity.services.authentication`, `com.unity.services.cloud-code`, `com.unity.services.leaderboards` packages

## Acceptance Criteria
- Given WebGL + network, when game starts, then anonymous auth completes with UGS player ID
- Given authenticated player, when best distance updates, then Cloud Code validates and writes score
- Given out-of-range distance or >6 submissions, then Cloud Code rejects
- Given Unity Editor play mode, then mocks register and game works as before
- Given network failure, then game continues; queued scores submit when connectivity returns
- Given UGS init failure, then mocks register with warning logged

## Constraints
- No `Task.Run` or `Thread` — WebGL single-threaded
- All UGS SDKs must be verified for Unity 6.3 LTS + WebGL compatibility
- Score post < 2s p95; leaderboard fetch < 1s p95
- Anonymous auth only (no user accounts for MVP)
