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

        /// <summary>
        /// This unit's critical-hit chance (0..1) and damage multiplier. 0 chance = "use the rules' global crit"
        /// (enemies), so per-hero crit can be added without touching a single enemy asset.
        /// </summary>
        public double CritChance { get; set; }

        public double CritDamageMultiplier { get; set; } = 1d;

        /// <summary>
        /// How long THIS unit's swing travels before its damage lands, in seconds. 0 = "use the rules' window"
        /// (a placeholder with no art). Set from the unit's art at spawn, exactly like <see cref="CritChance"/>:
        /// it is the time up to and including the art's own strike frame, so the damage lands when that unit's
        /// swing connects instead of on a global constant.
        /// </summary>
        public double SwingSeconds { get; set; }

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

        // --- Ability (Ability-Plan) ---
        // Kept as PLAIN VALUES, never as the ability asset: Scripts/Sim is its own assembly and must not depend on
        // IdleRPG.Data (the compile error that proved it is why this comment exists). The view and the panel look the
        // ability up by id; the sim only needs the id, the label, the clip slot and the cooldown.
        private double abilityCooldown;
        private bool abilityCastRequested;

        /// <summary>Id of the ability this unit may cast, or empty for none.</summary>
        public string AbilityId { get; private set; } = "";

        /// <summary>Display name for the log line ("Shield Wall").</summary>
        public string AbilityName { get; private set; } = "";

        /// <summary>Which art slot plays it ("Ability", "Attack02"). The view resolves frames and rate from this.</summary>
        public string AbilityClipSlot { get; private set; } = "";

        /// <summary>Seconds between casts. The blast itself is instant; this is the clock that refills it.</summary>
        public double AbilityCooldownSec { get; private set; }

        public bool HasAbility => !string.IsNullOrEmpty(AbilityId);

        /// <summary>Seconds until the ability is usable again. 0 = ready.</summary>
        public double AbilityCooldownRemaining => abilityCooldown < 0d ? 0d : abilityCooldown;

        /// <summary>True when there is something to cast, the unit is up, and the clock has run out.</summary>
        public bool IsAbilityReady => HasAbility && IsAlive && abilityCooldown <= 0d;

        /// <summary>Gives the unit an ability and clears its clock, so a fresh fight starts with it ready.</summary>
        public void SetAbility(string id, string displayName, string clipSlot, double cooldownSec)
        {
            AbilityId = string.IsNullOrEmpty(id) ? "" : id;
            AbilityName = string.IsNullOrEmpty(displayName) ? AbilityId : displayName;
            AbilityClipSlot = string.IsNullOrEmpty(clipSlot) ? "Ability" : clipSlot;
            AbilityCooldownSec = cooldownSec < 0d ? 0d : cooldownSec;

            abilityCooldown = 0d;
            abilityCastRequested = false;
        }

        /// <summary>
        /// Asks to cast right now. False when there is nothing to cast, the unit is down, or it is still cooling -
        /// so a press can never queue a cast the fight then has to refuse.
        /// </summary>
        public bool RequestAbility()
        {
            if (!IsAbilityReady)
            {
                return false;
            }

            abilityCastRequested = true;
            return true;
        }

        /// <summary>
        /// The fight drains a request here and raises the cast. True when a cast fired, with the ability's values in
        /// the out parameters, having started its cooldown. False when nothing was requested or the unit went down.
        /// </summary>
        public bool ConsumeAbilityCast(out string abilityId, out string abilityName, out string clipSlot, out double cooldownSec)
        {
            abilityId = null;
            abilityName = null;
            clipSlot = null;
            cooldownSec = 0d;

            if (!abilityCastRequested)
            {
                return false;
            }

            abilityCastRequested = false;

            if (!HasAbility || !IsAlive)
            {
                return false;
            }

            abilityCooldown = AbilityCooldownSec;
            abilityId = AbilityId;
            abilityName = AbilityName;
            clipSlot = AbilityClipSlot;
            cooldownSec = AbilityCooldownSec;
            return true;
        }

        // --- Behaviour ---
        /// <summary>Advances the attack cooldown and the ability cooldown.</summary>
        public void Tick(double deltaTime)
        {
            if (!IsAlive || deltaTime <= 0d)
            {
                return;
            }

            if (abilityCooldown > 0d)
            {
                abilityCooldown -= deltaTime;
            }

            attackTimer -= deltaTime;
            if (attackTimer <= 0d)
            {
                IsSwingReady = true;
            }
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
            IsSwingReady = false;
        }

        /// <summary>
        /// This combatant's cooldown has finished and it is waiting for its turn. The fight picks exactly one
        /// ready unit at a time, so a unit can stay ready through other people's swings; the timer keeps counting
        /// down while it waits, so the most-negative timer is the one that has waited longest (used to break ties).
        /// </summary>
        public bool IsSwingReady { get; private set; }

        /// <summary>
        /// Called when this combatant's swing lands (impact). The full interval is re-armed from now: the next
        /// swing is ready a full <see cref="AttackIntervalSec"/> later, which keeps the steady-state hit rate
        /// (and DPS) at exactly one swing per interval. Nothing is subtracted for the flight time - the swing
        /// already occupied that time, so the next one starts counting from impact.
        /// </summary>
        public void EndSwing()
        {
            IsSwingReady = false;
            attackTimer = AttackIntervalSec;
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

