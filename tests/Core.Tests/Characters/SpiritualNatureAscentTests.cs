using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The elect's own ascent (R10b; LORE.md §3.1; the user's choices of 2026-10-12): at the Purple Mansion it condenses its
    /// destined Fruition's abilities one after another with its experience alone — no aligned technique nor Qi; with all five
    /// it is a pseudo-Golden Core, weighing as a Golden Core in strength; forging toward its destiny, it takes the position at
    /// once, with no second plea to Heaven.
    /// </summary>
    [TestFixture]
    public class SpiritualNatureAscentTests
    {
        private const string OrthodoxWater = "orthodox-water";

        private static readonly string[] FiveOrthodoxWater =
        {
            "orthodox-water:boundless-sea", "orthodox-water:ford-watcher", "orthodox-water:storm-sky",
            "orthodox-water:dike-guard", "orthodox-water:river-farewell"
        };

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        /// <summary>A Purple Mansion of the Orthodox Water with the given abilities and the realm's XP; elected or not.</summary>
        private static CharacterData Mansion(TestWorld w, int abilities, bool elect)
        {
            var c = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: PowerLadder.PurpleMansionStageFromAbilities(abilities));
            c.DivineAbilities = FiveOrthodoxWater.Take(abilities).ToList();
            c.FoundationId = c.DivineAbilities[0];
            c.CultivationXP = Xp;
            c.SpiritualRoot = 99; // the Threshold rarely stops it
            if (elect)
            {
                c.SubPath = CultivationSubPath.SpiritualNature;
                c.FruitionId = OrthodoxWater;
                c.ForbiddenTasks = new List<TaskType>();
            }
            return w.Join(c);
        }

        // ---- Continuous abilities ----

        [Test]
        public void AnElect_CondensesItsDestinysNextAbility_WithoutAlignedTechniqueOrQi()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var elect = Mansion(w, abilities: 1, elect: true);
            w.Abilities.ProcessBreakthroughPhase();
            CollectionAssert.AreEqual(FiveOrthodoxWater.Take(2), elect.DivineAbilities);
        }

        [Test]
        public void AnotherCultivator_StillNeedsTheAlignedQi()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var other = Mansion(w, abilities: 1, elect: false);
            w.Abilities.ProcessBreakthroughPhase();
            Assert.AreEqual(1, other.DivineAbilities.Count);
        }

        [Test]
        public void AnElect_StillNeedsTheExperience()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var elect = Mansion(w, abilities: 1, elect: true);
            elect.CultivationXP = 0;
            w.Abilities.ProcessBreakthroughPhase();
            Assert.AreEqual(1, elect.DivineAbilities.Count);
        }

        // ---- Pseudo-Golden Core ----

        [Test]
        public void AnElect_WithAllFiveAbilities_WeighsAsAGoldenCore()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            Assert.AreEqual(CultivationRealm.GoldenCore, PowerLadder.EffectiveRealm(Mansion(w, abilities: 5, elect: true)));
        }

        [Test]
        public void AnElect_ShortOfItsFiveAbilities_StaysAPurpleMansion()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            Assert.AreEqual(CultivationRealm.PurpleMansion, PowerLadder.EffectiveRealm(Mansion(w, abilities: 4, elect: true)));
        }

        [Test]
        public void AGrandPerfection_NotElected_StaysAPurpleMansion()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            Assert.AreEqual(CultivationRealm.PurpleMansion, PowerLadder.EffectiveRealm(Mansion(w, abilities: 5, elect: false)));
        }

        [Test]
        public void APseudoGoldenCore_HuntsWithAGoldenCoresPower()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var elect = Mansion(w, abilities: 5, elect: true);
            Assert.AreEqual(HuntRules.Power(CultivationRealm.GoldenCore, elect.RealmStage), HuntRules.Power(elect));
        }

        [Test]
        public void APseudoGoldenCore_ReachesAGoldenCoreFoe()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var elect = Mansion(w, abilities: 5, elect: true);
            Assert.IsTrue(RealmGap.Reaches(new[] { elect }, CultivationRealm.GoldenCore, Fixtures.Content.Balance.RealmGap));
        }

        // ---- Direct ascent ----

        [Test]
        public void AnElect_ForgingTowardItsDestiny_TakesThePositionAtOnce()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var elect = Mansion(w, abilities: 5, elect: true);
            w.Knowledge.Reveal(FactKind.GoldSeeking, OrthodoxWater, KnowledgeSource.Mirror);
            Assert.IsTrue(w.GoldenCore.Forge(elect, OrthodoxWater));
            Assert.AreNotEqual(GoldenCoreState.MetallicEssenceOnly, elect.GoldenCore, "no second plea to Heaven");
        }

        [Test]
        public void AnotherCultivator_ForgesAnEssenceWithoutPosition()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var other = Mansion(w, abilities: 5, elect: false);
            w.Knowledge.Reveal(FactKind.GoldSeeking, OrthodoxWater, KnowledgeSource.Mirror);
            w.GoldenCore.Forge(other, OrthodoxWater);
            Assert.AreEqual(GoldenCoreState.MetallicEssenceOnly, other.GoldenCore);
        }
    }
}
