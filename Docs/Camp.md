# Camp — the interlude between waves

> **Status: built (P1-P4), 25 Oct 2026.** Branch `feature/story-scenes`.
> A camp is a still scene between waves: three heroes, a placeholder fire, three choices.
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
- The panel shows the next stat: `+1 ATK` with `next: DEF`.

---

## 4. Healing rules

- `healHeroesOnStageAdvance` is **off**. Camp is the only voluntary heal.
- A new run and an ascension start full. A wipe still restores the party (reset point).
- Short Rest does not revive. Long Rest is the only revive.
- A dead hero carries across waves until a Long Rest or a wipe.

---

## 5. The panel

```
CAMP                                 1 LONG-REST TOKEN

Knight 40/120    Soldier 90/100    Mage 0/80 (FALLEN)

              [placeholder fire]

[ SHORT REST ]        [ LONG REST ]        [ TRAIN ]
10% free              30% + REVIVE         +1 ATK
20% (AD)              uses token           next: DEF

auto in 9…
```

- Modal panel on the modal canvas. The header/nav are behind the scrim.
- The root stays active; a CanvasGroup hides it. This lets `Start` subscribe to the service.
- HP rows show current/max. A fallen hero is shown in gray and `FALLEN`.
- Long Rest is disabled when the token is spent: `TOKEN SPENT`.
- Short Rest+ label is `20% (AD)`, or `20% FREE` for No Ads buyers.
- Train label shows the next stat.
- **Auto after 10 s:** any hero below 50% or fallen -> free 10% Short Rest; otherwise Train. Long Rest and the ad are never automatic.

---

## 6. Code

| Piece | Job |
|---|---|
| `Progression/CampService.cs` | Timing, choices, token, heal, train, resume |
| `UI/CampPanelUI.cs` | The still panel and its buttons |
| `Editor/MvpSceneBuilder.Camp.cs` | Builds the panel under the modal safe area |
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
| Camp every 3 waves feels like a chore | `campEveryWaves`, `campAutoSeconds` |

## 9. Manual checks (stage 1)

1. **F7** opens the camp over the fight. Combat freezes; three actors and the fire show; HP rows are correct.
2. **Wave 4**: fight to wave 3; when it clears, the camp opens before wave 4 spawns. No wave 4 enemies behind it.
3. **Short Rest (free)**: damage a hero, choose 10%. HP rises by 10% of max; the camp closes and wave 4 spawns.
4. **Short Rest+ (ad)**: choose 20% (AD). The mock ad plays; HP rises by 20%; `ad playing…` blocks other buttons and the auto timer.
5. **Long Rest**: choose it once. Token goes to 0, living heroes heal 30%, a fallen hero returns at 20%, and the button reads `TOKEN SPENT`.
6. **Train**: first train gives +1 ATK, second gives +1 DEF. The training line and the next-stat label update; hero attack rises by 1 in the stats.
7. **Auto**: open the camp and wait 10 s. Hurt party -> free Short Rest; healthy party -> Train. Long Rest and the ad never fire automatically.
8. **Save / quit**: after a Long Rest and a Train, quit and reload. Token, training and the last camp survive; the same checkpoint does not reopen.
9. **Wipe**: wipe after a camp. The replayed stage offers its camps again. (Training can be farmed by repeated retries; that is the no-cap risk.)
10. **Off switch**: `campEnabled = false` on `BalanceConfig`; waves flow with no camp.

