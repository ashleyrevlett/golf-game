---
topic: State Machines
tags: [state-machine, appstate, gamestate, transitions, side-effects]
---

# State Machines

## New enum values trigger all existing switch/if handlers -- audit subscribers
<!-- issue: #97 | pr: #106 -->
- Adding `Paused` to `AppState` caused `GameManager.HandleAppStateChanged` to deactivate (wiping shot progress) because it treated any non-`Playing` state as inactive
- Plan-reviewer caught this before implementation by reading the actual handler code (lines 76-86)
- Fix: add `isActive` guard so `Activate()` only runs on first transition to `Playing`, not on every re-entry
- Rule: when adding a new enum value to a state machine, audit every subscriber's handler for implicit "else" branches that assume the old set of states
- The architect should include a subscriber audit table in the plan for any enum addition
