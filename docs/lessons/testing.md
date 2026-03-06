---
topic: Testing
tags: [testing, test-cleanup, coverage-theater, unity-tests, nunit]
---

# Testing

## Coverage theater patterns to delete on sight
<!-- issue: #68 | pr: #69 -->
- Tests that count enum values (`Enum.GetValues().Length`) break on any addition and catch nothing the compiler doesn't
- "Event exists" tests (subscribe/unsubscribe, assert nothing fired) verify C# syntax, not behavior
- `Assert.DoesNotThrow` as the sole assertion is a red flag -- if the null guard is removed, the test name says nothing about what broke
- Tests that inline-reproduce production formulas (`Mathf.Lerp`, string interpolation) without calling production code test math, not your system
- Tests for Unity API roundtrips (PlayerPrefs get/set, AudioListener.volume assignment) test the engine, not your code

## EditMode tests for private event handlers need explicit callout
<!-- issue: #97 | pr: #106 -->
- When a MonoBehaviour subscribes to events in `Start()` (which doesn't run in EditMode), tests must manually wire the handler
- If the test recreates the handler logic in a lambda, it tests the lambda -- not the production code
- At minimum: add a comment explaining the EditMode limitation and that the lambda mirrors production logic
- Better: use `[InternalsVisibleTo]` or move the test to PlayMode where `Start()` fires naturally
