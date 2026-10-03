using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// How far a realm goes (AUDIT_LORE.md §1.5, 2026-10-03): a Qi Cultivator rides the wind a few regions from home, a
    /// Foundation crosses its state, a Purple Mansion goes through the Great Void; a team goes as far as its strongest
    /// carries it. The clan's operations out of reach are refused, with the reason.
    /// </summary>
    [TestFixture]
    public class TravelRulesTests
    {
        private static IReadOnlyList<RegionDefinition> Regions => Fixtures.Content.Regions;
        private static TravelSettings Travel => Fixtures.Content.Balance.Travel;
        private const string Home = "jingshui-lake";

        private static bool Reach(CultivationRealm realm, string to) => TravelRules.CanReach(realm, Home, to, Regions, Travel);

        [Test]
        public void TheEmbryonic_StayHome() =>
            Assert.IsFalse(Reach(CultivationRealm.Embryonic, "heshan"));

        [Test]
        public void AQiCultivator_RidesTheWind_AFewRegionsFromHome()
        {
            Assert.IsTrue(Reach(CultivationRealm.QiRefinement, "heshan"), "one border");
            Assert.IsTrue(Reach(CultivationRealm.QiRefinement, "grey-reed-plain"), "two borders");
            Assert.IsFalse(Reach(CultivationRealm.QiRefinement, "mount-yunfeng"), "four borders");
        }

        [Test]
        public void AFoundation_CrossesItsState_ButNoFurther()
        {
            Assert.IsTrue(Reach(CultivationRealm.Foundation, "mount-yunfeng"));
            Assert.IsTrue(Reach(CultivationRealm.Foundation, "linxi"), "the state itself");
            Assert.IsFalse(Reach(CultivationRealm.Foundation, "kun"));
            Assert.IsFalse(Reach(CultivationRealm.Foundation, "mount-jianfeng"), "a region of another state");
        }

        [Test]
        public void APurpleMansion_GoesAnywhere_ThroughTheGreatVoid()
        {
            Assert.IsTrue(Reach(CultivationRealm.PurpleMansion, "kun"));
            Assert.IsTrue(Reach(CultivationRealm.PurpleMansion, "black-pearl-atoll"));
        }

        private static (GameSession S, FactionData Far) FarPower(CultivationRealm realm)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var far = System.Linq.Enumerable.First(s.Factions.Factions, f => f.RegionId == "mount-yunfeng");
            far.HighestRealm = realm;
            return (s, far);
        }

        [Test]
        public void APower_GoesAsFarAsItsStrongestCarriesIt()
        {
            var (s, far) = FarPower(CultivationRealm.QiRefinement);
            Assert.IsFalse(TravelRules.PowerReaches(far, Home, s.Context.Content), "four regions away, a Qi Cultivator cannot come");
            far.HighestRealm = CultivationRealm.Foundation;
            Assert.IsTrue(TravelRules.PowerReaches(far, Home, s.Context.Content), "a Foundation crosses its state");
        }

        [Test]
        public void APowerTooFar_CannotAmbushTheClansMembers()
        {
            var (s, far) = FarPower(CultivationRealm.QiRefinement);
            foreach (var m in s.Clan.LivingMembers) m.LastOperationYear = s.Clock.Year; // all away
            s.Resources.AddSpiritStones(100_000); // all worth taking
            Assert.IsFalse(s.Schemes.Ambush(far));
        }

        [Test]
        public void APowerTooFar_CannotProbeTheClanByHand()
        {
            var (s, far) = FarPower(CultivationRealm.QiRefinement);
            s.SecretBook.Create("internal-feud", SecretBook.ClanHolder, null);
            Assert.IsNotNull(s.Probes.PowerProbe(far, SecretBook.ClanHolder, ProbeApproach.Infiltration, new List<string>()).Refusal);
        }

        [Test]
        public void AProbe_OfAPowerTooFar_IsRefused_WithTheReason()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var qi = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            s.Clan.AddMember(qi);
            var far = System.Linq.Enumerable.First(s.Factions.Factions, f => f.RegionId == "mount-yunfeng");
            far.HighestRealm = CultivationRealm.QiRefinement; // within reach of its realm: only the distance stops it
            if (s.SecretBook.NextUnknown(SecretBook.ClanHolder, far.Name) == null) s.SecretBook.Create("internal-feud", far.Name, null);
            var plan = new ProbePlan(far.Name, ProbeApproach.Infiltration, new List<string> { qi.ID }, new List<string>(), 0);
            StringAssert.Contains("trop loin", s.Probes.RefusalOf(plan));
            var mirror = plan with { Approach = ProbeApproach.MirrorSight };
            Assert.IsFalse((s.Probes.RefusalOf(mirror) ?? "").Contains("trop loin"), "the mirror's sight needs no road");
        }
    }
}
