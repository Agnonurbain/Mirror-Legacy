using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The elders' bond to a treasure and Transmutation (the user's rule, 2026-10-03: what befalls the clan befalls the
    /// world): a power of the Purple Mansion may find a Spiritual Treasure; a peak Foundation of it may bind itself to one —
    /// a Purple Mansion's power, and never a step further; a world holder rarely tries a Transmutation to a free Realization
    /// of its Virtue — a failure births a holder's demon.
    /// </summary>
    [TestFixture]
    public class WorldBondAndTransmutationTests
    {
        private static GameSession Session(System.Func<WorldArsenalSettings, WorldArsenalSettings> tweak, int transmutation = 25)
        {
            var b = Fixtures.QuietContent.Balance;
            var content = Fixtures.QuietContent with
            {
                Balance = b with
                {
                    WorldArsenal = tweak(b.WorldArsenal with { ElderCondenseChance = 0, ForgeChance = 0, FamilyForgeChance = 0 }),
                    GoldenCore = b.GoldenCore with { TransmutationChance = transmutation, AxiomPenalty = 0, TransferChance = 0, TransformationChance = 0 },
                }
            };
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
        }

        [Test]
        public void AGreatPower_MayFindASpiritualTreasure()
        {
            var s = Session(a => a with { TreasureFindChance = 1.0 });
            var great = s.Factions.Factions.First(f => f.HighestRealm >= CultivationRealm.PurpleMansion);
            s.Arsenal.ProcessYear();
            Assert.IsTrue(great.Artifacts.Any(x => x.Class == ArtifactClass.SpiritualTreasure));
        }

        [Test]
        public void APeakFoundation_BindsItselfToItsPowersTreasure()
        {
            var s = Session(a => a with { BondChance = 1.0 });
            var power = s.Factions.Factions.First();
            power.Artifacts.Add(s.Artifacts.Shape(s.Context.Content.ArtifactForms[0].Id, CultivationRealm.PurpleMansion, null, ArtifactClass.SpiritualTreasure));
            var peak = new FactionElder { Id = "peak", Name = "peak", Realm = CultivationRealm.Foundation, Stage = 4, MaxLifespan = 300 };
            power.Elders.RemoveAll(e => e.Realm == CultivationRealm.Foundation); // the test's own peak, alone
            power.Elders.Add(peak);
            s.Arsenal.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, peak.Realm);
            Assert.IsTrue(peak.TreasureBound);
            Assert.IsFalse(power.Artifacts.Any(x => x.Class == ArtifactClass.SpiritualTreasure), "the treasure is its now, for life");
            peak.GoldenCoreOdds = 0.99;
            peak.Perfected = true;
            peak.RealmSinceYear = -1000;
            for (int i = 0; i < 5; i++) { s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase); s.Elders.ProcessYear(); }
            Assert.AreEqual(CultivationRealm.PurpleMansion, peak.Realm, "never a step further");
        }

        private static (FactionData, FactionElder) Holder(GameSession s)
        {
            foreach (var id in new[] { "gathered-wood", "nourishing-wood", "mutable-wood", "hidden-wood", "orthodox-wood" })
                if (s.Fruitions.State(id).Status == FruitionStatus.Occupied) s.Fruitions.Vacate(id);
            var power = s.Factions.Factions.First();
            var holder = new FactionElder { Id = "h", Name = "h", Realm = CultivationRealm.GoldenCore, Stage = 1, MaxLifespan = 1000, FruitionId = "gathered-wood" };
            s.Fruitions.Claim("gathered-wood", "h");
            power.Elders.Add(holder);
            return (power, holder);
        }

        [Test]
        public void AWorldHolder_MayTransmute()
        {
            var s = Session(a => a with { TransmuteChance = 1.0 }, transmutation: 99);
            var (power, holder) = Holder(s);
            s.Arsenal.ProcessYear();
            Assert.AreNotEqual("gathered-wood", holder.FruitionId);
            Assert.AreEqual(Element.Wood, s.Context.Content.Fruitions.Single(f => f.Id == holder.FruitionId).Element);
            Assert.AreEqual(FruitionStatus.Free, s.Fruitions.State("gathered-wood").Status);
        }

        [Test]
        public void AFailedTransmutation_BirthsAHoldersDemon()
        {
            var b = Fixtures.QuietContent.Balance;
            var s = Session(a => a with { TransmuteChance = 1.0 }, transmutation: 1);
            var (power, holder) = Holder(s);
            bool demon = false;
            s.Events.OnElderDied += (p, e, d) => { if (e == holder) demon = d; };
            s.Arsenal.ProcessYear();
            Assert.IsFalse(power.Elders.Contains(holder));
            Assert.IsTrue(demon);
        }
    }
}
