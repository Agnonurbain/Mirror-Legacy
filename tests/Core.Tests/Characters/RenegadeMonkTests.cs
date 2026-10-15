using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The renegade monk (L7; the user's choice of 2026-10-15): a captive of the clan converted under duress by a temple becomes
    /// a named elder of that temple, and the clan meets it again — as the temple's champion in the Dao debates, where it knows
    /// the clan (the clan's odds fall), and as the leader of the temple's army in a war; beaten on the field, it may be taken
    /// back and won over to the clan again.
    /// </summary>
    [TestFixture]
    public class RenegadeMonkTests
    {
        private const string Modern = "Temple du Lotus";

        private static ConversionSettings Settings => Fixtures.Content.Balance.Conversions;
        private static DebateSettings Debate => Fixtures.Content.Balance.Buddhism.Debates;

        private static TestWorld World(double sample)
        {
            var w = new TestWorld(new FixedRandom(sample));
            w.Factions.InitializeFactions();
            w.Resources.AddSpiritStones(1000);
            return w;
        }

        private static FactionData Temple(TestWorld w) => w.Factions.GetFactionByName(Modern);

        /// <summary>A member of the clan, captive of the temple, converted under duress.</summary>
        private static (CharacterData captive, FactionElder renegade) Renegade(TestWorld w)
        {
            var captive = w.Join(Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation, stage: 2));
            captive.MaxLifespan = captive.Age + 200;
            w.Captives.Take(captive, Modern);
            w.Buddhism.ProcessYear();
            return (captive, Temple(w).Elders.SingleOrDefault(e => e.RenegadeId == captive.ID));
        }

        // ---- The renegade joins its temple ----

        [Test]
        public void AConvertedCaptive_BecomesAnElderOfItsTemple()
        {
            var w = World(0.0);
            var (captive, renegade) = Renegade(w);
            Assert.IsNotNull(renegade);
            Assert.AreEqual(captive.FullName, renegade.Name);
            Assert.AreEqual(captive.Realm, renegade.Realm);
            Assert.AreEqual(captive.Age, renegade.Age(w.Ctx.Clock.Year));
        }

        [Test]
        public void AnElderOfAnotherPower_TurningToATemple_IsNoRenegade()
        {
            var w = World(0.0);
            var renegades = w.Factions.Factions.SelectMany(f => f.Elders).Where(e => e.RenegadeId != null);
            Assert.IsEmpty(renegades);
        }

        // ---- In the Dao debates ----

        [Test]
        public void TheRenegade_KnowsTheClan_InTheDebates()
        {
            var w = World(0.0);
            var (_, renegade) = Renegade(w);
            var debater = w.Join(Fixtures.Cultivator(age: 40, realm: CultivationRealm.Foundation, stage: 3));
            int with = w.Debates.Odds(debater, Modern);
            Temple(w).Elders.Remove(renegade);
            int without = w.Debates.Odds(debater, Modern);
            Assert.AreEqual(Math.Clamp(without - Settings.RenegadeDebateEdge, Debate.MinChance, Debate.MaxChance), with);
            Assert.Less(with, without);
        }

        [Test]
        public void TheDiplomacyScreen_NamesTheRenegade_AmongTheTemplesDebaters()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var temple = s.Factions.GetFactionByName(Modern);
            temple.Elders.Add(new FactionElder { Id = "renegade-x", Name = "Mo Lin", Realm = CultivationRealm.Foundation, Stage = 1, BornYear = -30, MaxLifespan = 200, RenegadeId = "x" });
            var target = DiplomacyView.DebateTargets(s).Single(t => t.Temple == Modern);
            Assert.AreEqual("Mo Lin", target.Renegade);
        }

        // ---- At war ----

        private static void AtWar(TestWorld w)
        {
            Temple(w).PowerLevel = 150;
            w.Wars.DeclareOn(Modern);
            w.WarBattles.Spring(w.Wars.War(Modern), Temple(w), 0);
        }

        [Test]
        public void TheRenegade_LeadsItsTemplesArmy()
        {
            var w = World(0.0);
            var (captive, renegade) = Renegade(w);
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5)));
            AtWar(w);
            var leader = w.WarBattles.Army(w.WarBattles.Pending)[0];
            Assert.AreEqual(captive.FullName, leader.FullName);
            Assert.AreEqual(renegade.Realm, leader.Realm);
            Assert.AreEqual(CultivationPath.Buddhist, leader.Path);
        }

        [Test]
        public void TheRenegade_BeatenOnTheField_MayBeTakenBack()
        {
            var w = World(0.0);
            var (captive, _) = Renegade(w);
            var fighter = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));
            w.Clan.AppointPatriarch(fighter);
            AtWar(w);
            CharacterData taken = null;
            w.Ctx.Events.OnRenegadeTakenBack += (m, temple) => taken = m;
            w.WarBattles.Fight(new[] { fighter.ID });
            foreach (var foe in w.WarBattles.Current.Enemies) foe.TakeDamage(foe.MaxVitality);
            w.WarBattles.Current.EndTurn();
            w.WarBattles.Conclude();
            Assert.AreSame(captive, taken);
            Assert.IsFalse(captive.Departed);
            CollectionAssert.Contains(w.Clan.LivingMembers, captive);
            Assert.IsFalse(Temple(w).Elders.Any(e => e.RenegadeId == captive.ID));
        }

        [Test]
        public void TheRenegade_NotBeaten_StaysWithItsTemple()
        {
            var w = World(0.0);
            var (captive, _) = Renegade(w);
            var fighter = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));
            w.Clan.AppointPatriarch(fighter);
            AtWar(w);
            w.WarBattles.Fight(new[] { fighter.ID });
            foreach (var ally in w.WarBattles.Current.Allies) ally.TakeDamage(100000);
            w.WarBattles.Current.EndTurn();
            w.WarBattles.Conclude();
            Assert.IsTrue(captive.Departed);
            Assert.IsTrue(Temple(w).Elders.Any(e => e.RenegadeId == captive.ID));
        }

        // ---- Kept ----

        [Test]
        public void TheRenegade_IsKept_AcrossASave()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Factions.GetFactionByName(Modern).Elders.Add(new FactionElder { Id = "renegade-x", Name = "Mo Lin", Realm = CultivationRealm.Foundation, Stage = 1, BornYear = -30, MaxLifespan = 200, RenegadeId = "x" });
            var loaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            Assert.AreEqual("x", loaded.Factions.GetFactionByName(Modern).Elders.Single(e => e.Id == "renegade-x").RenegadeId);
        }
    }
}
