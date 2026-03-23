# Phase 1: CI/CD Pipeline - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 01-ci-cd-pipeline
**Areas discussed:** Platform migration, _headers content, Unity license secrets, Deploy trigger

---

## Platform Migration (mid-session scope update)

User revealed during discussion that the project needs to switch from GitLab CI to GitHub Actions with GameCI. This was not in the original requirements but was captured as a key decision.

**User's choice:** Switch to GitHub Actions + GameCI
**Notes:** Affects CI-01 through CI-05 significantly — new workflow file, different secret names, GameCI handles Unity activation

---

## _headers Content

| Option | Description | Selected |
|--------|-------------|----------|
| Cache-Control only | index.html: no-cache. Build/*: immutable, 1 year | ✓ |
| Cache-Control + basic security | Adds X-Frame-Options: SAMEORIGIN and X-Content-Type-Options: nosniff | |
| You decide | Claude picks sensible defaults | |

**User's choice:** Cache-Control only
**Notes:** Simple and correct for a WebGL game. No Content-Encoding (decompressionFallback handles decompression).

---

## Unity License Secrets

| Option | Description | Selected |
|--------|-------------|----------|
| GameCI standard (3 secrets) | UNITY_LICENSE + UNITY_EMAIL + UNITY_PASSWORD | ✓ (implicit via GameCI choice) |
| Offline only (UNITY_LICENSE) | Manual .ulf file, simpler but expires ~30 days | |

**User's choice:** GameCI approach (3 secrets) — resolved by platform choice
**Notes:** GameCI requires all three for online activation. UNITY_LICENSE alone was the old manual approach.

---

## Deploy Trigger

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-deploy on push to main | Every merge to main deploys automatically | ✓ |
| Manual trigger only | workflow_dispatch or manual approval | |
| Auto-build, manual deploy | Build auto, deploy requires approval | |

**User's choice:** Auto-deploy on push to main
**Notes:** Personal project — no need for manual deploy gating.

---

## Claude's Discretion

- Whether to delete `.gitlab-ci.yml` or leave in place
- GameCI action version to pin
- PR preview deploy job
- Exact workflow job structure and names
- Unity build timeout values

## Deferred Ideas

- PR preview deployments to Cloudflare Pages
- Self-hosted GitHub runner
- Unity serial-based license (non-expiring)
