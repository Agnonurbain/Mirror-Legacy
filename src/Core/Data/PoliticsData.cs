using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>A bond between two powers: an alliance, or a vassalage (A the suzerain of B).</summary>
    public enum BondKind { Alliance, Vassalage }

    /// <summary>A bond between two powers (saved); the suzerain's grip on a vassal power grows until it absorbs it.</summary>
    public sealed record PowerBond(string Id, BondKind Kind, string A, string B, int StartYear, bool Secret)
    {
        public int Grip { get; init; }
    }

    /// <summary>Powers banded against the clan (saved), for the years left.</summary>
    public sealed record Coalition(List<string> Members, int YearsLeft);

    /// <summary>An ally of the clan, attacked, calls it to arms (saved): answered, refused, or left unanswered — a refusal.</summary>
    public sealed record CallToArms(string Ally, string Attacker, int Year);

    /// <summary>
    /// The powers' own politics (balance.json « politics »; user decision 2026-09-27; interpretations of D7): how they ally
    /// (affinity: neighbours, one Dao, less their mutual distrust), feud, subjugate and absorb, when they band against the
    /// clan, what a call to arms costs, and how often a suzerain's full grip absorbs a vassal clan.
    /// </summary>
    public sealed record PoliticsSettings
    {
        public double AllianceChance { get; init; }
        public int AllianceAffinity { get; init; }
        public int NeighbourAffinity { get; init; }
        public int SamePathAffinity { get; init; }
        public int MaxBondsPerYear { get; init; } = 1;

        public double FeudChance { get; init; }
        public double FeudWealthShare { get; init; }
        public double FeudPowerShare { get; init; }
        public int AllyDistrust { get; init; }

        public double VassalizeChance { get; init; }
        public int VassalizePowerRatio { get; init; } = 1;
        public double VassalTributeShare { get; init; }
        public int GripPerYear { get; init; }
        public int GripThreshold { get; init; } = 1;

        public int CoalitionSuspicion { get; init; }
        public int CoalitionMinMembers { get; init; } = 1;
        public int CoalitionYears { get; init; } = 1;
        public double CoalitionStonesShare { get; init; }
        public int CoalitionRelation { get; init; }
        public double CoalitionSchemeFactor { get; init; } = 1;

        public int CallStonesCost { get; init; }
        public int CallAttackerRelation { get; init; }
        public int CallAllyRelation { get; init; }

        /// <summary>How many times a suzerain's grip may fill before it absorbs a vassal clan (the game is lost).</summary>
        public int ClanAbsorptionSteps { get; init; } = 1;
    }
}
