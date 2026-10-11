namespace MirrorChronicles.Data
{
    /// <summary>
    /// An ambush awaiting the clan's answer (G6; saved): the power that sprang it, the year, the member it struck, the
    /// agents' realm and stage, how many they are, and the seed that makes them. The clan fights it on the grid or lets
    /// chance settle it, as before the battles.
    /// </summary>
    public sealed record Ambush(string Faction, int Year, string TargetId, CultivationRealm Realm, int Stage, int Agents, int Seed);

    /// <summary>How an ambush fought on the grid ended: the agents beaten off (one taken), the member taken, or a flight.</summary>
    public enum AmbushOutcome { Repelled, Taken, Escaped }
}
