using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The clan's own distrust of each power (user request 2026-09-27): its memory of what a power did to it — a strike,
    /// a member taken, an agent caught, a treaty betrayed, a probe spotted, blackmail, a thief caught, a spy unmasked, a
    /// coalition — fading slowly with the years. A power the clan distrusts probes it with more difficulty (the clan
    /// watches it); the diplomacy screen shows a sign of it, never a figure.
    /// </summary>
    [TestFixture]
    public class ClanWatchTests
    {
        private const string Ruan = "Famille Ruan";
        private static ClanWatchSettings Settings => Fixtures.Content.Balance.ClanWatch;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 5)));
            return w;
        }

        [Test]
        public void EveryOffence_IsRemembered()
        {
            var w = World(new FixedRandom(0.999));
            var bus = w.Ctx.Events;
            bus.TriggerClanStruck(Ruan);
            bus.TriggerMemberCaptured(w.Join(Fixtures.Cultivator()), Ruan);
            bus.TriggerAgentCaught(Ruan);
            bus.TriggerTreatyBetrayed(Ruan);
            bus.TriggerProbeSpotted(Ruan);
            bus.TriggerBlackmail(Ruan);
            bus.TriggerTheft("des pierres", Ruan);
            bus.TriggerSpyUnmasked(Ruan);
            bus.TriggerCoalitionFormed(new List<string> { Ruan });

            int expected = Settings.Struck + Settings.MemberTaken + Settings.AgentCaught + Settings.TreatyBetrayed + Settings.ProbeSpotted
                + Settings.Blackmail + Settings.ThiefCaught + Settings.SpyUnmasked + Settings.Coalition;
            Assert.AreEqual(System.Math.Min(SuspicionLedger.Max, expected), w.Suspicion.ClanDistrust(Ruan));
        }

        [Test]
        public void AnUnknownThief_LeavesNobodyToBlame()
        {
            var w = World(new FixedRandom(0.999));
            w.Ctx.Events.TriggerTheft("des pierres", null);
            Assert.IsTrue(w.Factions.Factions.All(f => w.Suspicion.ClanDistrust(f.Name) == 0));
        }

        [Test]
        public void TheMemory_FadesWithTheYears()
        {
            var w = World(new FixedRandom(0.999));
            w.Ctx.Events.TriggerClanStruck(Ruan);
            w.Watch.ProcessYear();
            Assert.AreEqual(Settings.Struck - Settings.FadePerYear, w.Suspicion.ClanDistrust(Ruan));
        }

        [Test]
        public void APowerTheClanDistrusts_ProbesItWithMoreDifficulty()
        {
            var w = World(new FixedRandom(0.999));
            w.SecretBook.Create("executed-agent", SecretBook.ClanHolder, null);
            var ruan = w.Factions.GetFactionByName(Ruan);
            double trusted = w.Probes.PowerChanceAgainst(ruan, SecretBook.ClanHolder, ProbeApproach.Infiltration);
            w.Suspicion.AddClanDistrust(Ruan, 60);
            Assert.Less(w.Probes.PowerChanceAgainst(ruan, SecretBook.ClanHolder, ProbeApproach.Infiltration), trusted);
        }

        [Test]
        public void TheDiplomacyScreen_ShowsASign_NeverAFigure()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.IsNull(DiplomacyView.Powers(s).Single(p => p.Name == Ruan).ClanWatch);
            s.Suspicion.AddClanDistrust(Ruan, 40);
            var sign = DiplomacyView.Powers(s).Single(p => p.Name == Ruan).ClanWatch;
            StringAssert.Contains("méfie", sign);
            Assert.IsFalse(sign.Any(char.IsDigit));
        }

        [Test]
        public void RoundTrip_KeepsTheClansDistrust()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Suspicion.AddClanDistrust(Ruan, 35);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(35, reloaded.Suspicion.ClanDistrust(Ruan));
        }

        [Test]
        public void AnAbsorbedPower_IsForgotten()
        {
            var w = World(new FixedRandom(0.999));
            w.Suspicion.AddClanDistrust(Ruan, 30);
            w.Ctx.Events.TriggerPowerAbsorbed(Ruan, "Secte du Pic des Nuées");
            Assert.AreEqual(0, w.Suspicion.ClanDistrust(Ruan));
        }

        [Test]
        public void AProbeEndingInDisaster_IsRememberedOnce()
        {
            var w = World(new SequenceRandom(0.999, 0.0, 0.0)); // it fails, is seen, ends in disaster: an agent caught
            w.SecretBook.Create("executed-agent", SecretBook.ClanHolder, null);
            w.Factions.GetFactionByName(Ruan).HighestRealm = w.Clan.LivingMembers.Max(m => m.Realm); // within sight of each other
            w.Probes.PowerProbe(w.Factions.GetFactionByName(Ruan), SecretBook.ClanHolder, ProbeApproach.Infiltration, new List<string>());
            Assert.AreEqual(Settings.AgentCaught, w.Suspicion.ClanDistrust(Ruan));
        }

        [Test]
        public void AnExtortion_IsRemembered()
        {
            var w = World(new FixedRandom(0.999));
            w.Ctx.Events.TriggerExtortion(Ruan);
            Assert.AreEqual(Settings.Extortion, w.Suspicion.ClanDistrust(Ruan));
        }
    }
}
