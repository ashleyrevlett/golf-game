---
status: partial
phase: 01-ci-cd-pipeline
source: [01-VERIFICATION.md]
started: 2026-03-23T03:04:00Z
updated: 2026-03-23T14:30:00Z
---

## Current Test

Test 2 — verify Cache-Control headers on live deploy

## Tests

### 1. End-to-end pipeline run
expected: Configure the 5 GitHub secrets (UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD, CLOUDFLARE_API_TOKEN, CLOUDFLARE_ACCOUNT_ID), push to main, confirm all three jobs (lint, cloud-code-tests, build-and-deploy) pass and the game deploys to the Cloudflare Pages URL
result: passed
notes: "All three jobs green. Deployed to https://golf-game-99x.pages.dev/ via wrangler pages deploy. Required fixes: build path (BUILD_PATH+BUILD_FILE concatenation), Docker root permissions (chown after unity-builder), Cloudflare project recreated as Direct Upload (was Git-connected)."

### 2. Live Cache-Control header inspection
expected: After deploy, index.html returns Cache-Control: no-cache and /Build/* files return Cache-Control: public, max-age=31536000, immutable via browser DevTools Network tab
result: pending
notes: "Deploy is live at https://golf-game-99x.pages.dev/ — needs manual header inspection."

## Summary

total: 2
passed: 1
issues: 0
pending: 1
skipped: 0
blocked: 0

## Gaps

