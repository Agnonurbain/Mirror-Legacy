using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>An agent a power planted in another (P1; saved): whose it is, where it hides, since when.</summary>
    public sealed record PowerSpy(string Owner, string Host, int Year);

    /// <summary>
    /// The powers' blackmail and spies against each other (balance.json « powerIntrigues »; P1, the user's rule 2026-10-03:
    /// what befalls the clan befalls the world; the user's choices 2026-10-12; interpretations). Each year a power — the greedy
    /// and the scheming more often — may blackmail another whose secret it knows, for a share of its wealth: a weaker victim
    /// pays and buys quiet for some years; a victim at least as strong refuses, and the secret spread makes the others distrust
    /// it and sets it feuding with the blackmailer. A power may plant a spy in a neighbour no stronger than itself: each year
    /// the spy brings clues on its host's secrets, until it is unmasked — executed, its host distrusting and feuding.
    /// </summary>
    public sealed class PowerIntrigueSettings
    {
        public double BlackmailChance { get; init; }
        public Dictionary<FactionPersonality, double> PersonalityFactor { get; init; } = new Dictionary<FactionPersonality, double>();
        public double BlackmailWealthShare { get; init; }
        public int QuietYears { get; init; } = 1;
        /// <summary>A victim this many times the blackmailer's power (or more) refuses.</summary>
        public double RefuseStrengthRatio { get; init; } = 1;
        public int RefusalDistrust { get; init; }

        public double SpyChance { get; init; }
        public int SpyCluesPerYear { get; init; }
        public double SpyUnmaskChance { get; init; }
        public int UnmaskDistrust { get; init; }
    }
}
