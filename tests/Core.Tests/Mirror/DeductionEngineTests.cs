using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;

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
                && w.Deduction.ClanTechniques.Contains(technique));
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
