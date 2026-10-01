using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers' intrigues (LORE.md D7; user decision 2026-09-27). Blackmail: a power holding proof against the clan
    /// may demand stones instead of striking — paid, it keeps quiet for a while; refused (or left unanswered), it spreads
    /// its proof. Theft: a power may steal a captured beast, copy a manual, take stones or Qi — patrols make it rarer and
    /// may catch the thief (an agent held: proof); otherwise the clan does not know who. Infiltration: the spouse a power
    /// sends in an arranged marriage may be its spy, feeding it proof and clues each year; the mirror may sound a spouse,
    /// and an unmasked spy is turned into a double agent or executed.
    /// </summary>
    [TestFixture]
    public class IntrigueTests
    {
        private const string Ruan = "Famille Ruan";   // aggressive
        private const string Fang = "Famille Fang";
        private const string Chrysanthemum = "Porte du Chrysanthème Noir"; // manipulative

        private static IntrigueSettings Settings => Fixtures.Content.Balance.Intrigues;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3)));
            return w;
        }

        private static FactionData Power(TestWorld w, string name) => w.Factions.GetFactionByName(name);

        // ---- Blackmail ----

        [Test]
        public void APowerHoldingProof_MayDemandStones_InsteadOfStriking()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { Power(w, Chrysanthemum) });
            w.Suspicion.AddEvidence(Chrysanthemum, Settings.BlackmailEvidence);

            w.Intrigues.ProcessYear();

            var demand = w.Intrigues.Demands.Single();
            Assert.AreEqual(Chrysanthemum, demand.Faction);
            Assert.AreEqual((int)(w.Resources.SpiritStones * Settings.BlackmailStonesShare), demand.Stones);
        }

        [Test]
        public void APowerWithoutProof_HasNothingToSell()
        {
            var w = World(new FixedRandom(0.0));
            w.Intrigues.ProcessYear();
            Assert.AreEqual(0, w.Intrigues.Demands.Count);
        }

        private static TestWorld Blackmailed()
        {
            var w = World(new FixedRandom(0.999));
            w.Intrigues.RestoreDemands(new[] { new Demand(Chrysanthemum, 200, w.Ctx.Clock.Year) }, null);
            return w;
        }

        [Test]
        public void PayingTheBlackmail_BuysSilence_ForAWhile()
        {
            var w = Blackmailed();
            int stones = w.Resources.SpiritStones;
            int wealth = Power(w, Chrysanthemum).Wealth;

            Assert.IsNull(w.Intrigues.Pay(Chrysanthemum));

            Assert.AreEqual(stones - 200, w.Resources.SpiritStones);
            Assert.AreEqual(wealth + 200, Power(w, Chrysanthemum).Wealth);
            Assert.AreEqual(w.Ctx.Clock.Year + Settings.QuietYears, w.Intrigues.QuietUntil[Chrysanthemum]);
            Assert.AreEqual(0, w.Intrigues.Demands.Count);
        }

        [Test]
        public void RefusingTheBlackmail_SpreadsTheProof()
        {
            var w = Blackmailed();
            Assert.IsNull(w.Intrigues.Refuse(Chrysanthemum));
            Assert.AreEqual(Settings.RefusedSpreadEvidence, w.Suspicion.Evidence(Fang));
            Assert.AreEqual(0, w.Intrigues.Demands.Count);
        }

        [Test]
        public void ADemandLeftUnanswered_IsARefusal()
        {
            var w = Blackmailed();
            w.Ctx.Clock.Restore(w.Ctx.Clock.Year + 1, w.Ctx.Clock.Phase);
            w.Intrigues.ProcessYear();
            Assert.AreEqual(Settings.RefusedSpreadEvidence, w.Suspicion.Evidence(Fang));
        }

        // ---- Proof made public buys no silence (user decision 2026-10-01: a cascade of blackmail in the long runs) ----

        [Test]
        public void ARefusedBlackmailer_MakesItsProofPublic()
        {
            var w = Blackmailed();
            w.Suspicion.AddEvidence(Chrysanthemum, 60);
            w.Intrigues.Refuse(Chrysanthemum);
            Assert.AreEqual(60, w.Suspicion.PublicEvidence, "all it held is known to all");
        }

        [Test]
        public void ProofEveryoneKnows_SellsNoSilence()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { Power(w, Fang) });
            w.Suspicion.AddPublicEvidence(60);
            w.Suspicion.AddEvidence(Fang, 60 + Settings.BlackmailEvidence - 1); // its own share falls short

            w.Intrigues.ProcessYear();

            Assert.IsFalse(w.Intrigues.Demands.Any(d => d.Kind == DemandKind.Silence));
        }

        [Test]
        public void ProofBeyondWhatIsPublic_StillSells()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { Power(w, Fang) });
            w.Suspicion.AddPublicEvidence(60);
            w.Suspicion.AddEvidence(Fang, 60 + Settings.BlackmailEvidence);

            w.Intrigues.ProcessYear();

            Assert.AreEqual(Fang, w.Intrigues.Demands.Single(d => d.Kind == DemandKind.Silence).Faction);
        }

        // ---- Theft ----

        [Test]
        public void AThief_TakesACapturedBeast_BeforeTheRitual()
        {
            var w = World(new FixedRandom(0.999));
            w.Resources.AddBeast(new CapturedBeast("b1", CultivationRealm.QiRefinement, 3, null));

            string stolen = w.Intrigues.Steal(Power(w, Ruan), IntrigueTarget.Beast);

            StringAssert.Contains("bête", stolen);
            Assert.AreEqual(0, w.Resources.Beasts.Count);
        }

        [Test]
        public void AThief_CopiesAManual_OrTakesStones_OrQi()
        {
            var w = World(new FixedRandom(0.999));
            w.Techniques.Learn("clear-spring-sutra");
            w.Resources.AddQi("clear-spring-qi", 2);
            var ruan = Power(w, Ruan);
            ruan.Techniques.Remove("clear-spring-sutra");
            int stones = w.Resources.SpiritStones;

            w.Intrigues.Steal(ruan, IntrigueTarget.Manual);
            w.Intrigues.Steal(ruan, IntrigueTarget.Stones);
            w.Intrigues.Steal(ruan, IntrigueTarget.Qi);

            CollectionAssert.Contains(ruan.Techniques, "clear-spring-sutra");
            Assert.IsTrue(w.Techniques.Knows("clear-spring-sutra"), "a copy: the clan keeps its manual");
            Assert.AreEqual(stones - (int)(stones * Settings.TheftStonesShare), w.Resources.SpiritStones);
            Assert.AreEqual(1, w.Resources.SpiritualQi["clear-spring-qi"]);
        }

        [Test]
        public void Patrols_MakeTheftRarer()
        {
            var ruan = new FactionData { Personality = FactionPersonality.Aggressive };
            Assert.Greater(IntrigueRules.TheftChance(ruan, patrols: 0, Settings), IntrigueRules.TheftChance(ruan, patrols: 3, Settings));
        }

        [Test]
        public void APatrol_MayCatchTheThief()
        {
            var w = World(new FixedRandom(0.0));
            var guard = w.Join(Fixtures.Cultivator());
            guard.CurrentTask = TaskType.Patrol;

            w.Intrigues.Steal(Power(w, Ruan), IntrigueTarget.Stones);

            Assert.AreEqual(Ruan, w.Captives.Prisoners.Single().Faction, "caught: an agent held, proof of the theft");
        }

        [Test]
        public void TheYear_BringsOneTheftAtMost()
        {
            var w = World(new FixedRandom(0.0));
            int stones = w.Resources.SpiritStones;
            w.Intrigues.ProcessYear();
            Assert.AreEqual(stones - (int)(stones * Settings.TheftStonesShare), w.Resources.SpiritStones);
        }

        // ---- Infiltration ----

        [Test]
        public void AnArrangedSpouse_MayBeASpy_AndAlwaysComesFromItsPower()
        {
            var w = World(new FixedRandom(0.0));
            var member = w.Join(Fixtures.Cultivator(age: 20));
            Assert.IsTrue(w.Marriages.HandleArrangedMarriage(member, Power(w, Chrysanthemum).ID, isForced: false));
            var spouse = w.Clan.FindById(member.SpouseID);
            Assert.AreEqual(Chrysanthemum, spouse.FromFaction);
            Assert.AreEqual(Chrysanthemum, spouse.SpyFor);
        }

        [Test]
        public void ASpy_FeedsItsPower_EachYear()
        {
            var w = World(new FixedRandom(0.999));
            var spy = w.Join(Fixtures.Cultivator());
            spy.SpyFor = Chrysanthemum;
            w.Intrigues.ProcessYear();
            Assert.AreEqual((Settings.SpyEvidence, Settings.SpyClues), (w.Suspicion.Evidence(Chrysanthemum), w.Suspicion.MirrorClues(Chrysanthemum)));
        }

        [Test]
        public void TheMirror_SoundsASpouse_AtItsPrice()
        {
            var w = World(new FixedRandom(0.999));
            var spy = w.Join(Fixtures.Cultivator());
            spy.SpyFor = Chrysanthemum;
            var honest = w.Join(Fixtures.Cultivator());
            w.Mirror.AddPower(100);
            int power = w.Mirror.MirrorPower;

            StringAssert.Contains(Chrysanthemum, w.Intrigues.Unmask(spy.ID));
            Assert.IsTrue(spy.SpyUnmasked);
            StringAssert.Contains("personne", w.Intrigues.Unmask(honest.ID));
            Assert.AreEqual(power - 2 * Settings.UnmaskMirrorCost, w.Mirror.MirrorPower);
        }

        [Test]
        public void ADoubleAgent_TurnsItsPowersProofAgainstItself()
        {
            var w = World(new FixedRandom(0.999));
            var spy = w.Join(Fixtures.Cultivator());
            spy.SpyFor = Chrysanthemum;
            w.Suspicion.AddEvidence(Chrysanthemum, 50);
            w.Mirror.AddPower(100);
            w.Intrigues.Unmask(spy.ID);

            Assert.IsNull(w.Intrigues.Turn(spy.ID));
            w.Intrigues.ProcessYear();

            Assert.AreEqual(50 - Settings.DoubleAgentRelief, w.Suspicion.Evidence(Chrysanthemum));
        }

        [Test]
        public void AnUnmaskedSpy_MayBeExecuted_ButNotAnUnsoundedOne()
        {
            var w = World(new FixedRandom(0.999));
            var spy = w.Join(Fixtures.Cultivator());
            spy.SpyFor = Chrysanthemum;
            Assert.IsNotNull(w.Intrigues.ExecuteSpy(spy.ID), "the clan must know first");
            w.Mirror.AddPower(100);
            w.Intrigues.Unmask(spy.ID);
            int relation = Power(w, Chrysanthemum).RelationWithPlayer;

            Assert.IsNull(w.Intrigues.ExecuteSpy(spy.ID));

            Assert.AreEqual(DeathCause.ExecutedAsSpy, spy.CauseOfDeath);
            Assert.AreEqual(relation + Settings.SpyExecutionRelation, Power(w, Chrysanthemum).RelationWithPlayer);
        }

        // ---- Saves ----

        [Test]
        public void RoundTrip_KeepsTheDemands_AndTheBoughtSilence()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Intrigues.RestoreDemands(new[] { new Demand(Chrysanthemum, 150, 1) }, new System.Collections.Generic.Dictionary<string, int> { [Ruan] = 9 });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(new Demand(Chrysanthemum, 150, 1), reloaded.Intrigues.Demands.Single());
            Assert.AreEqual(9, reloaded.Intrigues.QuietUntil[Ruan]);
        }

        // ---- Review ----

        [Test]
        public void AnAbsorbedBlackmailer_LeavesNoDemand()
        {
            var w = Blackmailed();
            w.Ctx.Events.TriggerPowerAbsorbed(Chrysanthemum, Ruan);
            Assert.AreEqual(0, w.Intrigues.Demands.Count);
        }

        [Test]
        public void APowerThatStruckThisYear_DoesNotAlsoBlackmail()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { Power(w, Chrysanthemum) });
            w.Suspicion.AddEvidence(Chrysanthemum, Fixtures.Content.Balance.Plots.ProofThreshold);
            w.Suspicion.AddToClan(Chrysanthemum, Fixtures.Content.Balance.Plots.ActThreshold);

            w.Plots.ProcessYear(); // it strikes, proof in hand
            w.Intrigues.ProcessYear();

            Assert.AreEqual(0, w.Intrigues.Demands.Count, "one blow a year");
        }

        [Test]
        public void APowerConfrontingTheClan_DoesNotBlackmail()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { Power(w, Chrysanthemum) });
            w.Suspicion.AddEvidence(Chrysanthemum, Settings.BlackmailEvidence);
            w.Secrets.RestoreConfrontation(new Confrontation(Chrysanthemum, 1));
            w.Intrigues.ProcessYear();
            Assert.AreEqual(0, w.Intrigues.Demands.Count);
        }

        [Test]
        public void AnUnmaskedSpy_NeverLeadsTheClan()
        {
            var w = World(new FixedRandom(0.999));
            var spy = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            spy.SpyFor = Chrysanthemum;
            w.Clan.AppointPatriarch(spy);
            w.Mirror.AddPower(100);

            w.Intrigues.Unmask(spy.ID);

            Assert.AreNotEqual(spy.ID, w.Clan.PatriarchID, "unmasked, the spy is set aside");
        }

        [Test]
        public void AFullLedger_GainsLess_FromARefusal()
        {
            var w = Blackmailed();
            w.Suspicion.AddEvidence(Ruan, 60);
            w.Intrigues.Refuse(Chrysanthemum);
            Assert.Less(w.Suspicion.Evidence(Ruan) - 60, Settings.RefusedSpreadEvidence, "diminishing: it knew most of it");
            Assert.AreEqual(Settings.RefusedSpreadEvidence, w.Suspicion.Evidence(Fang));
        }

        [Test]
        public void SoundingNobody_SaysSo()
        {
            var w = World(new FixedRandom(0.999));
            StringAssert.Contains("introuvable", w.Intrigues.Unmask("no-such-member"));
        }
    }
}
