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

        // ---- What the sect changes (the user's decision, 2026-09-30) ----

        [Test]
        public void ThePeaks_TeachBetter()
        {
            int Taught(bool sect)
            {
                var w = new TestWorld();
                var teacher = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
                var student = w.Join(Fixtures.Cultivator());
                w.Tasks.AssignTask(teacher, TaskType.Teaching);
                w.Tasks.AssignTask(student, TaskType.Cultivation);
                if (sect) w.Sect.Restore(1);
                int before = student.CultivationXP + 1000 * student.RealmStage;
                w.Tasks.ProcessYearlyTasks();
                return student.CultivationXP + 1000 * student.RealmStage - before;
            }
            Assert.Greater(Taught(true), Taught(false));
        }

        [Test]
        public void TheTown_PraysMore()
        {
            int Prayers(bool sect)
            {
                var s = Quiet();
                for (int i = 0; i < 10; i++) s.Clan.AddMember(Fixtures.Mortal()); // the town's mortals
                if (sect) s.Sect.Restore(s.Clock.Year);
                int before = s.Resources.Prayers;
                s.Events.TriggerYearStarted(s.Clock.Year);
                return s.Resources.Prayers - before;
            }
            Assert.Greater(Prayers(true), Prayers(false));
        }

        [Test]
        public void ASuccession_Unsettles_TheClan_LessSoInASect()
        {
            int Unrest(bool sect)
            {
                var s = Quiet();
                if (sect) s.Sect.Restore(s.Clock.Year);
                var heir = s.Clan.LivingMembers.First(m => m.ID != s.Clan.PatriarchID);
                var watcher = s.Clan.LivingMembers.First(m => m.ID != s.Clan.PatriarchID && m != heir);
                watcher.MentalStability = 80;
                s.Clan.AppointPatriarch(heir);
                s.Events.TriggerYearStarted(s.Clock.Year);
                return 80 - watcher.MentalStability;
            }
            Assert.AreEqual(Settings.SuccessionUnrest, Unrest(false));
            Assert.AreEqual(Settings.SectSuccessionUnrest, Unrest(true));
        }

        [Test]
        public void ANewSect_IsSeen_ByTheGreatSects()
        {
            var s = Ready();
            var sects = s.Factions.Factions.Where(f => f.Kind == FactionKind.Sect).ToList();
            var before = sects.ToDictionary(f => f.Name, f => f.RelationWithPlayer);
            s.Sect.Found();
            Assert.IsTrue(sects.All(f => f.RelationWithPlayer < before[f.Name] || f.RelationWithPlayer == -100));
            Assert.AreEqual(Settings.GreedFactor, s.Sect.GreedFactor);
        }

        [Test]
        public void ThePeaks_CostTheirUpkeep_EachYear()
        {
            var s = Ready();
            s.Sect.Found();
            s.Resources.AddSpiritStones(Settings.PeaksUpkeep);
            int stones = s.Resources.SpiritStones;
            s.Sect.PayThePeaks();
            Assert.AreEqual(stones - Settings.PeaksUpkeep, s.Resources.SpiritStones);
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
