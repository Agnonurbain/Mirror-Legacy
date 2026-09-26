using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>What an oath's clause forbids or promises (L4d); new acts are added here and in oaths.json.</summary>
    public enum OathAct
    {
        Harm,          // never harm the other party (devour their foundation, graft it, strike them…)
        RevealSecret,  // never betray what was confided
        Espionage,     // never spy on the other party
        Service        // render a service before the pact's deadline
    }

    /// <summary>The ways around an oath (« nothing is perfect »), enabled by oaths.json.</summary>
    public enum LoopholeKind
    {
        ThirdParty,   // the letter, not the spirit: someone else acts
        FalseName,    // sworn under a name that is not one's own: it binds nothing
        Expiry,       // the oath runs out
        MirrorVeil,   // the mirror, of the Dao Embryo's rank, veils a breach from Heaven
        Purification, // pills and calm lift a Heart Demon
        Rebirth       // reincarnation or conversion: the sworn one « died » (with L7)
    }

    /// <summary>A clause one may swear on one's path (oaths.json).</summary>
    public sealed class ClauseDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public OathAct Act { get; init; }

        /// <summary>1 (light) to 3 (grave): indexes the consequences of balance.json.</summary>
        public int Severity { get; init; }

        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>A way around an oath the world allows (oaths.json).</summary>
    public sealed class LoopholeDefinition
    {
        public LoopholeKind Kind { get; init; }
        public string Name { get; init; }
        public string Notes { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>oaths.json: the clauses one may swear and the loopholes that exist.</summary>
    public sealed class OathCatalog
    {
        public IReadOnlyList<ClauseDefinition> Clauses { get; init; } = Array.Empty<ClauseDefinition>();
        public IReadOnlyList<LoopholeDefinition> Loopholes { get; init; } = Array.Empty<LoopholeDefinition>();
    }

    /// <summary>What breaking an oath costs, by the clause's severity (balance.json).</summary>
    public sealed class OathSettings
    {
        /// <summary>Chance the path is interrupted (progression sealed), by severity 1-3; otherwise a Heart Demon slows it.</summary>
        public IReadOnlyList<double> InterruptChanceBySeverity { get; init; } = Array.Empty<double>();

        /// <summary>Chance an interruption also wounds the Dao (a Qi deviation).</summary>
        public double DeviationChanceOnInterrupt { get; init; }

        /// <summary>Years a Heart Demon haunts, by severity 1-3.</summary>
        public IReadOnlyList<int> HeartDemonYearsBySeverity { get; init; } = Array.Empty<int>();

        /// <summary>Cultivation speed while a Heart Demon haunts, and the stability it tears away at once.</summary>
        public double HeartDemonSpeed { get; init; }
        public int HeartDemonStabilityLoss { get; init; }

        /// <summary>Purification: herbs spent and chance of lifting the Heart Demon.</summary>
        public int PurificationHerbs { get; init; }
        public double PurificationChance { get; init; }

        /// <summary>Mirror power to veil the next breach of a sworn member.</summary>
        public int MirrorVeilCost { get; init; }
    }

    /// <summary>A pact between two cultivators, each sworn on their path (saved).</summary>
    [Serializable]
    public sealed class PactData
    {
        public string Id { get; set; }
        public string PartyA { get; set; }
        public string PartyB { get; set; }
        public List<string> Clauses { get; set; } = new List<string>();
        public int SwornYear { get; set; }

        /// <summary>The year after which it lapses (a service falls due then); null: for life.</summary>
        public int? ExpiresYear { get; set; }

        /// <summary>A party who swore under a false name is not bound (a loophole).</summary>
        public bool FalseNameA { get; set; }
        public bool FalseNameB { get; set; }

        /// <summary>A party who rendered the promised service.</summary>
        public bool FulfilledA { get; set; }
        public bool FulfilledB { get; set; }

        public PactData Clone()
        {
            var copy = (PactData)MemberwiseClone();
            copy.Clauses = new List<string>(Clauses ?? new List<string>());
            return copy;
        }
    }
}
