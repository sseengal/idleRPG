# REVIEW — candidates for deletion

> Nothing here is deleted until you confirm. Each entry says **why** it is a candidate, **what still depends on it**,
> and **what happens if we delete it**. Reply with the ids you want removed (e.g. "delete R1") and I will remove the
> file and fix any references in the same pass.

---

## R1 — `Docs/archive/Plan.md` (711 lines) — MVP build journal  ·  status: **DELETED 2026-09-22**

- Deleted on the owner's call ("check and update/remove obsolete docs"). The MVP it described is finished; its live
  content moved into `Checklist.md` (status), `Roadmap.md` (backlog) and `Architecture.md` (decision log), and it was
  the only doc still opening with "Last updated: Step 6".
- What was lost: the per-step verification logs for Steps 0-6 (what was tested on which date) and the original locked
  spec decisions in their first phrasing. `Architecture.md` keeps every decision that still binds.
- Every reference was removed in the same pass: root `README.md`, `Docs/README.md`, `Docs/Checklist.md`.
- If the history is ever wanted, `git log -- Docs/archive/Plan.md` still has it.

---

## Cleared (checked, and staying)

| Doc | Checked | Verdict |
|---|---|---|
| `Architecture.md` | 2026-09-21 (Step 11e) | current - decision log extended (B28-B33) |
| `Checklist.md` | 2026-09-21 (Step 11e) | current - master record, includes the standing regression surfaces |
| `Roadmap.md` | 2026-09-21 (Step 11e) | current - 11a/11b/11c/11e done, Step 12 next |
| `Sim-Core.md` | 2026-09-21 (Step 11e) | current - ranks removed, wave size derivation documented |
| `Content.md` | 2026-09-21 (Step 11c) | current - budget split + recipe described |
| `UI-UX.md` | 2026-09-21 (Step 11e) | current - battle-log contract added |
| `Idle-Economy.md` | 2026-09-21 (Step 11c refresh) | current - offline payout uses the wave-size mean |
| `Progression.md` | 2026-09-20 | current - untouched by Steps 10/11 (no progression changes) |
| `README.md` (root) | 2026-09-21 | current - quick start, now points into `Docs/` |
