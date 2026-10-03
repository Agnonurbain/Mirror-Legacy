using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Artifacts found (L4f, user decisions 2026-10-03): a tomb's guardian leaves its Spiritual Artifact, sometimes a
    /// Spiritual Treasure — the only source of those, with the phenomena of the dead and the loot of war; ruins yield Dharma
    /// Artifacts; a yielding enemy is plundered. A Foundation at its peak may bind itself to a Spiritual Treasure (LORE.md
    /// §5.4.2, Lan Guyu): the power of a Purple Mansion with one ability, two of its own lineage, and never a step further.
    /// </summary>
    [TestFixture]
    public class ArtifactFindTests
    {
        private static GameContent Sure()
        {
            var b = Fixtures.QuietContent.Balance;
            return Fixtures.QuietContent with
            {
                Balance = b with
                {
                    Artifacts = b.Artifacts with
                    {
                        Finding = b.Artifacts.Finding with { RuinsChance = 1, WarLootChance = 1, MansionDeathTreasureChance = 1 }
                    }
                }
            };
        }

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Sure() });

        [Test]
        public void ATombsGuardian_LeavesAPurpleMansionsArtifact()
        {
            var s = Session();
            s.Events.TriggerTombLooted();
            var found = s.Artifacts.Armoury.Single();
            Assert.AreEqual(CultivationRealm.PurpleMansion, found.Rank);
            Assert.That(found.Class, Is.EqualTo(ArtifactClass.SpiritualArtifact).Or.EqualTo(ArtifactClass.SpiritualTreasure));
        }

        [Test]
        public void Ruins_YieldADharmaArtifact()
        {
            var s = Session();
            s.Finds.SearchTheRuins();
            Assert.AreEqual(ArtifactClass.DharmaArtifact, s.Artifacts.Armoury.Single().Class);
        }

        [Test]
        public void AYieldingEnemy_IsPlundered_ByItsRank()
        {
            var s = Session();
            var enemy = s.Factions.Factions.First(f => f.HighestRealm == CultivationRealm.Foundation);
            s.Events.TriggerClanWarWon(enemy.Name);
            Assert.AreEqual(CultivationRealm.Foundation, s.Artifacts.Armoury.Single().Rank);
        }

        [Test]
        public void APurpleMansionsDeath_MayLeaveASpiritualTreasureOfItsLineage()
        {
            var s = Session();
            var lineage = s.Context.Content.Fruitions.First(f => f.Abilities.Count > 0);
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion);
            m.FoundationId = $"{lineage.Id}:{lineage.Abilities[0].Id}";
            s.Clan.AddMember(m);
            s.Clan.Kill(m, DeathCause.OldAge);
            var treasure = s.Artifacts.Armoury.Single();
            Assert.AreEqual(ArtifactClass.SpiritualTreasure, treasure.Class);
            Assert.AreEqual(lineage.Id, treasure.Lineage, "its body turns to things of its foundation");
        }

        private static CharacterData PeakFoundation(GameSession s, out string lineage)
        {
            var fruition = s.Context.Content.Fruitions.First(f => f.Abilities.Count > 0);
            lineage = fruition.Id;
            var m = Fixtures.Cultivator(age: 80, realm: CultivationRealm.Foundation, stage: MirrorChronicles.Characters.PowerLadder.StageCount(CultivationRealm.Foundation));
            m.FoundationId = $"{fruition.Id}:{fruition.Abilities[0].Id}";
            s.Clan.AddMember(m);
            return m;
        }

        [Test]
        public void BoundToASpiritualTreasure_AFoundationHasAPurpleMansionsPower_AndGoesNoFurther()
        {
            var s = Session();
            var m = PeakFoundation(s, out var lineage);
            var form = s.Context.Content.ArtifactForms.First().Id;
            var treasure = s.Artifacts.Create(form, CultivationRealm.PurpleMansion, lineage, ArtifactClass.SpiritualTreasure);
            Assert.IsNull(s.Finds.Bind(m.ID, treasure.Id));
            Assert.AreEqual(CultivationRealm.PurpleMansion, m.Realm);
            Assert.AreEqual(2, m.RealmStage, "a treasure of its lineage: the power of two abilities");
            Assert.IsEmpty(m.DivineAbilities, "without holding any");
            Assert.IsTrue(m.ProgressionSealed, "its destiny is the treasure's");
            Assert.IsTrue(m.TreasureBound);
            Assert.IsNotNull(s.Artifacts.Equip(m, s.Artifacts.Create(form, CultivationRealm.QiRefinement, null).Id), "bound for life");
        }

        [Test]
        public void ATreasureOfAnotherLineage_GivesOneAbilitysPower()
        {
            var s = Session();
            var m = PeakFoundation(s, out var lineage);
            var other = s.Context.Content.Fruitions.First(f => f.Id != lineage && f.Abilities.Count > 0).Id;
            var treasure = s.Artifacts.Create(s.Context.Content.ArtifactForms.First().Id, CultivationRealm.PurpleMansion, other, ArtifactClass.SpiritualTreasure);
            s.Finds.Bind(m.ID, treasure.Id);
            Assert.AreEqual(1, m.RealmStage);
        }

        [Test]
        public void OnlyAPeakFoundation_BindsItself_AndOnlyToASpiritualTreasure()
        {
            var s = Session();
            var m = PeakFoundation(s, out var lineage);
            var artifact = s.Artifacts.Create(s.Context.Content.ArtifactForms.First().Id, CultivationRealm.PurpleMansion, lineage);
            StringAssert.Contains("Trésor Spirituel", s.Finds.Bind(m.ID, artifact.Id));
            m.RealmStage = 1;
            var treasure = s.Artifacts.Create(s.Context.Content.ArtifactForms.First().Id, CultivationRealm.PurpleMansion, lineage, ArtifactClass.SpiritualTreasure);
            StringAssert.Contains("sommet", s.Finds.Bind(m.ID, treasure.Id));
        }
    }
}
