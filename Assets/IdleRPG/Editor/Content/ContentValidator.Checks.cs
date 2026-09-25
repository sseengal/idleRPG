using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// One check per content area: formation, encounters, wave recipe, heroes, enemies, party, waves, upgrades, asset-vs-spec and the balance band.
    /// </summary>
    public static partial class ContentValidator
    {
        // ------------------------------------------------------------------
        // Formation (Step 10a)
        // ------------------------------------------------------------------
        /// <summary>
        /// Board sanity: the asset exists, the party fits on it, the unlock lists are ordered and sane, and the
        /// row rules are inside their legal ranges. Catches a hand-edited FormationData before the sim does.
        /// </summary>
        private static void CheckFormation()
        {
            string path = "Assets/IdleRPG/Data/Config/Formation_Default.asset";
            FormationData formation = AssetDatabase.LoadAssetAtPath<FormationData>(path);

            if (formation == null)
            {
                Add(Severity.Error, "formation", $"No FormationData at {path}; run Tools > Idle RPG > Generate Data Assets.");
                return;
            }

            PartyConfig party = SceneWiringUtility.LoadPartyConfig();
            int heroes = party != null ? party.ValidHeroCount : 0;

            if (formation.SlotCount < heroes)
            {
                Add(Severity.Error, "formation",
                    $"Board has {formation.SlotCount} slot(s) but the party has {heroes} hero(es).");
            }

            if (formation.BackRowTargetWeight > 1f)
            {
                Add(Severity.Error, "formation",
                    $"backRowTargetWeight must be in [0, 1] but is {formation.BackRowTargetWeight}.");
            }

            Add(Severity.Info, "formation",
                $"board front {formation.FrontSlots} / back {formation.BackSlots} ({formation.SlotCount} slots, " +
                $"all usable), back-rank target weight {formation.BackRowTargetWeight:0.##}.");
        }

        // ------------------------------------------------------------------
        // Encounters (Step 11a)
        // ------------------------------------------------------------------
        /// <summary>
        /// Multi-enemy sanity: the team size must fit the portrait layout, and the wave budget knobs are reported so
        /// a balance change is visible in the report. With enemiesPerWave = 1 this is the parity baseline.
        /// </summary>
        private static void CheckEncounters()
        {
            string path = ContentSpecIO.ConfigFolder + "/BalanceConfig.asset";
            BalanceConfig balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(path);

            if (balance == null)
            {
                Add(Severity.Error, "encounters", $"No BalanceConfig at {path}.");
                return;
            }

            int rawMax = SoField.Int(balance, "maxEnemiesPerWave", balance.MaxEnemiesPerWave);
            int rawMin = SoField.Int(balance, "minEnemiesPerWave", balance.MinEnemiesPerWave);

            if (rawMax > BalanceConfig.HardEnemyCap || rawMin > BalanceConfig.HardEnemyCap)
            {
                Add(Severity.Warning, "encounters",
                    $"wave size is clamped to {BalanceConfig.HardEnemyCap} (asset says min {rawMin} / max {rawMax}); " +
                    "the portrait layout holds three.");
            }

            if (rawMin > rawMax)
            {
                Add(Severity.Warning, "encounters", $"minEnemiesPerWave ({rawMin}) is above maxEnemiesPerWave ({rawMax}).");
            }

            CheckWaveRecipe(balance);

            Add(Severity.Info, "encounters",
                $"wave size {balance.MinEnemiesPerWave}-{balance.MaxEnemiesPerWave}, recipe {WaveComposition.Describe(balance)}, " +
                $"wave budget HP x{balance.WaveHealthMultiplier:0.##} ATK x{balance.WaveAttackMultiplier:0.##} " +
                $"gold x{balance.WaveGoldMultiplier:0.##}.");
        }

        /// <summary
        /// The wave-size recipe must be usable: weights above zero, every count inside min..max, and the
        /// resulting average has to be at least 1 (it drives the offline payout).
        /// </summary>
        private static void CheckWaveRecipe(BalanceConfig balance)
        {
            var recipe = balance.WaveCountWeights;
            int total = 0;

            if (recipe.Count == 0)
            {
                Add(Severity.Warning, "encounters", "Wave-size recipe is empty; every wave falls back to minEnemiesPerWave.");
            }

            for (int i = 0; i < recipe.Count; i++)
            {
                WaveCountWeight entry = recipe[i];

                if (entry.weight < 0)
                {
                    Add(Severity.Error, "encounters", $"Wave-size weight for {entry.count} enemies is negative.");
                }
                else if (entry.weight > 0 && (entry.count < balance.MinEnemiesPerWave || entry.count > balance.MaxEnemiesPerWave))
                {
                    Add(Severity.Warning, "encounters",
                        $"Wave-size {entry.count} is outside {balance.MinEnemiesPerWave}..{balance.MaxEnemiesPerWave} and will never be used.");
                }
                else
                {
                    total += entry.weight;
                }
            }

            if (total <= 0)
            {
                Add(Severity.Error, "encounters", "Wave-size weights sum to zero; every wave falls back to minEnemiesPerWave.");
            }
            else if (balance.MeanEnemiesPerWave < 1d)
            {
                Add(Severity.Error, "encounters", $"Wave-size average is {balance.MeanEnemiesPerWave:0.##}; it must be at least 1.");
            }
        }

        // ------------------------------------------------------------------
        // Spec checks
        // ------------------------------------------------------------------
        private static void CheckHeroes(HeroSpecFile file)
        {
            if (file.heroes.Count == 0)
            {
                Add(Severity.Error, "heroes", "No heroes in the spec.");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<string> assets = new HashSet<string>();

            for (int i = 0; i < file.heroes.Count; i++)
            {
                HeroSpec hero = file.heroes[i];
                RequireId(hero.id, "heroes", hero.asset, ids, assets);

                if (hero.baseHealth <= 0f)
                {
                    Add(Severity.Error, "heroes", $"{hero.id}: baseHealth must be > 0 (is {hero.baseHealth}).");
                }

                if (hero.baseAttack <= 0f)
                {
                    Add(Severity.Error, "heroes", $"{hero.id}: baseAttack must be > 0 (is {hero.baseAttack}).");
                }

                if (hero.attackIntervalSec < 0.1f)
                {
                    Add(Severity.Error, "heroes", $"{hero.id}: attackIntervalSec must be >= 0.1 (is {hero.attackIntervalSec}).");
                }

                if (ContentSpecIO.FromHex(hero.tint, Color.clear).a <= 0f)
                {
                    Add(Severity.Warning, "heroes", $"{hero.id}: tint '{hero.tint}' is not a valid colour.");
                }
            }

            Add(Severity.Info, "heroes", $"{file.heroes.Count} heroes checked.");
        }

        private static void CheckEnemies(EnemySpecFile file)
        {
            if (file.enemies.Count == 0)
            {
                Add(Severity.Error, "enemies", "No enemies in the spec.");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<string> assets = new HashSet<string>();
            int bosses = 0;

            for (int i = 0; i < file.enemies.Count; i++)
            {
                EnemySpec enemy = file.enemies[i];
                RequireId(enemy.id, "enemies", enemy.asset, ids, assets);

                if (enemy.isBoss)
                {
                    bosses++;
                }

                if (enemy.baseHealth <= 0f)
                {
                    Add(Severity.Error, "enemies", $"{enemy.id}: baseHealth must be > 0 (is {enemy.baseHealth}).");
                }

                if (enemy.attackIntervalSec < 0.1f)
                {
                    Add(Severity.Error, "enemies", $"{enemy.id}: attackIntervalSec must be >= 0.1 (is {enemy.attackIntervalSec}).");
                }

                if (enemy.baseGoldDrop < 0f)
                {
                    Add(Severity.Error, "enemies", $"{enemy.id}: baseGoldDrop must be >= 0 (is {enemy.baseGoldDrop}).");
                }

                if (enemy.isBoss && (enemy.bossHealthMultiplier < 1f || enemy.bossGoldMultiplier < 1f))
                {
                    Add(Severity.Warning, "enemies", $"{enemy.id}: boss multipliers below 1 make a boss weaker than a normal enemy.");
                }

                // Step 11a: the two formation fields. A typo silently falls back to the default, so warn.
                if (!KnownTargetRules.Contains((enemy.targetRule ?? "").Trim().ToLowerInvariant()))
                {
                    Add(Severity.Warning, "enemies",
                        $"{enemy.id}: unknown targetRule '{enemy.targetRule}' (falls back to inherit). " +
                        $"Known: {string.Join(", ", KnownTargetRules)}.");
                }

            }

            if (bosses == 0)
            {
                Add(Severity.Error, "enemies", "No boss enemy defined; boss waves have nothing to spawn.");
            }

            Add(Severity.Info, "enemies", $"{file.enemies.Count} enemies checked ({bosses} boss).");
        }

        private static void CheckParty(PartySpecFile file, HeroSpecFile heroes)
        {
            HashSet<string> known = new HashSet<string>();
            for (int i = 0; i < heroes.heroes.Count; i++)
            {
                known.Add(heroes.heroes[i].id);
            }

            if (file.heroIds.Count == 0)
            {
                Add(Severity.Error, "party", "Party is empty.");
                return;
            }

            // The board (Step 10) decides how many heroes fit - not a hardcoded party size.
            FormationData formation = AssetDatabase.LoadAssetAtPath<FormationData>("Assets/IdleRPG/Data/Config/Formation_Default.asset");
            int boardSlots = formation != null ? formation.SlotCount : PartyConfig.FallbackPartySize;


            if (file.heroIds.Count > boardSlots)
            {
                Add(Severity.Warning, "party",
                    $"Party has {file.heroIds.Count} heroes; the formation board has {boardSlots} slot(s).");
            }

            HashSet<string> seen = new HashSet<string>();

            for (int i = 0; i < file.heroIds.Count; i++)
            {
                string id = file.heroIds[i];

                if (!known.Contains(id))
                {
                    Add(Severity.Error, "party", $"Lane {i} references unknown hero '{id}'.");
                }

                if (!seen.Add(id))
                {
                    Add(Severity.Error, "party", $"Hero '{id}' appears in the party twice.");
                }
            }

            Add(Severity.Info, "party", $"{file.heroIds.Count} lanes checked.");
        }

        private static void CheckWaves(WaveSpecFile file, EnemySpecFile enemies)
        {
            HashSet<string> known = new HashSet<string>();
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                known.Add(enemies.enemies[i].id);
            }

            RequirePool("waves.normal", file.normalEnemyIds, known);
            RequirePool("waves.boss", file.bossEnemyIds, known);

            Add(Severity.Info, "waves", $"normal {file.normalEnemyIds.Count}, boss {file.bossEnemyIds.Count}.");
        }

        private static void CheckUpgrades(UpgradeSpecFile file)
        {
            HashSet<string> statTypes = new HashSet<string>();
            HashSet<string> ids = new HashSet<string>();
            int compoundingTracks = 0;

            for (int i = 0; i < file.statUpgrades.Count; i++)
            {
                StatUpgradeSpec upgrade = file.statUpgrades[i];
                statTypes.Add((upgrade.statType ?? "").ToLowerInvariant());

                if (!ids.Add(upgrade.id))
                {
                    Add(Severity.Error, "upgrades", $"Duplicate stat upgrade id '{upgrade.id}'.");
                }

                if (upgrade.baseCost <= 0d)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: baseCost must be > 0 (is {upgrade.baseCost}).");
                }

                if (upgrade.costGrowth < 1f)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: costGrowth must be >= 1 (is {upgrade.costGrowth}).");
                }

                if (upgrade.statGainPerLevelFraction < 0f)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: statGainPerLevelFraction must be >= 0.");
                }

                // B3d: the mode decides whether power is linear (loses the race) or compounding (races it).
                // A silent fall back to additive is a balance change, so an unknown/missing value is reported
                // instead of being swallowed.
                string mode = (upgrade.effectMode ?? "").Trim().ToLowerInvariant();
                bool compounding = mode == "multiplicative" || mode == "compounding";

                if (mode.Length == 0)
                {
                    Add(Severity.Warning, "upgrades",
                        $"{upgrade.id}: spec has no effectMode (treated as additive). Re-run Export Specs From Assets.");
                }
                else if (!compounding && mode != "additive")
                {
                    Add(Severity.Error, "upgrades",
                        $"{upgrade.id}: unknown effectMode '{upgrade.effectMode}' (use \"additive\" or \"multiplicative\").");
                }
                else if (compounding && upgrade.statGainPerLevelFraction <= 0f)
                {
                    Add(Severity.Error, "upgrades",
                        $"{upgrade.id}: compounding with statGainPerLevelFraction 0 leaves the track dead.");
                }
                else if (compounding)
                {
                    compoundingTracks++;
                }
            }

            string[] required = { "attack", "health", "defense" };
            for (int i = 0; i < required.Length; i++)
            {
                if (!statTypes.Contains(required[i]))
                {
                    Add(Severity.Error, "upgrades", $"Missing the '{required[i]}' stat upgrade track.");
                }
            }

            HashSet<string> prestigeIds = new HashSet<string>();

            for (int i = 0; i < file.prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeSpec upgrade = file.prestigeUpgrades[i];

                if (!prestigeIds.Add(upgrade.id))
                {
                    Add(Severity.Error, "upgrades", $"Duplicate prestige upgrade id '{upgrade.id}'.");
                }

                if (upgrade.baseCostTokens <= 0d)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: baseCostTokens must be > 0.");
                }

                if (upgrade.effectPerLevel <= 0f)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: effectPerLevel must be > 0.");
                }
            }

            Add(Severity.Info, "upgrades",
                $"{file.statUpgrades.Count} stat + {file.prestigeUpgrades.Count} prestige tracks checked " +
                $"({compoundingTracks} compounding).");
        }

        // ------------------------------------------------------------------
        // Spec <-> asset agreement (catches hand-edited assets and orphan files)
        // ------------------------------------------------------------------
        private static void CheckAssetsAgainstSpecs(HeroSpecFile heroes, EnemySpecFile enemies)
        {
            HashSet<string> specHeroIds = new HashSet<string>();
            for (int i = 0; i < heroes.heroes.Count; i++)
            {
                specHeroIds.Add(heroes.heroes[i].id);
            }

            List<HeroData> heroAssets = ContentSpecIO.LoadAll<HeroData>(ContentSpecIO.HeroFolder);
            for (int i = 0; i < heroAssets.Count; i++)
            {
                string id = SoField.Text(heroAssets[i], "heroID", heroAssets[i].name);

                if (!specHeroIds.Contains(id))
                {
                    Add(Severity.Warning, "assets",
                        $"Hero asset '{heroAssets[i].name}' (id '{id}') has no spec entry; Generate would ignore it.");
                }
            }

            HashSet<string> specEnemyIds = new HashSet<string>();
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                specEnemyIds.Add(enemies.enemies[i].id);
            }

            List<EnemyData> enemyAssets = ContentSpecIO.LoadAll<EnemyData>(ContentSpecIO.EnemyFolder);
            for (int i = 0; i < enemyAssets.Count; i++)
            {
                string id = SoField.Text(enemyAssets[i], "enemyID", enemyAssets[i].name);

                if (!specEnemyIds.Contains(id))
                {
                    Add(Severity.Warning, "assets",
                        $"Enemy asset '{enemyAssets[i].name}' (id '{id}') has no spec entry; Generate would ignore it.");
                }
            }

            Add(Severity.Info, "assets", $"{heroAssets.Count} hero + {enemyAssets.Count} enemy assets scanned.");
        }

        // ------------------------------------------------------------------
        // Balance band (one bounded headless stage run, reusing the Balance Lab runner)
        // ------------------------------------------------------------------
        private static void CheckBalanceBand()
        {
            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");
            WaveConfig waves = BalanceLabMenu.Load<WaveConfig>("WaveConfig");
            PartyConfig party = BalanceLabMenu.Load<PartyConfig>("PartyConfig");

            if (balance == null || waves == null || party == null)
            {
                Add(Severity.Error, "balance", "Config assets missing; skipped the stage-1 band check.");
                return;
            }

            BalanceLabMenu.StageRun run = BalanceLabMenu.RunStage(balance, waves, party, 1, balance.CombatPaceMultiplier);

            if (run.Wiped)
            {
                Add(Severity.Error, "balance", "An unupgraded party wipes on stage 1.");
            }

            if (run.Seconds < MinStageOneSeconds || run.Seconds > MaxStageOneSeconds)
            {
                Add(Severity.Error, "balance",
                    $"Stage 1 takes {run.Seconds:0}s; target is {MinStageOneSeconds}-{MaxStageOneSeconds}s.");
            }

            Add(Severity.Info, "balance",
                $"Stage 1: {run.Seconds:0}s, {run.Kills} kills, {run.Gold:0} gold, " +
                $"{(run.Seconds <= 0d ? 0d : run.Gold / run.Seconds):0.00} gold/s.");
        }

        // ------------------------------------------------------------------
        // Asset references (B4a): every pool element must resolve
        // ------------------------------------------------------------------
        /// <summary>
        /// Catches the "recipe deleted, asset left pointing at it" bug.
        ///
        /// ELI5: runtime code is polite - if a wave's list holds a hole, it quietly spawns fewer monsters instead of
        /// crashing, so the only symptom is a smaller number (this is how a deleted enemy once turned 22 kills into 18).
        /// The inspector is not polite: a hole here is an error with a name and an index.
        /// </summary>
        private static void CheckAssetReferences(EnemySpecFile enemies, HeroSpecFile heroes)
        {
            HashSet<string> specEnemyIds = new HashSet<string>();
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                specEnemyIds.Add(enemies.enemies[i].id);
            }

            HashSet<string> specHeroIds = new HashSet<string>();
            for (int i = 0; i < heroes.heroes.Count; i++)
            {
                specHeroIds.Add(heroes.heroes[i].id);
            }

            WaveConfig waves = BalanceLabMenu.Load<WaveConfig>("WaveConfig");
            if (waves == null)
            {
                Add(Severity.Error, "refs", "No WaveConfig asset; run Tools > Idle RPG > Generate Data Assets.");
            }
            else
            {
                CheckEnemyPool("WaveConfig.normalEnemies", waves, "normalEnemies", specEnemyIds, true);
                CheckEnemyPool("WaveConfig.bossEnemies", waves, "bossEnemies", specEnemyIds, true);
            }

            PartyConfig party = BalanceLabMenu.Load<PartyConfig>("PartyConfig");
            if (party == null)
            {
                Add(Severity.Error, "refs", "No PartyConfig asset; run Tools > Idle RPG > Generate Data Assets.");
            }
            else
            {
                int slots = SoField.Count(party, "heroes");
                for (int i = 0; i < slots; i++)
                {
                    Object element = SoField.ElementAt(party, "heroes", i);
                    if (element == null)
                    {
                        Add(Severity.Error, "refs",
                            $"PartyConfig.heroes[{i}] is empty - a deleted hero asset leaves a hole; re-run Generate.");
                        continue;
                    }

                    HeroData hero = element as HeroData;
                    string id = hero != null ? SoField.Text(hero, "heroID", hero.name) : element.name;

                    if (!specHeroIds.Contains(id))
                    {
                        Add(Severity.Error, "refs",
                            $"PartyConfig.heroes[{i}] references '{id}', which is not in heroes.json (spec and assets out of sync).");
                    }
                }
            }
        }

        /// <summary>One enemy pool: no holes, every id known, no duplicates.</summary>
        private static void CheckEnemyPool(string label, Object owner, string field, HashSet<string> knownIds, bool requireNonEmpty)
        {
            int count = SoField.Count(owner, field);
            if (count == 0)
            {
                Add(requireNonEmpty ? Severity.Error : Severity.Warning, "refs", $"{label} is empty.");
                return;
            }

            HashSet<string> seen = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                Object element = SoField.ElementAt(owner, field, i);
                if (element == null)
                {
                    Add(Severity.Error, "refs",
                        $"{label}[{i}] is empty - a deleted enemy asset leaves a hole here (waves then spawn fewer enemies). " +
                        "Re-run Tools > Idle RPG > Content > Generate Assets From Specs.");
                    continue;
                }

                EnemyData enemy = element as EnemyData;
                string id = enemy != null ? enemy.EnemyID : element.name;

                if (string.IsNullOrEmpty(id))
                {
                    Add(Severity.Error, "refs", $"{label}[{i}] ('{element.name}') has no enemy id.");
                    continue;
                }

                if (!knownIds.Contains(id))
                {
                    Add(Severity.Error, "refs", $"{label}[{i}] references '{id}', which is not in enemies.json.");
                }

                if (!seen.Add(id))
                {
                    Add(Severity.Warning, "refs", $"{label} lists '{id}' more than once (weights that enemy twice).");
                }
            }
        }

        /// <summary>
        /// Every stat track owns one stat, buys something, the spec and asset agree, and the prestige tree exists.
        ///
        /// ELI5: the recipe card says what an upgrade should be; the asset is what the game uses. This check makes sure
        /// neither drifted - a hand-edited asset that disagrees with its card is caught here, not discovered by a player.
        /// </summary>
        private static void CheckUpgradeAssets()
        {
            UpgradeSpecFile spec = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);
            Dictionary<string, StatUpgradeSpec> specByAsset = new Dictionary<string, StatUpgradeSpec>();

            if (spec != null)
            {
                for (int i = 0; i < spec.statUpgrades.Count; i++)
                {
                    StatUpgradeSpec entry = spec.statUpgrades[i];
                    string assetName = string.IsNullOrEmpty(entry.asset) ? entry.id : entry.asset;
                    specByAsset[assetName] = entry;
                }
            }

            List<StatUpgradeData> statAssets = ContentSpecIO.LoadAll<StatUpgradeData>(ContentSpecIO.ConfigFolder);
            HashSet<string> statTypes = new HashSet<string>();

            for (int i = 0; i < statAssets.Count; i++)
            {
                StatUpgradeData upgrade = statAssets[i];
                string statType = upgrade.StatType.ToString().ToLowerInvariant();

                if (!statTypes.Add(statType))
                {
                    Add(Severity.Error, "refs",
                        $"Two stat upgrade assets both cover '{statType}' ({upgrade.name}); the resolver would use one and ignore the other.");
                }

                if (upgrade.StatGainPerLevelFraction <= 0f)
                {
                    Add(Severity.Error, "refs", $"{upgrade.name}: statGainPerLevelFraction is 0, so the track buys nothing.");
                }

                if (!specByAsset.TryGetValue(upgrade.name, out StatUpgradeSpec entry))
                {
                    Add(Severity.Error, "refs",
                        $"Stat upgrade asset '{upgrade.name}' has no entry in upgrades.json; Generate Assets From Specs would rewrite it silently.");
                    continue;
                }

                bool specUsesCompounding = (entry.effectMode ?? "").Trim().ToLowerInvariant() is "multiplicative" or "compounding";
                bool assetUsesCompounding = upgrade.EffectMode == StatEffectMode.Multiplicative;

                if (specUsesCompounding != assetUsesCompounding)
                {
                    Add(Severity.Error, "refs",
                        $"{upgrade.name}: effectMode is {(assetUsesCompounding ? "multiplicative" : "additive")} in the asset but " +
                        $"\"{(string.IsNullOrEmpty(entry.effectMode) ? "additive" : entry.effectMode)}\" in upgrades.json. " +
                        "Re-run Export/Generate so the card and the asset tell the same story.");
                }

                if (System.Math.Abs(upgrade.StatGainPerLevelFraction - entry.statGainPerLevelFraction) > 0.001f)
                {
                    Add(Severity.Warning, "refs",
                        $"{upgrade.name}: gain is {upgrade.StatGainPerLevelFraction:0.###} in the asset but {entry.statGainPerLevelFraction:0.###} in upgrades.json.");
                }
            }

            List<PrestigeUpgradeData> prestigeAssets = ContentSpecIO.LoadAll<PrestigeUpgradeData>(ContentSpecIO.ConfigFolder);
            if (prestigeAssets.Count == 0)
            {
                Add(Severity.Warning, "refs",
                    "No prestige upgrade assets found; ascension would pay tokens with nothing to spend them on.");
            }

            List<AutomationDef> automationAssets = ContentSpecIO.LoadAll<AutomationDef>(ContentSpecIO.ConfigFolder);
            HashSet<string> autoIds = new HashSet<string>();
            bool autoSpecEmpty = spec == null || spec.automationUpgrades.Count == 0;

            for (int i = 0; i < automationAssets.Count; i++)
            {
                AutomationDef card = automationAssets[i];
                string id = card.AutomationID;

                if (!autoIds.Add(id))
                {
                    Add(Severity.Error, "refs", $"Duplicate automation card id '{id}'.");
                }

                if (card.BaseCostTokens <= 0d)
                {
                    Add(Severity.Error, "refs", $"{card.name}: baseCostTokens must be > 0.");
                }

                if (!autoSpecEmpty && !SpecHasAutomation(spec, card.name))
                {
                    Add(Severity.Error, "refs",
                        $"Automation card '{card.name}' has no entry in tracks.json; Generate would rebuild it silently.");
                }
            }

            if (autoSpecEmpty && automationAssets.Count > 0)
            {
                Add(Severity.Warning, "refs",
                    "Automation cards exist but tracks.json lists none; re-run Export Specs From Assets.");
            }
        }

        private static bool SpecHasAutomation(UpgradeSpecFile spec, string assetName)
        {
            for (int i = 0; i < spec.automationUpgrades.Count; i++)
            {
                AutomationSpec entry = spec.automationUpgrades[i];
                if (entry == null)
                {
                    continue;
                }

                string candidate = string.IsNullOrEmpty(entry.asset) ? entry.id : entry.asset;
                if (candidate == assetName)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------------
        // Loop health (B4c): play the loop, do not just look at one stage
        // ------------------------------------------------------------------
        /// <summary>The frontier stage the robot must reach inside the time budget. A floor, not a ceiling - raise it as the base grows.</summary>
        private const int LoopHealthTargetStage = 25;

        /// <summary>How far the robot may play before we stop paying for the check (~1.3x the stage-30 cost measured in B3d).</summary>
        private const int LoopHealthMaxStage = 40;

        /// <summary>A wall that takes longer than this to break is a warning (a tuning signal), not an error.</summary>
        private const double LoopHealthWorstWallMinutes = 12d;

        /// <summary>
        /// Plays the loop (B4c) and asserts the frontier still moves.
        ///
        /// ELI5: the stage-1 band check cannot see a stall - an unupgraded party wipes at stage 4 whatever the balance is.
        /// This check lets the robot play: fight, wipe, fall back, farm, buy, push. If it cannot reach the target stage
        /// inside the budget, the balance has a ceiling that no amount of playing can fix, and the build fails.
        ///
        /// Honest limits: the robot only shops after a wipe (a live player buys mid-climb), so the cadence numbers are
        /// wall-phase numbers. They are reported, never asserted.
        /// </summary>
        private static void CheckLoopHealth()
        {
            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");
            WaveConfig waves = BalanceLabMenu.Load<WaveConfig>("WaveConfig");
            PartyConfig party = BalanceLabMenu.Load<PartyConfig>("PartyConfig");

            if (balance == null || waves == null || party == null)
            {
                Add(Severity.Error, "loop", "Config assets missing; skipped the loop-health check.");
                return;
            }

            StatUpgradeData[] statUpgrades =
            {
                BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_ATK"),
                BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_HP"),
                BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_DEF")
            };

            PrestigeUpgradeData[] prestigeUpgrades =
            {
                BalanceLabMenu.Load<PrestigeUpgradeData>("Prestige_Gold"),
                BalanceLabMenu.Load<PrestigeUpgradeData>("Prestige_Damage"),
                BalanceLabMenu.Load<PrestigeUpgradeData>("Prestige_Health")
            };

            for (int i = 0; i < statUpgrades.Length; i++)
            {
                if (statUpgrades[i] == null)
                {
                    Add(Severity.Error, "loop", "A stat upgrade asset is missing; the robot cannot buy power.");
                    return;
                }
            }

            ClimbSimulation.ClimbResult climb = ClimbSimulation.Simulate(
                balance, waves, party, statUpgrades, prestigeUpgrades, ClimbPolicy.Cheapest, LoopHealthMaxStage);

            if (climb.Stuck)
            {
                Add(Severity.Error, "loop",
                    $"the robot is STUCK on stage {climb.StuckStage} after {climb.TotalSeconds / 60d:0.0} min " +
                    $"({climb.Upgrades} upgrades bought, {climb.WorstWallSeconds / 60d:0.0} min on the worst wall). " +
                    "Income cannot out-grow the content curve - check each track's effectMode and gain.");
            }
            else if (climb.ReachedStage < LoopHealthTargetStage)
            {
                Add(Severity.Error, "loop",
                    $"the robot only reached stage {climb.ReachedStage} in {climb.TotalSeconds / 60d:0.0} min; " +
                    $"the loop needs stage {LoopHealthTargetStage} to stay healthy.");
            }
            else if (climb.BudgetExhausted)
            {
                Add(Severity.Warning, "loop",
                    $"the robot reached stage {climb.ReachedStage} but used the whole time budget while still climbing; " +
                    "the climb is slowing down.");
            }

            if (climb.WorstWallSeconds / 60d > LoopHealthWorstWallMinutes)
            {
                Add(Severity.Warning, "loop",
                    $"the worst wall took {climb.WorstWallSeconds / 60d:0.0} min to break (target < {LoopHealthWorstWallMinutes:0} min).");
            }

            double peakSeconds = 0d;
            int peakStage = 0;
            for (int i = 0; i < climb.ClearedStages.Count; i++)
            {
                if (climb.ClearedStages[i].Seconds > peakSeconds)
                {
                    peakSeconds = climb.ClearedStages[i].Seconds;
                    peakStage = climb.ClearedStages[i].Stage;
                }
            }

            Add(Severity.Info, "loop", string.Format(
                "robot (cheapest-buy) reached stage {0} in {1:0.0} min | walls {2}, worst {3:0.0} min | peak stage time {4:0}s (stage {5}) | {6} upgrades | median shopping gap {7}",
                climb.ReachedStage,
                climb.TotalSeconds / 60d,
                climb.Walls,
                climb.WorstWallSeconds / 60d,
                peakSeconds,
                peakStage,
                climb.Upgrades,
                climb.MedianShoppingGapSeconds < 0d
                    ? "n/a"
                    : string.Format("{0:0.0} min (wall phase only)", climb.MedianShoppingGapSeconds / 60d)));
        }
    }
}
