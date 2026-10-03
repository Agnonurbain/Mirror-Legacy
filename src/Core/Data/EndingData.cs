using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// What a condition of a dynastic ending weighs (endings.json; LORE.md §11.9). Read by name from the data: never
    /// rename a member.
    /// </summary>
    public enum EndingConditionKind
    {
        MemberRealm,     // a living member at Realm or above
        MemberPosition,  // a living True Monarch holding Position
        PositionMove,    // a member rose to Position from one of From (Transfer, Transformation), once in the game
        TrueMonarchs,    // Count living members at the Golden Core or above, at once
        GoldenLine,      // Count generations in a row, parent to child, each at the Golden Core or above
        Vassals,         // Count (0: all) powers of RegionId (and its places) of FactionKinds are the clan's vassals
        Hegemony,        // the clan outweighs in war every power of RegionId, with Vassals vassals, Years years in a row
        Year,            // the line endures to Year
        MirrorShards,    // Count shards of the mirror restored
        AnyOf,           // one of AnyOf holds
        SectFounded,     // the clan has founded its sect (B3d)
        Awaits,          // a system still to come (the ending's Awaits says which): never holds meanwhile
        AncestorReturned, // a living member reborn from a True Monarch of the clan has forged its Golden Core again (R9)
        ImperialCore     // the clan reigns, and a living member forged its Golden Core by governing (R20)
    }

    /// <summary>One condition of a dynastic ending; only the fields its kind reads are set.</summary>
    public sealed class EndingCondition
    {
        public EndingConditionKind Kind { get; init; }
        public CultivationRealm Realm { get; init; }
        public GoldenCoreState Position { get; init; }
        public IReadOnlyList<GoldenCoreState> From { get; init; } = Array.Empty<GoldenCoreState>();
        public int Count { get; init; }
        public string RegionId { get; init; }
        public IReadOnlyList<FactionKind> FactionKinds { get; init; } = Array.Empty<FactionKind>();
        public int Vassals { get; init; }
        public int Years { get; init; }
        public int Year { get; init; }
        public IReadOnlyList<EndingCondition> AnyOf { get; init; } = Array.Empty<EndingCondition>();
    }

    /// <summary>
    /// A dynastic ending (endings.json; LORE.md §11.9, the user's decisions of 2026-09-30): its name and the story it
    /// tells, the conditions that must all hold, and — for an ending defined before its system — what it awaits.
    /// </summary>
    public sealed class EndingDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Narrative { get; init; }
        public IReadOnlyList<EndingCondition> Conditions { get; init; } = Array.Empty<EndingCondition>();
        public string Awaits { get; init; }
        public string Source { get; init; } // where the lore or the wiki tells it
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }
}
