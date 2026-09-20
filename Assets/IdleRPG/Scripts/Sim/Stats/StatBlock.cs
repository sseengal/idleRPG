using System;

namespace IdleRPG.Sim
{
    /// <summary>
    /// Fixed-size, allocation-free stat container. One per combatant per encounter.
    /// Reads are array lookups (see <see cref="StatId.Slot"/>), so the damage pipeline can query
    /// many stats per hit without dictionaries or boxing.
    /// </summary>
    public sealed class StatBlock
    {
        private readonly double[] values = new double[StatId.Count];

        public StatBlock()
        {
        }

        public StatBlock(double hp, double attack, double defense)
        {
            values[StatId.SlotHp] = hp < 0d ? 0d : hp;
            values[StatId.SlotAttack] = attack < 0d ? 0d : attack;
            values[StatId.SlotDefense] = defense < 0d ? 0d : defense;
        }

        /// <summary>Copy constructor (services hand the sim a snapshot, never a live object).</summary>
        public StatBlock(StatBlock source)
        {
            if (source != null)
            {
                Array.Copy(source.values, values, values.Length);
            }
        }

        public double Get(int slot)
        {
            return slot < 0 || slot >= values.Length ? 0d : values[slot];
        }

        public double Get(string id)
        {
            return Get(StatId.Slot(id));
        }

        public void Set(int slot, double value)
        {
            if (slot < 0 || slot >= values.Length)
            {
                return;
            }

            values[slot] = value;
        }

        public void Set(string id, double value)
        {
            Set(StatId.Slot(id), value);
        }

        /// <summary>Adds a flat amount (clamped at 0 for stat values that cannot be negative).</summary>
        public void Add(int slot, double delta)
        {
            if (slot < 0 || slot >= values.Length)
            {
                return;
            }

            double sum = values[slot] + delta;
            values[slot] = sum < 0d ? 0d : sum;
        }

        /// <summary>Multiplies a stat (used by the pipeline's multiplicative bucket).</summary>
        public void Multiply(int slot, double factor)
        {
            if (slot < 0 || slot >= values.Length || factor < 0d)
            {
                return;
            }

            values[slot] *= factor;
        }

        // Convenience accessors for the stats the MVP combat loop uses today.
        public double MaxHealth
        {
            get => Get(StatId.SlotHp);
            set => Set(StatId.SlotHp, value);
        }

        public double Attack
        {
            get => Get(StatId.SlotAttack);
            set => Set(StatId.SlotAttack, value);
        }

        public double Defense
        {
            get => Get(StatId.SlotDefense);
            set => Set(StatId.SlotDefense, value);
        }

        public void CopyFrom(StatBlock source)
        {
            if (source == null)
            {
                return;
            }

            Array.Copy(source.values, values, values.Length);
        }

        /// <summary>Replaces non-finite values with a fallback (guards against a broken modifier chain).</summary>
        public void Sanitize(double fallback = 0d)
        {
            for (int i = 0; i < values.Length; i++)
            {
                double value = values[i];
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    values[i] = fallback;
                }
                else if (value < 0d)
                {
                    values[i] = 0d;
                }
            }
        }

        /// <summary>Debug/UI helper: "hp=240 atk=12 def=12".</summary>
        public override string ToString()
        {
            return $"hp={MaxHealth:0.##} atk={Attack:0.##} def={Defense:0.##}";
        }
    }
}
