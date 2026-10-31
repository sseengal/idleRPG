using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using IdleRPG.Core;

namespace IdleRPG.Tests
{
    /// <summary>
    /// The battle log is the single source of truth for the feed. These tests pin the promises the view relies on:
    /// one call = one entry, oldest-first order, a capped history, a clean reset, and no listener able to starve
    /// the next one.
    /// </summary>
    public sealed class BattleLogTests
    {
        [SetUp]
        public void SetUp()
        {
            BattleLog.Uninstall();
            BattleLog.HeroNameResolver = null;
            BattleLog.EnemyNameResolver = null;
            BattleLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            BattleLog.Uninstall();
            BattleLog.HeroNameResolver = null;
            BattleLog.EnemyNameResolver = null;
            BattleLog.Clear();
        }

        [Test]
        public void Append_WritesOneEntryPerCall_InOrder()
        {
            BattleLog.Append("alpha", BattleLogKind.Damage);
            BattleLog.Append("beta", BattleLogKind.Gold);

            Assert.AreEqual(2, BattleLog.Entries.Count);
            Assert.AreEqual("alpha", BattleLog.Entries[0].Text);
            Assert.AreEqual(BattleLogKind.Damage, BattleLog.Entries[0].Kind);
            Assert.AreEqual("beta", BattleLog.Entries[1].Text);
            Assert.AreEqual(BattleLogKind.Gold, BattleLog.Entries[1].Kind);
            Assert.Greater(BattleLog.Entries[1].Sequence, BattleLog.Entries[0].Sequence);
        }

        [Test]
        public void Append_IgnoresEmptyText()
        {
            BattleLog.Append(null);
            BattleLog.Append(string.Empty);

            Assert.AreEqual(0, BattleLog.Entries.Count);
        }

        [Test]
        public void Say_KeepsSpeakerApartFromTheLine()
        {
            BattleLog.Say("Knight", "Weapons ready!");

            Assert.AreEqual(1, BattleLog.Entries.Count);
            Assert.AreEqual("Knight", BattleLog.Entries[0].Speaker);
            Assert.AreEqual("Weapons ready!", BattleLog.Entries[0].Text);
            Assert.AreEqual(BattleLogKind.Dialogue, BattleLog.Entries[0].Kind);
            Assert.AreEqual("Knight: Weapons ready!", BattleLog.Entries[0].ToString());
        }

        [Test]
        public void Say_WithoutSpeaker_StoresTheLineAlone()
        {
            BattleLog.Say(null, "The forest is quiet.");

            Assert.AreEqual(1, BattleLog.Entries.Count);
            Assert.IsTrue(string.IsNullOrEmpty(BattleLog.Entries[0].Speaker));
            Assert.AreEqual("The forest is quiet.", BattleLog.Entries[0].Text);
        }

        [Test]
        public void Append_DropsTheOldest_WhenTheHistoryIsFull()
        {
            int extra = 5;

            for (int i = 0; i < BattleLog.Capacity + extra; i++)
            {
                BattleLog.Append("line " + i);
            }

            Assert.AreEqual(BattleLog.Capacity, BattleLog.Entries.Count);
            Assert.AreEqual("line " + extra, BattleLog.Entries[0].Text);
            Assert.AreEqual("line " + (BattleLog.Capacity + extra - 1), BattleLog.Entries[BattleLog.Entries.Count - 1].Text);
        }

        [Test]
        public void Clear_EmptiesTheHistory_AndTellsListeners()
        {
            int cleared = 0;
            Action listener = () => cleared++;

            BattleLog.Cleared += listener;
            BattleLog.Append("something");
            BattleLog.Clear();
            BattleLog.Cleared -= listener;

            Assert.AreEqual(0, BattleLog.Entries.Count);
            Assert.AreEqual(1, cleared);
        }

        [Test]
        public void Sequence_KeepsGrowing_AcrossAClear()
        {
            BattleLog.Append("before");
            long first = BattleLog.Entries[0].Sequence;

            BattleLog.Clear();
            BattleLog.Append("after");

            Assert.Greater(BattleLog.Entries[0].Sequence, first);
        }

        [Test]
        public void Install_StartsACleanHistory()
        {
            BattleLog.Append("stale session line");

            BattleLog.Install();

            Assert.AreEqual(0, BattleLog.Entries.Count);
            Assert.AreEqual(0, BattleLog.TotalAppended);
        }

        [Test]
        public void Appended_OneBrokenListener_DoesNotBlockTheNext()
        {
            int seen = 0;
            Action<BattleLogEntry> broken = entry => throw new InvalidOperationException("boom");
            Action<BattleLogEntry> healthy = entry => seen++;

            LogAssert.Expect(LogType.Error, new Regex(@"\[BattleLog\] A listener of 'Appended' threw"));

            BattleLog.Appended += broken;
            BattleLog.Appended += healthy;

            BattleLog.Append("still arrives");

            BattleLog.Appended -= broken;
            BattleLog.Appended -= healthy;

            Assert.AreEqual(1, seen);
            Assert.AreEqual(1, BattleLog.Entries.Count);
        }
    }
}
