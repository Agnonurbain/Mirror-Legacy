using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// Talisman Qi (LORE.md §11.5, the wiki's page on the mirror's talismans): a being of the Qi Cultivation or
    /// beyond sacrificed and ten thousand prayers gathered, the mirror refines a talisman Qi of the sacrifice's
    /// rank — grey for the Qi Cultivation, white for the Foundation — offering one to three to suit the bearer's
    /// talent and temper. Each gives a trait and a leap in cultivation.
    /// </summary>
    [TestFixture]
    public class TalismanTests
    {
        private static TalismanSettings Settings => Fixtures.Content.Balance.Talismans;

        private static TalismanDefinition Talisman(string id) => Fixtures.Content.Talismans.Single(t => t.Id == id);

        /// <summary>A world whose clan has gathered the prayers a ritual needs.</summary>
        private static TestWorld Devout()
        {
            var w = new TestWorld();
            w.Resources.AddPrayers(Settings.PrayersPerRitual);
            return w;
        }

        private static CharacterData Bearer(TestWorld w, int root = 80, Temperament temper = Temperament.Serene)
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 2);
            c.SpiritualRoot = root;
            c.Temperament = temper;
            return w.Join(c);
        }

        private static CharacterData Sacrifice(TestWorld w, CultivationRealm realm = CultivationRealm.QiRefinement) =>
            w.Join(Fixtures.Cultivator(realm: realm, stage: 1));

        // ---- The catalog ----

        [Test]
        public void ShippedTalismans_HoldTheFourteenOfTheLore()
        {
            Assert.AreEqual(6, Fixtures.Content.Talismans.Count(t => t.Rank == TalismanRank.Grey));
            Assert.AreEqual(8, Fixtures.Content.Talismans.Count(t => t.Rank == TalismanRank.White));
        }

        [Test]
        public void ShippedTalismans_KeepTheFiguresTheWikiGives()
        {
            Assert.AreEqual(40, Talisman("prolong-life").LifespanYears);                  // « an extra 40 years of lifespan »
            Assert.AreEqual(2.0, Talisman("radiant-snow-pine-ridge").CultivationSpeed);   // « cultivate twice as effectively »
        }

        [Test]
        public void Load_Refuses_ATalismanWithANonPositiveSpeed()
        {
            var file = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.TalismansFile));
            file.First()["cultivationSpeed"] = 0;
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.TalismansFile ? file.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.TalismansFile, error.Message);
        }

        // ---- The offer: one to three, to suit talent and temper ----

        [TestCase(20, 1)]
        [TestCase(55, 2)]
        [TestCase(90, 3)]
        public void Offer_GivesMoreChoices_ToAGreaterTalent(int root, int choices)
        {
            var w = new TestWorld();
            Assert.AreEqual(choices, TalismanRules.Offer(Bearer(w, root), TalismanRank.White, Fixtures.Content.Talismans, Settings, w.Ctx.Rng).Count);
        }

        [Test]
        public void Offer_PutsTheTalismansOfTheBearersTemperFirst()
        {
            var w = new TestWorld();
            var offer = TalismanRules.Offer(Bearer(w, root: 20, temper: Temperament.Fiery), TalismanRank.Grey, Fixtures.Content.Talismans, Settings, w.Ctx.Rng);
            CollectionAssert.Contains(Talisman(offer.Single()).Temperaments, Temperament.Fiery);
        }

        [Test]
        public void Offer_HoldsOnlyTheRankOfTheSacrifice()
        {
            var w = new TestWorld();
            var offer = TalismanRules.Offer(Bearer(w, root: 90), TalismanRank.Grey, Fixtures.Content.Talismans, Settings, w.Ctx.Rng);
            Assert.IsTrue(offer.All(id => Talisman(id).Rank == TalismanRank.Grey));
        }

        // ---- The ritual ----

        [Test]
        public void PerformRitual_SacrificesTheBeing_SpendsThePrayers_AndMakesAnOffer()
        {
            var w = Devout();
            var bearer = Bearer(w);
            var victim = Sacrifice(w);

            Assert.IsTrue(w.Talismans.PerformRitual(bearer, victim));

            Assert.AreEqual(DeathCause.Sacrificed, victim.CauseOfDeath);
            Assert.AreEqual(0, w.Resources.Prayers);
            Assert.AreEqual(bearer.ID, w.Talismans.PendingOffer.BeneficiaryId);
            Assert.IsTrue(w.Talismans.PendingOffer.Choices.All(id => Talisman(id).Rank == TalismanRank.Grey));
        }

        [Test]
        public void PerformRitual_AFoundationSacrifice_RefinesAWhiteTalisman()
        {
            var w = Devout();
            Assert.IsTrue(w.Talismans.PerformRitual(Bearer(w), Sacrifice(w, CultivationRealm.Foundation)));
            Assert.IsTrue(w.Talismans.PendingOffer.Choices.All(id => Talisman(id).Rank == TalismanRank.White));
        }

        [Test]
        public void PerformRitual_Refuses_WithoutTenThousandPrayers()
        {
            var w = new TestWorld();
            w.Resources.AddPrayers(Settings.PrayersPerRitual - 1);
            var victim = Sacrifice(w);
            Assert.IsFalse(w.Talismans.PerformRitual(Bearer(w), victim));
            Assert.IsTrue(victim.IsAlive);
        }

        [Test]
        public void PerformRitual_Refuses_ASacrificeBelowTheQiCultivation()
        {
            var w = Devout();
            Assert.IsFalse(w.Talismans.PerformRitual(Bearer(w), Sacrifice(w, CultivationRealm.Embryonic)));
        }

        [Test]
        public void PerformRitual_Refuses_ABearerWhoAlreadyHasATalisman()
        {
            var w = Devout();
            var bearer = Bearer(w);
            bearer.TalismanQiId = "prolong-life";
            Assert.IsFalse(w.Talismans.PerformRitual(bearer, Sacrifice(w)));
        }

        [Test]
        public void PerformRitual_Refuses_TheBearerAsTheirOwnSacrifice()
        {
            var w = Devout();
            var bearer = Bearer(w);
            Assert.IsFalse(w.Talismans.PerformRitual(bearer, bearer));
        }

        // ---- The choice: a trait and a leap ----

        [Test]
        public void Choose_GivesTheTalisman_AndALeapInCultivation()
        {
            var w = Devout();
            var bearer = Bearer(w);
            w.Talismans.PerformRitual(bearer, Sacrifice(w));
            string chosen = w.Talismans.PendingOffer.Choices[0];

            Assert.IsTrue(w.Talismans.Choose(chosen));

            Assert.AreEqual(chosen, bearer.TalismanQiId);
            Assert.AreEqual(2 + Settings.GreyStageLeap, bearer.RealmStage);
            Assert.IsNull(w.Talismans.PendingOffer);
        }

        [Test]
        public void Choose_Refuses_ATalismanThatWasNotOffered()
        {
            var w = Devout();
            w.Talismans.PerformRitual(Bearer(w), Sacrifice(w));
            string notOffered = Fixtures.Content.Talismans.First(t => !w.Talismans.PendingOffer.Choices.Contains(t.Id)).Id;
            Assert.IsFalse(w.Talismans.Choose(notOffered));
        }

        [Test]
        public void Leap_StaysWithinTheRealm()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 8);
            TalismanRules.Leap(c, 3);
            Assert.AreEqual(CultivationRealm.QiRefinement, c.Realm);
            Assert.AreEqual(9, c.RealmStage);
        }

        [Test]
        public void Choose_ProlongLife_AddsFortyYears()
        {
            var w = Devout();
            var bearer = Bearer(w);
            int before = bearer.MaxLifespan;
            w.Talismans.Restore(new TalismanOffer(bearer.ID, new System.Collections.Generic.List<string> { "prolong-life" }));

            w.Talismans.Choose("prolong-life");

            Assert.AreEqual(before + 40, bearer.MaxLifespan);
        }

        // ---- The traits at work ----

        [Test]
        public void RadiantSnowPineRidge_DoublesTheCultivationSpeed()
        {
            var w = new TestWorld();
            var plain = Bearer(w);
            var calm = Bearer(w);
            calm.TalismanQiId = "radiant-snow-pine-ridge";
            Assert.AreEqual(2.0 * w.Cultivation.SpeedOf(plain), w.Cultivation.SpeedOf(calm), 1e-9);
        }

        [Test]
        public void RadiantSnowPineRidge_DispelsTheIllusionsOfThePurpleMansion()
        {
            var plain = Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 4);
            var calm = Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 4);
            calm.TalismanQiId = "radiant-snow-pine-ridge";
            plain.MentalStability = calm.MentalStability = 40; // below the 99 % ceiling
            var content = Fixtures.Content;
            int bonus = Talisman("radiant-snow-pine-ridge").IllusionsBonus;
            Assert.Greater(bonus, 0);
            Assert.AreEqual(PurpleMansionRules.IllusionsChance(plain, content) + bonus, PurpleMansionRules.IllusionsChance(calm, content));
        }

        [Test]
        public void VermilionFlowingRainbow_GivesMoreGiftedChildren()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var father = w.Join(Fixtures.Cultivator());
            var mother = w.Join(Fixtures.Cultivator(isMale: false));
            int plainRoot = w.Clan.GenerateChild(father, mother).SpiritualRoot;
            father.TalismanQiId = "vermilion-flowing-rainbow";

            int giftedRoot = w.Clan.GenerateChild(father, mother).SpiritualRoot;

            Assert.AreEqual(plainRoot + Talisman("vermilion-flowing-rainbow").OffspringRootBonus, giftedRoot);
        }

        // ---- Prayers ----

        [Test]
        public void Prayers_RiseEachYear_FromTheMortalsAndThePrestige()
        {
            var w = new TestWorld();
            w.Join(Fixtures.Mortal());
            w.Join(Fixtures.Mortal(isMale: false));
            int expected = 2 * Settings.PrayersPerMortalPerYear + w.Resources.Prestige * Settings.PrayersPerPrestigePerYear;

            w.Ctx.Events.TriggerYearStarted(2);

            Assert.AreEqual(expected, w.Resources.Prayers);
        }

        // ---- Saves ----

        [Test]
        public void RoundTrip_KeepsThePrayersTheOfferAndTheTalismans()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            var bearer = s.Clan.GetPatriarch();
            bearer.TalismanQiId = "holding-profit";
            s.Resources.AddPrayers(1234);
            s.Talismans.Restore(new TalismanOffer(bearer.ID, new System.Collections.Generic.List<string> { "prolong-life" }));

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.AreEqual("holding-profit", reloaded.Clan.GetPatriarch().TalismanQiId);
            Assert.AreEqual(s.Resources.Prayers, reloaded.Resources.Prayers);
            CollectionAssert.AreEqual(new[] { "prolong-life" }, reloaded.Talismans.PendingOffer.Choices);
        }
    }
}
