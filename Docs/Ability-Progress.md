# Abilities - progress and open items

Living status page. The contract (what the feature must do) stays in `Ability-Plan.md`; this page says what
actually works today, what was measured, and what is still open. **Fight order, strip choice, timings and the
animation audit now live on one page: `Combat-Sequencing.md`.**

**Last checked against the code: 24 Oct 2026.** That check removed three stale "open" items: the `TabController`
panel bug, the `UiSpace.cs` tracking question and the Goblin asset. Details are under "Resolved in the 24 Oct check".

## Where the feature stands

**Next work: hero ability polish** (guard timing, Holy Slash, Shatter, on-target impact
effects). Boss moves and the unused Summon/Heal/Block strips come after. Art is **not** next.

| Layer | State |
|---|---|
| Art strips (one special per character) | done, validated |
| Ability definitions + 2 abilities per hero + save v7 | done |
| Buy / unlock (stage gate) / assign | done |
| Roster swap Archer to Soldier, save v8 | done |
| Cast: request, event, log line, cooldown, ABILITIES tab, F4 debug | done, verified |
| **Cast sequencing in the action lane** (queued press, target, travel window, impact) | **done 24 Oct 2026** - see `Combat-Sequencing.md` §3 and §7 |
| Effect (shield / heal / damage) | **done 24 Oct 2026** - shield is a party cover stance: 2 intercepted hits, half damage; see `Combat-Sequencing.md` §7 |
| Enemy casts (auto on their own cooldown) | **done 24 Oct 2026** |
| **Enemy cast timing is random, not a metronome** ("Wild Cards") | **done 24 Oct 2026** - first roll waits 2-6 s, then 35% chance per 1-3 s roll |
| **Enemy moves hit harder** ("Heavier Hands") | **done 24 Oct 2026** - damage moves x1.8-1.9 of a plain hit (was x1.3-1.4) |
| **Enemy move is announced loudly** ("Herald") | **done 24 Oct 2026** - orange "`<enemy> unleashes <move>!`" log line, orange name over the caster, 1.5x damage number |

## Measured, not assumed

- **Cooldown runs on the fight's clock.** Press CAST, advance the fight by 0.8 seconds, read 13.2 seconds left
  of 14. The clock pauses when the game pauses, because it belongs to the sim.
- **The ABILITIES panel fits its container.** Column content spans y -228..244 and x -511..511; the container
  spans y -262..262 and x -526..526. Nothing spills out, and the panel has no scroll view.
- **Labels read correctly:** Knight 240/240 Guard 14s, Soldier 110/110 Holy Slash READY, Mage 130/130 Shatter READY.
- **11 ability definitions** (`Content/Specs/abilities.json`, 11 assets in `Data/Abilities`), 15 of 42 characters
  carry a special strip, 0 broken clips, art pipeline PASS, save drift test PASS at v8.
- **Enemy moves appear from stage 1.** Before this pass the first ability-carrying enemy was the Skeleton at stage
  11, so a new player could fight for an hour and never see one. Slime now uses Spike, so the move shows up in the
  opening waves. Cost: a Slime can take 1.8x a normal hit instead of 1x - it reads as a hit, not a wall.
- **Slime casts with its second attack strip.** The Slime has no `Ability` art, so its Spike was silently falling
  back to the plain attack (Attack01, 6 frames) while its 12-frame Attack02 sat unused. Slime now points at its own
  ability row `enemy_spike_slime`, which names `Attack02`; the Lava Slime and Lava Behemoth keep `enemy_spike`
  (their own Spike strip). Verified in Play: `cast Enemy idx=0 id=enemy_spike_slime slot=Attack02`, then the view
  plays that strip. Side effect: the blow lands on the last frame of a 12-frame / 12 fps strip, so the hero takes
  the damage about 1.0 s after the call-out (still inside the 2.5 s orange window).
- **Heavy number measured:** the same floating text plays a plain hit at 32 px red (`FF5959FF`) and an ability hit at
  48 px orange (`FF8C26FF`).

## Correction: the fight never stopped because of the offline popup

An earlier note claimed the daily/offline rewards overlay held the fight. That was wrong.

- The popup contains no pause code: it writes no `Time.timeScale`, disables nothing, and never touches the run.
- The fight looked frozen because the **Editor** was not ticking: the project sets
  `Application.runInBackground = false`, and while the Editor window was unfocused `Time.frameCount` moved only
  2 to 5 in about 40 seconds (roughly 0.1 fps).
- Lesson for measurement: never use a value that resets per swing (the attack timer) as proof that the game is
  alive. Use something monotonic, or drive the sim by hand and read the result.

## Fixed in this pass

- **ABILITIES tab layout.** Columns were placed with pixel offsets sized for a ~200 unit strip. The real container
  is 1052 x 523. Every band is now a **fraction** of the column, so the layout cannot overflow whatever the
  container height turns out to be. No scroll view was added and none is wanted.
