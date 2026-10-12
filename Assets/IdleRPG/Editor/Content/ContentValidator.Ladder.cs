using UnityEngine;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// The stage ladder's structural checks: 100 stages, 10 bands.
    ///
    /// ELI5: the ladder is a few hundred hand-written numbers in one file. A typo - a window that ends before it
    /// starts, a stage no enemy covers, two bosses on one stage - turns into an empty or wrong wave at a stage
    /// nobody reaches while developing. This reads the file and shouts at build time instead of waiting for a
    /// player to walk into it.
    ///
    /// It judges STRUCTURE, never balance: whether a fight is fair is the golden numbers' job plus the owner's walk
    /// of three bands. It runs automatically as part of <see cref="Validate"/> (so it is inside Run All Checks).
    /// </summary>
    public static partial class ContentValidator
    {
        /// <summary>Stages the ladder promises (the doc's MVP target).</summary>
        private const int LadderStages = 100;

        /// <summary>Stages per band: band 1 is stages 1-10, band 10 is 91-100.</summary>
        private const int LadderBandWidth = 10;

        /// <summary>
        /// Fewer than this many distinct enemies on a stage reads as a monotonous wave. Three is the design's own
        /// floor: each band owns a small themed cast (3-4 faces) instead of every earlier band bleeding into it.
        /// </summary>
        private const int MinPoolPerStage = 3;

        /// <summary>Above this, a stage repeats faces too rarely for the player to learn them.</summary>
        private const int MaxPoolPerStage = 6;

        private static void CheckLadder(EnemySpecFile enemies, WaveSpecFile waves)
        {
            if (enemies == null || enemies.enemies == null || enemies.enemies.Count == 0)
            {
                return;   // an empty roster is already reported by the spec checks
            }

            // 1) Every band the roster uses must have a scaling row, or its enemies silently fall back to 1x.
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                EnemySpec enemy = enemies.enemies[i];
                int band = BandOf(enemy);

                if (BandRow(enemies, band) == null)
                {
                    Add(Severity.Error, "ladder",
                        $"{enemy.id} starts at stage {enemy.firstStage} (band {band}) but the band table has no row {band}.");
                }

                // 2) A window that ends before it starts can never be available on any stage.
                if (enemy.lastStage != 0 && enemy.lastStage < enemy.firstStage)
                {
                    Add(Severity.Error, "ladder",
                        $"{enemy.id}: lastStage {enemy.lastStage} is before firstStage {enemy.firstStage}.");
                }
            }

            // 3) Stage by stage: is the pool populated and sized, and does exactly one boss cover it?
            for (int stage = 1; stage <= LadderStages; stage++)
            {
                int normal = 0;
                int bosses = 0;
                string firstBoss = null;

                for (int i = 0; i < enemies.enemies.Count; i++)
                {
                    EnemySpec enemy = enemies.enemies[i];

                    if (!Covers(enemy, stage))
                    {
                        continue;
                    }

                    if (enemy.isBoss)
                    {
                        // Only a boss that is IN THE POOL can spawn: a spare asset with a wide window is harmless.
                        if (InBossPool(waves, enemy.id))
                        {
                            bosses++;
                        }

                        if (firstBoss == null)
                        {
                            firstBoss = enemy.id;
                        }
                    }
                    else if (InPool(waves, enemy.id))
                    {
                        normal++;
                    }
                }

                if (normal < MinPoolPerStage)
                {
                    Add(Severity.Error, "ladder",
                        $"stage {stage}: only {normal} pooled enemy/enemies cover it (need {MinPoolPerStage}). " +
                        "Widen an enemy's window so the wave picker has something to choose from.");
                }

                if (normal > MaxPoolPerStage)
                {
                    Add(Severity.Warning, "ladder",
                        $"stage {stage}: pool of {normal} - wide. Each band owns a small cast (3-4); this stage mixes bands.");
                }

                if (bosses == 0)
                {
                    Add(Severity.Error, "ladder", $"stage {stage}: no boss covers it, but every stage ends in a boss wave.");
                }
                else if (bosses > 1)
                {
                    Add(Severity.Error, "ladder",
                        $"stage {stage}: {bosses} bosses cover it (first is {firstBoss}) - the boss wave would rotate between them.");
                }
            }

            // 4) The readout the doc asks for: the pool size per band, and anything left out of the pool.
            int bands = (LadderStages + LadderBandWidth - 1) / LadderBandWidth;

            for (int band = 1; band <= bands; band++)
            {
                int first = (band - 1) * LadderBandWidth + 1;
                int last = Mathf.Min(LadderStages, band * LadderBandWidth);
                int pool = 0;

                for (int i = 0; i < enemies.enemies.Count; i++)
                {
                    EnemySpec enemy = enemies.enemies[i];

                    if (!enemy.isBoss && Covers(enemy, first) && InPool(waves, enemy.id))
                    {
                        pool++;
                    }
                }

                Add(Severity.Info, "ladder", $"band {band} (stages {first}-{last}): pool of {pool} at stage {first}.");
            }

            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                EnemySpec enemy = enemies.enemies[i];

                if (!enemy.isBoss && !InPool(waves, enemy.id))
                {
                    Add(Severity.Info, "ladder", $"{enemy.id} is in the roster but not in the normal pool (summoned only?).");
                }
                else if (enemy.isBoss && !InBossPool(waves, enemy.id))
                {
                    Add(Severity.Info, "ladder", $"{enemy.id} is a boss but not in the boss pool - a spare that cannot spawn.");
                }
            }
        }

        private static int BandOf(EnemySpec enemy)
        {
            return (Mathf.Max(1, enemy.firstStage) - 1) / LadderBandWidth + 1;
        }

        private static EnemyBandSpec BandRow(EnemySpecFile file, int band)
        {
            if (file.bands == null)
            {
                return null;
            }

            for (int i = 0; i < file.bands.Count; i++)
            {
                if (file.bands[i] != null && file.bands[i].band == band)
                {
                    return file.bands[i];
                }
            }

            return null;
        }

        private static bool Covers(EnemySpec enemy, int stage)
        {
            return stage >= Mathf.Max(1, enemy.firstStage) && (enemy.lastStage == 0 || stage <= enemy.lastStage);
        }

        private static bool InBossPool(WaveSpecFile waves, string id)
        {
            if (waves == null || waves.bossEnemyIds == null)
            {
                return false;
            }

            for (int i = 0; i < waves.bossEnemyIds.Count; i++)
            {
                if (waves.bossEnemyIds[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InPool(WaveSpecFile waves, string id)
        {
            if (waves == null || waves.normalEnemyIds == null)
            {
                return false;
            }

            for (int i = 0; i < waves.normalEnemyIds.Count; i++)
            {
                if (waves.normalEnemyIds[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
