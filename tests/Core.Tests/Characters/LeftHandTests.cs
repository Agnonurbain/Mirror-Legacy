using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Left Hand paths (LORE.md §6.9, §5.9 R18-R19): the power of a Golden Core without a position. The
    /// true ones are autonomous (the Radiant Visage Immortals of the Supreme Yang, the Woven Jade Immortals of
    /// the Lesser Yin); the false ones depend on a superior and fall with them.
    /// </summary>
    [TestFixture]
    public class LeftHandTests
    {
        private const double Pass = 0.0;
        private const double Fail = 0.995;
        private const string LesserYin = "lesser-yin";
        private const string MutableWater = "mutable-water";

        private static readonly string[] FiveLesserYin =
        {
            "lesser-yin:cold-drought-monarch-fire", "lesser-yin:engulfed-fragrance", "lesser-yin:unrevealed-3",
            "lesser-yin:unrevealed-4", "lesser-yin:unrevealed-5"
        };

        private static readonly string[] FiveOrthodoxWater =
        {
            "orthodox-water:boundless-sea", "orthodox-water:ford-watcher", "orthodox-water:storm-sky",
            "orthodox-water:dike-guard", "orthodox-water:river-farewell"
        };

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        private static GoldenCoreSettings Settings => Fixtures.Content.Balance.GoldenCore;

        private static CharacterData Master(TestWorld w, params string[] abilities)
        {
            var c = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion, stage: 4);
            c.DivineAbilities = new List<string>(abilities);
            c.FoundationId = abilities[0];
            c.RealmStage = PowerLadder.PurpleMansionStageFromAbilities(abilities.Length);
            c.CultivationXP = Xp;
            return w.Join(c);
        }

        /// <summary>A Grand Perfection of the Lesser Yin whose clan knows the Woven Jade path.</summary>
        private static CharacterData WovenJadeAdept(TestWorld w)
        {
            var c = Master(w, FiveLesserYin);
            w.Knowledge.Reveal(FactKind.LeftHand, LesserYin, KnowledgeSource.Mirror);
            return c;
        }

        /// <summary>Tan Qing, holder of the Mutable Water, has granted the clan leave.</summary>
        private static void PatronAgrees(TestWorld w)
        {
            w.Resources.AddSpiritStones(Settings.PermissionStones);
            Assert.IsTrue(w.GoldenCore.RequestPermission(MutableWater));
        }

        // ---- The data ----

        [Test]
        public void ShippedFruitions_NameTheTwoTrueLeftHandPaths()
        {
            var paths = Fixtures.Content.Fruitions.Where(f => f.LeftHand != null).ToDictionary(f => f.Id, f => f.LeftHand);
            CollectionAssert.AreEquivalent(new Dictionary<string, string>
            {
                ["supreme-yang"] = "Immortels au Visage Radieux",
                ["lesser-yin"] = "Immortels de Jade Tressé"
            }, paths);
        }

        // ---- The true Left Hand: autonomous, without position ----

        [Test]
        public void ForgeTrueLeftHand_GivesAGoldenCoresPowerWithoutPosition()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = WovenJadeAdept(w);
            var before = w.Fruitions.State(LesserYin);

            Assert.IsTrue(w.GoldenCore.ForgeTrueLeftHand(c));

            Assert.AreEqual(CultivationRealm.GoldenCore, c.Realm);
            Assert.AreEqual(GoldenCoreState.TrueLeftHand, c.GoldenCore);
            Assert.AreEqual(LesserYin, c.FruitionId);
            Assert.AreEqual(0, c.CultivationXP);
            Assert.AreEqual(PowerLadder.MaxLifespan(CultivationRealm.GoldenCore, 1), c.MaxLifespan);
            Assert.AreEqual(before, w.Fruitions.State(LesserYin)); // no authority over the lineage
        }

        [Test]
        public void ForgeTrueLeftHand_Refuses_WithoutKnowingThePath()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveLesserYin);
            Assert.IsFalse(w.GoldenCore.ForgeTrueLeftHand(c));
        }

        [Test]
        public void ForgeTrueLeftHand_Refuses_ALineageWithoutLeftHand()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater);
            w.Knowledge.Reveal(FactKind.LeftHand, "orthodox-water", KnowledgeSource.Mirror);
            Assert.IsFalse(w.GoldenCore.ForgeTrueLeftHand(c));
        }

        [Test]
        public void ForgeTrueLeftHand_Refuses_BeforeTheGrandPerfectionOfTheLineage()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var mixed = Master(w, FiveLesserYin.Take(4).Append("orthodox-water:boundless-sea").ToArray());
            var four = Master(w, FiveLesserYin.Take(4).ToArray());
            w.Knowledge.Reveal(FactKind.LeftHand, LesserYin, KnowledgeSource.Mirror);

            Assert.IsFalse(w.GoldenCore.ForgeTrueLeftHand(mixed));
            Assert.IsFalse(w.GoldenCore.ForgeTrueLeftHand(four));
        }

        [Test]
        public void ForgeTrueLeftHand_Failure_GivesLifeToAMetalEssenceDemon()
        {
            var w = new TestWorld(new FixedRandom(Fail));
            var c = WovenJadeAdept(w);

            Assert.IsTrue(w.GoldenCore.ForgeTrueLeftHand(c));

            Assert.AreEqual(DeathCause.MetalEssenceDemon, c.CauseOfDeath);
        }

        [Test]
        public void DecipherLeftHand_RevealsThePath_OnlyForALineageThatHasOne()
        {
            var w = new TestWorld();
            int before = w.Mirror.MirrorPower;

            Assert.IsFalse(w.GoldenCore.DecipherLeftHand("orthodox-water"));
            Assert.IsTrue(w.GoldenCore.DecipherLeftHand(LesserYin));

            Assert.IsTrue(w.Knowledge.Knows(FactKind.LeftHand, LesserYin));
            Assert.AreEqual(before - Settings.LeftHandMirrorCost, w.Mirror.MirrorPower);
        }

        // ---- The false Left Hand: a patron's borrowed power ----

        [Test]
        public void BindToPatron_Refuses_WithoutTheHoldersLeave()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater);
            Assert.IsFalse(w.GoldenCore.BindToPatron(c, MutableWater));
            Assert.AreEqual(CultivationRealm.PurpleMansion, c.Realm);
        }

        [Test]
        public void BindToPatron_MakesAFalseLeftHand_DependingOnTheHolder()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater.Take(Settings.FalseLeftHandMinAbilities).ToArray());
            PatronAgrees(w);

            Assert.IsTrue(w.GoldenCore.BindToPatron(c, MutableWater));

            Assert.AreEqual(CultivationRealm.GoldenCore, c.Realm);
            Assert.AreEqual(GoldenCoreState.FalseLeftHand, c.GoldenCore);
            Assert.AreEqual(MutableWater, c.FruitionId);
            Assert.AreEqual("Tan Qing", c.PatronId);
        }

        [Test]
        public void BindToPatron_Refuses_BelowTheAbilitiesRequired()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater.Take(Settings.FalseLeftHandMinAbilities - 1).ToArray());
            PatronAgrees(w);
            Assert.IsFalse(w.GoldenCore.BindToPatron(c, MutableWater));
        }

        [Test]
        public void BindToPatron_Failure_SpendsTheExperience_ButForgesNoEssence()
        {
            var w = new TestWorld(new SequenceRandom(Pass, Fail)); // the leave granted, the binding failed
            var c = Master(w, FiveOrthodoxWater);
            PatronAgrees(w);

            Assert.IsTrue(w.GoldenCore.BindToPatron(c, MutableWater));

            Assert.IsTrue(c.IsAlive);
            Assert.AreEqual(CultivationRealm.PurpleMansion, c.Realm);
            Assert.AreEqual(0, c.CultivationXP);
        }

        [Test]
        public void AFalseLeftHand_PaysItsPatronEachYear()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater);
            PatronAgrees(w);
            w.GoldenCore.BindToPatron(c, MutableWater);
            w.Resources.AddSpiritStones(Settings.FalseLeftHandYearlyStones);
            int before = w.Resources.SpiritStones;

            w.GoldenCore.ProcessBreakthroughPhase();

            Assert.AreEqual(GoldenCoreState.FalseLeftHand, c.GoldenCore);
            Assert.AreEqual(before - Settings.FalseLeftHandYearlyStones, w.Resources.SpiritStones);
        }

        [Test]
        public void AFalseLeftHand_FallsWhenItsPatronIsGone()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater);
            PatronAgrees(w);
            w.GoldenCore.BindToPatron(c, MutableWater);
            w.Resources.AddSpiritStones(Settings.FalseLeftHandYearlyStones);
            w.Fruitions.ChangeHolder(MutableWater, "Nouveau détenteur");

            w.GoldenCore.ProcessBreakthroughPhase();

            AssertFallen(c);
        }

        [Test]
        public void AFalseLeftHand_FallsWhenTheTributeIsNotPaid()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater);
            PatronAgrees(w);
            w.GoldenCore.BindToPatron(c, MutableWater);
            w.Resources.ConsumeSpiritStones(w.Resources.SpiritStones);

            w.GoldenCore.ProcessBreakthroughPhase();

            AssertFallen(c);
        }

        /// <summary>Back to the Purple Mansion, abilities kept, the borrowed years gone.</summary>
        private static void AssertFallen(CharacterData c)
        {
            Assert.IsTrue(c.IsAlive);
            Assert.AreEqual(CultivationRealm.PurpleMansion, c.Realm);
            Assert.AreEqual(GoldenCoreState.None, c.GoldenCore);
            Assert.IsNull(c.PatronId);
            Assert.AreEqual(5, c.DivineAbilities.Count);
            Assert.AreEqual(PowerLadder.PurpleMansionStageFromAbilities(5), c.RealmStage);
            Assert.LessOrEqual(c.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.PurpleMansion, c.RealmStage));
        }

        // ---- Borrowing a Fruition's light (LORE.md §5.4.2): a « Merciful » Purple Mansion lent by a True Monarch ----

        private static CharacterData FoundationPeak(TestWorld w)
        {
            var c = Fixtures.Cultivator(age: 120, realm: CultivationRealm.Foundation, stage: 4);
            c.FoundationId = "orthodox-water:boundless-sea";
            return w.Join(c);
        }

        [Test]
        public void BorrowLight_LendsAPurpleMansionsPower_WithoutAbilities()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = FoundationPeak(w);
            PatronAgrees(w);

            Assert.IsTrue(w.GoldenCore.BorrowLight(c, MutableWater));

            Assert.AreEqual(CultivationRealm.PurpleMansion, c.Realm);
            Assert.IsTrue(c.BorrowedLight);
            Assert.AreEqual("Tan Qing", c.PatronId);
            Assert.AreEqual(0, c.DivineAbilities.Count);
            Assert.IsFalse(w.Abilities.Pursue(c, "orthodox-water:river-farewell"), "a borrowed light condenses nothing of its own");
        }

        [Test]
        public void BorrowLight_Refuses_WithoutTheLendersLeave_OrBeforeTheFoundationsPeak()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = FoundationPeak(w);
            Assert.IsFalse(w.GoldenCore.BorrowLight(c, MutableWater));

            PatronAgrees(w);
            c.RealmStage = 3;
            Assert.IsFalse(w.GoldenCore.BorrowLight(c, MutableWater));
        }

        [Test]
        public void ABorrowedLight_GoesOutWithItsLender()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = FoundationPeak(w);
            PatronAgrees(w);
            w.GoldenCore.BorrowLight(c, MutableWater);
            w.Resources.AddSpiritStones(Settings.LightBorrowingYearlyStones);
            w.Fruitions.ChangeHolder(MutableWater, "Nouveau détenteur");

            w.GoldenCore.ProcessBreakthroughPhase();

            Assert.AreEqual(CultivationRealm.Foundation, c.Realm);
            Assert.AreEqual(4, c.RealmStage);
            Assert.IsFalse(c.BorrowedLight);
            Assert.IsNull(c.PatronId);
            Assert.LessOrEqual(c.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.Foundation, 4));
        }
    }
}
