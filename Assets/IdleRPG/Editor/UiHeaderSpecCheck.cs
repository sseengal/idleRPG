using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Golden numbers for the top bar: the one element that is on every screen (the lobby borrows it).
    ///
    /// ELI5: a list of the exact sizes the top bar must have, plus a reader that measures the scene and says which
    /// number is wrong. It is the same "golden numbers" idea as the balance baseline (BalanceBaseline), applied to
    /// layout: the numbers are written down twice on purpose, so moving one is a decision, not an accident.
    ///
    /// Why it exists: the same header measured 1016 x 83 in battle and 1015 x 163 in the lobby, its icon was 56 in a
    /// 51-high chip, and its menu button was 110 x 51 - about 44 x 20 dp, half a finger. Nothing measured the built
    /// scene, so the docs drifted too (they claimed a 101-tall header and a 230-tall nav bar that no longer exists).
    ///
    /// Menu: Tools &gt; Idle RPG &gt; UI &gt; Check Header
    /// Batch: Unity -batchmode -quit -executeMethod IdleRPG.EditorTools.UiHeaderSpecCheck.Check
    ///
    /// The numbers below are the SPEC (2026-11-25). They also live in <see cref="UiSpace"/> and <see cref="UiTouch"/>;
    /// this file duplicates them deliberately, so a change to the ruler alone fails here instead of sliding through.
    /// </summary>
    public static class UiHeaderSpecCheck
    {
        // ------------------------------------------------------------------
        // The spec. Change these only with the owner, and update Docs/UI-Layout.md in the same commit.
        // ------------------------------------------------------------------
        /// <summary>One currency plate, one boost badge: 80 units (about 32 dp) - the icon plus 8 above and below.</summary>
        private const float SpecChipHeight = 80f;

        /// <summary>The anchor of the whole bar: 64 units (about 26 dp), the size a HUD currency glyph needs.</summary>
        private const float SpecChipIconSize = 64f;

        /// <summary>The slot an icon centres in: the icon plus one inset, so a wide gem and a narrow key share a line.</summary>
        private const float SpecIconSlot = 80f;

        /// <summary>The inset from a chip's side to its icon.</summary>
        private const float SpecChipInset = 16f;

        /// <summary>The bar is exactly one plate tall - there is no container behind it.</summary>
        private const float SpecBarHeight = 80f;

        /// <summary>The band: page margin + bar + one section gap. The page starts here.</summary>
        private const float SpecBandHeight = 128f;

        /// <summary>The green plus in a plate's corner: 32 units (about 13 dp).</summary>
        private const float SpecBadgeSize = 32f;

        /// <summary>Badge right edge: one icon inset + one icon slot from the plate's left edge (96).</summary>
        private const float SpecBadgeX = 96f;

        /// <summary>Badge bottom edge: half the icon below the plate's middle (-32), so it overlaps the icon.</summary>
        private const float SpecBadgeY = -32f;

        private const float Tolerance = 0.5f;

        /// <summary>The lobby borrows the header; its source is checked so it cannot re-anchor it again.</summary>
        private const string LobbySourcePath = "Assets/IdleRPG/Scripts/UI/StartScreenUI.cs";

        public struct Issue
        {
            public string Area;
            public string Message;
        }

        private static readonly List<Issue> Issues = new List<Issue>();

        [MenuItem("Tools/Idle RPG/UI/Check Header", priority = 12)]
        public static void Check()
        {
            Validate();

            foreach (Issue issue in Issues)
            {
                Debug.LogError($"[UiHeader] {issue.Message}");
            }

            Debug.Log(Issues.Count == 0
                ? "[UiHeader] HEADER: PASS - one bar on every screen, icon inside its chip, menu button at the touch minimum."
                : $"[UiHeader] HEADER: FAIL - {Issues.Count} golden number(s) do not match.");
        }

        /// <summary>True when every golden number matches. Used by <see cref="RegressionCheckMenu"/>.</summary>
        public static bool RunCheckWithResult()
        {
            foreach (Issue issue in Validate())
            {
                Debug.LogError($"[UiHeader] {issue.Message}");
            }

            if (Issues.Count > 0)
            {
                return false;
            }

            Debug.Log("[UiHeader] HEADER: PASS");
            return true;
        }

        private static List<Issue> Validate()
        {
            Issues.Clear();
            CheckRules();
            CheckScene();
            CheckLobbyBorrow();
            return Issues;
        }

        /// <summary>The numbers in the ruler must be the spec, or every screen inherits the change.</summary>
        private static void CheckRules()
        {
            Expect("UiSpace.ChipHeight", UiSpace.ChipHeight, SpecChipHeight);
            Expect("UiSpace.ChipIconSize", UiSpace.ChipIconSize, SpecChipIconSize);
            Expect("UiSpace.ChipIconInset", UiSpace.ChipIconInset, SpecChipInset);
            Expect("UiSpace.HeaderBarHeight", UiSpace.HeaderBarHeight, SpecBarHeight);
            Expect("UiSpace.HeaderBandHeight", UiSpace.HeaderBandHeight, SpecBandHeight);

            if (UiSpace.ChipIconSize > UiSpace.ChipHeight)
            {
                Add("UiSpace", "The chip icon is taller than its chip. The art will poke out of the box.");
            }

            if (UiTouch.IconButtonHitSize + Tolerance < UiTouch.MinTarget)
            {
                Add("UiTouch", $"The gear's tap square is {UiTouch.IconButtonHitSize}; the touch minimum is {UiTouch.MinTarget}.");
            }
        }

        /// <summary>Measures the built scene, which is the thing that actually drifts.</summary>
        private static void CheckScene()
        {
            RectTransform header = Find("/HUD/Canvas/SafeArea/Header");

            if (header == null)
            {
                Add("Scene", "No /HUD/Canvas/SafeArea/Header. Open Main.unity, or the header was renamed.");
                return;
            }

            RectTransform safeArea = header.parent as RectTransform;
            float canvasWidth = safeArea != null ? safeArea.rect.width : 1080f;

            // The bar draws NOTHING (owner, 2026-11-25): the plates are the only surfaces. A bar image means the old
            // light-blue container came back.
            UnityEngine.UI.Image bar = header.GetComponent<UnityEngine.UI.Image>();

            if (bar != null && bar.sprite != null && bar.enabled)
            {
                Add("Header", "The header draws a bar again. The currency plates are the only surfaces on it.");
            }

            Expect("Header height", header.rect.height, SpecBarHeight);
            Expect("Header width", header.rect.width, canvasWidth - 2f * UiSpace.PageMargin);

            if (Mathf.Abs(header.offsetMax.y + UiSpace.PageMargin) > Tolerance)
            {
                Add("Header", $"The bar sits {Mathf.Abs(header.offsetMax.y):0.#} units from the safe-area top; the page margin is {UiSpace.PageMargin}.");
            }

            RectTransform row = Find("/HUD/Canvas/SafeArea/Header/ChipRow");

            if (row == null)
            {
                Add("Header", "No ChipRow. The chips must sit in one layout row so they cannot overlap.");
            }
            else
            {
                Expect("ChipRow height", row.rect.height, SpecChipHeight);
                Expect("ChipRow width", row.rect.width, header.rect.width - (SpecChipIconSize + UiSpace.PageMargin));
            }

            CheckChips();

            RectTransform page = Find("/HUD/Canvas/SafeArea/BattlePage");

            if (page != null && Mathf.Abs(page.offsetMax.y + SpecBandHeight) > Tolerance)
            {
                Add("BattlePage", $"The page starts {Mathf.Abs(page.offsetMax.y):0.#} units from the safe-area top; the header band is {SpecBandHeight}.");
            }

            if (Find("/HUD/Canvas/SafeArea/Header/ChipRow/BoostChip") == null)
            {
                Add("Header", "The boost badge is not a row item. Floating it over the bar covers the gold chip.");
            }
        }

        private static void CheckChips()
        {
            string[] chips = { "GoldChip", "GemChip", "TokenChip" };

            foreach (string chipName in chips)
            {
                RectTransform chip = Find("/HUD/Canvas/SafeArea/Header/ChipRow/" + chipName);

                if (chip == null)
                {
                    Add("Header", $"No {chipName}. The row must hold three currency chips.");
                    continue;
                }

                Expect(chipName + " height", chip.rect.height, SpecChipHeight);

                UnityEngine.UI.Image plate = chip.GetComponent<UnityEngine.UI.Image>();

                if (plate == null || plate.sprite == null)
                {
                    Add(chipName, "The chip has no plate art. It must wear CurrencyContainer, not a flat block.");
                }
                else if (plate.type != UnityEngine.UI.Image.Type.Sliced)
                {
                    Add(chipName, "The chip's plate is not 9-sliced, so its rim will smear when the chip is wide.");
                }

                RectTransform icon = chip.Find("Icon") as RectTransform;

                if (icon == null)
                {
                    Add(chipName, "No Icon child.");
                }
                else
                {
                    Expect(chipName + " icon slot", icon.rect.width, SpecIconSlot);
                    Expect(chipName + " icon height", icon.rect.height, SpecChipIconSize);
                    Expect(chipName + " icon inset", icon.anchoredPosition.x, SpecChipInset);
                }

                TextMeshProUGUI value = chip.GetComponentInChildren<TextMeshProUGUI>(true);

                if (value == null)
                {
                    Add(chipName, "No Value label.");
                    continue;
                }

                Expect(chipName + " value size", value.fontSizeMax, UiType.Display);

                if (value.horizontalAlignment != HorizontalAlignmentOptions.Center)
                {
                    Add(chipName, "The value is not centred in its space. It should read as one block inside the plate.");
                }

                Transform badge = chip.Find("BuyBadge");

                if (badge == null)
                {
                    Add(chipName, "No BuyBadge. The lobby's shop affordance is missing.");
                }
                else
                {
                    RectTransform badgeRect = badge as RectTransform;
                    Expect(chipName + " badge width", badgeRect.rect.width, SpecBadgeSize);
                    Expect(chipName + " badge height", badgeRect.rect.height, SpecBadgeSize);
                    Expect(chipName + " badge right edge", badgeRect.anchoredPosition.x, SpecBadgeX);
                    Expect(chipName + " badge bottom edge", badgeRect.anchoredPosition.y, SpecBadgeY);

                    UnityEngine.UI.Image badgeImage = badge.GetComponent<UnityEngine.UI.Image>();

                    if (badgeImage == null || badgeImage.sprite == null)
                    {
                        Add(chipName, "The buy badge has no art. It must wear currency_plus.");
                    }
                    else if (badgeImage.raycastTarget)
                    {
                        Add(chipName, "The buy badge takes taps. The plate is the button; the badge is decoration.");
                    }
                }

                if (!value.enableAutoSizing)
                {
                    Add(chipName, "The value does not shrink. A six-digit number will spill out of the chip.");
                }
                else if (Mathf.Abs(value.fontSizeMin - UiType.Micro) > Tolerance)
                {
                    Add(chipName, $"The value shrinks to {value.fontSizeMin}, not to the Micro floor ({UiType.Micro}).");
                }
            }
        }

        /// <summary>
        /// The lobby borrows the header. If it re-anchors it, the bar changes size between screens - exactly the fault
        /// this check was written for, so it is guarded at the source as well as in the scene.
        /// </summary>
        private static void CheckLobbyBorrow()
        {
            if (!File.Exists(LobbySourcePath))
            {
                Add("Lobby", $"{LobbySourcePath} is missing; cannot check the header borrow.");
                return;
            }

            string source = File.ReadAllText(LobbySourcePath);
            Match method = Regex.Match(source,
                @"private void AttachSharedHeader\(\)[\s\S]*?headerRect\.SetParent\(box, false\);");

            if (!method.Success)
            {
                Add("Lobby", "AttachSharedHeader() no longer borrows the header the expected way.");
                return;
            }

            string body = method.Value;

            if (Regex.IsMatch(body, @"\banchor(Min|Max)\s*=") || Regex.IsMatch(body, @"\boffset(Min|Max)\s*="))
            {
                Add("Lobby", "AttachSharedHeader() re-anchors the header. One bar must keep one rect on every screen.");
            }
        }

        private static RectTransform Find(string hierarchyPath)
        {
            GameObject go = GameObject.Find(hierarchyPath);
            return go != null ? go.transform as RectTransform : null;
        }

        private static void Expect(string area, float actual, float expected)
        {
            if (Mathf.Abs(actual - expected) > Tolerance)
            {
                Add(area, $"{area} is {actual:0.#}; the golden number is {expected:0.#}.");
            }
        }

        private static void Add(string area, string message)
        {
            Issues.Add(new Issue { Area = area, Message = message });
        }
    }
}
