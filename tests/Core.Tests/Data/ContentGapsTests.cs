using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>
    /// What the lore and the wiki do not say is marked in the data (user request, 2026-09-26): each item's
    /// provenance and its interpreted fields, so a new source replaces them in the data without touching code;
    /// the gaps report lists what is left to confirm.
    /// </summary>
    [TestFixture]
    public class ContentGapsTests
    {
        [Test]
        public void ShippedData_MarksTheInterpretedFields()
        {
            var content = Fixtures.Content;
            CollectionAssert.Contains(content.Qi.Single(q => q.Id == "six-harmonies-qi").InterpretedFields, "Foundation");
            CollectionAssert.Contains(content.Techniques.Single(t => t.ID == "six-harmonies-method").InterpretedFields, "Grade");
            // the wiki gives the Clear Spring Qi's foundation; its name follows its method
            CollectionAssert.AreEqual(new[] { "Name" }, content.Qi.Single(q => q.Id == "clear-spring-qi").InterpretedFields);
        }

        [Test]
        public void ShippedData_GivesEachItemItsProvenance()
        {
            var content = Fixtures.Content;
            Assert.AreEqual(Provenance.Wiki, content.Qi.Single(q => q.Id == "clear-spring-qi").Provenance);
            Assert.AreEqual(Provenance.Lore, content.Fruitions.Single(f => f.Id == "orthodox-water").Provenance);
            var placed = content.Fruitions.Single(f => f.Id == "veiled-yin").Abilities.Single(a => a.Id == "returning-yin-mist");
            Assert.AreEqual(Provenance.Interpretation, placed.Provenance); // an isolated foundation placed by interpretation
        }

        [Test]
        public void Load_Refuses_AnInterpretedFieldThatDoesNotExist()
        {
            var qi = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.QiFile));
            qi.Single(q => (string)q["id"] == "six-harmonies-qi")["interpretedFields"] = new JArray("Fondation"); // a typo
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.QiFile ? qi.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.QiFile, error.Message);
        }

        [Test]
        public void Report_ListsTheInterpretations_AndWhatTheWorldHasNotRevealed()
        {
            var report = ContentGaps.Report(Fixtures.Content);
            Assert.IsTrue(report.Any(line => line.Contains("six-harmonies-qi") && line.Contains("Foundation")));
            Assert.IsTrue(report.Any(line => line.Contains("nourishing-water") && line.Contains("unrevealed-4")));
        }

        /// <summary>Prints the gaps (./Scripts/dev.sh gaps).</summary>
        [Test, Explicit]
        public void ContentGapsReport()
        {
            foreach (var line in ContentGaps.Report(Fixtures.Content))
                TestContext.Progress.WriteLine(line);
        }
    }
}
