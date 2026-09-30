using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Combat;
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
        public OathSystem Oaths { get; }
        public AgingSystem Aging { get; }
        public WoundSystem Wounds { get; }
        public FactionManager Factions { get; }
        public FruitionRegistry Fruitions { get; }
        public GoldenCoreSystem GoldenCore { get; }
        public TalismanSystem Talismans { get; }
        public KnowledgeExchange Exchange { get; }
        public BeastRegistry Bestiary { get; }
        public SuspicionLedger Suspicion { get; }
        public HuntOperations Hunts { get; }
        public PlotSystem Plots { get; }
        public RegionalQi Place { get; }
        public MirrorLore Lore { get; }
        public TreatySystem Treaties { get; }
        public PowerPoliticsSystem Politics { get; }
        public PatronSystem Patrons { get; }
        public WarSystem Wars { get; }
        public ChallengeSystem Challenges { get; }
        public UpkeepSystem Upkeep { get; }
        public DaoHuntSystem DaoHunts { get; }
        public IntrigueSystem Intrigues { get; }
        public SecretBook SecretBook { get; }
        public ClanWatch Watch { get; }
        public SecretDealings Dealings { get; }
        public ProbeSystem Probes { get; }
        public CaptiveSystem Captives { get; }
        public SchemeSystem Schemes { get; }
        public SecretSystem Secrets { get; }
        public MirrorSystem Mirror { get; }
        public DeductionEngine Deduction { get; }
        public BuildingSystem Buildings { get; }
        public AllianceSystem Alliances { get; }
        public EspionageSystem Espionage { get; }
        public TaskAssignmentSystem Tasks { get; }
        public MarriageSystem Marriages { get; }
        public MarriageAlliance Matches { get; }
        public EventManager RandomEvents { get; }
        public LegacySystem Legacy { get; }
        public StoryEventManager Story { get; }
        public VictoryConditionSystem Victory { get; }
        public ClanAnnals Annals { get; }
        public DynasticEndings Endings { get; }
        public SectSystem Sect { get; }
        public ShardSystem Shards { get; }
        public PowerShards PowerShards { get; }

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
            Fruitions = new FruitionRegistry(Context);
            Place = new RegionalQi(Context, Fruitions, Techniques);
            Cultivation = new CultivationSystem(Context, Karma, Techniques, Resources, Place);
            Breakthroughs = new BreakthroughSystem(Context, Clan, Cultivation);
            Foundations = new FoundationSystem(Context, Clan, Techniques);
            PurpleMansion = new PurpleMansionSystem(Context, Clan, Cultivation, Techniques);
            Abilities = new DivineAbilitySystem(Context, Clan, Techniques, Resources);
            Aging = new AgingSystem(Context, Clan);
            Wounds = new WoundSystem(Context, Stability);
            Factions = new FactionManager(Context);
            Mirror = new MirrorSystem(Context, Clan, Breakthroughs);
            Deduction = new DeductionEngine(Context, Mirror, Techniques);
            Oaths = new OathSystem(Context, Clan, Resources, Mirror, Knowledge);
            GoldenCore = new GoldenCoreSystem(Context, Clan, Fruitions, Mirror, Knowledge, Resources);
            Talismans = new TalismanSystem(Context, Clan, Resources, Factions, Mirror);
            Shards = new ShardSystem(Context, Clan, Mirror, Techniques, Knowledge, Wounds);
            Suspicion = new SuspicionLedger();
            Treaties = new TreatySystem(Context, Clan, Resources, Factions, Suspicion, Techniques);
            Exchange = new KnowledgeExchange(Context, Factions, Techniques, Resources, Mirror, Treaties);
            Bestiary = new BeastRegistry(Context);
            Buildings = new BuildingSystem(Context, Clan, Resources, Stability, Cultivation);
            Alliances = new AllianceSystem(Context, Factions, Resources);
            Espionage = new EspionageSystem(Context, Factions, Deduction, Stability, Techniques);
            Tasks = new TaskAssignmentSystem(Context, Clan, Cultivation, Resources, Stability, Factions, Deduction, Espionage, Buildings, Techniques, Talismans, Bestiary, Shards);
            Hunts = new HuntOperations(Context, Clan, Resources, Mirror, Factions, Bestiary, Knowledge, Talismans, Suspicion, Stability);
            Lore = new MirrorLore(Context, Factions, seed);
            Secrets = new SecretSystem(Context, Clan, Factions, Suspicion, Oaths, Mirror, Lore);
            Plots = new PlotSystem(Context, Clan, Resources, Factions, Suspicion, Secrets, Treaties);
            Captives = new CaptiveSystem(Context, Clan, Resources, Factions, Suspicion, Oaths, Mirror, Hunts);
            Politics = new PowerPoliticsSystem(Context, Resources, Factions, Suspicion, Treaties);
            Watch = new ClanWatch(Context, Suspicion);
            SecretBook = new SecretBook(Context, Suspicion);
            PowerShards = new PowerShards(Context, Clan, Factions, SecretBook, Suspicion, Lore, Treaties, Resources, Shards);
            Dealings = new SecretDealings(Context, Clan, Resources, Factions, Suspicion, SecretBook);
            Probes = new ProbeSystem(Context, Clan, Factions, Suspicion, Treaties, Politics, Mirror, Lore, Captives, SecretBook, Hunts, Resources, Patrons);
            Wars = new WarSystem(Context, Clan, Resources, Factions, Suspicion, Treaties, Politics, Alliances);
            Intrigues = new IntrigueSystem(Context, Clan, Resources, Factions, Suspicion, Techniques, Mirror, Captives, Treaties, Plots, Secrets, Wars);
            Patrons = new PatronSystem(Context, Clan, Resources);
            Challenges = new ChallengeSystem(Context, Clan, Resources, Factions, Techniques, Wounds, Suspicion);
            Upkeep = new UpkeepSystem(Context, Clan, Resources, Stability);
            DaoHunts = new DaoHuntSystem(Context, Clan, Factions, Treaties, Buildings);
            Schemes = new SchemeSystem(Context, Clan, Factions, Captives, Secrets, Treaties, Politics, Patrons);
            Marriages = new MarriageSystem(Context, Clan, Factions, Stability, Resources);
            Matches = new MarriageAlliance(Context, Clan, Factions, Treaties, Marriages, Suspicion);
            RandomEvents = new EventManager(Context, Clan, Factions, Deduction, Resources, Stability, Buildings);
            Legacy = new LegacySystem(Context, Clan, Resources, Deduction);
            Story = new StoryEventManager(Context, Clan, Resources, Stability, Factions);
            Victory = new VictoryConditionSystem(Context, Clan);
            Annals = new ClanAnnals(Context, Clan, Karma);
            Sect = new SectSystem(Context, Clan, Resources);
            Endings = new DynasticEndings(Context, Clan, Treaties, Factions, Wars, Mirror, Annals, Victory, Sect); // judged last
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
            session.Annals.Restore(null, 1, session.Clan.Registry.Records); // the founders' realms are no milestone
            session.Factions.InitializeFactions();
            session.Fruitions.DrawWorld(FruitionRegistry.WorldRandom(setup.Seed));
            session.Bestiary.Draw(BeastRegistry.WorldRandom(setup.Seed));
            session.SecretBook.DrawPowerSecrets(SecretBook.WorldRandom(setup.Seed), session.Factions.Factions);
            session.PowerShards.Place(PowerShards.WorldRandom(setup.Seed)); // three shards lie with three powers (B3c3)
            session.Deduction.AddFragment(Element.Fire, 1, "Rouleau calciné");
            session.Deduction.AddFragment(Element.Wood, 1, "Lamelle de bambou");

            session.Log.Info($"[Session] The {session.Clan.ClanName} clan begins its story (seed {setup.Seed}).");
            return session;
        }

        /// <summary>
        /// Rebuilds a game from a save, bringing older saves up to date (stages, orifices, lifespans).
        /// The random stream restarts from the seed, year and phase: reproducible, not a continuation.
        /// </summary>
        /// <summary>
        /// A save's text loaded, or null with the reason when it cannot be (unreadable, or read but inconsistent): the
        /// boundary with the player's files, where nothing may crash.
        /// </summary>
        public static GameSession TryLoad(string json, GameSetup setup, out string error)
        {
            error = null;
            try
            {
                var data = SaveSerializer.Deserialize(json);
                if (data.HistoricalRecords?.Any(r => r == null) == true) throw new System.IO.InvalidDataException("a record of the clan is empty");
                return FromSaveData(data, setup);
            }
            catch (Exception e) when (e is System.IO.InvalidDataException || e is ArgumentException || e is InvalidOperationException
                || e is KeyNotFoundException || e is NullReferenceException || e is InvalidCastException || e is FormatException)
            {
                error = e.Message;
                return null;
            }
        }

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
            session.Mirror.Restore(data.MirrorPower, data.RestoredFragments, data.MirrorAsleepUntil);
            session.Shards.Restore(data.RecoveredShards); // none before 2.21
            session.Shards.RestoreRuins(data.RevealedRuins);
            session.Karma.Restore(data.GenerationCount, data.TotalBirths, data.TotalDeaths, data.LastPatriarchId ?? session.Clan.PatriarchID);
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
            session.Oaths.Restore((data.Pacts ?? new List<PactData>()).Select(p => p.Clone()), data.VeiledOathBreakers);
            session.GoldenCore.Restore(data.GoldenCorePermissions); // saves made before L4b have none
            session.Resources.RestorePrayers(data.Prayers);           // saves made before 2.6: none gathered
            session.Resources.RestoreBeasts(data.CapturedBeasts);
            session.Suspicion.Restore(data.SuspicionOfClan, data.Distrust); // hidden; none in saves before 2.9
            session.Suspicion.RestoreEvidence(data.Evidence);
            session.Suspicion.RestoreMirrorClues(data.MirrorClues);
            session.Secrets.RestoreConfrontation(data.Confrontation);
            session.Captives.RestorePrisoners(data.Prisoners); // none in saves before 2.10
            session.Treaties.RestoreTreaties(data.Treaties);   // none in saves before 2.11
            session.Politics.RestoreBonds(data.PowerBonds);    // none in saves before 2.12
            session.Politics.RestoreCoalition(data.Coalition);
            session.Politics.RestoreCalls(data.CallsToArms);
            session.Intrigues.RestoreDemands(data.Demands, data.QuietUntil); // none in saves before 2.13
            session.SecretBook.Restore(data.SecretsHeld, data.SecretProgress);
            if (data.SecretsHeld == null) session.SecretBook.DrawPowerSecrets(SecretBook.WorldRandom(data.Seed), session.Factions.Factions); // before 2.14
            session.PowerShards.Place(PowerShards.WorldRandom(data.Seed)); // saves before 2.21: the powers' shards placed now
            session.Probes.RestoreAlertness(data.Alertness);
            session.Suspicion.RestoreClanDistrust(data.ClanDistrust); // none in saves before 2.15
            session.Dealings.RestoreSpent(data.SpentSecrets);
            session.Patrons.RestorePacts(data.PatronPacts);
            session.Wars.Restore(data.Wars, data.ClanWars); // none in saves before 2.17
            session.Challenges.Restore(data.PendingChallenge); // none in saves before 2.18
            session.Upkeep.Restore(data.Impoverished);          // false in saves before 2.19
            session.DaoHunts.Restore(data.DaoPreys);            // none in saves before 2.20
            if (data.WorldBeasts != null) session.Bestiary.Restore(data.WorldBeasts);
            else session.Bestiary.Draw(BeastRegistry.WorldRandom(data.Seed)); // saved before 2.8: the world's beasts from its seed
            if (data.HuntingGround != null) session.Tasks.SetHuntingGround(data.HuntingGround); // a place gone from the map: home
            session.Talismans.Restore(data.TalismanOffer);
            int period = content.Balance.Talismans.RitualPeriodYears;
            session.Talismans.RestoreCalendar(data.NextRitualYear ?? (data.CurrentYear + period - 1) / period * period);
            session.Fruitions.Restore(data.FruitionStates, FruitionRegistry.WorldRandom(data.Seed)); // older saves: the world their seed draws
            session.Story.Restore(data.TriggeredStoryEvents ?? new List<StoryTriggerType>(), data.PendingStoryEvents ?? new List<StoryTriggerType>());
            session.Victory.Restore(data.GameLost); // a game « won » under the old rule goes on: there is no forced victory now
            session.Annals.Restore(data.Annals, session.Karma.GenerationCount, records); // none before 2.21
            session.Endings.Restore(data.PositionMoves, data.EndingStreaks);
            session.Sect.Restore(data.SectFoundedYear); // none before 2.21

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
                RecoveredShards = Shards.Recovered.ToList(),
                RevealedRuins = Shards.RevealedRuins.ToList(),
                MirrorAsleepUntil = Mirror.AsleepUntil,
                Fragments = Deduction.Fragments.Select(f => f.Clone()).ToList(),
                Techniques = Techniques.Deduced.Select(t => t.Clone()).ToList(),
                Knowledge = Knowledge.Keys.ToList(), // the known techniques live there since 2.3
                Pacts = Oaths.Pacts.Select(p => p.Clone()).ToList(),
                VeiledOathBreakers = Oaths.Veiled.ToList(),
                SpiritualQi = new Dictionary<string, int>(Resources.SpiritualQi),
                QiHarvestProgress = new Dictionary<string, int>(Resources.QiHarvestProgress),
                FruitionStates = new Dictionary<string, FruitionState>(Fruitions.States),
                GoldenCorePermissions = new Dictionary<string, string>(GoldenCore.Permissions),
                Prayers = Resources.Prayers,
                CapturedBeasts = Resources.Beasts.ToList(),
                WorldBeasts = Bestiary.Beasts.ToList(),
                SuspicionOfClan = new Dictionary<string, int>(Suspicion.ClanSuspicions),
                Distrust = new Dictionary<string, int>(Suspicion.Distrusts),
                Evidence = new Dictionary<string, int>(Suspicion.Evidences),
                MirrorClues = new Dictionary<string, int>(Suspicion.AllMirrorClues),
                Confrontation = Secrets.Confrontation,
                Prisoners = Captives.Prisoners.ToList(),
                Treaties = Treaties.All.ToList(),
                PowerBonds = Politics.Bonds.ToList(),
                Coalition = Politics.Coalition == null ? null : Politics.Coalition with { Members = Politics.Coalition.Members.ToList() },
                CallsToArms = Politics.PendingCalls.ToList(),
                Demands = Intrigues.Demands.ToList(),
                QuietUntil = new Dictionary<string, int>(Intrigues.QuietUntil),
                SecretsHeld = SecretBook.All.ToList(),
                SecretProgress = new Dictionary<string, int>(SecretBook.AllProgress),
                Alertness = new Dictionary<string, int>(Probes.AllAlertness),
                ClanDistrust = new Dictionary<string, int>(Suspicion.AllClanDistrust),
                SpentSecrets = Dealings.Spent.ToList(),
                PatronPacts = Patrons.Pacts.ToList(),
                Wars = Wars.Wars.Select(w => w with { SideA = w.SideA.ToList(), SideB = w.SideB.ToList() }).ToList(),
                ClanWars = Wars.ClanWars.ToList(),
                PendingChallenge = Challenges.Pending,
                Impoverished = Upkeep.Impoverished,
                DaoPreys = DaoHunts.Known.ToList(),
                Annals = Annals.Entries.ToList(),
                PositionMoves = new Dictionary<string, string>(Endings.Moves),
                EndingStreaks = new Dictionary<string, int>(Endings.Streaks),
                SectFoundedYear = Sect.FoundedYear,
                HuntingGround = Tasks.HuntingGround,
                NextRitualYear = Talismans.NextRitualYear,
                TalismanOffer = Talismans.PendingOffer == null ? null
                    : new TalismanOffer(Talismans.PendingOffer.BeneficiaryId, new List<string>(Talismans.PendingOffer.Choices), Talismans.PendingOffer.Leap),
                GenerationCount = Karma.GenerationCount,
                TotalBirths = Karma.TotalBirths,
                TotalDeaths = Karma.TotalDeaths,
                LastPatriarchId = Karma.LastPatriarchId,
                Buildings = Buildings.Buildings.Select(b => new BuildingData(b.Type) { Level = b.Level }).ToList(),
                Factions = Factions.Factions.Select(f => f.Clone()).ToList(),
                TriggeredStoryEvents = Story.TriggeredEvents.ToList(),
                PendingStoryEvents = Story.PendingTriggers.ToList(),
                GameLost = Victory.GameLost
            };
        }

        private static GameContent RequireContent(GameSetup setup) =>
            setup?.Content ?? throw new ArgumentException("GameSetup.Content is required: load it with GameContentLoader.", nameof(setup));

        /// <summary>Moves to the next phase and resolves it. Does nothing once the game is over.</summary>
        public void AdvancePhase()
        {
            if (Victory.IsOver || Challenges.Current != null) return; // a battle under way is fought to its end first

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
                    Secrets.ProcessYear();            // those who know may talk (L2c.4b)
                    Plots.ProcessYear();              // the powers investigate, and strike what they suspect (D7)
                    Schemes.ProcessYear();            // the powers scheme for profit: ambushes on members away (L6a)
                    Captives.ProcessYear();           // the captives are interrogated, and may be executed
                    Treaties.ProcessYear();           // the treaties: tribute, trade, the allies close by — and betrayal (D7)
                    Politics.ProcessYear();           // the powers ally, feud, subjugate, band against the clan (2026-09-27)
                    Intrigues.ProcessYear();          // blackmail, theft, spies (2026-09-27)
                    Probes.ProcessYear();             // the powers probe each other and the clan (2026-09-27)
                    Watch.ProcessYear();              // the clan's memory of offences fades a little
                    Dealings.ProcessYear();           // a power with a pierced secret of the clan may expose it
                    Patrons.ProcessYear();            // the great partners' tribute, boons, and wrath
                    Wars.ProcessYear();               // open wars: battles, surrenders, coalitions against a hegemon, the clan's war
                    Challenges.ProcessYear();         // a rival's challenge left unanswered lapses: silence is a refusal
                    RandomEvents.TriggerYearlyEvent();
                    Marriages.ProcessAnnualMarriages(); // before Inheritance, so newlyweds can have children
                    DaoHunts.ProcessYear();             // a ripe Dao is prey (LORE.md §5.3.3): learnt, then struck, maybe foiled
                    Upkeep.PayUpkeep();                 // the year's income in, every member costs its upkeep; short, a poor year
                    break;
                case GamePhase.Breakthrough:
                    Breakthroughs.ProcessBreakthroughPhase();
                    PurpleMansion.ProcessBreakthroughPhase(); // the ascent's four trials and the retreats under way
                    Abilities.ProcessBreakthroughPhase();     // divine abilities condensed from the Dao Partners
                    GoldenCore.ProcessBreakthroughPhase();    // the false Left Hands pay their patrons, or fall
                    break;
                case GamePhase.Inheritance:
                    Clan.ProcessAnnualBirths(Upkeep.BirthFactor);
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
