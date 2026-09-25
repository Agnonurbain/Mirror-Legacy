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
        /// <summary>The player's clan (LORE.md §13.1); moves to data in phase G2.</summary>
        public const string DefaultClanName = "Mo";

        public int Seed { get; }
        public GameContext Context { get; }
        public GameEventBus Events => Context.Events;
        public IGameLog Log => Context.Log;
        public GameClock Clock => Context.Clock;

        public ClanManager Clan { get; }
        public ResourceManager Resources { get; }
        public MentalStabilitySystem Stability { get; }
        public ClanKarmaSystem Karma { get; }
        public CultivationSystem Cultivation { get; }
        public BreakthroughSystem Breakthroughs { get; }
        public AgingSystem Aging { get; }
        public WoundSystem Wounds { get; }
        public FactionManager Factions { get; }
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
            Context = new GameContext(new GameEventBus(), setup.Log ?? new NullGameLog(), rng, new GameClock());

            // Construction order = reaction order: stability and karma react to a death before the
            // legacy is paid, the story notices, and victory is judged last.
            Clan = new ClanManager(Context, clanName);
            Resources = new ResourceManager(Context);
            Stability = new MentalStabilitySystem(Context, Clan);
            Karma = new ClanKarmaSystem(Context, Clan);
            Cultivation = new CultivationSystem(Context, Karma);
            Breakthroughs = new BreakthroughSystem(Context, Clan, Cultivation);
            Aging = new AgingSystem(Context, Clan);
            Wounds = new WoundSystem(Context, Stability);
            Factions = new FactionManager(Context);
            Mirror = new MirrorSystem(Context, Clan, Breakthroughs);
            Deduction = new DeductionEngine(Context, Mirror);
            Buildings = new BuildingSystem(Context, Clan, Resources, Stability, Cultivation);
            Alliances = new AllianceSystem(Context, Factions, Resources);
            Espionage = new EspionageSystem(Context, Factions, Deduction, Stability);
            Tasks = new TaskAssignmentSystem(Context, Clan, Cultivation, Resources, Stability, Factions, Deduction, Espionage, Buildings);
            Marriages = new MarriageSystem(Context, Clan, Factions, Stability);
            RandomEvents = new EventManager(Context, Clan, Factions, Deduction, Resources, Stability, Buildings, setup.RandomEvents);
            Legacy = new LegacySystem(Context, Clan, Resources, Deduction);
            Ascension = new AscensionSystem(Context, Clan);
            Story = new StoryEventManager(Context, Clan, Resources, Stability, Factions, setup.StoryEvents);
            Victory = new VictoryConditionSystem(Context, Clan, Karma, Ascension);
        }

        /// <summary>A new game: the founders, the known world and the mirror's first two fragments.</summary>
        public static GameSession NewGame(GameSetup setup)
        {
            var session = new GameSession(setup.Seed, new Random(setup.Seed), setup.ClanName ?? DefaultClanName, setup);

            FoundingClan.Found(session.Clan, session.Context.Rng);
            session.Karma.Restore(1, 0, 0, session.Clan.PatriarchID);
            session.Factions.InitializeDefaultFactions();
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

            var rng = new Random(unchecked(data.Seed * 31 + data.CurrentYear * 4 + (int)data.CurrentPhase));
            var session = new GameSession(data.Seed, rng, data.ClanName ?? DefaultClanName, setup);

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
            else session.Factions.InitializeDefaultFactions();
            session.Deduction.Restore(
                (data.Fragments ?? new List<FragmentData>()).Select(f => f.Clone()),
                (data.Techniques ?? new List<TechniqueData>()).Select(t => t.Clone()));
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
                Techniques = Deduction.ClanTechniques.Select(t => t.Clone()).ToList(),
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
