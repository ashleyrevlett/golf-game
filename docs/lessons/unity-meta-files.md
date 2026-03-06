---
topic: Unity .meta files
tags: [unity, meta-files, assets, pre-commit]
---

# Unity .meta Files

## Missing .meta files slip past implementer despite documentation
<!-- issue: #20 | pr: #21 -->
- Every new file/directory under `Assets/` needs a `.meta` file committed alongside it
- The pre-commit checklist in CLAUDE.md documents this, but documentation alone does not prevent the mistake
- Code-reviewer caught 6 missing `.meta` files (3 scripts, 2 JS files, 1 directory) requiring a fix cycle
- Enforcement gap: the implementer has no automated check -- relies on reviewer to catch it
- If this recurs, add an explicit `git ls-files Assets/ --others --exclude-standard` check to the implementer's post-push verification

## Worktree builds cannot generate .meta files without Unity MCP access
<!-- issue: #97 | pr: #106 -->
- Recurrence confirmed: 3 missing .meta files caught by code-reviewer, same pattern as #20
- Root cause: implementer ran in git worktree where Unity MCP was not connected, so `refresh_unity` could not generate .meta files
- Manual .meta creation in a fix commit works but adds a review cycle
- The CLAUDE.md documentation and lesson from #20 did not prevent it -- automation is needed
- Action: add a pre-push check to the implementer workflow that runs `git ls-files Assets/ --others --exclude-standard` and fails if untracked files exist
