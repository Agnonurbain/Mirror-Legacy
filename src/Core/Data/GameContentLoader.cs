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
    /// Reads and checks the data files. Refuses content the game could not play, naming the file at
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
        public const string TechniquesFile = "techniques.json";
        public const string QiFile = "qi.json";

        public static IReadOnlyList<string> Files { get; } =
            new[] { ClanFile, NamesFile, BalanceFile, FactionsFile, EventsFile, StoryFile, TechniquesFile, QiFile };

        /// <summary>The kinds a deduction can yield, which the deduction names must cover.</summary>
        private static readonly TechniqueKind[] DeducibleKinds =
            { TechniqueKind.Cultivation, TechniqueKind.Spell, TechniqueKind.Movement, TechniqueKind.Weapon };

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
            var catalog = Read<TechniqueCatalog>(readFile, TechniquesFile);
            var qi = Read<List<QiDefinition>>(readFile, QiFile);

            CheckClan(clan);
            CheckNames(names);
            CheckBalance(balance);
            CheckFactions(factions);
            CheckEvents(events);
            CheckStory(story, factions);
            CheckQi(qi);
            CheckTechniques(catalog, qi);
            CheckClanKnowledge(clan, catalog.Techniques, qi);

            return new GameContent
            {
                Clan = clan,
                Names = names,
                Balance = balance,
                Factions = factions,
                RandomEvents = events,
                StoryEvents = story,
                Techniques = catalog.Techniques,
                Qi = qi,
                DeductionNames = catalog.DeductionNames
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
            Require(balance.TechniqueSpeedByGrade != null && balance.TechniqueSpeedByGrade.Count == TechniqueRules.MaxGrade
                && balance.TechniqueSpeedByGrade.All(s => s > 0),
                BalanceFile, $"techniqueSpeedByGrade needs one positive speed per grade, {TechniqueRules.MinGrade} to {TechniqueRules.MaxGrade}.");
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

        private static void CheckQi(List<QiDefinition> qi)
        {
            Require(qi.All(q => !string.IsNullOrWhiteSpace(q.Id) && !string.IsNullOrWhiteSpace(q.Name)), QiFile, "every Qi needs an id and a name.");
            Require(qi.Select(q => q.Id).Distinct().Count() == qi.Count, QiFile, "two Qi share an id.");
            var never = qi.FirstOrDefault(q => q.YearsPerPortion < 1);
            Require(never == null, QiFile, $"{never?.Id} needs at least one year per portion, or it can never be gathered.");
        }

        private static void CheckTechniques(TechniqueCatalog catalog, List<QiDefinition> qi)
        {
            var techniques = catalog.Techniques;
            Require(techniques != null && techniques.All(t => !string.IsNullOrWhiteSpace(t.ID) && !string.IsNullOrWhiteSpace(t.Name)),
                TechniquesFile, "every technique needs an id and a name.");
            Require(techniques.Select(t => t.ID).Distinct().Count() == techniques.Count, TechniquesFile, "two techniques share an id.");

            var qiById = qi.ToDictionary(q => q.Id);
            foreach (var t in techniques)
            {
                Require(t.Grade >= TechniqueRules.MinGrade && t.Grade <= TechniqueRules.MaxGrade, TechniquesFile,
                    $"{t.ID}: the grade must lie between {TechniqueRules.MinGrade} and {TechniqueRules.MaxGrade} (7 stands for 7+).");

                bool needsQi = TechniqueRules.Covers(t, CultivationRealm.QiRefinement);
                if (t.Kind == TechniqueKind.Cultivation)
                    Require(TechniqueRules.SupremeRealm(t) >= t.RequiredRealm, TechniquesFile, $"{t.ID}: its supreme realm lies below its first realm.");
                Require(!needsQi || t.RequiredQiId != null, TechniquesFile, $"{t.ID}: a Qi Cultivation method needs its Qi (requiredQiId).");
                Require(needsQi || t.RequiredQiId == null, TechniquesFile, $"{t.ID}: only a Qi Cultivation method needs a Qi.");

                if (t.RequiredQiId != null)
                {
                    Require(qiById.TryGetValue(t.RequiredQiId, out var q), TechniquesFile, $"{t.ID}: the Qi \"{t.RequiredQiId}\" is not in {QiFile}.");
                    Require(q.Vanished == (t.Category == TechniqueCategory.Ancestral), TechniquesFile,
                        $"{t.ID}: an ancestral method is one whose Qi has vanished, and only such a method.");
                }

                if (t.Flaws != null)
                {
                    Require(t.Flaws.CounteredById == null || techniques.Any(o => o.ID == t.Flaws.CounteredById), TechniquesFile,
                        $"{t.ID}: countered by \"{t.Flaws.CounteredById}\", which the catalog does not have.");
                    Require(t.Flaws.LifespanFactor > 0 && t.Flaws.LifespanFactor <= 1, TechniquesFile, $"{t.ID}: the lifespan factor must lie in (0, 1].");
                    Require(t.Flaws.SpeedByRealm == null || t.Flaws.SpeedByRealm.Values.All(s => s > 0), TechniquesFile, $"{t.ID}: flawed speeds must be positive.");
                }
            }

            var names = catalog.DeductionNames;
            Require(names != null && !string.IsNullOrWhiteSpace(names.Template), TechniquesFile, "deductionNames needs a template.");
            Require(DeducibleKinds.All(k => names.Kinds != null && names.Kinds.ContainsKey(k)), TechniquesFile,
                "deductionNames.kinds needs a noun for every kind a deduction can yield.");
            Require(Enum.GetValues(typeof(Element)).Cast<Element>().Where(e => e != Element.None)
                    .All(e => names.Elements != null && names.Elements.ContainsKey(e)),
                TechniquesFile, "deductionNames.elements needs a phrase for every element.");
            Require(names.GradeWords != null && names.GradeWords.Count == TechniqueRules.MaxGrade, TechniquesFile,
                "deductionNames.gradeWords needs one word (possibly empty) per grade.");
        }

        /// <summary>What the clan knows at the start (clan.json) must exist, and founders must practise a method they can.</summary>
        private static void CheckClanKnowledge(ClanDefinition clan, IReadOnlyList<TechniqueData> techniques, List<QiDefinition> qi)
        {
            var known = clan.StartingTechniques ?? Array.Empty<string>();
            var unknown = known.FirstOrDefault(id => techniques.All(t => t.ID != id));
            Require(unknown == null, ClanFile, $"the starting technique \"{unknown}\" is not in {TechniquesFile}.");

            var unknownQi = (clan.StartingQi ?? new Dictionary<string, int>()).FirstOrDefault(kv => qi.All(q => q.Id != kv.Key) || kv.Value < 0);
            Require(unknownQi.Key == null, ClanFile, $"the starting Qi \"{unknownQi.Key}\" is not in {QiFile} or has a negative amount.");

            foreach (var f in clan.Founders)
            {
                var method = techniques.FirstOrDefault(t => t.ID == f.CultivationMethod);
                if (f.CultivationMethod != null)
                    Require(method != null && method.Kind == TechniqueKind.Cultivation && known.Contains(method.ID), ClanFile,
                        $"{f.FirstName} practises \"{f.CultivationMethod}\", which is not a cultivation method the clan knows.");
                if (f.Realm >= CultivationRealm.QiRefinement)
                    Require(TechniqueRules.Covers(method, f.Realm), ClanFile,
                        $"{f.FirstName} is a Qi cultivator or beyond and needs a method covering their realm (LORE.md §5.2).");
            }
        }

        private static bool IsProbability(double value) => value >= 0 && value <= 1;

        private static void Require(bool condition, string file, string problem)
        {
            if (!condition) throw new InvalidDataException($"{file}: {problem}");
        }
    }
}
