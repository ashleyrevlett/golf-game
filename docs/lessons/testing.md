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
