using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The mirror's screen (G6, L2b): its power and the seeds it sustains; each intervention with its cost, its effect and
    /// why not now; the Talisman Seed offered only to a mortal the clan examined (an unexamined orifice is never guessed);
    /// the Judgment's targets; the fragments and a deduction's cost before it is attempted.
    /// </summary>
    [TestFixture]
    public class MirrorViewTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static CharacterData Mortal(GameSession s, bool examined)
        {
            var mortal = Fixtures.Mortal();
            mortal.OrificeKnown = examined;
            s.Clan.AddMember(mortal);
            return mortal;
        }

        [Test]
        public void TheHeader_ShowsThePower_AndTheSeeds()
        {
            var s = Session();
            var header = MirrorView.Header(s);
            Assert.AreEqual(s.Mirror.MirrorPower, header.Power);
            Assert.AreEqual(MirrorSystem.MaxMirrorPower, header.MaxPower);
            Assert.AreEqual(s.Mirror.TalismanSeedCapacity, header.SeedCapacity);
            Assert.AreEqual(s.Clan.LivingMembers.Count(m => m.HasTalismanSeed), header.Seeds);
        }

        [Test]
        public void EveryIntervention_HasItsCost_AndSaysWhyNot()
        {
            var s = Session();
            var all = MirrorView.Interventions(s);
            CollectionAssert.AreEquivalent(new[] { MirrorView.Shield, MirrorView.Judgment, MirrorView.Seed, MirrorView.Pulse }, all.Select(i => i.Id));
            Assert.AreEqual(MirrorSystem.AncestralShieldCost, all.Single(i => i.Id == MirrorView.Shield).Cost);
            StringAssert.Contains("bataille", all.Single(i => i.Id == MirrorView.Pulse).Refusal, "a pulse is given in battle");

            s.Mirror.Restore(MirrorSystem.AncestralShieldCost - 1, 0);
            StringAssert.Contains("puissance", MirrorView.Interventions(s).Single(i => i.Id == MirrorView.Shield).Refusal);

            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            Assert.IsTrue(s.Mirror.UseAncestralShield());
            StringAssert.Contains("veille déjà", MirrorView.Interventions(s).Single(i => i.Id == MirrorView.Shield).Refusal);
        }

        [Test]
        public void TheSeed_IsOffered_OnlyToAMortalTheClanExamined()
        {
            var s = Session();
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            var known = Mortal(s, examined: true);
            var unknown = Mortal(s, examined: false);

            var candidates = MirrorView.SeedCandidates(s);

            Assert.IsNull(candidates.Single(c => c.Id == known.ID).Refusal);
            StringAssert.Contains("non examiné", candidates.Single(c => c.Id == unknown.ID).Refusal, "never guess an orifice");
            Assert.IsFalse(candidates.Select(c => s.Clan.FindById(c.Id)).Any(m => m.HasTalismanSeed || (m.OrificeKnown && m.HasSpiritualOrifice)),
                "a known cultivator needs no seed");
        }

        [Test]
        public void TheSeed_IsRefused_WhenTheMirrorSustainsNoMore()
        {
            var s = Session();
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            for (int i = s.Clan.LivingMembers.Count(m => m.HasTalismanSeed); i < s.Mirror.TalismanSeedCapacity; i++)
                Mortal(s, examined: true).HasTalismanSeed = true;
            var mortal = Mortal(s, examined: true);
            StringAssert.Contains("capacité", MirrorView.SeedCandidates(s).Single(c => c.Id == mortal.ID).Refusal);
        }

        [Test]
        public void TheJudgment_TellsAnInLaw_FromBlood()
        {
            var s = Session();
            var inLaw = Fixtures.Cultivator();
            inLaw.FromFaction = "Famille Tao";
            s.Clan.AddMember(inLaw);
            var targets = MirrorView.JudgmentTargets(s);
            Assert.AreEqual(s.Clan.LivingMembers.Count, targets.Count);
            StringAssert.Contains("Famille Tao", targets.Single(t => t.Id == inLaw.ID).Origin);
        }

        [Test]
        public void ADeduction_GivesItsCost_OrWhyNot()
        {
            var s = Session();
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            s.Deduction.AddFragment(Element.Fire, 2, "Feuillet brûlé");
            s.Deduction.AddFragment(Element.Fire, 3, "Stèle fendue");
            var ids = MirrorView.Fragments(s).Select(f => f.Id).ToList();

            StringAssert.Contains("2 à 5", MirrorView.DeductionPreview(s, ids.Take(1).ToList()).Refusal);
            var preview = MirrorView.DeductionPreview(s, ids);
            Assert.IsNull(preview.Refusal);
            Assert.AreEqual(ids.Count * DeductionEngine.PowerPerFragment, preview.Cost);

            s.Mirror.Restore(DeductionEngine.PowerPerFragment, 0);
            StringAssert.Contains("puissance", MirrorView.DeductionPreview(s, ids).Refusal);
        }

        // ---- Review ----

        [Test]
        public void TheJudgment_NamesAMortal_AsTheOtherScreensDo()
        {
            var s = Session();
            var unexamined = Mortal(s, examined: false);
            var mortal = Mortal(s, examined: true);
            var targets = MirrorView.JudgmentTargets(s);
            Assert.AreEqual("Orifice non examiné", targets.Single(t => t.Id == unexamined.ID).Realm);
            Assert.AreEqual(MirrorChronicles.Characters.RankCatalog.DisplayName(mortal), targets.Single(t => t.Id == mortal.ID).Realm);
        }

        [Test]
        public void TheShield_IsNotInvokedTwice()
        {
            var s = Session();
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            Assert.IsTrue(s.Mirror.UseAncestralShield());
            int power = s.Mirror.MirrorPower;
            Assert.IsFalse(s.Mirror.UseAncestralShield());
            Assert.AreEqual(power, s.Mirror.MirrorPower, "no power burnt for nothing");
        }

        [Test]
        public void TheSeedIntervention_SaysWhyNot()
        {
            var s = Session();
            s.Mirror.Restore(MirrorSystem.TalismanSeedCost - 1, 0);
            StringAssert.Contains("puissance", MirrorView.Interventions(s).Single(i => i.Id == MirrorView.Seed).Refusal);
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            for (int i = s.Clan.LivingMembers.Count(m => m.HasTalismanSeed); i < s.Mirror.TalismanSeedCapacity; i++)
                Mortal(s, examined: true).HasTalismanSeed = true;
            StringAssert.Contains("davantage", MirrorView.Interventions(s).Single(i => i.Id == MirrorView.Seed).Refusal);
        }
    }
}
