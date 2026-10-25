using System;
using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Sim;
using IdleRPG.Utils;

namespace IdleRPG.Core
{
    /// <summary>What a feed line is, so the view can colour it without reading the sentence.</summary>
    public enum BattleLogKind
    {
        Damage,
        Critical,
        Incoming,
        EnemyKilled,
        HeroDied,
        Ability,
        EnemyAbility,
        Heal,
        Guard,
        Gold,
        Event,
        Defeat,
        System,
    }

    /// <summary>One frozen line. Text is built once, when the action happens.</summary>
    public readonly struct BattleLogEntry
    {
        public long Sequence { get; }

        public string Text { get; }

        public BattleLogKind Kind { get; }

        public BattleLogEntry(long sequence, string text, BattleLogKind kind)
        {
            Sequence = sequence;
            Text = text;
            Kind = kind;
        }

        public override string ToString() => Text;
    }

    /// <summary>
    /// THE battle log. One store, one writer, one sentence per action.
    ///
    /// The view paints what this store holds and owns nothing itself. Hiding the battle page, rebuilding the view,
    /// or a broken listener can no longer lose or freeze the history: the store keeps the last
    /// <see cref="Capacity"/> lines and the view redraws from them.
    ///
    /// Text is built here, at the moment of the action, because names change between waves (a later rebuild would
    /// name the wrong enemy). Names come from the game wiring through <see cref="HeroNameResolver"/> /
    /// <see cref="EnemyNameResolver"/> so this class never touches the UI.
    /// </summary>
    public static class BattleLog
    {
        /// <summary>How much history the game keeps. The view shows its own smaller window.</summary>
        public const int Capacity = 200;

        private static readonly List<BattleLogEntry> entries = new List<BattleLogEntry>(Capacity);

        private static long nextSequence;
        private static bool installed;

        /// <summary>(heroIndex) -> the name the player reads. Set once by the game wiring.</summary>
        public static Func<int, string> HeroNameResolver { get; set; }

        /// <summary>(enemyIndex) -> the name the player reads (A/B/C suffix for a stacked wave).</summary>
        public static Func<int, string> EnemyNameResolver { get; set; }

        /// <summary>One line was added. The view paints it; nothing else reads this.</summary>
        public static event Action<BattleLogEntry> Appended;

        /// <summary>The history was wiped (new session). The view drops its lines.</summary>
        public static event Action Cleared;

        /// <summary>Oldest first. Read-only: the store is the only writer.</summary>
        public static IReadOnlyList<BattleLogEntry> Entries => entries;

        /// <summary>Total lines ever appended this session (diagnostics and tests).</summary>
        public static long TotalAppended => nextSequence;

        /// <summary>
        /// Connects the store to the game events. Called once by the composition root
        /// (<see cref="GameManager.WireUp"/>), before the save load, so a resumed session is caught.
        /// Safe to call again: it detaches first and starts a clean history.
        /// </summary>
        public static void Install()
        {
            Uninstall();
            entries.Clear();
            nextSequence = 0;

            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.HeroDamaged += OnHeroDamaged;
            GameEvents.HeroDied += OnHeroDied;
            GameEvents.Healed += OnHealed;
            GameEvents.ShieldApplied += OnShieldApplied;
            GameEvents.AbilityCast += OnAbilityCast;
            GameEvents.AscensionCompleted += OnAscensionCompleted;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed += OnOfflineRewardsClaimed;
            GameEvents.CombatMessage += OnCombatMessage;
            GameEvents.SaveLoaded += OnSaveLoaded;

            installed = true;
        }

        /// <summary>Detaches every subscription. Used by Install and by tests.</summary>
        public static void Uninstall()
        {
            if (!installed)
            {
                return;
            }

            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.Healed -= OnHealed;
            GameEvents.ShieldApplied -= OnShieldApplied;
            GameEvents.AbilityCast -= OnAbilityCast;
            GameEvents.AscensionCompleted -= OnAscensionCompleted;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed -= OnOfflineRewardsClaimed;
            GameEvents.CombatMessage -= OnCombatMessage;
            GameEvents.SaveLoaded -= OnSaveLoaded;

            installed = false;
        }

        /// <summary>The only write path. Empty text is ignored so a broken caller cannot write a blank line.</summary>
        public static void Append(string text, BattleLogKind kind = BattleLogKind.Event)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            BattleLogEntry entry = new BattleLogEntry(++nextSequence, text, kind);
            entries.Add(entry);

            while (entries.Count > Capacity)
            {
                entries.RemoveAt(0);
            }

            Action<BattleLogEntry> handler = Appended;

            if (handler == null)
            {
                return;
            }

            // Isolate every listener: one broken view must not starve the next one.
            Delegate[] listeners = handler.GetInvocationList();

            for (int i = 0; i < listeners.Length; i++)
            {
                try
                {
                    ((Action<BattleLogEntry>)listeners[i])(entry);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[BattleLog] A listener of 'Appended' threw: {exception}");
                }
            }
        }

        /// <summary>Wipes the history and tells the view to do the same.</summary>
        public static void Clear()
        {
            entries.Clear();

            Action handler = Cleared;

            if (handler == null)
            {
                return;
            }

            Delegate[] listeners = handler.GetInvocationList();

            for (int i = 0; i < listeners.Length; i++)
            {
                try
                {
                    ((Action)listeners[i])();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[BattleLog] A listener of 'Cleared' threw: {exception}");
                }
            }
        }

        // ------------------------------------------------------------------
        // Names (one rule for the whole game)
        // ------------------------------------------------------------------
        private static string HeroName(int heroIndex)
        {
            Func<int, string> resolver = HeroNameResolver;

            if (resolver != null)
            {
                try
                {
                    string name = resolver(heroIndex);

                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[BattleLog] Hero name resolver threw for index {heroIndex}: {exception}");
                }
            }

            return heroIndex < 0 ? "The party" : $"Hero {heroIndex + 1}";
        }

