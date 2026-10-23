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

        /// <summary>
        /// Absorbs damage before health (the pipeline fills this from Step 12 on).
        /// </summary>
        public double Shield { get; set; }

        private double guardFactor = 1d;
        private double guardSecondsRemaining;

        /// <summary>
        /// Damage taken is multiplied by this while a guard is up (1 = no guard). An ability's Shield kind sets it for
        /// a few seconds, so "half damage for 4 s" really halves every hit that lands inside that window.
        /// </summary>
        public double DamageTakenMultiplier => guardSecondsRemaining > 0d ? guardFactor : 1d;

        /// <summary>
        /// Raises (or refreshes) a guard: incoming damage is multiplied by <paramref name="factor"/> for
        /// <paramref name="seconds"/>. A factor is kept inside 0.05..1, because a guard can never make damage worse.
        /// </summary>
        public void ApplyGuard(double factor, double seconds)
        {
            if (!IsAlive || seconds <= 0d)
            {
                return;
            }

            guardFactor = factor < 0.05d ? 0.05d : (factor > 1d ? 1d : factor);
            guardSecondsRemaining = seconds;
        }

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

        // Enemy intent only: seconds left before the next "shall I cast now?" roll. A ready clock on its own used to
        // mean "cast on the very next beat", which made enemy moves feel like a metronome. Now the unit waits, rolls
        // once, and only a pass turns into a cast.
        private double abilityPatience;

        /// <summary>What this unit may cast, or <see cref="SimAbility.None"/> when it has nothing.</summary>
        public SimAbility Ability { get; private set; } = SimAbility.None;

        /// <summary>Id of the ability this unit may cast, or empty for none.</summary>
        public string AbilityId => Ability.Id;

        /// <summary>Display name for the log line ("Shield Wall").</summary>
        public string AbilityName => Ability.DisplayName;

        /// <summary>Which art slot plays it ("Ability", "Attack02"). The view resolves frames and rate from this.</summary>
        public string AbilityClipSlot => Ability.ClipSlot;

        /// <summary>Seconds between casts, counted from the announce - the wait in the lane is not charged.</summary>
        public double AbilityCooldownSec => Ability.CooldownSec;

        public bool HasAbility => Ability.IsValid;

        /// <summary>Seconds until the ability is usable again. 0 = ready.</summary>
        public double AbilityCooldownRemaining => abilityCooldown < 0d ? 0d : abilityCooldown;

        /// <summary>True when there is something to cast, the unit is up, and the clock has run out.</summary>
        public bool IsAbilityReady => HasAbility && IsAlive && abilityCooldown <= 0d;

        /// <summary>
        /// True when a cast has been asked for but the lane has not announced it yet. The ability button shows this as
        /// "queued": the wait is real, so it must be visible instead of looking like a dead press.
        /// </summary>
        public bool HasCastRequest => abilityCastRequested;

        /// <summary>Gives the unit an ability and clears its clock, so a fresh fight starts with it ready.</summary>
        public void SetAbility(SimAbility ability)
        {
            Ability = ability;
            abilityCooldown = 0d;
            abilityCastRequested = false;
            abilityPatience = 0d;
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
        /// The fight takes the pending request when the lane is free. True only when the cast may really start now,
        /// so a request made while the unit stood ready cannot fire after it fell.
        /// </summary>
        public bool TryTakeAbilityRequest()
        {
            if (!abilityCastRequested)
            {
                return false;
            }

            abilityCastRequested = false;
            return IsAbilityReady;
        }

        /// <summary>Throws a pending request away (the unit died, the wave ended, the run reset).</summary>
        public void CancelAbilityRequest()
        {
            abilityCastRequested = false;
        }

        /// <summary>
        /// The cast was announced: its clock starts now. Starting it at the announce (not at the press) is
        /// deliberate - a queued press waits for the lane, and the player is not charged for that wait.
        /// </summary>
        public void StartAbilityCooldown()
        {
            abilityCooldown = HasAbility ? Ability.CooldownSec : 0d;
        }

        /// <summary>
        /// Makes the unit wait this long before its first cast roll. Used at wave start, so a fight does not open with
        /// an enemy move on its very first beat.
        /// </summary>
        public void HoldAbility(double seconds)
        {
            abilityPatience = seconds > 0d ? seconds : 0d;
        }

        /// <summary>
        /// Enemy intent, one beat at a time. While the clock is out the unit waits; when the wait ends it rolls ONCE
        /// against <paramref name="chance"/>. A pass means "cast now"; a miss means it swings instead and waits again
        /// for a random spell of <paramref name="retryMinSec"/> to <paramref name="retryMaxSec"/>.
        ///
        /// One roll per wait, never one per frame: a per-frame roll would fire almost at once no matter how small the
        /// chance, and the number would stop meaning what it says. The dice come from the fight's own generator, so a
        /// replay and an offline run land on the same casts.
        /// </summary>
        public bool TickAbilityIntent(double deltaTime, IRng rng, double chance, double retryMinSec, double retryMaxSec)
        {
            if (!IsAbilityReady)
            {
                abilityPatience = 0d;
                return false;
            }

            if (abilityPatience > 0d)
            {
                abilityPatience -= deltaTime;
                if (abilityPatience > 0d)
                {
                    return false;
                }
            }

            if (rng == null || rng.NextDouble() < chance)
            {
                abilityPatience = 0d;
                return true;
            }

            double min = retryMinSec > 0d ? retryMinSec : 0d;
            double max = retryMaxSec > min ? retryMaxSec : min;
            abilityPatience = min + (rng != null && max > min ? rng.NextDouble() * (max - min) : 0d);
            return false;
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

            if (guardSecondsRemaining > 0d)
            {
                guardSecondsRemaining -= deltaTime;
                if (guardSecondsRemaining <= 0d)
                {
                    guardSecondsRemaining = 0d;
                    guardFactor = 1d;
                }
            }

            attackTimer -= deltaTime;
            if (attackTimer <= 0d)
            {
                IsSwingReady = true;
            }
        }

        /// <summary>Applies damage (guard, then shield, then health). Returns the amount actually removed.</summary>
        public double TakeDamage(double damage)
        {
            if (damage <= 0d || !IsAlive)
            {
                return 0d;
            }

            // A guard multiplies the WHOLE hit (health and shield alike), which is what "takes half damage" means.
            double incoming = damage * DamageTakenMultiplier;
            double applied = 0d;

            if (Shield > 0d)
            {
                double absorbed = incoming > Shield ? Shield : incoming;
                Shield -= absorbed;
                applied += absorbed;
            }

            double toHealth = incoming - applied;
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
            guardSecondsRemaining = 0d;
            guardFactor = 1d;
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

