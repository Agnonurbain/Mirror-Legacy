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

        // ---- The powers' own politics ----

        [Test]
        public void Powers_ShowTheirPublicAllies_AndTheirSuzerain_NotTheSecretOnes()
        {
            var s = NewGame();
            s.Politics.RestoreBonds(new[]
            {
                new PowerBond("a", BondKind.Alliance, "Famille Fang", Tao, 1, false),
                new PowerBond("x", BondKind.Alliance, "Famille Fang", "Famille Gu", 1, true),
                new PowerBond("v", BondKind.Vassalage, "Secte du Pic des Nuées", "Famille Fang", 1, false)
            });

            var fang = DiplomacyView.Powers(s).Single(p => p.Name == "Famille Fang");

            CollectionAssert.AreEqual(new[] { Tao }, fang.Allies);
            Assert.AreEqual("Secte du Pic des Nuées", fang.Suzerain);
        }

        [Test]
        public void TheCoalition_AndTheCall_AreShown()
        {
            var s = NewGame();
            Assert.IsNull(DiplomacyView.Coalition(s));
            Assert.IsNull(DiplomacyView.Call(s));
            s.Politics.RestoreCoalition(new Coalition(new[] { "Famille Ruan", "Famille Lou" }.ToList(), 4));
            s.Politics.RestoreCall(new CallToArms(Tao, "Famille Ruan", 1));

            Assert.AreEqual((4, 2), (DiplomacyView.Coalition(s).YearsLeft, DiplomacyView.Coalition(s).Members.Count));
            var call = DiplomacyView.Call(s);
            Assert.AreEqual((Tao, "Famille Ruan", Fixtures.Content.Balance.Politics.CallStonesCost), (call.Ally, call.Attacker, call.Cost));
        }

        [Test]
        public void AVassalClan_SeesHowCloseItIsToBeingAbsorbed()
        {
            var s = NewGame();
            s.Treaties.Propose("Secte du Pic des Nuées", TreatyKind.Vassalage);
            var treaty = s.Treaties.With("Secte du Pic des Nuées").Single();
            s.Treaties.RestoreTreaties(new[] { treaty with { Absorptions = 1 } });

            var line = DiplomacyView.Powers(s).Single(p => p.Name == "Secte du Pic des Nuées").Treaties.Single();

            Assert.AreEqual((1, Fixtures.Content.Balance.Politics.ClanAbsorptionSteps), (line.Absorptions, line.AbsorptionSteps));
        }

        [Test]
        public void TheBlackmailDemands_AreShown()
        {
            var s = NewGame();
            s.Intrigues.RestoreDemands(new[] { new Demand("Porte du Chrysanthème Noir", 150, 1) }, null);
            Assert.AreEqual(new DemandLine("Porte du Chrysanthème Noir", 150, "pour son silence", "elle répand ses preuves"), DiplomacyView.Demands(s).Single());
        }
    }
}
