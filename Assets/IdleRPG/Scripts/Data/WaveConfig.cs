using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Enemy composition of a stage: a rotating pool of normal enemies plus a boss.
    /// Stage 1 always uses the top of each list, higher stages rotate deterministically.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Idle RPG/Data/Wave Config", order = 11)]
    public class WaveConfig : ScriptableObject
    {
        [Header("Normal Enemies")]
        [Tooltip("Rotated across normal waves. Null entries are skipped with a warning.")]
        [SerializeField] private List<EnemyData> normalEnemies = new List<EnemyData>();

        [Header("Bosses")]
        [Tooltip("Rotated across boss waves. Falls back to the last normal enemy if empty.")]
        [SerializeField] private List<EnemyData> bossEnemies = new List<EnemyData>();

        public int NormalEnemyCount => normalEnemies == null ? 0 : normalEnemies.Count;

        public int BossEnemyCount => bossEnemies == null ? 0 : bossEnemies.Count;

        /// <summary>
        /// Returns the enemy for a given stage/wave, or null when nothing is configured.
        /// Deterministic: same inputs always resolve to the same enemy.
        /// </summary>
        /// <param name="stage">Current stage, 1-based.</param>
        /// <param name="wave">Current wave, 1-based.</param>
        /// <param name="normalWavesPerStage">How many normal waves precede the boss wave.</param>
        public EnemyData GetEnemyFor(int stage, int wave, int normalWavesPerStage)
        {
            bool isBossWave = IsBossWave(wave, normalWavesPerStage);
            List<EnemyData> pool = isBossWave ? bossEnemies : normalEnemies;

            EnemyData picked = PickFromPool(pool, stage, wave);

            if (picked == null && isBossWave)
            {
                // Boss list empty -> fall back to a normal enemy so the run can continue.
                picked = PickFromPool(normalEnemies, stage, wave);
                Debug.LogWarning($"[WaveConfig] No boss configured for stage {stage} wave {wave}; falling back to a normal enemy.");
            }

            if (picked == null)
            {
                Debug.LogError("[WaveConfig] No enemies configured at all. Assign EnemyData assets in the inspector.");
            }

            return picked;
        }

        /// <summary>
        /// The enemy team for one wave: the wave's normal pick plus its rotation neighbours.
        ///
        /// ELI5: the wave already knows which enemy it would have used on its own. A multi-enemy wave keeps that
        /// enemy first and fills the remaining slots with the next ones in the same rotation, so waves get variety
        /// for free and the first enemy of every wave is exactly the one it would have been before.
        ///
        /// Deterministic from (stage, wave) - same inputs, same team - which is why the save file needs no
        /// composition data (no schema change). A team is smaller than requested only when the pool is smaller.
        /// </summary>
        /// <param name="count">How many enemies the wave should hold (1 = the single-enemy behaviour).</param>
        public List<EnemyData> GetEnemiesFor(int stage, int wave, int normalWavesPerStage, int count)
        {
            List<EnemyData> team = new List<EnemyData>();

            EnemyData primary = GetEnemyFor(stage, wave, normalWavesPerStage);
            if (primary == null)
            {
                return team;
            }

            team.Add(primary);

            int wanted = Mathf.Clamp(count, 1, MaxTeamSize);
            if (wanted <= 1)
            {
                return team;
            }

            List<EnemyData> pool = IsBossWave(wave, normalWavesPerStage) ? bossEnemies : normalEnemies;
            if (pool == null || pool.Count == 0)
            {
                return team;
            }

            int start = PoolIndexOf(pool, stage, wave);

            for (int step = 1; step <= pool.Count && team.Count < wanted; step++)
            {
                EnemyData candidate = pool[(start + step) % pool.Count];

                if (candidate == null || candidate == primary)
                {
                    continue;
                }

                team.Add(candidate);
            }

            return team;
        }

        /// <summary>A wave is a boss wave when it is the (normalWavesPerStage + 1)-th wave of a stage.</summary>
        public static bool IsBossWave(int wave, int normalWavesPerStage)
        {
            int safeNormalWaves = Mathf.Max(1, normalWavesPerStage);
            return wave >= safeNormalWaves + 1;
        }

        /// <summary>Team size ceiling (mirrors <see cref="BalanceConfig.MaxEnemiesPerWave"/> and the sim cap).</summary>
        public const int MaxTeamSize = 3;

        private static int PoolIndexOf(List<EnemyData> pool, int stage, int wave)
        {
            int seed = (Mathf.Max(1, stage) - 1) * 31 + (Mathf.Max(1, wave) - 1);
            return seed % pool.Count;
        }

        private static EnemyData PickFromPool(List<EnemyData> pool, int stage, int wave)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            return pool[PoolIndexOf(pool, stage, wave)];
        }
    }
}