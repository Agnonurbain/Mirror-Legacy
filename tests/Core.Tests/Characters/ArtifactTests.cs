using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The artifacts by realm (L4f, user decisions 2026-10-03; 📚 the wiki's classes): Dharma Artifacts for the Qi
    /// Refinement and the Foundation, Spiritual Artifacts and the rarer Spiritual Treasures for the Purple Mansion. Each has
    /// a rank and often a lineage; a bearer of its lineage draws more from it; nobody wields one above its own realm. A
    /// weapon adds to its bearer's strength, a guard foils ambushes and harvests, a cultivation artifact of its lineage
    /// speeds its bearer. One borne by each member; the others lie in the clan's armoury, where a dead bearer's returns.
    /// </summary>
    [TestFixture]
    public class ArtifactTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static string Form(GameSession s, ArtifactEffect effect) => s.Context.Content.ArtifactForms.First(f => f.Effect == effect).Id;

        private static string LineageOf(CharacterData m) => FoundationRef.Parse(m.FoundationId).FruitionId;

        private static CharacterData Foundation(GameSession s)
        {
            var m = Fixtures.Cultivator(realm: CultivationRealm.Foundation);
            var lineage = s.Context.Content.Fruitions.First(f => f.Abilities.Count > 0);
            m.FoundationId = $"{lineage.Id}:{lineage.Abilities[0].Id}";
            s.Clan.AddMember(m);
            return m;
        }

        [Test]
        public void AWeapon_AddsToItsBearersStrength_MoreForItsLineage()
        {
            var s = Session();
            var m = Foundation(s);
            int bare = HuntRules.Power(m);
            var plain = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.Foundation, null);
            Assert.IsNull(s.Artifacts.Equip(m, plain.Id));
            int withPlain = HuntRules.Power(m);
            Assert.Greater(withPlain, bare);
            var kin = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.Foundation, LineageOf(m));
            Assert.IsNull(s.Artifacts.Equip(m, kin.Id));
            Assert.Greater(HuntRules.Power(m), withPlain, "a bearer of its lineage draws more from it");
            Assert.IsTrue(s.Artifacts.Armoury.Any(a => a.Id == plain.Id), "one borne at a time: the other goes back to the armoury");
        }

        [Test]
        public void NobodyWieldsAnArtifactAboveItsRealm()
        {
            var s = Session();
            var m = Foundation(s);
            var high = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.PurpleMansion, null);
            StringAssert.Contains("royaume", s.Artifacts.Equip(m, high.Id));
        }

        [Test]
        public void AGuard_FoilsAmbushesAndHarvests()
        {
            var s = Session();
            var m = Foundation(s);
            var guard = s.Artifacts.Create(Form(s, ArtifactEffect.Protection), CultivationRealm.Foundation, null);
            s.Artifacts.Equip(m, guard.Id);
            Assert.Greater(ArtifactRules.Protection(m), 0);
        }

        [Test]
        public void ACultivationArtifactOfItsLineage_SpeedsItsBearer()
        {
            var s = Session();
            var m = Fixtures.Cultivator();
            s.Clan.AddMember(m);
            double bare = s.Cultivation.SpeedOf(m);
            var qi = s.Techniques.FindQi(s.Techniques.MethodOf(m)?.RequiredQiId);
            m.FoundationId = qi.Foundation; // the lineage of its Qi
            double aligned = s.Cultivation.SpeedOf(m);
            var aid = s.Artifacts.Create(Form(s, ArtifactEffect.Cultivation), CultivationRealm.QiRefinement, LineageOf(m));
            s.Artifacts.Equip(m, aid.Id);
            Assert.Greater(s.Cultivation.SpeedOf(m), aligned);
            var other = s.Context.Content.Fruitions.First(f => f.Id != LineageOf(m) && f.Abilities.Count > 0).Id;
            var foreign = s.Artifacts.Create(Form(s, ArtifactEffect.Cultivation), CultivationRealm.QiRefinement, other);
            s.Artifacts.Equip(m, foreign.Id);
            Assert.AreEqual(aligned, s.Cultivation.SpeedOf(m), 1e-9, "only an artifact of its lineage helps it cultivate");
        }

        [Test]
        public void ADeadBearersArtifact_ReturnsToTheArmoury()
        {
            var s = Session();
            var m = Foundation(s);
            var sword = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.Foundation, null);
            s.Artifacts.Equip(m, sword.Id);
            Assert.IsFalse(s.Artifacts.Armoury.Any(a => a.Id == sword.Id));
            s.Clan.Kill(m, DeathCause.OldAge);
            Assert.IsTrue(s.Artifacts.Armoury.Any(a => a.Id == sword.Id));
            Assert.IsNull(m.Artifact);
        }

        [Test]
        public void TheClasses_FollowTheRanks()
        {
            var s = Session();
            Assert.AreEqual(ArtifactClass.DharmaArtifact, s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.QiRefinement, null).Class);
            Assert.AreEqual(ArtifactClass.DharmaArtifact, s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.Foundation, null).Class);
            Assert.AreEqual(ArtifactClass.SpiritualArtifact, s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.PurpleMansion, null).Class);
            var treasure = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.PurpleMansion, null, ArtifactClass.SpiritualTreasure);
            var artifact = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.PurpleMansion, null);
            Assert.Greater(treasure.Strength, artifact.Strength, "a Spiritual Treasure, rarer, is stronger");
        }

        [Test]
        public void TheArmoury_AndTheBorneArtifacts_SurviveASave()
        {
            var s = Session();
            var m = Foundation(s);
            var sword = s.Artifacts.Create(Form(s, ArtifactEffect.Combat), CultivationRealm.Foundation, null);
            s.Artifacts.Equip(m, sword.Id);
            s.Artifacts.Create(Form(s, ArtifactEffect.Protection), CultivationRealm.QiRefinement, null);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            Assert.AreEqual(s.Artifacts.Armoury.Count, reloaded.Artifacts.Armoury.Count);
            Assert.AreEqual(HuntRules.Power(m), HuntRules.Power(reloaded.Clan.FindById(m.ID)));
        }
    }
}
