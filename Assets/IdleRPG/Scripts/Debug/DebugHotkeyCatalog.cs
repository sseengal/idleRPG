using System.Text;
using UnityEngine;

namespace IdleRPG.Debugging
{
    /// <summary>One keyboard shortcut: the key as printed, and what it does.</summary>
    public readonly struct DebugHotkeyEntry
    {
        public readonly string Key;
        public readonly string Action;

        public DebugHotkeyEntry(string key, string action)
        {
            Key = key;
            Action = action;
        }
    }

    /// <summary>
    /// The one list of dev hotkeys, so the code, the on-screen overlay and the README cannot drift apart.
    ///
    /// ELI5: the cheat keys used to be listed in three places (the script comment, the README and nowhere in
    /// game), so any forgotten key simply vanished from the docs. There is now one catalogue: the F3 overlay
    /// prints it, a debug menu item prints it, and the README table is written from it.
    ///
    /// Amounts are deliberately not part of the descriptions - they are inspector fields on
    /// <see cref="DebugHotkeys"/> and would silently rot here.
    /// </summary>
    public static class DebugHotkeyCatalog
    {
        /// <summary>Every dev hotkey, in the order the overlay prints them.</summary>
        public static readonly DebugHotkeyEntry[] All =
        {
            new DebugHotkeyEntry("Tab", "cycle page (Battle / Upgrades / Ascend / Shop)"),
            new DebugHotkeyEntry("1 / 2", "+1 ATK / +10 ATK for every hero"),
            new DebugHotkeyEntry("3 / 4", "+10 HP / +10 DEF for every hero"),
            new DebugHotkeyEntry("G / T / C", "grant test gold / tokens / gems"),
            new DebugHotkeyEntry("B", "watch ad for the gold boost"),
            new DebugHotkeyEntry("A / R / S", "ascend / retry after defeat / skip a stage"),
            new DebugHotkeyEntry("F", "buy instant income (gem sink)"),
            new DebugHotkeyEntry("L", "log live state to the console"),
            new DebugHotkeyEntry("F3", "toggle this overlay"),
            new DebugHotkeyEntry("F5", "save now"),
            new DebugHotkeyEntry("F9", "delete the save"),
            new DebugHotkeyEntry("F10", "rewind the logout clock 3h and re-run the payout"),
        };

        /// <summary>Appends the legend as text lines; used by the overlay and the debug menu.</summary>
        public static void AppendLegend(StringBuilder builder, string header = "DEV HOTKEYS  (dev builds only)")
        {
            if (builder == null)
            {
                return;
            }

            builder.AppendLine(header);

            for (int i = 0; i < All.Length; i++)
            {
                // Padded so the actions line up in a monospaced-ish TMP label.
                builder.AppendLine($"{All[i].Key,-11} {All[i].Action}");
            }
        }

        /// <summary>Convenience for menus/logs that want the whole legend as one string.</summary>
        public static string BuildLegend(string header = "DEV HOTKEYS  (dev builds only)")
        {
            StringBuilder builder = new StringBuilder(560);
            AppendLegend(builder, header);
            return builder.ToString().TrimEnd();
        }

        /// <summary>Convenience for one-line logging (only needed while the overlay is closed).</summary>
        public static void LogLegend()
        {
            Debug.Log("[DebugHotkeys] " + BuildLegend().Replace("\n", " | "));
        }
    }
}
