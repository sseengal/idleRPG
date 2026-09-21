using UnityEngine;

namespace IdleRPG.Core
{
    /// <summary>
    /// Payload for <see cref="GameEvents.EnemyDamaged"/>.
    /// A struct so the event can grow without breaking subscribers.
    /// </summary>
    public struct EnemyDamagedInfo
    {
        public double Damage;
        public double CurrentHealth;
        public double MaxHealth;
        public bool IsCritical;

        /// <summary>Which enemy in the wave was hit (0-based; the wave shows 1-3 enemies stacked).</summary>
        public int EnemyIndex;

        /// <summary>Party lane of the hero that dealt the damage (-1 when unknown).</summary>
        public int AttackerIndex;

        public EnemyDamagedInfo(double damage, double currentHealth, double maxHealth, bool isCritical = false, int attackerIndex = -1, int enemyIndex = 0)
        {
            Damage = damage;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            IsCritical = isCritical;
            AttackerIndex = attackerIndex;
            EnemyIndex = enemyIndex;
        }

        /// <summary>Remaining health as 0..1.</summary>
        public float NormalizedHealth
        {
            get
            {
                if (MaxHealth <= double.Epsilon)
                {
                    return 0f;
                }

                return Mathf.Clamp01((float)(CurrentHealth / MaxHealth));
            }
        }
    }
}