namespace MirrorChronicles.Data
{
    /// <summary>
    /// A rival's challenge awaiting the clan's answer (saved): the power that sends it, the year, the rivals' realm and
    /// stage (those of the clan's best free fighter when it was issued), how many they are, and the seed that makes them.
    /// </summary>
    public sealed record Challenge(string Faction, int Year, CultivationRealm Realm, int Stage, int Rivals, int Seed);

    public enum ChallengeOutcome { Won, Lost, Withdrawn, Declined }

    /// <summary>
    /// A rival's challenge (balance.json « challenges »; G6; interpretations — the lore says nothing of it): how many the
    /// clan may send and the rivals may be, the wager the victor takes, the face a refusal costs with the challenger, and
    /// the chance that a fallen fighter dies (a blow not held back) rather than yields — the challenge is by the rules.
    /// </summary>
    public sealed record ChallengeSettings
    {
        public int MaxFighters { get; init; } = 1;
        public int MaxRivals { get; init; } = 1;
        public int Wager { get; init; }
        public int DeclineRelation { get; init; }
        public double DeathChance { get; init; } = 1;
    }
}