        private static string EnemyName(int enemyIndex)
        {
            Func<int, string> resolver = EnemyNameResolver;

            if (resolver != null)
            {
                try
                {
                    string name = resolver(enemyIndex);

                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[BattleLog] Enemy name resolver threw for index {enemyIndex}: {exception}");
                }
            }

            return "the enemy";
        }

        // ------------------------------------------------------------------
        // Game events -> one sentence each (moved verbatim from the old view)
        // ------------------------------------------------------------------
        private static void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            string attacker = HeroName(info.AttackerIndex);
            string target = EnemyName(info.EnemyIndex);

            Append(info.IsCritical
                    ? string.Format("{0} CRITS {1} for {2}", attacker, target, NumberFormatter.Format(info.Damage))
                    : string.Format("{0} hits {1} for {2}", attacker, target, NumberFormatter.Format(info.Damage)),
                info.IsCritical ? BattleLogKind.Critical : BattleLogKind.Damage);
        }

        private static void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex, bool isCritical, bool isGuarded)
        {
            string attacker = EnemyName(attackerEnemyIndex);
            string target = HeroName(heroIndex);

            // Incoming crits read the same as outgoing ones: "<who> CRITS <whom> for N".
            Append(isCritical
                    ? string.Format("{0} CRITS {1} for {2}  ({3}/{4})", attacker, target,
                        NumberFormatter.Format(damage),
                        NumberFormatter.Format(currentHealth),
                        NumberFormatter.Format(maxHealth))
                    : string.Format("{0} hits {1} for {2}  ({3}/{4})", attacker, target,
                        NumberFormatter.Format(damage),
                        NumberFormatter.Format(currentHealth),
                        NumberFormatter.Format(maxHealth)),
                isCritical ? BattleLogKind.Critical : BattleLogKind.Incoming);
        }

        private static void OnEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            // The index is the truth (it carries the A/B/C slot name); the passed name is the plain archetype.
            Append(string.Format("{0} defeated  +{1} gold", EnemyName(enemyIndex), NumberFormatter.Format(goldReward)),
                BattleLogKind.EnemyKilled);
        }

        /// <summary>
        /// A unit cast its ability: the "cause" line. It is written when the cast is announced, so it always appears
        /// before the impact line that follows - the same order a basic attack reads in.
        /// </summary>
        private static void OnAbilityCast(CombatantSide side, int index, string abilityId, string abilityName, string clipSlot, double cooldownSeconds)
        {
            if (side == CombatantSide.Enemy)
            {
                Append(string.Format("{0} unleashes {1}!", EnemyName(index), abilityName), BattleLogKind.EnemyAbility);
                return;
            }

            Append(string.Format("{0} casts {1}", HeroName(index), abilityName), BattleLogKind.Ability);
        }

        private static void OnHealed(HealEvent heal)
        {
            string target = heal.Side == CombatantSide.Enemy ? EnemyName(heal.Index) : HeroName(heal.Index);

            Append(string.Format("{0} healed for {1} by {2}  ({3}/{4})", target,
                NumberFormatter.Format(heal.Amount),
                heal.AbilityName,
                NumberFormatter.Format(heal.TargetHealth),
                NumberFormatter.Format(heal.TargetMaxHealth)), BattleLogKind.Heal);
        }

        private static void OnShieldApplied(ShieldEvent shield)
        {
            string target = shield.Side == CombatantSide.Enemy ? EnemyName(shield.Index) : HeroName(shield.Index);

            Append(string.Format("{0} guards with {1}  ({2}% damage for {3}s)", target,
                shield.AbilityName,
                NumberFormatter.Format(shield.Factor * 100d),
                NumberFormatter.Format(shield.Seconds)), BattleLogKind.Guard);
        }

        private static void OnHeroDied(int heroIndex)
        {
            Append(string.Format("{0} has fallen", HeroName(heroIndex)), BattleLogKind.HeroDied);
        }

        private static void OnAscensionCompleted(double tokensEarned, int newHighestStage)
        {
            Append(string.Format("Ascended - +{0} token(s), best stage {1}",
                NumberFormatter.Format(tokensEarned), newHighestStage), BattleLogKind.Gold);
        }

        private static void OnUpgradePurchased(int heroIndex, HeroStatType statType, int newLevel, double goldCost)
        {
            Append(string.Format("{0} {1} -> Lv {2}  (-{3} gold)", HeroName(heroIndex),
                statType.ToDisplayName(), newLevel, NumberFormatter.Format(goldCost)), BattleLogKind.Event);
        }

        private static void OnOfflineRewardsClaimed(double gold)
        {
            Append(string.Format("Offline earnings claimed  +{0} gold", NumberFormatter.Format(gold)),
                BattleLogKind.Gold);
        }

        /// <summary>Renders the news channel (wave/clear/wipe/boss/drops/future systems). Never dropped.</summary>
        private static void OnCombatMessage(LogMessage message)
        {
            BattleLogKind kind = BattleLogKind.Event;

            switch (message.Kind)
            {
                case LogMessageKind.Defeat:
                    kind = BattleLogKind.Defeat;
                    break;
                case LogMessageKind.Reward:
                    kind = BattleLogKind.Gold;
                    break;
            }

            Append(message.Text, kind);
        }

        /// <summary>New session (save load / restart): clean history plus a visible divider.</summary>
        private static void OnSaveLoaded()
        {
            Clear();
            Append("-- session resumed --", BattleLogKind.Event);
        }
    }
}
