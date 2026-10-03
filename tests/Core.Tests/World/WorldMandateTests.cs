using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The elders' Mandate of Life (the user's rule, 2026-10-03: what befalls the clan befalls the world): a war of its power or a
    /// demon ravaging its region may embody the image of an elder's abilities — a Purple
    /// Mansion not yet at its Grand Perfection may gather its five abilities at a leap.
    /// </summary>
    [TestFixture]
    public class WorldMandateTests
    {
        private static GameSession Session(double leap)
        {
            var b = Fixtures.QuietContent.Balance;
            var content = Fixtures.QuietContent with { Balance = b with { Mandate = b.Mandate with { ElderLeapChance = leap } } };
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
        }

        private static FactionElder Mansion(FactionData power)
        {
            var e = new FactionElder { Id = $"m-{power.ID}", Name = "m", Realm = CultivationRealm.PurpleMansion, Stage = 2, MaxLifespan = 800, Perfected = false };
            power.Elders.Add(e);
            return e;
        }

        [Test]
        public void AWar_MayBringAnElderToItsGrandPerfection()
        {
            var s = Session(1.0);
            var a = s.Factions.Factions[0];
            var d = s.Factions.Factions[1];
            var bystander = s.Factions.Factions[2];
            var ea = Mansion(a);
            var ed = Mansion(d);
            var eb = Mansion(bystander);
            s.Events.TriggerWarBegun(a.Name, d.Name);
            Assert.IsTrue(ea.Perfected && ed.Perfected);
            Assert.IsFalse(eb.Perfected, "a war elsewhere embodies nothing of it");
        }

        [Test]
        public void ADemonsPeril_MayToo_ButAFellowsDeathDoesNot()
        {
            var s = Session(1.0);
            var power = s.Factions.Factions[0];
            var e = Mansion(power);
            s.Events.TriggerElderDied(power, new FactionElder { Id = "x", Name = "x", Realm = CultivationRealm.Foundation }, false);
            Assert.IsFalse(e.Perfected, "a mourning needs kin: the elders have none");
            s.Events.TriggerWorldDemon(new MetalEssenceDemon("d", "d", DemonTier.Ascent, 0, 10) { RegionId = power.RegionId });
            Assert.IsTrue(e.Perfected);
        }

        [Test]
        public void WithoutTheLeap_NothingChanges()
        {
            var s = Session(0.0);
            var a = s.Factions.Factions[0];
            var e = Mansion(a);
            s.Events.TriggerWarBegun(a.Name, s.Factions.Factions[1].Name);
            Assert.IsFalse(e.Perfected);
        }
    }
}
