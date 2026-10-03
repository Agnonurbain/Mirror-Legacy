using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// A new Purple Mansion draws the old powers' eyes (audit §1.9, the user's decision 2026-10-03; 📚 Chi Wei): the powers of
    /// that realm or above able to reach it grow wary, and one may come to test it — the loser bears a wound. The world's new
    /// Purple Mansions alike.
    /// </summary>
    [TestFixture]
    public class AscentWatchTests
    {
        private static GameSession Session(double testChance)
        {
            var b = Fixtures.QuietContent.Balance;
            var content = Fixtures.QuietContent with { Balance = b with { AscentWatch = b.AscentWatch with { TestChance = testChance } } };
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
        }

        private static CharacterData Newcomer(GameSession s)
        {
            var m = Fixtures.Cultivator(age: 150, realm: CultivationRealm.PurpleMansion, stage: 1);
            s.Clan.AddMember(m);
            return m;
        }

        [Test]
        public void ANewPurpleMansion_MakesThePowersInReach_Wary()
        {
            var s = Session(testChance: 0);
            var watcher = s.Factions.Factions.First(f => f.HighestRealm >= CultivationRealm.PurpleMansion);
            var lesser = s.Factions.Factions.First(f => f.HighestRealm < CultivationRealm.PurpleMansion);
            int before = s.Suspicion.OfClan(watcher.Name), lesserBefore = s.Suspicion.OfClan(lesser.Name);
            s.Events.TriggerPurpleMansionAscent(Newcomer(s));
            Assert.Greater(s.Suspicion.OfClan(watcher.Name), before);
            Assert.AreEqual(lesserBefore, s.Suspicion.OfClan(lesser.Name), "a lesser power does not weigh a Purple Mansion");
        }

        [Test]
        public void ATest_ByAStrongerPower_WoundsTheNewcomer()
        {
            var s = Session(testChance: 1.0);
            foreach (var f in s.Factions.Factions) f.HighestRealm = CultivationRealm.GoldenCore; // the testers far above it
            var newcomer = Newcomer(s);
            s.Events.TriggerPurpleMansionAscent(newcomer);
            Assert.AreEqual(1, newcomer.DaoWounds, "tested by a True Monarch, it bears the wound");
        }

        [Test]
        public void APowersNewPurpleMansion_IsWatchedByTheOthers()
        {
            var s = Session(testChance: 0);
            var rising = s.Factions.Factions.First();
            var watcher = s.Factions.Factions.First(f => f != rising && f.HighestRealm >= CultivationRealm.PurpleMansion);
            int before = s.Suspicion.Distrust(watcher.Name, rising.Name);
            var elder = new FactionElder { Id = "e", Name = "e", Realm = CultivationRealm.PurpleMansion, Stage = 1, MaxLifespan = 500 };
            s.Events.TriggerElderRose(rising, elder);
            Assert.Greater(s.Suspicion.Distrust(watcher.Name, rising.Name), before);
        }
    }
}
