using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers are born and fall (the living world, step C, user decision 2026-10-01). A power without elders, or ruined,
    /// disperses into its strongest neighbour. A Purple Mansion elder who is not his power's strongest may leave with
    /// followers and found a gate — strictly: two elders of the Foundation or above follow him; a sect asks a Golden Core
    /// founder and four. A kingdom is the hardest of all: a power with a Golden Core, three vassals and the size of the
    /// greatest. While the world counts fewer powers than at first, a family may rise.
    /// </summary>
    [TestFixture]
    public class PowerLifecycleTests
    {
        private const string Gate = "Porte du Roc Obscur";

        private static GameContent With(System.Func<PowerLifecycleSettings, PowerLifecycleSettings> tweak) => Fixtures.QuietContent with
        {
            Balance = Fixtures.QuietContent.Balance with { PowerLifecycle = tweak(Fixtures.QuietContent.Balance.PowerLifecycle) }
        };

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Fixtures.QuietContent });

        private static FactionElder Elder(string id, CultivationRealm realm) =>
            new FactionElder { Id = id, Name = id, Realm = realm, Stage = 1, BornYear = 0, MaxLifespan = 1000, RealmSinceYear = 0 };

        [Test]
        public void APower_WithoutElders_FallsIntoItsStrongestNeighbour()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName("Famille Lou");
            var arts = power.Techniques.ToList();
            power.Elders.Clear();
            s.Lifecycle.ProcessYear();
            Assert.IsNull(s.Factions.GetFactionByName("Famille Lou"));
            Assert.IsTrue(s.Factions.Factions.Any(f => arts.All(f.Techniques.Contains)), "its arts pass to its heir");
        }

        [Test]
        public void ARuinedPower_Falls()
        {
            var s = Session();
            s.Factions.GetFactionByName("Famille Lou").PowerLevel = s.Context.Content.Balance.PowerLifecycle.FallPower - 1;
            s.Lifecycle.ProcessYear();
            Assert.IsNull(s.Factions.GetFactionByName("Famille Lou"));
        }

        [Test]
        public void AFallenPowersShard_PassesToItsHeir()
        {
            var s = Session();
            s.PowerShards.Hide("pale-seal-jade", "Famille Lou");
            s.Factions.GetFactionByName("Famille Lou").Elders.Clear();
            s.Lifecycle.ProcessYear();
            Assert.IsNotNull(s.PowerShards.HolderOf("pale-seal-jade"));
            Assert.AreNotEqual("Famille Lou", s.PowerShards.HolderOf("pale-seal-jade"));
        }

        [Test]
        public void APurpleMansion_WithTwoFollowers_FoundsAGate()
        {
            var s = Session(With(l => l with { SecessionChance = 1.0 }));
            var parent = s.Factions.GetFactionByName(Gate);
            parent.Elders.Clear();
            parent.Elders.Add(Elder("top", CultivationRealm.PurpleMansion));
            parent.Elders.Add(Elder("founder", CultivationRealm.PurpleMansion));
            parent.Elders.Add(Elder("f1", CultivationRealm.Foundation));
            parent.Elders.Add(Elder("f2", CultivationRealm.Foundation));
            s.Lifecycle.ProcessYear();
            var born = s.Factions.Factions.FirstOrDefault(f => f.Elders.Any(e => e.Id == "founder"));
            Assert.IsNotNull(born, "the founder leaves");
            Assert.AreEqual(FactionKind.Gate, born.Kind);
            Assert.AreNotEqual(parent, born);
            Assert.AreEqual(3, born.Elders.Count, "he leaves with his two followers");
            Assert.IsTrue(parent.Elders.Any(e => e.Id == "top"), "the strongest stays");
        }

        [Test]
        public void WithoutFollowers_NoGateIsFounded()
        {
            var s = Session(With(l => l with { SecessionChance = 1.0 }));
            var parent = s.Factions.GetFactionByName(Gate);
            parent.Elders.Clear();
            parent.Elders.Add(Elder("top", CultivationRealm.PurpleMansion));
            parent.Elders.Add(Elder("founder", CultivationRealm.PurpleMansion));
            parent.Elders.Add(Elder("f1", CultivationRealm.Foundation));
            s.Lifecycle.ProcessYear();
            Assert.IsTrue(parent.Elders.Any(e => e.Id == "founder"), "a gate is not founded as easily as a clan");
        }

        [Test]
        public void AFoundation_NeverFoundsAGate()
        {
            var s = Session(With(l => l with { SecessionChance = 1.0 }));
            var parent = s.Factions.GetFactionByName("Famille Lou");
            parent.Elders.Clear();
            parent.Elders.Add(Elder("top", CultivationRealm.Foundation));
            for (int i = 0; i < 4; i++) parent.Elders.Add(Elder($"f{i}", CultivationRealm.Foundation));
            s.Lifecycle.ProcessYear();
            Assert.AreEqual(5, parent.Elders.Count, "no Foundation founds a gate");
        }

        [Test]
        public void ASect_NeedsAGoldenCoreFounder_AndFourFollowers()
        {
            var s = Session(With(l => l with { SecessionChance = 1.0 }));
            var parent = s.Factions.GetFactionByName("Secte du Pic des Nuées");
            parent.Elders.Clear();
            parent.Elders.Add(Elder("top", CultivationRealm.GoldenCore));
            parent.Elders.Add(Elder("founder", CultivationRealm.GoldenCore));
            for (int i = 0; i < 4; i++) parent.Elders.Add(Elder($"f{i}", CultivationRealm.PurpleMansion));
            s.Lifecycle.ProcessYear();
            var born = s.Factions.Factions.Single(f => f.Elders.Any(e => e.Id == "founder"));
            Assert.AreEqual(FactionKind.Sect, born.Kind);
        }

        private static void Vassals(GameSession s, string suzerain, int count)
        {
            var bonds = s.Factions.Factions.Where(f => f.Name != suzerain && f.Kind == FactionKind.Family).Take(count)
                .Select((f, i) => new PowerBond($"b{i}", BondKind.Vassalage, suzerain, f.Name, 0, false)).ToList();
            s.Politics.RestoreBonds(bonds);
        }

        [Test]
        public void AKingdom_IsFounded_ByAGreatPowerWithAGoldenCore_AndThreeVassals()
        {
            var s = Session(With(l => l with { KingdomChance = 1.0 }));
            var sect = s.Factions.GetFactionByName("Secte du Pic des Nuées"); // a Golden Core, the greatest of Linxi
            Vassals(s, sect.Name, 3);
            s.Lifecycle.ProcessYear();
            Assert.AreEqual(FactionKind.State, sect.Kind, "the hardest of all: it reigns now");
        }

        [Test]
        public void AKingdom_IsNotFounded_WithoutVassals()
        {
            var s = Session(With(l => l with { KingdomChance = 1.0 }));
            var sect = s.Factions.GetFactionByName("Secte du Pic des Nuées");
            Vassals(s, sect.Name, 2);
            s.Lifecycle.ProcessYear();
            Assert.AreEqual(FactionKind.Sect, sect.Kind);
        }

        [Test]
        public void AKingdom_IsNotFounded_WithoutAGoldenCore()
        {
            var s = Session(With(l => l with { KingdomChance = 1.0 }));
            var gate = s.Factions.GetFactionByName(Gate); // a Purple Mansion gate
            gate.PowerLevel = 1_000_000;
            Vassals(s, gate.Name, 5);
            s.Lifecycle.ProcessYear();
            Assert.AreEqual(FactionKind.Gate, gate.Kind);
        }

        [Test]
        public void AFamilyRises_WhenThePowersAreFewer()
        {
            var s = Session(With(l => l with { RiseChance = 1.0 }));
            s.Factions.RemoveFaction("Famille Lou");
            var before = s.Factions.Factions.Select(f => f.Name).ToList();
            s.Lifecycle.ProcessYear();
            var risen = s.Factions.Factions.Single(f => !before.Contains(f.Name));
            Assert.AreEqual(FactionKind.Family, risen.Kind);
            Assert.AreEqual(CultivationRealm.Foundation, risen.HighestRealm, "a family rises with a Foundation");
            Assert.IsNotEmpty(risen.Elders);
        }

        [Test]
        public void NoFamilyRises_WhileTheWorldIsFull()
        {
            var s = Session(With(l => l with { RiseChance = 1.0 }));
            int powers = s.Factions.Factions.Count;
            s.Lifecycle.ProcessYear();
            Assert.AreEqual(powers, s.Factions.Factions.Count);
        }

        [Test]
        public void ANewPower_SurvivesASave()
        {
            var content = With(l => l with { RiseChance = 1.0 });
            var s = Session(content);
            s.Factions.RemoveFaction("Famille Lou");
            var before = s.Factions.Factions.Select(f => f.Name).ToList();
            s.Lifecycle.ProcessYear();
            var risen = s.Factions.Factions.Single(f => !before.Contains(f.Name));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = content });
            var again = reloaded.Factions.GetFactionByName(risen.Name);
            Assert.IsNotNull(again);
            Assert.AreEqual(risen.Elders.Count, again.Elders.Count);
        }

        [Test]
        public void TheChronicle_TellsAFall_AndABirth()
        {
            var s = Session(With(l => l with { RiseChance = 1.0 }));
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            s.Factions.GetFactionByName("Famille Lou").Elders.Clear();
            var before = s.Factions.Factions.Select(f => f.Name).ToList();
            s.Lifecycle.ProcessYear(); // it falls, and a family rises in its place
            Assert.IsTrue(chronicle.Entries.Any(e => e.Contains("Famille Lou")), "the fall is told");
            var risen = s.Factions.Factions.Single(f => !before.Contains(f.Name));
            Assert.IsTrue(chronicle.Entries.Any(e => e.Contains(risen.Name)), "the birth is told");
        }

        [Test]
        public void OverFiveCenturies_TheWorldKeepsItsShape([Values(1, 2, 3)] int seed)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = seed, Content = Fixtures.QuietContent });
            int first = s.Factions.Factions.Count;
            for (int year = 0; year < 500; year++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Elders.ProcessYear();
                s.PowerEconomy.ProcessYear();
                s.Lifecycle.ProcessYear();
            }
            Assert.That(s.Factions.Factions.Count, Is.InRange(first - 6, first + 6), "powers fall and rise, the world keeps its shape");
            Assert.That(s.Factions.Factions.Count(f => f.Kind == FactionKind.State), Is.LessThanOrEqualTo(s.Context.Content.Factions.Count(f => f.Kind == FactionKind.State) + 1),
                "a kingdom is the rarest founding");
        }
    }
}
