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
