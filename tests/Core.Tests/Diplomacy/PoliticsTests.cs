using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// The powers' own politics (LORE.md D7; user decision 2026-09-27): they ally with one another — neighbours, of one
    /// Dao, who do not distrust each other; they feud, the strong taking from a weaker neighbour, the victim's allies
    /// watching; the strong subjugate the weak and, their grip complete, absorb them. When enough powers suspect the clan,
    /// they form a coalition against it. An ally of the clan attacked calls it to arms: answering costs, refusing breaks
    /// the treaty; a call left unanswered is a refusal.
    /// </summary>
    [TestFixture]
    public class PoliticsTests
    {
        private const string Tao = "Famille Tao";
        private const string Fang = "Famille Fang";
        private const string Ruan = "Famille Ruan";
        private const string Lou = "Famille Lou";
        private const string Peak = "Secte du Pic des Nuées";

        private static PoliticsSettings Settings => Fixtures.Content.Balance.Politics;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3)));
            return w;
        }

        private static FactionData Power(TestWorld w, string name) => w.Factions.GetFactionByName(name);

        // ---- Alliances between powers ----

        [Test]
        public void Affinity_DrawsNeighboursOfOneDaoTogether_AndDistrustPartsThem()
        {
            var a = new FactionData { Path = CultivationPath.Immortal };
            var b = new FactionData { Path = CultivationPath.Immortal };
            var far = new FactionData { Path = CultivationPath.Buddhist };
            Assert.Greater(PoliticsRules.Affinity(a, b, neighbours: true, 0, Settings), PoliticsRules.Affinity(a, far, neighbours: true, 0, Settings));
            Assert.Greater(PoliticsRules.Affinity(a, b, neighbours: true, 0, Settings), PoliticsRules.Affinity(a, b, neighbours: false, 0, Settings));
            Assert.Greater(PoliticsRules.Affinity(a, b, true, 0, Settings), PoliticsRules.Affinity(a, b, true, 40, Settings));
        }

        [Test]
        public void TheYear_BindsAFewPowers_NotAll()
        {
            var w = World(new FixedRandom(0.0));
            w.Politics.ProcessYear();
            Assert.That(w.Politics.Bonds.Count(b => b.Kind == BondKind.Alliance), Is.InRange(1, Settings.MaxBondsPerYear));
        }

        // ---- Feuds ----

        [Test]
        public void AFeud_TakesFromTheWeaker_AndItsAlliesWatch()
        {
            var w = World(new FixedRandom(0.999));
            w.Politics.RestoreBonds(new[] { new PowerBond("b", BondKind.Alliance, Fang, Tao, 1, false) });
            var fang = Power(w, Fang);
            int wealth = fang.Wealth;
            int power = fang.PowerLevel;

            w.Politics.Feud(Power(w, Ruan), fang);

            Assert.AreEqual(wealth - (int)(wealth * Settings.FeudWealthShare), fang.Wealth);
            Assert.Less(fang.PowerLevel, power);
            Assert.AreEqual(Settings.AllyDistrust, w.Suspicion.Distrust(Tao, Ruan), "the victim's ally distrusts the attacker");
        }

        [Test]
        public void AnAllyOfTheClan_Attacked_CallsItToArms()
        {
            var w = World(new FixedRandom(0.999));
            w.Factions.ChangeRelation(Power(w, Tao).ID, 30);
            Assert.IsNull(w.Treaties.Propose(Tao, TreatyKind.Defence));

            w.Politics.Feud(Power(w, Ruan), Power(w, Tao));

            Assert.AreEqual(new CallToArms(Tao, Ruan, w.Ctx.Clock.Year), w.Politics.PendingCall);
        }

        // ---- The call to arms ----

        private static TestWorld Called()
        {
            var w = World(new FixedRandom(0.999));
            w.Factions.ChangeRelation(Power(w, Tao).ID, 30);
            w.Treaties.Propose(Tao, TreatyKind.Defence);
            w.Politics.Feud(Power(w, Ruan), Power(w, Tao));
            return w;
        }

        [Test]
        public void AnsweringTheCall_CostsStones_AndTheAttackersFavour()
        {
            var w = Called();
            int stones = w.Resources.SpiritStones;
            int ruan = Power(w, Ruan).RelationWithPlayer;
            int tao = Power(w, Tao).RelationWithPlayer;

            Assert.IsNull(w.Politics.AnswerCall());

            Assert.AreEqual(stones - Settings.CallStonesCost, w.Resources.SpiritStones);
            Assert.AreEqual(ruan + Settings.CallAttackerRelation, Power(w, Ruan).RelationWithPlayer);
            Assert.AreEqual(tao + Settings.CallAllyRelation, Power(w, Tao).RelationWithPlayer);
            Assert.IsNull(w.Politics.PendingCall);
            Assert.IsTrue(w.Treaties.Has(Tao, TreatyKind.Defence));
        }

        [Test]
        public void RefusingTheCall_BreaksTheTreaty()
        {
            var w = Called();
            Assert.IsNull(w.Politics.RefuseCall());
            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Defence));
            Assert.IsNull(w.Politics.PendingCall);
        }

        [Test]
        public void ACallLeftUnanswered_IsARefusal()
        {
            var w = Called();
            w.Ctx.Clock.Restore(w.Ctx.Clock.Year + 1, w.Ctx.Clock.Phase);
            w.Politics.ProcessYear();
            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Defence));
        }

        [Test]
        public void TheCall_CannotBeAnswered_WithoutTheStones()
        {
            var w = Called();
            w.Resources.ConsumeSpiritStones(w.Resources.SpiritStones);
            StringAssert.Contains("pierres", w.Politics.AnswerCall());
            Assert.IsNotNull(w.Politics.PendingCall);
        }

        // ---- Vassals and absorption ----

        [Test]
        public void AVassalPower_PaysItsSuzerain_UntilItIsAbsorbed()
        {
            var w = World(new FixedRandom(0.999));
            w.Politics.Subjugate(Power(w, Peak), Power(w, Fang));
            var fang = Power(w, Fang);
            int wealth = fang.Wealth;

            w.Politics.ProcessYear();

            Assert.AreEqual(wealth - (int)(wealth * Settings.VassalTributeShare), fang.Wealth);
            Assert.AreEqual(Peak, w.Politics.SuzerainOf(Fang));
        }

        [Test]
        public void AFullGrip_AbsorbsTheVassal_IntoItsSuzerain()
        {
            var w = World(new FixedRandom(0.999));
            var peak = Power(w, Peak);
            var fang = Power(w, Fang);
            w.Politics.RestoreBonds(new[] { new PowerBond("v", BondKind.Vassalage, Peak, Fang, 1, false) { Grip = Settings.GripThreshold } });
            int wealth = peak.Wealth + fang.Wealth;
            string art = fang.Techniques.FirstOrDefault(t => !peak.Techniques.Contains(t));

            w.Politics.ProcessYear();

            Assert.IsNull(w.Factions.GetFactionByName(Fang), "the vassal is no more");
            Assert.GreaterOrEqual(peak.Wealth, wealth);
            if (art != null) CollectionAssert.Contains(peak.Techniques, art);
            Assert.IsFalse(w.Politics.Bonds.Any(b => b.A == Fang || b.B == Fang));
        }

        [Test]
        public void APowerBoundToTheClan_IsNotSubjugated()
        {
            var w = World(new FixedRandom(0.999));
            w.Treaties.Propose(Tao, TreatyKind.NonAggression);
            Assert.IsFalse(w.Politics.Subjugate(Power(w, Peak), Power(w, Tao)));
        }

        // ---- A coalition against the clan ----

        [Test]
        public void EnoughSuspicion_RaisesACoalition_AgainstTheClan()
        {
            var w = World(new FixedRandom(0.999));
            foreach (var name in new[] { Ruan, Lou, Fang }) w.Suspicion.AddToClan(name, Settings.CoalitionSuspicion);

            w.Politics.ProcessYear();

            CollectionAssert.AreEquivalent(new[] { Ruan, Lou, Fang }, w.Politics.Coalition.Members);
            Assert.Greater(w.Politics.SchemeFactor(Power(w, Ruan)), 1.0, "its members scheme more");
            Assert.AreEqual(1.0, w.Politics.SchemeFactor(Power(w, Tao)));
        }

        [Test]
        public void APowerBoundToTheClan_StaysOutOfTheCoalition()
        {
            var w = World(new FixedRandom(0.999));
            w.Factions.ChangeRelation(Power(w, Lou).ID, 30);
            Assert.IsNull(w.Treaties.Propose(Lou, TreatyKind.NonAggression));
            foreach (var name in new[] { Ruan, Lou, Fang, Tao }) w.Suspicion.AddToClan(name, Settings.CoalitionSuspicion);

            w.Politics.ProcessYear();

            CollectionAssert.DoesNotContain(w.Politics.Coalition.Members, Lou);
        }

        [Test]
        public void TheCoalition_StrikesEachYear_ThenDisbands()
        {
            var w = World(new FixedRandom(0.999));
            w.Politics.RestoreCoalition(new Coalition(new[] { Ruan, Lou, Fang }.ToList(), 1));
            int stones = w.Resources.SpiritStones;
            int ruan = Power(w, Ruan).RelationWithPlayer;

            w.Politics.ProcessYear();

            Assert.AreEqual(stones - (int)(stones * Settings.CoalitionStonesShare), w.Resources.SpiritStones);
            Assert.AreEqual(ruan + Settings.CoalitionRelation, Power(w, Ruan).RelationWithPlayer);
            Assert.IsNull(w.Politics.Coalition, "its years are spent");
        }

        // ---- The clan absorbed ----

        [Test]
        public void AClanWhoseSuzerainsGripFillsOnceTooOften_IsAbsorbed_AndTheGameLost()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Treaties.Propose(Peak, TreatyKind.Vassalage);
            var treaty = s.Treaties.With(Peak).Single();
            s.Treaties.RestoreTreaties(new[] { treaty with { Grip = Fixtures.Content.Balance.Treaties.GripThreshold, Absorptions = Settings.ClanAbsorptionSteps - 1 } });

            s.Treaties.ProcessYear();

            Assert.IsTrue(s.Victory.GameLost);
        }

        // ---- Saves ----

        [Test]
        public void RoundTrip_KeepsTheBonds_TheCoalition_AndTheCall()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Politics.RestoreBonds(new[] { new PowerBond("b", BondKind.Alliance, Fang, Tao, 1, true) });
            s.Politics.RestoreCoalition(new Coalition(new[] { Ruan, Lou }.ToList(), 3));
            s.Politics.RestoreCall(new CallToArms(Tao, Ruan, 1));

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.AreEqual(s.Politics.Bonds.Single(), reloaded.Politics.Bonds.Single());
            CollectionAssert.AreEqual(new[] { Ruan, Lou }, reloaded.Politics.Coalition.Members);
            Assert.AreEqual(3, reloaded.Politics.Coalition.YearsLeft);
            Assert.AreEqual(new CallToArms(Tao, Ruan, 1), reloaded.Politics.PendingCall);
        }

        // ---- Review: what an absorption carries with it ----

        private static void AbsorbFangIntoPeak(TestWorld w, params PowerBond[] others) =>
            w.Politics.RestoreBonds(new[] { new PowerBond("v", BondKind.Vassalage, Peak, Fang, 1, false) { Grip = Settings.GripThreshold } }.Concat(others));

        [Test]
        public void AnAbsorbedPowersVassals_PassToItsSuzerain()
        {
            var w = World(new FixedRandom(0.999));
            AbsorbFangIntoPeak(w, new PowerBond("c", BondKind.Vassalage, Fang, "Famille Kang", 1, false));
            w.Politics.ProcessYear();
            Assert.AreEqual(Peak, w.Politics.SuzerainOf("Famille Kang"));
        }

        [Test]
        public void TheClansTreaties_WithAnAbsorbedPower_EndAtOnce()
        {
            var w = World(new FixedRandom(0.999));
            AbsorbFangIntoPeak(w);
            w.Factions.ChangeRelation(Power(w, Fang).ID, 30);
            Assert.IsNull(w.Treaties.Propose(Fang, TreatyKind.NonAggression));

            w.Politics.ProcessYear();

            Assert.IsFalse(w.Treaties.Has(Fang, TreatyKind.NonAggression));
        }

        [Test]
        public void ACall_WhoseAllyIsAbsorbed_Lapses()
        {
            var w = World(new FixedRandom(0.999));
            AbsorbFangIntoPeak(w);
            w.Politics.RestoreCall(new CallToArms(Fang, Ruan, w.Ctx.Clock.Year));
            w.Politics.ProcessYear();
            Assert.IsNull(w.Politics.PendingCall);
        }

        [Test]
        public void AnAbsorbedPowersBeasts_BelongToItsSuzerain()
        {
            var w = World(new FixedRandom(0.999));
            w.Bestiary.Restore(new[] { new WorldBeast("b", "fox", "heshan", CultivationRealm.QiRefinement, 1, Fang) });
            AbsorbFangIntoPeak(w);
            w.Politics.ProcessYear();
            Assert.AreEqual(Peak, w.Bestiary.Beasts.Single().OwnerFaction);
        }

        [Test]
        public void AConfrontation_ByAnAbsorbedPower_Ends()
        {
            var w = World(new FixedRandom(0.999));
            AbsorbFangIntoPeak(w);
            w.Secrets.RestoreConfrontation(new Confrontation(Fang, 1));
            w.Politics.ProcessYear();
            Assert.IsNull(w.Secrets.Confrontation);
        }

        // ---- Review: calls queued, archives inherited ----

        private static TestWorld TwoAlliesAttacked()
        {
            var w = World(new FixedRandom(0.999));
            foreach (var ally in new[] { Tao, "Famille Lü" })
            {
                w.Factions.ChangeRelation(Power(w, ally).ID, 30);
                Assert.IsNull(w.Treaties.Propose(ally, TreatyKind.Defence));
            }
            w.Politics.Feud(Power(w, Ruan), Power(w, Tao));
            w.Politics.Feud(Power(w, Lou), Power(w, "Famille Lü"));
            return w;
        }

        [Test]
        public void TwoAlliesAttacked_BothCall_OneAfterTheOther()
        {
            var w = TwoAlliesAttacked();
            Assert.AreEqual(Tao, w.Politics.PendingCall.Ally);
            Assert.IsNull(w.Politics.AnswerCall());
            Assert.AreEqual("Famille Lü", w.Politics.PendingCall.Ally, "the second call waits its turn");
        }

        [Test]
        public void EveryCallLeftUnanswered_IsARefusal()
        {
            var w = TwoAlliesAttacked();
            w.Ctx.Clock.Restore(w.Ctx.Clock.Year + 1, w.Ctx.Clock.Phase);
            w.Politics.ProcessYear();
            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Defence) || w.Treaties.Has("Famille Lü", TreatyKind.Defence));
            Assert.IsNull(w.Politics.PendingCall);
        }

        [Test]
        public void ASuzerain_InheritsWhatItsAbsorbedVassalHeld()
        {
            var w = World(new FixedRandom(0.999));
            w.Suspicion.AddToClan(Fang, 40);
            w.Suspicion.AddToClan(Peak, 10);
            w.Suspicion.AddEvidence(Fang, 30);
            w.Suspicion.AddMirrorClues(Fang, 25);
            w.Suspicion.AddDistrust(Fang, Ruan, 20);
            w.Suspicion.AddDistrust(Tao, Fang, 15);
            AbsorbFangIntoPeak(w);

            w.Politics.ProcessYear();

            Assert.AreEqual((40, 30, 25), (w.Suspicion.OfClan(Peak), w.Suspicion.Evidence(Peak), w.Suspicion.MirrorClues(Peak)), "it seized the archives");
            Assert.AreEqual(20, w.Suspicion.Distrust(Peak, Ruan));
            Assert.IsFalse(w.Suspicion.ClanSuspicions.ContainsKey(Fang) || w.Suspicion.Distrusts.Keys.Any(k => k.Contains(Fang)), "nothing left of the vanished");
        }
    }
}
