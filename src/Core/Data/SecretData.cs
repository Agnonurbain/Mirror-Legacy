using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>Who may hold a kind of secret.</summary>
    public enum SecretHolder { Clan, Power, Any }

    /// <summary>
    /// A kind of secret (secrets.json; user decision 2026-09-27): its name, its rank of importance (1 minor, 2 serious,
    /// 3 grave, 4 vital; the supreme rank is the mirror's own, with its knowers) and who may hold it.
    /// </summary>
    public sealed class SecretKind
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public int Rank { get; init; }
        public SecretHolder Holder { get; init; }
        public bool Drawn { get; init; } = true; // drawn with the world (a patron's design is created by its offer)
        public string Notes { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>A secret someone holds (saved): its kind and rank, since when, and whom it concerns (a victim, a framed power; may be null).</summary>
    public sealed record Secret(string Id, string KindId, string Holder, int Rank, int Year, string Subject);

    /// <summary>How a probe goes about its work.</summary>
    public enum ProbeApproach { Provocation, Infiltration, Bribery, RecordTheft, MirrorSight }

    /// <summary>A probe the clan plans: its target, its way, its team, the powers joining it, the stones of a bribe.</summary>
    public sealed record ProbePlan(string Target, ProbeApproach Approach, List<string> TeamIds, List<string> Partners, int Stones);

    /// <summary>What came of a probe: its refusal, success, whether it was seen, a disaster, the secrets it laid bare, the allies that lingered.</summary>
    public sealed record ProbeOutcome(string Refusal, bool Success, bool Detected, bool Disaster, IReadOnlyList<string> Revealed,
        IReadOnlyList<string> LateAllies)
    {
        public static ProbeOutcome Refused(string why) => new ProbeOutcome(why, false, false, false, Array.Empty<string>(), Array.Empty<string>());
    }

    /// <summary>Everything a probe's odds weigh (see ProbeRules.SuccessChance).</summary>
    public sealed record ProbeFactors
    {
        public ProbeApproach Approach { get; init; }
        public int ProberStrength { get; init; }
        public int TargetGuard { get; init; }
        public int Rank { get; init; } = 1;
        public int Alertness { get; init; }
        public bool Insider { get; init; }
        public bool Neighbours { get; init; }
        public int TargetDistrust { get; init; }
        public int Stones { get; init; }
        public FactionPersonality TargetTemper { get; init; }

        /// <summary>Points of chance lent from elsewhere (a great partner's sight).</summary>
        public int Bonus { get; init; }
    }

    /// <summary>
    /// What a pierced secret is worth (balance.json « dealings »; 2026-09-27; interpretations): the blackmail's price and the
    /// resentment it leaves by rank, how much stronger than the clan a power may be and still fear it, the distrust an
    /// exposure spreads by rank and the holder's hatred, the sale's price by rank and the chance the holder learns who
    /// sold it, and how often a power that pierced a secret of the clan exposes it.
    /// </summary>
    public sealed record DealingSettings
    {
        public List<int> BlackmailPriceByRank { get; init; } = new List<int>();
        public List<int> ResentmentByRank { get; init; } = new List<int>();
        public int FearMargin { get; init; }
        public List<int> ExposeDistrustByRank { get; init; } = new List<int>();
        public int ExposeRelation { get; init; }
        public List<int> SellPriceByRank { get; init; } = new List<int>();
        public double SellLeakChance { get; init; }
        public int SoldResentment { get; init; }
        public double AiExposeChance { get; init; }

        /// <summary>A secret exposed by a power reaches the others as a rumour: this share of the proof a probe would give.</summary>
        public double ExposedEvidenceShare { get; init; } = 1;
        public int AiExposeRelation { get; init; }
    }

    /// <summary>
    /// Secrets and probes (balance.json « secrets »; user decision 2026-09-27; interpretations of D7): the clues each rank
    /// takes, the proof a pierced secret of the clan gives, each approach's odds, depth, gain, risk of being seen and of
    /// disaster, how strength, vigilance, insiders, neighbours, distrust, bribes and temper weigh, how partners and the
    /// target's allies behave, the mirror's sight, the powers' own probes, and the secrets the powers hold from the start.
    /// </summary>
    public sealed record SecretSettings
    {
        public List<int> RankThreshold { get; init; } = new List<int>();
        public List<int> KnownEvidenceByRank { get; init; } = new List<int>();

        public Dictionary<ProbeApproach, int> BaseChance { get; init; } = new Dictionary<ProbeApproach, int>();
        public Dictionary<ProbeApproach, int> RankPenalty { get; init; } = new Dictionary<ProbeApproach, int>();
        public Dictionary<ProbeApproach, int> Gain { get; init; } = new Dictionary<ProbeApproach, int>();
        public Dictionary<ProbeApproach, double> DetectChance { get; init; } = new Dictionary<ProbeApproach, double>();
        public Dictionary<ProbeApproach, double> DisasterChance { get; init; } = new Dictionary<ProbeApproach, double>();
        public Dictionary<FactionPersonality, Dictionary<ProbeApproach, int>> TemperMod { get; init; } =
            new Dictionary<FactionPersonality, Dictionary<ProbeApproach, int>>();

        public double StrengthWeight { get; init; }
        public double AlertnessWeight { get; init; }
        public double AlertDetect { get; init; }
        public int InsiderBonus { get; init; }
        public int NeighbourBonus { get; init; }
        public double DistrustWeight { get; init; }
        public int BribeUnit { get; init; } = 1;
        public Dictionary<FactionPersonality, double> BribeTemper { get; init; } = new Dictionary<FactionPersonality, double>();
        public int MinPercent { get; init; }
        public int MaxPercent { get; init; } = 100;

        public double PartnerWeight { get; init; }
        public double PartnerLeakChance { get; init; }
        public int PartnerMinRelation { get; init; }

        public double AllyPromptBase { get; init; }
        public double AllyDistrustWeight { get; init; }
        public Dictionary<FactionPersonality, double> AllyLinger { get; init; } = new Dictionary<FactionPersonality, double>();
        public double AllyGuardWeight { get; init; }
        public double LateWitnessShare { get; init; }

        public int MirrorSightCostPerRank { get; init; }
        public int KnowerSightPenalty { get; init; }

        public int DetectedDistrust { get; init; }
        public int DetectedRelation { get; init; }
        public int AlertnessPerProbe { get; init; }
        public int AlertnessDetectedExtra { get; init; }
        public int AlertnessDecay { get; init; }
        public double MirrorShare { get; init; }
        public int PatrolGuardPoints { get; init; }

        public double AiProbeChance { get; init; }
        public int AiProbeSuspicion { get; init; }
        public int AiProbeDistrust { get; init; }
        public int MaxAiProbesPerYear { get; init; }
        public double AiPartnerChance { get; init; }

        public int PowerSecretsMin { get; init; }
        public int PowerSecretsMax { get; init; }
    }
}
