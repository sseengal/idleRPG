# Monetisation — sources, sinks, guards

> Owns: every way the game earns money and every way it asks for it, and the limits that keep both honest.
> Parent: `Architecture.md` (AD6/AD7 - one measured rate, no second formula), `Idle-Economy.md` (time payouts).
> Planned as base step **B7** (see `Roadmap.md` §2). **Last verified:** 2026-09-21 (design pass; nothing built yet).

---

## 1. The rule that comes before any of it

**Never let a purchase out-earn playing.** Every payout - ad, IAP, shop, offline - converts *time* into rewards
through the `SimLedger`'s **measured rate**, exactly like the offline window does today. No second formula, no flat
gold bundles that beat the rate. This is already AD6/AD7 in `Architecture.md`; monetisation is just another caller.

Second rule: **nothing is required to progress.** Ads and gems buy *time and convenience*, never a wall bypass.

---

## 2. What already exists (9b-1 / 9b-2 / 9b-3)

| Type | Item | Current numbers |
|---|---|---|
| Source | **Milestone stages** | first clear of every 5th stage pays **5 gems** (new best only) |
| Sink | Offline cap extension | 50 gems -> +1h, max +3h bought |
| Sink | Instant income | 30 gems -> +1h of income at the measured rate, repeatable |
| Ad | x2 gold boost | x2 for 3600s (`MockAdService`) |
| Plumbing | `IAdService` + `MockAdService`, `AdGoldBoost*` knobs, `ShopService` rows | swap-in ready |

Gems are no longer boss-only: since the loop beat landed, milestone stages pay them and the fallback bounce cannot
farm them (a re-clear is not a new best). Tuning the amounts is part of B7.

---

## 3. Base slice (B7) - what we ship

| Type | Item | Default | Notes |
|---|---|---|---|
| Ad | x2 gold boost | 30 min (retune from 60) | exists; shorter window, more often |
| Ad | **Double your offline earnings** | one tap on the welcome-back popup | highest-converting idle ad; loop-native |
| Ad | **Instant offline claim** | skip the wait, no gems | the fallback when gems are unaffordable |
| Source | **Daily streak** | gems + a short ad boost, growing to a cap on day 7 | gives gems a free trickle |
| Source | **Milestones** | first clear of every 5th stage pays 5 gems | ties gems to the frontier (landed with the loop beat) |
| Sink | Offline cap +1h | 50 gems (exists) | unchanged |
| Sink | Instant income | 30 gems (exists) | unchanged |
| Sink | **Permanent +5% gold** | rising cost per level (`TrackService`, B5) | the classic long-tail gem sink |
| Sink | **Automation unlock** | auto-buy tier 1 for gems, tier 2 for more | monetises the churn fix (B6) - best kind of sink |
| IAP | `IIapService` + `MockIapService` | products: no-ads (one-time), starter pack, gem bundle S/M/L, offline pack | mock now, real store is one implementation swap |

## 4. Guards (non-negotiable)

1. **Ledger-only payouts** - everything routes through the measured rate (see §1).
2. **Per-day ad caps** - `AdPlacementDef` rows: max shows per placement per day (start: 3 boost, 5 offline-related).
3. **No ad payouts while offline** - the offer waits for the player to return.
4. **Validator guard** - fail the content check if any sink grants more than **2x** the income of the time its cost
   implies, i.e. shops may not beat the ledger.
5. **Claim idempotency** - pending/claimed flags per offer (already the pattern in `IdleTimeService`).
6. **Tamper safety** - clock rewinds pay nothing (already enforced).

## 5. Data vs code

| Lives in data (spec + generate) | Lives in code |
|---|---|
| ad placements, caps, boost duration/multiplier | `IAdService` / `IIapService` implementations |
| gem costs and rewards, daily-streak table | `ShopService` purchase flow, claim guards |
| permanent-multiplier track (B5) | `TrackService` (one purchase path) |
| milestone rewards | milestone trigger in the stage-clear path |

**Consequence:** adding a new sink or ad placement after B7 is content work, not engineering - which is the whole
point of doing B5 (tracks) before B7.

## 6. Order inside B7

1. `IIapService` + `MockIapService` + no-ads flag (proves the swap seam).  **DONE 2026-09-27** (see `Checklist.md` §1k)
2. Daily streak + milestone gems (fixes gem scarcity first, so the sinks have something to spend).  **DONE 2026-09-27**
   (see `Checklist.md` §1l - calendar `{5,8,10,12,15,18,25}` + day-7 gold boost, local-midnight, tamper-safe; milestones verified unchanged)
3. Ad placements (**x2 gold boost** retuned 60 -> 30 min, **double offline** - the redundant "instant claim" was cut:
   the popup already pays instantly) + per-day caps.  **BUILT 2026-09-27** (see `Checklist.md` §1m - placements
   `GoldBoost {5/day, 300s}` + `DoubleOffline {3/day, 30s}`, one `TryShowAdPlacement` gate, popup DOUBLE button,
   shop CTA counters; play pass pending the editor bridge)
4. Permanent-multiplier + automation sinks (needs B5 and B6; owner-approved 2026-09-27: gems may unlock automation
   as an accelerant - tokens stay the free path).  **PART 1 BUILT 2026-09-27** (see `Checklist.md` §1n - currency is
   data now (`costCurrency`), the **Golden Foundry** +5% gold track ships (25 gems, x1.6, cap 10 = +50%), the shop
   renders gems tracks from data, and ASCEND stays token-only).  **Deferred:** the automation-for-gems accelerant
   (a duplicate card would break ownership; it must be a checkout currency override - its own small step).
5. Validator guard + a shop readout in the dev overlay.  **DONE 2026-09-27** (see `Checklist.md` §1o -
   `CheckMonetisation()` enforces the 2x time-value rule for time sinks, a **rate-free payback floor** (>= 1h of play,
   warn < 3h, `costGrowth > 1`) for permanent multipliers, "no product sells power" over `IapCatalog`, and free-player
   reachability of the cheapest gem sink; F3 now prints the money/ads lines. Planted cheat card -> 2 errors, planted
   power SKU -> 1 error, both reverted; final `Validate Content` clean, `Run All Checks` PASS).
   **B7 is complete** - the mock-era money path is finished.

## 6b. What is NOT in the base (moved to B10 - live store wiring, 2026-09-27)

`Monetisation.md` above describes the *logic* of the money path, and all of it ships behind two seams
(`IIapService`, `IAdService`) that today are only implemented by mocks. Because **MVP is defined as
store-submittable** (see `Checklist.md` §1p), the real wiring is its own step and its own risk:

| Obligation | Why it is mandatory | Home |
|---|---|---|
| Real rewarded ads + real billing | mock-only code cannot earn or be accepted | **B10** |
| **Restore Purchases** in the UI | Apple rejects a non-consumable IAP with no restore path | **B10** |
| iOS ATT + EEA consent flow | required once a real ad SDK collects identifiers | **B10** |
| `PrivacyInfo.xcprivacy` + Play Data Safety | both stores require the declaration; a mismatch is a rejection | **B10** / **B10b** |
| Ad failure must not burn a daily cap | a no-fill or a skip must leave the cap intact (ledger honesty stays) | **B10** |
| Product records + prices in both consoles | the `IapCatalog` SKU list is the single source to copy | **B10b** (Store Day-0) |

## 7. Explicitly not in the base

Season pass, timed bundles, gacha/pity, gear monetisation, "pay to skip stage", ads between fights, energy systems.
All of those either break the ledger rule or turn the loop into a chore.
