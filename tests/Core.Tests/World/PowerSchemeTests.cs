using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers scheme against each other (the user's rule, 2026-10-03: what befalls the clan befalls the world): a power
    /// steals from a neighbour no stronger than itself — an artifact, a copy of a technique, wealth —, ambushes one of its
    /// elders for a ransom (unpaid in time, the captive is put to death), or harvests its ripe Dao; caught, the victim feuds
    /// back. Allies spare each other.
    /// </summary>
    [TestFixture]
    public class PowerSchemeTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });

        private static (FactionData Thief, FactionData Victim) Pair(GameSession s)
        {
            var thief = s.Factions.Factions.OrderByDescending(f => f.HighestRealm).First();
            var victim = s.Factions.Factions.First(f => f != thief && f.HighestRealm <= thief.HighestRealm && f.Elders.Any(e => e.Realm < CultivationRealm.GoldenCore));
            return (thief, victim);
        }

        [Test]
        public void AThief_TakesAnArtifactFirst()
        {
            var s = Session();
            var (thief, victim) = Pair(s);
            var sword = s.Artifacts.Shape(s.Context.Content.ArtifactForms[0].Id, CultivationRealm.Foundation, null);
            victim.Artifacts.Add(sword);
            s.PowerSchemes.Steal(thief, victim);
            Assert.IsTrue(thief.Artifacts.Contains(sword));
            Assert.IsFalse(victim.Artifacts.Contains(sword));
        }

        [Test]
        public void AThief_ElseCopiesATechnique_ElseTakesWealth()
        {
            var s = Session();
            var (thief, victim) = Pair(s);
            victim.Artifacts.Clear();
            var art = victim.Techniques.FirstOrDefault(t => !thief.Techniques.Contains(t));
            if (art == null) { art = "clear-spring-sutra"; victim.Techniques.Add(art); thief.Techniques.Remove(art); }
            s.PowerSchemes.Steal(thief, victim);
            Assert.IsTrue(thief.Techniques.Contains(art) && victim.Techniques.Contains(art), "a copy");
            thief.Techniques.AddRange(victim.Techniques.Where(t => !thief.Techniques.Contains(t)).ToList());
            victim.Wealth = 1000;
            int wealth = thief.Wealth;
            s.PowerSchemes.Steal(thief, victim);
            Assert.Greater(thief.Wealth, wealth);
            Assert.Less(victim.Wealth, 1000);
        }

        [Test]
        public void AnAmbush_TakesAnElder_ForARansom()
        {
            var s = Session();
            var (thief, victim) = Pair(s);
            int elders = victim.Elders.Count;
            Assert.IsTrue(s.PowerSchemes.Ambush(thief, victim));
            Assert.AreEqual(elders - 1, victim.Elders.Count);
            var captive = s.PowerSchemes.Captives.Single();
            victim.Wealth = 1_000_000;
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.PowerSchemes.ProcessYear();
            Assert.AreEqual(elders, victim.Elders.Count, "ransomed, it comes home");
            Assert.IsEmpty(s.PowerSchemes.Captives);
        }

        [Test]
        public void AnUnransomedCaptive_IsPutToDeath()
        {
            var s = Session();
            var (thief, victim) = Pair(s);
            s.PowerSchemes.Ambush(thief, victim);
            var captive = s.PowerSchemes.Captives.Single();
            victim.Wealth = 0;
            bool died = false;
            s.Events.OnElderDied += (p, e, d) => { if (e.Id == captive.Elder.Id) died = true; };
            for (int i = 0; i <= s.Context.Content.Balance.PowerSchemes.CaptiveYears; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.PowerSchemes.ProcessYear();
            }
            Assert.IsTrue(died);
            Assert.IsFalse(s.PowerSchemes.Captives.Contains(captive), "the world takes others meanwhile; this one is gone");
        }

        [Test]
        public void AStrongerPower_HarvestsARipeDao()
        {
            var s = Session();
            var (thief, victim) = Pair(s);
            var ripe = new FactionElder { Id = "ripe", Name = "ripe", Realm = CultivationRealm.Foundation, Stage = 4, MaxLifespan = 300 };
            victim.Elders.Add(ripe);
            int elders = victim.Elders.Count;
            Assert.IsTrue(s.PowerSchemes.Harvest(thief, victim));
            Assert.AreEqual(elders - 1, victim.Elders.Count, "a ripe Dao is taken (the victim may hold more than one)");
        }

        [Test]
        public void AlliesSpareEachOther()
        {
            var s = Session();
            var (thief, victim) = Pair(s);
            s.Politics.RestoreBonds(new[] { new PowerBond("a", BondKind.Alliance, thief.Name, victim.Name, 0, false) });
            Assert.IsFalse(s.PowerSchemes.MayScheme(thief, victim));
        }

        [Test]
        public void TheCaptives_SurviveASave()
        {
            var s = Session();
            var (thief, victim) = Pair(s);
            s.PowerSchemes.Ambush(thief, victim);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.VeteranQuietContent });
            Assert.AreEqual(1, reloaded.PowerSchemes.Captives.Count);
        }
    }
}
