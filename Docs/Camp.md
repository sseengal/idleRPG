# Camp — the interlude between waves

> **Status: built (P1-P5), 28 Oct 2026.** Branch `feature/story-scenes`.
> A camp is a still scene between waves inside the battle page: three heroes, a placeholder fire,
> and four choices in the band where the ability buttons normally sit. No popup, no timer.
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
| **Short Rest** | Free | 10% max HP, living only | None |
| **Short Rest+** | Ad (free for No Ads) | 20% max HP, living only | None |
| **Long Rest** | **1 camp token** | 30% max HP + revive fallen at 20% | 1 per run |
| **Train** | Free | **Alternates** +1 ATK, then +1 DEF, party-wide, run-only | None |

- Short Rest is always available and never blocked.
- Short Rest+ replaces the 10% heal with 20%; it does not add to it.
- No Ads buyers get the 20% path free, still with a button press.
- Long Rest is the scarce decision. The token refreshes on a new run.
- Train alternates automatically: even total trains = ATK, odd = DEF. No toggle.
- No Press On. Every camp has a free, positive action.

---

## 3. Train

- Party-wide: every hero gains +1 to the chosen stat.
- Run-only: reset on a new run / ascension.
- No cap: 30 trains = +15 ATK and +15 DEF per hero by stage 10.
- Saved as `runTrainAttackBonus` / `runTrainDefenseBonus`.
- Applied as a flat bonus after multipliers in `StatResolver.GetAttack` / `GetDefense`.
- The camp strip shows the next stat: `+1 ATK` with `NEXT: DEF`.

---

## 4. Healing rules

- `healHeroesOnStageAdvance` is **off**. Camp is the only voluntary heal.
- A new run and an ascension start full. A wipe still restores the party (reset point).
- Short Rest does not revive. Long Rest is the only revive.
- A dead hero carries across waves until a Long Rest or a wipe.

---

## 5. The view

```
┌──────────────────────────────────────────┐
│ gold · gems · stage                      │
├──────────────────────────────────────────┤
│  CAMP                                    │
│  1 LONG-REST TOKEN                       │
│  TRAINED +5 ATK · +5 DEF                 │
│                                          │
│   hero        hero        hero           │
│  Knight 40/120  Soldier 90/100  Mage 0/80│
│              [fire]                      │
├──────────────────────────────────────────┤
│ [SHORT REST] [SHORT REST+] [LONG REST] [TRAIN] │
│ [10% FREE  ] [20% (AD)  ] [30%+REVIVE] [+1 ATK ] │
│ [          ] [         ] [1 TOKEN   ] [NEXT:DEF] │
├──────────────────────────────────────────┤
│ battle log                               │
├──────────────────────────────────────────┤
│ BATTLE  PARTY  UPGRADES  ASCEND  SHOP    │
└──────────────────────────────────────────┘
```

- The view lives inside the battle viewport. The arena backdrop stays; the fighting heroes and enemies hide.
- The camp strip replaces the ability strip in the same band. The log is never covered.
- `CampPanelUI` lives on the always-active `HUD` root, so it hears a camp open on any page and switches
  the battle page forward. A forced choice never waits unseen behind the party or upgrade tabs.
- The view and strip start hidden and switch on `CampService.Opened`; `Closed` puts the board,
  enemy stack and ability strip back.
- HP rows show current/max. A fallen hero is shown in gray and `FALLEN`.
- Long Rest is disabled when the token is spent: `TOKEN SPENT`.
- Short Rest+ label is `20% (AD)`, or `20% FREE` for No Ads buyers.
- Train label shows the next stat.
- **No timer.** The fight holds until the player taps a choice. An idle player stops at the camp.
  Auto-camp, if wanted later, is an opt-in automation card, not a hidden default.

---

## 6. Code

| Piece | Job |
|---|---|
| `Progression/CampService.cs` | Timing, choices, token, heal, train, resume |
| `UI/CampPanelUI.cs` | The view and strip: build actors, refresh HP/labels, switch the battle page |
| `Editor/MvpSceneBuilder.Camp.cs` | Builds `CampView` under the viewport and `CampStrip` in the ability band |
| `CombatDirector` | `WaveGate` + `ResumeAfterGate`; the camp holds before a spawn |
| `CombatManager` | `SetWaveGate`, `ResumeAfterCamp`, `HealPartyFraction` |
| `Encounter` / `Combatant` | `HealPartyFraction`, `ReviveTo` |
| `StatResolver` | Run ATK / DEF bonuses |
| `GameManager.Camp.cs` | Run state: token, last camp, training; public `Camp` and `Monetization` |
| Save | `runCampTokens`, `runLastCampStage`, `runLastCampWave`, `runTrainAttackBonus`, `runTrainDefenseBonus` |

The gate is asked before every wave spawn. It covers the camp (waves 4, 7, 10) and is the same seam the story scenes will use (wave 1 intro, wave 5 mid, boss intro).

---

## 7. Save and reset

- Additive fields; no schema bump. `runCampTokens = -1` on an old save means "fill to the run's capacity".
- Reset on a new run / ascension: token back, training cleared, last camp cleared.
- A camp resolved at stage 3/wave 3 is remembered by **stage and wave**, so the next stage's wave 3 camp still happens.
- Quitting during a camp: the camp reopens on load because nothing was resolved or saved.

---

## 8. Risks and tuning knobs

| Risk | Knob |
|---|---|
| 30 trains = +30 points per hero | `campTrainAttackPerTrain` / `campTrainDefensePerTrain` |
| Free 10% x 3 = 30% per stage | `campShortRestPercent` |
| Up to 30 ads per run if always upgraded | No cap by design; watch retention |
| Dead heroes carry | `campRevivePercent`, `campLongRestPercent` |
| Camp every 3 waves feels like a chore | `campEveryWaves` |
| No timer stalls an idle player | Intended. Offer auto-camp later as an opt-in automation card |

## 9. Manual checks (stage 1)

1. **F7** opens the camp in the battle viewport. Combat freezes; three actors, the fire and HP rows show;
   the ability strip and the fighting heroes/enemies hide; the log stays visible.
2. **Wave 4**: fight to wave 3; when it clears, the camp opens before wave 4 spawns. No wave 4 enemies behind it.
3. **Short Rest (free)**: damage a hero, choose 10%. HP rises by 10% of max; the camp closes, the ability
   strip and fighting heroes return, and wave 4 spawns.
4. **Short Rest+ (ad)**: choose 20% (AD). The mock ad plays; HP rises by 20%; the camp strip is disabled
   while the ad is pending.
5. **Long Rest**: choose it once. Token goes to 0, living heroes heal 30%, a fallen hero returns at 20%,
   and the button reads `TOKEN SPENT`.
6. **Train**: first train gives +1 ATK, second gives +1 DEF. The training line and the next-stat label update;
   hero attack rises by 1 in the stats.
7. **No timer**: open the camp and wait. It stays open until a choice is tapped.
8. **Other page**: open the Party tab, then trigger a camp (F7 or wave 3). The battle page comes forward
   with the camp. You can switch to Party while it is open; the camp is still there when you return.
9. **Save / quit**: after a Long Rest and a Train, quit and reload. Token, training and the last camp survive;
   the same checkpoint does not reopen.
10. **Wipe**: wipe after a camp. The replayed stage offers its camps again. (Training can be farmed by
    repeated retries; that is the no-cap risk.)
11. **Off switch**: `campEnabled = false` on `BalanceConfig`; waves flow with no camp.

