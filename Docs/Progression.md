# Progression — meta layers, currencies, prestige

> Owns: everything the player buys/earns outside a single fight.
> Parent: `Architecture.md` (§3.2 SO contracts, §4 save, AD5-AD7).
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** Step 11e (2026-09-21).
> Specs Steps 14-18. Acceptance tests live in `Roadmap.md`.

---

## 1. Progression axes (each must map to DPS, eHP, income or time)

| Axis | Earned from | Spent on | Reset by | Step |
|---|---|---|---|---|
| **Hero stat levels** (exists) | gold | gold | Ascension | 14 (generic keys) |
| **Ability levels** | ability scrolls + gold | scrolls | Ascension | 13-14 |
| **Roster / stars** | hero shards (dupes, bosses, bounties) | shards | never | 17 |
| **Relics** | stage materials (+ dupes to upgrade) | materials | never | 18 |
| **Prestige tracks (L1)** | prestige tokens | tokens | never | exists |
| **Transcendence tracks (L2)** | essence | essence | never | 18 |
| **Automation unlocks** | tokens / milestones | tokens | never | 16 |
| **Formation slots + presets** | stage/zone milestones | - | never | 10 |
| **Codex / account milestones** | stage, zone, kill counts | - | never | 18 |

Rule: **every axis must have a live sink at every stage of the game.** A returning player with nothing to buy
stops feeling progress → churn.

## 2. Currencies (kills the "dead gems" problem)

| Currency | Source | Sinks | Target affordability |
|---|---|---|---|
| `gold` | every kill (primary) | hero stats, ability levels | 1 upgrade per 20-60s early, 3-10 min mid |
| `gems` | bosses (`gemsPerBossKill`), milestones | **fast-forward, offline-cap extension, extra expedition slot, extra auto-buy slot, bounty reroll, small scroll bundles** | 1 meaningful buy per 30-60 min |
| `tokens` | ascension (exists) | prestige tracks | 1-3 levels per ascension early |
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
| **L1 Ascension** (exists) | stage >= `minStageToAscend` | `tokens` | permanent gold/damage/health multipliers; resets gold + stage + hero levels | prestige tree (exists) + **automation unlocks** (Step 16) |
| **L2 Transcendence** | clear `zonesPerTranscendence` zones | `essence` | global multipliers; keeps relics, roster, tokens | ability tiers, extra team slot, difficulty affix toggle, +1 offline cap hour |
| **L3 (later)** | seasons / long horizon | seasonal marks | cosmetics + account-wide multipliers | season modifiers (`ZoneData.seasonTag`) |

Rules: a higher layer must never invalidate a lower one (L2 keeps L1 tracks and adds), and every layer states its
**expected time to first prestige** - L1 30-60 min, L2 1-3 days, L3 weeks.

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
| Wall break while idle | <= 3 min of accumulated frontier income |
| First ascension | 30-60 min |
| Offline cap reached | 2h equivalent (locked - see `Idle-Economy.md`) |
| Decision cadence | an affordable, meaningful purchase every 1-3 min in the active phase |

## 10. Save keys touched

`roster`, `partySlots`, `statLevels`, `tracks`, `currency`, `milestones`, `automation`, `ledger`.
No enum values, no asset refs (AD5). Ascension reset clears `statLevels` unless `resetPolicy = Never`;
`roster`, `Never` tracks and `milestones` survive.

## 11. Open questions

1. Team size 5: does the battle layout stay portrait-viable (2 hero rows x 3 + 3 enemies)? → decide in Step 10 with a mock screenshot.
2. Relics per-hero vs per-account: start per-account; revisit if builds feel flat.
3. Star-up ability slots: +1 slot at 2/4 stars, or a fixed 3-slot loadout with stars granting passives? → decide in Step 13 once abilities exist.

