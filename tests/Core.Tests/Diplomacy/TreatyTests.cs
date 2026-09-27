using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// The clan's treaties with the powers (LORE.md D7: an alliance is a contract of profit, never a guarantee). A power
    /// accepts when it gains: relation, temper, the balance of strength, and — hidden — what it suspects of the clan.
    /// Non-aggression, trade, defence, vassalage both ways; secret or sealed by a Dao oath. Each year a power may betray,
    /// more when it suspects the clan, far less when its oath binds it; the others see a public betrayal and distrust the
    /// betrayer in silence; a secret treaty may come to light. The clan may break a treaty too, at a price.
    /// </summary>
    [TestFixture]
    public class TreatyTests
    {
        private const string Tao = "Famille Tao";              // a friendly merchant family (Foundation)
        private const string Lou = "Famille Lou";              // hostile (Foundation)
        private const string Fang = "Famille Fang";            // a small isolationist family (Foundation)
        private const string Peak = "Secte du Pic des Nuées";  // an expansionist Golden Core sect
        private const string Ruan = "Famille Ruan";            // aggressive (Purple Mansion)

        private static TreatySettings Settings => Fixtures.Content.Balance.Treaties;

        private static TestWorld World(System.Random rng, CultivationRealm strongest = CultivationRealm.QiRefinement)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: strongest, stage: 3)));
            return w;
        }

        private static FactionData Power(TestWorld w, string name) => w.Factions.GetFactionByName(name);

        private static void Warm(TestWorld w, string name, int by) => w.Factions.ChangeRelation(Power(w, name).ID, by);

        // ---- Proposing: profit decides ----

        [Test]
        public void NonAggression_IsAccepted_FromAFriend_RefusedByAnEnemy()
        {
            var w = World(new FixedRandom(0.999));
            Assert.IsNull(w.Treaties.Propose(Tao, TreatyKind.NonAggression));
            Assert.IsTrue(w.Treaties.Has(Tao, TreatyKind.NonAggression));
            StringAssert.Contains("relation", w.Treaties.Propose(Lou, TreatyKind.NonAggression));
        }

        [Test]
        public void Trade_PleasesAMerchant_MoreThanARecluse()
        {
            var w = World(new FixedRandom(0.999));
            Assert.Greater(TreatyRules.Willingness(Power(w, Tao), TreatyKind.Trade, false, CultivationRealm.QiRefinement, 0, Settings),
                TreatyRules.Willingness(Power(w, Fang), TreatyKind.Trade, false, CultivationRealm.QiRefinement, 0, Settings));
        }

        [Test]
        public void WhatAPowerSuspects_MakesItLessWilling()
        {
            var w = World(new FixedRandom(0.999));
            Assert.Greater(TreatyRules.Willingness(Power(w, Tao), TreatyKind.Trade, false, CultivationRealm.QiRefinement, 0, Settings),
                TreatyRules.Willingness(Power(w, Tao), TreatyKind.Trade, false, CultivationRealm.QiRefinement, 60, Settings));
        }

        [Test]
        public void ASuzerain_MustBeClearlyStronger_WhicheverSide()
        {
            var weak = World(new FixedRandom(0.999));
            Assert.IsNull(weak.Treaties.Propose(Peak, TreatyKind.Vassalage), "a Golden Core sect takes the clan as vassal");
            StringAssert.Contains("protéger", World(new FixedRandom(0.999), CultivationRealm.Foundation).Treaties.Propose(Fang, TreatyKind.Vassalage));

            var strong = World(new FixedRandom(0.999), CultivationRealm.PurpleMansion);
            Assert.IsNull(strong.Treaties.Propose(Fang, TreatyKind.Vassalage, clanAsSuzerain: true));
            StringAssert.Contains("ascendant", strong.Treaties.Propose(Peak, TreatyKind.Vassalage, clanAsSuzerain: true));
        }

        [Test]
        public void OneTreatyOfAKind_PerPower_AndOneSuzerain_ForTheClan()
        {
            var w = World(new FixedRandom(0.999));
            Assert.IsNull(w.Treaties.Propose(Tao, TreatyKind.NonAggression));
            Assert.IsNotNull(w.Treaties.Propose(Tao, TreatyKind.NonAggression));
            Assert.IsNull(w.Treaties.Propose(Peak, TreatyKind.Vassalage));
            Warm(w, "Empire de Kun", 50);
            StringAssert.Contains("suzerain", w.Treaties.Propose("Empire de Kun", TreatyKind.Vassalage));
        }

        // ---- What a treaty does ----

        [Test]
        public void ATradeTreaty_BringsStones_AndCheaperKnowledge()
        {
            var w = World(new FixedRandom(0.999));
            var art = w.Exchange.Offers(Tao).First();
            int price = w.Exchange.PriceOf(art, Tao);
            int stones = w.Resources.SpiritStones;
            w.Treaties.Propose(Tao, TreatyKind.Trade);

            w.Treaties.ProcessYear();

            Assert.AreEqual(stones + Settings.TradeIncome, w.Resources.SpiritStones);
            Assert.Less(w.Exchange.PriceOf(art, Tao), price);
        }

        [Test]
        public void ANonAggressionTreaty_KeepsThePowerFromAmbushingTheClan()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { Power(w, Tao) });
            w.Treaties.Propose(Tao, TreatyKind.NonAggression);
            var envoy = w.Join(Fixtures.Cultivator());
            envoy.CurrentTask = TaskType.Diplomacy;

            w.Schemes.ProcessYear();

            Assert.IsNull(envoy.CaptorFaction);
        }

        [Test]
        public void ADefensiveAlly_MayFoilAnAmbush()
        {
            var w = World(new FixedRandom(0.0));
            Warm(w, Tao, 30);
            Assert.IsNull(w.Treaties.Propose(Tao, TreatyKind.Defence));
            var envoy = w.Join(Fixtures.Cultivator());
            envoy.CurrentTask = TaskType.Diplomacy;

            Assert.IsTrue(w.Schemes.Ambush(Power(w, Ruan)));

            Assert.IsNull(envoy.CaptorFaction, "the ally's escort");
        }

        [Test]
        public void AnAlly_DrawsCloserToTheSecret()
        {
            var w = World(new FixedRandom(0.999));
            Warm(w, Tao, 30);
            w.Treaties.Propose(Tao, TreatyKind.Defence);
            w.Treaties.ProcessYear();
            Assert.AreEqual(Settings.AllyProximityClues, w.Suspicion.MirrorClues(Tao));
        }

        [Test]
        public void AVassal_PaysTribute_AndItsSuzerainsGripTightens()
        {
            var w = World(new FixedRandom(0.999));
            w.Treaties.Propose(Peak, TreatyKind.Vassalage);
            int stones = w.Resources.SpiritStones;
            int wealth = Power(w, Peak).Wealth;

            w.Treaties.ProcessYear();

            int tribute = (int)(stones * Settings.VassalTributeShare);
            Assert.AreEqual(stones - tribute, w.Resources.SpiritStones);
            Assert.AreEqual(wealth + tribute, Power(w, Peak).Wealth);
            Assert.AreEqual(Settings.GripPerYear, w.Treaties.With(Peak).Single().Grip);
        }

        [Test]
        public void TheSuzerainsFullGrip_TakesStones_AndTheClansBestArt()
        {
            var w = World(new FixedRandom(0.999));
            w.Techniques.Learn("clear-spring-sutra");
            w.Treaties.Propose(Peak, TreatyKind.Vassalage);
            w.Treaties.RestoreTreaties(new[] { w.Treaties.With(Peak).Single() with { Grip = Settings.GripThreshold } });
            int stones = w.Resources.SpiritStones;

            w.Treaties.ProcessYear();

            Assert.Less(w.Resources.SpiritStones, stones - (int)(stones * Settings.VassalTributeShare));
            CollectionAssert.Contains(Power(w, Peak).Techniques, "clear-spring-sutra");
            Assert.AreEqual(0, w.Treaties.With(Peak).Single().Grip, "the grip starts over");
        }

        [Test]
        public void AVassalOfTheClan_PaysItTribute()
        {
            var w = World(new FixedRandom(0.999), CultivationRealm.PurpleMansion);
            w.Treaties.Propose(Fang, TreatyKind.Vassalage, clanAsSuzerain: true);
            int stones = w.Resources.SpiritStones;
            int wealth = Power(w, Fang).Wealth;

            w.Treaties.ProcessYear();

            int tribute = (int)(wealth * Settings.VassalTributeShare);
            Assert.AreEqual(stones + tribute, w.Resources.SpiritStones);
            Assert.AreEqual(wealth - tribute, Power(w, Fang).Wealth);
        }

        [Test]
        public void ATreatyHoldsBackAnUnprovenStrike()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.Embryonic);
            Warm(w, Ruan, 30);
            Assert.IsNull(w.Treaties.Propose(Ruan, TreatyKind.NonAggression));
            w.Suspicion.AddToClan(Ruan, Fixtures.Content.Balance.Plots.ActThreshold);
            int stones = w.Resources.SpiritStones;

            w.Plots.ProcessYear();

            Assert.AreEqual(stones, w.Resources.SpiritStones);
        }

        // ---- Betrayal ----

        [Test]
        public void BetrayalTempts_TheSuspicious_AndTheStrong_FarLessTheSworn()
        {
            var power = new FactionData { Personality = FactionPersonality.Manipulative, HighestRealm = CultivationRealm.Foundation };
            var treaty = new Treaty("t", TreatyKind.Trade, "x", 1, null, false, false, false);
            double calm = TreatyRules.BetrayalChance(power, treaty, 0, CultivationRealm.Foundation, Settings);
            Assert.Greater(TreatyRules.BetrayalChance(power, treaty, 80, CultivationRealm.Foundation, Settings), calm);
            Assert.Greater(TreatyRules.BetrayalChance(power, treaty, 0, CultivationRealm.Embryonic, Settings), calm);
            Assert.Less(TreatyRules.BetrayalChance(power, treaty with { Sealed = true }, 0, CultivationRealm.Foundation, Settings), calm);
        }

        [Test]
        public void APublicBetrayal_IsSeen_AndTheBetrayerDistrusted()
        {
            var w = World(new FixedRandom(0.0));
            w.Treaties.Propose(Tao, TreatyKind.NonAggression);
            int relation = Power(w, Tao).RelationWithPlayer;
            int stones = w.Resources.SpiritStones;

            w.Treaties.ProcessYear();

            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.NonAggression));
            Assert.AreEqual(relation + Settings.BetrayalRelation, Power(w, Tao).RelationWithPlayer);
            Assert.Less(w.Resources.SpiritStones, stones, "it strikes the clan it swore not to strike");
            Assert.AreEqual(Settings.BetrayalWitnessDistrust, w.Suspicion.Distrust(Fang, Tao));
        }

        [Test]
        public void ASecretTreaty_Betrayed_HasNoWitness()
        {
            var w = World(new SequenceRandom(0.0, 0.999)); // it betrays; nothing else comes to light
            w.Treaties.Propose(Tao, TreatyKind.NonAggression, secret: true);
            w.Treaties.ProcessYear();
            Assert.AreEqual(0, w.Suspicion.Distrust(Fang, Tao));
        }

        [Test]
        public void ASecretTreaty_ComingToLight_MakesEveryoneDoubtBoth()
        {
            var w = World(new SequenceRandom(0.999, 0.0)); // no betrayal; it comes to light
            w.Treaties.Propose(Tao, TreatyKind.Trade, secret: true);

            w.Treaties.ProcessYear();

            Assert.IsFalse(w.Treaties.With(Tao).Single().Secret);
            Assert.AreEqual(Settings.SecretDiscoveryDistrust, w.Suspicion.Distrust(Fang, Tao));
            Assert.AreEqual(Settings.SecretDiscoveryDistrust, w.Suspicion.OfClan(Fang));
        }

        // ---- The clan breaks its word ----

        [Test]
        public void BreakingATreaty_CostsTheRelation_AndTheClansName()
        {
            var w = World(new FixedRandom(0.999));
            w.Treaties.Propose(Tao, TreatyKind.Trade);
            int relation = Power(w, Tao).RelationWithPlayer;

            Assert.IsNull(w.Treaties.Break(w.Treaties.With(Tao).Single().Id));

            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Trade));
            Assert.AreEqual(relation + Settings.BreakRelation, Power(w, Tao).RelationWithPlayer);
            Assert.AreEqual(Settings.BreakReputation, w.Suspicion.OfClan(Fang));
        }

        [Test]
        public void BreakingASealedTreaty_BringsAHeartDemon()
        {
            var w = World(new FixedRandom(0.999));
            Assert.IsNull(w.Treaties.Propose(Tao, TreatyKind.Trade, sealedByOath: true));
            w.Treaties.Break(w.Treaties.With(Tao).Single().Id);
            Assert.AreEqual(Settings.SealedHeartDemonYears, w.Clan.GetPatriarch().HeartDemonYearsLeft);
        }

        [Test]
        public void ATreatyForAFewYears_Ends()
        {
            var w = World(new FixedRandom(0.999));
            w.Treaties.Propose(Tao, TreatyKind.Trade, years: 1);
            w.Ctx.Clock.Restore(w.Ctx.Clock.Year + 1, w.Ctx.Clock.Phase);
            w.Treaties.ProcessYear();
            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Trade));
        }

        [Test]
        public void RoundTrip_KeepsTheTreaties()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Treaties.Propose(Tao, TreatyKind.Trade, secret: true);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(s.Treaties.All.Single(), reloaded.Treaties.All.Single());
        }

        // ---- Review ----

        [Test]
        public void BreakingAVassalage_EitherWay_CostsLikeAnyBrokenWord()
        {
            var w = World(new FixedRandom(0.999));
            w.Treaties.Propose(Peak, TreatyKind.Vassalage);
            int relation = Power(w, Peak).RelationWithPlayer;

            Assert.IsNull(w.Treaties.Break(w.Treaties.With(Peak).Single().Id));

            Assert.AreEqual(relation + Settings.BreakRelation, Power(w, Peak).RelationWithPlayer);
            StringAssert.DoesNotContain("suzerain", w.Treaties.Propose(Peak, TreatyKind.Vassalage), "the clan serves no one now — only the relation is spoiled");

            var strong = World(new FixedRandom(0.999), CultivationRealm.PurpleMansion);
            strong.Treaties.Propose(Fang, TreatyKind.Vassalage, clanAsSuzerain: true);
            Assert.IsNull(strong.Treaties.Break(strong.Treaties.With(Fang).Single().Id));
            Assert.AreEqual(0, strong.Treaties.All.Count);
        }

        [Test]
        public void ACaptivePatriarch_CannotSwear()
        {
            var w = World(new FixedRandom(0.999));
            w.Clan.GetPatriarch().CaptorFaction = Ruan; // held, and nobody else to lead
            StringAssert.Contains("captif", w.Treaties.Propose(Tao, TreatyKind.Trade, sealedByOath: true));
        }

        [Test]
        public void TwoTreatiesInOneYear_OneBetrayed_TheOtherServed()
        {
            var w = World(new SequenceRandom(0.999, 0.0)); // the first is kept, the second betrayed
            w.Treaties.Propose(Peak, TreatyKind.Vassalage);
            w.Treaties.Propose(Tao, TreatyKind.NonAggression);

            w.Treaties.ProcessYear();

            Assert.AreEqual(Settings.GripPerYear, w.Treaties.With(Peak).Single().Grip);
            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.NonAggression));
        }

        [Test]
        public void DeclaringWar_OnAnUnknownPower_IsRefused()
        {
            var w = World(new FixedRandom(0.999));
            Assert.IsNotNull(w.Alliances.DeclareWar("no-such-id"));
            Assert.IsNull(w.Alliances.DeclareWar(Power(w, Lou).ID));
        }

        [Test]
        public void Load_RefusesATreatyShareAboveOne()
        {
            var file = Newtonsoft.Json.Linq.JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.BalanceFile));
            file["treaties"]["vassalTributeShare"] = 1.5;
            var error = Assert.Throws<System.IO.InvalidDataException>(() => GameContentLoader.Load(name =>
                name == GameContentLoader.BalanceFile ? file.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains("treaties", error.Message);
        }
    }
}
