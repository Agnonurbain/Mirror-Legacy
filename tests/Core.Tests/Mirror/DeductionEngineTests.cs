using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>The mirror deduces techniques from fragments, at a cost in power.</summary>
    [TestFixture]
    public class DeductionEngineTests
    {
        private static TestWorld PoweredWorld()
        {
            var w = new TestWorld();
            w.Mirror.Restore(100, 0);
            return w;
        }

        private static List<FragmentData> Fragments(TestWorld w, params (Element element, int quality)[] specs)
        {
            foreach (var (element, quality) in specs)
                w.Deduction.AddFragment(element, quality, "test");
            return w.Deduction.Fragments.ToList();
        }

        [Test]
        public void AttemptDeduction_NeedsAtLeastTwoFragments()
        {
            var w = PoweredWorld();
            Assert.IsNull(w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 1))));
        }

        [Test]
        public void AttemptDeduction_AcceptsAtMostFiveFragments()
        {
            var w = PoweredWorld();
            var six = Fragments(w, (Element.Fire, 1), (Element.Fire, 1), (Element.Fire, 1), (Element.Fire, 1), (Element.Fire, 1), (Element.Fire, 1));
            Assert.IsNull(w.Deduction.AttemptDeduction(six));
        }

        [Test]
        public void AttemptDeduction_SpendsTenPowerPerFragmentAndTheFragments()
        {
            var w = PoweredWorld();
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 2), (Element.Fire, 2), (Element.Water, 1)));
            Assert.IsTrue(technique != null && w.Mirror.MirrorPower == 70 && w.Deduction.Fragments.Count == 0
                && w.Techniques.Deduced.Contains(technique) && w.Techniques.Knows(technique.ID));
        }

        [Test]
        public void AttemptDeduction_KeepsTheFragments_WhenThePowerIsLacking()
        {
            var w = new TestWorld();
            w.Mirror.Restore(10, 0);
            w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 1), (Element.Wood, 1)));
            Assert.AreEqual(2, w.Deduction.Fragments.Count);
        }

        [Test]
        public void AttemptDeduction_TakesTheMostCommonElement()
        {
            var w = PoweredWorld();
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 1), (Element.Fire, 1), (Element.Water, 1)));
            Assert.AreEqual(Element.Fire, technique.DominantElement);
        }

        [Test]
        public void AttemptDeduction_RisksDeviation_WhenWaterMeetsFire()
        {
            var w = PoweredWorld();
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 1), (Element.Water, 1)));
            Assert.AreEqual(35, technique.RiskFactor);
        }

        [Test]
        public void AttemptDeduction_IsSafer_WhenWoodFeedsFire()
        {
            var w = PoweredWorld();
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 1), (Element.Wood, 1)));
            Assert.AreEqual(0, technique.RiskFactor);
        }

        /// <summary>A world whose every draw is the given sample: 0 yields a cultivation method, 0.99 a weapon art.</summary>
        private static TestWorld Fixed(double sample)
        {
            var w = new TestWorld(new FixedRandom(sample));
            w.Mirror.Restore(100, 0);
            return w;
        }

        [TestCase(new[] { 1, 1 }, 1)]
        [TestCase(new[] { 3, 4, 4, 3 }, 4)]
        [TestCase(new[] { 5, 5, 5, 5, 5 }, 7)]
        public void AttemptDeduction_GradesTheTechniqueFromItsFragments(int[] qualities, int grade)
        {
            var w = PoweredWorld();
            var technique = w.Deduction.AttemptDeduction(Fragments(w, qualities.Select(q => (Element.Fire, q)).ToArray()));
            Assert.AreEqual(grade, technique.Grade);
        }

        [Test]
        public void AttemptDeduction_YieldsASecretTechnique()
        {
            // LORE.md §2.3: a technique rebuilt from pieces, possibly imperfect
            var w = PoweredWorld();
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 2), (Element.Fire, 2)));
            Assert.AreEqual(TechniqueCategory.Secret, technique.Category);
        }

        [Test]
        public void AttemptDeduction_NamesTheTechniqueWithTheWordsOfTheData()
        {
            var w = Fixed(0.99); // a weapon art
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 5), (Element.Fire, 5), (Element.Fire, 5), (Element.Fire, 5)));

            var names = Fixtures.Content.DeductionNames;
            string expected = names.Template.Replace("{kind}", names.Kinds[TechniqueKind.Weapon])
                .Replace("{grade}", names.GradeWords[5]).Replace("{element}", names.Elements[Element.Fire]);
            Assert.AreEqual(expected, technique.Name);
        }

        [Test]
        public void AttemptDeduction_OfAMethod_BuildsItOnAHarvestableQiOfItsElement()
        {
            var w = Fixed(0.0); // a cultivation method
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Water, 3), (Element.Water, 3)));

            var qi = Fixtures.Content.Qi.Single(q => q.Id == technique.RequiredQiId);
            Assert.IsTrue(technique.Kind == TechniqueKind.Cultivation && technique.RequiredRealm == CultivationRealm.QiRefinement);
            Assert.IsTrue(qi.Element == Element.Water && !qi.Vanished && !qi.Ubiquitous);
        }

        [Test]
        public void AttemptDeduction_OfAnArt_NeedsNoQiAndOpensAtTheRealmOfItsGrade()
        {
            var w = Fixed(0.99); // a weapon art
            var technique = w.Deduction.AttemptDeduction(Fragments(w, (Element.Fire, 5), (Element.Fire, 5), (Element.Fire, 5), (Element.Fire, 5)));

            Assert.IsTrue(technique.Kind == TechniqueKind.Weapon && technique.Effect == TechniqueEffect.Strike && technique.RequiredQiId == null);
            Assert.AreEqual(CultivationRealm.Foundation, technique.RequiredRealm); // grade 6
        }

        [Test]
        public void AttemptDeduction_YieldsNoMethod_WhenNoQiCanBeHarvested()
        {
            // A method without a harvestable Qi could never lead anyone into Qi Cultivation
            var content = Fixtures.Content with { Qi = Fixtures.Content.Qi.Where(q => q.Vanished || q.Ubiquitous).ToList() };
            var ctx = new GameContext(new GameEventBus(), new RecordingGameLog(), new FixedRandom(0.0), new GameClock(), content);
            var clan = new ClanManager(ctx, "Mo");
            var library = new TechniqueLibrary(ctx);
            var cultivation = new CultivationSystem(ctx, new ClanKarmaSystem(ctx, clan), library, new ResourceManager(ctx));
            var mirror = new MirrorSystem(ctx, clan, new BreakthroughSystem(ctx, clan, cultivation));
            var deduction = new DeductionEngine(ctx, mirror, library);
            deduction.AddFragment(Element.Water, 3);
            deduction.AddFragment(Element.Water, 3);

            var technique = deduction.AttemptDeduction(deduction.Fragments.ToList()); // the first draw would pick a method

            Assert.AreNotEqual(TechniqueKind.Cultivation, technique.Kind);
        }

        [Test]
        public void AttemptDeduction_RequiresAnExistingRealm_EvenFromDivineFragments()
        {
            var w = PoweredWorld();
            var divine = Fragments(w, (Element.Light, 5), (Element.Light, 5), (Element.Light, 5), (Element.Light, 5), (Element.Light, 5));
            var technique = w.Deduction.AttemptDeduction(divine);
            Assert.IsTrue(Enum.IsDefined(typeof(CultivationRealm), technique.RequiredRealm));
        }
    }
}
