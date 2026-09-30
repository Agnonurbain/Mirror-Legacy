using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>
    /// The Double House (📚 the novel's family: « Peak and Town »; B3d, LORE.md §11.9): the clan founds its sect — its
    /// cultivators up on the peaks, its mortals down in the town — once a member of the Purple Mansion can hold a peak, it
    /// counts enough cultivators, and it can pay for the peaks. The ending « La Double Maison » follows.
    /// </summary>
    [TestFixture]
    public class SectTests
    {
        private static GameSession Quiet() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static SectSettings Settings => Fixtures.Content.Balance.Sect;

        /// <summary>A clan ready to found its sect: a Purple Mansion, enough Qi cultivators, the stones.</summary>
        private static GameSession Ready()
        {
            var s = Quiet();
            s.Clan.AddMember(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            while (s.Clan.LivingMembers.Count(m => m.Realm >= CultivationRealm.QiRefinement) < Settings.MinCultivators)
                s.Clan.AddMember(Fixtures.Cultivator());
            s.Resources.AddSpiritStones(Settings.FoundingStones);
            return s;
        }

        [Test]
        public void TheSect_NeedsAPurpleMansion()
        {
            var s = Quiet();
            StringAssert.Contains("Manoir Pourpre", s.Sect.FoundingRefusal());
        }

        [Test]
        public void TheSect_NeedsEnoughCultivators()
        {
            var s = Quiet();
            s.Clan.AddMember(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            s.Resources.AddSpiritStones(Settings.FoundingStones);
            StringAssert.Contains("cultivateurs", s.Sect.FoundingRefusal());
        }

        [Test]
        public void TheSect_NeedsTheStonesForItsPeaks()
        {
            var s = Ready();
            s.Resources.ConsumeSpiritStones(s.Resources.SpiritStones);
            StringAssert.Contains("pierres", s.Sect.FoundingRefusal());
        }

        [Test]
        public void FoundingTheSect_PaysForThePeaks_AndTellsTheDoubleHouse()
        {
            var s = Ready();
            int stones = s.Resources.SpiritStones;

            Assert.IsNull(s.Sect.Found());

            Assert.IsTrue(s.Sect.Founded && s.Sect.FoundedYear == s.Clock.Year && s.Resources.SpiritStones == stones - Settings.FoundingStones);
            Assert.IsTrue(s.Annals.Entries.Any(e => e.Kind == AnnalKind.SectFounded) && s.Endings.IsReached("double-house"));
        }

        [Test]
        public void TheSect_IsFoundedOnce()
        {
            var s = Ready();
            s.Sect.Found();
            s.Resources.AddSpiritStones(Settings.FoundingStones);
            Assert.IsNotNull(s.Sect.Found());
        }

        [Test]
        public void TheDoubleHouse_NoLongerAwaitsItsSystem()
        {
            Assert.IsNull(Fixtures.Content.Endings.Single(e => e.Id == "double-house").Awaits);
        }

        [Test]
        public void TheSect_SurvivesASave()
        {
            var s = Ready();
            s.Sect.Found();
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.IsTrue(reloaded.Sect.Founded && reloaded.Sect.FoundedYear == s.Sect.FoundedYear);
        }
    }
}
