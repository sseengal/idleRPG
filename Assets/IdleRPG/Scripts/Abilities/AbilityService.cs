using System;
using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;

namespace IdleRPG.Abilities
{
    /// <summary>
    /// Owns what the PLAYER has done with abilities: which are unlocked, and which hero wears which one.
    ///
    /// Two pieces of state, stored two different ways on purpose:
    /// - Unlocked rides the save's keyed level list as "ability.&lt;id&gt;" (level 0 = locked, 1 = unlocked). That is
    ///   where the game already keeps "how far along" anything is, and it leaves room for a future ability level-up
    ///   without a schema change.
    /// - The assignment needs a hero id per ability id, which a number cannot hold, so it uses its own small list
    ///   (schema v7) - held as IDs rather than catalogue row numbers, so reordering the catalogue can never silently
    ///   re-point a player's choice.
    ///
    /// The state lives here in memory and is written into the save by the snapshot mapper, exactly like every other
    /// service. Nothing in here touches a file.
    /// </summary>
    public sealed class AbilityService
    {
        private readonly EconomyManager economy;

        /// <summary>The stage the player is on, asked at unlock time (never cached - the run moves).</summary>
        private readonly Func<int> currentStage;

        private readonly HashSet<string> unlocked = new HashSet<string>();
        private readonly Dictionary<string, string> assignedByHero = new Dictionary<string, string>();

        public AbilityService(EconomyManager economy, Func<int> currentStage)
        {
            this.economy = economy;
            this.currentStage = currentStage;
        }

        /// <summary>Raised when an ability is bought, so the row showing it can redraw.</summary>
        public event Action<AbilityDef> Unlocked;

        /// <summary>Raised with the hero id when that hero's ability changes (assigned or cleared).</summary>
        public event Action<string> AssignmentChanged;

        public int UnlockedCount => unlocked.Count;

