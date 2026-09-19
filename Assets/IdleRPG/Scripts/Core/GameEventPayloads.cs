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

        public EnemyDamagedInfo(double damage, double currentHealth, double maxHealth, bool isCritical = false)
        {
            Damage = damage;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            IsCritical = isCritical;
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