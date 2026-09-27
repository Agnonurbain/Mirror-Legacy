using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// Who knows the mirror exists (user decision, 2026-09-27): an artefact of the very highest level, known only to a
    /// handful of very high-level beings — not all of them: it depends on their realm and their seniority, the old
    /// knowing more secrets than the young; the same goes for the Dao Embryos. Below the Golden Core, nobody knows. Who
    /// knows is hidden and fixed by the world (its seed); knowledge only grows with the years.
    /// </summary>
    [TestFixture]
    public class MirrorLoreTests
    {
        private const string Peak = "Secte du Pic des Nuées";   // a Golden Core power, its elder unnamed
        private const string PaleMoon = "Secte de la Lune Pâle"; // the Venerable Lingxu, a named Golden Core
        private const string Ruan = "Famille Ruan";               // a Purple Mansion family

        private static MirrorLoreSettings Settings => Fixtures.Content.Balance.MirrorLore;

        /// <summary>The shipped content where every Golden Core and above knows (to test who, not whether).</summary>
        public static GameContent EveryElderKnows => Fixtures.Content with
        {
            Balance = Fixtures.Content.Balance with
            {
                MirrorLore = Fixtures.Content.Balance.MirrorLore with
                {
                    KnowChance = Settings.KnowChance.ToDictionary(p => p.Key, _ => 1.0), MaxChance = 1.0
                }
            }
        };

        private static MirrorLore Lore(GameContent content = null, int seed = 1)
        {
            var w = new TestWorld(new System.Random(seed), content);
            w.Factions.InitializeFactions();
            return new MirrorLore(w.Ctx, w.Factions, seed);
        }

        [Test]
        public void BelowTheGoldenCore_NobodyKnows()
        {
            Assert.AreEqual(0, MirrorLoreRules.KnowChance(CultivationRealm.PurpleMansion, 1000, Settings));
            Assert.IsFalse(Lore(EveryElderKnows).Knows(Ruan));
        }

        [Test]
        public void TheOldKnowMore_AndTheDaoEmbryosMore_ThanTheGoldenCores()
        {
            Assert.Greater(MirrorLoreRules.KnowChance(CultivationRealm.GoldenCore, 500, Settings),
                MirrorLoreRules.KnowChance(CultivationRealm.GoldenCore, 100, Settings));
            Assert.Greater(MirrorLoreRules.KnowChance(CultivationRealm.DaoEmbryo, 100, Settings),
                MirrorLoreRules.KnowChance(CultivationRealm.GoldenCore, 100, Settings));
            Assert.LessOrEqual(MirrorLoreRules.KnowChance(CultivationRealm.GoldenImmortal, 100_000, Settings), Settings.MaxChance);
        }

        [Test]
        public void NotEveryoneOfThatLevel_Knows_AndTheWorldDecides()
        {
            var answers = Enumerable.Range(1, 60).Select(seed => Lore(seed: seed).Knows(Peak)).ToList();
            Assert.IsTrue(answers.Contains(true) && answers.Contains(false), "some worlds, not all");
            Assert.AreEqual(Lore(seed: 7).Knows(Peak), Lore(seed: 7).Knows(Peak), "a world keeps its answer");
        }

        [Test]
        public void TheKnower_IsTheNamedElder_OrAnUnnamedOne()
        {
            var lore = Lore(EveryElderKnows);
            Assert.AreEqual("Vénérable Lingxu", lore.KnowerOf(PaleMoon));
            StringAssert.Contains(Peak, lore.KnowerOf(Peak));
            Assert.IsNull(lore.KnowerOf(Ruan));
        }

        [Test]
        public void AFigureNotYetBorn_KnowsNothing()
        {
            var unborn = new FigureDefinition { Id = "x", Name = "X", Realm = CultivationRealm.GoldenCore, BornYear = 50 };
            Assert.AreEqual(0, MirrorLoreRules.FigureChance(unborn, year: 1, Settings));
        }
    }
}
