using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The diplomacy screen (G6): every power with its kind, strongest realm and relation, the treaties that bind it to the
    /// clan, and why a proposal would be refused — never the hidden suspicion (D7).
    /// </summary>
    [TestFixture]
    public class DiplomacyViewTests
    {
        private const string Tao = "Famille Tao";

        private static GameSession NewGame() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        [Test]
        public void Powers_ListEveryPower_TheFriendliestFirst()
        {
            var s = NewGame();
            var powers = DiplomacyView.Powers(s);
            Assert.AreEqual(s.Factions.Factions.Count, powers.Count);
            Assert.GreaterOrEqual(powers[0].Relation, powers[powers.Count - 1].Relation);
        }

        [Test]
        public void Powers_ShowTheTreatiesThatBindThem()
        {
            var s = NewGame();
            s.Treaties.Propose(Tao, TreatyKind.Trade, secret: true, sealedByOath: true, years: 5);

            var treaty = DiplomacyView.Powers(s).Single(p => p.Name == Tao).Treaties.Single();

            Assert.AreEqual("commerce", treaty.Kind);
            Assert.IsTrue(treaty.Secret && treaty.Sealed);
            Assert.AreEqual(5, treaty.YearsLeft);
        }

        [Test]
        public void ProposalRefusal_SaysWhyNot_OrNothing()
        {
            var s = NewGame();
            Assert.IsNull(DiplomacyView.ProposalRefusal(s, Tao, TreatyKind.NonAggression, clanAsSuzerain: false, sealedByOath: false));
            StringAssert.Contains("relation", DiplomacyView.ProposalRefusal(s, "Famille Lou", TreatyKind.NonAggression, false, false));
        }

        [Test]
        public void KindLabel_NamesEveryKindDifferently()
        {
            var labels = System.Enum.GetValues(typeof(TreatyKind)).Cast<TreatyKind>().Select(DiplomacyView.KindLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }
    }
}
