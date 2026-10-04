using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The pilot and the artifacts (L4f, 2026-10-03): it arms its members with the best artifact each can wield (its own
    /// lineage first, the strongest members first), its forge makes one a year when the treasury allows, and it borrows
    /// from a friendly power. It steals none, and binds no Foundation that may still rise.
    /// </summary>
    [TestFixture]
    public class BalancePilotArtifactTests
    {
        private static GameSession Session()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 0;
            return s;
        }

        private static string Form(GameSession s, ArtifactEffect e) => s.Context.Content.ArtifactForms.First(f => f.Effect == e).Id;

        [Test]
        public void ThePilot_ArmsItsStrongest_WithWhatTheyCanWield()
        {
            var s = Session();
            var foundation = Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation);
            s.Clan.AddMember(foundation);
            var high = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.Foundation, null);
            var low = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.QiRefinement, null);
            BalanceRun.Act(s);
            Assert.AreEqual(high.Id, foundation.Artifact?.Id, "the strongest takes the best");
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.Artifact?.Id == low.Id), "the other goes to someone who can wield it");
        }

        [Test]
        public void ThePilotsForge_MakesAnArtifact_WhenTheTreasuryAllows()
        {
            var s = Session();
            s.Buildings.GetBuilding(BuildingType.Forge).Level = 2;
            s.Resources.AddOres(1_000);
            s.Resources.AddSpiritStones(50_000);
            var smith = Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation).AsSmith(s);
            s.Clan.AddMember(smith);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Artifacts.All.Any(), "the forge works");
        }

        [Test]
        public void ThePilot_BorrowsNothing_WhileItsArmouryHoldsSomething()
        {
            var s = Session();
            var sect = s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);
            sect.RelationWithPlayer = 90;
            s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.GoldenCore, null); // nobody wields it: it stays in store
            BalanceRun.Act(s);
            Assert.IsFalse(s.Artifacts.All.Any(a => a.LentBy == sect.Name), "a favour is not spent for nothing");
        }

        [Test]
        public void ThePilot_BorrowsFromAFriendlyPower()
        {
            var s = Session();
            var sect = s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);
            sect.RelationWithPlayer = 90;
            BalanceRun.Act(s);
            Assert.IsTrue(s.Artifacts.All.Any(a => a.LentBy == sect.Name));
        }
    }
}
