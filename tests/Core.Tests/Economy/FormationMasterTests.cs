using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Economy
{
    /// <summary>
    /// The Protective Formation is an Immortal Art (AUDIT_LORE.md §2.3, the user's decisions 2026-10-03/04): a formation master of
    /// the clan raises it — gifted, holding the art's legacy, of a mastery by level — or the clan hires a friendly power's.
    /// </summary>
    [TestFixture]
    public class FormationMasterTests
    {
        private static FormationSettings F => Fixtures.Content.Balance.Arts.Formation;

        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Resources.AddSpiritStones(100_000);
            return s;
        }

        private static CharacterData Master(GameSession s, int mastery)
        {
            s.Arts.GainLegacy(ImmortalArt.Formations);
            var m = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            m.TalismanQiId = "holding-profit"; // the hundred arts' talisman Qi: a gift
            m.ArtMastery[ImmortalArt.Formations] = mastery;
            s.Clan.AddMember(m);
            return m;
        }

        [Test]
        public void WithoutAFormationMaster_TheFormationDoesNotRise()
        {
            var s = Session();
            s.Clan.AddMember(Fixtures.Cultivator(realm: CultivationRealm.Foundation)); // a Foundation alone no longer suffices
            StringAssert.Contains("formations", s.Buildings.UpgradeRefusal(BuildingType.ProtectiveFormation));
            Assert.IsFalse(s.Buildings.Upgrade(BuildingType.ProtectiveFormation));
        }

        [Test]
        public void AnApprentice_RaisesTheFirstLevels_AndSpendsItsYear()
        {
            var s = Session();
            var m = Master(s, 1);
            Assert.IsNull(s.Buildings.UpgradeRefusal(BuildingType.ProtectiveFormation));
            Assert.IsTrue(s.Buildings.Upgrade(BuildingType.ProtectiveFormation));
            Assert.AreEqual(1, s.Buildings.FormationLevel);
            Assert.AreEqual(s.Clock.Year, m.LastOperationYear);
            StringAssert.Contains("formations", s.Buildings.UpgradeRefusal(BuildingType.ProtectiveFormation), "its year is spent");
        }

        [Test]
        public void TheHigherLevels_AskAnAdept_ThenAMaster()
        {
            var s = Session();
            Master(s, 1);
            s.Buildings.GetBuilding(BuildingType.ProtectiveFormation).Level = F.AdeptFromLevel - 1;
            StringAssert.Contains("adepte", s.Buildings.UpgradeRefusal(BuildingType.ProtectiveFormation));
            Master(s, s.Context.Content.Balance.Arts.AdeptAt);
            Assert.IsNull(s.Buildings.UpgradeRefusal(BuildingType.ProtectiveFormation));
            s.Buildings.GetBuilding(BuildingType.ProtectiveFormation).Level = F.MasterFromLevel - 1;
            StringAssert.Contains("maître", s.Buildings.UpgradeRefusal(BuildingType.ProtectiveFormation));
        }

        [Test]
        public void AFriendlyPower_LendsItsMaster_ForAFee()
        {
            var s = Session();
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 0;
            var friend = s.Factions.Factions.First(f => f.HighestRealm >= CultivationRealm.Foundation);
            StringAssert.Contains("relation", s.Buildings.HireRefusal(friend.Name));
            friend.RelationWithPlayer = F.HireRelation;
            int stones = s.Resources.SpiritStones;
            int cost = s.Buildings.HireCost();
            Assert.Greater(cost, s.Buildings.GetBuilding(BuildingType.ProtectiveFormation).UpgradeCost);
            Assert.IsNull(s.Buildings.HireFormation(friend.Name));
            Assert.AreEqual(1, s.Buildings.FormationLevel);
            Assert.AreEqual(stones - cost, s.Resources.SpiritStones);
        }

        [Test]
        public void TheFormationsHeight_AsksAPurpleMansionPower()
        {
            var s = Session();
            var friend = s.Factions.Factions.First(f => f.HighestRealm == CultivationRealm.Foundation);
            friend.RelationWithPlayer = 100;
            s.Buildings.GetBuilding(BuildingType.ProtectiveFormation).Level = F.MasterFromLevel - 1;
            StringAssert.Contains("Manoir Pourpre", s.Buildings.HireRefusal(friend.Name));
        }
    }
}
