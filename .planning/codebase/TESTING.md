# Testing Patterns

**Analysis Date:** 2026-03-22

## Test Framework

**C# Tests:**
- Runner: Unity Test Framework (UTF) with NUnit
- Framework: NUnit 3.x
- Config: Assembly definitions per test type (`GolfGame.Tests.EditMode.asmdef`, `GolfGame.Tests.PlayMode.asmdef`)

**JavaScript Tests (Cloud Code):**
- Runner: Node.js built-in `node:test` module (Node.js 18+)
- Assertion: `node:assert/strict`
- Test file: `Assets/CloudCode/validate-and-post-score.test.js`

**Run Commands:**

```bash
# Unity Edit Mode Tests (Editor only)
Window > General > Test Runner (in Unity Editor)

# Unity Play Mode Tests (requires Play mode)
Window > General > Test Runner (in Unity Editor)

# Cloud Code JS tests
node --test Assets/CloudCode/validate-and-post-score.test.js

# Manual integration testing
Open project in Unity 6, run Play mode in Gameplay scene
```

## Test File Organization

**Location:**
- C# tests: `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`
- JS tests: `Assets/CloudCode/*.test.js`

**Naming:**
- Pattern: `[ClassName]Tests.cs` (Edit Mode) or `[ClassName]PlayModeTests.cs` (Play Mode)
- Example: `BallControllerTests.cs`, `GameManagerPlayModeTests.cs`, `WindSystemTests.cs`
- JS pattern: `[module-name].test.js` (e.g., `validate-and-post-score.test.js`)

**Directory Structure:**
```
Assets/Tests/
  EditMode/
    BallControllerTests.cs
    WindSystemTests.cs
    GameOverControllerTests.cs
    PlayerPrefsBestScoreServiceTests.cs
    LeaderboardControllerTests.cs
    CameraControllerTests.cs
    YardageMarkerBuilderTests.cs
    GolfGame.Tests.EditMode.asmdef
  PlayMode/
    GameManagerPlayModeTests.cs
    AppManagerPlayModeTests.cs
    BallControllerPlayModeTests.cs
    GolfGame.Tests.PlayMode.asmdef
```

## Test Structure

**Suite Organization:**
```csharp
[TestFixture]
public class BallControllerTests
{
    private GameObject ballObj;
    private BallController ballController;
    private Rigidbody rb;

    [SetUp]
    public void SetUp()
    {
        // Initialize test fixtures before each test
        ballObj = new GameObject("Ball");
        ballController = ballObj.AddComponent<BallController>();
        // ...
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        if (ballObj != null)
            Object.DestroyImmediate(ballObj);
    }

    [Test]
    public void TestName_Scenario_ExpectedResult()
    {
        // Arrange
        var shot = new ShotParameters { PowerNormalized = 1f };

        // Act
        ballController.Launch(shot);

        // Assert
        Assert.IsTrue(ballController.IsFlying);
    }
}
```

**Patterns:**
- Use `[SetUp]` to initialize test GameObjects and components before each test
- Use `[TearDown]` to destroy GameObjects and clean up state
- Test method naming: `MethodName_Scenario_ExpectedResult`
- Arrange-Act-Assert (AAA) structure within test body

## Edit Mode vs. Play Mode Tests

**Edit Mode Tests:**
- Location: `Assets/Tests/EditMode/`
- When: `[Test]` attribute
- Use: Unit tests for component setup, method contracts, state checks
- Limitations: No physics simulation, coroutines don't run, `Awake()` doesn't auto-fire
- Must manually invoke `Awake()` using `SendMessage()`:
  ```csharp
  ballController.SendMessage("Awake");
  ```
- Suppress physics warnings:
  ```csharp
  LogAssert.ignoreFailingMessages = true;
  ```

