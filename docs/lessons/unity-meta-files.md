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
