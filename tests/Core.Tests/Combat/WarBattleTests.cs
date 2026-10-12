using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>
    /// A war's yearly battle fought on the grid (G6; the user's choices of 2026-10-12): the clan picks up to five fighters
    /// against the enemy's leader and its soldiers a realm below, a soldier per thousand of its power, each ally who came
    /// keeping one off the field. Won, the enemy weakened and looted; lost, the war's toll, a fallen fighter may die; left,
    /// a smaller toll. Several wars' battles wait in turn; chance may settle any, and headless runs always do.
    /// </summary>
    [TestFixture]
    public class WarBattleTests
    {
        private const string Ruan = "Famille Ruan";
        private const string Lou = "Famille Lou";
        private static WarSettings Settings => Fixtures.VeteranContent.Balance.Wars;

        private static (TestWorld w, CharacterData fighter) World(Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Resources.AddSpiritStones(1000);
            var fighter = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));
            w.Clan.AppointPatriarch(fighter);
            return (w, fighter);
        }

        private static FactionData Power(TestWorld w, string name = Ruan) => w.Factions.GetFactionByName(name);

        private static ClanWar AtWar(TestWorld w, string name = Ruan)
        {
            w.Wars.DeclareOn(name);
            return w.Wars.War(name);
        }

        private static (TestWorld w, CharacterData fighter) Sprung(Random rng, int powerLevel = 150, int allies = 0)
        {
            var (w, fighter) = World(rng);
            Power(w).PowerLevel = powerLevel;
            Power(w).HighestRealm = CultivationRealm.QiRefinement;
            w.WarBattles.Spring(AtWar(w), Power(w), allies);
            return (w, fighter);
        }

        private static void Win(TestWorld w)
        {
            foreach (var foe in w.WarBattles.Current.Enemies) foe.TakeDamage(foe.MaxVitality);
            w.WarBattles.Current.EndTurn();
        }

        private static void Lose(TestWorld w)
        {
            foreach (var ally in w.WarBattles.Current.Allies) ally.TakeDamage(100000);
            w.WarBattles.Current.EndTurn();
        }

        // ---- The battle waits ----

        [Test]
        public void Spring_LeavesTheBattleWaiting_WhenTheClanHasAFighter()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            Assert.AreEqual(Ruan, w.WarBattles.Pending.Enemy);
        }

        [Test]
        public void Spring_LeavesItToChance_WhenNobodyCanFight()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Mortal()));
            Assert.IsFalse(w.WarBattles.Spring(AtWar(w), Power(w), 0));
        }

        [Test]
        public void Spring_QueuesTheBattles_OfSeveralWars()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            w.WarBattles.Spring(AtWar(w, Lou), Power(w, Lou), 0);
            CollectionAssert.AreEqual(new[] { Ruan, Lou }, w.WarBattles.Queue.Select(b => b.Enemy));
        }

        // ---- The enemy's army ----

        [Test]
        public void Army_IsLed_AtThePowersStrongestRealm()
        {
            var (w, _) = World(new FixedRandom(0.0));
            Power(w).HighestRealm = CultivationRealm.Foundation;
            w.WarBattles.Spring(AtWar(w), Power(w), 0);
            Assert.AreEqual(CultivationRealm.Foundation, w.WarBattles.Army(w.WarBattles.Pending)[0].Realm);
        }

        [Test]
        public void Army_SoldiersStand_ARealmBelowTheLeader()
        {
            var (w, _) = World(new FixedRandom(0.0));
            Power(w).HighestRealm = CultivationRealm.Foundation;
            Power(w).PowerLevel = 3000;
            w.WarBattles.Spring(AtWar(w), Power(w), 0);
            Assert.IsTrue(w.WarBattles.Army(w.WarBattles.Pending).Skip(1).All(s => s.Realm == CultivationRealm.QiRefinement));
        }

        [Test]
        public void Army_SendsASoldier_PerThousandOfPower()
        {
            var (w, _) = Sprung(new FixedRandom(0.0), powerLevel: 2500);
            Assert.AreEqual(1 + 2500 / Settings.ArmyPowerPerSoldier, w.WarBattles.Army(w.WarBattles.Pending).Count);
        }

        [Test]
        public void Army_SendsAtMost_TheSettingsSoldiers()
        {
            var (w, _) = Sprung(new FixedRandom(0.0), powerLevel: 100000);
            Assert.AreEqual(1 + Settings.ArmyMaxSoldiers, w.WarBattles.Army(w.WarBattles.Pending).Count);
        }

        [Test]
        public void Army_LosesASoldier_PerAllyWhoCame()
        {
            var (w, _) = Sprung(new FixedRandom(0.0), powerLevel: 3000, allies: 2);
            Assert.AreEqual(1 + 3 - 2, w.WarBattles.Army(w.WarBattles.Pending).Count);
        }

        [Test]
        public void Army_KeepsItsLeader_WhateverTheAllies()
        {
            var (w, _) = Sprung(new FixedRandom(0.0), powerLevel: 1000, allies: 5);
            Assert.AreEqual(1, w.WarBattles.Army(w.WarBattles.Pending).Count);
        }

        // ---- The clan answers ----

        [Test]
        public void Fight_OpensTheBattle_WithTheChosen()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            Assert.IsNull(w.WarBattles.Fight(new[] { fighter.ID }));
            Assert.AreSame(fighter, w.WarBattles.Current.Allies.Single().BaseData);
        }

        [Test]
        public void Fight_Refuses_MoreThanTheSettingsFighters()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            var many = Enumerable.Range(0, Settings.WarMaxFighters).Select(_ => w.Join(Fixtures.Cultivator()).ID).Append(fighter.ID).ToList();
            Assert.IsNotNull(w.WarBattles.Fight(many));
        }

        [Test]
        public void Fight_Refuses_AMemberWhoCannotFight()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            Assert.IsNotNull(w.WarBattles.Fight(new[] { w.Join(Fixtures.Mortal()).ID }));
        }

        // ---- The battle's end ----

        [Test]
        public void Conclude_WeakensTheEnemy_WhenTheClanWins()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0), powerLevel: 1000);
            w.WarBattles.Fight(new[] { fighter.ID });
            Win(w);
            w.WarBattles.Conclude();
            Assert.Less(Power(w).PowerLevel, 1000);
        }

        [Test]
        public void Conclude_SparesTheFallen_WhenTheClanWins()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            var second = w.Join(Fixtures.Cultivator());
            w.WarBattles.Fight(new[] { fighter.ID, second.ID });
            w.WarBattles.Current.Allies.First(a => a.BaseData == second).TakeDamage(100000);
            Win(w);
            w.WarBattles.Conclude();
            Assert.IsTrue(second.IsAlive);
        }

        [Test]
        public void Conclude_CostsTheWarsToll_WhenTheClanLoses()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.999));
            int stones = w.Resources.SpiritStones;
            w.WarBattles.Fight(new[] { fighter.ID });
            Lose(w);
            w.WarBattles.Conclude();
            Assert.AreEqual(stones - (int)(stones * Settings.ClanWarStonesLoss), w.Resources.SpiritStones);
        }

        [Test]
        public void Conclude_MayKillAFallenFighter_WhenTheClanLoses()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0)); // within the war's death chance
            w.WarBattles.Fight(new[] { fighter.ID });
            Lose(w);
            w.WarBattles.Conclude();
            Assert.IsFalse(fighter.IsAlive);
        }

        [Test]
        public void Conclude_CostsASmallerToll_WhenTheClanWithdraws()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            int stones = w.Resources.SpiritStones;
            w.WarBattles.Fight(new[] { fighter.ID });
            foreach (var ally in w.WarBattles.Current.Allies) ally.Flee();
            w.WarBattles.Current.EndTurn();
            w.WarBattles.Conclude();
            Assert.AreEqual(stones - (int)(stones * Settings.ClanWarStonesLoss * Settings.WithdrawStonesFactor), w.Resources.SpiritStones);
        }

        [Test]
        public void Conclude_TellsHowItEnded()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            WarBattleOutcome? outcome = null;
            w.Ctx.Events.OnWarBattleSettled += (_, o) => outcome = o;
            w.WarBattles.Fight(new[] { fighter.ID });
            Win(w);
            w.WarBattles.Conclude();
            Assert.AreEqual(WarBattleOutcome.Won, outcome);
        }

        [Test]
        public void Conclude_LeavesTheNextWarsBattle_Waiting()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            w.WarBattles.Spring(AtWar(w, Lou), Power(w, Lou), 0);
            w.WarBattles.Fight(new[] { fighter.ID });
            Win(w);
            w.WarBattles.Conclude();
            Assert.AreEqual((Lou, (object)null), (w.WarBattles.Pending.Enemy, (object)w.WarBattles.Current));
        }

        [Test]
        public void Pending_ForgetsABattle_OnceItsWarIsOver()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            w.Wars.SuePeace(Ruan);
            Assert.IsNull(w.WarBattles.Pending, "peace made: nothing left to fight");
        }

        [Test]
        public void Conclude_CostsNothing_WhenPeaceCameDuringTheBattle()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            w.WarBattles.Fight(new[] { fighter.ID });
            w.Wars.SuePeace(Ruan);
            int stones = w.Resources.SpiritStones;
            Lose(w);
            w.WarBattles.Conclude();
            Assert.AreEqual((stones, true), (w.Resources.SpiritStones, fighter.IsAlive));
        }

        [Test]
        public void Fight_IsRefused_WhileABattleIsUnderWay()
        {
            var (w, fighter) = Sprung(new FixedRandom(0.0));
            w.WarBattles.Fight(new[] { fighter.ID });
            Assert.IsNotNull(w.WarBattles.Fight(new[] { fighter.ID }));
        }

        [Test]
        public void Spring_QueuesAWarOnce()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            w.WarBattles.Spring(w.Wars.War(Ruan), Power(w), 0);
            Assert.AreEqual(1, w.WarBattles.Queue.Count);
        }

        [Test]
        public void LetBe_SettlesTheBattleByChance()
        {
            var (w, _) = Sprung(new FixedRandom(0.0), powerLevel: 1000); // chance always wins it
            Assert.IsNull(w.WarBattles.LetBe());
            Assert.Less(Power(w).PowerLevel, 1000);
        }

        // ---- In the session ----

        [Test]
        public void AHeadlessSession_LeavesTheWarsBattlesToChance()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1));
            s.Wars.DeclareOn(Ruan);
            s.Wars.ProcessYear();
            Assert.IsNull(s.WarBattles.Pending);
        }

        [Test]
        public void TheYear_WaitsForAWarsBattle()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1) with { InteractiveBattles = true });
            s.Wars.DeclareOn(Ruan);
            s.WarBattles.Restore(new[] { new PendingWarBattle(Ruan, s.Clock.Year, CultivationRealm.QiRefinement, CultivationRealm.Embryonic, 1, 0, 7) });
            var (year, phase) = (s.Clock.Year, s.Clock.Phase);
            s.AdvancePhase();
            Assert.AreEqual((year, phase), (s.Clock.Year, s.Clock.Phase), "a war's battle is answered first");
        }

        [Test]
        public void RoundTrip_KeepsTheBattlesWaiting()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1));
            var battles = new[]
            {
                new PendingWarBattle(Ruan, 2, CultivationRealm.Foundation, CultivationRealm.QiRefinement, 2, 1, 7),
                new PendingWarBattle(Lou, 2, CultivationRealm.QiRefinement, CultivationRealm.Embryonic, 0, 0, 9)
            };
            s.WarBattles.Restore(battles);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.VeteranSetup());
            CollectionAssert.AreEqual(battles, reloaded.WarBattles.Queue);
        }
    }
}