        /// <summary>
        /// Reads every "ability." level out of the payload. Reads the WHOLE keyed list rather than asking for known
        /// ids, so an ability added later - or one this build no longer contains - round-trips untouched instead of
        /// being silently dropped on the next save.
        /// </summary>
        public void LoadFromSave(SaveData data)
        {
            unlocked.Clear();
            assignedByHero.Clear();

            if (data == null)
            {
                return;
            }

            if (data.levels != null)
            {
                for (int i = 0; i < data.levels.Count; i++)
                {
                    LevelRecord record = data.levels[i];
                    if (record == null || record.level <= 0 ||
                        string.IsNullOrEmpty(record.key) ||
                        !record.key.StartsWith(SaveData.SaveKeys.AbilityUnlockPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // A bare "ability." names nothing (hand-edited or truncated payload), so it is not an unlock.
                    string abilityId = record.key.Substring(SaveData.SaveKeys.AbilityUnlockPrefix.Length);
                    if (abilityId.Length > 0)
                    {
                        unlocked.Add(abilityId);
                    }
                }
            }

            if (data.abilityAssignments != null)
            {
                for (int i = 0; i < data.abilityAssignments.Count; i++)
                {
                    AbilityAssignmentRecord record = data.abilityAssignments[i];
                    if (record == null || string.IsNullOrEmpty(record.heroID) || string.IsNullOrEmpty(record.abilityID))
                    {
                        continue;
                    }

                    assignedByHero[record.heroID] = record.abilityID;
                }
            }
        }

        /// <summary>Writes both halves back into the payload the snapshot mapper is building.</summary>
        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            foreach (string abilityId in unlocked)
            {
                data.SetLevel(SaveData.SaveKeys.AbilityUnlock(abilityId), 1);
            }

            data.abilityAssignments = new List<AbilityAssignmentRecord>();
            foreach (KeyValuePair<string, string> pair in assignedByHero)
            {
                data.abilityAssignments.Add(new AbilityAssignmentRecord(pair.Key, pair.Value));
            }
        }

        // ------------------------------------------------------------------
        // Unlock
        // ------------------------------------------------------------------

        public bool IsUnlocked(AbilityDef ability)
        {
            return ability != null && unlocked.Contains(ability.AbilityID);
        }

        /// <summary>True when the player's progress has reached the ability's gate.</summary>
        public bool MeetsStage(AbilityDef ability)
        {
            return ability != null && ability.IsAvailableAt(currentStage != null ? currentStage() : 0);
        }

        /// <summary>What would happen if the player pressed unlock now. Spends nothing.</summary>
        public AbilityUnlockResult CanUnlock(AbilityDef ability)
        {
            if (ability == null)
            {
                return AbilityUnlockResult.Invalid;
            }

            if (IsUnlocked(ability))
            {
                return AbilityUnlockResult.AlreadyUnlocked;
            }

            if (!MeetsStage(ability))
            {
                return AbilityUnlockResult.StageLocked;
            }

            // Only a COSTED ability needs a balance to consult. A free ability must unlock with no economy at all -
            // otherwise the starting ability could never be bought, and the row would say "cannot afford" forever.
            if (ability.TokenCost > 0d && (economy == null || !economy.CanAfford(CurrencyType.PrestigeTokens, ability.TokenCost)))
            {
                return AbilityUnlockResult.CannotAfford;
            }

            return AbilityUnlockResult.Bought;
        }

        /// <summary>
        /// Buys the ability. Checks the gate first, then pays, then records - so a failed stage check can never cost
        /// the player anything. A free ability (token cost 0) simply succeeds without touching the balance.
        /// </summary>
        public AbilityUnlockResult TryUnlock(AbilityDef ability)
        {
            AbilityUnlockResult verdict = CanUnlock(ability);
            if (verdict != AbilityUnlockResult.Bought)
            {
                return verdict;
            }

            if (ability.TokenCost > 0d && (economy == null || !economy.SpendTokens(ability.TokenCost)))
            {
                return AbilityUnlockResult.CannotAfford;
            }

            unlocked.Add(ability.AbilityID);
            Unlocked?.Invoke(ability);
            return AbilityUnlockResult.Bought;
        }

        // ------------------------------------------------------------------
        // Assignment
        // ------------------------------------------------------------------

        /// <summary>The ability id worn by a hero, or an empty string. Not proof it is unlocked - use IsAssigned.</summary>
        public string AssignedId(string heroId)
        {
            if (string.IsNullOrEmpty(heroId))
            {
                return "";
            }

            return assignedByHero.TryGetValue(heroId, out string id) ? id : "";
        }

        /// <summary>True when this hero wears this ability AND has unlocked it - i.e. it is actually in the fight.</summary>
        public bool IsAssigned(string heroId, AbilityDef ability)
        {
            return ability != null && IsUnlocked(ability) &&
                   string.Equals(AssignedId(heroId), ability.AbilityID, StringComparison.Ordinal);
        }

        /// <summary>
        /// Puts an ability on a hero (or clears it with null). Refuses an ability the player has not unlocked, so a
        /// save edited by hand, or a stale row, can never put a hero into the fight with something never paid for.
        /// </summary>
        public bool Assign(string heroId, AbilityDef ability)
        {
            if (string.IsNullOrEmpty(heroId))
            {
                return false;
            }

            if (ability == null)
            {
                bool removed = assignedByHero.Remove(heroId);
                if (removed)
                {
                    AssignmentChanged?.Invoke(heroId);
                }

                return removed;
            }

            if (!IsUnlocked(ability))
            {
                return false;
            }

            if (string.Equals(AssignedId(heroId), ability.AbilityID, StringComparison.Ordinal))
            {
                return true;
            }

            assignedByHero[heroId] = ability.AbilityID;
            AssignmentChanged?.Invoke(heroId);
            return true;
        }

        /// <summary>True when this hero is the one who could ever wear this ability (its owner in the content).</summary>
        public static bool Owns(HeroData hero, AbilityDef ability)
        {
            return hero != null && ability != null && hero.Ability == ability;
        }
    }
}
