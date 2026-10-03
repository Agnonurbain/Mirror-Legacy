using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Presentation
{
    public enum GoldenCoreAction { Decipher, Forge, Permission, Claim, Transfer, Transform, Transmute, Treasure, Designation, Unseal, Subdue, Bribe }

    /// <summary>An action of the Golden Core: who, toward what, its line with its odds or price, and why not (null: it can).</summary>
    public sealed record GoldenCoreOption(GoldenCoreAction Kind, string MemberId, string Target, string Label, string Refusal);

    /// <summary>
    /// The Golden Core's actions as the player sees them (L4e, G6, 2026-10-03): decipher a gold-seeking method, forge the
    /// metal essence, ask the holder's leave and the position, Transfer, Transformation and Transmutation; a Dharma Treasure
    /// and its Designation; and the Underworld's matters — subdue a demon let be, soothe a grudge.
    /// </summary>
    public static class GoldenCoreView
    {
        private static string Lineage(GameSession s, string id) => s.Context.Content.Fruitions.FirstOrDefault(f => f.Id == id)?.Name ?? id;

        private static string RouteLabel(PositionRoute route) => route switch
        {
            PositionRoute.Realization => "la Réalisation",
            PositionRoute.Surplus => "un Surplus",
            PositionRoute.IntercalaryBridge => "un Intercalaire par un pont",
            _ => "un Intercalaire"
        };

        public static IReadOnlyList<GoldenCoreOption> Actions(GameSession s)
        {
            var options = new List<GoldenCoreOption>();
            var gc = s.Context.Content.Balance.GoldenCore;
            foreach (var m in s.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm >= CultivationRealm.PurpleMansion))
            {
                foreach (var (target, route, knows) in s.GoldenCore.ForgeTargets(m))
                {
                    bool specialised = route == PositionRoute.IntercalaryThreeTwo;
                    if (!knows)
                    {
                        int cost = specialised ? gc.SpecialisedMirrorCost : gc.GoldSeekingMirrorCost;
                        options.Add(new GoldenCoreOption(GoldenCoreAction.Decipher, m.ID, target,
                            $"{m.FullName} : le miroir déchiffre la recherche d'or {(specialised ? "spécialisée " : "")}de {Lineage(s, target)} ({cost})", s.Mirror.PayRefusal(cost)));
                        continue;
                    }
                    options.Add(new GoldenCoreOption(GoldenCoreAction.Forge, m.ID, target,
                        $"{m.FullName} : forger l'essence métallique vers {RouteLabel(route)} de {Lineage(s, target)} — {s.GoldenCore.ForgeOdds(m)} %", null));
                }
                if (m.GoldenCore == GoldenCoreState.MetallicEssenceOnly && m.FruitionId != null)
                {
                    string refusal = s.GoldenCore.ClaimRefusal(m);
                    var state = s.Fruitions.State(m.FruitionId);
                    if (refusal != null && state?.Status == FruitionStatus.Occupied && !s.GoldenCore.Permissions.ContainsKey(m.FruitionId))
                    {
                        var tribute = Tribute(s, m.FruitionId);
                        options.Add(new GoldenCoreOption(GoldenCoreAction.Permission, m.ID, m.FruitionId,
                            $"{m.FullName} : offrir un tribut à {state.Holder} pour sa permission ({(int)(gc.PermissionChance * 100)} %)"
                            + (tribute == null ? "" : $" : {string.Join(", ", tribute.Select(t => t.Label))}"),
                            tribute == null ? "le clan n'a rien qui vaille un tel tribut (les pierres n'y comptent pas)" : null));
                    }
                    options.Add(new GoldenCoreOption(GoldenCoreAction.Claim, m.ID, m.FruitionId,
                        $"{m.FullName} : demander sa position dans {Lineage(s, m.FruitionId)} — {s.GoldenCore.ClaimOdds(m)} %", refusal));
                }
                bool ownFree = m.FruitionId != null && s.Fruitions.State(m.FruitionId)?.Status == FruitionStatus.Free;
                if (m.GoldenCore == GoldenCoreState.Surplus)
                    options.Add(new GoldenCoreOption(GoldenCoreAction.Transfer, m.ID, m.FruitionId, $"{m.FullName} : Transfert vers la Réalisation de {Lineage(s, m.FruitionId)} — {gc.TransferChance} %",
                        ownFree ? null : "la Réalisation n'est pas libre"));
                if (m.GoldenCore == GoldenCoreState.Intercalary)
                    options.Add(new GoldenCoreOption(GoldenCoreAction.Transform, m.ID, m.FruitionId, $"{m.FullName} : Transformation vers la Réalisation de {Lineage(s, m.FruitionId)} — {gc.TransformationChance} %",
                        ownFree ? null : "la Réalisation n'est pas libre"));
                if (m.GoldenCore == GoldenCoreState.Realization)
                {
                    var origin = s.Context.Content.Fruitions.FirstOrDefault(f => f.Id == m.FruitionId);
                    foreach (var target in s.Context.Content.Fruitions.Where(f => origin != null && f.Id != origin.Id && f.Element == origin.Element
                                 && s.Fruitions.State(f.Id)?.Status == FruitionStatus.Free))
                        options.Add(new GoldenCoreOption(GoldenCoreAction.Transmute, m.ID, target.Id,
                            $"{m.FullName} : Transmutation vers {target.Name} — {s.GoldenCore.TransmutationOdds(m)} % (sinon, un démon)", null));
                }
                if (m.Realm >= CultivationRealm.GoldenCore && !m.HasDharmaTreasure && m.TreasureReadyYear == null)
                    options.Add(new GoldenCoreOption(GoldenCoreAction.Treasure, m.ID, null,
                        $"{m.FullName} : condenser un Trésor de Dharma ({s.Context.Content.Balance.Dharma.CondenseOres} minerais)", s.Dharma.CondenseRefusal(m)));
                if (m.HasDharmaTreasure)
                    options.Add(new GoldenCoreOption(GoldenCoreAction.Designation, m.ID, null,
                        $"{m.FullName} : hypothéquer son trésor sur sa Fruition (Désignation de Rang)", null));
            }
            foreach (var d in s.Dharma.Designations.Where(d => s.Dharma.MasterOf(d) == null))
                options.Add(new GoldenCoreOption(GoldenCoreAction.Unseal, null, d.Id, $"Desceller la Désignation de {d.MasterName}, sans maître : elle retourne à sa Fruition", null));
            foreach (var d in s.Demons.Ravaging)
                options.Add(new GoldenCoreOption(GoldenCoreAction.Subdue, null, d.Id, $"Soumettre le démon né de {d.Name}", null));
            if (s.Demons.Grudge > 0)
                options.Add(new GoldenCoreOption(GoldenCoreAction.Bribe, null, null, $"Offrir une essence au Monde Souterrain ({s.Demons.Grudge} ans de rancune)",
                    s.Demons.Essences == 0 ? "le clan ne garde aucune essence" : null));
            return options;
        }

        /// <summary>The cheapest tribute in kind for a lineage's leave, or null.</summary>
        public static IReadOnlyList<AccordCandidate> Tribute(GameSession s, string fruitionId)
        {
            int worth = s.Context.Content.Balance.GoldenCore.PermissionWorth;
            return AccordView.Bundle(AccordView.Offerings(s.Accords, s.Clan, s.Techniques, s.Resources, s.SecretBook, s.Artifacts,
                s.GoldenCore.TributeRecipient(fruitionId), worth, precious: true), worth);
        }

        /// <summary>Does it: null when done; else what came of it (French).</summary>
        public static string Perform(GameSession s, GoldenCoreOption o)
        {
            var m = o.MemberId == null ? null : s.Clan.FindById(o.MemberId);
            switch (o.Kind)
            {
                case GoldenCoreAction.Decipher:
                    bool specialised = s.GoldenCore.ForgeTargets(m).Any(t => t.FruitionId == o.Target && t.Route == PositionRoute.IntercalaryThreeTwo);
                    return s.GoldenCore.DecipherGoldSeeking(o.Target, specialised) ? null : "le miroir n'y parvient pas";
                case GoldenCoreAction.Forge:
                    if (!s.GoldenCore.Forge(m, o.Target)) return "la forge ne peut être tentée";
                    return m.IsAlive ? null : "l'essence prend vie : un Démon d'Essence Métallique est né";
                case GoldenCoreAction.Permission:
                    var tribute = Tribute(s, o.Target);
                    if (tribute == null) return "le clan n'a rien qui vaille un tel tribut";
                    return s.GoldenCore.RequestPermission(o.Target, tribute.Select(t => t.Term).ToList()) ? null : "le détenteur refuse le tribut";
                case GoldenCoreAction.Claim:
                    if (!s.GoldenCore.ClaimPosition(m)) return "la position ne peut être demandée";
                    return m.IsAlive ? null : "le Ciel refuse : un Démon d'Essence Métallique est né";
                case GoldenCoreAction.Transfer:
                    s.GoldenCore.Transfer(m);
                    return m.GoldenCore == GoldenCoreState.Realization ? null : "le Transfert échoue : son Dao est blessé";
                case GoldenCoreAction.Transform:
                    s.GoldenCore.Transform(m);
                    return m.GoldenCore == GoldenCoreState.Realization ? null : "la Transformation échoue : son Dao est blessé";
                case GoldenCoreAction.Transmute: return s.GoldenCore.Transmute(m, o.Target);
                case GoldenCoreAction.Treasure: return s.Dharma.Condense(m);
                case GoldenCoreAction.Designation: return s.Dharma.MakeDesignation(m);
                case GoldenCoreAction.Unseal: return s.Dharma.Unseal(o.Target);
                case GoldenCoreAction.Subdue: return s.Demons.Subdue(o.Target);
                case GoldenCoreAction.Bribe: return s.Demons.Bribe();
                default: return "action inconnue";
            }
        }
    }
}