**Play Mode Tests:**
- Location: `Assets/Tests/PlayMode/`
- When: `[UnityTest]` attribute (returns `IEnumerator`)
- Use: Integration tests, coroutines, state machine transitions, physics
- Requires yielding frames: `yield return null;`
- Can use `WaitForSeconds`:
  ```csharp
  yield return new WaitForSeconds(3.2f);
  ```
- Full GameObject lifecycle runs (`Awake()`, `Start()`, `OnDestroy()`)

**Example decision:**
- BallController launch and velocity: Edit Mode (no physics needed)
- GameManager coroutine-based state resets: Play Mode (WaitForSeconds required)

## Mocking

**Framework:** NUnit/built-in delegates (no external mocking library)

**Pattern in JavaScript tests:**
```javascript
function createMockContext(overrides = {}) {
    return {
        projectId: "test-project",
        playerId: "test-player",
        accessToken: "test-token",
        ...overrides
    };
}

let dataApi = {
    getItems: async () => ({ data: { results: [{ value: 0 }] } }),
    setItem: async () => ({})
};

let leaderboardsApi = {
    addLeaderboardPlayerScore: async () => ({})
};
```

**Pattern in C# tests:**
- Create test fixtures directly: `new GameObject()`, `AddComponent<T>()`
- Use null checks and conditional logic instead of mocking frameworks
- Tests verify behavior with `null` gracefully handled (e.g., optional components):
  ```csharp
  [Test]
  public void Launch_WithNoWindSystem_StillFlies()
  {
      var shot = new ShotParameters { PowerNormalized = 0.8f };
      ballController.Launch(shot);
      Assert.IsTrue(ballController.IsFlying);
  }
  ```

**What to Mock:**
- External service calls (Auth, Leaderboard — tested at integration level)
- Expensive I/O operations (Resources.Load, cloud APIs)
- Time-dependent behavior (coroutines in Play Mode tests)

**What NOT to Mock:**
- Core physics and game logic
- Component interactions (use real GameObjects and `AddComponent<>()`)
- Event-driven state changes

## Fixtures and Factories

**Test Data:**

In C# Edit Mode, create fixtures directly:
```csharp
var shot = new ShotParameters
{
    PowerNormalized = 1f,
    AimAngleDegrees = 0f,
    BackspinRpm = 3000f,
    SidespinRpm = 0f
};

ballController.Launch(shot);
```

In JavaScript tests, use factory functions:
```javascript
function createMockContext(overrides = {}) {
    return {
        projectId: "test-project",
        playerId: "test-player",
        accessToken: "test-token",
        ...overrides
    };
}

const result = await validateAndPostScore(
    { params: { distance: 5.0 }, context: createMockContext(), logger },
    dataApi, leaderboardsApi
);
```

**Location:**
- Inline factories in test files (no shared fixture library)
- Fixtures created in `[SetUp]` methods

## Coverage

**Requirements:** Not enforced; coverage driven by feature completeness

**View Coverage:**
- Manual testing in Editor via Test Runner
- No automated coverage reporting configured

**Focus areas (tested):**
- State machines (GameManager state transitions)
- Critical physics (BallController launch, velocity)
- UI logic (GameOverController, LeaderboardController)
- Service contracts (Auth, Leaderboard interfaces)
- Ball mechanics (distance calculations, landing detection)

## Test Types

**Unit Tests:**
- Scope: Single component or function behavior
- Location: `Assets/Tests/EditMode/`
- Example: `BallControllerTests.Launch_SetsNonZeroVelocity()`
- Verify: Method contracts, state changes, public API contracts

**Integration Tests (Play Mode):**
- Scope: Component interactions, coroutine-based workflows
- Location: `Assets/Tests/PlayMode/`
- Example: `GameManagerPlayModeTests.BallLanded_CoroutineResetsToReady()`
- Verify: State machine transitions, event firing, async behavior

**Cloud Code Tests (JavaScript):**
- Scope: Server-side score validation and submission
- Location: `Assets/CloudCode/validate-and-post-score.test.js`
- Run: `node --test Assets/CloudCode/validate-and-post-score.test.js`
- Verify: Input validation (distance range, max submissions), counter increments

