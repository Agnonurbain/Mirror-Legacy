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

        /// <summary>Facts the clan knows at the start (« Kind:Subject »), beyond what its techniques entail.</summary>
        public IReadOnlyList<string> Knowledge { get; init; } = Array.Empty<string>();
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

        /// <summary>The four trials of the breakthrough to the Purple Mansion (LORE.md §5.4.1).</summary>
        public PurpleMansionSettings PurpleMansion { get; init; }

        /// <summary>Condensing the divine abilities of the Purple Mansion (LORE.md §5.4.3).</summary>
        public DivineAbilitySettings DivineAbilities { get; init; }

        /// <summary>Chance a year of study reveals the Dao Partners of the scholar's foundation.</summary>
        public double StudyRevealsPartnersChance { get; init; }

        /// <summary>The technique rules the lore leaves open (L3 interpretations), replaceable when a source speaks.</summary>
        public TechniqueSettings Techniques { get; init; }
    }

    /// <summary>
    /// Values of the technique rules that the lore does not give (user request, 2026-09-26: every interpretation
    /// lives in the data). Grades 1 to 7 index the lists.
    /// </summary>
    public sealed class TechniqueSettings
    {
        /// <summary>Speed of Embryonic Breathing without a manual.</summary>
        public double CommonBreathingSpeed { get; init; }

        /// <summary>Portions of a method's Qi absorbed on entering Qi Cultivation (none for a ubiquitous Qi).</summary>
        public int QiPortionsToEnter { get; init; }

        /// <summary>Portions of one's Qi the immortal foundation absorbs.</summary>
        public int FoundationQiPortions { get; init; }

        /// <summary>Portions of an aligned technique's Qi a divine ability absorbs.</summary>
        public int AlignedQiPortions { get; init; }

        /// <summary>The lowest grade the elders choose for a member entering Qi Cultivation on their own.</summary>
        public int LowestGradeChosenForAMember { get; init; }

        /// <summary>The realm from which an art of each grade can be wielded.</summary>
        public IReadOnlyList<CultivationRealm> ArtRequiredRealmByGrade { get; init; } = Array.Empty<CultivationRealm>();

        /// <summary>Extra steps in battle of a movement art of each grade.</summary>
        public IReadOnlyList<int> MovementArtStepsByGrade { get; init; } = Array.Empty<int>();

        /// <summary>A deduction's grade: the fragments' average, +1 from this many fragments, at most this grade (7+ only from five divine ones).</summary>
        public int DeductionCompleteFragments { get; init; }
        public int DeductionMaxGrade { get; init; }
    }

    /// <summary>What condensing a divine ability costs (balance.json, tuned by simulation).</summary>
    public sealed class DivineAbilitySettings
    {
        /// <summary>Base chance (%) of passing the Threshold of Immortality, the fourth ability.</summary>
        public int ThresholdBaseChance { get; init; }

        /// <summary>Rare spiritual objects and pills that condense an ability at half the time, shallowly.</summary>
        public int ResourceStones { get; init; }
        public int ResourceHerbs { get; init; }
        public int ResourceOres { get; init; }

        /// <summary>Spiritual objects of the Purple Mansion completing a grafted foundation.</summary>
        public int GraftOres { get; init; }

        /// <summary>Years a grafted donor has left, stripped of all cultivation (user decision, 2026-09-25: one to five).</summary>
        public int GraftDonorMinYearsLeft { get; init; }
        public int GraftDonorMaxYearsLeft { get; init; }
    }

    /// <summary>Odds and durations of the Purple Mansion's breakthrough (balance.json, tuned by simulation).</summary>
    public sealed class PurpleMansionSettings
    {
        /// <summary>Base chance (%) of the Ascent; failing it kills.</summary>
        public int AscentBaseChance { get; init; }

        /// <summary>Years of the Manifestation retreat (« about six years »).</summary>
        public int ManifestationYears { get; init; }

        /// <summary>Base chance (%) of the Manifestation; a higher grade and more techniques ease it.</summary>
        public int ManifestationBaseChance { get; init; }

        /// <summary>Base chance (%) of the Illusions; failing them costs one's whole cultivation.</summary>
        public int IllusionsBaseChance { get; init; }

        /// <summary>The Manifestation's ease: per grade above 5, per technique known (up to a cap).</summary>
        public int ManifestationPerGrade { get; init; }
        public int ManifestationPerTechnique { get; init; }
        public int ManifestationTechniqueCap { get; init; }

        /// <summary>How long the Great Void holds a cultivator, band by band; past the last band, for life.</summary>
        public IReadOnlyList<VoidBand> VoidBands { get; init; } = Array.Empty<VoidBand>();
    }

    /// <summary>A share of cultivators the Great Void holds between two durations.</summary>
    public sealed class VoidBand
    {
        public double Chance { get; init; }
        public int MinYears { get; init; }
        public int MaxYears { get; init; }
    }

    /// <summary>Chance of a spiritual orifice at birth, by the number of parents who have one.</summary>
    public sealed class OrificeOdds
    {
        public double Commoner { get; init; }
        public double OneParent { get; init; }
        public double TwoParents { get; init; }
    }
}
