# Camp — the interlude between waves

> **Status: built (P1-P5), 28 Oct 2026.** Branch `feature/story-scenes`.
> A camp is a still scene between waves inside the battle page: three heroes, a low fire,
> and **three** choices in the band where the ability buttons normally sit. No popup, no timer.
> It is the first use of the story layer. No map, no story runner, no new art.

---

## 1. The flow

```
stage: 9 normal waves + 1 boss = 10
wave 1-3   fight
  CAMP     after wave 3
wave 4-5   fight
wave 6     fight
  CAMP     after wave 6
wave 7-9   fight
  CAMP     after wave 9   <- pre-boss rest
wave 10    BOSS
```

3 camps per stage, 30 per run (10 stages).

---

## 2. The three options

| Option | Cost | Effect | Cap |
|---|---|---|---|
| **Short Rest** | Free | 10% max HP; fallen revived at 10% | None |
| **Long Rest** | Rewarded ad (free for No Ads) | 20% max HP; fallen revived at 20% | None |
| **Train** | Free | **Alternates** +1 ATK, then +1 DEF, party-wide, run-only | None |

- Short Rest and Long Rest both heal the living and bring a fallen hero back at the healed fraction.
- Long Rest is the bigger heal and costs one rewarded ad. It is available **every camp**, with no daily cap.
  No Ads buyers get it free, still with a button press.
- Train alternates automatically: even total trains = ATK, odd = DEF. No toggle. The card shows `+1 ATK` / `+1 DEF`.
- Every camp has a free, positive action (Short Rest or Train). No Press On.
- The camp token is retired. The long rest no longer spends a token.
- Camp Train is **free**. Gold is spent only in the permanent TRAINING tree.

---

## 3. Train

- Party-wide: every hero gains +1 to the chosen stat.
- Run-only: reset on a new run / ascension.
- No cap: 30 trains = +15 ATK and +15 DEF per hero by stage 10.
- **Free: no gold.** The camp choice is heal HP or raise party stats.
- Saved as `runTrainAttackBonus` / `runTrainDefenseBonus`.
- Applied as a flat bonus after multipliers in `StatResolver.GetAttack` / `GetDefense`.
- The camp strip shows `+1 ATK` / `+1 DEF` (the effect is party-wide; the card does not repeat it).

---

## 4. Healing rules

- `healHeroesOnStageAdvance` is **off**. Camp is the only voluntary heal.
- A new run and an ascension start full. A wipe still restores the party (reset point).
- **Both Short Rest and Long Rest revive a fallen hero at the healed fraction** (10% / 20%). There is no separate
  camp revive.
- The **Revive** ASCEND perk is the only run-over revive: one per run, full heal, same stage.
- A dead hero carries across waves until a camp heal or a wipe.

---

## 5. The view

```
┌──────────────────────────────────────────┐
│ gold · gems · menu                       │
├──────────────────────────────────────────┤
│  CAMP                                    │
│                                          │
│               KNIGHT                     │
│              [269/269]                   │
│                                          │
│   SOLDIER     [fire]      MAGE           │
│  [123/123]               [146/146]       │
├──────────────────────────────────────────┤
│ [SHORT REST] [LONG REST] [TRAIN]         │
│ [+10% HP   ] [+20% HP(Ad)] [+1 ATK     ]│
├──────────────────────────────────────────┤
│ battle log                               │
└──────────────────────────────────────────┘
```

- The view lives inside the battle viewport. The arena backdrop stays; the fighting heroes and enemies hide.
- The **middle hero stands above the fire** (y 0.55); the **left and right heroes stand beside it** (y 0.42),
  so the three form a triangle around the flames. The mage is flipped to face the fire. The three idle clips
  start **0 / 0.12 / 0.24 s** apart.
- The camp strip replaces the ability strip in the same band. The log is never covered.
- `CampPanelUI` lives on the always-active `HUD` root, so it hears a camp open on any page and switches
  the battle page forward. A forced choice never waits unseen behind the party or upgrade tabs.
- The view and strip start hidden and switch on `CampService.Opened`; `Closed` puts the board,
  enemy stack and ability strip back.
- Each actor has a name line and a **green HP bar** under its feet. The current/max numbers are inside the bar.
  A fallen hero is gray, reads `FALLEN`, and the bar is empty.
- Every card is **two lines**. The short rest is `+10% HP`; the long rest is `+20% HP (Ad)`
  (`+20% HP (Free)` for No Ads); the train is `+1 ATK` / `+1 DEF`.
- While the camp is open the battle log is wiped and shows one line: `The party camps for the night..`.
  A short rest writes `The party takes a short rest.` after the choice; the train writes its own line. The long rest
  leaves the camp line in place.
