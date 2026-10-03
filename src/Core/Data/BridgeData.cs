using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// A bridge between two lineages (R6, 4A+1B=C): four abilities of <see cref="From"/> and one of <see cref="Bridge"/> (the
    /// named <see cref="Ability"/>, or any) lead to the Intercalary of <see cref="To"/> — only while <see cref="Virtue"/> is
    /// corrupted.
    /// </summary>
    public sealed record PositionBridge
    {
        public string From { get; init; }
        public string Bridge { get; init; }
        public string Ability { get; init; }
        public string To { get; init; }
        public Element Virtue { get; init; }
        public string Source { get; init; }
        public Provenance Provenance { get; init; }
    }

    /// <summary>The bridges of the positions (balance.json « positionBridges », user decisions 2026-10-03), and the Virtues corrupted at the start.</summary>
    public sealed record PositionBridgeSettings
    {
        public List<Element> CorruptedAtStart { get; init; } = new List<Element>();
        public List<PositionBridge> Bridges { get; init; } = new List<PositionBridge>();
    }
}
