using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers' answer to what they suspect (L2c.4a; LORE.md D7 « everything is a plot »): a suspicious power
    /// investigates and gathers proof; past a threshold it strikes the clan. With proof, it is its right and the clan's
    /// name suffers; without proof, it strikes only when it believes it can — and every other power, allies included,
    /// starts doubting it in silence.
    /// </summary>
    [TestFixture]
    public class PlotTests
    {
        private const string Ruan = "Famille Ruan";
        private const string Peak = "Secte du Pic des Nuées";
        private const string Fang = "Famille Fang";

        private static PlotSettings Settings => Fixtures.Content.Balance.Plots;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng, MirrorLoreTests.EveryElderKnows); // the Golden Core powers' elders know the mirror
            w.Factions.InitializeFactions();
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5)); // the clan's strongest: Qi Cultivation
            return w;
        }

        // ---- Investigating ----

        [Test]
        public void ASuspiciousPower_Investigates_AndFindsProof()
        {
            var w = World(new FixedRandom(0.0));
            w.Suspicion.AddToClan(Ruan, Settings.InvestigateThreshold);

            w.Plots.ProcessYear();

            Assert.AreEqual(Settings.EvidencePerFinding, w.Suspicion.Evidence(Ruan));
        }

        [Test]
        public void ABarelySuspiciousPower_DoesNotBother()
        {
            var w = World(new FixedRandom(0.0));
            w.Suspicion.AddToClan(Ruan, Settings.InvestigateThreshold - 1);
            w.Plots.ProcessYear();
            Assert.AreEqual(0, w.Suspicion.Evidence(Ruan));
        }

        [Test]
        public void AStrongerPower_InvestigatesBetter()
        {
            var w = World(new FixedRandom(0.0));
            double family = PlotRules.InvestigationChance(w.Factions.GetFactionByName(Fang), 50, Fixtures.Content);
            double sect = PlotRules.InvestigationChance(w.Factions.GetFactionByName(Peak), 50, Fixtures.Content);
            Assert.Greater(sect, family);
        }

        // ---- Striking with proof ----

        [Test]
        public void AProvenCase_IsStruck_AndTheClansNameSuffers()
        {
            var w = World(new FixedRandom(0.0));
            var ruan = w.Factions.GetFactionByName(Ruan);
            int relation = ruan.RelationWithPlayer;
            w.Suspicion.AddToClan(Ruan, Settings.ActThreshold);
            w.Suspicion.AddEvidence(Ruan, Settings.ProofThreshold);
            int stones = w.Resources.SpiritStones;

            w.Plots.ProcessYear();

            Assert.AreEqual(relation + Settings.ReprisalRelation, ruan.RelationWithPlayer);
            Assert.Less(w.Resources.SpiritStones, stones);
            Assert.AreEqual(Settings.ProofReputation, w.Suspicion.OfClan(Fang), "the others hear of the clan's deed");
            Assert.AreEqual(0, w.Suspicion.Distrust(Fang, Ruan), "striking with proof is its right");
            Assert.AreEqual(0, w.Suspicion.OfClan(Ruan), "the account is settled");
        }

        // ---- Striking without proof: possible, with a hidden price ----

        [Test]
        public void AStrongPower_MayStrikeWithoutProof_AndEveryoneDoubtsIt()
        {
            var w = World(new FixedRandom(0.0));
            w.Suspicion.AddToClan(Peak, Settings.ActThreshold); // no proof at all

            w.Plots.ProcessYear();

            Assert.AreEqual(0, w.Suspicion.OfClan(Peak), "it struck");
            Assert.IsTrue(w.Factions.Factions.Where(f => f.Name != Peak)
                .All(f => w.Suspicion.Distrust(f.Name, Peak) == Settings.WitnessDistrust), "allies included, silently");
        }

        [Test]
        public void AWeakPower_DoesNotDareWithoutProof()
        {
            var w = World(new FixedRandom(0.0));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1)); // the clan now matches the Fang
            w.Suspicion.AddToClan(Fang, Settings.ActThreshold);

            w.Plots.ProcessYear();

            Assert.AreEqual(Settings.ActThreshold, w.Suspicion.OfClan(Fang), "it waits for proof");
        }

        // ---- Saves ----

        [Test]
        public void RoundTrip_KeepsTheProofGathered()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Suspicion.AddEvidence(Ruan, 35);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(35, reloaded.Suspicion.Evidence(Ruan));
        }

        // ---- Leaks (L2c.4b): those who know may talk; an oath of secrecy holds their tongue ----

        private static CharacterData Keeper(TestWorld w, int stability = 20)
        {
            var c = w.Join(Fixtures.Cultivator());
            c.KnowsMirrorSecret = true;
            c.MentalStability = stability;
            return c;
        }

        [Test]
        public void AKeeperWhoTalks_GivesAPowerCluesAboutTheMirror()
        {
            var w = World(new FixedRandom(0.0));
            Keeper(w);

            w.Secrets.ProcessYear();

            var told = w.Factions.Factions.Single(f => w.Suspicion.MirrorClues(f.Name) > 0);
            Assert.AreEqual(Settings.LeakMirrorClue, w.Suspicion.MirrorClues(told.Name));
            Assert.AreEqual(Settings.LeakEvidence, w.Suspicion.Evidence(told.Name));
        }

        [Test]
        public void TheLeak_GoesToThePowerThatAsksMost()
        {
            var w = World(new FixedRandom(0.0));
            Keeper(w);
            w.Suspicion.AddToClan(Fang, 40);
            w.Secrets.ProcessYear();
            Assert.AreEqual(Settings.LeakMirrorClue, w.Suspicion.MirrorClues(Fang));
        }

        [Test]
        public void ThoseWhoDoNotKnow_HaveNothingToTell()
        {
            var w = World(new FixedRandom(0.0));
            w.Secrets.ProcessYear();
            Assert.IsTrue(w.Factions.Factions.All(f => w.Suspicion.MirrorClues(f.Name) == 0));
        }

        [Test]
        public void LeakChance_FallsWithAStableMind_AndAnOathOfSecrecy()
        {
            var w = World(new FixedRandom(0.0));
            var shaken = Keeper(w, stability: 20);
            var steady = Keeper(w, stability: 90);
            Assert.Greater(PlotRules.LeakChance(shaken, sworn: false, Fixtures.Content), PlotRules.LeakChance(steady, sworn: false, Fixtures.Content));
            Assert.Less(PlotRules.LeakChance(shaken, sworn: true, Fixtures.Content), PlotRules.LeakChance(shaken, sworn: false, Fixtures.Content));
        }

        [Test]
        public void ASwornKeeperWhoTalks_BreaksTheOath()
        {
            var w = World(new FixedRandom(0.0));
            var keeper = Keeper(w);
            var patriarch = w.Join(Fixtures.Cultivator());
            w.Oaths.Swear(keeper, patriarch, new[] { "keep-secret" });

            w.Secrets.ProcessYear();

            Assert.IsTrue(keeper.ProgressionSealed || keeper.HeartDemonYearsLeft > 0, "punished by the Dao");
        }

        [Test]
        public void AHuntsTeam_AndATalismansBearer_ComeToKnowTheSecret()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Factions.InitializeFactions();
            w.Talismans.RestoreCalendar(w.Ctx.Clock.Year);
            var beast = new WorldBeast("iron-boar-heshan-1", "iron-boar", "heshan", CultivationRealm.QiRefinement, 1, null);
            w.Bestiary.Restore(new[] { beast });
            w.Knowledge.Reveal(FactKind.Beast, beast.Id, KnowledgeSource.Studied);
            var striker = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 7));
            var decoy = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3));

            w.Hunts.Execute(new HuntPlan
            {
                TargetBeastId = beast.Id,
                Team = new System.Collections.Generic.Dictionary<string, HuntRole> { [striker.ID] = HuntRole.Striker },
                DiversionMemberId = decoy.ID,
                DiversionRegionId = "wuyang"
            });

            Assert.IsTrue(striker.KnowsMirrorSecret && decoy.KnowsMirrorSecret);
        }

        [Test]
        public void RoundTrip_KeepsTheMirrorClues()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Suspicion.AddMirrorClues(Ruan, 40);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(40, reloaded.Suspicion.MirrorClues(Ruan));
        }

        [Test]
        public void RoundTrip_KeepsThePublicProof()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Suspicion.AddPublicEvidence(35);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(35, reloaded.Suspicion.PublicEvidence);
        }

        [Test]
        public void PublicProof_FadesWithTheYears()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Suspicion.AddPublicEvidence(35);
            s.AdvanceYear();
            Assert.Less(s.Suspicion.PublicEvidence, 35, "people forget");
        }

        // ---- The mirror's answers, the pierced secret, the seizure (L2c.4c) ----

        [Test]
        public void BlurMemories_DimsAPowersCluesAndProof_ForTheMirrorsPower()
        {
            var w = World(new FixedRandom(0.0));
            w.Suspicion.AddMirrorClues(Ruan, 80);
            w.Suspicion.AddEvidence(Ruan, 60);
            w.Mirror.AddPower(100);
            int power = w.Mirror.MirrorPower;

            Assert.IsTrue(w.Secrets.BlurMemories(Ruan));

            Assert.AreEqual(80 - Settings.BlurClues, w.Suspicion.MirrorClues(Ruan));
            Assert.AreEqual(60 - Settings.BlurEvidence, w.Suspicion.Evidence(Ruan));
            Assert.AreEqual(power - Settings.BlurMirrorCost, w.Mirror.MirrorPower);
        }

        [Test]
        public void BlurMemories_HoldsLessOnAGoldenCoresPower()
        {
            // the soul's locks are undetectable « even at the Purple Mansion » (§11.5): not beyond
            var w = World(new FixedRandom(0.0));
            w.Suspicion.AddMirrorClues(Peak, 80);
            w.Mirror.AddPower(100);
            w.Secrets.BlurMemories(Peak);
            Assert.AreEqual(80 - (int)(Settings.BlurClues * Settings.BlurStrongFactor), w.Suspicion.MirrorClues(Peak));
        }

        [Test]
        public void PlantFalseProof_TurnsAPowersProofIntoDistrustOfAnother()
        {
            var w = World(new FixedRandom(0.0));
            w.Suspicion.AddEvidence(Ruan, 60);
            w.Mirror.AddPower(100);

            Assert.IsTrue(w.Secrets.PlantFalseProof(Ruan, "Famille Lou"));

            Assert.AreEqual(60 - Settings.FalseProofAmount, w.Suspicion.Evidence(Ruan));
            Assert.AreEqual(Settings.FalseProofAmount, w.Suspicion.Distrust(Ruan, "Famille Lou"));
        }

        [Test]
        public void APiercedSecret_BringsAnInvestigatorAtOnce()
        {
            var w = World(new FixedRandom(0.999)); // nobody leaks
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);

            w.Secrets.ProcessYear();

            Assert.AreEqual(Peak, w.Secrets.Confrontation?.Faction);
            Assert.AreEqual(SuspicionLedger.Max, w.Suspicion.OfClan(Peak), "the consequences are immediate");
        }

        [Test]
        public void ThePowerThatStillKnows_SeizesTheMirror()
        {
            var w = World(new FixedRandom(0.999));
            bool seized = false;
            w.Ctx.Events.OnMirrorSeized += faction => seized = faction == Peak;
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);
            w.Secrets.ProcessYear(); // the investigator comes

            w.Secrets.ProcessYear(); // nothing was done

            Assert.IsTrue(seized);
        }

        [Test]
        public void APowerMadeToDoubt_CannotAct()
        {
            var w = World(new FixedRandom(0.999));
            bool seized = false;
            w.Ctx.Events.OnMirrorSeized += _ => seized = true;
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);
            w.Secrets.ProcessYear();
            for (int i = 0; i < 4; i++) // below the doubt's threshold, even for an elder: the mirror spends itself over and over
            {
                w.Mirror.AddPower(100);
                w.Secrets.BlurMemories(Peak);
            }

            w.Secrets.ProcessYear();

            Assert.IsFalse(seized);
            Assert.IsNull(w.Secrets.Confrontation);
        }

        private const string Kun = "Empire de Kun"; // the strongest Golden Core power

        [Test]
        public void AnOrdinaryPower_CannotNameTheMirror_OnlySuspectATreasure()
        {
            var w = World(new FixedRandom(0.999)); // no leak, and the rumour does not rise
            w.Suspicion.AddMirrorClues(Fang, SuspicionLedger.Max);

            w.Secrets.ProcessYear();

            Assert.IsNull(w.Secrets.Confrontation, "a family does not know what the mirror is");
            Assert.AreEqual(Fixtures.Content.Balance.MirrorLore.TreasureSuspicion, w.Suspicion.OfClan(Fang));
            Assert.Less(w.Suspicion.MirrorClues(Fang), Settings.DoubtClues, "it has told itself all it could");
        }

        [Test]
        public void TheRumour_MayReachAPowerWhereSomeoneKnows()
        {
            var w = World(new FixedRandom(0.0));
            w.Suspicion.AddMirrorClues(Fang, SuspicionLedger.Max);

            w.Secrets.ProcessYear();

            Assert.AreEqual(Settings.LeakMirrorClue, w.Suspicion.MirrorClues(Kun), "profit: it sells a rumour of treasure to the strongest who could read it");
            Assert.AreEqual(0, w.Suspicion.MirrorClues(Ruan), "never to an ordinary power");
        }

        [Test]
        public void WhenNobodyKnows_TheRumourGoesNowhere()
        {
            var nobody = Fixtures.Content with
            {
                Balance = Fixtures.Content.Balance with
                {
                    MirrorLore = Fixtures.Content.Balance.MirrorLore with
                    {
                        KnowChance = Fixtures.Content.Balance.MirrorLore.KnowChance.ToDictionary(p => p.Key, _ => 0.0),
                        PerCentury = Fixtures.Content.Balance.MirrorLore.PerCentury.ToDictionary(p => p.Key, _ => 0.0)
                    }
                }
            };
            var w = new TestWorld(new FixedRandom(0.0), nobody);
            w.Factions.InitializeFactions();
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);

            w.Secrets.ProcessYear();

            Assert.IsNull(w.Secrets.Confrontation);
            Assert.IsTrue(w.Factions.Factions.Where(f => f.Name != Peak).All(f => w.Suspicion.MirrorClues(f.Name) == 0));
        }

        [Test]
        public void TwoKnowersAtOnce_TheStrongestConfronts_TheOtherWaitsItsTurn()
        {
            var w = World(new FixedRandom(0.999));
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);
            w.Suspicion.AddMirrorClues(Kun, SuspicionLedger.Max);

            w.Secrets.ProcessYear();

            Assert.AreEqual(Kun, w.Secrets.Confrontation?.Faction, "the strongest who knows");
            Assert.AreEqual(SuspicionLedger.Max, w.Suspicion.MirrorClues(Peak), "a knower never merely suspects a treasure");
            Assert.AreEqual(0, w.Suspicion.OfClan(Peak));
        }

        [Test]
        public void AKnowerTooWeakToSeize_TellsOnlyAPeer()
        {
            var w = World(new FixedRandom(0.999));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.GoldenCore, stage: 1)); // the clan matches the Peak
            bool seized = false;
            w.Ctx.Events.OnMirrorSeized += _ => seized = true;
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);
            w.Secrets.ProcessYear(); // the investigator comes

            w.Secrets.ProcessYear();

            Assert.IsFalse(seized);
            Assert.Greater(w.Suspicion.MirrorClues(Kun), 0, "a peer who knows");
            Assert.AreEqual(0, w.Suspicion.MirrorClues(Fang), "never an ordinary power");
        }

        [Test]
        public void ASeizedMirror_LosesTheGame()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Events.TriggerMirrorSeized(Peak);
            Assert.IsTrue(s.Victory.GameLost);
        }

        [Test]
        public void RoundTrip_KeepsAConfrontation()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Secrets.RestoreConfrontation(new Confrontation(Peak, 1));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(new Confrontation(Peak, 1), reloaded.Secrets.Confrontation);
        }

        // ---- Review of 2026-09-26: a sale ends the confrontation; no double blow; ties drawn ----

        [Test]
        public void ASoldSecret_EndsTheConfrontation_ForGood()
        {
            var w = World(new FixedRandom(0.999));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.GoldenCore, stage: 1)); // the Peak cannot dare
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);
            w.Secrets.ProcessYear(); // the investigator comes
            w.Secrets.ProcessYear(); // it tells a peer

            w.Secrets.ProcessYear(); // and does not come back

            Assert.IsNull(w.Secrets.Confrontation);
            Assert.Less(w.Suspicion.MirrorClues(Peak), Settings.DoubtClues, "it passed on what it knew");
        }

        [Test]
        public void APiercedSecret_IsNotAlsoStruckAsAnOrdinaryCase_ThatYear()
        {
            var w = World(new FixedRandom(0.999));
            var peak = w.Factions.GetFactionByName(Peak);
            int relation = peak.RelationWithPlayer;
            int stones = w.Resources.SpiritStones;
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);

            w.Secrets.ProcessYear();
            w.Plots.ProcessYear();

            Assert.AreEqual(relation, peak.RelationWithPlayer);
            Assert.AreEqual(stones, w.Resources.SpiritStones);
        }

        [Test]
        public void ALeak_AmongPowersAskingAsMuch_GoesToAnyOfThem()
        {
            var w = World(new SequenceRandom(0.0, 0.999)); // it leaks; the draw falls on the last of the tied
            Keeper(w);
            w.Suspicion.AddToClan(Ruan, 40);
            w.Suspicion.AddToClan(Fang, 40);

            w.Secrets.ProcessYear();

            Assert.AreEqual(Settings.LeakMirrorClue, w.Suspicion.MirrorClues(Fang));
            Assert.AreEqual(0, w.Suspicion.MirrorClues(Ruan));
        }

        // ---- No chain reaction (the long games, 2026-09-29) ----

        [Test]
        public void AProvenStrike_SpendsItsProof()
        {
            var w = World(new FixedRandom(0.999));
            w.Suspicion.AddEvidence(Ruan, Settings.ProofThreshold);
            w.Suspicion.AddToClan(Ruan, Settings.ActThreshold);
            w.Plots.ProcessYear();
            Assert.IsTrue(w.Plots.StruckThisYear.Contains(Ruan));
            Assert.AreEqual(0, w.Suspicion.Evidence(Ruan), "the proof is spent by the blow");
        }

        [Test]
        public void ARumour_MakesThePowersWary_NotReadyToStrike()
        {
            var w = World(new FixedRandom(0.999));
            w.Suspicion.AddEvidence(Ruan, Settings.ProofThreshold);
            w.Suspicion.AddToClan(Ruan, Settings.ActThreshold);
            w.Suspicion.AddToClan(Fang, Settings.ActThreshold - 2);
            w.Suspicion.AddToClan(Peak, 0);
            w.Plots.ProcessYear();
            Assert.Less(w.Suspicion.OfClan(Fang), Settings.ActThreshold, "a rumour makes them investigate, not strike");
            Assert.AreEqual(Settings.ProofReputation, w.Suspicion.OfClan(Peak), "the clan's name suffers all the same");
        }

        [Test]
        public void SuspicionAndProof_FadeWithTheYears()
        {
            var w = World(new FixedRandom(0.999));
            w.Suspicion.AddToClan(Ruan, 40);
            w.Suspicion.AddEvidence(Ruan, 20);
            w.Ctx.Events.TriggerYearStarted(2);
            Assert.AreEqual(40 - Settings.SuspicionFadePerYear, w.Suspicion.OfClan(Ruan));
            Assert.AreEqual(20 - Settings.EvidenceFadePerYear, w.Suspicion.Evidence(Ruan));
        }

        // ---- The investigator, not the elder (2026-09-29) ----

        [Test]
        public void TheInvestigator_IsBlurredAtFullStrength_AndOneBlurMakesItDoubt()
        {
            var w = World(new FixedRandom(0.999));
            bool seized = false;
            w.Ctx.Events.OnMirrorSeized += _ => seized = true;
            w.Suspicion.AddMirrorClues(Peak, SuspicionLedger.Max);
            w.Secrets.ProcessYear(); // the investigator comes
            Assume.That(w.Secrets.Confrontation?.Faction, Is.EqualTo(Peak));
            w.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);

            Assert.IsTrue(w.Secrets.BlurMemories(Peak));
            Assert.AreEqual(SuspicionLedger.Max - Settings.BlurClues, w.Suspicion.MirrorClues(Peak), "an envoy's mind, not an elder's");

            w.Secrets.ProcessYear();
            Assert.IsFalse(seized);
            Assert.IsNull(w.Secrets.Confrontation, "made to doubt");
        }
    }
}
