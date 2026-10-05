using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>What a great partner is.</summary>
    public enum PatronKind { Beast, Figure }

    /// <summary>What a great partner gives: protection (an ambush foiled), insight (fragments), sight (the clan's probes).</summary>
    public enum PatronBoon { Protection, Insight, Sight }

    /// <summary>
    /// A great partner of the very high level (patrons.json; user decision 2026-09-27): a great beast or a lone figure,
    /// where it dwells, the clan's strength it deals with, its yearly tribute, and its boon and how strong it is.
    /// </summary>
    public sealed class PatronDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public PatronKind Kind { get; init; }
        public string RegionId { get; init; } // where it dwells (for the map and the screens; no rule weighs it yet)
        public CultivationRealm MinClanRealm { get; init; }
        public int Tribute { get; init; }
        public PatronBoon Boon { get; init; }
        public int BoonStrength { get; init; }
        public string Notes { get; init; }

        /// <summary>The lands an ancient pact shelters from its beasts — whoever lives there, no tribute asked (audit §4.5).</summary>
        public IReadOnlyList<string> Shelters { get; init; } = Array.Empty<string>();
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>A pact with a great partner (saved): since when, and its favour.</summary>
    public sealed record PatronPact(string PatronId, int StartYear, int Favor);

    /// <summary>The pacts' favour (balance.json « patrons »; interpretations): its start and ceiling, what a paid or unpaid tribute does, the favour from which the clan may leave safely.</summary>
    public sealed record PatronSettings
    {
        public int StartFavor { get; init; }
        public int MaxFavor { get; init; }
        public int PaidFavor { get; init; }
        public int UnpaidFavorLoss { get; init; }
        public int SafeEndFavor { get; init; }
    }
}
