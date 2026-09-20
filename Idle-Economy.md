# Idle-Economy — rates, offline, time payouts, monetisation, telemetry

> Owns: every way the game pays the player for time, and every way it asks for money.
> Parent: `Architecture.md` (AD6, AD7, B4-B7, B13, A9).
> Specs Steps 9, 17, 19. Acceptance tests live in `Roadmap.md`.

---

## 1. Core principle

**One measured rate per currency, and every payout goes through it.** Offline, expeditions, bounties, ads and
milestone chests all convert *time* (or a currency) into rewards using `SimLedger` rates - never a second formula
that can drift from what the player earns while playing (AD6/AD7).

```
ledger.goldPerSecond      measured rolling 60s (existing EconomyRateTracker, promoted into SimLedger)
ledger.shardsPerSecond    same idea, fed by the reward funnel
ledger.essencePerSecond   same
ledger.secondsPerStage    for "how far could I have gotten" style UI
```

Fallback order (unchanged from the MVP): measured -> saved rate -> formula estimate.

## 2. Time-based payout table (locked)

| Payout | Trigger | Formula | Caps / guards |
|---|---|---|---|
| **Offline gold** (shipped) | app relaunch | `min(away, cap) x goldPerSecond x 0.7`, dual cap | wall 28800s + equivalent 7200s; negative delta pays 0; one claim per window |
| **Offline shards/essence** (Step 17+) | same claim | `paidSeconds x ledger.shardsPerSecond x 0.5` | same dual cap; only for currencies unlocked by then |
| **Expedition reward** | timer ends while away or online | `definition.rewardTable x durationFactor` | per-slot, one expedition per hero, no stacking |
| **Bounty** (return hub) | player picks 1 of 3 | `bountyTable x awayFactor` | one pick per return, rerollable with gems/ads |
| **Ad fast-forward** | player watches an ad | `minutes x rate x 0.5` | daily cap per `AdPlacementDef`, never while offline |
| **Milestone chests** | stage/zone first clear | fixed tables | one-shot per id (repeatable flag for walls) |

`paidSeconds` is always computed from `GameClock` timestamps stored in the save; every path clamps negative and
absurd deltas (tamper) and is idempotent via a pending/claimed flag.

## 3. `IdleTimeService` (one owner, replaces ad-hoc offline code)

```
IdleTimeService
  ClaimOffline() -> OfflineReward  (existing OfflineProgressManager behaviour, extended to bundles)
  StartExpedition(heroId, definitionId) / CollectExpedition(slot)
  RollBounties(awaySeconds) / ClaimBounty(id)
  Tick(now)         # expires boosts, completes expeditions, resets dailies
  Guards: pendingClaim, negativeDelta -> 0, clampSeconds, per-source daily caps
```

- The existing `OfflineProgressManager` becomes the offline *calculator* inside this service; its verified guards
  (dual cap, tamper, double-claim) stay as-is.
- Everything time-based is registered here, so there is exactly one place that reads wall-clock time.

## 4. Return hub (the 30-second session - B1)

Target flow on relaunch (<= 3 taps to be back farming):

```
1  Offline popup: "You were away 3h" -> CLAIM (gold + shards if unlocked) [+ optional x2 via ad]
2  Hub card: expedition finished -> COLLECT  (and one tap to START a new one)
3  Hub card: bounty x3 -> PICK ONE
   -> "Battle" button, auto-retry already running in the background
```

Rules: the popup never blocks longer than the claim; unattended returns still progress the run (auto-retry), so the
hub is a bonus layer, never a gate.

## 5. Ads (B6 - data-driven, mock until Step 19)

| Placement | Reward | Daily cap | Notes |
|---|---|---|---|
| `GoldBoostDouble` (shipped) | x2 gold for 1h | 6 | already implemented via `IAdService` |
| `OfflineDouble` | x2 the offline claim | 3 | granted after the claim, before it is spent |
| `ExpeditionSkip` | finish one expedition | 3 | never while offline |
| `BountyReroll` | new bounty set | 3 | |
| `GemTopUp` | small gems | 2 | rewarded only, no forced interstitials (C6) |

