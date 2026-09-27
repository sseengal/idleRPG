using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Save;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Migration checks: old payloads must upgrade without losing anything, and without inventing a layout.
    /// </summary>
    public static partial class SaveRoundTripMenu
    {
        /// <summary>Old payloads must upgrade without losing anything, and without inventing a layout.</summary>
        private static bool CheckMigrations()
        {
            bool ok = true;

            // A v2 file has no partySlots: it must upgrade to v3 with an empty layout (read as default front row).
            SaveData v2 = SaveData.CreateDefault();
            v2.schemaVersion = 2;
            v2.partySlots = null;
            v2.gold = 500d;

            SaveData migratedV2 = SaveMigrations.Migrate(v2);

            if (migratedV2.schemaVersion != SaveData.CurrentVersion || migratedV2.partySlots == null)
            {
                Debug.LogError($"[SaveRoundTrip] v2 migration wrong: version={migratedV2.schemaVersion}, " +
                               $"slots={(migratedV2.partySlots == null ? "null" : "ok")}.");
                ok = false;
            }
            else if (migratedV2.partySlots.Count != 0)
            {
                Debug.LogError($"[SaveRoundTrip] v2 migration invented a layout ({migratedV2.partySlots.Count} slots); " +
                               "it must stay empty so the default front row applies.");
                ok = false;
            }
            else if (!Mathf.Approximately((float)migratedV2.gold, 500f))
            {
                Debug.LogError($"[SaveRoundTrip] v2 migration lost gold ({migratedV2.gold}).");
                ok = false;
            }
            else
            {
                Debug.Log("[SaveRoundTrip] v2 -> v3 migration: keeps values, adds an empty board.");
            }

            // A v3 file has no runBestStage: it must adopt the current stage (never invent a higher one) and stay
            // inside the lifetime best, because that number gates and prices the next ascension.
            SaveData v3 = SaveData.CreateDefault();
            v3.schemaVersion = 3;
            v3.currentStage = 12;
            v3.highestStageReached = 20;
            v3.runBestStage = 0;

            SaveData migratedV3 = SaveMigrations.Migrate(v3);

            if (migratedV3.schemaVersion != SaveData.CurrentVersion)
            {
                Debug.LogError($"[SaveRoundTrip] v3 migration left version {migratedV3.schemaVersion}.");
                ok = false;
            }
            else if (migratedV3.runBestStage != 12)
            {
                Debug.LogError($"[SaveRoundTrip] v3 migration set runBestStage {migratedV3.runBestStage}; " +
                               "expected the current stage (12).");
                ok = false;
            }
            else
            {
                Debug.Log("[SaveRoundTrip] v3 -> v4 migration: run best adopts the current stage.");
            }

            // A v4 file stores hero levels in fixed columns + a prestige list. It must convert to keyed records AND get the
            // one-time power compensation (Fix A): level 80 additive == level 25 compounding, so heroes keep their
            // relative power and enemies stop being one-shot. Core numbers (stage/gold/tokens) are untouched.
            SaveData v4 = SaveData.CreateDefault();
            v4.schemaVersion = 4;
            v4.powerCompensated = false;   // a real v4 file has no marker -> legacy, must be compensated
            v4.currentStage = 24;
            v4.highestStageReached = 24;
            v4.runBestStage = 24;
            v4.gold = 4793.29d;
            v4.prestigeTokens = 2d;
            v4.heroes = new List<HeroProgressRecord>
            {
                new HeroProgressRecord("hero_knight", 80, 80, 80),
                new HeroProgressRecord("hero_archer", 80, 80, 80),
                new HeroProgressRecord("hero_mage", 91, 80, 80)
            };
            v4.prestigeUpgrades = new List<PrestigeUpgradeRecord>
            {
                new PrestigeUpgradeRecord("Prestige_Gold", 0),
                new PrestigeUpgradeRecord("Prestige_Damage", 0),
                new PrestigeUpgradeRecord("Prestige_Health", 0)
            };

            SaveData migratedV4 = SaveMigrations.Migrate(v4);
            SaveData migratedV4Twice = SaveMigrations.Migrate(migratedV4);

            bool v4NumbersKept =
                migratedV4.schemaVersion == SaveData.CurrentVersion &&
                migratedV4.powerCompensated &&
                migratedV4.GetLevel("hero_knight.attack") == 25 &&   // floor(ln(1+0.1x80)/ln(1.09))
                migratedV4.GetLevel("hero_knight.health") == 25 &&
                migratedV4.GetLevel("hero_knight.defense") == 52 &&  // floor(ln(1+0.15x80)/ln(1.05))
                migratedV4.GetLevel("hero_archer.defense") == 52 &&
                migratedV4.GetLevel("hero_mage.attack") == 26 &&     // floor(ln(1+0.1x91)/ln(1.09))
                migratedV4.GetLevel("Prestige_Health") == 0 &&       // global tracks untouched
                migratedV4.currentStage == 24 &&
                migratedV4.highestStageReached == 24 &&
                migratedV4.runBestStage == 24 &&
                System.Math.Abs(migratedV4.gold - 4793.29d) < 0.001d &&
                migratedV4.prestigeTokens == 2d &&
                migratedV4.heroes.Count == 0 &&
                migratedV4.prestigeUpgrades.Count == 0 &&
                migratedV4Twice.GetLevel("hero_knight.attack") == 25 &&   // never compensated twice
                migratedV4Twice.GetLevel("hero_mage.attack") == 26;

            if (!v4NumbersKept)
            {
                Debug.LogError("[SaveRoundTrip] v4 -> v5 + power compensation lost value(s); check levels/powerEra.");
                ok = false;
            }
            else
            {
                Debug.Log($"[SaveRoundTrip] v4 -> v5 migration: {migratedV4.levels.Count} keyed level(s), " +
                          "additive-era compensation applied once (25/25/52, 26), core numbers preserved.");
            }

            // Regression for the EXACT bug the player hit (2026-09-25): a legacy v5 file that was stamped
            // "compounding" by the buggy string marker must STILL be recognised as legacy and compensated. The
            // marker now defaults to false, so the stray "powerEra" key is ignored and the file is fixed once.
            string legacyV5Json = "{\"schemaVersion\":5,\"currentStage\":28,\"powerEra\":\"compounding\"," +
                "\"levels\":[{\"key\":\"hero_knight.attack\",\"level\":80},{\"key\":\"hero_knight.defense\",\"level\":80}," +
                "{\"key\":\"Prestige_Gold\",\"level\":0},{\"key\":\"autoBuy\",\"level\":0}]}";
            SaveData legacyV5 = JsonUtility.FromJson<SaveData>(legacyV5Json);
            bool parsedAsLegacy = !legacyV5.powerCompensated;   // read BEFORE Migrate (it mutates and returns the same instance)
            SaveData fixedLegacy = SaveMigrations.Migrate(legacyV5);
            SaveData fixedTwice = SaveMigrations.Migrate(fixedLegacy);

            bool legacyFixOk =
                parsedAsLegacy &&
                fixedLegacy.powerCompensated &&
                fixedLegacy.GetLevel("hero_knight.attack") == 25 &&
                fixedLegacy.GetLevel("hero_knight.defense") == 52 &&
                fixedLegacy.GetLevel("Prestige_Gold") == 0 &&  // global tracks untouched
                fixedLegacy.GetLevel("autoBuy") == 0 &&
                fixedTwice.GetLevel("hero_knight.attack") == 25;  // never compensated twice

            if (!legacyFixOk)
            {
                Debug.LogError("[SaveRoundTrip] legacy v5 save was not compensated once (the player's one-shot bug).");
                ok = false;
            }
            else
            {
                Debug.Log("[SaveRoundTrip] legacy v5 with stray marker: recognised as additive era and compensated once.");
            }

            // A v1 file (no version at all) must still land on the current schema.
            SaveData v1 = SaveData.CreateDefault();
            v1.schemaVersion = 0;
            SaveData migratedV1 = SaveMigrations.Migrate(v1);

            if (migratedV1.schemaVersion != SaveData.CurrentVersion)
            {
                Debug.LogError($"[SaveRoundTrip] v1 migration left the version at {migratedV1.schemaVersion}.");
                ok = false;
            }
            else
            {
                Debug.Log("[SaveRoundTrip] v1 -> v3 migration: lands on the current schema.");
            }

            return ok;
        }

        /// <summary>
        /// If a real save exists, prove two things about it: the payload is stable through the *current* schema,
        /// and upgrading it from its on-disk version is additive-only (no line on disk is lost).
        /// </summary>
    }
}
