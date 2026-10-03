using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The clan's forge makes artifacts (L4f, user decisions 2026-10-03): a smith of the rank's realm at least, a forge high
    /// enough (a Spiritual Artifact asks a high one), ores and stones; the artifact takes the smith's lineage. An artifact may
    /// be raised by one rank (📚 a Foundation's soft armour raised to a Purple Mansion's Spiritual Artifact). A Spiritual
    /// Treasure is never forged: only found.
    /// </summary>
    [TestFixture]
    public class ArtifactForgeTests
    {
        private static GameSession Session(int forge = 5)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Buildings.GetBuilding(BuildingType.Forge).Level = forge;
            s.Resources.AddOres(10_000);
            s.Resources.AddSpiritStones(100_000);
            return s;
        }

        private static CharacterData Smith(GameSession s, CultivationRealm realm)
        {
            var m = Fixtures.Cultivator(age: 100, realm: realm);
            var lineage = s.Context.Content.Fruitions.First(f => f.Abilities.Count > 0);
            if (realm >= CultivationRealm.Foundation) m.FoundationId = $"{lineage.Id}:{lineage.Abilities[0].Id}";
            s.Clan.AddMember(m);
            return m;
        }

        private static string Sword(GameSession s) => s.Context.Content.ArtifactForms.First(f => f.Effect == ArtifactEffect.Combat).Id;

        [Test]
        public void ASmith_ForgesAnArtifactOfItsLineage_ForOresAndStones()
        {
            var s = Session();
            var smith = Smith(s, CultivationRealm.Foundation);
            int ores = s.Resources.SpiritualOres;
            Assert.IsNull(s.Forge.Make(Sword(s), CultivationRealm.Foundation, smith.ID));
            var made = s.Artifacts.Armoury.Single();
            Assert.AreEqual(ArtifactClass.DharmaArtifact, made.Class);
            Assert.AreEqual(FoundationRef.Parse(smith.FoundationId).FruitionId, made.Lineage, "it takes the smith's lineage");
            Assert.Less(s.Resources.SpiritualOres, ores);
            StringAssert.Contains("année", s.Forge.Make(Sword(s), CultivationRealm.Foundation, smith.ID), "one work a year");
        }

        [Test]
        public void ASmith_ForgesNothingAboveItsRealm()
        {
            var s = Session();
            var smith = Smith(s, CultivationRealm.QiRefinement);
            StringAssert.Contains("royaume", s.Forge.Make(Sword(s), CultivationRealm.Foundation, smith.ID));
        }

        [Test]
        public void ASpiritualArtifact_AsksAHighForge()
        {
            var s = Session(forge: 2);
            var smith = Smith(s, CultivationRealm.PurpleMansion);
            StringAssert.Contains("forge", s.Forge.Make(Sword(s), CultivationRealm.PurpleMansion, smith.ID));
            s.Buildings.GetBuilding(BuildingType.Forge).Level = s.Context.Content.Balance.Artifacts.Forging[CultivationRealm.PurpleMansion].ForgeLevel;
            Assert.IsNull(s.Forge.Make(Sword(s), CultivationRealm.PurpleMansion, smith.ID));
            Assert.AreEqual(ArtifactClass.SpiritualArtifact, s.Artifacts.Armoury.Single().Class);
        }

        [Test]
        public void AnArtifact_IsRaisedByOneRank()
        {
            var s = Session();
            var smith = Smith(s, CultivationRealm.PurpleMansion);
            var armour = s.Artifacts.Create(s.Context.Content.ArtifactForms.First(f => f.Effect == ArtifactEffect.Protection).Id, CultivationRealm.Foundation, null);
            Assert.IsNull(s.Forge.Raise(armour.Id, smith.ID));
            var raised = s.Artifacts.Armoury.Single();
            Assert.AreEqual(CultivationRealm.PurpleMansion, raised.Rank);
            Assert.AreEqual(ArtifactClass.SpiritualArtifact, raised.Class);
            Assert.Greater(raised.Protection, armour.Protection);
            Assert.AreEqual(armour.Id, raised.Id, "the same armour, raised");
        }

        [Test]
        public void ASpiritualTreasure_IsNeverForgedNorRaised()
        {
            var s = Session();
            var smith = Smith(s, CultivationRealm.GoldenCore);
            var treasure = s.Artifacts.Create(Sword(s), CultivationRealm.PurpleMansion, null, ArtifactClass.SpiritualTreasure);
            Assert.IsNotNull(s.Forge.Raise(treasure.Id, smith.ID));
            Assert.IsNotNull(s.Forge.Raise(s.Artifacts.Create(Sword(s), CultivationRealm.PurpleMansion, null).Id, smith.ID),
                "above the Purple Mansion, the Golden Core's own treasures");
        }
    }
}
