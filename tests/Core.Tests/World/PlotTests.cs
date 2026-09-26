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
            var w = new TestWorld(rng);
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
    }
}