`AdPlacementDef` rows carry `dailyCap`, `cooldownSec`, `rewardTableId`. Real SDK swap = a new `IAdService`
implementation; no gameplay code changes.

## 6. Premium currency & shop (efficiency only - B5)

| SKU | Type | Why it sells |
|---|---|---|
| Offline cap +2h (permanent) | gem sink / IAP | the highest-value idle purchase |
| Extra expedition slot | gem sink / IAP | more parallel idle income |
| Extra automation slot | gem sink / IAP | quality of life at scale |
| Fast-forward x2 (1h) | gem sink | instant gratification |
| Ability scroll bundle | gem sink | power-adjacent but also earned in play |
| No-ads | IAP | classic |
| Starter pack | IAP | one-time value, no power creep |
| Season pass (local first) | IAP | cosmetics + currency track |

Hard rules: **never sell raw power** (no "double your DPS"), never time-gate a mechanic behind a purchase, and every
paid item must also be earnable in game (slower).

## 7. Anti-abuse

| Risk | Guard |
|---|---|
| Clock rewind | negative delta -> 0 (shipped); also clamp against `ledger.playTimeSeconds` monotonicity |
| Clock forward | dual caps bound the payout anyway |
| Save edit | ids-only schema + sanity clamps on load (`SaveSystem.Sanitize`) + a warning log; no server authority yet |
| Claim replay | pending/claimed flags per window; single writer (`SaveCoordinator`) |
| Infinite offline loop (relaunch spam) | min offline window (30s, shipped) + the offline window advances on every save |
| Ledger poisoning | external grants excluded from rate samples (shipped `BeginExternalGrant`) |

## 8. Telemetry (local first - B10)

```
TelemetryFeed (ring buffer, ~200 entries, no allocation in the loop)
  SessionStart / SessionEnd(seconds, stage, goldPerSecond)
  StageReached(stage, secondsSinceStart)
  WallHit(stage, minutesStuck)
  OfflineClaim(awaySeconds, paidSeconds, gold)
  Purchase(trackId, levels, cost, currency)
  Ascend(tokens, stage)      Transcend(tokens, stage)
  AdWatched(placementId, rewardId)
```

- Purpose: tune pacing from real sessions, not vibes ("where do players stall?", "is the offline claim ever
  claimed?"). The dev overlay (F3) renders a live subset.
- Local only, no network. An opt-in upload lands after the game proves it needs it (Step 20+).
- **Implemented (Step 8b):** `Scripts/Debug/TelemetryFeed.cs` - 200-entry ring buffer subscribed to `GameEvents`
  (stage, defeat, offline offer/claim, save, upgrade, ascend, boost), read by the F3 overlay. No per-frame cost.
- Cheapest ingestion point: the same events `GameEvents` already raises; the feed just samples the interesting ones.

## 9. Config knobs (all data, no literals)

| Knob | Where | Current / target |
|---|---|---|
| `offlineCapSeconds` | `BalanceConfig` | 28800 (locked) |
| `offlineMaxEquivalentSeconds` | `BalanceConfig` | 7200 (locked) |
| `offlineEfficiency` | `BalanceConfig` | 0.7 (locked) |
| `minOfflineSecondsForPopup` | `BalanceConfig` | 30 |
| extra offline cap hours | shop/gems | +2h per purchase, cap +4h total |
| `expeditionDurationOptions` | `ExpeditionDef` | 1h / 4h / 8h |
| `bountyChoicesOffered` | `BountyDef` | 3 |
| ad daily caps | `AdPlacementDef` | per placement, see §5 |
| `fastForwardMultiplier` | `BalanceConfig` | x2 (ad/gem), never above x3 |
| `ledgerWindowSeconds` | `BalanceConfig` | 60 (same as the shipped rate tracker) |

## 10. Open questions

1. Does the offline claim pay every unlocked currency at once, or gold only until shards/essence exist? Proposal:
   gold + whichever currencies are unlocked at claim time, each with its own efficiency factor.
2. Expedition rewards: paid as loot tables (random) or fixed by duration (predictable)? Idle players prefer
   predictable; keep a small random bonus roll on top.
3. Should gem sinks be one-time purchases (cap extension) or repeatable (fast-forward)? Both: one-time for
   permanent slots, repeatable for consumables.

