using System;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Events;
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

    /// <summary>Shared builders for the simulation tests.</summary>
    internal static class Fixtures
    {
        public static GameContext Context(int seed = 1) => Context(new Random(seed));

        public static GameContext Context(Random rng) =>
            new GameContext(new GameEventBus(), new RecordingGameLog(), rng, new GameClock());

        /// <summary>A living cultivator with a known orifice and a lifespan matching the realm.</summary>
        public static CharacterData Cultivator(bool isMale = true, int age = 30,
            CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 1)
        {
            return new CharacterData
            {
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