**E2E Tests:**
- Not automated; manual testing in Editor or WebGL builds required
- Process: Open Gameplay.unity, play through complete 6-shot game, verify game over screen

## Common Patterns

**Async Testing in Play Mode:**

Use `[UnityTest]` with `IEnumerator` return type:
```csharp
[UnityTest]
public IEnumerator BallLanded_CoroutineResetsToReady()
{
    yield return null; // let Start run

    gameManager.Activate();
    gameManager.LaunchShot();

    bool resetFired = false;
    gameManager.OnResetToTee += () => resetFired = true;

    gameManager.BallLanded();
    yield return new WaitForSeconds(3.2f);

    Assert.IsTrue(resetFired);
    Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
}
```

**Event Testing:**

Capture event payloads in a test variable:
```csharp
[UnityTest]
public IEnumerator OnShotStateChanged_FiresOnTransitions()
{
    yield return null;

    gameManager.Activate();

    var states = new System.Collections.Generic.List<ShotState>();
    gameManager.OnShotStateChanged += s => states.Add(s);

    gameManager.LaunchShot();
    gameManager.BallLanded();

    Assert.AreEqual(2, states.Count);
    Assert.AreEqual(ShotState.Flying, states[0]);
    Assert.AreEqual(ShotState.Landed, states[1]);
}
```

**Error/Boundary Testing:**

Test both valid and invalid inputs:
```csharp
[Test]
public void Launch_NullParameters_DoesNotThrow()
{
    Assert.DoesNotThrow(() => ballController.Launch(null));
    Assert.IsFalse(ballController.IsFlying);
}

[Test]
public void Launch_WhileAlreadyFlying_DoesNothing()
{
    var shot = new ShotParameters { PowerNormalized = 0.5f };
    ballController.Launch(shot);
    var velocity1 = rb.linearVelocity;

    var shot2 = new ShotParameters { PowerNormalized = 1f };
    ballController.Launch(shot2);
    var velocity2 = rb.linearVelocity;

    Assert.AreEqual(velocity1, velocity2);
}
```

**JavaScript Validation Testing:**

Mock API responses and verify counter behavior:
```javascript
it("increments submission counter on success", async () => {
    let setItemCalled = false;
    let savedValue = null;
    dataApi.setItem = async (projectId, playerId, item) => {
        setItemCalled = true;
        savedValue = item.value;
    };

    await validateAndPostScore(
        { params: { distance: 5.0 }, context: createMockContext(), logger },
        dataApi, leaderboardsApi
    );

    assert.equal(setItemCalled, true);
    assert.equal(savedValue, 1); // 0 + 1
});

it("handles missing submission counter gracefully", async () => {
    dataApi.getItems = async () => { throw new Error("Not found"); };

    let savedValue = null;
    dataApi.setItem = async (projectId, playerId, item) => {
        savedValue = item.value;
    };

    const result = await validateAndPostScore(
        { params: { distance: 5.0 }, context: createMockContext(), logger },
        dataApi, leaderboardsApi
    );

    assert.equal(result.success, true);
    assert.equal(savedValue, 1); // starts at 0, increments to 1
});
```

## Pre-Test Checklist

- [ ] Test runs in Edit Mode or Play Mode (appropriate for scenario)
- [ ] `[SetUp]` creates fresh GameObjects for each test
- [ ] `[TearDown]` cleans up with `Object.DestroyImmediate()` (Edit Mode) or `Object.Destroy()` (Play Mode)
- [ ] Async tests use `[UnityTest]` with `IEnumerator` and appropriate `yield return`
- [ ] Event tests capture payloads in collections
- [ ] Error cases tested alongside happy path
- [ ] No `LogAssert.ignoreFailingMessages` left enabled after test completes
- [ ] JS tests use `node:test` and `node:assert/strict`
- [ ] JS tests create fresh mocks in `beforeEach()`

---

*Testing analysis: 2026-03-22*
