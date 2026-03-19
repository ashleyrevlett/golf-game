## Implementation Plan

> **Agent:** `software-architect`

**Approach**: Add a `cloud-code-test` job to the `test` stage in `.gitlab-ci.yml` that runs `node --test` against the existing Cloud Code test file. Uses the `node:20` image (already used by the `deploy` job). No new dependencies or scripts needed — the test file uses only Node built-ins.

**Key decisions**:
- Separate job, not bolted onto `lint` — the lint job uses `dotnet/sdk:8.0` which has no Node.js
- `node:20` matches the deploy job's image, avoiding version drift
- `only:` triggers match the existing `lint` job for consistency

### Files to modify
- `.gitlab-ci.yml` — add `cloud-code-test` job to `test` stage

### Steps

**1. Add `cloud-code-test` job to `.gitlab-ci.yml`**
- File: `.gitlab-ci.yml`
- Add after the `lint` job, before `deploy`
- Job definition:
  ```yaml
  cloud-code-test:
    stage: test
    image: node:20
    script:
      - node --test Assets/CloudCode/validate-and-post-score.test.js
    only:
      - main
      - merge_requests
  ```
- This runs in parallel with `lint` (same stage, different job)
- No `npm install` needed — test uses only `node:test` and `node:assert/strict` (Node built-ins)

### Testing

**Test strategy**: CI pipeline verification — this is a CI config change, so the test IS the pipeline run.

**Automated verification**:
- The job itself runs all 7 existing tests in `validate-and-post-score.test.js`
- Pipeline fails if any test fails (Node `--test` exits non-zero on failure)
- Job runs on MRs and main pushes (matching `lint` job triggers)

**Manual verification**:
- Push to the `feature/4` branch or open an MR to trigger the pipeline
- Verify `cloud-code-test` job appears in the pipeline
- Verify it runs in parallel with `lint` (both in `test` stage)
- Verify all 7 tests pass in the job log

**Local verification** (already confirmed):
- `node --test Assets/CloudCode/validate-and-post-score.test.js` — 7/7 pass, no external dependencies

### Risks
- None significant. The `node:20` image is already used in the pipeline. The test file has zero external dependencies. Worst case: if Node's built-in test runner API changes in a future major version, but we're pinned to `node:20`.

### Insights
- Cloud Code tests are pure Node.js with no npm dependencies — they mock UGS server APIs inline. This pattern keeps CI fast (no install step) and is worth preserving for future Cloud Code scripts.

Skip Agents: visual-designer, doc-writer
