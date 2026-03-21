## 🏗️ Implementation Plan

> **Agent:** `software-architect`

**Approach**: Introduce thin internal wrapper interfaces (`IUgsAuthProvider`, `IUgsCloudCodeProvider`, `IUgsLeaderboardsProvider`) that expose only the SDK surface the services actually use, then inject them via constructor into `UgsAuthService` and `UgsLeaderboardService`. Tests use hand-rolled stubs of these thin interfaces. This avoids stubbing the massive UGS SDK interfaces (which have dozens of unused members) while still mocking at the SDK boundary, not the service interface.

**Key decisions**:
- **Thin wrappers over direct SDK interface injection**: The UGS SDK interfaces (`IAuthenticationService`, `ICloudCodeService`, `ILeaderboardsService`) each have 20+ members. Hand-rolling stubs for them would be 90% `NotImplementedException` boilerplate. Thin wrappers expose only the 3-5 members we use, keeping stubs tiny and stable against SDK updates.
- **Internal DTO `RawLeaderboardScore` instead of SDK response types**: UGS SDK response types (`LeaderboardScoresPage`, SDK `LeaderboardEntry`) may have internal constructors, making them hard to construct in tests. Our own struct avoids this and keeps the test assembly decoupled from SDK internals.
- **No UGS SDK references needed in test asmdef**: Because thin wrappers use only primitives and our own types, the test assembly doesn't need direct UGS SDK assembly references. This deviates from the exploration addendum's AC but is a consequence of the cleaner wrapper approach.
- **All wrapper types in one file**: These are small, tightly-coupled internal types (3 interfaces, 3 implementations, 1 DTO). Grouping avoids file bloat. The "one MonoBehaviour per file" convention doesn't apply to plain interfaces/structs.

### Files to modify
- `Assets/Scripts/Multiplayer/UgsAuthService.cs` -- add constructor taking `IUgsAuthProvider`, replace `AuthenticationService.Instance` calls with provider
- `Assets/Scripts/Multiplayer/UgsLeaderboardService.cs` -- add constructor taking `IUgsCloudCodeProvider` + `IUgsLeaderboardsProvider`, replace static singleton calls with providers
- `Assets/Scripts/Multiplayer/Bootstrap.cs` -- pass `DefaultUgsXxxProvider` instances to constructors in the WebGL registration path

### Files to create
- `Assets/Scripts/Multiplayer/UgsSdkProviders.cs` -- thin interfaces, default implementations, `RawLeaderboardScore` DTO
- `Assets/Tests/EditMode/UgsAuthServiceTests.cs` -- tests for auth service logic with stub provider
- `Assets/Tests/EditMode/UgsLeaderboardServiceTests.cs` -- tests for leaderboard service logic with stub providers

### Steps

**1. Create thin SDK provider interfaces and default implementations**
- File: `Assets/Scripts/Multiplayer/UgsSdkProviders.cs`
- Define `RawLeaderboardScore` internal struct with fields: `Rank` (int, 0-based), `PlayerId` (string), `PlayerName` (string, nullable), `Score` (double)
- Define `IUgsAuthProvider` interface:
  ```csharp
  internal interface IUgsAuthProvider
  {
      bool IsSignedIn { get; }
      string PlayerId { get; }
      string AccessToken { get; }
      Task SignInAnonymouslyAsync();
  }
  ```
- Define `IUgsCloudCodeProvider` interface:
  ```csharp
  internal interface IUgsCloudCodeProvider
  {
      Task<T> CallEndpointAsync<T>(string function, Dictionary<string, object> args);
  }
  ```
- Define `IUgsLeaderboardsProvider` interface:
  ```csharp
  internal interface IUgsLeaderboardsProvider
  {
      Task<List<RawLeaderboardScore>> GetScoresAsync(string leaderboardId, int limit);
      Task<RawLeaderboardScore> GetPlayerScoreAsync(string leaderboardId);
  }
  ```
- Implement `DefaultUgsAuthProvider` wrapping `AuthenticationService.Instance` -- delegates `IsSignedIn`, `PlayerId`, `AccessToken`, `SignInAnonymouslyAsync()` directly
- Implement `DefaultUgsCloudCodeProvider` wrapping `CloudCodeService.Instance` -- delegates `CallEndpointAsync<T>()`
- Implement `DefaultUgsLeaderboardsProvider` wrapping `LeaderboardsService.Instance` -- converts SDK `LeaderboardScoresPage.Results` entries to `RawLeaderboardScore` in `GetScoresAsync`, and converts single SDK `LeaderboardEntry` to `RawLeaderboardScore` in `GetPlayerScoreAsync`. Uses `new GetScoresOptions { Limit = limit }` internally.

