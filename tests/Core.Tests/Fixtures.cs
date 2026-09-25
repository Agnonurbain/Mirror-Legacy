using System;
using System.IO;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests
{
    /// <summary>Every draw returns the same sample, so a roll's outcome is predictable.</summary>
    internal sealed class FixedRandom : Random
    {
        private readonly double sample;
        public FixedRandom(double sample) { this.sample = sample; }
        protected override double Sample() => sample;
    }

    /// <summary>
    /// Returns the given samples in order, then repeats the last one. Next(1, 101) on a sample s
    /// yields 1 + (int)(s * 100), so 0.0 → 1, 0.495 → 50, 0.995 → 100.
    /// </summary>
    internal sealed class SequenceRandom : Random
    {
        private readonly double[] samples;
        private int index;
        public SequenceRandom(params double[] samples) { this.samples = samples; }
        protected override double Sample() => samples[Math.Min(index++, samples.Length - 1)];
    }

    /// <summary>Every system of a session, wired in the session's order, for the system tests.</summary>
    internal sealed class TestWorld
    {
        public GameContext Ctx { get; }
        public ClanManager Clan { get; }
        public ResourceManager Resources { get; }
        public MentalStabilitySystem Stability { get; }
        public ClanKarmaSystem Karma { get; }
        public TechniqueLibrary Techniques { get; }
        public CultivationSystem Cultivation { get; }
        public BreakthroughSystem Breakthroughs { get; }
        public FoundationSystem Foundations { get; }
        public PurpleMansionSystem PurpleMansion { get; }
        public FactionManager Factions { get; }
        public MirrorSystem Mirror { get; }
        public DeductionEngine Deduction { get; }
        public BuildingSystem Buildings { get; }
        public AllianceSystem Alliances { get; }
        public EspionageSystem Espionage { get; }
        public TaskAssignmentSystem Tasks { get; }
        public MarriageSystem Marriages { get; }

        public TestWorld(Random rng)
        {
            Ctx = Fixtures.Context(rng);
            Clan = new ClanManager(Ctx, "Mo");
            Resources = new ResourceManager(Ctx);
            Stability = new MentalStabilitySystem(Ctx, Clan);
            Karma = new ClanKarmaSystem(Ctx, Clan);
            Techniques = new TechniqueLibrary(Ctx);
            Cultivation = new CultivationSystem(Ctx, Karma, Techniques, Resources);
            Breakthroughs = new BreakthroughSystem(Ctx, Clan, Cultivation);
            Foundations = new FoundationSystem(Ctx, Clan, Techniques);
            PurpleMansion = new PurpleMansionSystem(Ctx, Clan, Cultivation, Techniques);
            Factions = new FactionManager(Ctx);
            Mirror = new MirrorSystem(Ctx, Clan, Breakthroughs);
            Deduction = new DeductionEngine(Ctx, Mirror, Techniques);
            Buildings = new BuildingSystem(Ctx, Clan, Resources, Stability, Cultivation);
            Alliances = new AllianceSystem(Ctx, Factions, Resources);
            Espionage = new EspionageSystem(Ctx, Factions, Deduction, Stability);
            Tasks = new TaskAssignmentSystem(Ctx, Clan, Cultivation, Resources, Stability, Factions, Deduction, Espionage, Buildings, Techniques);
            Marriages = new MarriageSystem(Ctx, Clan, Factions, Stability);
        }

        public TestWorld(int seed = 1) : this(new Random(seed)) { }

        /// <summary>Adds a member to the clan and returns it.</summary>
        public CharacterData Join(CharacterData member)
        {
            Clan.AddMember(member);
            return member;
        }
    }

    /// <summary>Shared builders for the simulation tests.</summary>
    internal static class Fixtures
    {
        private static readonly Lazy<string> DataDirectoryPath = new Lazy<string>(FindDataDirectory);
        private static readonly Lazy<GameContent> ShippedContent = new Lazy<GameContent>(() => GameContentLoader.Load(ReadDataFile));

        /// <summary>The content the game ships (game/data/*.json).</summary>
        public static GameContent Content => ShippedContent.Value;

        /// <summary>The shipped content without random events, so a test sees only what it provokes.</summary>
        public static GameContent QuietContent => Content with { RandomEvents = Array.Empty<RandomEventData>() };

        public static string DataDirectory => DataDirectoryPath.Value;

        public static string ReadDataFile(string fileName) => File.ReadAllText(Path.Combine(DataDirectory, fileName));

        public static GameSetup Setup(int seed = 1) => new GameSetup { Seed = seed, Content = Content };

        public static GameContext Context(int seed = 1) => Context(new Random(seed));

        public static GameContext Context(Random rng) =>
            new GameContext(new GameEventBus(), new RecordingGameLog(), rng, new GameClock(), Content);

        /// <summary>game/data, found from the test assembly by walking up to the solution.</summary>
        private static string FindDataDirectory()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MirrorLegacy.sln")))
                dir = dir.Parent;
            if (dir == null) throw new DirectoryNotFoundException("MirrorLegacy.sln not found above the test directory.");
            return Path.Combine(dir.FullName, "game", "data");
        }

        /// <summary>The Mo clan's method (grade 3, speed 1) and its Qi, which Qi and Foundation cultivators practise.</summary>
        public const string ClanMethod = "clear-spring-sutra";
        public const string ClanQi = "clear-spring-qi";

        /// <summary>
        /// A living cultivator with a known orifice and a lifespan matching the realm; in Qi Cultivation or
        /// the Foundation, they practise the clan's method on its Qi (LORE.md §5.2: none reaches Qi without one).
        /// </summary>
        public static CharacterData Cultivator(bool isMale = true, int age = 30,
            CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 1)
        {
            bool practises = realm == CultivationRealm.QiRefinement || realm == CultivationRealm.Foundation;
            return new CharacterData
            {
                CultivationMethodId = practises ? ClanMethod : null,
                QiId = practises ? ClanQi : null,
                FirstName = "Test",
                LastName = "Mo",
                IsMale = isMale,
                Age = age,
                Realm = realm,
                RealmStage = stage,
                HasSpiritualOrifice = true,
                OrificeKnown = true,
                SpiritualRoot = 50,
                MaxLifespan = PowerLadder.MaxLifespan(realm, stage)
            };
        }

        /// <summary>A living, examined mortal adult.</summary>
        public static CharacterData Mortal(bool isMale = true, int age = 30)
        {
            return new CharacterData
            {
                FirstName = "Test",
                LastName = "Mo",
                IsMale = isMale,
                Age = age,
                OrificeKnown = true,
                MaxLifespan = 70
            };
        }
    }
}
