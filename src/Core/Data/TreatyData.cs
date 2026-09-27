using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>What a treaty binds (LORE.md D7; user decision 2026-09-27).</summary>
    public enum TreatyKind { NonAggression, Trade, Defence, Vassalage }

    /// <summary>
    /// A treaty between the clan and a power (saved): its kind, when it began and ends (null: open), whether it is secret
    /// or sealed by a Dao oath, which side is the suzerain in a vassalage, and the suzerain's grip on a vassal clan.
    /// </summary>
    public sealed record Treaty(string Id, TreatyKind Kind, string Faction, int StartYear, int? EndYear, bool Secret, bool Sealed,
        bool ClanIsSuzerain)
    {
        public int Grip { get; init; }

        /// <summary>How many times the suzerain's grip on the vassal clan has filled; too many, and the clan is absorbed.</summary>
        public int Absorptions { get; init; }
    }

    /// <summary>
    /// The treaties' rules (balance.json « treaties »; interpretations of D7: an alliance is a contract of profit, never a
    /// guarantee). A power's willingness: the kind's base, its temper's bias, its relation, the balance of strength, less
    /// what it suspects of the clan; it accepts from a relation and a willingness. Each year it may betray: the base, its
    /// temper, its suspicion, whether it is clearly stronger, far less when sworn.
    /// </summary>
    public sealed record TreatySettings
    {
        public Dictionary<TreatyKind, int> MinRelation { get; init; } = new Dictionary<TreatyKind, int>();
        public Dictionary<TreatyKind, int> AcceptBase { get; init; } = new Dictionary<TreatyKind, int>();
        public Dictionary<FactionPersonality, Dictionary<TreatyKind, int>> PersonalityBias { get; init; } =
            new Dictionary<FactionPersonality, Dictionary<TreatyKind, int>>();
        public int StrengthWeight { get; init; }
        public double SuspicionWeight { get; init; }
        public int AcceptThreshold { get; init; }

        /// <summary>A suzerain's strongest realm must exceed its vassal's by this many.</summary>
        public int SuzerainRealmMargin { get; init; } = 1;

        public double BetrayalBase { get; init; }
        public Dictionary<FactionPersonality, double> BetrayalTemper { get; init; } = new Dictionary<FactionPersonality, double>();
        public double SuspicionBetrayalWeight { get; init; }
        public double StrongerBetrayalFactor { get; init; } = 1;
        public double SealedFactor { get; init; } = 1;

        /// <summary>A betrayal: the relation lost, the witnesses' distrust (public treaties), the share of stones seized.</summary>
        public int BetrayalRelation { get; init; }
        public int BetrayalWitnessDistrust { get; init; }
        public double BetrayalStonesShare { get; init; }

        /// <summary>A secret treaty's yearly chance of coming to light, and the distrust of both sides it then brings.</summary>
        public double SecretDiscoveryChance { get; init; }
        public int SecretDiscoveryDistrust { get; init; }

        public int TradeIncome { get; init; }
        public double TradeDiscount { get; init; } = 1;

        /// <summary>An ally's (or a suzerain's) chance of foiling an ambush, and how close to the secret it draws each year.</summary>
        public double DefenceGuardChance { get; init; }
        public int AllyProximityClues { get; init; }

        /// <summary>A vassal's yearly tribute, the suzerain's grip growing each year; at the threshold it takes stones and an art.</summary>
        public double VassalTributeShare { get; init; }
        public int GripPerYear { get; init; }
        public int GripThreshold { get; init; } = 1;
        public double GripStonesShare { get; init; }

        /// <summary>The clan breaking its word: the relation lost, the others' suspicion of it, a Heart Demon when sworn.</summary>
        public int BreakRelation { get; init; }
        public int BreakReputation { get; init; }
        public int SealedHeartDemonYears { get; init; }
    }
}
