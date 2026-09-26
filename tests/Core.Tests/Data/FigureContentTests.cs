using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>
    /// The named cultivators of the world's powers (figures.json, from the wiki's character pages, renamed): who
    /// serves which power, the realm they reach, when they were born counted from the mirror's finding (year 0).
    /// </summary>
    [TestFixture]
    public class FigureContentTests
    {
        private static FigureDefinition Figure(string id) => Fixtures.Content.Figures.Single(f => f.Id == id);

        [Test]
        public void ShippedFigures_KeepTheLexiconsNames()
        {
            Assert.AreEqual("Bai Chengyu", Figure("chi-wei").Name);
            Assert.AreEqual("Bai Ruozhi", Figure("chi-buzi").Name);
            Assert.AreEqual("Vénérable Lingxu", Figure("shangyuan").Name);
        }

        [Test]
        public void ShippedFigures_ServeThePowersOfTheLore()
        {
            Assert.AreEqual("Secte du Pic des Nuées", Figure("chi-buzi").FactionName);
            Assert.AreEqual("Secte de la Lune Pâle", Figure("shangyuan").FactionName);
            Assert.AreEqual(CultivationRealm.GoldenCore, Figure("shangyuan").Realm);
        }

        [Test]
        public void ShippedFigures_BornBeforeTheMirror_CountTheirYearsBeforeIt()
        {
            Assert.AreEqual(-192, Figure("chi-buzi").BornYear); // « 192 B.M. »
        }

        [Test]
        public void ShippedFigures_NeverOutrankTheirPower()
        {
            foreach (var figure in Fixtures.Content.Figures)
            {
                var power = Fixtures.Content.Factions.Single(f => f.Name == figure.FactionName);
                Assert.LessOrEqual(figure.Realm, power.HighestRealm, $"{figure.Name} of {power.Name}");
            }
        }

        [Test]
        public void ShippedFigures_BearNoNameOfTheSource()
        {
            var source = new[] { "Chi Buzi", "Chi Wei", "Shangyuan", "Tang Yuanwu", "Si Boxiu", "Zhang Yun", "Xiao Chuting",
                "Ning Tiaoxiao", "Ning Wan", "Kong Yanxi", "Lin Wei", "Zipei", "Tulong Jian", "Hengxing", "Chang Xiaozi" };
            Assert.IsFalse(Fixtures.Content.Figures.Any(f => source.Contains(f.Name)));
        }

        [Test]
        public void Load_Refuses_AFigureOfAnUnknownPower()
        {
            var file = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.FiguresFile));
            file.First()["factionName"] = "Secte Inconnue";
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.FiguresFile ? file.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.FiguresFile, error.Message);
        }

        [Test]
        public void Map_ShowsTheFiguresOfAPower_BornByNow()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var figures = WorldMapView.FiguresOf(s, "Secte du Pic des Nuées");
            CollectionAssert.Contains(figures.Select(f => f.Name), "Bai Ruozhi");
            Assert.IsTrue(figures.All(f => !string.IsNullOrEmpty(f.Realm)));
        }

        [Test]
        public void Map_HidesTheFiguresNotYetBorn()
        {
            var unborn = new FigureDefinition { Id = "later", Name = "Né plus tard", FactionName = "Secte du Pic des Nuées",
                Realm = CultivationRealm.Foundation, BornYear = 50 };
            var content = Fixtures.QuietContent with { Figures = Fixtures.Content.Figures.Append(unborn).ToList() };
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content });

            Assert.IsFalse(WorldMapView.FiguresOf(s, "Secte du Pic des Nuées").Any(f => f.Name == "Né plus tard"));
        }
    }
}
