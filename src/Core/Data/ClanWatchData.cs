namespace MirrorChronicles.Data
{
    /// <summary>
    /// The clan's own distrust of each power (balance.json « clanWatch »; user request 2026-09-27; interpretations): what
    /// each offence adds to its memory, how much fades each year, and from what distrust the diplomacy screen shows a sign.
    /// </summary>
    public sealed record ClanWatchSettings
    {
        public int Struck { get; init; }
        public int MemberTaken { get; init; }
        public int AgentCaught { get; init; }
        public int TreatyBetrayed { get; init; }
        public int ProbeSpotted { get; init; }
        public int Blackmail { get; init; }
        public int ThiefCaught { get; init; }
        public int SpyUnmasked { get; init; }
        public int Coalition { get; init; }
        public int FadePerYear { get; init; }

        /// <summary>The signs: wary from the first, distrustful from the second, deeply so from the third.</summary>
        public int WaryFrom { get; init; } = 1;
        public int DistrustFrom { get; init; } = 1;
        public int DeepDistrustFrom { get; init; } = 1;
    }
}
