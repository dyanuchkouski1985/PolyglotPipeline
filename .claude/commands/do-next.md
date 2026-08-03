---
description: Implement the next unchecked task from Plan.md, one step at a time
---

Read `CLAUDE.md` and `Plan.md` at the repo root.

1. Scan `Plan.md` phase by phase, top to bottom, and find the **first** unchecked task
   (a line starting with `- [ ]`).
   - If every task is checked, report that Plan.md is fully implemented and stop — do not invent
     new work.
2. Implement **only that one task** — nothing earlier (already done) and nothing later (not yet
   its turn), even if it would be convenient to combine steps. Follow the architecture, stack, and
   ground rules described in `CLAUDE.md` (no auth, GET-only endpoints, latest stable library/image
   versions, etc.).
3. If the task has a corresponding **Verify** note in its phase, do what's programmatically
   checkable yourself (e.g. `curl` the endpoint, inspect a container's data via its CLI) to confirm
   the task works. Anything that genuinely requires a browser is left for the user.
4. Check the box for the completed task in `Plan.md` (` - [ ]` → ` - [x]`). If this was the last
   task in its phase, leave the phase's Verify note as-is — that's the user's manual confirmation
   step, not something to check off.
5. If completing the task changes what belongs in CLAUDE.md's "Commands" section (or any other part
   of CLAUDE.md that's gone stale), update it.
6. Do **not** run `git add` or `git commit` — the user reviews and commits each step themselves.
7. Stop after this one task. Do not continue on to the next task automatically, even if it looks
   trivial.
8. Before finishing, re-check `Plan.md`: confirm the task you implemented is actually marked
   `- [x]`. If step 4 was skipped or missed for any reason, go back and update `Plan.md` now —
   never end the command with the implemented task still shown as unchecked.

End with a short summary: which task you implemented, what files changed, and how the user can
verify it themselves (including any manual/browser check called out in Plan.md). Finish with a
suggested commit message in a code block — short, imperative, matching this repo's existing commit
style (check `git log` if unsure) — covering just this task's changes. Do not run `git commit`
yourself; the message is only a suggestion for the user to use when they commit.
