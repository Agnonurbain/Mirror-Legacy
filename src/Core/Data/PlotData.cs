namespace MirrorChronicles.Data
{
    /// <summary>
    /// How the powers answer what they suspect (balance.json, L2c.4a; LORE.md D7; interpretations): when they
    /// investigate, how they find proof, when they strike, and the hidden price of striking without proof.
    /// </summary>
    public sealed record PlotSettings
    {
        /// <summary>A power investigates from this suspicion; its yearly chance grows per point of suspicion and per realm of its strongest.</summary>
        public int InvestigateThreshold { get; init; }
        public double InvestigationChancePerPoint { get; init; }
        public double InvestigationBonusPerRealm { get; init; }
        public int EvidencePerFinding { get; init; }

        /// <summary>A power strikes from this suspicion; with this much proof it is its right.</summary>
        public int ActThreshold { get; init; }
        public int ProofThreshold { get; init; }

        /// <summary>Without proof, a power dares only when its strongest realm exceeds the clan's by this many.</summary>
        public int BoldnessRealmMargin { get; init; }

        /// <summary>The blow: the relation lost, the share of the clan's stones taken.</summary>
        public int ReprisalRelation { get; init; }
        public double ReprisalStonesShare { get; init; }

        /// <summary>Striking without proof: the silent distrust every other power gains toward the striker.</summary>
        public int WitnessDistrust { get; init; }

        /// <summary>Striking with proof: the suspicion every other power gains toward the clan (its name suffers).</summary>
        public int ProofReputation { get; init; }
    }
}
