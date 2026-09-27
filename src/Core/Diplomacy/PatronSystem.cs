using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Pacts of the very high level (user decision 2026-09-27): with a great beast or a lone figure (patrons.json), only
    /// for a clan strong enough. A yearly tribute keeps the partner's favour; its boon protects the clan (an ambush
    /// foiled), teaches it (fragments of technique) or lends it its sight (the clan's probes). Unpaid, favour falls; at
    /// nothing, the partner's wrath kills the clan's weakest member and ends the pact — one mistake can be fatal. The clan
    /// may end a pact: safely in good favour, at the partner's wrath otherwise. Each action answers with its refusal, or null.
    /// </summary>
    public sealed class PatronSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly List<PatronPact> pacts = new List<PatronPact>();

        public PatronSystem(GameContext ctx, ClanManager clan, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
        }

        private PatronSettings Settings => ctx.Content.Balance.Patrons;

        public IReadOnlyList<PatronPact> Pacts => pacts;

        public void RestorePacts(IEnumerable<PatronPact> saved)
        {
            pacts.Clear();
            if (saved != null) pacts.AddRange(saved.Where(p => p != null && Patron(p.PatronId) != null));
        }

        private PatronDefinition Patron(string id) => ctx.Content.Patrons.FirstOrDefault(p => p.Id == id);

        private IEnumerable<PatronDefinition> Bound(PatronBoon boon) => pacts.Select(p => Patron(p.PatronId)).Where(p => p?.Boon == boon);

        /// <summary>The chance a great partner's protection foils an ambush (the strongest one's).</summary>
        public double GuardChance => Bound(PatronBoon.Protection).Select(p => p.BoonStrength / 100.0).DefaultIfEmpty(0).Max();

        /// <summary>Points of chance a great partner's sight lends the clan's probes.</summary>
        public int SightBonus => Bound(PatronBoon.Sight).Sum(p => p.BoonStrength);

        /// <summary>Why the partner will not hear the clan now; null when it will.</summary>
        public string Refusal(string patronId)
        {
            var patron = Patron(patronId);
            if (patron == null) return "puissance inconnue";
            if (pacts.Any(p => p.PatronId == patronId)) return "un pacte vous lie déjà";
            var strongest = clan.LivingMembers.Where(m => m.CaptorFaction == null).Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if (strongest < patron.MinClanRealm) return $"elle ne traite qu'avec les forts ({Characters.RankCatalog.RealmName(patron.MinClanRealm)} requis)";
            return resources.SpiritStones < patron.Tribute ? $"il faut {patron.Tribute} pierres pour le premier tribut" : null;
        }

        public string Propose(string patronId)
        {
            string refusal = Refusal(patronId);
            if (refusal != null) return refusal;
            var patron = Patron(patronId);
            resources.ConsumeSpiritStones(patron.Tribute);
            pacts.Add(new PatronPact(patronId, ctx.Clock.Year, Settings.StartFavor));
            ctx.Log.Info($"[Patrons] The clan makes a pact with {patron.Name}.");
            return null;
        }

        /// <summary>The clan ends a pact: safe in good favour; otherwise the partner's wrath.</summary>
        public string End(string patronId)
        {
            var pact = pacts.FirstOrDefault(p => p.PatronId == patronId);
            if (pact == null) return "aucun pacte";
            pacts.Remove(pact);
            if (pact.Favor < Settings.SafeEndFavor) Wrath(Patron(patronId));
            return null;
        }

        public void ProcessYear()
        {
            var s = Settings;
            foreach (var pact in pacts.ToList())
            {
                var patron = Patron(pact.PatronId);
                bool paid = resources.ConsumeSpiritStones(patron.Tribute);
                int favor = System.Math.Min(s.MaxFavor, pact.Favor + (paid ? s.PaidFavor : -s.UnpaidFavorLoss));
                if (favor <= 0)
                {
                    pacts.Remove(pact);
                    Wrath(patron);
                    continue;
                }
                pacts[pacts.IndexOf(pact)] = pact with { Favor = favor };
                if (paid && patron.Boon == PatronBoon.Insight) resources.AddTechniqueFragments(patron.BoonStrength);
            }
        }

        /// <summary>The partner's wrath: the clan's weakest member dies.</summary>
        private void Wrath(PatronDefinition patron)
        {
            var victim = clan.LivingMembers.OrderBy(m => (int)m.Realm).ThenBy(m => m.RealmStage).ThenBy(m => m.Age).FirstOrDefault();
            if (victim != null) clan.Kill(victim, DeathCause.Combat);
            ctx.Log.Warning($"[Patrons] {patron?.Name ?? "A great partner"} turns its wrath on the clan.");
            ctx.Events.TriggerPatronWrath(patron?.Name);
        }
    }
}
