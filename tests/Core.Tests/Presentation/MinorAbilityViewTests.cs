using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The minor abilities as the player sees them (G6, 2026-10-03): for each lineage, those the clan knows and how many it
    /// does not; the mirror's deduction at its price, or why not; a holding power's teaching in kind (the gifts proposed),
    /// or why not; a theft at its odds. Each option does what it says.
    /// </summary>
    [TestFixture]
    public class MinorAbilityViewTests
    {
        private const string Lineage = "gathered-wood";

        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            return s;
        }

        private static FactionData Holder(GameSession s, int relation)
        {
            var power = s.Factions.Factions.First(f => f.Elders.Count > 0);
            power.Elders[0].FruitionId = Lineage;
            power.RelationWithPlayer = relation;
            return power;
        }

        private static MinorAbilityLine Line(GameSession s) => MinorAbilityView.Lines(s).Single(l => l.Lineage == Lineage);

        [Test]
        public void ALineage_ShowsItsKnownMinors_AndTheDeductionsRefusal()
        {
            var s = Session();
            Holder(s, 0);
            var line = Line(s);
            StringAssert.Contains("cinq", line.Options.Single(o => o.Kind == MinorDeal.Deduce).Refusal);
            Assert.Greater(line.Unknown, 0);
        }

        [Test]
        public void AHolder_TeachesInKind_WhenTheClanHasEnoughToGive()
        {
            var s = Session();
            var power = Holder(s, 60);
            s.Resources.AddQi("clear-spring-qi", 40);
            var learn = Line(s).Options.Single(o => o.Kind == MinorDeal.Learn && o.Power == power.Name);
            Assert.IsNull(learn.Refusal);
            StringAssert.Contains("Qi", learn.Label, "the gifts proposed are named");
            Assert.IsNull(MinorAbilityView.Perform(s, Lineage, learn));
            Assert.AreEqual(1, s.Minors.Known(Lineage).Count);
        }

        [Test]
        public void AColdHolder_Refuses_AndATheftShowsItsOdds()
        {
            var s = Session();
            var power = Holder(s, -10);
            s.Clan.AddMember(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            var line = Line(s);
            Assert.IsNotNull(line.Options.Single(o => o.Kind == MinorDeal.Learn).Refusal);
            StringAssert.Contains("%", line.Options.Single(o => o.Kind == MinorDeal.Steal && o.Power == power.Name).Label);
        }

        [Test]
        public void ALineageWithoutAnythingToLearn_IsNotListed()
        {
            var s = Session();
            Assert.IsFalse(MinorAbilityView.Lines(s).Any(l => l.Lineage == Lineage && l.Unknown == 0 && l.Known.Count == 0));
        }
    }
}
