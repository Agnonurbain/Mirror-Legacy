namespace MirrorChronicles.Data
{
    /// <summary>
    /// A rival's challenge awaiting the clan's answer (saved): the power that sends it, the year, the rivals' realm and
    /// stage (those of the clan's best free fighter when it was issued), how many they are, the seed that makes them, and
    /// whether it is to the death (a power that hates the clan; 2026-09-29).
    /// </summary>
    public sealed record Challenge(string Faction, int Year, CultivationRealm Realm, int Stage, int Rivals, int Seed, bool ToTheDeath = false);

    public enum ChallengeOutcome { Won, Lost, Withdrawn, Declined }

    /// <summary>
    /// A rival's challenge (balance.json « challenges »; G6; interpretations — the lore says nothing of it): how many the
    /// clan may send and the rivals may be, the wager the victor takes, the face a refusal costs with the challenger, and
    /// the chance that a fallen fighter dies (a blow not held back) rather than yields — the challenge is by the rules —
    /// and the challenge to the death a power that hates the clan may send instead.
    /// </summary>
    public sealed record ChallengeSettings
    {
        public int MaxFighters { get; init; } = 1;
        public int MaxRivals { get; init; } = 1;
        public int Wager { get; init; }
        public int DeclineRelation { get; init; }
        public double DeathChance { get; init; } = 1;

        // A challenge to the death: from a power whose relation fell this low or whose suspicion of the clan rose this
        // high, with this chance; the fallen always die, and fleeing costs this many times the face.
        public int DeathGrudgeRelation { get; init; }
        public int DeathGrudgeSuspicion { get; init; } = 100;
        public double DeathChallengeChance { get; init; }
        public double DeathDeclineFactor { get; init; } = 1;
    }
}
