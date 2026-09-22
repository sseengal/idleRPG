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

1. `IIapService` + `MockIapService` + no-ads flag (proves the swap seam).
2. Daily streak + milestone gems (fixes gem scarcity first, so the sinks have something to spend).
3. Ad placements (double offline, instant claim) + per-day caps.
4. Permanent-multiplier + automation sinks (needs B5 and B6).
5. Validator guard + a shop readout in the dev overlay.

## 7. Explicitly not in the base

Season pass, timed bundles, gacha/pity, gear monetisation, "pay to skip stage", ads between fights, energy systems.
All of those either break the ledger rule or turn the loop into a chore.
