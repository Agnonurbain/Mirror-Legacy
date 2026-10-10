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
    // Wiring: every system built in a fixed order, which is the order their reactions run in.
    public sealed partial class GameSession
    {
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
            Elders = new ElderSystem(Context, Factions);
            PowerEconomy = new PowerEconomy(Context, Factions);
            Sect = new SectSystem(Context, Clan, Resources, Stability, Karma, Factions);
            Mirror = new MirrorSystem(Context, Clan, Breakthroughs);
            Deduction = new DeductionEngine(Context, Mirror, Techniques);
            Oaths = new OathSystem(Context, Clan, Resources, Mirror, Knowledge);
            GoldenCore = new GoldenCoreSystem(Context, Clan, Fruitions, Mirror, Knowledge, Resources);
            Talismans = new TalismanSystem(Context, Clan, Resources, Factions, Mirror, Sect);
            Shards = new ShardSystem(Context, Clan, Mirror, Techniques, Knowledge, Wounds);
            Suspicion = new SuspicionLedger();
            Treaties = new TreatySystem(Context, Clan, Resources, Factions, Suspicion, Techniques);
            Exchange = new KnowledgeExchange(Context, Factions, Techniques, Resources, Mirror, Treaties);
            Bestiary = new BeastRegistry(Context);
            Buildings = new BuildingSystem(Context, Clan, Resources, Stability, Cultivation);
            Alliances = new AllianceSystem(Context, Factions, Resources);
            Espionage = new EspionageSystem(Context, Factions, Deduction, Stability, Techniques);
            Tasks = new TaskAssignmentSystem(Context, Clan, Cultivation, Resources, Stability, Factions, Deduction, Espionage, Buildings, Techniques, Talismans, Bestiary, Shards, Sect);
            Hunts = new HuntOperations(Context, Clan, Resources, Mirror, Factions, Bestiary, Knowledge, Talismans, Suspicion, Stability);
            Lore = new MirrorLore(Context, Factions, seed);
            Secrets = new SecretSystem(Context, Clan, Factions, Suspicion, Oaths, Mirror, Lore);
            Plots = new PlotSystem(Context, Clan, Resources, Factions, Suspicion, Secrets, Treaties);
            Captives = new CaptiveSystem(Context, Clan, Resources, Factions, Suspicion, Oaths, Mirror, Hunts);
            Politics = new PowerPoliticsSystem(Context, Resources, Factions, Suspicion, Treaties);
            Lifecycle = new PowerLifecycle(Context, Factions, Politics, Elders);
            WorldFruitions = new WorldFruitions(Context, Clan, Factions, Fruitions, Elders, Suspicion, Mirror);
            Demons = new MetalEssenceDemons(Context, Clan, Resources, Factions);
            Ancestors = new AncestorReturn(Context, Clan, Fruitions, Demons, Factions);
            Watch = new ClanWatch(Context, Suspicion);
            SecretBook = new SecretBook(Context, Suspicion);
            Accords = new KnowledgeAccords(Context, Clan, Factions, Techniques, Resources, SecretBook);
            Sponsorships = new Sponsorships(Context, Clan, Factions, Techniques, SecretBook, Accords, Mirror);
            PowerShards = new PowerShards(Context, Clan, Factions, SecretBook, Suspicion, Lore, Treaties, Resources, Shards);
            ShardSense = new ShardSense(Context, Clan, Factions, Shards, PowerShards, Mirror);
            Dealings = new SecretDealings(Context, Clan, Resources, Factions, Suspicion, SecretBook);
            Probes = new ProbeSystem(Context, Clan, Factions, Suspicion, Treaties, Politics, Mirror, Lore, Captives, SecretBook, Hunts, Resources, Patrons);
            Wars = new WarSystem(Context, Clan, Resources, Factions, Suspicion, Treaties, Politics, Alliances);
            Intrigues = new IntrigueSystem(Context, Clan, Resources, Factions, Suspicion, Techniques, Mirror, Captives, Treaties, Plots, Secrets, Wars, Sect);
            Patrons = new PatronSystem(Context, Clan, Resources);
            DesignEffects = new PatronDesignEffects(Context, Clan, Factions, Treaties, Wars, Suspicion, Wounds, Accords, Resources, Lore, Patrons);
            Rivals = new AscentRivals(Context, Clan, Factions, Suspicion, Sponsorships, Treaties, Buildings, Resources);
            Paths = new AscentPaths(Context, Clan, Factions, Techniques, Suspicion, Shards, Wounds);
            Challenges = new ChallengeSystem(Context, Clan, Resources, Factions, Techniques, Wounds, Suspicion);
            Upkeep = new UpkeepSystem(Context, Clan, Resources, Stability);
            DaoHunts = new DaoHuntSystem(Context, Clan, Factions, Treaties, Buildings, seed);
            Schemes = new SchemeSystem(Context, Clan, Factions, Captives, Secrets, Treaties, Politics, Patrons);
            Marriages = new MarriageSystem(Context, Clan, Factions, Stability, Resources);
            Matches = new MarriageAlliance(Context, Clan, Factions, Treaties, Marriages, Suspicion);
            RandomEvents = new EventManager(Context, Clan, Factions, Deduction, Resources, Stability, Buildings);
            Legacy = new LegacySystem(Context, Clan, Resources, Deduction);
            Story = new StoryEventManager(Context, Clan, Resources, Stability, Factions);
            Victory = new VictoryConditionSystem(Context, Clan);
            Annals = new ClanAnnals(Context, Clan, Karma);
            Absorption = new ClanAbsorption(Context, Clan, Resources, Factions, Treaties, Suspicion, Techniques, PowerShards, Shards);
            Imperial = new ImperialWay(Context, Clan, Sect, Treaties, Factions);
            Phenomena = new RegionalPhenomena(Context, Resources);
            Dharma = new DharmaTreasures(Context, Clan, Resources);
            Wars.DomainGuard = () => Dharma.DomainStrength; // the treasures and Designations weigh in the clan's wars (L4e)
            Artifacts = new ArtifactArmoury(Context, Clan);
            Forge = new ArtifactForge(Context, Clan, Resources, Buildings, Artifacts);
            Arsenal = new WorldArsenal(Context, Factions, Artifacts, Fruitions);
            Rebirths = new WorldRebirths(Context, Factions, Fruitions);
            Rebirths.Clan = Clan;
            Rebirths.Suspicion = Suspicion;
            AscentWatch = new AscentWatch(Context, Factions, Suspicion, Wounds);
            Enthrallment = new Enthrallment(Context, Clan, Factions, Mirror, Suspicion);
            Mirror.VoidOpen = () => Shards.CanTraverseVoid;
            Mirror.Wounds = Wounds;
            Mirror.EssencesHeld = () => Demons.Essences;
            Light = new LightStrike(Context, Mirror, Factions, Suspicion);
            Moonlight = new MoonlightGift(Context, Mirror, Clan, Resources, Suspicion);
            Marks = new TechniqueMarks(Context, Mirror, Factions, Suspicion);
            Arts = new ArtSystem(Context, Clan, Cultivation, Resources);
            Forge.Arts = Arts;
            Clientage = new ClientageSystem(Context, Clan, Resources, Suspicion);
            Treaties.Clientage = Clientage;
            Treaties.OnClientBroke = sect => Wars.WagedOnClan(sect);
            Buildings.Arts = Arts;
            Arts.Factions = Factions;
            Arts.Accords = Accords;
            Arts.Mirror = Mirror;
            Arts.Deduction = Deduction;
            Buildings.Factions = Factions;
            Alchemy = new AlchemySystem(Context, Clan, Resources, Arts, Factions, Suspicion);
            Breakthroughs.Alchemy = Alchemy;
            Alchemy.Accords = Accords;
            Alchemy.Captives = Captives;
            Oaths.Alchemy = Alchemy;
            Abilities.Alchemy = Alchemy;
            Challenges.Alchemy = Alchemy;
            PowerSchemes = new PowerSchemeSystem(Context, Factions, Politics);
            Finds = new ArtifactFinds(Context, Clan, Factions, Artifacts);
            ArtifactTrade = new ArtifactTrade(Context, Clan, Resources, Factions, Suspicion, Artifacts);
            Intrigues.Armoury = Artifacts;
            Bridges = new PositionBridges(Context);
            Mandate = new MandateOfLife(Context, Clan, Factions);
            GoldenCore.CorruptedVirtues = () => Bridges.Corrupted; // the bridges of the corrupted Virtues (R6)
            Minors = new MinorAbilities(Context, Clan, Resources, Factions, Suspicion, Mirror, Knowledge, Accords);
            Accords.Armoury = Artifacts; // an artifact may be given in an accord
            GoldenCore.Accords = Accords;   // a holder's leave is paid in kind
            GoldenCore.Factions = Factions;
            ArtifactTrade.Accords = Accords;
            Schemes.Guard = m => 1 - (1 - Dharma.GuardChance(m)) * (1 - Dharma.DomainGuardChance) * (1 - ArtifactRules.Protection(m));
            Place.Phenomena = Phenomena; // a death's weather, a failure's lasting phenomenon (L4c)
            GoldenCore.GovernanceBonus = Imperial.BonusFor; // a sovereign cultivates by governing (R20)
            Imperial.Politics = Politics;
            Elders.GoverningBonus = Imperial.GoverningBonus; // the world's kingdoms govern too
            GoldenCore.PlaceBonus = Place.BreakthroughBonus; // an atmosphere favouring the lineage helps the forge (§5.8)
            Endings = new DynasticEndings(Context, Clan, Treaties, Factions, Wars, Mirror, Annals, Victory, Sect, Absorption, Imperial); // judged last
        }
    }
}
