using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>The Immortal Arts' screen (audit §2): the legacies held, each cultivator's gifts, mastery, and why it cannot practise.</summary>
    [TestFixture]
    public class ArtViewTests
    {
        [Test]
        public void TheScreen_ShowsTheLegacies_TheGifts_AndWhyNot()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual(4, ArtView.Legacies(s).Count);
            Assert.IsFalse(ArtView.Legacies(s).Any(l => l.Held), "no legacy at the start (the user's decision)");
            var line = ArtView.Members(s).First();
            Assert.AreEqual(4, line.Skills.Count);
            Assert.IsTrue(line.Skills.All(k => k.Refusal != null), "without a legacy, no art");
            s.Arts.GainLegacy(ImmortalArt.Forge);
            Assert.IsTrue(ArtView.Legacies(s).Single(l => l.Art == ImmortalArt.Forge).Held);
        }
    }
}
