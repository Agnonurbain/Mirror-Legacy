using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>The lineages as the clan knows them (the library's « Lignées » tab, 2026-10-01).</summary>
    [TestFixture]
    public class LineagesViewTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        [Test]
        public void EveryLineage_IsListed_WithItsStatusAndHolder()
        {
            var s = Session();
            var rows = LineagesView.Rows(s);
            Assert.AreEqual(s.Context.Content.Fruitions.Count, rows.Count);
            var mutable = rows.Single(r => r.Id == "mutable-water");
            StringAssert.Contains("Tan Qing", mutable.State);
        }

        [Test]
        public void AHiddenLineage_ShowsNoHolder_AndOffersTheMirror()
        {
            var s = Session();
            var violet = LineagesView.Rows(s).Single(r => r.Id == "violet-qi");
            StringAssert.Contains("cachée", violet.State);
            Assert.IsTrue(violet.CanReveal);
        }

        [Test]
        public void ARaceAndAReturn_AreShown()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent with
            {
                Balance = Fixtures.QuietContent.Balance with
                {
                    WorldFruitions = Fixtures.QuietContent.Balance.WorldFruitions with { HolderPassChance = 1.0, ReincarnationChance = 1.0 },
                    GoldenCore = Fixtures.QuietContent.Balance.GoldenCore with { TransferChance = 0, TransformationChance = 0 } // nobody rises meanwhile
                }
            } });
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.WorldFruitions.ProcessYear();
            var mutable = LineagesView.Rows(s).Single(r => r.Id == "mutable-water");
            StringAssert.Contains("course", mutable.State);
            StringAssert.Contains("Tan Qing", mutable.State, "he may come back");
        }
    }
}
