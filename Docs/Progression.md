# Progression — meta layers, currencies, prestige

> Owns: everything the player buys/earns outside a single fight.
> Parent: `Architecture.md` (§3.2 SO contracts, §4 save, AD5-AD7).
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** 24 Oct 2026 (re-read against the code).
> Specs Steps 14-18. Acceptance tests live in `Roadmap.md`.
>
> **Built today:** gold stat tracks, ascension, automation cards, gear (save v6) and one ability per hero (save v7).
> **Drift note (29 Oct 2026):** hero stats and prestige still run on `StatUpgradeData` / `PrestigeUpgradeData`. The
> generic `ProgressionTrack` + `TrackService` exist and serve the gem tracks only, so §4 below is a target for the
> migration, not a description of every track today.
> Stars, relics, expeditions, scrolls and the L2/L3 prestige layers below are **design targets**, not code.

---

## 1. Progression axes (each must map to DPS, eHP, income or time)

| Axis | Earned from | Spent on | Reset by | Step |
|---|---|---|---|---|
| **Hero stat levels** (RETIRED in UI, values kept) | gold | gold | Ascension | 14 (generic keys) |
| **Training - hero chains** (exists) | gold | gold | never | built |
| **Training - party track** (exists) | perk tokens | tokens | never | built |

> **Retired 2026-11-08:** the per-hero gold stat track has no UI any more (its saved levels still apply). Crit chance
> and crit damage - the two rows that only lived there - return later through the **experience-points** system, not
> through the training tree.
| **Ability levels** | ability scrolls + gold | scrolls | Ascension | 13-14 |
| **Roster / stars** | hero shards (dupes, bosses, bounties) | shards | never | 17 |
| **Relics** | stage materials (+ dupes to upgrade) | materials | never | 18 |
| **Prestige tracks (L1)** | prestige tokens | tokens | never | **exists, buyable on the ASCENSION page** |
| **Party levels → boons** | XP from kills (this run) | - | run end | **slice 1 built** |
| **Transcendence tracks (L2)** | essence | essence | never | 18 |
| **Automation unlocks** | tokens / milestones | tokens | never | 16 |
| **Formation slots + presets** | stage/zone milestones | - | never | 10 |
| **Codex / account milestones** | stage, zone, kill counts | - | never | 18 |

Rule: **every axis must have a live sink at every stage of the game.** A returning player with nothing to buy
stops feeling progress → churn.

### 1a. Party levels and boons (run-scoped, built 2026-11-09)

The three heroes share **one** XP bar. Every kill pays XP, the bar fills, and each level pays the player **one boon
choice** - a stay-for-this-run upgrade picked from three cards. This is the roguelite layer: it is rebuilt every run.

