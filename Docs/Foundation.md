# Foundation — the locked architecture

> **Status:** Phase 0 complete, 28 Oct 2026.
> **Scope:** the decisions that every later visual and feature change must follow.
> **Parent:** `Architecture.md`. This page does not repeat balance numbers or screen measurements.

---

## 1. The decision

The long-term route is:

**URP 2D + camera-rendered battle stage + hybrid prefabs + services + instance event hub.**

The cheaper route was rejected: UI-only arena effects cannot be clipped to the battle viewport.

## 2. What each layer owns

| Layer | Owns | Does not own |
|---|---|---|
| `IdleRPG.Sim` | Deterministic combat, encounters, stats | Unity objects, scenes, UI |
| Application services | Run, stage flow, combat, camp, economy, save | Drawing, layout, input |
| Battle stage | Backdrop, heroes, enemies, damage numbers, camp view | Header, nav, ability strip, log |
| HUD overlay | Header, ability strip, log, nav, modals | Battle-stage effects |
| Data assets | Balance, content, tuning | Runtime state |

## 3. Pipeline truth

- Unity 6000.6.0f1.
- URP 2D is the only pipeline.
- Mobile quality uses `Assets/Settings/Mobile_RPAsset.asset` with `Mobile_Renderer2D.asset`.
- PC quality uses `Assets/Settings/PC_RPAsset.asset` with `PC_Renderer2D.asset`.
- The old 3D `Mobile_Renderer.asset` and `PC_Renderer.asset` are removed, not kept as a fallback.
- The arena is still Screen Space Overlay UI in Phase 0. Phase 1 moves it to a dedicated stage camera and RenderTexture.

## 4. Non-negotiable rules

1. One pipeline. Never run Built-in and URP together.
2. One battle surface. Effects apply to the stage RenderTexture, never to the whole screen.
3. No buttons inside the battle stage. Input stays in the HUD overlay.
4. Prefabs for reusable views. The scene builder owns the shell, bands and safe areas.
5. `GameManager` is a facade. New flow goes into a service.
6. Cross-system events use the event hub. Local UI actions use direct calls.
7. Save changes ship with a migration and a round-trip test.
8. Every phase stays shippable.

## 5. Phases

| Phase | Work | Done when |
|---|---|---|
| 0 | Truth pass, URP 2D renderers, baseline | Look unchanged; build and checks pass |
| 1 | Battle stage camera + RenderTexture | Battle page pixel-matched; effects only inside the stage |
| 2 | Prefab hybrid | Hero, enemy, ability and camp widgets editable without code |
| 3 | Services + event hub | Run and camp flow testable without the HUD |
| 4 | Data split + save migrations | Old saves load; content is data-only |
| 5 | Tests and polish | PlayMode smoke, screenshots, performance, a11y, l10n |

## 6. Phase 0 record

- Baseline captured at 1080 × 1920 before the pipeline switch.
- Both quality levels now use a 2D renderer.
- `Main Camera` still relies on URP's runtime-added camera data; Phase 1 adds the dedicated stage camera.
- Docs corrected: save v11, current page bands, pipeline.
- No gameplay code changed.
