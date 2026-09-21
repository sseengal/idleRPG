# REVIEW — candidates for deletion

> Nothing here is deleted until you confirm. Each entry says **why** it is a candidate, **what still depends on it**,
> and **what happens if we delete it**. Reply with the ids you want removed (e.g. "delete R1") and I will remove the
> file and fix any references in the same pass.

---

## R1 — `Docs/archive/Plan.md` (701 lines) — MVP build journal  ·  status: **awaiting your confirmation**

- **What it is:** the original MVP plan and its verification logs for Steps 0-6 (tooling, spec decisions, phase
  checklist, per-step verification results, MVP balance review, save-file location, mobile settings audit, open risks).
- **Why it is a candidate:** the MVP it describes is finished and its live content moved on. Current status lives in
  `Checklist.md` (master record) and `Roadmap.md` (ordered backlog); the design decisions it locked are now in
  `Architecture.md` (decision log) and the topic docs. It is the only doc that still opens with "Last updated: Step 6".
- **What still points at it:** the root `README.md` links to it as "the MVP build journal"; nothing in code reads it.
- **If we delete it:** we lose the per-step verification logs for the MVP (what was tested on which date) and the
  original locked spec decisions in their first phrasing. `Architecture.md` keeps every decision that still binds,
  but not the historical "we tested X on day Y" record.
- **Recommendation:** **keep for now, delete at release** (it is small, and it is the only historical audit trail of
  the MVP). Move it to `Docs/archive/` either way - done.

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
