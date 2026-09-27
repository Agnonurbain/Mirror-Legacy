using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>A power's blackmail awaiting the clan's answer (saved): the stones it wants, and since when.</summary>
    public sealed record Demand(string Faction, int Stones, int Year);

    /// <summary>What a thief may take from the clan.</summary>
    public enum IntrigueTarget { Beast, Manual, Stones, Qi }

    /// <summary>
    /// The powers' intrigues (balance.json « intrigues »; user decision 2026-09-27; interpretations of D7): blackmail
    /// (from what proof, how often by temper, what share of the clan's stones, how long a payment buys silence, how far a
    /// refusal spreads the proof), theft (how often by temper, how patrols hinder and catch), infiltration (how often an
    /// arranged spouse is a spy by its power's temper, what it feeds its power, what sounding a spouse costs the mirror,
    /// what a double agent undoes, what executing a spy costs).
    /// </summary>
    public sealed record IntrigueSettings
    {
        public int BlackmailEvidence { get; init; }
        public double BlackmailChance { get; init; }
        public Dictionary<FactionPersonality, double> BlackmailTemper { get; init; } = new Dictionary<FactionPersonality, double>();
        public double BlackmailStonesShare { get; init; }
        public int QuietYears { get; init; }
        public int RefusedSpreadEvidence { get; init; }
        public int RefusedRelation { get; init; }

        public double TheftChance { get; init; }
        public Dictionary<FactionPersonality, double> TheftTemper { get; init; } = new Dictionary<FactionPersonality, double>();
        public double PatrolGuard { get; init; }
        public double CatchPerPatrol { get; init; }
        public double MaxCatchChance { get; init; } = 1;
        public double TheftStonesShare { get; init; }

        public Dictionary<FactionPersonality, double> SpyChance { get; init; } = new Dictionary<FactionPersonality, double>();
        public int SpyEvidence { get; init; }
        public int SpyClues { get; init; }
        public int UnmaskMirrorCost { get; init; }
        public int DoubleAgentRelief { get; init; }
        public int SpyExecutionRelation { get; init; }
    }
}
