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

        /// <summary>A wave is a boss wave when it is the (normalWavesPerStage + 1)-th wave of a stage.</summary>
        public static bool IsBossWave(int wave, int normalWavesPerStage)
        {
            int safeNormalWaves = Mathf.Max(1, normalWavesPerStage);
            return wave >= safeNormalWaves + 1;
        }

        private static EnemyData PickFromPool(List<EnemyData> pool, int stage, int wave)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            int seed = (Mathf.Max(1, stage) - 1) * 31 + (Mathf.Max(1, wave) - 1);
            int index = seed % pool.Count;
            return pool[index];
        }
    }
}