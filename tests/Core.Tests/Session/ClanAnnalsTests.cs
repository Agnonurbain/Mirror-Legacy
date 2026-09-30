using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The clan's Annals (LORE.md §11.9, B3a): the milestones of a game, each dated and kept once — the first
    /// member to reach each realm, the first to take each Golden Core position, each new generation.
    /// </summary>
    [TestFixture]
    public class ClanAnnalsTests
    {
        private static GameSession Quiet() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        [Test]
        public void TheFirstMemberToReachARealm_IsRecorded_WithTheYearAndTheirName()
        {
            var s = Quiet();
            var c = s.Clan.LivingMembers[0];
            s.Events.TriggerBreakthroughSuccess(c, CultivationRealm.Foundation);

            var entry = s.Annals.Entries.Single(e => e.Kind == AnnalKind.RealmReached);
            Assert.AreEqual(new AnnalEntry(AnnalKind.RealmReached, s.Clock.Year, (int)CultivationRealm.Foundation, c.FullName), entry);
        }

        [Test]
        public void ASecondMemberReachingTheSameRealm_AddsNothing()
        {
            var s = Quiet();
            s.Events.TriggerBreakthroughSuccess(s.Clan.LivingMembers[0], CultivationRealm.Foundation);
            s.Events.TriggerBreakthroughSuccess(s.Clan.LivingMembers[1], CultivationRealm.Foundation);
            Assert.AreEqual(1, s.Annals.Entries.Count(e => e.Kind == AnnalKind.RealmReached));
        }

        [Test]
        public void TheFirstPositionOfEachKind_IsRecorded_Once()
        {
            var s = Quiet();
            var c = s.Clan.LivingMembers[0];
            s.Events.TriggerPositionTaken(c, GoldenCoreState.Realization, GoldenCoreState.MetallicEssenceOnly);
            s.Events.TriggerPositionTaken(s.Clan.LivingMembers[1], GoldenCoreState.Realization, GoldenCoreState.MetallicEssenceOnly);
            s.Events.TriggerPositionTaken(c, GoldenCoreState.Surplus, GoldenCoreState.MetallicEssenceOnly);

            CollectionAssert.AreEquivalent(new[] { (int)GoldenCoreState.Realization, (int)GoldenCoreState.Surplus },
                s.Annals.Entries.Where(e => e.Kind == AnnalKind.PositionTaken).Select(e => e.Value));
        }

        [Test]
        public void ANewGeneration_IsRecorded_UnderItsPatriarch()
        {
            var s = Quiet();
            var founder = s.Clan.GetPatriarch();
            var heir = s.Clan.LivingMembers.First(m => m != founder);
            s.Clan.AppointPatriarch(heir);

            s.Events.TriggerYearStarted(s.Clock.Year);

            Assert.AreEqual(new AnnalEntry(AnnalKind.Generation, s.Clock.Year, 2, heir.FullName),
                s.Annals.Entries.Single(e => e.Kind == AnnalKind.Generation));
        }

        [Test]
        public void TheAnnals_SurviveASave()
        {
            var s = Quiet();
            s.Events.TriggerBreakthroughSuccess(s.Clan.LivingMembers[0], CultivationRealm.Foundation);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            CollectionAssert.AreEqual(s.Annals.Entries, reloaded.Annals.Entries);
            reloaded.Events.TriggerBreakthroughSuccess(reloaded.Clan.LivingMembers[1], CultivationRealm.Foundation);
            Assert.AreEqual(1, reloaded.Annals.Entries.Count, "a milestone kept is not recorded twice after a load");
        }

        [Test]
        public void AnOlderSave_KnowsThePositionsItsMembersAlreadyHeld()
        {
            var old = Quiet();
            var holder = old.Clan.LivingMembers[0];
            holder.Realm = CultivationRealm.GoldenCore;
            holder.GoldenCore = GoldenCoreState.Realization;
            var data = old.ToSaveData();
            data.Annals = null;

            var s = GameSession.FromSaveData(data, Fixtures.Setup());
            s.Events.TriggerPositionTaken(s.Clan.LivingMembers[1], GoldenCoreState.Realization, GoldenCoreState.MetallicEssenceOnly);

            Assert.IsFalse(s.Annals.Entries.Any(e => e.Kind == AnnalKind.PositionTaken), "the clan held a Realization before this save's Annals");
        }

        [Test]
        public void AnOlderSave_StartsWithEmptyAnnals()
        {
            var data = Quiet().ToSaveData();
            data.Annals = null;
            Assert.IsEmpty(GameSession.FromSaveData(data, Fixtures.Setup()).Annals.Entries);
        }

        [Test]
        public void TheAnnalsView_TellsEachMilestone_InFrench()
        {
            var s = Quiet();
            var c = s.Clan.LivingMembers[0];
            s.Events.TriggerBreakthroughSuccess(c, CultivationRealm.Foundation);
            s.Events.TriggerPositionTaken(c, GoldenCoreState.Realization, GoldenCoreState.MetallicEssenceOnly);

            var lines = AnnalsView.Lines(s);

            Assert.AreEqual($"An {s.Clock.Year} : {c.FullName} atteint l'Établissement des Fondations, une première pour le clan.", lines[0]);
            Assert.AreEqual($"An {s.Clock.Year} : {c.FullName} obtient la première Réalisation d'une Fruition du clan.", lines[1]);
        }
    }
}