- **Live balance data can no longer be overwritten by accident.** Root cause found: `MvpSceneBuilder.BuildMvpScene`
  called `DataAssetGenerator.GenerateAll()`, so *every scene rebuild* rewrote the data assets - enemy stats went
  back to generator values (Slime 57 -> 60) and `WaveConfig` lost its wave entries (about 56 of them). The builder
  no longer generates data, and the generator lost its `Tools > Generate Data Assets` menu entry. It survives only
  as the bootstrap the debug scene builder calls when the data is missing entirely.

## Open - UI (hand this to the front-end framework team)

- Placeholder pictures for characters and bosses still exist; hero icons should point at the first idle frame.

## Open - backend (no UI)

0. **Ability unlock story moment (MVP stop-gap lives now).** Every hero owns and wears its first ability from the
   first wave, because no screen or story beat hands one over yet. One switch does it: `GameManager.unlockAllAbilitiesForMvp`
   (true today), calling `AbilityService.OpenAllForMvp`. When the story beat is ready, set the switch off and unlock
   each ability at its beat; nothing else has to change, because the fight already reads whichever ability is worn.
   The `TODO: ability unlock story moment` markers in `AbilityService.cs` and `GameManager.Wiring.cs` point at the two
   places to edit.

1. ~~Effect layer~~ and ~~enemy auto-cast~~ — **done 24 Oct 2026**; see `Combat-Sequencing.md` §7 and §9.
2. **Assignments in the save.** Which hero wears which ability is currently session state; it should be persisted
   (the v7 assignment list already exists, so this is mostly wiring plus a migration if the shape changes).
3. **Abilities from data.** The editor catalogue still drives which ability a hero gets; the ability definitions
   and the card data should be the single source.
4. **Enemy/boss and balance housekeeping.** Remove character/boss placeholders and rework the art validator to "art,
   or else a placeholder". Boss moves (32 of 40 have none) and the nine unused Summon / Heal / Block strips are
   listed in `Combat-Sequencing.md` §11 and §16. **The golden numbers are not re-recorded yet** — they stay frozen
   until story sequencing and the MVP are finished (`Content.md` §10, "When the baseline is re-recorded").

## Resolved in the 24 Oct check

Three items were listed as open and were already done. They are recorded here so nobody re-opens them.

- **`TabController.Show(index)` does activate its page.** Verified in `UI/TabController.cs`: `Show` sets
  `ActiveIndex`, then calls `tab.panel.SetActive(isActive)` for every tab, so the chosen panel is on and the others
  are off. The earlier note measured a moment before that fix landed.
- **`UiSpace.cs` is tracked and owned.** It lives at `Assets/IdleRPG/Scripts/UI/UiSpace.cs` and is in Git
  (`a818643 UI spacing: the management panels use UiSpace roles`). It is part of this project, not a file from
  another team.
- **The Goblin asset is gone.** `Data/Enemies/Enemy_Goblin.asset` no longer exists; the card was removed with the
  band 1 change (`72cdc37`).

## Dirty working tree - historical record (resolved 24 Oct 2026)

> **Archived. Not current status.** This section records what the working tree looked like before the 24 Oct
> commit. The tree is clean now, and every item below was settled. Read it only to understand why a rule exists.

`Main.unity` (~30k lines), `BalanceConfig.asset`, `WaveConfig.asset`, hero/enemy data assets, the TMP font assets,
placeholder `.meta` files, a stray `Enemy_Goblin.asset`, and the untracked `TinyRPGAssets/` + `TinyRPGAssets2/`
art packs were all in the tree before the commit.

Split by what each one was:

- **Damage from the generator (restored from git):** `WaveConfig.asset` had lost ~56 wave entries, which broke the
  stage ladder in the working tree. The enemy/hero data assets had generator numbers back (`Enemy_Slime`
  health 57 -> 60, attack 9.9 -> 6, interval 1.7 -> 2). `Enemy_Goblin.asset` was a stray the generator recreated
  with `firstStage: 1`; nothing referenced it, and it was deleted.
- **Additive, kept:** `BalanceConfig.asset` gained gear fields (inventoryCap, gear rarity weights, prices).
- **Mechanical importer churn:** placeholder `.meta` files gained empty Android/iOS override blocks; the two TMP
  font assets were rewritten by the font importer.
- **Owned by this project:** `Assets/IdleRPG/Scripts/UI/UiSpace.cs` is the spacing ladder. It is tracked now.
- **Art source packs:** `TinyRPGAssets/` + `TinyRPGAssets2/` (6.6 MB) were downloads, not project content; they
  live outside `Assets/` and are ignored by Git.

The scene carries the panel layout from that pass and is committed.
