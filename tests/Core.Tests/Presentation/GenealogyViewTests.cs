using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The clan's family tree (G6): every member ever recorded, by generation — founders first, a child one below its
    /// parents, a spouse married in beside the member — the living and the dead, the patriarch marked.
    /// </summary>
    [TestFixture]
    public class GenealogyViewTests
    {
        private static GameSession NewGame() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static KinNode Node(GameSession s, string id) => GenealogyView.Tree(s).Single(n => n.Id == id);

        [Test]
        public void TheFounders_StandAtTheFirstGeneration()
        {
            var s = NewGame();
            Assert.IsTrue(s.Clan.LivingMembers.All(m => Node(s, m.ID).Generation == 0));
            Assert.IsTrue(Node(s, s.Clan.PatriarchID).IsPatriarch);
        }

        [Test]
        public void AChild_StandsOneGenerationBelowItsParents()
        {
            var s = NewGame();
            var father = s.Clan.GetPatriarch();
            var mother = s.Clan.LivingMembers.First(m => !m.IsMale && m.SpouseID == father.ID);
            var child = s.Clan.GenerateChild(father, mother);

            Assert.AreEqual(1, Node(s, child.ID).Generation);
            Assert.AreEqual((father.ID, mother.ID), (Node(s, child.ID).FatherId, Node(s, child.ID).MotherId));
        }

        [Test]
        public void ASpouseMarriedIn_StandsBesideTheMember()
        {
            var s = NewGame();
            var father = s.Clan.GetPatriarch();
            var mother = s.Clan.LivingMembers.First(m => !m.IsMale && m.SpouseID == father.ID);
            var child = s.Clan.GenerateChild(father, mother);
            var bride = new CharacterData { FirstName = "Lan", LastName = "Gu", IsMale = !child.IsMale, Age = 20, SpouseID = child.ID };
            child.SpouseID = bride.ID;
            s.Clan.AddMember(bride);

            var tree = GenealogyView.Tree(s);
            var a = tree.Single(n => n.Id == child.ID);
            var b = tree.Single(n => n.Id == bride.ID);
            Assert.AreEqual(a.Generation, b.Generation);
            Assert.AreEqual(1, System.Math.Abs(a.Column - b.Column), "husband and wife side by side");
        }

        [Test]
        public void TheDead_StayInTheTree_WithHowTheyDied()
        {
            var s = NewGame();
            var elder = s.Clan.LivingMembers.Last();
            s.Clan.Kill(elder, DeathCause.OldAge);

            var node = Node(s, elder.ID);
            Assert.IsFalse(node.Alive);
            StringAssert.Contains("de vieillesse", node.Status);
        }

        [Test]
        public void ACaptive_IsMarkedInTheTree()
        {
            var s = NewGame();
            var member = s.Clan.LivingMembers.Last();
            s.Captives.Take(member, "Famille Ruan");
            StringAssert.Contains("captif de Famille Ruan", Node(s, member.ID).Status);
        }

        [Test]
        public void EachGeneration_HasItsColumnsInOrder()
        {
            var tree = GenealogyView.Tree(NewGame());
            foreach (var generation in tree.GroupBy(n => n.Generation))
                CollectionAssert.AreEquivalent(Enumerable.Range(0, generation.Count()), generation.Select(n => n.Column));
        }
    }
}
