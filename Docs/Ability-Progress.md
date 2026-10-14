# Abilities - progress and open items

Living status page. The contract (what the feature must do) stays in `Ability-Plan.md`; this page says what
actually works today, what was measured, and what is still open.

## Where the feature stands

| Layer | State |
|---|---|
| Art strips (one special per character) | done, validated |
| Ability definitions + 2 abilities per hero + save v7 | done |
| Buy / unlock (stage gate) / assign | done |
| Roster swap Archer to Soldier, save v8 | done |
| Cast: request, event, log line, cooldown, ABILITIES tab, F4 debug | done, verified |
| Effect (shield / heal / damage) | **not started** |

## Measured, not assumed

- **Cooldown runs on the fight's clock.** Press CAST, advance the fight by 0.8 seconds, read 13.2 seconds left
  of 14. The clock pauses when the game pauses, because it belongs to the sim.
- **The ABILITIES panel fits its container.** Column content spans y -228..244 and x -511..511; the container
  spans y -262..262 and x -526..526. Nothing spills out, and the panel has no scroll view.
- **Labels read correctly:** Knight 240/240 Shield Wall 14s, Soldier 110/110 Charge READY, Mage 130/130 Fireball READY.
- 21 abilities / 76 cards, 0 broken clips, art pipeline PASS, save drift test PASS at v8.

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

- `TabController.Show(index)` sets the index but does not activate the page when called from code: the ability
  panel stayed `activeSelf = false` while the battle log stayed active. The on-screen tab buttons work. One small
  item, in the file that team now owns.
- Placeholder pictures for characters and bosses still exist; hero icons should point at the first idle frame.
- A `UiSpace.cs` file is untracked in `Assets/IdleRPG/Scripts/UI/` - confirm whether that is the front-end team's.

## Open - backend (no UI)

1. **Effect layer.** `AbilityKind` is carried but nothing happens on a cast. Order: shield (Knight Shield Wall) ->
   heal -> damage. Sim side: a shield pool that absorbs damage before health, a heal that raises health, a damage
   burst that goes through the normal damage path, each raising an event the view can show.
2. **Enemy auto-cast.** Enemy abilities (`enemy_block`, `enemy_beam`, `enemy_spike`, `enemy_volley`) exist and
   never fire.
3. **Assignments in the save.** Which hero wears which ability is currently session state; it should be persisted
   (the v7 assignment list already exists, so this is mostly wiring plus a migration if the shape changes).
4. **Abilities from data.** The editor catalogue still drives which ability a hero gets; the ability definitions
   and the card data should be the single source.
5. **Enemy/boss and balance housekeeping.** Restore the files the generator damaged (see the dirty-tree section),
   remove character/boss placeholders, rework the art validator to "art, or else a placeholder", and re-baseline the
   golden numbers. The generator footgun itself is closed.

## Dirty working tree - needs a decision, do not commit blind

`Main.unity` (~30k lines), `BalanceConfig.asset`, `WaveConfig.asset`, hero/enemy data assets, the TMP font assets,
placeholder `.meta` files, an untracked `Enemy_Goblin.asset`, and the untracked `TinyRPGAssets/` +
`TinyRPGAssets2/` art packs.

Split by what each one actually is:

- **Damage from the generator (restore from git):** `WaveConfig.asset` lost ~56 wave entries - the stage ladder is
  broken in the working tree right now. The enemy/hero data assets have generator numbers back (`Enemy_Slime`
  health 57 -> 60, attack 9.9 -> 6, interval 1.7 -> 2). `Enemy_Goblin.asset` is a stray the generator recreated
  with `firstStage: 1`; nothing references it (not the scene, not the wave config), so it can be deleted.
- **Additive, safe to keep:** `BalanceConfig.asset` gained gear fields (inventoryCap, gear rarity weights, prices).
- **Mechanical importer churn:** placeholder `.meta` files gained empty Android/iOS override blocks; the two TMP
  font assets were rewritten by the font importer.
- **Not mine:** `Assets/IdleRPG/Scripts/UI/UiSpace.cs` is the front-end team's spacing ladder (untracked). Left alone.
- **Art source packs:** `TinyRPGAssets/` + `TinyRPGAssets2/` (6.6 MB) are downloads, not project content; they live
  outside `Assets/`, so git should ignore them rather than track them.

The scene carries the new panel layout and is intentionally left uncommitted until the above is settled.
