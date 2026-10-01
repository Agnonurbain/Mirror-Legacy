using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>What the first designs do when they fall due (balance.json « patronDesigns »; LORE.md §11.10; 🔎).</summary>
    public sealed record PatronDesignSettings
    {
        public int HarvestRelationLoss { get; init; } // a weaker patron, thwarted, resents the clan
        public int MarkClues { get; init; }           // what the mark lets the patron see of the mirror
        public int FlawWounds { get; init; }          // the Dao wounds of a hidden flaw
        public int SincereGift { get; init; }         // the stones a sincere patron gives
        public int HumanPillsTaken { get; init; }     // the cultivators taken as ingredients
        public double FatedRootShare { get; init; }   // what is left of a fated child's root once its destiny is reaped
        public int PyramidStability { get; init; }    // the stability a bound soul loses
        public int PawnSuspicion { get; init; }       // a third power turned against the clan
        public int ScapegoatSuspicion { get; init; }  // every power's suspicion of a scapegoat
        public int BulwarkPrestige { get; init; }     // the prestige of standing as a rampart
        public double AtmosphereStonesShare { get; init; } // the share of the stones a ruined region costs
        public int ImperialSuspicion { get; init; }   // the states' suspicion of an imperial blood
        public double VoidKeyDeathChance { get; init; }
        public int VoidKeyTreasure { get; init; }
    }

    /// <summary>When a patron's design falls due (LORE.md §11.10, C).</summary>
    public enum DesignDue { PurpleMansion, GoldenCore, Years, HeirBorn }

    /// <summary>
    /// A patron's hidden design (designs.json; LORE.md §11.10, C; the user's decisions of 2026-09-30): what a power that offers
    /// an ascent method truly wants, when it falls due, and whether the mirror can cleanse it from the manual.
    /// </summary>
    public sealed class PatronDesign
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public DesignDue Due { get; init; }
        public int Years { get; init; }           // for a design due after years
        public bool Cleansable { get; init; }     // a mark or a flaw the mirror can wash out of the manual
        public int Weight { get; init; } = 1;     // how often a patron harbours it
        public string Source { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }
}
