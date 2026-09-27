using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// Secrets for everyone, by rank (user decision 2026-09-27; LORE.md D7). Besides the mirror, the clan's deeds leave
    /// secrets of graded importance, and every power holds secrets of its own. Clues peel them from the surface — the
    /// least grave first. Probes (« sondages ») pierce them: provocation, infiltration, bribery, theft of records, the
    /// mirror's sight — alone or joined, once or repeated, weighed by strength, the target's guard and vigilance, its
    /// temper, proximity, an insider, the secret's rank. The target calls its allies: one that comes in time guards it,
    /// one that lingers — its own profit — lets the secret out and learns it too. A probe may be spotted, or end in
    /// disaster: an agent taken.
    /// </summary>
    [TestFixture]
    public class SecretsTests
    {
        private const string Ruan = "Famille Ruan";
        private const string Fang = "Famille Fang";
        private const string Tao = "Famille Tao";
        private const string Gu = "Famille Gu";   // a merchant
        private const string Peak = "Secte du Pic des Nuées";
        private static string Clan => SecretBook.ClanHolder;

        private static SecretSettings Settings => Fixtures.Content.Balance.Secrets;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 5)));
            return w;
        }

        private static Secret Hold(TestWorld w, string holder, string kind) => w.SecretBook.Create(kind, holder, null);

        // ---- The book of secrets ----

        [Test]
        public void TheCatalog_GradesSecretsFromMinorToVital_ForTheClanAndThePowers()
        {
            var kinds = Fixtures.Content.SecretKinds;
            CollectionAssert.IsSubsetOf(new[] { 1, 2, 3, 4 }, kinds.Select(k => k.Rank).Distinct());
            Assert.IsTrue(kinds.Any(k => k.Holder == SecretHolder.Clan) && kinds.Any(k => k.Holder == SecretHolder.Power));
            Assert.IsTrue(kinds.All(k => k.Rank >= 1 && k.Rank <= 4), "the supreme rank is the mirror's own (its knowers)");
        }

        [Test]
        public void EveryPower_HoldsSecretsOfItsOwn_FromTheWorldsDraw()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            Assert.IsTrue(s.Factions.Factions.All(f => s.SecretBook.Of(f.Name).Count >= 1));
            var again = GameSession.NewGame(Fixtures.Setup(1));
            CollectionAssert.AreEqual(s.SecretBook.All.Select(x => x.KindId), again.SecretBook.All.Select(x => x.KindId), "the world decides");
        }

        [Test]
        public void TheClansDeeds_LeaveSecrets()
        {
            var w = World(new FixedRandom(0.0));
            w.Mirror.AddPower(100);
            w.Suspicion.AddEvidence(Ruan, 40);
            Assert.IsTrue(w.Secrets.PlantFalseProof(Ruan, Fang));
            w.Captives.RestorePrisoners(new[] { new Prisoner("a", Tao, CultivationRealm.QiRefinement, 1) });
            w.Captives.Execute("a");

            CollectionAssert.AreEquivalent(new[] { "planted-false-proof", "executed-agent" }, w.SecretBook.Of(Clan).Select(x => x.KindId));
            Assert.AreEqual(Fang, w.SecretBook.Of(Clan).Single(x => x.KindId == "planted-false-proof").Subject);
        }

        [Test]
        public void Clues_PeelTheLeastGraveSecretFirst()
        {
            var w = World(new FixedRandom(0.999));
            var grave = Hold(w, Ruan, "betrayed-ally");     // rank 3
            var minor = Hold(w, Ruan, "internal-feud");     // rank 1

            var revealed = w.SecretBook.AddClues(Clan, Ruan, Settings.RankThreshold[0]);

            CollectionAssert.AreEqual(new[] { minor }, revealed);
            Assert.IsTrue(w.SecretBook.Knows(Clan, minor.Id));
            Assert.IsFalse(w.SecretBook.Knows(Clan, grave.Id));
        }

        [Test]
        public void APowerThatPiercesAGraveSecretOfTheClan_HoldsProof()
        {
            var w = World(new FixedRandom(0.999));
            var deed = Hold(w, Clan, "blood-of-anothers-beast"); // rank 3
            w.SecretBook.AddClues(Ruan, Clan, 1000);
            Assert.IsTrue(w.SecretBook.Knows(Ruan, deed.Id));
            Assert.AreEqual(Settings.KnownEvidenceByRank[2], w.Suspicion.Evidence(Ruan));
        }

        // ---- The factors of a probe ----

        private static ProbeFactors Factors(int prober = 40, int guard = 40, int rank = 2) => new ProbeFactors
        {
            Approach = ProbeApproach.Infiltration, ProberStrength = prober, TargetGuard = guard, Rank = rank,
            TargetTemper = FactionPersonality.Expansionist
        };

        [Test]
        public void TheOdds_WeighStrength_Guard_Rank_Vigilance_Insiders_Bribes_AndPartners()
        {
            double baseline = ProbeRules.SuccessChance(Factors(), Settings);
            Assert.Greater(ProbeRules.SuccessChance(Factors(prober: 70), Settings), baseline, "a stronger team");
            Assert.Less(ProbeRules.SuccessChance(Factors(guard: 70), Settings), baseline, "a better-guarded target");
            Assert.Less(ProbeRules.SuccessChance(Factors(rank: 4), Settings), baseline, "a graver secret");
            Assert.Less(ProbeRules.SuccessChance(Factors() with { Alertness = 40 }, Settings), baseline, "a watchful target");
            Assert.Greater(ProbeRules.SuccessChance(Factors() with { Insider = true }, Settings), baseline, "a spy in place");
            Assert.Greater(ProbeRules.SuccessChance(Factors() with { Neighbours = true }, Settings), baseline);
            Assert.Less(ProbeRules.SuccessChance(Factors() with { TargetDistrust = 60 }, Settings), baseline, "it watches those it distrusts");

            var bribe = Factors() with { Approach = ProbeApproach.Bribery, Stones = 500 };
            Assert.Greater(ProbeRules.SuccessChance(bribe with { TargetTemper = FactionPersonality.Merchant }, Settings),
                ProbeRules.SuccessChance(bribe with { TargetTemper = FactionPersonality.Isolationist }, Settings), "a merchant sells");
        }

        [Test]
        public void AProvocation_ReadsTheSurface_ButIsSeen()
        {
            var low = Factors(rank: 1) with { Approach = ProbeApproach.Provocation };
            var deep = Factors(rank: 4) with { Approach = ProbeApproach.Provocation };
            Assert.Greater(ProbeRules.SuccessChance(low, Settings) - ProbeRules.SuccessChance(deep, Settings),
                ProbeRules.SuccessChance(Factors(rank: 1), Settings) - ProbeRules.SuccessChance(Factors(rank: 4), Settings));
            Assert.Greater(Settings.DetectChance[ProbeApproach.Provocation], Settings.DetectChance[ProbeApproach.Infiltration]);
        }

        [Test]
        public void AnAlly_Lingers_WhenItDistrustsTheTarget_OrWantsItsSecrets()
        {
            var loyal = new FactionData { Personality = FactionPersonality.Isolationist };
            var schemer = new FactionData { Personality = FactionPersonality.Manipulative };
            Assert.Greater(ProbeRules.AllyPromptChance(loyal, distrust: 0, Settings), ProbeRules.AllyPromptChance(schemer, distrust: 0, Settings));
            Assert.Greater(ProbeRules.AllyPromptChance(loyal, distrust: 0, Settings), ProbeRules.AllyPromptChance(loyal, distrust: 60, Settings));
        }

        // ---- The clan probes ----

        private static ProbePlan Plan(TestWorld w, string target, ProbeApproach approach = ProbeApproach.Infiltration) =>
            new ProbePlan(target, approach, new List<string> { w.Clan.GetPatriarch().ID }, new List<string>(), 0);

        [Test]
        public void ASuccessfulProbe_BringsClues_AndSpendsTheTeamsYear()
        {
            var w = World(new SequenceRandom(0.0, 0.999)); // it succeeds, unseen
            var feud = Hold(w, Fang, "internal-feud");

            var outcome = w.Probes.Probe(Plan(w, Fang));

            Assert.IsNull(outcome.Refusal);
            Assert.IsTrue(outcome.Success && !outcome.Detected);
            Assert.IsTrue(w.SecretBook.Knows(Clan, feud.Id));
            Assert.AreEqual(w.Ctx.Clock.Year, w.Clan.GetPatriarch().LastOperationYear, "one operation a year");
        }

        [Test]
        public void ASpottedProbe_IsKnownToItsTarget()
        {
            var w = World(new SequenceRandom(0.0, 0.0)); // it succeeds, but is seen
            Hold(w, Fang, "internal-feud");
            int relation = w.Factions.GetFactionByName(Fang).RelationWithPlayer;

            var outcome = w.Probes.Probe(Plan(w, Fang));

            Assert.IsTrue(outcome.Detected);
            Assert.AreEqual(relation + Settings.DetectedRelation, w.Factions.GetFactionByName(Fang).RelationWithPlayer);
            Assert.AreEqual(Settings.DetectedDistrust, w.Suspicion.OfClan(Fang));
            Assert.Greater(w.Probes.Alertness(Fang), 0);
        }

        [Test]
        public void AFailedProbe_Seen_MayEndInDisaster_AMemberTaken()
        {
            var w = World(new SequenceRandom(0.999, 0.0, 0.0)); // it fails, is seen, and ends in disaster
            Hold(w, Ruan, "internal-feud");
            var outcome = w.Probes.Probe(Plan(w, Ruan));
            Assert.IsTrue(outcome.Disaster);
            Assert.IsTrue(w.Clan.LivingMembers.Any(m => m.CaptorFaction == Ruan));
        }

        [Test]
        public void RepeatedProbes_MakeTheTargetWatchful()
        {
            var w = World(new SequenceRandom(0.0, 0.0));
            Hold(w, Fang, "betrayed-ally");
            double before = w.Probes.ChanceAgainst(Plan(w, Fang));
            w.Probes.Probe(Plan(w, Fang));
            w.Clan.GetPatriarch().LastOperationYear = null;
            Assert.Less(w.Probes.ChanceAgainst(Plan(w, Fang)), before);
        }

        [Test]
        public void AProbe_NeedsAFreeTeam()
        {
            var w = World(new FixedRandom(0.0));
            w.Clan.GetPatriarch().LastOperationYear = w.Ctx.Clock.Year;
            Assert.IsNotNull(w.Probes.Probe(Plan(w, Fang)).Refusal);
        }

        [Test]
        public void AJointProbe_SharesTheSpoils_ButAPartnerMayTalk()
        {
            var w = World(new SequenceRandom(0.999, 0.0, 0.999)); // the partner keeps quiet; success; unseen
            var feud = Hold(w, Fang, "internal-feud");
            var plan = Plan(w, Fang) with { Partners = new List<string> { Tao } };

            var outcome = w.Probes.Probe(plan);

            Assert.IsTrue(outcome.Success);
            Assert.IsTrue(w.SecretBook.Knows(Tao, feud.Id), "the partner learns it too");

            var leaky = World(new SequenceRandom(0.0, 0.0, 0.999)); // the partner talks: the probe is seen
            Hold(leaky, Fang, "internal-feud");
            Assert.IsTrue(leaky.Probes.Probe(Plan(leaky, Fang) with { Partners = new List<string> { Tao } }).Detected);
        }

        [Test]
        public void ALingeringAlly_LetsTheSecretOut_AndLearnsIt()
        {
            var w = World(new SequenceRandom(0.999, 0.0, 0.999)); // the ally lingers; success; unseen
            w.Politics.RestoreBonds(new[] { new PowerBond("b", BondKind.Alliance, Fang, Gu, 1, false) });
            var feud = Hold(w, Fang, "internal-feud");

            var outcome = w.Probes.Probe(Plan(w, Fang));

            CollectionAssert.Contains(outcome.LateAllies, Gu);
            Assert.IsTrue(w.SecretBook.Progress(Gu, feud.Id) > 0, "it watched the secret come out");
        }

        [Test]
        public void APromptAlly_GuardsTheTarget()
        {
            var w = World(new FixedRandom(0.0));
            double alone = w.Probes.ChanceAgainst(Plan(w, Fang));
            w.Politics.RestoreBonds(new[] { new PowerBond("b", BondKind.Alliance, Fang, Peak, 1, false) });
            Assert.Less(w.Probes.ChanceAgainst(Plan(w, Fang), alliesPrompt: true), alone);
        }

        [Test]
        public void TheMirrorsSight_CostsByRank_IsNeverSeen()
        {
            var w = World(new SequenceRandom(0.0, 0.0));
            w.Mirror.AddPower(100);
            Hold(w, Fang, "betrayed-ally"); // rank 3
            int power = w.Mirror.MirrorPower;

            var outcome = w.Probes.Probe(Plan(w, Fang, ProbeApproach.MirrorSight));

            Assert.IsFalse(outcome.Detected);
            Assert.AreEqual(power - Settings.MirrorSightCostPerRank * 3, w.Mirror.MirrorPower);
        }

        // ---- The powers probe ----

        [Test]
        public void APowersProbe_OfTheClan_AlsoFeedsItsMirrorClues()
        {
            var w = World(new SequenceRandom(0.0, 0.999));
            Hold(w, Clan, "executed-agent");
            w.Probes.PowerProbe(w.Factions.GetFactionByName(Ruan), Clan, ProbeApproach.Infiltration, new List<string>());
            Assert.Greater(w.Suspicion.MirrorClues(Ruan), 0);
        }

        [Test]
        public void AnAbsorbedPowersKnowledge_PassesToItsSuzerain()
        {
            var w = World(new FixedRandom(0.999));
            var secret = Hold(w, Ruan, "internal-feud");
            w.SecretBook.AddClues(Fang, Ruan, 1000);
            w.Ctx.Events.TriggerPowerAbsorbed(Fang, Peak);
            Assert.IsTrue(w.SecretBook.Knows(Peak, secret.Id));
        }

        [Test]
        public void RoundTrip_KeepsTheSecrets_TheClues_AndTheVigilance()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            var secret = s.SecretBook.Create("planted-false-proof", Clan, Fang);
            s.SecretBook.AddClues(Ruan, Clan, 10);
            s.Probes.RestoreAlertness(new Dictionary<string, int> { [Fang] = 25 });

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.IsTrue(reloaded.SecretBook.All.Contains(secret));
            Assert.AreEqual(s.SecretBook.Progress(Ruan, secret.Id), reloaded.SecretBook.Progress(Ruan, secret.Id));
            Assert.AreEqual(25, reloaded.Probes.Alertness(Fang));
        }

        // ---- Review ----

        [Test]
        public void EverySecret_KeepsItsOwnId_EvenAfterAnAbsorption()
        {
            var w = World(new FixedRandom(0.999));
            Hold(w, Ruan, "internal-feud");
            var first = Hold(w, Clan, "executed-agent");
            w.Ctx.Events.TriggerPowerAbsorbed(Ruan, Peak); // one secret fewer in the book
            var second = Hold(w, Clan, "executed-agent");
            Assert.AreNotEqual(first.Id, second.Id);
        }

        [Test]
        public void APartnerNamedTwice_CountsOnce()
        {
            var w = World(new FixedRandom(0.999));
            Hold(w, Fang, "internal-feud");
            var once = Plan(w, Fang) with { Partners = new List<string> { Tao } };
            var twice = Plan(w, Fang) with { Partners = new List<string> { Tao, Tao } };
            Assert.AreEqual(w.Probes.ChanceAgainst(once), w.Probes.ChanceAgainst(twice));
        }

        [Test]
        public void ATargetWithNothingLeftToLearn_IsNotProbed()
        {
            var w = World(new FixedRandom(0.0));
            StringAssert.Contains("rien", w.Probes.Probe(Plan(w, Fang)).Refusal);
            Assert.IsNull(w.Clan.GetPatriarch().LastOperationYear, "the team keeps its year");
        }

        [Test]
        public void APower_DoesNotProbeAClanWithoutSecrets()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { w.Factions.GetFactionByName(Ruan) });
            w.Suspicion.AddToClan(Ruan, Settings.AiProbeSuspicion);
            w.Probes.ProcessYear();
            Assert.AreEqual(0, w.Probes.Alertness(Clan));
        }
    }
}
