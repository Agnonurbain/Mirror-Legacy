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
    public sealed partial class GameSession
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

        /// <summary>A new Purple Mansion draws the old powers' eyes (audit §1.9).</summary>
        public AscentWatch AscentWatch { get; }

        /// <summary>A reborn True Monarch bends lesser minds (audit §1.8).</summary>
        public Enthrallment Enthrallment { get; }

        /// <summary>The mirror's Light against the powers' elders (audit §3.2).</summary>
        public LightStrike Light { get; }

        /// <summary>The Supreme Yin Moonlight given to the clan (audit §3.7).</summary>
        public MoonlightGift Moonlight { get; }

        /// <summary>The marks the clan's stolen arts keep of their power (audit §3.9).</summary>
        public TechniqueMarks Marks { get; }

        /// <summary>The clan's Immortal Arts: legacies, practitioners, mastery (audit §2).</summary>
        public ArtSystem Arts { get; }
        public AlchemySystem Alchemy { get; }
        public ClientageSystem Clientage { get; }
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
        public ClanAbsorption Absorption { get; }
        public KnowledgeAccords Accords { get; }
        public Sponsorships Sponsorships { get; }
        public PatronDesignEffects DesignEffects { get; }
        public AscentRivals Rivals { get; }
        public AscentPaths Paths { get; }
        public ShardSystem Shards { get; }
        public PowerShards PowerShards { get; }
        public ShardSense ShardSense { get; }
        public ElderSystem Elders { get; }
        public PowerEconomy PowerEconomy { get; }
        public PowerLifecycle Lifecycle { get; }
        public WorldFruitions WorldFruitions { get; }
        public AncestorReturn Ancestors { get; }
        public MetalEssenceDemons Demons { get; }
        public DharmaTreasures Dharma { get; }
        public ArtifactArmoury Artifacts { get; }
        public ArtifactForge Forge { get; }
        public WorldArsenal Arsenal { get; }
        public WorldRebirths Rebirths { get; }
        public PowerSchemeSystem PowerSchemes { get; }
        public ArtifactFinds Finds { get; }
        public ArtifactTrade ArtifactTrade { get; }
        public MinorAbilities Minors { get; }
        public PositionBridges Bridges { get; }
        public MandateOfLife Mandate { get; }
        public ImperialWay Imperial { get; }
        public RegionalPhenomena Phenomena { get; }


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
            session.Sect.Restore(null, 1);
            session.Annals.Restore(null, 1, session.Clan.Registry.Records); // the founders' realms are no milestone
            session.Factions.InitializeFactions();
            session.Fruitions.DrawWorld(FruitionRegistry.WorldRandom(setup.Seed));
            session.Bestiary.Draw(BeastRegistry.WorldRandom(setup.Seed));
            session.SecretBook.DrawPowerSecrets(SecretBook.WorldRandom(setup.Seed), session.Factions.Factions);
            session.PowerShards.Place(PowerShards.WorldRandom(setup.Seed)); // three shards lie with three powers (B3c3)
            session.Elders.Populate(ElderSystem.WorldRandom(setup.Seed));    // the powers' elders (the living world, 2026-10-01)
            session.WorldFruitions.Link();                                   // and the Realizations some of them hold
            BindToTheSuzerain(session, content);
            session.Deduction.AddFragment(Element.Fire, 1, "Rouleau calciné");
            session.Deduction.AddFragment(Element.Wood, 1, "Lamelle de bambou");

            session.Log.Info($"[Session] The {session.Clan.ClanName} clan begins its story (seed {setup.Seed}).");
            return session;
        }

        /// <summary>
        /// The world as the novel opens (📚 audit §4.1, §4.4, §4.9): the clan, its suzerain's client; each power under the one it
        /// answers to (the lake's families under the Cloud Peak or the Fiery Iron Gate); the old enmities.
        /// </summary>
        private static void BindToTheSuzerain(GameSession session, GameContent content)
        {
            if (session.Factions.GetFactionByName(content.Clan.Suzerain) is { } sect) session.Treaties.BindAsClient(sect.Name);
            foreach (var power in session.Factions.Factions.Where(f => f.Suzerain != null && session.Factions.GetFactionByName(f.Suzerain) != null))
                session.Politics.BindVassal(power.Suzerain, power.Name);
            foreach (var power in session.Factions.Factions)
                foreach (var (toward, amount) in power.StartingDistrust ?? new System.Collections.Generic.Dictionary<string, int>())
                    session.Suspicion.AddDistrust(power.Name, toward, amount);
        }

        private static GameContent RequireContent(GameSetup setup) =>
            setup?.Content ?? throw new ArgumentException("GameSetup.Content is required: load it with GameContentLoader.", nameof(setup));
    }
}
