# Refactor notes — how to prove nothing changed

> Working page for the code-shape pass (`Roadmap.md` §7b). Branch: `refactor/ui-kit`.
> Rule: one seam per commit, and a machine-checked "before" for every visual step.

---

## 1. Why pictures are not the proof (on this machine)

`ScreenCapture` and the editor capture tool returned a **black** image or a stale frame, because the Game view is not
rendering while the editor is unfocused. Do not treat a black PNG as "the UI is broken".

When the Game view is visible, a screenshot baseline is still the best proof. Use it then:

1. Play, wait for stage 1, press nothing.
2. Battle page is the default. Capture at 720×1280.
3. Then open PARTY, UPGRADES, ASCEND, SHOP and capture each.
4. Save under `Screenshots/refactor-baseline/` (gitignored) and compare the MD5s after the change.

A different hash is only meaningful if the game state is identical. Same stage, same wave, same save. When in doubt,
use §2 instead.

---

## 2. The layout fingerprint (works without rendering)

This walks the live UI and writes one line per RectTransform: path, anchors, position, size, active state, and for
text the font, size, auto-size flag. Sorted, so line order never matters.

**Run it in Play Mode, and force every panel to build first**, because rows are built lazily:

```csharp
var sc = UnityEngine.Object.FindFirstObjectByType<IdleRPG.UI.ScreenController>();
sc.ShowManagement(0); sc.ShowManagement(1); sc.ShowManagement(2); sc.ShowManagement(3); sc.ShowBattle();

var lines = new System.Collections.Generic.List<string>();
foreach (var rt in UnityEngine.Object.FindObjectsByType<UnityEngine.RectTransform>(UnityEngine.FindObjectsSortMode.None))
{
    if (rt == null) continue;
    string p = rt.name; var t = rt.parent; int d = 0;
    while (t != null && d < 40) { p = t.name + "/" + p; t = t.parent; d++; }
    var tmp = rt.GetComponent<TMPro.TMP_Text>();
    string txt = tmp != null ? $"font={tmp.font.name};size={tmp.fontSize:0.#};auto={tmp.enableAutoSizing}" : "";
    lines.Add($"{p}|min={rt.anchorMin.x:0.###},{rt.anchorMin.y:0.###}|max={rt.anchorMax.x:0.###},{rt.anchorMax.y:0.###}|pos={rt.anchoredPosition.x:0.##},{rt.anchoredPosition.y:0.##}|size={rt.sizeDelta.x:0.##},{rt.sizeDelta.y:0.##}|on={rt.gameObject.activeSelf}|{txt}");
}
lines.Sort(System.StringComparer.Ordinal);
System.IO.File.WriteAllText(System.IO.Path.GetFullPath("Screenshots/refactor-baseline/layout.txt"), string.Join("\n", lines));
return "rects=" + lines.Count;
```

Compare with:

```
diff Screenshots/refactor-baseline/layout-before.txt Screenshots/refactor-baseline/layout-after.txt
```

**Pass = empty diff.** Accepted exceptions: a value that is a live timer or a health number (do not include those in
the fingerprint), and lines for objects that did not exist before because a run was shorter.

The earlier attempt failed the comparison because the "before" run had not opened the management tabs, so the lazily
built rows were missing. Always run the snippet above verbatim, both times.

---

## 3. The gate for every refactor commit

| Check | Pass |
|---|---|
| Compile | zero errors, outside Play Mode |
| EditMode tests | **14 passed, 0 failed** |
| Regression menu | `[Regression] REGRESSION: PASS` (golden lines are warnings by decision, `Content.md` §10) |
| Fingerprint or screenshots | identical to the recorded baseline |
| Play check | 2 minutes: a swing lands on the strike frame, guard flash, death pose, one ability cast |

Record the result in the commit message, like the cleanup rule does.

---

## 4. Phase log

| Phase | State | Commit | Proof |
|---|---|---|---|
| 0 — branch + protocol | **done** | this page | protocol above |
| 1 — `Combat/` split into bridge + `Adapters/` | **done** | `00aeb17` | 8 files + 8 `.meta` moved (GUIDs kept), compile clean, 14/14 tests, gate PASS |
| 2 — one UI primitive kit | **not started** | — | needs the layout freeze: art/audio pass first |
| 3 — shared unit view | **not started** | — | needs Phase 2 green + phone pass |
| 4 — split `CombatSimulator` | optional | — | — |

### What Phase 1 changed

Nothing but paths. `Scripts/Combat/` is now the bridge only:

```
Combat/           CombatManager, CombatSimulator, CombatDirector
Combat/Adapters/  HeroCombatant, EnemyCombatant, EncounterFactory,
                  WaveComposition, Formation, CombatRewardCalculator,
                  SimRulesFactory, CombatScaling
```

No namespace change, no `asmdef` change, no serialized field touched. `git status` shows 16 renames, zero content
diffs.

---

## 5. Phase 2 starting point (for when layout is frozen)

Already true today, and it changes the plan for the better:

- `Scripts/UI/UiRuntime.cs` **already is** a primitive kit: `CreateNode`, `CreatePanel`, `CreateIcon`, `CreateText`,
  `CreateButton`, `CreateFilledImage`, `Stretch`, `Anchor`, `CenterOn`.
- `Editor/UiFactory.cs` (255 lines) is a **second** copy of the same kit for scene building.
- Runtime UI still calls `AddComponent<TextMeshProUGUI>` / `<Image>` / `<Button>` **15 times by hand**.

So Phase 2 is not "write a kit". It is: **delete the editor copy, point the builders at `UiRuntime`, and replace the
15 hand-built widgets.** That is smaller and safer than the original estimate.
