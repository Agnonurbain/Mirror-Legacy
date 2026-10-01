using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// An agent of a power the clan holds (L6a; saved): taken when an ambush failed. It can be interrogated once and
    /// denounced once — proof of its power's scheme.
    /// </summary>
    public sealed record Prisoner(string Id, string Faction, CultivationRealm Realm, int Year)
    {
        public bool Interrogated { get; init; }
        public bool Denounced { get; init; }
    }

    /// <summary>
    /// The powers' schemes against the clan for profit, and the captives on both sides (balance.json « schemes », L6a;
    /// LORE.md D7 « everything is a plot »; every figure an interpretation, to replace when a source speaks).
    /// </summary>
    public sealed record SchemeSettings
    {
        /// <summary>A power this hostile takes a hostage even from a clan that could not ransom it (2026-10-01, interpretation).</summary>
        public int HostageRelation { get; init; } = -50;

        /// <summary>
        /// A power's yearly chance of scheming: the base, times its temper (by personality), times its hostility
        /// (1 + hostility / 100 × weight), damped by friendship (1 − friendship / 100 × damping), times its greed for the
        /// clan's stones (1 + stones / reference, at most MaxGreed).
        /// </summary>
        public double BaseChance { get; init; }
        public Dictionary<FactionPersonality, double> PersonalityFactors { get; init; }
        public double HostilityWeight { get; init; }
        public double FriendshipDamping { get; init; }
        public int WealthReference { get; init; } = 1;
        public double MaxGreed { get; init; } = 1;

        /// <summary>At most this many ambushes a year, all powers together (the powers scheme, they do not swarm).</summary>
        public int MaxAmbushesPerYear { get; init; } = 1;

        /// <summary>The tasks that take a member away from the domain, where an ambush can reach them.</summary>
        public List<TaskType> AwayTasks { get; init; }

        /// <summary>The ambush, in percent: base + (the power's strongest realm − the member's) × per realm; failing, the agent may be taken.</summary>
        public int CaptureBase { get; init; }
        public int CapturePerRealm { get; init; }
        public double AgentTakenChance { get; init; }

        /// <summary>The captor's patience: past it, each year the captive may be executed.</summary>
        public int PatienceYears { get; init; } = 1;
        public double ExecutionChance { get; init; }

        /// <summary>Interrogation loosens tongues: the yearly leak chance times this factor.</summary>
        public double InterrogationFactor { get; init; } = 1;

        /// <summary>A ransom: base × factor^realm stones; paying it softens the captor.</summary>
        public int RansomBase { get; init; }
        public double RansomRealmFactor { get; init; } = 1;
        public int PaidRansomRelation { get; init; }

        /// <summary>The mirror blurs what a captive knows of it.</summary>
        public int SilenceMirrorCost { get; init; }

        /// <summary>
        /// A rescue, in percent: base + (the team's power − the captor's strongest at this stage) × per point; success
        /// angers the captor, failure makes it wonder about the clan.
        /// </summary>
        public int RescueBase { get; init; }
        public int RescuePerPowerPoint { get; init; }
        public int CaptorStage { get; init; }
        public int RescueRelation { get; init; }
        public int FailedRescueSuspicion { get; init; }

        /// <summary>What becomes of an agent: sold back, released, interrogated, denounced, executed.</summary>
        public int SellBackRelation { get; init; }
        public int ReleaseRelation { get; init; }
        public int InterrogationFragments { get; init; }
        public int DenounceDistrust { get; init; }
        public int DenounceRelation { get; init; }
        public int ExecuteRelation { get; init; }
        public int ExecuteWitnessRelation { get; init; }
    }
}
