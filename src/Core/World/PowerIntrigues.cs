using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>Until which year a blackmailer leaves a victim be, paid or refused (key « blackmailer|victim »).</summary>
        public IReadOnlyDictionary<string, int> QuietUntil => quietUntil;

        public void Restore(IEnumerable<PowerSpy> savedSpies, IReadOnlyDictionary<string, int> savedQuiet)
        {
            spies.Clear();
            if (savedSpies != null) spies.AddRange(savedSpies);
            quietUntil.Clear();
            if (savedQuiet != null) foreach (var kv in savedQuiet) quietUntil[kv.Key] = kv.Value;
        }

        private PowerIntrigueSettings Settings => ctx.Content.Balance.PowerIntrigues;

        public void ProcessYear()
        {
            foreach (var stale in quietUntil.Where(kv => kv.Value < ctx.Clock.Year).Select(kv => kv.Key).ToList()) quietUntil.Remove(stale);
            Spy();
            foreach (var power in factions.Factions.OrderBy(_ => ctx.Rng.Next()).ToList()) // no power always first
            {
                double factor = Settings.PersonalityFactor.TryGetValue(power.Personality, out var f) ? f : 1.0;
                if (ctx.Rng.Chance(Settings.BlackmailChance * factor))
                {
                    var victims = secrets.KnownBy(power.Name).Select(s => factions.GetFactionByName(s.Holder))
                        .Where(v => v != null && v != power).Distinct().ToList(); // the clan's secrets are its own matter
                    if (victims.Count > 0) Blackmail(power, victims[ctx.Rng.Next(victims.Count)]);
                }
                if (ctx.Rng.Chance(Settings.SpyChance * factor))
                {
                    var hosts = factions.Factions.Where(h => schemes.MayScheme(power, h) && !spies.Any(s => s.Owner == power.Name && s.Host == h.Name)).ToList();
                    if (hosts.Count > 0) Plant(power, hosts[ctx.Rng.Next(hosts.Count)]);
                }
            }
        }

        /// <summary>A blackmail on a victim whose secret the blackmailer knows, paid or refused; false when it knows none, the victim has nothing, or the matter rests.</summary>
        public bool Blackmail(FactionData blackmailer, FactionData victim)
        {
            if (blackmailer == null || victim == null || blackmailer == victim || politics.Married(blackmailer.Name, victim.Name)) return false;
            if (!secrets.KnownBy(blackmailer.Name).Any(s => s.Holder == victim.Name)) return false;
            string key = $"{blackmailer.Name}|{victim.Name}";
            if (quietUntil.TryGetValue(key, out int until) && ctx.Clock.Year <= until) return false; // paid, or refused: the matter rests

            var s = Settings;
            int price = (int)(Math.Max(0, victim.Wealth) * s.BlackmailWealthShare);
            if (price <= 0) return false; // nothing to squeeze from a pauper
            if (victim.PowerLevel >= blackmailer.PowerLevel * s.RefuseStrengthRatio)
            {
                foreach (var other in factions.Factions.Where(o => o != victim && o != blackmailer))
                    suspicion.AddDistrust(other.Name, victim.Name, s.RefusalDistrust); // the secret spread
                politics.Feud(victim, blackmailer);
                quietUntil[key] = ctx.Clock.Year + s.QuietYears; // the secret is out: no second demand soon
                ctx.Log.Info($"[PowerIntrigues] {victim.Name} refuses {blackmailer.Name}'s blackmail: its secret spreads, and it strikes back.");
                ctx.Events.TriggerPowerScheme("chantage refusé", blackmailer.Name, victim.Name);
                return true;
            }
            victim.Wealth -= price;
            blackmailer.Wealth += price;
            quietUntil[key] = ctx.Clock.Year + s.QuietYears;
            ctx.Log.Info($"[PowerIntrigues] {victim.Name} pays {blackmailer.Name} {price} stones for its silence.");
            ctx.Events.TriggerPowerScheme("chantage", blackmailer.Name, victim.Name);
            return true;
        }

        /// <summary>An agent planted in a neighbour; false when one already hides there.</summary>
        public bool Plant(FactionData owner, FactionData host)
        {
            if (owner == null || host == null || owner == host || spies.Any(s => s.Owner == owner.Name && s.Host == host.Name)) return false;
            spies.Add(new PowerSpy(owner.Name, host.Name, ctx.Clock.Year));
            ctx.Log.Info($"[PowerIntrigues] {owner.Name} plants an agent in {host.Name}.");
            return true;
        }

        /// <summary>Each spy brings its clues, unless its host or master is gone, or it is unmasked: executed, its host striking back.</summary>
        private void Spy()
        {
            var s = Settings;
            foreach (var spy in spies.ToList())
            {
                var owner = factions.GetFactionByName(spy.Owner);
                var host = factions.GetFactionByName(spy.Host);
                if (owner == null || host == null || !schemes.MayScheme(owner, host)) { spies.Remove(spy); continue; } // gone, allied, bound or beyond reach
                if (ctx.Rng.Chance(s.SpyUnmaskChance))
                {
                    spies.Remove(spy);
                    suspicion.AddDistrust(host.Name, owner.Name, s.UnmaskDistrust);
                    politics.Feud(host, owner);
                    ctx.Log.Info($"[PowerIntrigues] {host.Name} unmasks and executes an agent of {owner.Name}.");
                    ctx.Events.TriggerPowerScheme("espion démasqué", owner.Name, host.Name);
                    continue;
                }
                secrets.AddClues(owner.Name, host.Name, s.SpyCluesPerYear);
            }
        }
    }
}
