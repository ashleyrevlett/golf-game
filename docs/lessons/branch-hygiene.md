---
topic: Branch Hygiene
tags: [git, worktree, branches, rebase, pr-diff, cherry-pick]
---

# Branch Hygiene

## Worktree branches carry stale commits after squash-merge
<!-- issue: #103 | pr: #120 -->
- When branch B is created from a worktree that includes unmerged branch A, B carries A's commits
- After A is squash-merged to main, B's diff against main still shows A's original (pre-squash) changes
- Before opening a PR, run `git diff --stat origin/main...HEAD` and verify every file belongs to the current issue
- If stale commits exist, rebase onto `origin/main` to drop them: `git rebase origin/main`
- This caused an extra review cycle on PR #120 -- reviewer flagged 9 of 12 files as unrelated
