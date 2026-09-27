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
        public const string FruitionsFile = "fruitions.json";
        public const string OathsFile = "oaths.json";
        public const string RegionsFile = "regions.json";
        public const string TalismansFile = "talismans.json";
        public const string FiguresFile = "figures.json";
        public const string BeastsFile = "beasts.json";
        public const string AtmospheresFile = "atmospheres.json";
        public const string SecretsFile = "secrets.json";
        public const string PatronsFile = "patrons.json";

        public static IReadOnlyList<string> Files { get; } =
            new[] { ClanFile, NamesFile, BalanceFile, FactionsFile, EventsFile, StoryFile, TechniquesFile, QiFile, FruitionsFile, OathsFile, RegionsFile, TalismansFile, FiguresFile, BeastsFile, AtmospheresFile, SecretsFile, PatronsFile };

        /// <summary>Abilities a lineage has besides its substitutes: the orthodox five (LORE.md §6.1).</summary>
        private const int OrthodoxAbilities = 5;

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
            var fruitions = Read<FruitionCatalog>(readFile, FruitionsFile);
            var oaths = Read<OathCatalog>(readFile, OathsFile);
            var regions = Read<List<RegionDefinition>>(readFile, RegionsFile);
            var talismans = Read<List<TalismanDefinition>>(readFile, TalismansFile);
            var figures = Read<List<FigureDefinition>>(readFile, FiguresFile);
            var beasts = Read<List<BeastSpecies>>(readFile, BeastsFile);
            var atmospheres = Read<List<AtmosphereDefinition>>(readFile, AtmospheresFile);
            var secretKinds = Read<List<SecretKind>>(readFile, SecretsFile);
            var patrons = Read<List<PatronDefinition>>(readFile, PatronsFile);

            CheckClan(clan);
            CheckNames(names);
            CheckBalance(balance);
            CheckEvents(events);
            CheckStory(story, factions);
            CheckQi(qi);
            CheckTechniques(catalog, qi);
            CheckClanKnowledge(clan, catalog.Techniques, qi);
            CheckFruitions(fruitions);
            CheckOaths(oaths);
            CheckRegions(regions);
            CheckAtmospheres(atmospheres, fruitions.Fruitions);
            Require(secretKinds.Count > 0 && secretKinds.All(k => !string.IsNullOrWhiteSpace(k.Id) && !string.IsNullOrWhiteSpace(k.Name)
                    && k.Rank >= 1 && k.Rank <= 4 && k.InterpretedFields != null)
                && secretKinds.Select(k => k.Id).Distinct().Count() == secretKinds.Count
                && secretKinds.Any(k => k.Holder != SecretHolder.Clan),
                SecretsFile, "every secret kind needs a unique id, a name and a rank from 1 to 4 (the supreme rank is the mirror's own), and some for the powers.");
            CheckRegionalQi(regions, qi, atmospheres);
            CheckTalismans(talismans);
            CheckFigures(figures, factions);
            Require(beasts.Count > 0 && beasts.All(b => !string.IsNullOrWhiteSpace(b.Id) && !string.IsNullOrWhiteSpace(b.Name) && b.Habitats != null)
                && beasts.Select(b => b.Id).Distinct().Count() == beasts.Count, BeastsFile, "beasts needs species with unique ids, names and habitats.");
            CheckFactions(factions, regions, catalog.Techniques);
            Require(regions.Any(r => r.Id == clan.HomeRegion), ClanFile, $"homeRegion \"{clan.HomeRegion}\" is not a region of {RegionsFile}.");
            CheckFoundations(qi, catalog.Techniques, fruitions.Fruitions);
            CheckInterpretedFields(TechniquesFile, catalog.Techniques.Select(t => (t.ID, typeof(TechniqueData), (IEnumerable<string>)t.InterpretedFields)));
            CheckInterpretedFields(FactionsFile, factions.Select(f => (f.Name, typeof(FactionData), (IEnumerable<string>)f.InterpretedFields)));
            CheckInterpretedFields(FiguresFile, figures.Select(f => (f.Id, typeof(FigureDefinition), (IEnumerable<string>)f.InterpretedFields)));
            CheckInterpretedFields(TalismansFile, talismans.Select(t => (t.Id, typeof(TalismanDefinition), (IEnumerable<string>)t.InterpretedFields)));
            CheckInterpretedFields(RegionsFile, regions.Select(r => (r.Id, typeof(RegionDefinition), (IEnumerable<string>)r.InterpretedFields)));
            Require(patrons.All(p => !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name) && p.Tribute >= 0 && p.BoonStrength >= 0
                    && p.MinClanRealm >= CultivationRealm.PurpleMansion && regions.Any(r => r.Id == p.RegionId) && p.InterpretedFields != null)
                && patrons.Select(p => p.Id).Distinct().Count() == patrons.Count,
                PatronsFile, "every great partner needs a unique id, a name, a place of the map, a tribute and a boon, and deals only with a clan of the Purple Mansion or above.");
            CheckInterpretedFields(PatronsFile, patrons.Select(p => (p.Id, typeof(PatronDefinition), (IEnumerable<string>)p.InterpretedFields)));
            CheckInterpretedFields(SecretsFile, secretKinds.Select(k => (k.Id, typeof(SecretKind), (IEnumerable<string>)k.InterpretedFields)));
            CheckInterpretedFields(AtmospheresFile, atmospheres.Select(a => (a.Id, typeof(AtmosphereDefinition), (IEnumerable<string>)a.InterpretedFields)));
            CheckInterpretedFields(QiFile, qi.Select(q => (q.Id, typeof(QiDefinition), (IEnumerable<string>)q.InterpretedFields)));
            CheckInterpretedFields(FruitionsFile, fruitions.Fruitions.Select(f => (f.Id, typeof(FruitionDefinition), (IEnumerable<string>)f.InterpretedFields))
                .Concat(fruitions.Fruitions.SelectMany(f => f.Abilities.Select(a => ($"{f.Id}:{a.Id}", typeof(DivineAbilityDefinition), (IEnumerable<string>)a.InterpretedFields)))));

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
                DeductionNames = catalog.DeductionNames,
                Fruitions = fruitions.Fruitions,
                AnonymousHolder = fruitions.AnonymousHolder,
                Oaths = oaths,
                Regions = regions,
                Talismans = talismans,
                Figures = figures,
                BeastSpecies = beasts,
                Atmospheres = atmospheres,
                SecretKinds = secretKinds,
                Patrons = patrons
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
            var world = balance.UnspecifiedFruitionOdds;
            Require(world != null && IsProbability(world.Free) && IsProbability(world.Occupied) && IsProbability(world.Broken)
                && Math.Abs(world.Free + world.Occupied + world.Broken - 1.0) < 1e-6,
                BalanceFile, "unspecifiedFruitionOdds (free, occupied, broken) must be probabilities summing to 1.");
            Require(balance.HeartAlignedSpeed > 0 && balance.HeartMisalignedSpeed > 0, BalanceFile, "the Dao Heart speeds must be positive.");
            Require(IsProbability(balance.StudyRevealsPartnersChance), BalanceFile, "studyRevealsPartnersChance must lie between 0 and 1.");
            Require(IsProbability(balance.HeartAlignmentYearlyChance) && IsProbability(balance.TemperamentInheritanceChance),
                BalanceFile, "the Dao Heart's alignment and inheritance chances must lie between 0 and 1.");
            var mansion = balance.PurpleMansion;
            Require(mansion != null && mansion.ManifestationYears > 0 && mansion.VoidBands != null
                && mansion.VoidBands.All(b => IsProbability(b.Chance) && b.MinYears >= 0 && b.MinYears <= b.MaxYears)
                && mansion.VoidBands.Sum(b => b.Chance) <= 1.0 + 1e-9
                && mansion.ManifestationPerGrade >= 0 && mansion.ManifestationPerTechnique >= 0 && mansion.ManifestationTechniqueCap >= 0,
                BalanceFile, "purpleMansion needs positive manifestation years and void bands whose chances sum to at most 1 (the rest: for life).");
            var oathCosts = balance.Oaths;
            Require(oathCosts != null && oathCosts.InterruptChanceBySeverity?.Count == 3 && oathCosts.InterruptChanceBySeverity.All(IsProbability)
                && oathCosts.HeartDemonYearsBySeverity?.Count == 3 && oathCosts.HeartDemonYearsBySeverity.All(y => y > 0)
                && IsProbability(oathCosts.DeviationChanceOnInterrupt) && IsProbability(oathCosts.PurificationChance)
                && oathCosts.HeartDemonSpeed > 0 && oathCosts.HeartDemonStabilityLoss >= 0 && oathCosts.PurificationHerbs >= 0 && oathCosts.MirrorVeilCost >= 0,
                BalanceFile, "oaths needs three interruption chances and Heart Demon years (severity 1-3), and its costs.");
            Require(balance.Diplomacy != null && balance.Diplomacy.NeighbourIntensity >= 1 && IsProbability(balance.Diplomacy.StealManualChance),
                BalanceFile, "diplomacy needs a neighbour intensity of at least 1 and a steal chance between 0 and 1.");
            var trade = balance.KnowledgeTrade;
            Require(trade != null && trade.StonesPerGrade?.Count == TechniqueRules.MaxGrade && trade.StonesPerGrade.All(p => p >= 0)
                && trade.DaoPartnersMirrorCost >= 0 && trade.MinRelation >= Diplomacy.FactionManager.MinRelation && trade.MinRelation <= Diplomacy.FactionManager.MaxRelation,
                BalanceFile, "knowledgeTrade needs a price per grade (7, never negative), a mirror cost and a relation within -100..100.");
            var plots = balance.Plots;
            Require(plots != null && plots.InvestigateThreshold >= 0 && plots.ActThreshold >= plots.InvestigateThreshold
                && plots.ProofThreshold > 0 && plots.EvidencePerFinding > 0 && plots.InvestigationChancePerPoint >= 0
                && plots.InvestigationBonusPerRealm >= 0 && plots.BoldnessRealmMargin >= 0 && plots.ReprisalRelation <= 0
                && IsProbability(plots.ReprisalStonesShare) && plots.WitnessDistrust >= 0 && plots.ProofReputation >= 0
                && IsProbability(plots.LeakBaseChance) && plots.LeakStabilityScale > 0 && IsProbability(plots.SwornLeakFactor)
                && plots.LeakMirrorClue >= 0 && plots.LeakEvidence >= 0
                && plots.BlurMirrorCost >= 0 && plots.BlurClues >= 0 && plots.BlurEvidence >= 0 && IsProbability(plots.BlurStrongFactor)
                && plots.FalseProofMirrorCost >= 0 && plots.FalseProofAmount >= 0 && plots.ConfrontationYears >= 1
                && plots.DoubtClues > 0 && plots.DoubtClues <= 100,
                BalanceFile, "plots needs thresholds in order (investigate <= act), positive proof, and odds and penalties in range.");
            var schemes = balance.Schemes;
            Require(schemes != null && IsProbability(schemes.BaseChance) && schemes.PersonalityFactors != null
                && Enum.GetValues(typeof(FactionPersonality)).Cast<FactionPersonality>().All(p => schemes.PersonalityFactors.TryGetValue(p, out var f) && f >= 0)
                && schemes.HostilityWeight >= 0 && schemes.FriendshipDamping >= 0 && schemes.WealthReference > 0 && schemes.MaxGreed >= 1
                && schemes.MaxAmbushesPerYear >= 0 && schemes.AwayTasks != null && IsProbability(schemes.AgentTakenChance) && schemes.PatienceYears >= 1
                && IsProbability(schemes.ExecutionChance) && schemes.InterrogationFactor >= 0 && schemes.RansomBase >= 0
                && schemes.RansomRealmFactor >= 1 && schemes.SilenceMirrorCost >= 0 && schemes.InterrogationFragments >= 0
                && schemes.FailedRescueSuspicion >= 0 && schemes.DenounceDistrust >= 0,
                BalanceFile, "schemes needs a factor for every personality, the tasks away from the domain, and odds and costs in range.");
            var treaty = balance.Treaties;
            var kinds = Enum.GetValues(typeof(TreatyKind)).Cast<TreatyKind>().ToList();
            Require(treaty != null && kinds.All(k => treaty.MinRelation.ContainsKey(k) && treaty.AcceptBase.ContainsKey(k))
                && IsProbability(treaty.BetrayalBase) && treaty.BetrayalTemper.Values.All(t => t >= 0) && treaty.SuspicionBetrayalWeight >= 0
                && treaty.StrongerBetrayalFactor >= 0 && treaty.SealedFactor >= 0 && treaty.SuzerainRealmMargin >= 0
                && IsProbability(treaty.BetrayalStonesShare) && IsProbability(treaty.SecretDiscoveryChance) && IsProbability(treaty.DefenceGuardChance)
                && IsProbability(treaty.VassalTributeShare) && IsProbability(treaty.GripStonesShare) && treaty.GripThreshold > 0
                && treaty.TradeDiscount > 0 && treaty.SealedHeartDemonYears >= 0,
                BalanceFile, "treaties needs a relation and a base for every kind, and odds and shares between 0 and 1.");
            var pat = balance.Patrons;
            Require(pat != null && pat.StartFavor > 0 && pat.MaxFavor >= pat.StartFavor && pat.UnpaidFavorLoss > 0 && pat.SafeEndFavor >= 0,
                BalanceFile, "patrons needs a starting favour, a ceiling above it, a loss for an unpaid tribute, and a safe favour to leave.");
            var deal = balance.Dealings;
            Require(deal != null && deal.BlackmailPriceByRank.Count == 4 && deal.ResentmentByRank.Count == 4 && deal.ExposeDistrustByRank.Count == 4
                && deal.SellPriceByRank.Count == 4 && deal.BlackmailPriceByRank.All(p => p >= 0) && deal.SellPriceByRank.All(p => p >= 0)
                && IsProbability(deal.SellLeakChance) && IsProbability(deal.AiExposeChance) && IsProbability(deal.ExposedEvidenceShare),
                BalanceFile, "dealings needs four prices, resentments and distrusts (one per rank), and odds between 0 and 1.");
            var watch = balance.ClanWatch;
            Require(watch != null && watch.FadePerYear >= 0 && watch.WaryFrom > 0 && watch.DistrustFrom >= watch.WaryFrom
                && watch.DeepDistrustFrom >= watch.DistrustFrom && watch.DeepDistrustFrom <= 100,
                BalanceFile, "clanWatch needs a fading never negative and signs in order (wary <= distrust <= deep distrust <= 100).");
            var sec = balance.Secrets;
            var approaches = Enum.GetValues(typeof(ProbeApproach)).Cast<ProbeApproach>().ToList();
            Require(sec != null && sec.RankThreshold.Count == 4 && sec.RankThreshold.All(t => t > 0) && sec.KnownEvidenceByRank.Count == 4
                && approaches.All(a => sec.BaseChance.ContainsKey(a) && sec.Gain.ContainsKey(a) && sec.DetectChance.ContainsKey(a) && sec.DisasterChance.ContainsKey(a))
                && sec.DetectChance.Values.All(IsProbability) && sec.DisasterChance.Values.All(IsProbability) && IsProbability(sec.PartnerLeakChance)
                && IsProbability(sec.AllyPromptBase) && IsProbability(sec.LateWitnessShare) && IsProbability(sec.AiProbeChance) && IsProbability(sec.AiPartnerChance)
                && sec.MinPercent >= 0 && sec.MaxPercent <= 100 && sec.MinPercent <= sec.MaxPercent && sec.BribeUnit > 0
                && sec.PowerSecretsMin >= 0 && sec.PowerSecretsMax >= sec.PowerSecretsMin,
                BalanceFile, "secrets needs four rank thresholds and proofs, every approach's odds, gain, detection and disaster, and odds between 0 and 1.");
            var intrigues = balance.Intrigues;
            Require(intrigues != null && IsProbability(intrigues.BlackmailChance) && IsProbability(intrigues.BlackmailStonesShare)
                && intrigues.QuietYears >= 0 && intrigues.RefusedSpreadEvidence >= 0 && IsProbability(intrigues.TheftChance)
                && intrigues.PatrolGuard >= 0 && intrigues.CatchPerPatrol >= 0 && IsProbability(intrigues.MaxCatchChance)
                && IsProbability(intrigues.TheftStonesShare) && intrigues.SpyChance.Values.All(IsProbability)
                && intrigues.BlackmailTemper.Values.All(t => t >= 0) && intrigues.TheftTemper.Values.All(t => t >= 0)
                && intrigues.UnmaskMirrorCost >= 0 && intrigues.DoubleAgentRelief >= 0,
                BalanceFile, "intrigues needs odds and shares between 0 and 1, and costs and tempers never negative.");
            var politics = balance.Politics;
            Require(politics != null && IsProbability(politics.AllianceChance) && politics.MaxBondsPerYear >= 0 && IsProbability(politics.FeudChance)
                && IsProbability(politics.FeudWealthShare) && IsProbability(politics.FeudPowerShare) && IsProbability(politics.VassalizeChance)
                && politics.VassalizePowerRatio >= 1 && IsProbability(politics.VassalTributeShare) && politics.GripThreshold > 0
                && politics.CoalitionMinMembers >= 1 && politics.CoalitionYears >= 1 && IsProbability(politics.CoalitionStonesShare)
                && politics.CoalitionSchemeFactor >= 1 && politics.CallStonesCost >= 0 && politics.ClanAbsorptionSteps >= 1,
                BalanceFile, "politics needs odds and shares between 0 and 1, a positive grip threshold, and at least one member, year and step.");
            var lore = balance.MirrorLore;
            Require(lore != null && lore.KnowChance != null && lore.PerCentury != null && lore.KnowChance.Values.All(IsProbability)
                && lore.PerCentury.Values.All(p => p >= 0) && IsProbability(lore.MaxChance) && lore.UnknownAge >= 0
                && IsProbability(lore.RumourRiseChance) && lore.TreasureSuspicion >= 0
                && lore.KnowChance.Keys.All(r => r >= CultivationRealm.GoldenCore),
                BalanceFile, "mirrorLore needs chances between 0 and 1, only for the Golden Core and above (a handful of elders know the mirror).");
            var place = balance.RegionalQi;
            Require(place != null && place.KindElements != null && place.KindDensity != null && place.LineageStatusFactors != null
                && Enum.GetValues(typeof(RegionKind)).Cast<RegionKind>().All(k => place.KindElements.ContainsKey(k) && place.KindDensity.TryGetValue(k, out var d) && d > 0)
                && Enum.GetValues(typeof(FruitionStatus)).Cast<FruitionStatus>().All(s => place.LineageStatusFactors.TryGetValue(s, out var f) && f >= 0)
                && place.EverywhereFamilies != null && place.AbsentQiFactor >= 0,
                BalanceFile, "regionalQi needs the elements and a positive density of every kind of place, and a factor for every lineage state.");
            var hunt = balance.Hunt;
            Require(hunt != null && hunt.TimingApproach?.Count == 3 && hunt.TimingCapture?.Count == 3 && hunt.CoverExposure?.Count == 4
                && hunt.CoverStones?.Count == 4 && hunt.CoverStones.All(c => c >= 0) && hunt.AidMirrorCost?.Count == 3 && hunt.AidMirrorCost.All(c => c >= 0)
                && hunt.MaxLookouts >= 0 && hunt.CapturePerPowerPoint >= 0 && hunt.SeenExposure >= 0 && hunt.CleanBaseExposure >= 0
                && IsProbability(hunt.FrameBaseChance) && IsProbability(hunt.DeathChance) && hunt.FailedFrameBacklash >= 0,
                BalanceFile, "hunt needs three timing entries, four cover entries, three aid costs, and odds and costs never negative.");
            var bestiary = balance.Bestiary;
            Require(bestiary != null && bestiary.BeastsByKind != null && bestiary.BeastsByKind.Values.All(n => n >= 0)
                && IsProbability(bestiary.OwnedShare) && IsProbability(bestiary.ScoutRevealChance)
                && bestiary.RealmOdds?.Count == 3 && bestiary.RealmOdds.All(IsProbability) && Math.Abs(bestiary.RealmOdds.Sum() - 1.0) < 1e-6,
                BalanceFile, "bestiary needs beasts per kind, an owned share, a scouting chance and three realm odds summing to 1.");
            var talismanRitual = balance.Talismans;
            Require(talismanRitual != null && talismanRitual.RitualPeriodYears >= 1 && talismanRitual.HuntWindowYears >= 0
                && talismanRitual.HuntWindowYears < talismanRitual.RitualPeriodYears,
                BalanceFile, "talismans needs a ritual period of a year or more and a hunt window shorter than it.");
            Require(talismanRitual.PrayersPerRitual > 0 && talismanRitual.PrayersPerMortalPerYear >= 0
                && talismanRitual.PrayersPerPrestigePerYear >= 0 && talismanRitual.OfferRootThresholds?.Count == 2
                && talismanRitual.OfferRootThresholds[0] <= talismanRitual.OfferRootThresholds[1]
                && talismanRitual.GreyStageLeap >= 0 && talismanRitual.WhiteStageLeap >= 0
                && talismanRitual.BeastStagesPerExtraLeap >= 1
                && IsProbability(talismanRitual.OwnedBeastChance) && IsProbability(talismanRitual.OwnedBeastDiscoveryChance)
                && talismanRitual.OwnedBeastRelationPenalty <= 0,
                BalanceFile, "talismans needs positive prayers per ritual, two ordered root thresholds, leaps never negative, beast stages per leap of 1+ and a hunt chance.");
            var trials = balance.Trials;
            Require(trials != null && trials.ChakraChances != null && trials.ChakraChances.Values.All(c => c >= 0 && c <= 100)
                && new[] { trials.FoundationWallBaseChance, trials.MinimumTrialChance, trials.DissolutionBaseChance, trials.MaximumDissolutionChance }
                    .All(c => c >= 0 && c <= 100)
                && trials.FoundationAdvisedAge >= 0 && trials.FoundationWallLossPerYear >= 0 && trials.DissolutionChancePerYear >= 0
                && trials.ChakraMinimumRoot >= 0 && trials.WallMinimumRoot >= 0 && trials.OldAgeLifespanRatio > 0 && trials.OldAgePenalty >= 0
                && trials.MinorFailureMaxRoll >= 0 && trials.MinorFailureMaxRoll <= trials.MajorFailureMaxRoll && trials.MajorFailureMaxRoll <= 100
                && trials.BaseTalismanSeedCapacity >= 0,
                BalanceFile, "trials needs chances of 0-100 %, values never negative and a deviation table in order (minor <= major <= 100).");
            var modifiers = balance.TrialModifiers;
            Require(modifiers != null && modifiers.RootPointsPerPercent > 0 && modifiers.StabilityPointsPerPercent > 0 && modifiers.LowStabilityPenalty >= 0
                && modifiers.ReferenceGrade >= TechniqueRules.MinGrade && modifiers.ReferenceGrade <= TechniqueRules.MaxGrade,
                BalanceFile, "trialModifiers needs positive points per percent, a penalty never negative and a reference grade 1-7.");
            var abilities = balance.DivineAbilities;
            Require(abilities != null && abilities.ResourceStones >= 0 && abilities.ResourceHerbs >= 0 && abilities.ResourceOres >= 0 && abilities.GraftOres >= 0,
                BalanceFile, "divineAbilities needs its costs (never negative).");
            Require(IsProbability(balance.RipeDaoHuntChance) && IsProbability(balance.RipeDaoGuardedFactor),
                BalanceFile, "ripeDaoHuntChance and ripeDaoGuardedFactor lie between 0 and 1.");
            Require(abilities.ImageryXpFactor > 0 && abilities.ImageryXpFactor <= 1 && IsProbability(balance.BodyTraitInheritanceChance),
                BalanceFile, "the imagery factor lies in ]0, 1] and bodyTraitInheritanceChance between 0 and 1.");
            Require(abilities.GraftDonorMinYearsLeft >= 1 && abilities.GraftDonorMinYearsLeft <= abilities.GraftDonorMaxYearsLeft,
                BalanceFile, "a grafted donor's years left need 1 <= min <= max.");
            var core = balance.GoldenCore;
            Require(core != null && new[] { core.ForgeBaseChance, core.RealizationChance, core.SurplusChance, core.IntercalaryFourOneChance, core.IntercalaryThreeTwoChance,
                        core.TrueLeftHandChance, core.FalseLeftHandChance, core.TransferChance, core.TransformationChance }
                    .All(c => c >= 0 && c <= 100)
                && core.ShallowAbilityPenalty >= 0 && core.GraftedAbilityPenalty >= 0 && core.LifeLastBonus >= 0 && core.AxiomPenalty >= 0
                && core.PermissionStones >= 0 && IsProbability(core.PermissionChance) && core.GoldSeekingMirrorCost >= 0 && core.SpecialisedMirrorCost >= 0
                && core.LeftHandMirrorCost >= 0 && core.FalseLeftHandYearlyStones >= 0 && core.LightBorrowingYearlyStones >= 0
                && IsProbability(core.ReclaimChance) && core.ImagePointsPerYear > 0 && core.ImageToNextStage?.Count == 3 && core.ImageToNextStage.All(p => p > 0)
                && core.FalseLeftHandMinAbilities >= 1 && core.FalseLeftHandMinAbilities <= GoldenCoreRules.AbilitiesToForge,
                BalanceFile, "goldenCore needs chances of 0-100 %, penalties and costs never negative, a permission chance between 0 and 1, 1-5 abilities for a false Left Hand.");
            var rules = balance.Techniques;
            Require(rules != null && rules.CommonBreathingSpeed > 0 && rules.QiPortionsToEnter >= 0 && rules.FoundationQiPortions >= 0
                && rules.AlignedQiPortions >= 0 && rules.LowestGradeChosenForAMember >= TechniqueRules.MinGrade
                && rules.ArtRequiredRealmByGrade?.Count == TechniqueRules.MaxGrade
                && rules.MovementArtStepsByGrade?.Count == TechniqueRules.MaxGrade && rules.MovementArtStepsByGrade.All(s => s >= 0)
                && rules.DeductionCompleteFragments >= 2 && rules.DeductionMaxGrade >= TechniqueRules.MinGrade && rules.DeductionMaxGrade <= TechniqueRules.MaxGrade,
                BalanceFile, "techniques needs its rules: positive speed, portions, a grade per list entry (7), deduction bounds.");
        }

        /// <summary>The powers (L5): unique names, each on the map, holding only techniques of the catalog.</summary>
        private static void CheckFactions(List<FactionData> factions, List<RegionDefinition> regions, IReadOnlyList<TechniqueData> techniques)
        {
            Require(factions.All(f => !string.IsNullOrWhiteSpace(f.Name)), FactionsFile, "every faction needs a name.");
            var duplicate = factions.GroupBy(f => f.Name).FirstOrDefault(g => g.Count() > 1)?.Key;
            Require(duplicate == null, FactionsFile, $"two factions share the name \"{duplicate}\".");
            var listless = factions.FirstOrDefault(f => f.Techniques == null || f.InterpretedFields == null);
            Require(listless == null, FactionsFile, $"{listless?.Name}: techniques and interpretedFields must be lists (empty when none).");
            var lost = factions.FirstOrDefault(f => regions.All(r => r.Id != f.RegionId));
            Require(lost == null, FactionsFile, $"{lost?.Name}: its region \"{lost?.RegionId}\" is not a region of {RegionsFile}.");
            var unknown = factions.SelectMany(f => f.Techniques.Select(t => (f.Name, Technique: t)))
                .FirstOrDefault(p => techniques.All(t => t.ID != p.Technique));
            Require(unknown.Technique == null, FactionsFile, $"{unknown.Name}: \"{unknown.Technique}\" is not a technique of {TechniquesFile}.");
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
                Require(t.Kind != TechniqueKind.Movement || t.MovementSteps > 0, TechniquesFile, $"{t.ID}: a movement art needs its movementSteps.");

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

            var badFact = (clan.Knowledge ?? Array.Empty<string>()).FirstOrDefault(key => !World.Fact.TryParse(key, out _));
            Require(badFact == null, ClanFile, $"the starting fact \"{badFact}\" is not « Kind:Subject » of a known kind ({string.Join(", ", Enum.GetNames(typeof(World.FactKind)))}).");

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

        /// <summary>The lineages (LORE.md §6): unique, with their orthodox five, and a holder exactly where a status implies one.</summary>
        private static void CheckFruitions(FruitionCatalog catalog)
        {
            var fruitions = catalog.Fruitions;
            Require(!string.IsNullOrWhiteSpace(catalog.AnonymousHolder), FruitionsFile, "anonymousHolder names the True Monarchs the lore does not name.");
            Require(fruitions != null && fruitions.Count > 0 && fruitions.All(f => !string.IsNullOrWhiteSpace(f.Id) && !string.IsNullOrWhiteSpace(f.Name)),
                FruitionsFile, "every lineage needs an id and a name.");
            Require(fruitions.Select(f => f.Id).Distinct().Count() == fruitions.Count, FruitionsFile, "two lineages share an id.");

            foreach (var f in fruitions)
            {
                var abilities = f.Abilities ?? Array.Empty<DivineAbilityDefinition>();
                Require(abilities.All(a => !string.IsNullOrWhiteSpace(a.Id)) && abilities.Select(a => a.Id).Distinct().Count() == abilities.Count,
                    FruitionsFile, $"{f.Id}: every ability needs its own id.");
                Require(abilities.Count(a => !a.Substitute) >= OrthodoxAbilities, FruitionsFile,
                    $"{f.Id}: a lineage has five orthodox abilities (name them null while the world has not revealed them).");

                bool held = f.Status == FruitionStatus.Occupied || f.Status == FruitionStatus.Suspected;
                bool empty = f.Status == FruitionStatus.Free || f.Status == FruitionStatus.Broken || f.Status == FruitionStatus.Unspecified;
                Require(!held || !string.IsNullOrWhiteSpace(f.Holder), FruitionsFile, $"{f.Id}: an {f.Status} lineage needs its holder.");
                Require(!empty || f.Holder == null, FruitionsFile, $"{f.Id}: a {f.Status} lineage has no holder (former holders go in formerHolders).");
            }
        }

        /// <summary>Each Qi names a foundation that exists, and every Qi of a method reaching the Foundation names one (§5.3.1).</summary>
        private static void CheckFoundations(List<QiDefinition> qi, IReadOnlyList<TechniqueData> techniques, IReadOnlyList<FruitionDefinition> fruitions)
        {
            foreach (var q in qi.Where(q => q.Foundation != null))
            {
                var (fruitionId, abilityId) = FoundationRef.Parse(q.Foundation);
                var fruition = fruitions.FirstOrDefault(f => f.Id == fruitionId);
                Require(fruition != null && fruition.Abilities.Any(a => a.Id == abilityId), QiFile,
                    $"{q.Id}: the foundation \"{q.Foundation}\" is not an ability of {FruitionsFile}.");
            }

            var reachingFoundation = techniques.Where(t => TechniqueRules.Covers(t, CultivationRealm.Foundation) && t.RequiredQiId != null)
                .Select(t => t.RequiredQiId).Distinct();
            var missing = reachingFoundation.FirstOrDefault(id => qi.First(q => q.Id == id).Foundation == null);
            Require(missing == null, QiFile, $"{missing}: its methods reach the Foundation, so it must name the foundation it builds.");
        }

        /// <summary>The powers' figures: unique ids, a name, a power of factions.json, never above that power's highest realm.</summary>
        private static void CheckFigures(List<FigureDefinition> figures, List<FactionData> factions)
        {
            Require(figures.All(f => !string.IsNullOrWhiteSpace(f.Id) && !string.IsNullOrWhiteSpace(f.Name) && f.InterpretedFields != null),
                FiguresFile, "every figure needs an id, a name and a list of interpreted fields.");
            var duplicate = figures.GroupBy(f => f.Id).FirstOrDefault(g => g.Count() > 1)?.Key;
            Require(duplicate == null, FiguresFile, $"two figures share the id \"{duplicate}\".");
            foreach (var figure in figures)
            {
                var power = factions.FirstOrDefault(f => f.Name == figure.FactionName);
                Require(power != null, FiguresFile, $"{figure.Id}: \"{figure.FactionName}\" is not a faction of {FactionsFile}.");
                Require(figure.Realm <= power.HighestRealm, FiguresFile, $"{figure.Id}: above the highest realm of {power.Name}.");
            }
        }

        /// <summary>The talisman Qi (§11.5): unique ids, a name, a positive speed, no lost years.</summary>
        private static void CheckTalismans(List<TalismanDefinition> talismans)
        {
            Require(talismans.All(t => !string.IsNullOrWhiteSpace(t.Id) && !string.IsNullOrWhiteSpace(t.Name)), TalismansFile, "every talisman needs an id and a name.");
            var duplicate = talismans.GroupBy(t => t.Id).FirstOrDefault(g => g.Count() > 1)?.Key;
            Require(duplicate == null, TalismansFile, $"two talismans share the id \"{duplicate}\".");
            var wrong = talismans.FirstOrDefault(t => t.CultivationSpeed <= 0 || t.LifespanYears < 0 || t.IllusionsBonus < 0 || t.OffspringRootBonus < 0
                || t.Temperaments == null || t.Traits == null || t.InterpretedFields == null);
            Require(wrong == null, TalismansFile, $"{wrong?.Id}: a talisman needs a positive speed, figures never negative and lists (empty when none).");
        }

        /// <summary>The map (L5): unique places with a name, on the map, whose parent and neighbours exist and answer back.</summary>
        private static void CheckRegions(List<RegionDefinition> regions)
        {
            Require(regions.All(r => !string.IsNullOrWhiteSpace(r.Id) && !string.IsNullOrWhiteSpace(r.Name)), RegionsFile, "every region needs an id and a name.");
            var listless = regions.FirstOrDefault(r => r.Neighbours == null || r.InterpretedFields == null);
            Require(listless == null, RegionsFile, $"{listless?.Id}: neighbours and interpretedFields must be lists (empty when none).");
            var duplicate = regions.GroupBy(r => r.Id).FirstOrDefault(g => g.Count() > 1)?.Key;
            Require(duplicate == null, RegionsFile, $"two regions share the id \"{duplicate}\".");
            var byId = regions.ToDictionary(r => r.Id);
            foreach (var r in regions)
            {
                Require(r.X >= 0 && r.X <= 1 && r.Y >= 0 && r.Y <= 1, RegionsFile, $"{r.Id}: its position must lie within the map (0-1).");
                Require(r.ParentId == null || byId.ContainsKey(r.ParentId), RegionsFile, $"{r.Id}: its parent \"{r.ParentId}\" is not a region.");
                Require(r.ParentId == null || byId[r.ParentId].ParentId == null, RegionsFile, $"{r.Id}: its parent \"{r.ParentId}\" must be a state or a sea.");
                foreach (var n in r.Neighbours)
                {
                    Require(n != r.Id, RegionsFile, $"{r.Id} cannot border itself.");
                    Require(byId.ContainsKey(n), RegionsFile, $"{r.Id}: its neighbour \"{n}\" is not a region.");
                    Require(byId[n].Neighbours?.Contains(r.Id) == true, RegionsFile, $"{r.Id} borders {n}, but {n} does not border {r.Id}.");
                }
            }
        }

        /// <summary>Atmospheres (L5b): unique ids, names, lists, and favoured lineages that exist.</summary>
        private static void CheckAtmospheres(List<AtmosphereDefinition> atmospheres, IReadOnlyList<FruitionDefinition> fruitions)
        {
            Require(atmospheres.All(a => !string.IsNullOrWhiteSpace(a.Id) && !string.IsNullOrWhiteSpace(a.Name)
                    && a.FavouredFruitions != null && a.FavouredElements != null && a.FavouredPaths != null && a.InterpretedFields != null)
                && atmospheres.Select(a => a.Id).Distinct().Count() == atmospheres.Count,
                AtmospheresFile, "every atmosphere needs a unique id, a name and lists (empty when none).");
            var unknown = atmospheres.SelectMany(a => a.FavouredFruitions.Select(f => (a.Id, f))).FirstOrDefault(p => fruitions.All(f => f.Id != p.f));
            Require(unknown.f == null, AtmospheresFile, $"{unknown.Id}: the lineage \"{unknown.f}\" is not in {FruitionsFile}.");
        }

        /// <summary>The Qi of the places (L5b): the Qi they name exist, their atmosphere exists, their density is positive.</summary>
        private static void CheckRegionalQi(List<RegionDefinition> regions, IReadOnlyList<QiDefinition> qi, List<AtmosphereDefinition> atmospheres)
        {
            foreach (var r in regions)
            {
                var missing = r.Qi?.FirstOrDefault(id => qi.All(q => q.Id != id));
                Require(missing == null, RegionsFile, $"{r.Id}: the Qi \"{missing}\" is not in {QiFile}.");
                Require(r.AtmosphereId == null || atmospheres.Any(a => a.Id == r.AtmosphereId), RegionsFile,
                    $"{r.Id}: the atmosphere \"{r.AtmosphereId}\" is not in {AtmospheresFile}.");
                Require(r.QiDensity == null || r.QiDensity > 0, RegionsFile, $"{r.Id}: its Qi density must be positive.");
            }
        }

        /// <summary>Oaths (L4d): unique clauses with a name and a severity 1-3.</summary>
        private static void CheckOaths(OathCatalog oaths)
        {
            var clauses = oaths.Clauses ?? Array.Empty<ClauseDefinition>();
            Require(clauses.Count > 0 && clauses.All(c => !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Name)),
                OathsFile, "every clause needs an id and a name.");
            Require(clauses.Select(c => c.Id).Distinct().Count() == clauses.Count, OathsFile, "two clauses share an id.");
            var light = clauses.FirstOrDefault(c => c.Severity < 1 || c.Severity > 3);
            Require(light == null, OathsFile, $"{light?.Id}: a clause's severity lies between 1 and 3.");
            var loopholes = oaths.Loopholes ?? Array.Empty<LoopholeDefinition>();
            Require(loopholes.Count > 0 && loopholes.All(l => !string.IsNullOrWhiteSpace(l.Name)) && loopholes.Select(l => l.Kind).Distinct().Count() == loopholes.Count,
                OathsFile, "nothing is perfect: declare the loopholes, each named once.");
        }

        /// <summary>An interpreted field must name a real field, so the gaps report points at something to replace.</summary>
        private static void CheckInterpretedFields(string file, IEnumerable<(string Id, Type Type, IEnumerable<string> Fields)> items)
        {
            foreach (var (id, type, fields) in items)
            {
                var unknown = (fields ?? Enumerable.Empty<string>()).FirstOrDefault(field =>
                    type.GetProperty(field, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase) == null);
                Require(unknown == null, file, $"{id}: \"{unknown}\" is not a field of {type.Name}, so it cannot be an interpreted field.");
            }
        }

        private static bool IsProbability(double value) => value >= 0 && value <= 1;

        private static void Require(bool condition, string file, string problem)
        {
            if (!condition) throw new InvalidDataException($"{file}: {problem}");
        }
    }
}
