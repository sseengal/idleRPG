# Leaderboard MVP — design + build record

> (2026-10-04). Minimal global ranks: mock field, no accounts/backend. Entry point is a trophy button on
> the Battle HUD top bar; the board opens as a docked sheet over the battle page.

---

## Decisions (locked)

- **Entry point: Battle HUD top bar trophy button** (runtime-built by `HudHeaderUI`; no scene edits). The
  bottom nav stays at 4 items (Party-Page design note); a global meta feature reads naturally from the
  head-up screen, not from a management tab.
- **Mock data.** There is no identity/backend yet. A deterministic pool of 40 mock players is generated at
  runtime and merged with the local player's live numbers. The real backend later replaces the same surface
  (`LeaderboardService.Refresh`).
- **Row = rank · tag · party level · best stage.**
  - tag: mock tags from a fixed pool; the local player shows **"YOU"** (with a mark on its row).
  - party level = **total hero levels** (`StatResolver.TotalHeroLevels` — already exists, grows with play).
  - best stage = the game's all-time `HighestStageReached`; rank = position after sorting by best stage.
- Equal stages keep the mock player ahead of the local row (stable sort), so the climb past a tie is
  visible.
- The sheet is informational; rows are not tappable. Refreshes on open and on every `StageChanged` while
  visible.

## Code map

```
IdleRPG.Scripts/Leaderboard/
  LeaderboardEntry.cs    tag / partyLevel / maxStage / IsYou
  LeaderboardService.cs  mock pool + the live "YOU" row; Ranked list; Refresh() + Changed
IdleRPG.Scripts/UI/
  LeaderboardSheetUI.cs  docked sheet: header ("you are #k of n"), column headers, top-12 rows,
                         the player's row pinned/highlighted below the cut
IdleRPG.Scripts/UI/HudHeaderUI.cs
  runtime "RANKS" button (right edge of the top bar) -> Show() the sheet (created once under the canvas)
IdleRPG.Scripts/Core/GameManager.cs / Wiring / GameContext
  Leaderboard service created alongside Gear; exposed on GameManager + GameContext
```

## Build steps (executed)

1. `LeaderboardEntry` + `LeaderboardService` (mock pool, live-player merge, stable sort, `Changed`).
2. `LeaderboardSheetUI`: docked panel; build in `Build()`, rebuild rows in `Show()`, subscribe
   `StageChanged` for live refresh.
3. `HudHeaderUI`: runtime trophy button ("RANKS") that lazily builds the sheet under the HUD canvas.
4. `GameManager` wiring: service + context exposure.
5. Verification + docs.

## Verified (play, live probes)

- Button renders in the header; invoking it creates the sheet with `GLOBAL RANKS · you are #k of 41`.
- Rows show # / tag / lvl / best stage; the YOU row is highlighted and pinned below the top cut.
- Your row's party level = total hero levels; best stage = `HighestStageReached`; rank follows live stage
  changes (sheet refreshes via `StageChanged`).

## Mock -> real roadmap (recorded, not built)

Accounts/identity → per-player rows from a service; season window + weekly reset; "best rank this season";
band vs exact position; EL/spy friend-lists; cheat/verification caveats (stage must come from server-verified
progression). All swaps isolated behind `LeaderboardService.Refresh`/`Ranked`; the UI does not change.