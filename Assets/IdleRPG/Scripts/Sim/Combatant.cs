namespace IdleRPG.Sim
{
    public enum CombatantSide
    {
        Party = 0,
        Enemy = 1
    }

    /// <summary>Formation row. Heroes and enemies both use it (Step 10 adds the swap UI).</summary>
    public enum CombatRow
    {
        Front = 0,
        Back = 1
    }

    /// <summary>
    /// One participant in a fight - the "character sheet".
    ///
    /// ELI5: heroes and enemies used to be two different kinds of object with copy-pasted code. This is the
    /// one sheet both use: name, stats, health, position, attack timer. Anything that later has to touch
    /// *both* sides (burning, shields, taunts, targeting rules) can now be written once.
    ///
    /// Pure C#: it knows nothing about Unity, sprites or the save file. The runtime subclasses
    /// (`HeroCombatant`, `EnemyCombatant`) only add the art/data reference.
    /// </summary>
    public class Combatant
    {
        private double attackTimer;

        public Combatant(string id, string displayName, CombatantSide side, StatBlock stats, double attackIntervalSec, double minAttackIntervalSec = 0.1d)
        {
            Id = string.IsNullOrEmpty(id) ? "unknown" : id;
            DisplayName = string.IsNullOrEmpty(displayName) ? Id : displayName;
            Side = side;
            Stats = stats ?? new StatBlock();
            Stats.Sanitize();
            Stats.MaxHealth = Stats.MaxHealth < 1d ? 1d : Stats.MaxHealth;

            MinAttackIntervalSec = minAttackIntervalSec < 0.01d ? 0.01d : minAttackIntervalSec;
            AttackIntervalSec = ClampInterval(attackIntervalSec);

            CurrentHealth = Stats.MaxHealth;
            Row = CombatRow.Front;
            RestartTimer();
        }

        // --- Identity ---
        public string Id { get; private set; }

        public string DisplayName { get; private set; }

        public CombatantSide Side { get; private set; }

        public bool IsBoss { get; set; }

        /// <summary>Gold paid out when this combatant dies (0 for heroes).</summary>
        public double GoldReward { get; set; }

        // --- Position (formation) ---
        public CombatRow Row { get; set; }

        public int Column { get; set; }

        /// <summary>Lane index (party) or enemy index - the value events and the UI use.</summary>
        public int SlotIndex { get; set; }

        // --- Stats & health ---
        public StatBlock Stats { get; private set; }

        public double CurrentHealth { get; private set; }

        /// <summary>Absorbs damage before health (the pipeline fills this from Step 12 on).</summary>
        public double Shield { get; set; }

        public double AttackIntervalSec { get; private set; }

        /// <summary>Aggro weight used by threat-based targeting from Step 11 on.</summary>
        public double Threat { get; set; }

        /// <summary>
        /// This combatant's own targeting rule, if its archetype has a preference.
        /// null = fall back to the side rule the fight was started with (today's behaviour for every unit).
        /// </summary>
        public TargetRule? TargetRule { get; set; }

        public double MinAttackIntervalSec { get; private set; }

        public double MaxHealth => Stats.MaxHealth;

        public double Attack => Stats.Attack;

        public double Defense => Stats.Defense;

        public bool IsAlive => CurrentHealth > 0d;

        public double HealthPercent => MaxHealth <= 0d ? 0d : CurrentHealth / MaxHealth;

        public double AttackTimer => attackTimer;

        /// <summary>Stable string key for tooling and logs ("party:0", "enemy:2").</summary>
        public string Key => (Side == CombatantSide.Party ? "party:" : "enemy:") + SlotIndex;

        // --- Behaviour ---
        /// <summary>Advances the attack cooldown; true when a swing is ready this step.</summary>
        public bool Tick(double deltaTime)
        {
            if (!IsAlive || deltaTime <= 0d)
            {
                return false;
            }

            attackTimer -= deltaTime;
            if (attackTimer > 0d)
            {
                return false;
            }

            // Carry the overshoot so cadence never drifts, and clamp to avoid death spirals.
            attackTimer += AttackIntervalSec;
            if (attackTimer <= 0d)
            {
                attackTimer = AttackIntervalSec;
            }

            return true;
        }

        /// <summary>Applies damage (shield first, then health). Returns the amount actually removed.</summary>
        public double TakeDamage(double damage)
        {
            if (damage <= 0d || !IsAlive)
            {
                return 0d;
            }

            double applied = 0d;

            if (Shield > 0d)
            {
                double absorbed = damage > Shield ? Shield : damage;
                Shield -= absorbed;
                applied += absorbed;
            }

            double toHealth = damage - applied;
            if (toHealth > 0d)
            {
                double healthHit = toHealth > CurrentHealth ? CurrentHealth : toHealth;
                CurrentHealth -= healthHit;
                applied += healthHit;

                if (CurrentHealth < 0d)
                {
                    CurrentHealth = 0d;
                }
            }

            return applied;
        }

        /// <summary>Heals up to max health. Returns the amount healed.</summary>
        public double Heal(double amount)
        {
            if (amount <= 0d || !IsAlive)
            {
                return 0d;
            }

            double missing = MaxHealth - CurrentHealth;
            if (missing <= 0d)
            {
                return 0d;
            }

            double healed = amount > missing ? missing : amount;
            CurrentHealth += healed;
            return healed;
        }

        /// <summary>Full heal (stage advance, retry, or a fresh run).</summary>
        public void RestoreFullHealth()
        {
            CurrentHealth = Stats.MaxHealth;
            Shield = 0d;
            RestartTimer();
        }

        /// <summary>Re-arms the attack timer to the standard half-interval stagger.</summary>
        public void RestartTimer()
        {
            attackTimer = AttackIntervalSec * 0.5d;
        }

        /// <summary>Applies a new stat set, keeping the current health percentage.</summary>
        public void ApplyStats(StatBlock stats, double attackIntervalSec)
        {
            if (stats == null)
            {
                return;
            }

            // Behaviour preserved from the MVP: a living combatant keeps its health percentage, and one at
            // exactly 0% is restored to full. (Logged in Checklist.md; Step 12 revisits revive rules.)
            double ratio = MaxHealth > 0d ? CurrentHealth / MaxHealth : 1d;

            Stats.CopyFrom(stats);
            Stats.Sanitize();
            Stats.MaxHealth = Stats.MaxHealth < 1d ? 1d : Stats.MaxHealth;
            AttackIntervalSec = ClampInterval(attackIntervalSec);

            CurrentHealth = IsAlive || ratio > 0d
                ? ClampTo(ratio * Stats.MaxHealth, Stats.MaxHealth)
                : Stats.MaxHealth;
        }

        /// <summary>Readable dump for tools, logs and probes.</summary>
        public override string ToString()
        {
            return $"{DisplayName} [{Key}] {CurrentHealth:0.#}/{MaxHealth:0.#} {Stats}";
        }

        private double ClampInterval(double value)
        {
            return value < MinAttackIntervalSec ? MinAttackIntervalSec : value;
        }

        private static double ClampTo(double value, double maxValue)
        {
            if (value < 0d)
            {
                return 0d;
            }

            return value > maxValue ? maxValue : value;
        }
    }
}