| Rule | Value / behaviour |
|---|---|
| XP per kill | `partyXpPerKill x partyXpPerKillStageGrowth^(stage-1)`, bosses x `partyXpBossMultiplier` |
| Level requirement | `partyXpBase x partyXpGrowth^(level-1)`; today 20 x 1.18 (front-loaded - level 12 by stage 5). The fit targets ~50 x 1.35 and is measured, not guessed |
| Levels per run | **no hard cap** (decision 2026-11-09): the requirement curve limits the picks, not a counter. Target shape is **10-12 picks**, first by wave 2, last near where the run ends |
| Picks | one per level; a kill that crosses two levels queues two picks, dealt back to back |
| Lifetime | run-scoped - level, XP and queued picks clear with the run, next to run gold |
| Cards | **built 2026-11-09**: 3 dealt, rarity-weighted, never a maxed card, `REFRESH 1/1` per run; every pick writes a feed line (`Boon: Cleaving Blow`) |
| Deck | 13 cards: 9 numbers + 4 mechanics (Cleaving Blow, Battle Echo, Vampiric Edge, Thorned Mail) |
| Card content | `Data/Perks/*.asset` (ten starters), dealt by `Data/Boons/PerkPool.asset`; regenerate with **Tools > Idle RPG > Generate Boon Assets** |
| Saved | level, XP, queued picks, the hand (ids), re-rolls spent, **and the cards on screen** - so quitting mid-card is not a free re-roll |
| Code | `Progression/PartyProgression.cs`, `PerkLoadout.cs`, `PerkOfferService.cs` (pure C#), wired in `GameManager.Perks.cs` |

**Boons target heroes, not just the party.** A boon carries a target (`AllHeroes` or one hero id), so "thorns for the
Mage +5" is one data asset, not a special case. Tuning stays in data: same op, different target and number.

**The fight pauses for the choice** (decision 2026-11-09): when a pick is owed the run stops and the card opens, so
the player cannot lose health while reading. The hold is counted (`CombatManager.Pause(reason)`), because a cutscene,
the camp and a boon card can all want the fight frozen at once. The card sits on its own canvas at sorting order 300,
above the modal layer (100), the lobby (250) and the MENU button (251) - nothing of the fight may peek over a choice.

**Boons are temporary by design (decision 2026-11-09).** They last for the current run and die with it, exactly like
run gold: no unlocks, no carry-over, no permanent power. That is what lets a card be strong without breaking the
permanent ladder, and what makes each run a different build.

**There is no boon list page (decision 2026-11-09).** The hand is shown only on the level-up card - what you hold is
felt in the fight, not read off a screen. The trade-off is real: a player cannot look up "what did I take in wave 2".
If playtesting shows confusion, the cheap fix is a one-line count on the XP bar (`LV 4 - 6 boons`) before any list.

**A boon is a stat bonus today, not a new mechanic.** The seven ops (`AttackPercent`, `HealthPercent`, `DefensePercent`,
`GoldPercent`, `AttackSpeedPercent`, `CritChanceFlat`, `CritDamageFlat`) all land on getters `StatResolver` already
had, so a card is live in the next swing and there is no second stat pipeline. On-hit effects (cleave, extra strike)
are the next slice and need a hook inside `Encounter` - see `Boons.md` for that plan.

**Away time pays neither gold nor XP** (decision 2026-11-09): the away window is being retired in favour of daily
login bonuses, so time away is not a progression axis. There is no run-duration statistic to keep honest either.

## 2. Currencies (kills the "dead gems" problem)

| Currency | Source | Sinks | Target affordability |
|---|---|---|---|
| `gold` | every kill (primary) | hero stats, ability levels | 1 upgrade per 20-60s early, 3-10 min mid |
| `gems` | **milestone stages** - first clear of every 5th stage (`gemsPerMilestone`, `milestoneStageInterval`) | **fast-forward, offline-cap extension, extra expedition slot, extra auto-buy slot, bounty reroll, small scroll bundles** | 1 meaningful buy per 30-60 min |
| `tokens` | ascension (exists) | prestige tracks + the party Training track (2-30 per node) | 1-3 levels per ascension early |
| `shards.<heroId>` | dupes, boss chests, bounties | star-ups, unlocks | star-up per 10-30 min mid |
| `materials` | zone-tier stage drops | relic levels | relic level per 5-15 min mid |
| `essence` | transcendence | L2 tracks (global multipliers, ability tiers, slots) | 1 buy per transcendence |
| `scrolls` | bosses, expeditions | ability levels | a few per hour |

All are `CurrencyDef` SO rows - adding one is data-only. `CurrencyDef.isPremium` marks currency that the idle
rate never pays (gems keep their earn path but stay the ad/IAP-adjacent currency).

## 3. Cost curves (`CostCurve` SO - one place for all pricing)

```
cost(level)       = baseCost * growth^level                 # geometric (default)
                    | baseCost * (level+1)^power            # polynomial (soften late)
                    | step table (milestone prices)         # step
affordableN(gold) = log_growth(1 + gold*(growth-1)/(base*growth^level))   # closed form, already used
```

Targets: a **decision point every 1-3 minutes** in a track's active phase, and at the frontier an upgrade
must cost <= 30-60s of frontier income.

## 4. `ProgressionTrack` (replaces hardcoded upgrade maths - D7)

```
ProgressionTrack (SO)
  id, displayName, icon, description
  currency      gold | tokens | shards.<id> | materials | essence
  costCurve     CostCurve ref
  effect        one of: statAdd(statId, perLevel) | statPercent(statId, perLevel)
                        abilityUnlock(abilityId) | abilityLevel(abilityId)
                        rosterSlot(count) | automation(automationId) | loot(LootTable, perLevel)
  prerequisites (trackId -> minLevel) | (milestoneId) | (heroId owned)
  resetPolicy   Ascension | Transcendence | Never
  maxLevel      0 = unlimited
```

`ProgressionService` is the **only** purchase path: `CanBuy(trackId)`, `Cost(trackId, levels)`,
`Buy(trackId, levels)` → mutates levels, raises `TrackLevelChanged`, marks the save dirty.
UI never derives costs or affordability itself (today `HeroUpgradeRowUI` re-derives affordability - move it in).

## 5. Roster, stars, team size

| Concept | Rule |
|---|---|
| Ownership | `roster[{heroId, owned, star, shards}]`; unowned heroes show in the Codex as silhouettes |
| Unlock | milestone, shard count, or zone clear - never a hard paywall for the starter 3 |
| Stars 0-5 | star N costs `shardCurve(N)` of that hero's shards; grants +stat %, +1 ability slot at 2 and 4 stars, +passive at 3 stars |
| Team size | 3 at start, 4 after zone 2, 5 after zone 4 (formation slots unlock - Step 10 model) |
| Reserve heroes | unassigned owned heroes are expedition candidates (Step 17), never dead weight |
| Dupes | duplicate ownership converts to that hero's shards (`dupeShardValue` per rarity) |

## 6. Relics (long-horizon sinks; no randomized stats at first - C4)

| Slot | Source | Effect model |
|---|---|---|
| weapon / armor / trinket x2 | zone material drops, boss chests | `EffectSpec[]` + set bonuses |

- Upgrade costs materials only - **no RNG rolls** until the stat registry + effect pipeline are battle-tested.
- Set bonuses (2 / 4 pieces) are `EffectSpec` rows: `+8% crit`, `lifesteal 3%`, `+12% gold`.
- Relics are per-account (not per-hero) to keep UI and save small; hero-specific relics are deferred.

## 7. Prestige layers (each = a multiplier **and** a new mechanic - B8)

| Layer | Trigger | Currency | Grants | New mechanic |
|---|---|---|---|---|
| **L1 Ascension** (exists, retuned) | **this run** reached `minStageToAscend` | `tokens` | permanent gold/damage/health multipliers; resets gold + stage + hero levels + **the run best** | prestige tree (exists) + **automation unlocks** (Step 16) |
| **L2 Transcendence** | clear `zonesPerTranscendence` zones | `essence` | global multipliers; keeps relics, roster, tokens | ability tiers, extra team slot, difficulty affix toggle, +1 offline cap hour |
| **L3 (later)** | seasons / long horizon | seasonal marks | cosmetics + account-wide multipliers | season modifiers (`ZoneData.seasonTag`) |

Rules: a higher layer must never invalidate a lower one (L2 keeps L1 tracks and adds), and every layer states its
**expected time to first prestige** - L1 30-60 min, L2 1-3 days, L3 weeks.

### L1 in detail (retuned 2026-09-22)

What it does: press ASCEND and the run is cashed in - gold, hero levels, stage and the **run best** go to zero,
the lifetime best stays as a record, and you keep gems, tokens and every permanent upgrade you have bought.

| Knob | Value | Why |
|---|---|---|
| Gate | `runBestStage >= minStageToAscend` (10) | **per run.** Gating on the lifetime best left the button live forever and made tokens farmable (found in play) |
| Yield | `floor((runBestStage / 10) ^ 1.5)` | priced on the run, so re-climbing to the same stage pays the same and pushing further pays more |
| Effect | **+20% per level** (`effectPerLevel`) | +10% was ~6% of the power gap at the first wall: it could not break the wall it exists to answer |
| Cap | **25 levels** per track (`maxLevel`) | a 10-level cap (x2.0) ended the whole prestige system by ~stage 170 |

First ascension (stage 25) = 3 tokens = +60% damage: felt, still nowhere near free. Maxing one track is ~600 tokens
(a long haul), and each track reaches +500%.

**Why a player does it:** it is the escape valve. The bounce stalls at a wall, grinding stops moving the frontier,
so you cash the run in for a permanent multiplier and the next run is much faster. Verify with the robot player:
after an ascension the re-climb to the old ceiling must take well under the first climb, and the frontier must
move again.

## 8. Automation (B3 - the churn fix, Step 16)

| Rule | Automates | Unlock |
|---|---|---|
| `auto-buy` | keeps chosen tracks inside a spend budget | after 2nd ascension |
| `auto-ascend` | ascends at a token-yield threshold (optionally stage target) | after 3rd ascension |
| `auto-retry` | exists (`autoRetryEnabled`) | start |
| `auto-equip` | best relic per slot by a score (DPS / eHP weights) | zone 2 |
| `presets` | loadouts per situation (farm / boss / expedition) | Step 10 |

Automation rules are `AutomationDef` rows with per-rule `enabled` + `threshold`, evaluated on the 1s save tick
(cheap). Every automation action raises a toast ("auto-ascended: +14 tokens") - idling must be visible, not silent.

## 9. Pacing invariants (verify with Balance Lab, E13)

| Metric | Target |
|---|---|
| First upgrade | <= 30s of play |
| Stage 1 clear | 90-180s |
| Frontier stage clear | 2-3 min |
| Wall behaviour | **the bounce**: wipe -> fall back one stage -> clear it -> push again, forever, no input. A failed push costs seconds; farmed income is never zero. A wipe now shows a short **defeat card** (see `Run-Identity.md`) that auto-restarts - the beat is longer on new stages but still needs no input |
| Frontier movement | a push succeeds within ~3 min of accumulated frontier income (measured by the robot player) |
| First ascension | 30-60 min |
| Offline cap reached | 2h equivalent (locked - see `Idle-Economy.md`) |
| Decision cadence | an affordable, meaningful purchase every 1-3 min in the active phase |

## 10. Save keys touched

`roster`, `partySlots`, `statLevels`, `tracks`, `currency`, `milestones`, `automation`, `ledger`, `runBestStage`.
No enum values, no asset refs (AD5). Ascension reset clears `statLevels` unless `resetPolicy = Never`;
`roster`, `Never` tracks and `milestones` survive.

## 11. Open questions

1. Team size 5: does the battle layout stay portrait-viable (2 hero rows x 3 + 3 enemies)? → decide in Step 10 with a mock screenshot.
2. Relics per-hero vs per-account: start per-account; revisit if builds feel flat.
3. Star-up ability slots: +1 slot at 2/4 stars, or a fixed 3-slot loadout with stars granting passives? → decide in Step 13 once abilities exist.

