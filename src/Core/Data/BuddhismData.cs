using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>A monk's vow (R13-14b): what it forbids itself, for a faster nature and a firmer word.</summary>
    public enum BuddhistVow { None, NonViolence, Silence, Renunciation }

    /// <summary>What a debate came to (R13-14b): won, or lost — and then what befell the clan's debater (Shamed: only the clan's face).</summary>
    public enum DebateFate { Won, Converted, Wounded, Fell, Shamed }

    /// <summary>
    /// The Buddhist conversion and its two schools (balance.json « buddhism »; LORE.md §3.3, §3.7, R13-R14; the user's choices
    /// of 2026-10-12): a dying cultivator may convert at a temple to escape death, giving up its immortal way — a realm lower,
    /// its foundation, abilities and method lost. It then cultivates its nature and destiny without trial or method, at its
    /// temple's school's pace: the ancient school slow and stable up to the Dharma Master's apogee; the modern one faster but
    /// capped at the Dharma Master's beginning, its soul held by the temple, which may call it back.
    /// </summary>
    public sealed class BuddhismSettings
    {
        /// <summary>A cultivator with this many years left or fewer may convert.</summary>
        public int DyingYears { get; init; }

        /// <summary>The years the conversion gives beyond the convert's age.</summary>
        public int YearsGained { get; init; }

        /// <summary>The offering to the temple, in spirit stones.</summary>
        public int OfferingStones { get; init; }

        /// <summary>The temple's warmth after a conversion.</summary>
        public int ConversionRelation { get; init; }

        /// <summary>The share of a human's yearly cultivation each school gains, cultivating.</summary>
        public double AncientSpeed { get; init; }
        public double ModernSpeed { get; init; }

        /// <summary>The highest Golden Core stage each school reaches (the Dharma Master).</summary>
        public int AncientCeilingStage { get; init; }
        public int ModernCeilingStage { get; init; }

        /// <summary>Below this relation, a modern temple may call back a soul it holds — a chance each year.</summary>
        public int RecallBelow { get; init; }
        public double RecallChance { get; init; }

        /// <summary>The tasks each vow forbids (a non-violent monk breaks its vow fighting on the grid as well).</summary>
        public Dictionary<BuddhistVow, List<TaskType>> VowTasks { get; init; } = new Dictionary<BuddhistVow, List<TaskType>>();

        /// <summary>A kept vow's share of extra cultivation, and its points in a debate.</summary>
        public double VowSpeedBonus { get; init; }
        public int VowDebateBonus { get; init; }

        /// <summary>The Pure Lands (R13-14c): an ancient monk's chance to choose its rebirth in the clan; the warmth a modern
        /// temple asks to give the soul it holds back; the age at which the reborn child regains the monk's rank.</summary>
        public double AncientRebirthChance { get; init; }
        public int ModernRebirthRelation { get; init; }
        public int RegainAge { get; init; }

        /// <summary>The borrowing pyramid (R14): a modern monk's extra pace per realm of its temple's strongest.</summary>
        public double LentSpeedPerRealm { get; init; }

        /// <summary>An ancient Dharma Master at its apogee: its yearly chance to become a Revered One (R13).</summary>
        public double VenerableChance { get; init; }

        /// <summary>The Dao debates (R13-14b).</summary>
        public DebateSettings Debates { get; init; } = new DebateSettings();
    }

    /// <summary>
    /// The Dao debates between the clan and the temples (balance.json « buddhism.debates »; LORE.md §3.7; the user's choices of
    /// 2026-10-12): a temple may challenge the clan, the clan a temple; the odds weigh the ranks, the mind, a monk's training and
    /// a kept vow. The loser's clan loses face; an immortal may be converted by force, or wounded in its mandate; a monk's Dharma
    /// path breaks a realm lower.
    /// </summary>
    public sealed class DebateSettings
    {
        /// <summary>Each temple's yearly chance to challenge the clan, when none waits.</summary>
        public double ChallengeChance { get; init; }

        /// <summary>The odds in percent: the base, per realm of gap, per point of stability above 70, a monk's and their bounds.</summary>
        public int BaseChance { get; init; }
        public int RealmStep { get; init; }
        public double StabilityWeight { get; init; }
        public int MonkBonus { get; init; }
        public int MinChance { get; init; }
        public int MaxChance { get; init; }

        public int WinPrestige { get; init; }
        public int WinRelation { get; init; }
        public int LossPrestige { get; init; }
        public int LossRelation { get; init; }
        public int RefusePrestige { get; init; }
        public int RefuseRelation { get; init; }

        /// <summary>The chance an immortal below the Golden Core who loses is converted by the temple (else its mandate is wounded).</summary>
        public double ForcedConversionChance { get; init; }

        /// <summary>The years before the same temple debates the clan again.</summary>
        public int CooldownYears { get; init; }
    }

    /// <summary>A monk's soul awaiting rebirth in a child of the clan (R13-14c, saved): who it was, its temple, school and rank.</summary>
    public sealed record MonkSoul(string Name, string Temple, CultivationSubPath School, CultivationRealm Realm, int Stage);

    /// <summary>A temple's challenge awaiting the clan's champion (saved); unanswered by the next year, a refusal.</summary>
    public sealed record Debate(string Temple, int Year);
}
