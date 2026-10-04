using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// World parity for the Protective Formation (AUDIT_LORE.md §2.3, the user's rule: what befalls the clan befalls the world):
    /// every power keeps a formation its masters raise in time, up to what its realm knows; it guards the power against the
    /// clan's thefts and sabotages, and against its neighbours' thefts; a hired master sets no higher formation than its own.
    /// </summary>
    [TestFixture]
    public class PowerFormationTests
    {
        private static FormationSettings F => Fixtures.Content.Balance.Arts.Formation;

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Fixtures.QuietContent });

        [Test]
        public void EveryPower_BeginsWithAFormation_BelowWhatItsRealmKnows()
        {
            var s = Session();
            foreach (var p in s.Factions.Factions)
                Assert.AreEqual(System.Math.Max(0, PowerFormation.Cap(p, s.Context.Content) - 1), p.FormationLevel, p.Name);
            Assert.IsTrue(s.Factions.Factions.Any(p => p.FormationLevel > 0));
        }

        [Test]
        public void ItsMasters_RaiseIt_UpToItsRealmsKnowledge()
        {
            var c = Fixtures.QuietContent;
            var s = Session(c with { Balance = c.Balance with { Arts = c.Balance.Arts with { Formation = F with { PowerRiseChance = 1 } } } });
            var power = s.Factions.Factions.First(p => p.HighestRealm == CultivationRealm.Foundation);
            power.FormationLevel = 0;
            for (int i = 0; i < 10; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Elders.ProcessYear();
            }
            Assert.AreEqual(PowerFormation.Cap(power, s.Context.Content), power.FormationLevel);
        }

        [Test]
        public void AFormation_GuardsAPower_AgainstTheClansThefts()
        {
            var s = Session();
            var power = s.Factions.Factions.OrderBy(f => f.HighestRealm).First(); // within the team's reach
            var thief = Fixtures.Cultivator(realm: CultivationRealm.Foundation);
            s.Clan.AddMember(thief);
            var team = new List<CharacterData> { thief };
            power.FormationLevel = 0;
            double open = s.PowerShards.TheftChance(team, power);
            Assert.Greater(open, 0);
            power.FormationLevel = 3;
            double guarded = s.PowerShards.TheftChance(team, power);
            Assert.That(guarded, Is.EqualTo(open * (1 - 3 * s.Context.Content.Balance.DaoHunts.FormationGuard)).Within(1e-9));
        }

        [Test]
        public void AHiredMaster_SetsNoHigherFormation_ThanItsOwn()
        {
            var s = Session();
            s.Resources.AddSpiritStones(100_000);
            var friend = s.Factions.Factions.First(f => f.HighestRealm >= CultivationRealm.Foundation);
            friend.RelationWithPlayer = 100;
            friend.FormationLevel = 1;
            s.Buildings.GetBuilding(BuildingType.ProtectiveFormation).Level = 1;
            StringAssert.Contains("niveau 1", s.Buildings.HireRefusal(friend.Name));
            friend.FormationLevel = 2;
            Assert.IsNull(s.Buildings.HireRefusal(friend.Name));
        }

        [Test]
        public void AnOlderSave_GivesThePowers_TheirFormations()
        {
            var s = Session();
            var data = s.ToSaveData();
            data.SaveVersion = "2.39";
            foreach (var f in data.Factions) f.FormationLevel = 0;
            var loaded = GameSession.FromSaveData(data, new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            foreach (var p in loaded.Factions.Factions)
                Assert.AreEqual(System.Math.Max(0, PowerFormation.Cap(p, loaded.Context.Content) - 1), p.FormationLevel, p.Name);
        }
    }
}
