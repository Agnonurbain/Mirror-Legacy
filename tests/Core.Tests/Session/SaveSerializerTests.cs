using System.IO;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>Saves hold the whole game (version 2) and older saves are brought up to date on load.</summary>
    [TestFixture]
    public class SaveSerializerTests
    {
        private static string Snapshot(GameSession s) => string.Join("|",
            s.Clock.Year, s.Clock.Phase, s.Clan.LivingMembers.Count, s.Clan.Registry.Records.Count, s.Clan.PatriarchID,
            s.Resources.SpiritStones, s.Resources.Prestige, s.Mirror.MirrorPower, s.Karma.GenerationCount, s.Karma.TotalBirths,
            s.Buildings.GetBuilding(BuildingType.Mine).Level,
            string.Join(",", s.Factions.Factions.Select(f => f.RelationWithPlayer)),
            s.Deduction.Fragments.Count, s.Techniques.Deduced.Count,
            string.Join(",", s.Techniques.KnownIds.OrderBy(id => id)),
            string.Join(",", s.Resources.SpiritualQi.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")),
            string.Join(",", s.Resources.QiHarvestProgress.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")),
            string.Join(",", s.Clan.Registry.Records.Select(r => $"{r.CultivationMethodId}/{r.QiId}")));

        [Test]
        public void RoundTrip_KeepsTheTechniquesAndTheQi()
        {
            var s = GameSession.NewGame(Fixtures.Setup(4));
            s.Techniques.AddDeduced(new TechniqueData { ID = "deduced-1", Name = "Sutra de l'Onde", Kind = TechniqueKind.Cultivation, Grade = 3 });
            s.Resources.AddHarvestWork("seven-terraces-qi", 20);

            var reloaded = Reload(s);

            Assert.AreEqual(Snapshot(s), Snapshot(reloaded));
            Assert.AreEqual("Sutra de l'Onde", reloaded.Techniques.Find("deduced-1").Name);
        }

        [Test]
        public void FromSaveData_GivesAnOlderSaveTheClansKnowledge_AndItsQiCultivatorsTheirMethod()
        {
            var data = GameSession.NewGame(Fixtures.Setup(5)).ToSaveData();
            data.KnownTechniqueIds = null; // saved before phase L3
            data.SpiritualQi = null;
            data.QiHarvestProgress = null;
            foreach (var r in data.HistoricalRecords) { r.CultivationMethodId = null; r.QiId = null; }

            var s = GameSession.FromSaveData(data, Fixtures.Setup());

            Assert.IsTrue(s.Techniques.Knows("clear-spring-sutra") && s.Resources.QiPortions("clear-spring-qi") == 2);
            Assert.IsTrue(s.Clan.LivingMembers.Where(m => m.Realm >= CultivationRealm.QiRefinement)
                .All(m => m.CultivationMethodId == "clear-spring-sutra" && m.QiId == "clear-spring-qi"));
        }

        private static GameSession Reload(GameSession s) =>
            GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

        [Test]
        public void RoundTrip_RestoresTheWholeGame()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Buildings.Upgrade(BuildingType.Mine);
            s.Tasks.AssignTask(s.Clan.GetPatriarch(), TaskType.Mine);
            for (int y = 0; y < 3; y++) s.AdvanceYear();
            s.AdvancePhase();

            Assert.AreEqual(Snapshot(s), Snapshot(Reload(s)));
        }

        [Test]
        public void ToSaveData_IsASnapshot_ThatLaterPlayLeavesUntouched()
        {
            var s = GameSession.NewGame(Fixtures.Setup(2));
            var saved = s.ToSaveData();
            int savedAge = saved.HistoricalRecords[0].Age;
            int savedRelation = saved.Factions[0].RelationWithPlayer;

            s.Clan.LivingMembers[0].Age += 10;
            s.Factions.ChangeRelation(s.Factions.Factions[0].ID, 30);

            Assert.IsTrue(saved.HistoricalRecords[0].Age == savedAge && saved.Factions[0].RelationWithPlayer == savedRelation);
        }

        [Test]
        public void FromSaveData_CopiesTheSave_SoTwoLoadsStayIndependent()
        {
            var data = GameSession.NewGame(Fixtures.Setup(2)).ToSaveData();
            var first = GameSession.FromSaveData(data, Fixtures.Setup());
            var second = GameSession.FromSaveData(data, Fixtures.Setup());

            first.Clan.LivingMembers[0].Age += 10;

            Assert.AreNotEqual(first.Clan.LivingMembers[0].Age, second.Clan.LivingMembers[0].Age);
        }

        [Test]
        public void Serialize_WritesEnumsByName()
        {
            var json = SaveSerializer.Serialize(GameSession.NewGame(Fixtures.Setup(1)).ToSaveData());
            StringAssert.Contains("\"CurrentPhase\": \"Management\"", json);
        }

        [TestCase("{ not json")]
        [TestCase("null")]
        public void Deserialize_RefusesAnUnreadableSave(string json)
        {
            Assert.Throws<InvalidDataException>(() => SaveSerializer.Deserialize(json));
        }

        [Test]
        public void FromSaveData_BringsAVersionOneSaveUpToDate()
        {
            const string versionOne = @"{
                ""SaveVersion"": ""1.0"", ""ClanName"": ""Mo"", ""CurrentYear"": 7, ""CurrentPhase"": ""Management"",
                ""SpiritStones"": 640,
                ""HistoricalRecords"": [ { ""ID"": ""elder"", ""FirstName"": ""Wei"", ""LastName"": ""Mo"", ""IsMale"": true,
                    ""Age"": 50, ""MaxLifespan"": 0, ""IsAlive"": true, ""Realm"": ""QiRefinement"", ""RealmStage"": 0, ""MentalStability"": 70 } ]
            }";

            var s = GameSession.FromSaveData(SaveSerializer.Deserialize(versionOne), Fixtures.Setup());

            var elder = s.Clan.GetPatriarch();
            Assert.IsTrue(elder.ID == "elder" && elder.RealmStage == 1 && elder.HasSpiritualOrifice && elder.OrificeKnown
                && elder.MaxLifespan == 200 && s.Factions.Factions.Count == 8 && s.Resources.SpiritStones == 640 && s.Clock.Year == 7);
        }

        [Test]
        public void FromSaveData_RestoresTheFamilyName_OfFactionsSavedWithoutIt()
        {
            var data = GameSession.NewGame(Fixtures.Setup(1)).ToSaveData();
            foreach (var faction in data.Factions) faction.FamilyName = null; // a save from before FamilyName
            var s = GameSession.FromSaveData(data, Fixtures.Setup());
            Assert.AreEqual("Wang", s.Factions.Factions.First(f => f.Name == "Famille Wang").FamilyName);
        }

        [Test]
        public void FromSaveData_KeepsTheStoryEventsStillWaitingForAChoice()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Events.TriggerBreakthroughSuccess(s.Clan.GetPatriarch(), CultivationRealm.Foundation);
            Assert.AreEqual(StoryTriggerType.FirstFoundation, Reload(s).Story.PendingEvent.TriggerType);
        }
    }
}
