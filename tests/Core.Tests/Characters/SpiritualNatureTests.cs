using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The elect of the Spiritual Nature (R10; LORE.md §3.1; the user's choices of 2026-10-12): one child with an orifice in a
    /// thousand is elected, its destiny bound to a Fruition of its element — a free one first — whose virtue forbids it tasks.
    /// </summary>
    [TestFixture]
    public class SpiritualNatureTests
    {
        private static GameSession World() => GameSession.NewGame(Fixtures.VeteranSetup(1));

        private static GameSession World(double electChance)
        {
            var c = Fixtures.VeteranQuietContent;
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = c with { Balance = c.Balance with { SpiritualNature = new SpiritualNatureSettings
                { ElectChance = electChance, ForbiddenTasks = c.Balance.SpiritualNature.ForbiddenTasks } } } });
        }

        private static CharacterData Child(GameSession s, Element element)
        {
            var child = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1);
            child.Affinity = element;
            child.Age = 8; // a child, just found to bear an orifice
            s.Clan.AddMember(child);
            return child;
        }

        [Test]
        public void Elect_BindsTheChild_ToAFruitionOfItsElement()
        {
            var s = World();
            var child = Child(s, Element.Metal);
            Assert.IsTrue(s.SpiritualNature.Elect(child));
            Assert.AreEqual(Element.Metal, s.Context.Content.Fruitions.Single(f => f.Id == child.FruitionId).Element);
        }

        [Test]
        public void Elect_SetsTheChild_OnTheSpiritualNature()
        {
            var s = World();
            var child = Child(s, Element.Water);
            s.SpiritualNature.Elect(child);
            Assert.AreEqual(CultivationSubPath.SpiritualNature, child.SubPath);
        }

        [Test]
        public void Elect_ChoosesAFreeFruition_WhenOneIsLeft()
        {
            var s = World();
            var child = Child(s, Element.Fire);
            bool anyFree = s.Context.Content.Fruitions.Any(f => f.Element == Element.Fire && s.Fruitions.State(f.Id)?.Status == FruitionStatus.Free);
            Assume.That(anyFree, "a free Fire lineage in this world");
            s.SpiritualNature.Elect(child);
            Assert.AreEqual(FruitionStatus.Free, s.Fruitions.State(child.FruitionId).Status);
        }

        [Test]
        public void Elect_ForbidsTheTasksOfItsVirtue()
        {
            var s = World();
            var child = Child(s, Element.Metal);
            s.SpiritualNature.Elect(child);
            Assert.IsFalse(TaskRules.IsAllowed(child, TaskType.Diplomacy), "Metal's righteousness bows to no one");
        }

        [Test]
        public void Elect_Refuses_AChildWithoutAFittingFruition()
        {
            var s = World();
            var child = Child(s, Element.None);
            Assert.IsFalse(s.SpiritualNature.Elect(child));
        }

        [Test]
        public void Elect_Refuses_AnAdult()
        {
            var s = World();
            var spouse = Child(s, Element.Metal);
            spouse.Age = 30; // a spouse examined on joining the clan
            Assert.IsFalse(s.SpiritualNature.Elect(spouse));
        }

        [Test]
        public void Elect_Refuses_AChildAlreadyBoundToALineage()
        {
            var s = World();
            var child = Child(s, Element.Metal);
            child.FruitionId = "some-lineage";
            Assert.IsFalse(s.SpiritualNature.Elect(child));
        }

        [Test]
        public void Elect_LeavesAForbiddenTask_AtOnce()
        {
            var s = World();
            var child = Child(s, Element.Metal);
            child.CurrentTask = TaskType.Diplomacy;
            s.SpiritualNature.Elect(child);
            Assert.AreEqual(TaskType.None, child.CurrentTask);
        }

        [Test]
        public void Consider_Elects_WhenFateFalls()
        {
            var s = World(electChance: 1);
            var child = Child(s, Element.Wood);
            s.SpiritualNature.Consider(new[] { child });
            Assert.AreEqual(CultivationSubPath.SpiritualNature, child.SubPath);
        }

        [Test]
        public void Consider_ElectsNobody_WhenFateDoesNotFall()
        {
            var s = World(electChance: 0);
            var child = Child(s, Element.Wood);
            s.SpiritualNature.Consider(new[] { child });
            Assert.AreNotEqual(CultivationSubPath.SpiritualNature, child.SubPath);
        }

        [Test]
        public void TheChronicle_TellsAnElect()
        {
            var s = World();
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            s.SpiritualNature.Elect(Child(s, Element.Earth));
            StringAssert.Contains("Nature Spirituelle", chronicle.Entries.Last());
        }

        [Test]
        public void TheRoster_ShowsTheElect_AndWhatItsVirtueForbids()
        {
            var s = World();
            var child = Child(s, Element.Metal);
            s.SpiritualNature.Elect(child);
            var row = MirrorChronicles.Presentation.ClanDomainView.Roster(s).Single(r => r.Id == child.ID);
            Assert.That(row.Destiny, Does.Contain("Nature Spirituelle").And.Contain(MirrorChronicles.Presentation.ClanDomainView.TaskLabel(TaskType.Diplomacy).ToLowerInvariant()));
        }

        [Test]
        public void RoundTrip_KeepsTheElect()
        {
            var s = World();
            var child = Child(s, Element.Metal);
            s.SpiritualNature.Elect(child);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.VeteranSetup());
            var back = reloaded.Clan.FindById(child.ID);
            Assert.AreEqual((CultivationSubPath.SpiritualNature, child.FruitionId, true),
                (back.SubPath, back.FruitionId, back.ForbiddenTasks.Contains(TaskType.Diplomacy)));
        }
    }
}
