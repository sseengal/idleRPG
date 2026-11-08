using System.Collections.Generic;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Save;
using UnityEngine;

namespace IdleRPG.Core
{
    /// <summary>
    /// Party XP, boon picks, and the boon deck for the current run.
    ///
    /// ELI5: killing enemies fills a shared XP bar. Each level owes the player one card. When a card is owed the
    /// fight stops and three cards are dealt; the player taps one, it is theirs for the rest of the run, and the
    /// fight continues. Everything here is cleared when the run ends, like run gold.
    ///
    /// Why the manager owns it: the tracker (<see cref="PartyProgression"/>) counts, the hand
    /// (<see cref="PerkLoadout"/>) adds up, the dealer (<see cref="PerkOfferService"/>) picks. None of them knows
    /// what a run, a save or an event is; this file is the only place that joins the three.
    /// </summary>
    public sealed partial class GameManager
    {
        /// <summary>The whole deck. Wired by the scene builder, so a rebuild cannot drop it.</summary>
        [SerializeField] private PerkPoolDef boonPool;

        /// <summary>The party's run level, XP and queued boon picks.</summary>
        public PartyProgression Progression { get; private set; }

        /// <summary>Boons taken this run.</summary>
        public PerkLoadout Boons { get; private set; }

        private readonly List<PerkData> boonOffer = new List<PerkData>();
        private PerkOfferService boonOffers;
        private int boonRerollsUsed;

        /// <summary>The three cards currently on screen (empty when the card is closed).</summary>
        public IReadOnlyList<PerkData> BoonOffer => boonOffer;

        /// <summary>True while the level-up card is up.</summary>
        public bool BoonCardOpen => boonOffer.Count > 0;

        /// <summary>Free re-rolls left this run.</summary>
        public int BoonRerollsLeft => boonPool != null ? Mathf.Max(0, boonPool.RerollsPerRun - boonRerollsUsed) : 0;

        /// <summary>Levels a single run may reach (0 = no cap). Read by the HUD and the card.</summary>
        public int PartyLevelCap => balanceConfig != null ? balanceConfig.PartyMaxLevelPerRun : 20;

        /// <summary>
        /// Creates the tracker and the hand. Called by the composition root BEFORE the save is loaded, so a resumed
        /// run gets its level, XP, picks and boons back through the normal save path.
        /// </summary>
        private void BuildPartyProgression()
        {
            Progression = new PartyProgression(balanceConfig);

            Boons = new PerkLoadout();
            Boons.Changed += OnBoonsChanged;

            boonOffers = new PerkOfferService(boonPool);

            if (Resolver != null)
            {
                Resolver.Boons = Boons;
            }
        }

        /// <summary>A boon was taken: the stats changed, so tell everything that draws them.</summary>
        private void OnBoonsChanged()
        {
            Resolver?.NotifyGearChanged();
        }

        /// <summary>
        /// Pays XP for one kill and turns it into levels. Called from the kill reward path, so gold and XP always
        /// come from the same event - a kill can never pay one and not the other.
        /// </summary>
        private void AwardKillXp()
        {
            if (Progression == null)
            {
                return;
            }

            bool isBoss = combatManager != null && combatManager.IsBossWave;
            int levels = Progression.AddXp(Progression.XpForKill(CurrentStage, isBoss));

            RaisePartyXpChanged();

            if (levels > 0)
            {
                // One event per kill, not per level: the card reads PendingPicks, so crossing two levels at once
                // simply deals two choices back to back.
                GameEvents.RaisePartyLevelGained(Progression.Level, Progression.PendingPicks);
                OfferBoonChoiceIfNeeded();
            }
        }

        /// <summary>Tells the HUD where the bar stands (level, XP, XP needed).</summary>
        private void RaisePartyXpChanged()
        {
            if (Progression == null)
            {
                return;
            }

            GameEvents.RaisePartyXpChanged(Progression.Level, Progression.Xp, Progression.RequiredXp);
        }

        // ------------------------------------------------------------------
        // The level-up card
        // ------------------------------------------------------------------
        /// <summary>
        /// Opens the card when the run owes a choice: deals the cards, stops the fight, tells the UI. Safe to call
        /// often - it does nothing when no pick is owed or the card is already up.
        /// </summary>
        public void OfferBoonChoiceIfNeeded()
        {
            if (Progression == null || Progression.PendingPicks <= 0 || BoonCardOpen)
            {
                return;
            }

            RollBoonOffer();

            if (!BoonCardOpen)
            {
                // Nothing left to deal (every card maxed): pay the picks out and carry on instead of soft-locking.
                while (Progression.TrySpendPick())
                {
                }

                RaisePartyXpChanged();
                return;
            }

            combatManager?.Pause(CombatManager.PauseReasons.Boon);
            Save?.MarkDirty("boon-offer");
            GameEvents.RaiseBoonOfferChanged();
        }

