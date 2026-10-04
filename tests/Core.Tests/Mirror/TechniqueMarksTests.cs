using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The mark a stolen art keeps of its power (AUDIT_LORE.md §3.9, 2026-10-04; 📚 the mirror scrubs a sect's arts of their
    /// personal flairs): the power may recognize its style and gain proof; the mirror cleanses the mark.
    /// </summary>
    [TestFixture]
    public class TechniqueMarksTests
    {
        private static GameSession Session(double recognize = 1.0)
        {
            var b = Fixtures.QuietContent.Balance;
            var content = Fixtures.QuietContent with { Balance = b with { TechniqueMarks = b.TechniqueMarks with { RecognizeChance = recognize } } };
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
            s.Mirror.Restore(100, 0);
            return s;
        }

        private static (GameSession S, string Power, string Art) Stolen(double recognize = 1.0)
        {
            var s = Session(recognize);
            var power = s.Factions.Factions.First().Name;
            var art = s.Context.Content.Techniques.First(t => !s.Techniques.Knows(t.ID)).ID;
            s.Techniques.Learn(art);
            s.Events.TriggerManualStolen(power, art);
            return (s, power, art);
        }

        [Test]
        public void AStolenArt_BearsItsPowersMark_AndThePowerRecognizesIt()
        {
            var (s, power, art) = Stolen();
            Assert.AreEqual(power, s.Marks.MarkOf(art));
            Assert.AreEqual(power, LibraryView.Library(s).Single(l => l.Id == art).Mark);
            int evidence = s.Suspicion.Evidence(power);
            s.Marks.ProcessYear();
            Assert.Greater(s.Suspicion.Evidence(power), evidence);
        }

        [Test]
        public void TheMirror_CleansesTheMark()
        {
            var (s, power, art) = Stolen();
            Assert.IsNull(s.Marks.Cleanse(art));
            Assert.IsNull(s.Marks.MarkOf(art));
            int evidence = s.Suspicion.Evidence(power);
            s.Marks.ProcessYear();
            Assert.AreEqual(evidence, s.Suspicion.Evidence(power), "nothing left to recognize");
        }

        [Test]
        public void TheMarks_SurviveASave()
        {
            var (s, power, art) = Stolen(recognize: 0);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = s.Context.Content });
            Assert.AreEqual(power, reloaded.Marks.MarkOf(art));
        }
    }
}
