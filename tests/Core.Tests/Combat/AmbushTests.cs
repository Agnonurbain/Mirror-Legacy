using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>
    /// An ambush fought on the grid (G6; the user's choices of 2026-10-11): when a power's ambush is not foiled and a player
    /// is there, the members away with the one it struck face its agents — a realm below the power's strongest, never below
    /// the member struck. Beaten off, an agent is taken; beaten, the member struck is taken; nobody dies. The clan may let
    /// chance settle it instead, and headless runs always do.
    /// </summary>
    [TestFixture]
    public class AmbushTests
    {
        private const string Ruan = "Famille Ruan";
        private static SchemeSettings Settings => Fixtures.VeteranContent.Balance.Schemes;

        private static (TestWorld w, CharacterData target) World(System.Random rng, CultivationRealm powerRealm = CultivationRealm.Foundation)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            Power(w).HighestRealm = powerRealm;
            var target = w.Join(Away(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3)));
            w.Clan.AppointPatriarch(target);
            return (w, target);
        }

        private static FactionData Power(TestWorld w) => w.Factions.GetFactionByName(Ruan);

        private static CharacterData Away(CharacterData member)
        {
            member.CurrentTask = TaskType.Diplomacy;
            return member;
        }

        private static (TestWorld w, CharacterData target) Sprung(System.Random rng)
        {
            var (w, target) = World(rng);
            w.Ambushes.Spring(Power(w), target);
            return (w, target);
        }

        private static void Win(TestWorld w)
        {
            foreach (var agent in w.Ambushes.Current.Enemies) agent.TakeDamage(agent.MaxVitality);
            w.Ambushes.Current.EndTurn(); // the field sees it
        }

        private static void Lose(TestWorld w)
        {
            foreach (var fighter in w.Ambushes.Current.Allies) fighter.TakeDamage(10000);
            w.Ambushes.Current.EndTurn();
        }

        // ---- The ambush springs ----

        [Test]
        public void Spring_LeavesTheAmbushPending_WhenTheMemberStruckCanFight()
        {
            var (w, target) = World(new FixedRandom(0.0));
            w.Ambushes.Spring(Power(w), target);
            Assert.AreEqual((Ruan, target.ID), (w.Ambushes.Pending.Faction, w.Ambushes.Pending.TargetId));
        }

        [Test]
        public void Spring_LeavesItToChance_WhenTheMemberStruckCannotFight()
        {
            var (w, _) = World(new FixedRandom(0.0));
            var mortal = w.Join(Away(Fixtures.Mortal()));
            Assert.IsFalse(w.Ambushes.Spring(Power(w), mortal));
        }

        [Test]
        public void Fighters_JoinTheMembersAway_ToTheOneStruck()
        {
            var (w, target) = Sprung(new FixedRandom(0.0));
            var companion = w.Join(Away(Fixtures.Cultivator()));
            CollectionAssert.AreEqual(new[] { target, companion }, w.Ambushes.Fighters(w.Ambushes.Pending));
        }

        [Test]
        public void Fighters_LeaveOut_TheMembersAtHome()
        {
            var (w, target) = Sprung(new FixedRandom(0.0));
            w.Join(Fixtures.Cultivator()); // no task abroad
            CollectionAssert.AreEqual(new[] { target }, w.Ambushes.Fighters(w.Ambushes.Pending));
        }

        [Test]
        public void Fighters_AreAtMost_TheSettingsNumber()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            for (int i = 0; i < Settings.AmbushMaxFighters + 2; i++) w.Join(Away(Fixtures.Cultivator()));
            Assert.AreEqual(Settings.AmbushMaxFighters, w.Ambushes.Fighters(w.Ambushes.Pending).Count);
        }

        [Test]
        public void Agents_StandBelowThePowersStrongest()
        {
            var (w, target) = World(new FixedRandom(0.0), powerRealm: CultivationRealm.PurpleMansion);
            w.Ambushes.Spring(Power(w), target);
            Assert.IsTrue(w.Ambushes.Agents(w.Ambushes.Pending).All(a => a.Realm == CultivationRealm.PurpleMansion - Settings.AgentRealmsBelowPower));
        }

        [Test]
        public void Agents_NeverStandBelowTheMemberStruck()
        {
            var (w, target) = World(new FixedRandom(0.0), powerRealm: CultivationRealm.QiRefinement);
            w.Ambushes.Spring(Power(w), target);
            Assert.IsTrue(w.Ambushes.Agents(w.Ambushes.Pending).All(a => a.Realm == target.Realm));
        }

        [Test]
        public void Agents_AreTheSameEachTime()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            var ambush = w.Ambushes.Pending;
            CollectionAssert.AreEqual(w.Ambushes.Agents(ambush).Select(a => a.FirstName), w.Ambushes.Agents(ambush).Select(a => a.FirstName));
        }

        [Test]
        public void Agents_AreBetweenOneAndTheSettingsNumber()
        {
            var (w, _) = Sprung(new FixedRandom(0.999));
            Assert.That(w.Ambushes.Agents(w.Ambushes.Pending).Count, Is.InRange(1, Settings.AmbushMaxAgents));
        }

        // ---- The clan answers ----

        [Test]
        public void Fight_OpensTheBattle_WithTheFighters()
        {
            var (w, target) = Sprung(new FixedRandom(0.0));
            Assert.IsNull(w.Ambushes.Fight());
            Assert.AreSame(target, w.Ambushes.Current.Allies.Single().BaseData);
        }

        [Test]
        public void Fight_IsRefused_WithoutAnAmbush()
        {
            var (w, _) = World(new FixedRandom(0.0));
            Assert.IsNotNull(w.Ambushes.Fight());
        }

        [Test]
        public void Conclude_IsRefused_WhileTheBattleGoesOn()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            w.Ambushes.Fight();
            StringAssert.Contains("pas finie", w.Ambushes.Conclude());
        }

        [Test]
        public void Conclude_TakesAnAgent_WhenTheClanWins()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            w.Ambushes.Fight();
            Win(w);
            w.Ambushes.Conclude();
            Assert.AreEqual(Ruan, w.Captives.Prisoners.Single().Faction);
        }

        [Test]
        public void Conclude_TakesTheMemberStruck_WhenTheClanLoses()
        {
            var (w, target) = Sprung(new FixedRandom(0.0));
            w.Ambushes.Fight();
            Lose(w);
            w.Ambushes.Conclude();
            Assert.AreEqual(Ruan, target.CaptorFaction);
        }

        [Test]
        public void Conclude_KillsNobody_WhenTheClanLoses()
        {
            var (w, target) = Sprung(new FixedRandom(0.0));
            var companion = w.Join(Away(Fixtures.Cultivator()));
            w.Ambushes.Fight();
            Lose(w);
            w.Ambushes.Conclude();
            Assert.IsTrue(target.IsAlive && companion.IsAlive, "the agents want a hostage, not a corpse");
        }

        [Test]
        public void Conclude_ClosesTheAmbush()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            w.Ambushes.Fight();
            Win(w);
            w.Ambushes.Conclude();
            Assert.IsNull(w.Ambushes.Current);
        }

        [Test]
        public void Conclude_TellsHowItEnded()
        {
            var (w, _) = Sprung(new FixedRandom(0.0));
            AmbushOutcome? outcome = null;
            w.Ctx.Events.OnAmbushSettled += (_, o) => outcome = o;
            w.Ambushes.Fight();
            Win(w);
            w.Ambushes.Conclude();
            Assert.AreEqual(AmbushOutcome.Repelled, outcome);
        }

        [Test]
        public void Conclude_TellsAnEscape_WhenNobodyIsLeftToTake()
        {
            var (w, target) = Sprung(new FixedRandom(0.0));
            AmbushOutcome? outcome = null;
            w.Ctx.Events.OnAmbushSettled += (_, o) => outcome = o;
            w.Ambushes.Fight();
            Lose(w);
            target.CaptorFaction = "Famille Lou"; // taken elsewhere before the battle is settled
            w.Ambushes.Conclude();
            Assert.AreEqual(AmbushOutcome.Escaped, outcome);
        }

        [Test]
        public void Conclude_LetsTheMemberStruckGo_WhenItFled()
        {
            var (w, target) = Sprung(new FixedRandom(0.0));
            w.Ambushes.Fight();
            foreach (var fighter in w.Ambushes.Current.Allies) fighter.Flee();
            w.Ambushes.Current.EndTurn();
            w.Ambushes.Conclude();
            Assert.IsNull(target.CaptorFaction, "fled, it is not taken");
        }

        [Test]
        public void LetBe_SettlesTheAmbushByChance()
        {
            var (w, target) = Sprung(new FixedRandom(0.0)); // chance always takes the member
            Assert.IsNull(w.Ambushes.LetBe());
            Assert.AreEqual((Ruan, (Ambush)null), (target.CaptorFaction, w.Ambushes.Pending));
        }

        // ---- In the session ----

        [Test]
        public void AHeadlessSession_LeavesAmbushesToChance()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1));
            var target = Away(s.Clan.LivingMembers.First());
            target.HasSpiritualOrifice = target.OrificeKnown = true;
            s.Schemes.Ambush(s.Factions.GetFactionByName(Ruan));
            Assert.IsNull(s.Ambushes.Pending);
        }

        [Test]
        public void TheYear_WaitsForAnAmbushPending()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1) with { InteractiveBattles = true });
            s.Ambushes.Restore(new Ambush(Ruan, s.Clock.Year, s.Clan.LivingMembers.First().ID, CultivationRealm.QiRefinement, 1, 1, 7));
            var (year, phase) = (s.Clock.Year, s.Clock.Phase);
            s.AdvancePhase();
            Assert.AreEqual((year, phase), (s.Clock.Year, s.Clock.Phase), "an ambush is answered first");
        }

        [Test]
        public void RoundTrip_KeepsThePendingAmbush()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1));
            var ambush = new Ambush(Ruan, 1, s.Clan.LivingMembers.First().ID, CultivationRealm.QiRefinement, 2, 3, 7);
            s.Ambushes.Restore(ambush);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.VeteranSetup());
            Assert.AreEqual(ambush, reloaded.Ambushes.Pending);
        }
    }
}