- The old token and training labels are gone.
- **No timer.** The fight holds until the player taps a choice. An idle player stops at the camp.
  Auto-camp, if wanted later, is an opt-in automation card, not a hidden default.

---

## 6. Code

| Piece | Job |
|---|---|
| `Progression/CampService.cs` | Timing, choices, heal, train, resume |
| `UI/CampPanelUI.cs` | The view and strip: actors, HP names/bars, phase/facing, page switch |
| `UI/CampChoiceView.cs` | One neutral card: face, type roles and tokenised stack |
| `Editor/MvpSceneBuilder.Camp.cs` | Builds `CampView` (nameplates, fire) and `CampStrip` (three cards) |
| `CombatDirector` | `WaveGate` + `ResumeAfterGate`; the camp holds before a spawn |
| `CombatManager` | `SetWaveGate`, `ResumeAfterCamp`, `HealPartyFraction` |
| `Encounter` / `Combatant` | `HealPartyFraction`, `ReviveTo` |
| `StatResolver` | Run ATK / DEF bonuses |
| `GameManager.Camp.cs` | Run state: last camp, training; public `Camp` and `Monetization` |
| Save | `runLastCampStage`, `runLastCampWave`, `runTrainAttackBonus`, `runTrainDefenseBonus` (`runCampTokens` retired) |

The gate is asked before every wave spawn. It covers the camp (waves 4, 7, 10) and is the same seam the story scenes will use (wave 1 intro, wave 5 mid, boss intro).

---

## 7. Save and reset

- The camp token is retired. `runCampTokens` stays in the save for old files but is no longer read or spent.
- Reset on a new run / ascension: training cleared, last camp cleared.
- A camp resolved at stage 3/wave 3 is remembered by **stage and wave**, so the next stage's wave 3 camp still happens.
- Quitting during a camp: the camp reopens on load because nothing was resolved or saved.

---

## 8. Risks and tuning knobs

| Risk | Knob |
|---|---|
| 30 trains = +30 points per hero | `campTrainAttackPerTrain` / `campTrainDefensePerTrain` |
| Free 10% x 3 = 30% + revives per stage | `campShortRestPercent` |
| 20% x 3 = 60% + revives for 3 ads per stage; no ad cap | `campLongRestPercent` |
| Camp every 3 waves feels like a chore | `campEveryWaves` |
| No timer stalls an idle player | Intended. Offer auto-camp later as an opt-in automation card |

## 9. Manual checks (stage 1)

1. **F7** opens the camp in the battle viewport. Combat freezes; three actors, the fire and HP rows show;
   the ability strip and the fighting heroes/enemies hide; the log stays visible.
2. **Wave 4**: fight to wave 3; when it clears, the camp opens before wave 4 spawns. No wave 4 enemies behind it.
3. **Short Rest (free)**: damage a hero (or fell one), choose 10%. Living heroes heal 10% of max; a fallen hero
   returns at 10%; the camp closes, the ability strip and fighting heroes return, and wave 4 spawns. The log reads
   `The party takes a short rest.`
4. **Long Rest (ad)**: choose `+20% HP (Ad)`. The mock ad plays; living heroes heal 20%; a fallen hero returns at 20%.
   It is available at **every** camp. With No Ads owned the label reads `+20% HP (Free)`, no ad plays, and the same
   heal lands. The camp strip is disabled while the ad is pending. The log reads `The party camps for the night..`
5. **Three cards, two lines**: there is no `SHORT REST+`. The three cards wear the neutral face; the short is
   `+10% HP`, the long `+20% HP (Ad)`, the train `+1 ATK` / `+1 DEF`.
6. **Train**: first train gives +1 ATK, second gives +1 DEF. The label says the stat; no gold is spent;
   hero attack rises by 1 in the stats. The effect is party-wide.
7. **Camp view**: no token or training labels under `CAMP`. The middle hero is above the fire; the left and right
   heroes stand beside it. Each hero has a name line and a green HP bar under the feet, with the numbers inside the
   bar. A fallen hero is gray, reads `FALLEN`, and the bar is empty. The mage faces the fire; the three idle clips
   are out of step.
8. **No timer**: open the camp and wait. It stays open until a choice is tapped.
9. **Other page**: open the Party tab, then trigger a camp (F7 or wave 3). The battle page comes forward
   with the camp. You can switch to Party while it is open; the camp is still there when you return.
10. **Save / quit**: after a Train, quit and reload. Training and the last camp survive; the same checkpoint does
    not reopen. The camp token is retired; the header shows gold and gems only.
11. **Wipe**: wipe after a camp. The replayed stage offers its camps again. (Training can be farmed by
    repeated retries; that is the no-cap risk.)
12. **Off switch**: `campEnabled = false` on `BalanceConfig`; waves flow with no camp.

