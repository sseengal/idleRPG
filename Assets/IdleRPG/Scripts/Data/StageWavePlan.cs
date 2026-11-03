using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>One authored normal wave: one enemy type and how many of it.</summary>
    [Serializable]
    public struct StageWaveEntry
    {
        [Tooltip("1-based normal wave inside the stage.")]
        public int wave;

        [Tooltip("Enemy to spawn for this wave.")]
        public EnemyData enemy;

        [Tooltip("How many of this enemy. Clamped to 1-3.")]
        [Range(1, WaveConfig.MaxTeamSize)]
        public int count;

        public StageWaveEntry(int wave, EnemyData enemy, int count)
        {
            this.wave = wave;
            this.enemy = enemy;
            this.count = count;
        }
    }

    /// <summary>
    /// Optional authored wave list for one stage. When present, it replaces the rotating enemy pool for
    /// the listed normal waves. A stage without a plan keeps the old rotation exactly.
    /// </summary>
    [CreateAssetMenu(fileName = "StageWavePlan", menuName = "Idle RPG/Data/Stage Wave Plan", order = 12)]
    public sealed class StageWavePlan : ScriptableObject
    {
        [Tooltip("1-based stage this plan belongs to.")]
        [SerializeField] private int stage = 1;

        [Tooltip("How many normal waves this stage has. 0 = use BalanceConfig.NormalWavesPerStage.")]
        [SerializeField] private int normalWaveCount;

        [Tooltip("Normal waves only. The boss wave comes from WaveConfig.bossEnemies.")]
        [SerializeField] private List<StageWaveEntry> waves = new List<StageWaveEntry>();

        public int Stage => Mathf.Max(1, stage);

        public int NormalWaveCount => Mathf.Max(0, normalWaveCount);

        public IReadOnlyList<StageWaveEntry> Waves
        {
            get { return waves != null ? (IReadOnlyList<StageWaveEntry>)waves : Array.Empty<StageWaveEntry>(); }
        }

        /// <summary>Returns the authored enemy and count for one normal wave, or false when not listed.</summary>
        public bool TryGetWave(int waveNumber, out EnemyData enemy, out int count)
        {
            enemy = null;
            count = 0;

            if (waves == null || waveNumber < 1)
            {
                return false;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                StageWaveEntry entry = waves[i];

                if (entry.wave != waveNumber || entry.enemy == null || entry.count < 1)
                {
                    continue;
                }

                enemy = entry.enemy;
                count = Mathf.Clamp(entry.count, 1, WaveConfig.MaxTeamSize);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Appends every authored row for one wave, in order, until the team is full. Duplicate wave
        /// numbers are intentional: they mix two enemy types in one wave.
        /// </summary>
        public void AppendWave(int waveNumber, List<EnemyData> team, int maxTeamSize)
        {
            if (team == null || waves == null || waveNumber < 1 || team.Count >= maxTeamSize)
            {
                return;
            }

            for (int i = 0; i < waves.Count && team.Count < maxTeamSize; i++)
            {
                StageWaveEntry entry = waves[i];

                if (entry.wave != waveNumber || entry.enemy == null || entry.count < 1)
                {
                    continue;
                }

                int add = Mathf.Min(entry.count, maxTeamSize - team.Count);

                for (int slot = 0; slot < add; slot++)
                {
                    team.Add(entry.enemy);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only write used by the chapter seeder. Runtime never calls it.</summary>
        public void EditorSetContent(int stageNumber, int normalWaveCountForStage, params StageWaveEntry[] entries)
        {
            stage = Mathf.Max(1, stageNumber);
            normalWaveCount = Mathf.Max(0, normalWaveCountForStage);
            waves = new List<StageWaveEntry>(entries ?? Array.Empty<StageWaveEntry>());
        }
#endif

        private void OnValidate()
        {
            stage = Mathf.Max(1, stage);
            normalWaveCount = Mathf.Max(0, normalWaveCount);

            if (waves == null)
            {
                return;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                StageWaveEntry entry = waves[i];
                entry.wave = Mathf.Max(1, entry.wave);
                entry.count = Mathf.Clamp(entry.count, 1, WaveConfig.MaxTeamSize);
                waves[i] = entry;
            }
        }
    }
}