**2. Refactor UgsAuthService to use constructor injection**
- File: `Assets/Scripts/Multiplayer/UgsAuthService.cs`
- Add `private readonly IUgsAuthProvider _auth;` field
- Add constructor: `public UgsAuthService(IUgsAuthProvider auth)`
- Replace all `AuthenticationService.Instance.X` calls with `_auth.X`:
  - `IsSignedIn` property -> `_auth.IsSignedIn`
  - `PlayerId` property -> `_auth.PlayerId`
  - `AccessToken` usage -> `_auth.AccessToken`
  - `SignInAnonymouslyAsync()` -> `_auth.SignInAnonymouslyAsync()`
- Remove `using Unity.Services.Authentication;` -- no longer needed
- No changes to public method signatures or behavior

**3. Refactor UgsLeaderboardService to use constructor injection**
- File: `Assets/Scripts/Multiplayer/UgsLeaderboardService.cs`
- Add fields: `private readonly IUgsCloudCodeProvider _cloudCode;` and `private readonly IUgsLeaderboardsProvider _leaderboards;`
- Add constructor: `public UgsLeaderboardService(IUgsCloudCodeProvider cloudCode, IUgsLeaderboardsProvider leaderboards)`
- In `PostScoreAsync`: replace `CloudCodeService.Instance.CallEndpointAsync<ScorePostResult>(...)` with `_cloudCode.CallEndpointAsync<ScorePostResult>(...)`
- In `GetLeaderboardAsync`: replace `LeaderboardsService.Instance.GetScoresAsync(...)` with `_leaderboards.GetScoresAsync(LeaderboardId, count)`. Change iteration from `response.Results` (SDK type) to iterating the returned `List<RawLeaderboardScore>`. The mapping logic (rank+1, null PlayerName fallback, Score->Distance cast) stays in this method unchanged.
- In `GetPlayerRankAsync`: replace `LeaderboardsService.Instance.GetPlayerScoreAsync(...)` with `_leaderboards.GetPlayerScoreAsync(LeaderboardId)`. Use `entry.Rank` from `RawLeaderboardScore`.
- Remove `using Unity.Services.CloudCode;` and `using Unity.Services.Leaderboards;` -- no longer needed since the service uses our interfaces, not SDK types directly

**4. Update Bootstrap to pass providers**
- File: `Assets/Scripts/Multiplayer/Bootstrap.cs`
- In the `#if UNITY_WEBGL && !UNITY_EDITOR` block, change `UgsAuthService` and `UgsLeaderboardService` construction to pass default providers:
  ```csharp
  var authProvider = new DefaultUgsAuthProvider();
  var authService = new UgsAuthService(authProvider);
  await authService.SignInAsync();
  ServiceLocator.Register<IAuthService>(authService);
  ServiceLocator.Register<ILeaderboardService>(
      new UgsLeaderboardService(
          new DefaultUgsCloudCodeProvider(),
          new DefaultUgsLeaderboardsProvider()));
  ```
- No changes to the mock registration path (editor/non-WebGL)
- No behavior change at runtime

**5. Write UgsAuthService tests**
- File: `Assets/Tests/EditMode/UgsAuthServiceTests.cs`
- Define `StubUgsAuthProvider : IUgsAuthProvider` within the test file:
  - `IsSignedIn` { get; set; } -- configurable per test
  - `PlayerId` { get; set; } = `"test-player-id-123456"` (>= 6 chars for `[..6]` slice)
  - `AccessToken` { get; set; } = `"test-access-token"`
  - `SignInCallCount` int -- tracks invocations
  - `ExceptionToThrow` Exception -- if set, `SignInAnonymouslyAsync` throws it
  - `SignInAnonymouslyAsync()` -- increments count, throws if configured, sets `IsSignedIn = true`
- Test class: `UgsAuthServiceTests`
- SetUp: `LogAssert.ignoreFailingMessages = true` (suppress edit-mode physics warnings)
- TearDown: `LogAssert.ignoreFailingMessages = false`
- Test cases listed in Testing section below

**6. Write UgsLeaderboardService tests**
- File: `Assets/Tests/EditMode/UgsLeaderboardServiceTests.cs`
- Define `StubCloudCodeProvider : IUgsCloudCodeProvider` within the test file:
  - `Result` (object) -- returned cast to `T`
  - `ExceptionToThrow` -- if set, throws
  - `LastFunction` / `LastArgs` -- captures call arguments for assertions
- Define `StubLeaderboardsProvider : IUgsLeaderboardsProvider` within the test file:
  - `Scores` (`List<RawLeaderboardScore>`) -- returned by `GetScoresAsync`
  - `PlayerScore` (`RawLeaderboardScore`) -- returned by `GetPlayerScoreAsync`
  - `ExceptionToThrow` -- if set, throws
- Test class: `UgsLeaderboardServiceTests`
- SetUp: `LogAssert.ignoreFailingMessages = true`
- TearDown: `LogAssert.ignoreFailingMessages = false`
- Test cases listed in Testing section below

### Testing

**Test strategy**: EditMode unit tests. No live UGS connection. Stubs implement our thin internal interfaces (visible via `InternalsVisibleTo` already set in `AssemblyInfo.cs`).

