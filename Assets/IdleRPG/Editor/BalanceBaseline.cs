using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// The recorded golden numbers, in one place.
    ///
    /// ELI5: before this, the parity check printed numbers and a human compared them with the doc - so a change that
    /// halved everyone's damage still printed "PASS". Now the numbers live here as the expectation, and the regression
    /// command compares against them. Re-baselining is a deliberate edit to this file, with a date and a reason.
    ///
    /// Rules:
    /// 1. Only update <see cref="Recorded"/> when the change is intended and verified by hand.
    /// 2. Keep the tolerance tight (2%). Loosening it is how a parity net quietly stops catching anything.
    /// </summary>
    public static class BalanceBaseline
    {
        /// <summary>Relative tolerance for every comparison (±2%).</summary>
        public const double Tolerance = 0.02d;

        /// <summary>When and why these numbers were recorded - the last deliberate re-baseline.</summary>
        public const string Recorded = "2026-09-25 (B3d compounding upgrades; level 0 is unchanged, so parity holds)";

        /// <summary>Baseline stage 1 clear time at pace x1.0 (seconds).</summary>
        public const double StageOneSecondsPaceOne = 80d;

        /// <summary>Baseline stage 1 clear time at the shipped pace x1.6 (seconds).</summary>
        public const double StageOneSecondsShipped = 126d;

        /// <summary>Baseline kills in stage 1 with the wave-size recipe (1 x25, 2 x50, 3 x25).</summary>
        public const int StageOneKills = 22;

        /// <summary>Baseline gold paid by stage 1 (not affected by the wave-size recipe).</summary>
        public const double StageOneGold = 272d;

        /// <summary>Baseline stage 1 gold per second at the shipped pace.</summary>
        public const double StageOneGoldPerSecondShipped = 2.16d;

        /// <summary>True when the measured value is inside <paramref name="tolerance"/> of the recorded one.</summary>
        public static bool Matches(double actual, double expected, double tolerance = Tolerance)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual))
            {
                return false;
            }

            double allowed = System.Math.Abs(expected) * (tolerance <= 0d ? Tolerance : tolerance);
            return System.Math.Abs(actual - expected) <= allowed;
        }

        /// <summary>One PASS/FAIL line for the report, plus the delta so a drift is visible, not just "wrong".</summary>
        public static bool Compare(string label, double actual, double expected, out string line)
        {
            bool ok = Matches(actual, expected);
            double delta = expected == 0d ? 0d : (actual - expected) / expected * 100d;

            line = string.Format("{0} {1}: {2:0.###} (baseline {3:0.###}, {4:+0.0;-0.0;0.0}%)",
                ok ? "[Golden] ok" : "[Golden] FAIL", label, actual, expected, delta);

            return ok;
        }
    }
}