        /// <summary>Takes one of the offered cards. Spends one pick, then deals the next or resumes the fight.</summary>
        public bool ChooseBoon(PerkData boon)
        {
            if (boon == null || Progression == null || !boonOffer.Contains(boon) || !Progression.TrySpendPick())
            {
                return false;
            }

            Boons.Add(boon);
            Save?.MarkDirty("boon");
            boonOffer.Clear();

            if (Progression.PendingPicks > 0)
            {
                RollBoonOffer();   // the card stays open and deals the next pick
            }
            else
            {
                combatManager?.Resume(CombatManager.PauseReasons.Boon);
            }

            RaisePartyXpChanged();
            GameEvents.RaiseBoonOfferChanged();
            return true;
        }

        /// <summary>Re-rolls the three cards, if the run still has a free re-roll.</summary>
        public bool RerollBoonOffer()
        {
            if (!BoonCardOpen || BoonRerollsLeft <= 0)
            {
                return false;
            }

            boonRerollsUsed++;
            RollBoonOffer();
            Save?.MarkDirty("boon-reroll");
            GameEvents.RaiseBoonOfferChanged();
            return true;
        }

        private void RollBoonOffer()
        {
            boonOffer.Clear();

            if (boonOffers == null || Progression == null)
            {
                return;
            }

            // Seeded, not random: the deal is written to the save, so the same run and level always deal the same
            // hand. Re-rolling changes the seed, which is the only way a hand may change.
            int seed = Progression.Level * 7919 + boonRerollsUsed * 104729 + RunKills;
            boonOffer.AddRange(boonOffers.Roll(Boons, seed));
        }

        /// <summary>The boon fields of the save: the hand, the re-rolls spent, and the cards on screen.</summary>
        private void WriteBoonState(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            Boons?.WriteToSave(data);
            data.runBoonRerolls = boonRerollsUsed;
            data.runBoonOffer = new List<string>(boonOffer.Count);

            for (int i = 0; i < boonOffer.Count; i++)
            {
                if (boonOffer[i] != null)
                {
                    data.runBoonOffer.Add(boonOffer[i].BoonID);
                }
            }
        }

        /// <summary>Restores the hand and the hand on screen, so a reload cannot re-deal for free.</summary>
        private void ReadBoonState(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            Boons?.LoadFromSave(data, boonPool);
            boonRerollsUsed = Mathf.Max(0, data.runBoonRerolls);

            boonOffer.Clear();

            if (data.runBoonOffer != null && boonPool != null)
            {
                for (int i = 0; i < data.runBoonOffer.Count; i++)
                {
                    PerkData boon = boonPool.Find(data.runBoonOffer[i]);

                    if (boon != null)
                    {
                        boonOffer.Add(boon);
                    }
                }
            }
        }

        /// <summary>
        /// Dev/testing: banks party XP through the SAME path a kill uses, so the bar, the pick counter and the card
        /// all behave exactly as they would in play. Returns the number of levels gained.
        /// </summary>
        public int DebugGrantPartyXp(double amount)
        {
            if (Progression == null)
            {
                return 0;
            }

            int levels = Progression.AddXp(amount);
            RaisePartyXpChanged();

            if (levels > 0)
            {
                GameEvents.RaisePartyLevelGained(Progression.Level, Progression.PendingPicks);
                OfferBoonChoiceIfNeeded();
            }

            return levels;
        }

        /// <summary>
        /// Clears this run's level, XP, picks and boons. Called by the run reset next to the camp and gold reset -
        /// all of it is run-scoped, exactly like run gold.
        /// </summary>
        private void ResetPartyProgressionForNewRun()
        {
            if (Progression == null)
            {
                return;
            }

            Progression.ResetForNewRun();
            Boons?.ResetForNewRun();

            boonRerollsUsed = 0;
            boonOffer.Clear();

            // The card must never be left holding a fight that no longer exists.
            combatManager?.Resume(CombatManager.PauseReasons.Boon);

            RaisePartyXpChanged();
            GameEvents.RaisePartyLevelGained(Progression.Level, Progression.PendingPicks);
            GameEvents.RaiseBoonOfferChanged();
        }
    }
}


