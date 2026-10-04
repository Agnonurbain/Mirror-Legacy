using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The second foundation and the prolonged life (AUDIT_LORE.md §2.10; 📚 wiki Li_Xuanfeng: a Geng Metal pill, a new immortal
    /// foundation, a younger body; the patron « swallows human pills to prolong his life »): a Foundation re-forms its foundation
    /// on another Qi with the Recasting Pill — younger, or its path sealed; a prisoner is refined into a human pill — years of
    /// life, at the price of a Heart Demon and of its power's grudge; the world's old elders do the same.
    /// </summary>
    [TestFixture]
    public class LongevityPillTests
    {
        private static LongevitySettings L => Fixtures.Content.Balance.Arts.Longevity;

        private static GameSession Session(GameContent content = null)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Fixtures.QuietContent });
            s.Resources.AddHerbs(10_000);
            s.Resources.AddSpiritStones(100_000);
            return s;
        }

        private static GameContent With(System.Func<LongevitySettings, LongevitySettings> tweak)
        {
            var c = Fixtures.QuietContent;
            return c with { Balance = c.Balance with { Arts = c.Balance.Arts with { Longevity = tweak(c.Balance.Arts.Longevity) } } };
        }

        private static CharacterData AFoundation(GameSession s)
        {
            var m = Fixtures.Cultivator(age: 80, realm: CultivationRealm.Foundation, stage: 2);
            m.FoundationId = s.Context.Content.Qi.First(q => q.Id == m.QiId).Foundation;
            m.MaxLifespan = 120;
            s.Clan.AddMember(m);
            return m;
        }

        /// <summary>Another Qi with a foundation, in the clan's store.</summary>
        private static QiDefinition AnotherQi(GameSession s, CharacterData m)
        {
            var qi = s.Context.Content.Qi.First(q => q.Foundation != null && !q.Vanished && q.Foundation != m.FoundationId);
            s.Resources.AddQi(qi.Id, L.RecastQiPortions);
            return qi;
        }

        [Test]
        public void TheRecastingPill_GivesASecondFoundation_AndAYoungerBody()
        {
            var s = Session(With(l => l with { RecastSuccess = 1 }));
            var m = AFoundation(s);
            var qi = AnotherQi(s, m);
            StringAssert.Contains("Refonte", s.Alchemy.RecastRefusal(m, qi.Id));
            s.Alchemy.GainPills(PillKind.FoundationRecasting, 1);
            Assert.IsNull(s.Alchemy.RecastFoundation(m.ID, qi.Id));
            Assert.AreEqual(qi.Foundation, m.FoundationId);
            Assert.AreEqual(120 + L.RecastYears, m.MaxLifespan);
            Assert.AreEqual(0, s.Resources.QiPortions(qi.Id));
            Assert.AreEqual(0, s.Alchemy.PillsOf(PillKind.FoundationRecasting));
        }

        [Test]
        public void AFailedRecasting_SealsThePath()
        {
            var s = Session(With(l => l with { RecastSuccess = 0 }));
            var m = AFoundation(s);
            var before = m.FoundationId;
            var qi = AnotherQi(s, m);
            s.Alchemy.GainPills(PillKind.FoundationRecasting, 1);
            Assert.IsNull(s.Alchemy.RecastFoundation(m.ID, qi.Id));
            Assert.AreEqual(before, m.FoundationId);
            Assert.IsTrue(m.ProgressionSealed);
        }

        [Test]
        public void APrisoner_IsRefined_IntoYearsOfLife_ForAHeartDemon()
        {
            var s = Session(With(l => l with { HeartDemonChance = 1, DiscoveryChance = 1 }));
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var alchemist = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            alchemist.TalismanQiId = "holding-profit";
            alchemist.ArtMastery[ImmortalArt.Alchemy] = s.Context.Content.Balance.Arts.AdeptAt;
            s.Clan.AddMember(alchemist);
            var elder = AFoundation(s);
            var power = s.Factions.Factions.First();
            int relation = power.RelationWithPlayer;
            s.Captives.Imprison(new Prisoner("p1", power.Name, CultivationRealm.Foundation, s.Clock.Year));

            Assert.IsNull(s.Alchemy.HumanPill(alchemist.ID, "p1", elder.ID));
            Assert.AreEqual(120 + L.YearsByRealm[CultivationRealm.Foundation], elder.MaxLifespan);
            Assert.IsEmpty(s.Captives.Prisoners);
            Assert.Greater(elder.HeartDemonYearsLeft, 0, "a demonic practice breeds a Heart Demon");
            Assert.Less(power.RelationWithPlayer, relation, "its power learns of it");
            Assert.AreEqual(1, elder.HumanPillsTaken);
        }

        [Test]
        public void EachHumanPill_GivesLess()
        {
            var s = Session(With(l => l with { HeartDemonChance = 0, DiscoveryChance = 0 }));
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var alchemist = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            alchemist.TalismanQiId = "holding-profit";
            alchemist.ArtMastery[ImmortalArt.Alchemy] = s.Context.Content.Balance.Arts.AdeptAt;
            s.Clan.AddMember(alchemist);
            var elder = AFoundation(s);
            elder.HumanPillsTaken = 1;
            s.Captives.Imprison(new Prisoner("p1", s.Factions.Factions.First().Name, CultivationRealm.Foundation, s.Clock.Year));
            Assert.IsNull(s.Alchemy.HumanPill(alchemist.ID, "p1", elder.ID));
            Assert.AreEqual(120 + L.YearsByRealm[CultivationRealm.Foundation] / 2, elder.MaxLifespan);
        }

        [Test]
        public void TheWorldsOldElders_SwallowHumanPills_TooAtTheirDisciplesCost()
        {
            var s = Session(With(l => l with { ElderPillChance = 1 }));
            var power = s.Factions.Factions.First(p => PowerArts.Knows(p, ImmortalArt.Alchemy, s.Context.Content));
            var elder = power.Elders.First();
            elder.BornYear = s.Clock.Year - elder.MaxLifespan + 5; // its last years
            int lifespan = elder.MaxLifespan, disciples = power.PowerLevel;
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.Elders.ProcessYear();
            Assert.Greater(elder.MaxLifespan, lifespan);
            Assert.AreEqual(1, elder.HumanPillsTaken);
            Assert.Less(power.PowerLevel, disciples, "its disciples went into the cauldron");
        }
    }
}
