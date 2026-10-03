using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The phenomena (LORE.md §5.3.5, §5.4.5; L4c, 2026-10-03): a cultivator of the Foundation or above who dies leaves
    /// over the region an unusual weather tied to its foundation, and its body turns to spiritual things; while it lasts,
    /// those of the same lineage cultivate faster there. A Purple Mansion who fails the Golden Core leaves a lasting
    /// celestial phenomenon that slows every cultivator of the region (2.5 % in the lore). Exhausted before the Shenyang
    /// Mansion, one leaves nothing: « grandiose but fleeting, without spiritual trace ».
    /// </summary>
    [TestFixture]
    public class PhenomenaTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static string Home(GameSession s) => s.Context.Content.Clan.HomeRegion;

        /// <summary>A Qi Refinement cultivator of the clan's method, and the lineage of its Qi.</summary>
        private static (CharacterData Member, string Lineage) Cultivator(GameSession s)
        {
            var m = Fixtures.Cultivator();
            s.Clan.AddMember(m);
            var qi = s.Techniques.FindQi(s.Techniques.MethodOf(m)?.RequiredQiId);
            return (m, FoundationRef.Parse(qi.Foundation).FruitionId);
        }

        private static string OtherLineage(GameSession s, string lineage) =>
            s.Context.Content.Fruitions.First(f => f.Id != lineage && f.Abilities.Count > 0).Id;

        private static CharacterData Dying(GameSession s, CultivationRealm realm, string lineage)
        {
            var dead = Fixtures.Cultivator(age: 200, realm: realm);
            var ability = s.Context.Content.Fruitions.First(f => f.Id == lineage).Abilities.FirstOrDefault();
            dead.FoundationId = ability == null ? $"{lineage}:x" : $"{lineage}:{ability.Id}";
            s.Clan.AddMember(dead);
            return dead;
        }

        private static void Years(GameSession s, int years)
        {
            for (int i = 0; i < years; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Phenomena.ProcessYear();
            }
        }

        [Test]
        public void AFoundationsDeath_FavoursItsLineage_AtHome_ForSomeYears()
        {
            var s = Session();
            var (member, lineage) = Cultivator(s);
            double before = s.Place.SpeedFactor(member);
            s.Clan.Kill(Dying(s, CultivationRealm.Foundation, lineage), DeathCause.OldAge);
            Assert.Greater(s.Place.SpeedFactor(member), before, "the weather of its foundation favours its kin");
            Years(s, s.Context.Content.Balance.Phenomena.FoundationDeathYears + 1);
            Assert.AreEqual(before, s.Place.SpeedFactor(member), 1e-9, "the weather passes");
        }

        [Test]
        public void ADeath_LeavesSpiritualThings()
        {
            var s = Session();
            var (_, lineage) = Cultivator(s);
            int before = s.Resources.MedicinalHerbs + s.Resources.SpiritualOres;
            s.Clan.Kill(Dying(s, CultivationRealm.PurpleMansion, lineage), DeathCause.OldAge);
            Assert.Greater(s.Resources.MedicinalHerbs + s.Resources.SpiritualOres, before, "the body turns to spiritual things");
        }

        [Test]
        public void AnotherLineage_IsNotFavoured()
        {
            var s = Session();
            var (member, lineage) = Cultivator(s);
            double before = s.Place.SpeedFactor(member);
            s.Clan.Kill(Dying(s, CultivationRealm.Foundation, OtherLineage(s, lineage)), DeathCause.OldAge);
            Assert.AreEqual(before, s.Place.SpeedFactor(member), 1e-9);
        }

        [Test]
        public void ExhaustedBeforeShenyang_LeavesNoTrace()
        {
            var s = Session();
            var (_, lineage) = Cultivator(s);
            s.Clan.Kill(Dying(s, CultivationRealm.Foundation, lineage), DeathCause.AscentCollapse);
            Assert.IsEmpty(s.Phenomena.Active);
        }

        [Test]
        public void AFailedGoldenCore_SlowsEveryCultivatorOfTheRegion_ForLong()
        {
            var s = Session();
            var (member, lineage) = Cultivator(s);
            double before = s.Place.SpeedFactor(member);
            s.Clan.Kill(Dying(s, CultivationRealm.PurpleMansion, OtherLineage(s, lineage)), DeathCause.MetalEssenceDemon);
            Assert.AreEqual(before * (1 + s.Context.Content.Balance.Phenomena.FailureSpeed), s.Place.SpeedFactor(member), 1e-9);
            Years(s, 100);
            Assert.Less(s.Place.SpeedFactor(member), before, "a lasting celestial phenomenon");
        }

        [Test]
        public void APowersElderFailing_FarAway_LeavesTheClanUntouched_ButNearItDoesNot()
        {
            var s = Session();
            var (member, _) = Cultivator(s);
            double before = s.Place.SpeedFactor(member);
            var far = s.Factions.Factions.First(f => f.RegionId != Home(s));
            var elder = new FactionElder { Id = "e", Name = "e", Realm = CultivationRealm.PurpleMansion, Stage = 4, MaxLifespan = 500 };
            s.Events.TriggerElderDied(far, elder, true);
            Assert.AreEqual(before, s.Place.SpeedFactor(member), 1e-9);
            Assert.IsTrue(s.Phenomena.Active.Any(p => p.RegionId == far.RegionId), "the world keeps its own weather");
            var near = s.Factions.Factions.First(f => f.RegionId == Home(s));
            s.Events.TriggerElderDied(near, elder, true);
            Assert.Less(s.Place.SpeedFactor(member), before);
        }

        [Test]
        public void ManyDeaths_DoNotPileUpWithoutEnd()
        {
            var s = Session();
            var (member, lineage) = Cultivator(s);
            double before = s.Place.SpeedFactor(member);
            for (int i = 0; i < 40; i++) s.Clan.Kill(Dying(s, CultivationRealm.Foundation, lineage), DeathCause.OldAge);
            var settings = s.Context.Content.Balance.Phenomena;
            Assert.AreEqual(before * (1 + settings.MaxAlignedSpeed), s.Place.SpeedFactor(member), 1e-9);
            for (int i = 0; i < 40; i++) s.Clan.Kill(Dying(s, CultivationRealm.PurpleMansion, OtherLineage(s, lineage)), DeathCause.MetalEssenceDemon);
            Assert.AreEqual(before * (1 + settings.MaxAlignedSpeed + settings.MinGeneralSpeed), s.Place.SpeedFactor(member), 1e-9);
        }

        [Test]
        public void Phenomena_SurviveASave_AndTheChronicleTellsThem()
        {
            var s = Session();
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            var (member, lineage) = Cultivator(s);
            s.Clan.Kill(Dying(s, CultivationRealm.Foundation, lineage), DeathCause.OldAge);
            Assert.IsTrue(chronicle.Entries.Any(e => e.Contains("phénomène")));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            Assert.AreEqual(s.Phenomena.Active.Count, reloaded.Phenomena.Active.Count);
            Assert.AreEqual(s.Place.SpeedFactor(member), reloaded.Place.SpeedFactor(reloaded.Clan.FindById(member.ID)), 1e-9);
        }
    }
}
