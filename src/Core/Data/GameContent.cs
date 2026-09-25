using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// Everything the game reads from its data files (game/data/*.json): the founding clan, the name
    /// pools, the balance, the factions and the events. Shared by sessions and never modified by them:
    /// systems copy what they change (factions) and only read the rest.
    /// </summary>
    public sealed record GameContent
    {
        public ClanDefinition Clan { get; init; }
        public NamePools Names { get; init; }
        public BalanceSettings Balance { get; init; }
        public IReadOnlyList<FactionData> Factions { get; init; } = Array.Empty<FactionData>();
        public IReadOnlyList<RandomEventData> RandomEvents { get; init; } = Array.Empty<RandomEventData>();
        public IReadOnlyList<StoryEventData> StoryEvents { get; init; } = Array.Empty<StoryEventData>();
    }

    /// <summary>The player's clan at the start of a game (clan.json).</summary>
    public sealed class ClanDefinition
    {
        public string ClanName { get; init; }
        public IReadOnlyList<FounderDefinition> Founders { get; init; } = Array.Empty<FounderDefinition>();
    }

    /// <summary>How a founder relates to the patriarch.</summary>
    public enum FounderRole
    {
        Patriarch,
        Matriarch,  // the patriarch's wife
        Child,      // of the patriarch and the matriarch
        Kin         // a relative with no recorded parents (a brother, a cousin)
    }

    public sealed class FounderDefinition
    {
        public string FirstName { get; init; }
        public FounderRole Role { get; init; }
        public bool IsMale { get; init; }
        public int Age { get; init; }
        public int SpiritualRoot { get; init; }
        public Element Affinity { get; init; }
        public CultivationRealm Realm { get; init; }
        public int RealmStage { get; init; }
        public int MentalStability { get; init; } = 70;
    }

    /// <summary>First names by sex and the family names of wandering cultivators (names.json).</summary>
    public sealed class NamePools
    {
        public IReadOnlyList<string> Male { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Female { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> OutsiderFamilies { get; init; } = Array.Empty<string>();
    }

    /// <summary>Tunable rates (balance.json); decision D3 sets the orifice odds (LORE.md §4).</summary>
    public sealed class BalanceSettings
    {
        public OrificeOdds OrificeOdds { get; init; }
        public double AnnualBirthChance { get; init; }
        public int MinMotherAge { get; init; }
        public int MaxMotherAge { get; init; }
        public double AnnualMarriageChance { get; init; }
    }

    /// <summary>Chance of a spiritual orifice at birth, by the number of parents who have one.</summary>
    public sealed class OrificeOdds
    {
        public double Commoner { get; init; }
        public double OneParent { get; init; }
        public double TwoParents { get; init; }
    }
}
