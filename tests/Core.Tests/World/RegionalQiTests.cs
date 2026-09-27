using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The Qi of the places (L5b; LORE.md §2.5, §5.8): a place offers the Qi its nature carries (the waters by a lake),
    /// explicit lists where a source speaks; how abundant a Qi is depends on its lineage — rich where a Fruition's
    /// authority flows unhindered, poor where it is broken. An atmosphere favours some lineages, elements and paths:
    /// cultivation runs faster and breakthroughs are safer for them. A member cultivating a Qi foreign to the place
    /// goes slower. The clan cultivates at home.
    /// </summary>
    [TestFixture]
    public class RegionalQiTests
    {
        private const string Lake = "jingshui-lake";
        private static RegionalQiSettings Settings => Fixtures.Content.Balance.RegionalQi;

        private static RegionalQi Place(TestWorld w) => new RegionalQi(w.Ctx, w.Fruitions, w.Techniques);

        private static void Lineage(TestWorld w, string fruition, FruitionStatus status) =>
            w.Fruitions.Restore(new Dictionary<string, FruitionState> { [fruition] = new FruitionState(status, null) }, new System.Random(1));

        // ---- The Qi of a place ----

        [Test]
        public void ALake_OffersTheWaterQi_AndTheCommonBreath()
        {
            var qi = Place(new TestWorld()).QiOf(Lake).Select(q => q.Id).ToList();
            CollectionAssert.Contains(qi, "clear-spring-qi");
            CollectionAssert.Contains(qi, "common-breath-qi");
            CollectionAssert.DoesNotContain(qi, "ember-phoenix-qi");
        }

        [Test]
        public void EveryPlace_OffersSomeQi()
        {
            var place = Place(new TestWorld());
            Assert.IsTrue(Fixtures.Content.Regions.All(r => place.QiOf(r.Id).Count > 0));
        }

        [Test]
        public void AnUnknownPlace_OffersNothing()
        {
            Assert.AreEqual(0, Place(new TestWorld()).QiOf("nowhere").Count);
        }

        [Test]
        public void TheDensity_FollowsTheNatureOfThePlace()
        {
            Assert.Greater(Settings.KindDensity[RegionKind.Wilds], Settings.KindDensity[RegionKind.Desert]);
        }

        [Test]
        public void AQi_IsAbundantWhereItsLineageRules_AndPoorWhereItIsBroken()
        {
            var w = new TestWorld();
            var spring = Fixtures.Content.Qi.Single(q => q.Id == "clear-spring-qi"); // Orthodox Water

            Lineage(w, "orthodox-water", FruitionStatus.Occupied);
            double ruled = Place(w).Abundance(Lake, spring);
            Lineage(w, "orthodox-water", FruitionStatus.Free);
            double free = Place(w).Abundance(Lake, spring);
            Lineage(w, "orthodox-water", FruitionStatus.Broken);
            double broken = Place(w).Abundance(Lake, spring);

            Assert.Greater(ruled, free);
            Assert.Greater(free, broken);
        }

        [Test]
        public void AQiAbsentFromThePlace_HasNoAbundance()
        {
            var ember = Fixtures.Content.Qi.Single(q => q.Id == "ember-phoenix-qi");
            Assert.AreEqual(0, Place(new TestWorld()).Abundance(Lake, ember));
        }

        // ---- Cultivating at home ----

        [Test]
        public void AMemberWhoseQiIsFarAway_CultivatesSlower()
        {
            var w = new TestWorld();
            var spring = w.Join(Fixtures.Cultivator());
            var ember = w.Join(Fixtures.Cultivator());
            w.Techniques.Learn("ember-phoenix-stride");
            ember.CultivationMethodId = "ember-phoenix-stride";
            var place = Place(w);

            Assert.AreEqual(Settings.AbsentQiFactor, place.SpeedFactor(ember), 1e-9);
            Assert.Greater(place.SpeedFactor(spring), place.SpeedFactor(ember));
        }

        [Test]
        public void TheClansCultivation_FollowsItsLineagesFortune()
        {
            var w = new TestWorld();
            var member = w.Join(Fixtures.Cultivator());
            member.CurrentTask = TaskType.Cultivation;
            Lineage(w, "orthodox-water", FruitionStatus.Occupied);
            double ruled = w.Cultivation.SpeedOf(member);
            Lineage(w, "orthodox-water", FruitionStatus.Broken);
            double broken = w.Cultivation.SpeedOf(member);

            Assert.Greater(ruled, broken);
        }

        // ---- Atmospheres (§5.8) ----

        private static AtmosphereDefinition Atmosphere(string id) => Fixtures.Content.Atmospheres.Single(a => a.Id == id);

        [Test]
        public void TheLoresAtmospheres_AreInTheData()
        {
            CollectionAssert.IsSubsetOf(new[] { "profound-balance", "falling-water-rising-storm", "baleful-spirits-storehouse", "overturned-waters-storm" },
                Fixtures.Content.Atmospheres.Select(a => a.Id));
            Assert.AreEqual(-0.025, Atmosphere("overturned-waters-storm").GeneralSpeed, 1e-9, "a coastal cultivator lost 2.5 % (§5.7)");
            CollectionAssert.Contains(Atmosphere("baleful-spirits-storehouse").FavouredFruitions, "nourishing-water");
            CollectionAssert.Contains(Atmosphere("baleful-spirits-storehouse").FavouredPaths, CultivationPath.Devil);
        }

        [Test]
        public void SouthernLinxi_LiesUnderTheBalefulStorehouse()
        {
            var south = Fixtures.Content.Regions.Where(r => r.ParentId == "linxi" && r.Y >= 0.6).ToList();
            Assert.IsTrue(south.Count > 0 && south.All(r => r.AtmosphereId == "baleful-spirits-storehouse"));
            Assert.IsNull(Fixtures.Content.Regions.Single(r => r.Id == Lake).AtmosphereId, "the clan's lake is not in the south");
        }

        [Test]
        public void AnAtmosphere_SpeedsItsFavoured_AndWeighsOnAll()
        {
            var storm = new AtmosphereDefinition { GeneralSpeed = -0.05, FavouredSpeed = 0.2, FavouredFruitions = new[] { "hidden-water" } };
            Assert.AreEqual(1.15, RegionalQiRules.AtmosphereSpeed(storm, "hidden-water", Element.Water, CultivationPath.Immortal), 1e-9);
            Assert.AreEqual(0.95, RegionalQiRules.AtmosphereSpeed(storm, "orthodox-fire", Element.Fire, CultivationPath.Immortal), 1e-9);
            Assert.AreEqual(1.0, RegionalQiRules.AtmosphereSpeed(null, "hidden-water", Element.Water, CultivationPath.Immortal), 1e-9);
        }

        [Test]
        public void AnAtmosphere_FavoursByElement_AndByPath_Too()
        {
            var storehouse = new AtmosphereDefinition
            {
                FavouredSpeed = 0.1, FavouredElements = new[] { Element.Earth }, FavouredPaths = new[] { CultivationPath.Devil }, FavouredBreakthrough = 5
            };
            Assert.AreEqual(1.1, RegionalQiRules.AtmosphereSpeed(storehouse, null, Element.Earth, CultivationPath.Immortal), 1e-9);
            Assert.AreEqual(1.1, RegionalQiRules.AtmosphereSpeed(storehouse, null, Element.Water, CultivationPath.Devil), 1e-9);
            Assert.AreEqual(5, RegionalQiRules.AtmosphereBreakthrough(storehouse, null, Element.Earth, CultivationPath.Immortal));
            Assert.AreEqual(0, RegionalQiRules.AtmosphereBreakthrough(storehouse, null, Element.Water, CultivationPath.Immortal));
        }

        // ---- Checked at load ----

        private static void AssertRefused(string file, string replacement)
        {
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == file ? replacement : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(file, error.Message);
        }

        [Test]
        public void ARegion_UnderAnUnknownAtmosphere_IsRefused()
        {
            var file = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.RegionsFile));
            ((JObject)file.Single(r => (string)r["id"] == Lake))["atmosphereId"] = "no-such-atmosphere";
            AssertRefused(GameContentLoader.RegionsFile, file.ToString());
        }

        [Test]
        public void ARegion_OfferingAnUnknownQi_IsRefused()
        {
            var file = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.RegionsFile));
            ((JObject)file.Single(r => (string)r["id"] == Lake))["qi"] = new JArray("no-such-qi");
            AssertRefused(GameContentLoader.RegionsFile, file.ToString());
        }

        [Test]
        public void AnAtmosphere_FavouringAnUnknownLineage_IsRefused()
        {
            var file = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.AtmospheresFile));
            ((JObject)file[0])["favouredFruitions"] = new JArray("no-such-lineage");
            AssertRefused(GameContentLoader.AtmospheresFile, file.ToString());
        }

        // ---- Review (L5b) ----

        [Test]
        public void NoElement_IsFavouredByNobody()
        {
            var odd = new AtmosphereDefinition { FavouredSpeed = 0.2, FavouredElements = new[] { Element.None } };
            Assert.AreEqual(1.0, RegionalQiRules.AtmosphereSpeed(odd, null, Element.None, CultivationPath.Immortal), 1e-9);
        }

        [Test]
        public void AFavouringAtmosphere_EasesTheAscentToThePurpleMansion()
        {
            var south = GameContentLoader.Load(name => name == GameContentLoader.ClanFile // a clan under the Baleful Storehouse
                ? Fixtures.ReadDataFile(name).Replace("\"jingshui-lake\"", "\"baishi\"")
                : Fixtures.ReadDataFile(name));
            var w = new TestWorld(new System.Random(1), south);
            var devil = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 9));
            var immortal = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 9));
            devil.Path = CultivationPath.Devil;

            Assert.AreEqual(w.PurpleMansion.AscentChanceOf(immortal) + Atmosphere("baleful-spirits-storehouse").FavouredBreakthrough,
                w.PurpleMansion.AscentChanceOf(devil));
        }
    }
}
