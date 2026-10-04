using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The mirror's restoration tiers (📚 Lu_Jiangxian/Abilities; AUDIT_LORE.md §3, the user's decision 2026-10-04): the
    /// Light kills and wounds by tier, the perception widens from the domain to the lake, the state, everywhere; the Light
    /// strikes the powers' elders it perceives, and their power wonders.
    /// </summary>
    [TestFixture]
    public class MirrorTiersTests
    {
        private static MirrorTierSettings S => Fixtures.Content.Balance.MirrorTiers;

        [Test]
        public void TheLight_GrowsWithTheShards_AsTheWikiTells()
        {
            Assert.AreEqual("tue", MirrorTiers.Effect(MirrorTiers.Of(0, false, S), CultivationRealm.Embryonic));
            Assert.AreEqual("blesse", MirrorTiers.Effect(MirrorTiers.Of(0, false, S), CultivationRealm.QiRefinement), "at first a peak Summit Eye strike");
            Assert.IsNull(MirrorTiers.Effect(MirrorTiers.Of(0, false, S), CultivationRealm.Foundation));
            Assert.AreEqual("tue", MirrorTiers.Effect(MirrorTiers.Of(1, false, S), CultivationRealm.QiRefinement), "a shard: it kills the Qi Cultivation");
            Assert.AreEqual("blesse", MirrorTiers.Effect(MirrorTiers.Of(1, false, S), CultivationRealm.Foundation), "and wounds a realm above");
            Assert.AreEqual("tue", MirrorTiers.Effect(MirrorTiers.Of(1, true, S), CultivationRealm.PurpleMansion), "the Jade Buckle: it kills a Purple Mansion");
            Assert.IsNull(MirrorTiers.Effect(MirrorTiers.Of(6, true, S), CultivationRealm.DaoEmbryo));
        }

        [Test]
        public void ThePerception_WidensFromTheDomain_ToEverywhere()
        {
            var regions = Fixtures.Content.Regions;
            const string home = "jingshui-lake";
            Assert.IsTrue(MirrorTiers.Perceives(MirrorReach.Domain, home, home, regions));
            Assert.IsFalse(MirrorTiers.Perceives(MirrorReach.Domain, home, "heshan", regions));
            Assert.IsTrue(MirrorTiers.Perceives(MirrorReach.Lake, home, "heshan", regions));
            Assert.IsFalse(MirrorTiers.Perceives(MirrorReach.Lake, home, "mount-yunfeng", regions));
            Assert.IsTrue(MirrorTiers.Perceives(MirrorReach.State, home, "mount-yunfeng", regions));
            Assert.IsFalse(MirrorTiers.Perceives(MirrorReach.State, home, "kun", regions));
            Assert.IsTrue(MirrorTiers.Perceives(MirrorReach.Everywhere, home, "kun", regions));
        }

        [Test]
        public void TheUnrestoredLight_OnlyWoundsAQiCultivator_AndCannotTouchAFoundation()
        {
            var w = new TestWorld();
            var qi = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement));
            var foundation = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            StringAssert.Contains("ne l'atteint pas", w.Mirror.JudgmentRefusal(foundation));
            Assert.IsTrue(w.Mirror.UseMirrorJudgment(qi));
            Assert.IsTrue(qi.IsAlive);
            Assert.AreEqual(1, qi.DaoWounds);
        }

        [Test]
        public void TheLight_StrikesAPowersElderItPerceives_AndThePowerWonders()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var neighbour = s.Factions.Factions.First(f => f.RegionId == s.Context.Content.Clan.HomeRegion);
            var elder = new FactionElder { Id = "x", Name = "x", Realm = CultivationRealm.Embryonic, Stage = 1, MaxLifespan = 120 };
            neighbour.Elders.Add(elder);
            var far = s.Factions.Factions.First(f => f.RegionId == "mount-yunfeng");
            far.Elders.Add(new FactionElder { Id = "y", Name = "y", Realm = CultivationRealm.Embryonic, Stage = 1, MaxLifespan = 120 });
            Assert.IsTrue(s.Light.ElderTargets().Any(t => t.ElderId == "x"));
            Assert.IsFalse(s.Light.ElderTargets().Any(t => t.ElderId == "y"), "beyond the domain, the unrestored mirror sees nothing");
            int clues = s.Suspicion.MirrorClues(neighbour.Name);
            Assert.IsNull(s.Light.StrikeElder(neighbour.Name, "x"));
            Assert.IsFalse(neighbour.Elders.Contains(elder));
            Assert.Greater(s.Suspicion.MirrorClues(neighbour.Name), clues);
        }
    }
}
