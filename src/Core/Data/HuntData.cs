using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>What a member does in a hunt (L2c.3): strikes the beast, lures it, keeps watch, covers the retreat.</summary>
    public enum HuntRole { Striker, Lure, Lookout, Cover }

    /// <summary>When the hunt strikes: at dawn (neutral), by night (stealthier, harder to capture), on a festival (the owner distracted).</summary>
    public enum HuntTiming { Dawn, Night, Festival }

    /// <summary>The official reason for the team's absence: each costs stones and leaves fewer traces.</summary>
    public enum CoverStory { None, Trade, Pilgrimage, Escort }

    /// <summary>The mirror's help, paid in power: an illusion hides the approach, stolen memories erase witnesses (LORE.md §11.5).</summary>
    public enum MirrorAid { None, Illusion, MemoryTheft }

    /// <summary>
    /// A hunt planned as an operation (L2c.3; LORE.md D7 « everything is a plot »): the target, the team and its
    /// roles, the timing, the cover story, a diversion (a member seen elsewhere: « seen going left while going
    /// right »), a false trail (a power to blame) and the mirror's help.
    /// </summary>
    public sealed record HuntPlan
    {
        public string TargetBeastId { get; init; }
        public Dictionary<string, HuntRole> Team { get; init; } = new Dictionary<string, HuntRole>();
        public HuntTiming Timing { get; init; }
        public CoverStory Cover { get; init; }
        public string DiversionMemberId { get; init; }
        public string DiversionRegionId { get; init; }
        public string FramedFaction { get; init; }
        public MirrorAid Aid { get; init; }
    }

    /// <summary>What a hunt came to: whether it got close, whether it took the beast, the traces left, who was blamed, who fell.</summary>
    public sealed record HuntOutcome(bool Approached, bool Captured, int Exposure, string Blamed, string Refusal, IReadOnlyList<string> Casualties);

    /// <summary>The hunt's odds and costs (balance.json, L2c.3; interpretations). Lists are indexed by the enums above.</summary>
    public sealed record HuntSettings
    {
        public int ApproachBase { get; init; }
        public int LookoutBonus { get; init; }
        public int MaxLookouts { get; init; }
        public IReadOnlyList<int> TimingApproach { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> TimingCapture { get; init; } = Array.Empty<int>();
        public int DiversionBonus { get; init; }
        public int IllusionBonus { get; init; }

        /// <summary>Percent the approach loses per realm of the owner's strongest cultivator (its guard).</summary>
        public int GuardPenaltyPerRealm { get; init; }

        public int CaptureBase { get; init; }

        /// <summary>Percent per point of power (realm × 10 + stage) the best striker has over the beast.</summary>
        public int CapturePerPowerPoint { get; init; }
        public int LureBonus { get; init; }

        /// <summary>Traces left: seen on the approach, after a failed capture, after a clean one (before cover and memories).</summary>
        public int SeenExposure { get; init; }
        public int FailedCaptureExposure { get; init; }
        public int CleanBaseExposure { get; init; }
        public IReadOnlyList<int> CoverExposure { get; init; } = Array.Empty<int>();
        public int MemoryTheftExposure { get; init; }

        public IReadOnlyList<int> CoverStones { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> AidMirrorCost { get; init; } = Array.Empty<int>();

        /// <summary>A false trail: its base chance, more when the framed power lives by the target or is aggressive; a failed one adds to the suspicion.</summary>
        public double FrameBaseChance { get; init; }
        public double FrameNeighbourBonus { get; init; }
        public double FrameAggressiveBonus { get; init; }
        public int FailedFrameBacklash { get; init; }

        /// <summary>A failed capture: the strikers' shaken minds, and death when the beast was far stronger.</summary>
        public int FailedCaptureStabilityLoss { get; init; }
        public int DeathMargin { get; init; }
        public double DeathChance { get; init; }
    }
}