**Test files**:
- `Assets/Tests/EditMode/UgsAuthServiceTests.cs` -- auth service logic with injected stub
- `Assets/Tests/EditMode/UgsLeaderboardServiceTests.cs` -- leaderboard service logic with injected stubs

**Test cases**:

`UgsAuthServiceTests`:
- `SignInAsync_AlreadySignedIn_SkipsSignIn`: Set stub `IsSignedIn = true`. Call `SignInAsync`. Assert `stub.SignInCallCount == 0`.
- `SignInAsync_NotSignedIn_CallsProvider`: Set stub `IsSignedIn = false`. Call `SignInAsync`. Assert `stub.SignInCallCount == 1`.
- `GetPlayerTokenAsync_NotSignedIn_SignsInThenReturnsToken`: Set stub `IsSignedIn = false`, `AccessToken = "tok"`. Call `GetPlayerTokenAsync`. Assert result is `"tok"` and `stub.SignInCallCount == 1`.
- `GetPlayerTokenAsync_AlreadySignedIn_ReturnsTokenWithoutSignIn`: Set stub `IsSignedIn = true`, `AccessToken = "tok"`. Assert result is `"tok"` and `stub.SignInCallCount == 0`.
- `GetPlayerInfoAsync_MapsFields`: Set stub `PlayerId = "abcdef123"`, `AccessToken = "tok"`. Call `GetPlayerInfoAsync`. Assert `info.PlayerId == "abcdef123"`, `info.DisplayName == "Player_abcdef"`, `info.Token == "tok"`.
- `GetPlayerTokenAsync_ProviderThrows_RethrowsAndLogs`: Set `ExceptionToThrow = new Exception("boom")`, `IsSignedIn = false`. Assert `GetPlayerTokenAsync` throws with message containing `"boom"`. Use `LogAssert.Expect(LogType.Error, ...)` to verify error log.

`UgsLeaderboardServiceTests`:
- `PostScoreAsync_Success_NoThrow`: Stub returns `ScorePostResult { success = true }`. Call `PostScoreAsync("p", 5.0f)`. Assert no exception. Verify `stub.LastArgs["distance"]` is `5.0f`.
- `PostScoreAsync_Rejected_ThrowsWithReason`: Stub returns `ScorePostResult { success = false, reason = "too far" }`. Assert throws `InvalidOperationException` containing `"too far"`.
- `GetLeaderboardAsync_MapsRankToOneBased`: Stub with scores at ranks 0, 1, 2. Assert output ranks are 1, 2, 3.
- `GetLeaderboardAsync_NullPlayerName_UsesFallback`: Stub with `PlayerName = null`, `PlayerId = "abcdef789"`. Assert `DisplayName == "Player_abcdef"`.
- `GetLeaderboardAsync_NonNullPlayerName_UsesIt`: Stub with `PlayerName = "Eagle_Pro"`. Assert `DisplayName == "Eagle_Pro"`.
- `GetLeaderboardAsync_MapsScoreToDistance`: Stub with `Score = 3.14`. Assert `Distance == 3.14f`.
- `GetPlayerRankAsync_Success_ReturnsOneBased`: Stub with `PlayerScore.Rank = 4`. Assert returns `5`.
- `GetPlayerRankAsync_Exception_ReturnsNegativeOne`: Stub throws `Exception`. Assert returns `-1`.

**Existing test helpers to reuse**: `Bootstrap.ResetForTesting()`, `ServiceLocator.Clear()` for teardown. Pattern: `.GetAwaiter().GetResult()` for sync test execution of async methods (matches existing `MultiplayerTests.cs` convention).

**Manual verification**: After implementation, run all EditMode tests via Unity Test Runner to confirm the new tests pass and existing tests (especially `BootstrapTests` and `MultiplayerTests`) remain green.

### Risks
- **UGS SDK response type shape in `DefaultUgsLeaderboardsProvider`**: The `GetScoresAsync` return from the SDK uses `response.Results[i].Rank`, `.PlayerId`, `.PlayerName`, `.Score`. Implementer must verify these property names match the installed UGS Leaderboards package version. Mitigation: check the SDK source or autocomplete in the IDE before implementing.
- **`ScorePostResult` generic cast in stub**: `CallEndpointAsync<T>` returns `Task.FromResult((T)configuredResult)` where `Result` is `object`. This requires boxing for the struct `ScorePostResult`. Works correctly but implementer should verify with a quick manual test.

### Insights
- UGS SDK interfaces are too large to stub directly for unit tests -- thin wrappers exposing only the used surface are the pragmatic choice for hand-rolled stubs in codebases that don't use mocking libraries.
- `AssemblyInfo.cs` already has `InternalsVisibleTo("GolfGame.Tests.EditMode")`, enabling internal interfaces and DTOs to be used in tests without any asmdef changes.

Skip Agents: visual-designer
