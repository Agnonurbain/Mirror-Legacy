using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// Everything the game reads from its data files (game/data/*.json): the founding clan, the name
    /// pools, the balance, the factions, the events, the techniques and the spiritual Qi. Shared by
    /// sessions and never modified by them: systems copy what they change (factions) and only read the rest.
    /// </summary>
    public sealed record GameContent
    {
        public ClanDefinition Clan { get; init; }
        public NamePools Names { get; init; }
        public BalanceSettings Balance { get; init; }
        public IReadOnlyList<FactionData> Factions { get; init; } = Array.Empty<FactionData>();
        public IReadOnlyList<RandomEventData> RandomEvents { get; init; } = Array.Empty<RandomEventData>();
        public IReadOnlyList<StoryEventData> StoryEvents { get; init; } = Array.Empty<StoryEventData>();

        /// <summary>The known techniques of the world (LORE.md §2.4), shared: never modify one.</summary>
        public IReadOnlyList<TechniqueData> Techniques { get; init; } = Array.Empty<TechniqueData>();

        public IReadOnlyList<QiDefinition> Qi { get; init; } = Array.Empty<QiDefinition>();

        /// <summary>The words the mirror names its deductions with.</summary>
        public TechniqueNaming DeductionNames { get; init; }

        /// <summary>The Dao lineages (LORE.md §6), shared: a game keeps its own statuses (FruitionRegistry).</summary>
        public IReadOnlyList<FruitionDefinition> Fruitions { get; init; } = Array.Empty<FruitionDefinition>();

        /// <summary>How the world calls a True Monarch the lore does not name.</summary>
        public string AnonymousHolder { get; init; }
    }

    /// <summary>techniques.json: the catalog and the words of deduced names.</summary>
    public sealed class TechniqueCatalog
    {
        public IReadOnlyList<TechniqueData> Techniques { get; init; } = Array.Empty<TechniqueData>();
        public TechniqueNaming DeductionNames { get; init; }
    }

    /// <summary>
    /// How the mirror names a deduced technique: the template joins the kind's noun, the grade's word
    /// (possibly empty) and the element's phrase, e.g. « {kind}{grade} {element} ».
    /// </summary>
    public sealed class TechniqueNaming
    {
        public string Template { get; init; }
        public IReadOnlyDictionary<TechniqueKind, string> Kinds { get; init; } = new Dictionary<TechniqueKind, string>();
        public IReadOnlyDictionary<Element, string> Elements { get; init; } = new Dictionary<Element, string>();

        /// <summary>One word per grade, 1 to 7.</summary>
        public IReadOnlyList<string> GradeWords { get; init; } = Array.Empty<string>();
    }

    /// <summary>The player's clan at the start of a game (clan.json).</summary>
    public sealed class ClanDefinition
    {
        public string ClanName { get; init; }
        public IReadOnlyList<FounderDefinition> Founders { get; init; } = Array.Empty<FounderDefinition>();

        /// <summary>Catalog techniques the clan knows at the start.</summary>
        public IReadOnlyList<string> StartingTechniques { get; init; } = Array.Empty<string>();

        /// <summary>Portions of spiritual Qi in store at the start, by Qi.</summary>
        public IReadOnlyDictionary<string, int> StartingQi { get; init; } = new Dictionary<string, int>();
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

        /// <summary>The method the founder practises; required from Qi Cultivation on.</summary>
        public string CultivationMethod { get; init; }

        /// <summary>The founder's temper; drawn when the data leaves it out.</summary>
        public Temperament Temperament { get; init; }
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

        /// <summary>Cultivation speed of a method, by grade 1 to 7 (LORE.md §2.1: a higher grade cultivates faster).</summary>
        public IReadOnlyList<double> TechniqueSpeedByGrade { get; init; } = Array.Empty<double>();

        /// <summary>How a lineage whose status the lore leaves open is drawn at the start of a game (§11.7).</summary>
        public FruitionOdds UnspecifiedFruitionOdds { get; init; }

        /// <summary>Cultivation speed at the Foundation and beyond of a Dao Heart aligned with its lineage, and of another (§5.3.2).</summary>
        public double HeartAlignedSpeed { get; init; } = 1.0;
        public double HeartMisalignedSpeed { get; init; } = 1.0;

        /// <summary>Each year, the chance a foundation's holder takes on the temper its lineage favours.</summary>
        public double HeartAlignmentYearlyChance { get; init; }

        /// <summary>Chance a newborn takes a parent's temper (otherwise a temper of its own).</summary>
        public double TemperamentInheritanceChance { get; init; }
    }

    /// <summary>Chance of a spiritual orifice at birth, by the number of parents who have one.</summary>
    public sealed class OrificeOdds
    {
        public double Commoner { get; init; }
        public double OneParent { get; init; }
        public double TwoParents { get; init; }
    }
}
