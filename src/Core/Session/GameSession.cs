using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Mirror;
using MirrorChronicles.World;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// One game: every system wired in a fixed order, and the yearly turn.
    /// <para>
    /// Phases run in this order. Entering Events (the Management phase is over): tasks, faction AI,
    /// the random event, annual marriages. Breakthrough: every ready member attempts their trial.
    /// Inheritance: births, then orifice examinations. A new year: aging, building bonuses, then
    /// <c>OnYearStarted</c> (succession, mirror recharge, victory).
    /// </para>
    /// Reactions to events run in construction order (see <see cref="GameEventBus"/>).
    /// </summary>
    public sealed class GameSession
    {
        public int Seed { get; }
        public GameContext Context { get; }
        public GameEventBus Events => Context.Events;
        public IGameLog Log => Context.Log;
        public GameClock Clock => Context.Clock;

        public ClanManager Clan { get; }
        public ResourceManager Resources { get; }
        public MentalStabilitySystem Stability { get; }
        public ClanKarmaSystem Karma { get; }
        public KnowledgeBase Knowledge { get; }
        public TechniqueLibrary Techniques { get; }
        public CultivationSystem Cultivation { get; }
        public BreakthroughSystem Breakthroughs { get; }
        public FoundationSystem Foundations { get; }
        public PurpleMansionSystem PurpleMansion { get; }
        public DivineAbilitySystem Abilities { get; }
        public AgingSystem Aging { get; }
        public WoundSystem Wounds { get; }
        public FactionManager Factions { get; }
        public FruitionRegistry Fruitions { get; }
        public MirrorSystem Mirror { get; }
        public DeductionEngine Deduction { get; }
        public BuildingSystem Buildings { get; }
        public AllianceSystem Alliances { get; }
        public EspionageSystem Espionage { get; }
        public TaskAssignmentSystem Tasks { get; }
        public MarriageSystem Marriages { get; }
        public EventManager RandomEvents { get; }
        public LegacySystem Legacy { get; }
        public AscensionSystem Ascension { get; }
        public StoryEventManager Story { get; }
        public VictoryConditionSystem Victory { get; }

        private GameSession(int seed, Random rng, string clanName, GameSetup setup)
        {
            Seed = seed;
            Context = new GameContext(new GameEventBus(), setup.Log ?? new NullGameLog(), rng, new GameClock(), setup.Content);

            // Construction order = reaction order: stability and karma react to a death before the
            // legacy is paid, the story notices, and victory is judged last.
            Clan = new ClanManager(Context, clanName);
            Resources = new ResourceManager(Context);
            Stability = new MentalStabilitySystem(Context, Clan);
            Karma = new ClanKarmaSystem(Context, Clan);
            Knowledge = WorldKnowledge.Create(Context.Content);
            Techniques = new TechniqueLibrary(Context, Knowledge);
            Cultivation = new CultivationSystem(Context, Karma, Techniques, Resources);
            Breakthroughs = new BreakthroughSystem(Context, Clan, Cultivation);
            Foundations = new FoundationSystem(Context, Clan, Techniques);
            PurpleMansion = new PurpleMansionSystem(Context, Clan, Cultivation, Techniques);
            Abilities = new DivineAbilitySystem(Context, Clan, Techniques, Resources);
            Aging = new AgingSystem(Context, Clan);
            Wounds = new WoundSystem(Context, Stability);
            Factions = new FactionManager(Context);
            Fruitions = new FruitionRegistry(Context);
            Mirror = new MirrorSystem(Context, Clan, Breakthroughs);
            Deduction = new DeductionEngine(Context, Mirror, Techniques);
            Buildings = new BuildingSystem(Context, Clan, Resources, Stability, Cultivation);
            Alliances = new AllianceSystem(Context, Factions, Resources);
            Espionage = new EspionageSystem(Context, Factions, Deduction, Stability);
            Tasks = new TaskAssignmentSystem(Context, Clan, Cultivation, Resources, Stability, Factions, Deduction, Espionage, Buildings, Techniques);
            Marriages = new MarriageSystem(Context, Clan, Factions, Stability);
            RandomEvents = new EventManager(Context, Clan, Factions, Deduction, Resources, Stability, Buildings);
            Legacy = new LegacySystem(Context, Clan, Resources, Deduction);
            Ascension = new AscensionSystem(Context, Clan);
            Story = new StoryEventManager(Context, Clan, Resources, Stability, Factions);
            Victory = new VictoryConditionSystem(Context, Clan, Karma, Ascension);
        }

        /// <summary>A new game: the clan's knowledge and Qi, the founders, the known world and its lineages, the mirror's first two fragments.</summary>
        public static GameSession NewGame(GameSetup setup)
        {
            var content = RequireContent(setup);
            var session = new GameSession(setup.Seed, new Random(setup.Seed), content.Clan.ClanName, setup);

            foreach (var id in content.Clan.StartingTechniques) session.Techniques.Learn(id);
            foreach (var key in content.Clan.Knowledge) session.Knowledge.Reveal(Fact.Parse(key), KnowledgeSource.Start);
            foreach (var (qi, portions) in content.Clan.StartingQi) session.Resources.AddQi(qi, portions);
            FoundingClan.Found(session.Clan, content.Clan, session.Techniques, session.Context.Rng);
            session.Karma.Restore(1, 0, 0, session.Clan.PatriarchID);
            session.Factions.InitializeFactions();
            session.Fruitions.DrawWorld(FruitionRegistry.WorldRandom(setup.Seed));
            session.Deduction.AddFragment(Element.Fire, 1, "Scorched Scroll");
            session.Deduction.AddFragment(Element.Wood, 1, "Bamboo Slip");

            session.Log.Info($"[Session] The {session.Clan.ClanName} clan begins its story (seed {setup.Seed}).");
            return session;
        }

        /// <summary>
        /// Rebuilds a game from a save, bringing older saves up to date (stages, orifices, lifespans).
        /// The random stream restarts from the seed, year and phase: reproducible, not a continuation.
        /// </summary>
        public static GameSession FromSaveData(GameData data, GameSetup setup)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var content = RequireContent(setup);

            var rng = new Random(unchecked(data.Seed * 31 + data.CurrentYear * 4 + (int)data.CurrentPhase));
            var session = new GameSession(data.Seed, rng, data.ClanName ?? content.Clan.ClanName, setup);

            // Work on copies: the caller keeps its GameData, and two loads never share objects
            var records = (data.HistoricalRecords ?? new List<CharacterData>()).Select(r => r.Clone()).ToList();
            foreach (var record in records)
            {
                PowerLadder.Normalize(record);            // saves made before realm stages
                SpiritualOrificeRules.Normalize(record);  // saves made before orifices
                PowerLadder.NormalizeLifespan(record);    // keeps Dao wounds and mortal rolls
            }

            session.Clock.Restore(Math.Max(1, data.CurrentYear), data.CurrentPhase);
            session.Clan.Restore(records, data.PatriarchID);
            session.Resources.Restore(data.SpiritStones, data.MedicinalHerbs, data.SpiritualOres, data.Prestige, data.TechniqueFragments);
            session.Mirror.Restore(data.MirrorPower, data.RestoredFragments);
            session.Karma.Restore(data.GenerationCount, data.TotalBirths, data.TotalDeaths, data.LastPatriarchId ?? session.Clan.PatriarchID);
            session.Ascension.Restore(data.AscendedAncestors);
            if (data.Buildings != null) session.Buildings.Restore(data.Buildings);
            if (data.Factions != null && data.Factions.Count > 0) session.Factions.Restore(data.Factions.Select(f => f.Clone()));
            else session.Factions.InitializeFactions();
            session.Deduction.Restore((data.Fragments ?? new List<FragmentData>()).Select(f => f.Clone()));
            session.Knowledge.Restore(data.Knowledge);
            session.Techniques.Restore(
                data.Knowledge != null ? null : data.KnownTechniqueIds ?? content.Clan.StartingTechniques, // saves made before the knowledge module
                (data.Techniques ?? new List<TechniqueData>()).Select(t => t.Clone()));
            session.Resources.RestoreQi(data.SpiritualQi ?? content.Clan.StartingQi, data.QiHarvestProgress);
            foreach (var record in records)
            {
                session.Techniques.NormalizeMember(record);                  // a Qi cultivator practises a method
                if (data.Knowledge == null)                                 // saves made before the knowledge module
                {
                    session.Knowledge.Reveal(FactKind.Ability, record.FoundationId, KnowledgeSource.OlderSave);
                    foreach (var ability in record.DivineAbilities ?? new List<string>())
                        session.Knowledge.Reveal(FactKind.Ability, ability, KnowledgeSource.OlderSave);
                }
                if (record.Temperament == Temperament.None)                  // saves made before the Dao Heart
                    record.Temperament = FoundationRules.RandomTemperament(rng);
            }
            session.Fruitions.Restore(data.FruitionStates, FruitionRegistry.WorldRandom(data.Seed)); // older saves: the world their seed draws
            session.Story.Restore(data.TriggeredStoryEvents ?? new List<StoryTriggerType>(), data.PendingStoryEvents ?? new List<StoryTriggerType>());
            session.Victory.Restore(data.GameWon, data.GameLost);

            session.Log.Info($"[Session] The {session.Clan.ClanName} clan resumes in year {session.Clock.Year}.");
            return session;
        }

        /// <summary>A detached snapshot: later play never changes it, so it can be kept or written later.</summary>
        public GameData ToSaveData()
        {
            return new GameData
            {
                Seed = Seed,
                ClanName = Clan.ClanName,
                CurrentYear = Clock.Year,
                CurrentPhase = Clock.Phase,
                PatriarchID = Clan.PatriarchID,
                HistoricalRecords = Clan.Registry.Records.Select(r => r.Clone()).ToList(),
                SpiritStones = Resources.SpiritStones,
                MedicinalHerbs = Resources.MedicinalHerbs,
                SpiritualOres = Resources.SpiritualOres,
                Prestige = Resources.Prestige,
                TechniqueFragments = Resources.TechniqueFragments,
                MirrorPower = Mirror.MirrorPower,
                RestoredFragments = Mirror.RestoredFragments,
                Fragments = Deduction.Fragments.Select(f => f.Clone()).ToList(),
                Techniques = Techniques.Deduced.Select(t => t.Clone()).ToList(),
                Knowledge = Knowledge.Keys.ToList(), // the known techniques live there since 2.3
                SpiritualQi = new Dictionary<string, int>(Resources.SpiritualQi),
                QiHarvestProgress = new Dictionary<string, int>(Resources.QiHarvestProgress),
                FruitionStates = new Dictionary<string, FruitionState>(Fruitions.States),
                GenerationCount = Karma.GenerationCount,
                TotalBirths = Karma.TotalBirths,
                TotalDeaths = Karma.TotalDeaths,
                LastPatriarchId = Karma.LastPatriarchId,
                AscendedAncestors = Ascension.AscendedAncestorsCount,
                Buildings = Buildings.Buildings.Select(b => new BuildingData(b.Type) { Level = b.Level }).ToList(),
                Factions = Factions.Factions.Select(f => f.Clone()).ToList(),
                TriggeredStoryEvents = Story.TriggeredEvents.ToList(),
                PendingStoryEvents = Story.PendingTriggers.ToList(),
                GameWon = Victory.GameWon,
                GameLost = Victory.GameLost
            };
        }

        private static GameContent RequireContent(GameSetup setup) =>
            setup?.Content ?? throw new ArgumentException("GameSetup.Content is required: load it with GameContentLoader.", nameof(setup));

        /// <summary>Moves to the next phase and resolves it. Does nothing once the game is over.</summary>
        public void AdvancePhase()
        {
            if (Victory.IsOver) return;

            if (Clock.Advance()) BeginYear();
            else ResolvePhase(Clock.Phase);

            Events.TriggerPhaseChanged(Clock.Phase);
        }

        /// <summary>Plays the rest of the current year, up to the Management phase of the next one.</summary>
        public void AdvanceYear()
        {
            int year = Clock.Year;
            while (Clock.Year == year && !Victory.IsOver)
                AdvancePhase();
        }

        private void ResolvePhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Events: // the Management phase is over
                    Tasks.ProcessYearlyTasks();
                    Factions.ProcessYearlyFactionAI();
                    RandomEvents.TriggerYearlyEvent();
                    Marriages.ProcessAnnualMarriages(); // before Inheritance, so newlyweds can have children
                    break;
                case GamePhase.Breakthrough:
                    Breakthroughs.ProcessBreakthroughPhase();
                    PurpleMansion.ProcessBreakthroughPhase(); // the ascent's four trials and the retreats under way
                    Abilities.ProcessBreakthroughPhase();     // divine abilities condensed from the Dao Partners
                    break;
                case GamePhase.Inheritance:
                    Clan.ProcessAnnualBirths();
                    Clan.ExamineOrifices();
                    break;
            }
        }

        private void BeginYear()
        {
            Aging.AgeOneYear();
            Buildings.ApplyPassiveBonuses();
            Events.TriggerYearStarted(Clock.Year);
        }
    }
}
