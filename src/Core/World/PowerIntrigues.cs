using System;
using System.Collections.Generic;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers' blackmail and spies against each other (P1; the user's choices of 2026-10-12): the intrigues the clan
    /// suffers befall the world too. See <see cref="PowerIntrigueSettings"/> for the rules; the clan is never their target.
    /// </summary>
    public sealed class PowerIntrigues
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly PowerPoliticsSystem politics;
        private readonly PowerSchemeSystem schemes;
        private readonly SecretBook secrets;
        private readonly SuspicionLedger suspicion;
        private readonly List<PowerSpy> spies = new List<PowerSpy>();
        private readonly Dictionary<string, int> quietUntil = new Dictionary<string, int>();

        public PowerIntrigues(GameContext ctx, FactionManager factions, PowerPoliticsSystem politics, PowerSchemeSystem schemes,
            SecretBook secrets, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.politics = politics;
            this.schemes = schemes;
            this.secrets = secrets;
            this.suspicion = suspicion;
        }

        public IReadOnlyList<PowerSpy> Spies => spies;

        /// <summary>Until which year a blackmailer keeps quiet on a victim that paid (key « blackmailer|victim »).</summary>
        public IReadOnlyDictionary<string, int> QuietUntil => quietUntil;

        public void Restore(IEnumerable<PowerSpy> savedSpies, IReadOnlyDictionary<string, int> savedQuiet)
        {
            spies.Clear();
            if (savedSpies != null) spies.AddRange(savedSpies);
            quietUntil.Clear();
            if (savedQuiet != null) foreach (var kv in savedQuiet) quietUntil[kv.Key] = kv.Value;
        }

        public void ProcessYear() { }

        /// <summary>A blackmail on a victim whose secret the blackmailer knows; false when it knows none worth it, or keeps quiet.</summary>
        public bool Blackmail(FactionData blackmailer, FactionData victim) => throw new NotImplementedException();

        /// <summary>An agent planted in a neighbour; false when one already hides there.</summary>
        public bool Plant(FactionData owner, FactionData host) => throw new NotImplementedException();
    }
}
