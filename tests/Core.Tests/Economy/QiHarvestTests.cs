using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Economy
{
    /// <summary>
    /// The harvest of spiritual Qi follows the place (AUDIT_LORE.md §7.8; LORE.md §2.5): the clan harvests at home only a Qi
    /// the place offers, and as much as it abounds there — the place's density times its lineage's sway. A Qi absent from
    /// home comes by an accord, a reserve or a journey, never by the yearly task.
    /// </summary>
    [TestFixture]
    public class QiHarvestTests
    {
        private const string Ember = "ember-phoenix-qi"; // a fire Qi: the lake does not offer it

        private static CharacterData Harvester(TestWorld w, string method = null)
        {
            w.RuleEveryLineage();
            var member = w.Join(Fixtures.Cultivator());
            if (method != null) member.CultivationMethodId = method;
            member.CurrentTask = TaskType.GatherQi;
            return member;
        }

        private static void Years(TestWorld w, int years)
        {
            for (int y = 0; y < years; y++) w.Tasks.ProcessYearlyTasks();
        }

        [Test]
        public void AQiAbsentFromHome_IsNeverHarvested()
        {
            var w = new TestWorld();
            w.Techniques.Learn("ember-phoenix-stride");
            Harvester(w, "ember-phoenix-stride");
            Years(w, 5);
            Assert.AreEqual(0, w.Resources.QiPortions(Ember));
        }

        [Test]
        public void SentForAnAbsentQi_TheHarvesterTakesAQiOfHome()
        {
            var w = new TestWorld();
            w.Techniques.Learn(Fixtures.ClanMethod);
            w.Techniques.Learn("ember-phoenix-stride");
            Harvester(w).HarvestQiId = Ember;
            Years(w, 2);
            Assert.AreEqual(0, w.Resources.QiPortions(Ember));
            Assert.Greater(w.Resources.QiPortions(Fixtures.ClanQi), 0);
        }

        // ---- The pilot sends no harvester for a Qi absent from home ----

        private static MirrorChronicles.Session.GameSession Session()
        {
            var s = MirrorChronicles.Session.GameSession.NewGame(new MirrorChronicles.Session.GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Techniques.Learn("ember-phoenix-stride");
            return s;
        }

        [Test]
        public void ThePilot_AtTheWall_DoesNotHarvestItsOwnQi_WhenHomeLacksIt()
        {
            var s = Session();
            var walled = Fixtures.Cultivator(age: 30, realm: CultivationRealm.QiRefinement, stage: 9);
            walled.CultivationMethodId = "ember-phoenix-stride";
            walled.QiId = Ember;
            s.Clan.AddMember(walled);
            MirrorChronicles.Session.BalanceRun.SetTheIdleToWork(s);
            Assert.AreNotEqual(TaskType.GatherQi, walled.CurrentTask);
        }

        [Test]
        public void ThePilot_GathersTheQiOfItsBestMethod_OnlyAmongTheQiOfHome()
        {
            var s = Session(); // its best method's Qi is foreign; the Clear Spring's is home's, and the clan holds enough of it
            s.Resources.AddQi(Fixtures.ClanQi, 10);
            var youth = Fixtures.Cultivator(age: 16, realm: CultivationRealm.Embryonic, stage: 5);
            s.Clan.AddMember(youth);
            MirrorChronicles.Session.BalanceRun.SetTheIdleToWork(s);
            Assert.IsFalse(s.Clan.LivingMembers.Any(m => m.CurrentTask == TaskType.GatherQi));
        }

        [Test]
        public void TheHarvest_FollowsTheQisAbundanceAtHome()
        {
            var w = new TestWorld();
            w.Techniques.Learn(Fixtures.ClanMethod);
            Harvester(w);
            Years(w, 10);
            var spring = Fixtures.Content.Qi.Single(q => q.Id == Fixtures.ClanQi);
            double abundance = w.Place.Abundance(Fixtures.Content.Clan.HomeRegion, spring); // the lake's density, a held lineage
            Assert.AreEqual((int)(10 * abundance / spring.YearsPerPortion + 1e-9), w.Resources.QiPortions(Fixtures.ClanQi));
        }
    }
}
