using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// The powers' own politics (LORE.md D7; user decision 2026-09-27). Each year a few powers ally — neighbours of one
    /// Dao who do not distrust each other; an aggressive power may feud with a weaker neighbour, taking its wealth and
    /// strength, the victim's allies distrusting the attacker; an expansionist power may subjugate a far weaker neighbour,
    /// take its tribute and, its grip complete, absorb it. A power bound to the clan by a treaty is neither subjugated nor
    /// enlisted against it. When enough powers suspect the clan, they band into a coalition that strikes it each year and
    /// schemes twice as much. An ally of the clan attacked calls it to arms: answering costs stones and the attacker's
    /// favour, refusing breaks the treaty, and a call left unanswered to the next year is a refusal.
    /// </summary>
    public sealed class PowerPoliticsSystem
    {
        private readonly GameContext ctx;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly TreatySystem treaties;
        private readonly List<PowerBond> bonds = new List<PowerBond>();

        public PowerPoliticsSystem(GameContext ctx, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            TreatySystem treaties)
        {
            this.ctx = ctx;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            this.treaties = treaties;
            ctx.Events.OnPowerAbsorbed += (vassal, suzerain) => { if (suzerain == World.SecretBook.ClanHolder) Forget(vassal); };
        }

        private PoliticsSettings Settings => ctx.Content.Balance.Politics;

        public IReadOnlyList<PowerBond> Bonds => bonds;
        public Coalition Coalition { get; private set; }
        private readonly List<CallToArms> calls = new List<CallToArms>();

        /// <summary>The first call to arms awaiting the clan's answer (others wait their turn); null when none.</summary>
        public CallToArms PendingCall => calls.FirstOrDefault();

        /// <summary>Every call awaiting an answer, the oldest first.</summary>
        public IReadOnlyList<CallToArms> PendingCalls => calls;

        public string SuzerainOf(string power) => bonds.FirstOrDefault(b => b.Kind == BondKind.Vassalage && b.B == power)?.A;

        public IReadOnlyList<string> AlliesOf(string power) =>
            bonds.Where(b => b.Kind == BondKind.Alliance && (b.A == power || b.B == power)).Select(b => b.A == power ? b.B : b.A).ToList();

        /// <summary>How much more a power schemes against the clan: a coalition's members, twice as much.</summary>
        public double SchemeFactor(FactionData power) =>
            Coalition?.Members.Contains(power.Name) == true ? Settings.CoalitionSchemeFactor : 1.0;

        public void RestoreBonds(IEnumerable<PowerBond> saved)
        {
            bonds.Clear();
            if (saved != null) bonds.AddRange(saved);
        }

        public void RestoreCoalition(Coalition saved) => Coalition = saved;
        public void RestoreCall(CallToArms saved) => RestoreCalls(saved == null ? null : new[] { saved });

        public void RestoreCalls(IEnumerable<CallToArms> saved)
        {
            calls.Clear();
            if (saved != null) calls.AddRange(saved.Where(c => c != null));
        }

        public void ProcessYear()
        {
            while (PendingCall != null && PendingCall.Year < ctx.Clock.Year) RefuseCall(); // every call left unanswered: a refusal
            FormAlliances();
            if (ctx.Rng.Chance(Settings.FeudChance)) StartAFeud();
            if (ctx.Rng.Chance(Settings.VassalizeChance)) SubjugateAWeakNeighbour();
            ServeVassals();
            ProcessCoalition();
        }

        // ---- Alliances ----

        private void FormAlliances()
        {
            var s = Settings;
            var powers = factions.Factions.ToList();
            var pairs = powers.SelectMany((a, i) => powers.Skip(i + 1).Select(b => (a, b)))
                .Where(p => !Bonded(p.a.Name, p.b.Name))
                .Select(p => (p.a, p.b, affinity: PoliticsRules.Affinity(p.a, p.b, factions.AreNeighbours(p.a, p.b),
                    suspicion.Distrust(p.a.Name, p.b.Name) + suspicion.Distrust(p.b.Name, p.a.Name), s)))
                .Where(p => p.affinity >= s.AllianceAffinity)
                .OrderByDescending(p => p.affinity).ThenBy(p => p.a.Name, StringComparer.Ordinal).ThenBy(p => p.b.Name, StringComparer.Ordinal)
                .ToList();
            int formed = 0;
            foreach (var (a, b, _) in pairs)
            {
                if (formed >= s.MaxBondsPerYear) return;
                if (!ctx.Rng.Chance(s.AllianceChance)) continue;
                bonds.Add(new PowerBond($"bond-{a.ID}-{b.ID}-{ctx.Clock.Year}", BondKind.Alliance, a.Name, b.Name, ctx.Clock.Year, false));
                ctx.Log.Info($"[Politics] {a.Name} and {b.Name} ally.");
                formed++;
            }
        }

        private bool Bonded(string a, string b) => bonds.Any(x => (x.A == a && x.B == b) || (x.A == b && x.B == a));

        // ---- Feuds ----

        private void StartAFeud()
        {
            var attackers = factions.Factions.Where(f => f.Personality is FactionPersonality.Aggressive or FactionPersonality.Expansionist).ToList();
            var feuds = attackers.SelectMany(a => factions.Factions
                    .Where(d => factions.AreNeighbours(a, d) && d.PowerLevel < a.PowerLevel && !Bonded(a.Name, d.Name))
                    .Select(d => (a, d)))
                .ToList();
            if (feuds.Count == 0) return;
            var (attacker, defender) = ctx.Rng.Pick(feuds);
            Feud(attacker, defender);
        }

        /// <summary>A power strikes another: wealth and strength taken; the victim's allies distrust it; an ally of the clan calls it to arms.</summary>
        public void Feud(FactionData attacker, FactionData defender)
        {
            var s = Settings;
            int taken = (int)(defender.Wealth * s.FeudWealthShare);
            defender.Wealth -= taken;
            attacker.Wealth += taken;
            defender.PowerLevel -= (int)(defender.PowerLevel * s.FeudPowerShare);
            foreach (var ally in AlliesOf(defender.Name).Where(a => a != attacker.Name))
                suspicion.AddDistrust(ally, attacker.Name, s.AllyDistrust);
            ctx.Log.Info($"[Politics] {attacker.Name} strikes {defender.Name}.");
            if (treaties.Has(defender.Name, TreatyKind.Defence)) CallClanToArms(defender.Name, attacker.Name);
        }

        // ---- The call to arms ----

        /// <summary>An ally of the clan, attacked, calls it to arms — once, queued behind any other call.</summary>
        public void CallClanToArms(string ally, string attacker)
        {
            if (calls.Any(c => c.Ally == ally)) return;
            calls.Add(new CallToArms(ally, attacker, ctx.Clock.Year));
            ctx.Events.TriggerCallToArms(ally, attacker);
        }

        public string AnswerCall()
        {
            var call = PendingCall;
            if (call == null) return "aucun appel aux armes";
            var s = Settings;
            if (!resources.ConsumeSpiritStones(s.CallStonesCost)) return $"il faut {s.CallStonesCost} pierres spirituelles";
            ChangeRelation(call.Attacker, s.CallAttackerRelation);
            ChangeRelation(call.Ally, s.CallAllyRelation);
            calls.RemoveAt(0);
            ctx.Log.Info($"[Politics] The clan answers {call.Ally}'s call against {call.Attacker}.");
            return null;
        }

        public string RefuseCall()
        {
            var call = PendingCall;
            if (call == null) return "aucun appel aux armes";
            calls.RemoveAt(0);
            var treaty = treaties.With(call.Ally).FirstOrDefault(t => t.Kind == TreatyKind.Defence);
            if (treaty != null) treaties.Break(treaty.Id); // an ally abandoned: the treaty is broken
            ctx.Log.Warning($"[Politics] The clan leaves {call.Ally} alone against {call.Attacker}.");
            return null;
        }

        // ---- Vassals ----

        private void SubjugateAWeakNeighbour()
        {
            var s = Settings;
            var options = factions.Factions.Where(f => f.Personality == FactionPersonality.Expansionist)
                .SelectMany(a => factions.Factions
                    .Where(d => factions.AreNeighbours(a, d) && d.PowerLevel * s.VassalizePowerRatio <= a.PowerLevel)
                    .Select(d => (a, d)))
                .ToList();
            if (options.Count == 0) return;
            var (suzerain, vassal) = ctx.Rng.Pick(options);
            Subjugate(suzerain, vassal);
        }

        /// <summary>A power takes another as vassal; false when the vassal is bound to the clan, already served or itself.</summary>
        public bool Subjugate(FactionData suzerain, FactionData vassal)
        {
            if (suzerain == null || vassal == null || suzerain == vassal || treaties.With(vassal.Name).Count > 0
                || SuzerainOf(vassal.Name) != null || SuzerainOf(suzerain.Name) == vassal.Name) return false;
            bonds.RemoveAll(b => b.Kind == BondKind.Alliance && ((b.A == suzerain.Name && b.B == vassal.Name) || (b.A == vassal.Name && b.B == suzerain.Name)));
            bonds.Add(new PowerBond($"bond-{suzerain.ID}-{vassal.ID}-{ctx.Clock.Year}", BondKind.Vassalage, suzerain.Name, vassal.Name, ctx.Clock.Year, false));
            ctx.Log.Info($"[Politics] {suzerain.Name} takes {vassal.Name} as vassal.");
            return true;
        }

        private void ServeVassals()
        {
            var s = Settings;
            foreach (var id in bonds.Where(b => b.Kind == BondKind.Vassalage).Select(b => b.Id).ToList())
            {
                var bond = bonds.FirstOrDefault(b => b.Id == id); // as it stands now: an absorption this year may have changed it
                if (bond == null) continue;
                var suzerain = factions.GetFactionByName(bond.A);
                var vassal = factions.GetFactionByName(bond.B);
                if (suzerain == null || vassal == null)
                {
                    bonds.RemoveAll(b => b.Id == bond.Id);
                    continue;
                }
                if (bond.Grip >= s.GripThreshold)
                {
                    Absorb(suzerain, vassal);
                    continue;
                }
                int tribute = (int)(Math.Max(0, vassal.Wealth) * s.VassalTributeShare);
                vassal.Wealth -= tribute;
                suzerain.Wealth += tribute;
                bonds[bonds.FindIndex(b => b.Id == bond.Id)] = bond with { Grip = bond.Grip + s.GripPerYear };
            }
        }

        /// <summary>The vassal is no more: its wealth, half its strength and its arts go to its suzerain.</summary>
        /// <summary>A power the clan absorbed leaves the powers' web: its bonds, its calls, its place in a coalition.</summary>
        private void Forget(string power)
        {
            bonds.RemoveAll(b => b.A == power || b.B == power);
            calls.RemoveAll(c => c.Ally == power || c.Attacker == power);
            if (Coalition != null) Coalition = Coalition with { Members = Coalition.Members.Where(m => m != power).ToList() };
        }

        /// <summary>
        /// A power fallen (no elder left, or ruined) disperses (the living world, 2026-10-01): its heir — its strongest
        /// neighbour — gathers what remains, by the absorption's own path (its arts, its wealth, its shard, its bonds).
        /// </summary>
        public void Disperse(FactionData fallen, FactionData heir)
        {
            ctx.Events.TriggerPowerFell(fallen.Name, heir.Name);
            Absorb(heir, fallen);
        }

        private void Absorb(FactionData suzerain, FactionData vassal)
        {
            suzerain.Wealth += Math.Max(0, vassal.Wealth);
            suzerain.PowerLevel += vassal.PowerLevel / 2;
            foreach (var art in vassal.Techniques.Where(t => !suzerain.Techniques.Contains(t)).ToList()) suzerain.Techniques.Add(art);
            for (int i = 0; i < bonds.Count; i++) // the absorbed power's own vassals now serve its suzerain
                if (bonds[i].Kind == BondKind.Vassalage && bonds[i].A == vassal.Name && bonds[i].B != suzerain.Name)
                    bonds[i] = bonds[i] with { A = suzerain.Name };
            bonds.RemoveAll(b => b.A == vassal.Name || b.B == vassal.Name);
            if (calls.RemoveAll(c => c.Ally == vassal.Name || c.Attacker == vassal.Name) > 0) // a call lapses: one side is no more
                ctx.Log.Info($"[Politics] A call to arms lapses: {vassal.Name} is no more.");
            suspicion.Inherit(vassal.Name, suzerain.Name);
            if (Coalition != null) Coalition = Coalition with { Members = Coalition.Members.Where(m => m != vassal.Name).ToList() };
            factions.RemoveFaction(vassal.Name);
            ctx.Log.Warning($"[Politics] {suzerain.Name} absorbs {vassal.Name}.");
            ctx.Events.TriggerPowerAbsorbed(vassal.Name, suzerain.Name);
        }

        // ---- A coalition against the clan ----

        private void ProcessCoalition()
        {
            var s = Settings;
            if (Coalition == null)
            {
                var members = factions.Factions
                    .Where(f => suspicion.OfClan(f.Name) >= s.CoalitionSuspicion && treaties.With(f.Name).Count == 0)
                    .Select(f => f.Name).ToList();
                if (members.Count < s.CoalitionMinMembers) return;
                Coalition = new Coalition(members, s.CoalitionYears);
                ctx.Log.Warning($"[Politics] A coalition forms against the clan: {string.Join(", ", members)}.");
                ctx.Events.TriggerCoalitionFormed(members);
                return;
            }

            var standing = Coalition.Members.Where(m => factions.GetFactionByName(m) != null).ToList();
            if (standing.Count > 0)
            {
                resources.ConsumeSpiritStones((int)(resources.SpiritStones * s.CoalitionStonesShare));
                foreach (var member in standing) ChangeRelation(member, s.CoalitionRelation);
                ctx.Log.Warning("[Politics] The coalition strikes the clan.");
            }
            int left = Coalition.YearsLeft - 1;
            Coalition = left > 0 && standing.Count > 0 ? new Coalition(standing, left) : null;
        }

        private void ChangeRelation(string faction, int amount)
        {
            var power = factions.GetFactionByName(faction);
            if (power != null) factions.ChangeRelation(power.ID, amount);
        }
    }
}
