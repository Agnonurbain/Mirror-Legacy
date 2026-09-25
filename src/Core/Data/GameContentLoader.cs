using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MirrorChronicles.Characters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// Reads and checks the six data files. Refuses content the game could not play, naming the file at
    /// fault, so a modder or a designer sees the mistake at startup rather than mid-game.
    /// </summary>
    public static class GameContentLoader
    {
        public const string ClanFile = "clan.json";
        public const string NamesFile = "names.json";
        public const string BalanceFile = "balance.json";
        public const string FactionsFile = "factions.json";
        public const string EventsFile = "events.json";
        public const string StoryFile = "story.json";

        public static IReadOnlyList<string> Files { get; } =
            new[] { ClanFile, NamesFile, BalanceFile, FactionsFile, EventsFile, StoryFile };

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = { new StringEnumConverter() }
        };

        /// <param name="readFile">Returns the text of a data file by name (null when it does not exist).</param>
        /// <exception cref="InvalidDataException">A file is missing, unreadable or describes something unplayable.</exception>
        public static GameContent Load(Func<string, string> readFile)
        {
            var clan = Read<ClanDefinition>(readFile, ClanFile);
            var names = Read<NamePools>(readFile, NamesFile);
            var balance = Read<BalanceSettings>(readFile, BalanceFile);
            var factions = Read<List<FactionData>>(readFile, FactionsFile);
            var events = Read<List<RandomEventData>>(readFile, EventsFile);
            var story = Read<List<StoryEventData>>(readFile, StoryFile);

            CheckClan(clan);
            CheckNames(names);
            CheckBalance(balance);
            CheckFactions(factions);
            CheckEvents(events);
            CheckStory(story, factions);

            return new GameContent
            {
                Clan = clan,
                Names = names,
                Balance = balance,
                Factions = factions,
                RandomEvents = events,
                StoryEvents = story
            };
        }

        private static T Read<T>(Func<string, string> readFile, string file) where T : class
        {
            string json;
            try
            {
                json = readFile(file);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                throw new InvalidDataException($"{file}: cannot be read ({e.Message}).", e);
            }
            Require(json != null, file, "missing.");

            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings) ?? throw new InvalidDataException($"{file}: empty.");
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"{file}: {e.Message}", e);
            }
        }

        private static void CheckClan(ClanDefinition clan)
        {
            Require(!string.IsNullOrWhiteSpace(clan.ClanName), ClanFile, "the clan needs a name.");
            Require(clan.Founders != null && clan.Founders.Count > 0, ClanFile, "the clan needs founders.");
            Require(clan.Founders.Count(f => f.Role == FounderRole.Patriarch) == 1, ClanFile, "exactly one founder must be the Patriarch.");
            Require(clan.Founders.Count(f => f.Role == FounderRole.Matriarch) <= 1, ClanFile, "at most one founder can be the Matriarch.");
            foreach (var f in clan.Founders)
            {
                Require(!string.IsNullOrWhiteSpace(f.FirstName), ClanFile, "every founder needs a first name.");
                int minStage = f.Realm == CultivationRealm.Embryonic ? 0 : 1;
                Require(f.Age >= 0 && f.RealmStage >= minStage && f.RealmStage <= PowerLadder.StageCount(f.Realm),
                    ClanFile, $"{f.FirstName} has an impossible age or stage.");
            }
        }

        private static void CheckNames(NamePools names)
        {
            foreach (var (pool, label) in new[] { (names.Male, "male"), (names.Female, "female"), (names.OutsiderFamilies, "outsiderFamilies") })
                Require(pool != null && pool.Count > 0 && pool.All(n => !string.IsNullOrWhiteSpace(n)), NamesFile, $"the {label} pool needs names.");
        }

        private static void CheckBalance(BalanceSettings balance)
        {
            var odds = balance.OrificeOdds;
            Require(odds != null, BalanceFile, "orificeOdds is missing.");
            Require(IsProbability(odds.Commoner) && IsProbability(odds.OneParent) && IsProbability(odds.TwoParents),
                BalanceFile, "orifice odds must lie between 0 and 1.");
            Require(IsProbability(balance.AnnualBirthChance) && IsProbability(balance.AnnualMarriageChance),
                BalanceFile, "birth and marriage chances must lie between 0 and 1.");
            Require(balance.MinMotherAge >= 0 && balance.MinMotherAge <= balance.MaxMotherAge,
                BalanceFile, "the motherhood window is inverted.");
        }

        private static void CheckFactions(List<FactionData> factions)
        {
            Require(factions.All(f => !string.IsNullOrWhiteSpace(f.Name)), FactionsFile, "every faction needs a name.");
        }

        private static void CheckEvents(List<RandomEventData> events)
        {
            Require(events.All(e => !string.IsNullOrWhiteSpace(e.Name)), EventsFile, "every event needs a name.");
            Require(events.All(e => e.Weight > 0), EventsFile, "every event needs a positive weight, or it can never be drawn.");
        }

        private static void CheckStory(List<StoryEventData> story, List<FactionData> factions)
        {
            var unknown = story.SelectMany(e => e.Choices ?? new List<StoryChoice>())
                .Select(c => c.Outcome?.FactionName)
                .FirstOrDefault(name => !string.IsNullOrEmpty(name) && factions.All(f => f.Name != name));
            Require(unknown == null, StoryFile, $"a choice names the faction \"{unknown}\", which factions.json does not have.");
            Require(story.All(e => !string.IsNullOrWhiteSpace(e.Name)), StoryFile, "every story event needs a name.");
            Require(story.All(e => e.Choices != null && e.Choices.Count > 0 && e.Choices.All(c => !string.IsNullOrWhiteSpace(c.Label))),
                StoryFile, "every story event needs choices with labels.");
            Require(story.Select(e => e.TriggerType).Distinct().Count() == story.Count, StoryFile, "each milestone can have only one story event.");
        }

        private static bool IsProbability(double value) => value >= 0 && value <= 1;

        private static void Require(bool condition, string file, string problem)
        {
            if (!condition) throw new InvalidDataException($"{file}: {problem}");
        }
    }
}
