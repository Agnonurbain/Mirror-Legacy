using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>A war between powers (saved): each side with its allies, and each side's strength at the start.</summary>
    public sealed record War(string Id, List<string> SideA, List<string> SideB, int StartYear, int InitialA, int InitialB);

    /// <summary>The clan's own war (saved): its enemy, since when, and the enemy's strength at the start.</summary>
    public sealed record ClanWar(string Enemy, int StartYear, int EnemyInitial);

    /// <summary>
    /// Open wars (balance.json « wars »; user decision 2026-09-27; interpretations): a power's war strength (its realm and
    /// size), a battle's losses and loot, the ratio under which a side yields, how long a war may last, when powers declare
    /// wars and band against a hegemon, the clan's strength and losses, and peace.
    /// </summary>
    public sealed record WarSettings
    {
        public double StrengthPerPowerLevel { get; init; }
        public double BattleLossShare { get; init; }
        public double WinnerLootShare { get; init; }
        public double SurrenderRatio { get; init; }
        public int MaxWarYears { get; init; } = 1;
        public double WarChance { get; init; }
        public int WarDistrust { get; init; }
        public double HegemonRatio { get; init; } = 1;
        public double HegemonChance { get; init; }

        public double ClanStrengthPerMember { get; init; }
        public double AllyJoinChance { get; init; }
        public double ClanWarStonesLoss { get; init; }
        public double ClanWarDeathChance { get; init; }
        public double ClanLootShare { get; init; }
        public double PeaceTributeShare { get; init; }
        public int PeaceRelation { get; init; }
        public double PowerDeclareChance { get; init; }
        public int DeclareSuspicion { get; init; }
        public int DeclareRelation { get; init; }
    }
}
