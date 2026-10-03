namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Imperial Way (balance.json « imperialWay », LORE.md §3.6, §5.9 R20; user decisions 2026-10-03 — interpretations).
    /// The clan founds its kingdom as the powers do (powerLifecycle's kingdom rules), and its sovereign cultivates by
    /// governing: each year on the throne adds <see cref="MeritPerYear"/> points to its odds of the Golden Core, plus
    /// <see cref="MeritPerVassal"/> per vassal and one per <see cref="MembersPerMerit"/> members, up to
    /// <see cref="MaxMerit"/>. The kingdoms of the world resent a new crown: <see cref="CrownRelationLoss"/>.
    /// </summary>
    public sealed record ImperialWaySettings
    {
        public int MeritPerYear { get; init; } = 1;
        public int MeritPerVassal { get; init; } = 1;
        public int MembersPerMerit { get; init; } = 100;
        public int MaxMerit { get; init; } = 30;
        public int CrownRelationLoss { get; init; } = 20;
    }
}
