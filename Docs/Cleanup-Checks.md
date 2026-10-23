# Cleanup checks — prove nothing changed

Use this page for **consolidation only**: moving files, merging duplicates, fixing docs, renaming, removing
dead assets. A cleanup must not change what the player sees, what a save contains, or what a fight produces.

If a step cannot pass this gate, it is not a cleanup. Stop and treat it as a feature change.

---

## 1. The gate — run it before and after every cleanup step

Run all four in the same order, before the edit and after it.

| # | Check | Where | Pass condition |
|---|---|---|---|
| 1 | Scripts compile | Unity Console | zero compile errors |
| 2 | Full regression net | `Tools > Idle RPG > Run All Checks (regression)` | log ends with `[Regression] REGRESSION: PASS` |
| 3 | EditMode tests (12 tests) | `Window > General > Test Runner > EditMode > Run All` | 12 passed, 0 failed |
| 4 | Player loop smoke | `Docs/Manual-Tests.md` §1 plus §2 of this page | every item matches the "before" notes |

Check 2 already covers save drift, content validation, golden numbers, the UI type scale and the UI spacing ladder.
Check 3 covers drop routing, the formulas and the save round trip.

**The golden numbers are frozen until the MVP baseline pass** (`Content.md` §10). A cleanup must leave them exactly
where they are — same seconds, kills, gold, rate. If a tidy-up moves one, the tidy-up changed behaviour, so revert it
instead of re-recording the baseline. A golden-number FAIL during *feature* work is a different case: record the new
numbers and carry on, without editing `BalanceBaseline.cs`.

### Known drift at 24 Oct 2026 — not caused by a cleanup

`Run All Checks (regression)` currently ends **FAIL** on four golden numbers:

| Check | Today | Baseline (2026-09-25) |
|---|---|---|
| stage 1 seconds @x1.0 | 201.7 | 80 |
| stage 1 seconds @x1.6 | 183.4 | 126 |
| stage 1 gold | 128 | 272 |
| stage 1 gold/s @x1.6 | 0.698 | 2.16 |

The same numbers were logged on **23 Oct 2026, before the cleanup pass**, and they match the content validator's own
stage-1 line (`183s, 22 kills, 128 gold, 0.70 gold/s`). Cause: the ability and enemy-cast work changed fight length
after the 2026-09-25 baseline. **Do not re-baseline for a cleanup.** Compare a new run against the numbers above;
they are the real "before" until the MVP baseline pass.

Record the result of each check in the commit message. Example:
`Docs: one page per topic (checks: PASS, 12/12 EditMode, smoke same)`.

---

## 2. The player loop smoke (5 minutes)

This is the smallest path that touches every layer: sim, bridge, presentation, HUD, save.

1. Enter Play Mode in `Assets/IdleRPG/Scenes/Main.unity`.
2. A wave starts by itself. Heroes and enemies trade blows, one at a time.
3. A damage number appears on the unit that was hit, on the frame the picture reaches full reach.
4. The battle log shows one line per action, with the real attacker's name.
5. Press one ability button. It shows QUEUED, then the unit plays its strip, then the effect lands.
6. Open each management tab. Each one shows its own page and hides the others.
7. Let one stage finish. Gold rises, the next wave starts, nothing is stuck.

If any of these differ from the "before" notes, the step changed behaviour. Revert it.

---

## 3. Traps that break "no behaviour change"

| Trap | Why it bites | What to do |
|---|---|---|
| Rebuilding the scene (`Tools > Idle RPG > Build MVP Scene`) | It rewrites `Main.unity` and produces a diff of tens of thousands of lines, so a real change can hide in it | Do not rebuild during cleanup. Edit the scene narrowly, or leave it alone |
| Regenerating data (`DataAssetGenerator`) | It overwrites hand-tuned enemy stats and wave entries | Never run it as part of a cleanup |
| Moving anything loaded by name | `Resources.Load` and string paths break silently when a file moves | Search for the name before the move; move with Unity, not the file system, so GUIDs survive |
| Editing a `.prefab`, `.asset` or `.unity` file in a text editor | Unity re-serializes it and adds unrelated churn | Use the Editor or an editor script |
| Renaming a serialized field | The value resets to default and nothing fails loudly | Do not rename serialized fields in a cleanup |
| Deleting a folder that "looks empty" | It may still hold a `.meta` file that something references | Check references first, then delete inside Unity |
| Touching a generated file | The next generation pass undoes the tidy-up | Change the generator or the spec instead |

---

## 4. Scope rule

One cleanup item per commit. One item means one subject: for example "the docs index" or "one duplicated helper".

Do not fold a balance tweak, a new ability, an art swap or a store setting into a cleanup commit. Those are
separate work with their own checks.

---

## 5. Where the known candidates live

- `Docs/REVIEW.md` — deletion candidates and resolved removals.
- `Docs/Manual-Tests.md` — the full manual runbook and the device passes.
- `Docs/README.md` — which page is current for each topic.
