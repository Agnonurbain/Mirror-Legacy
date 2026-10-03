using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    public enum MinorDeal { Deduce, Learn }

    /// <summary>A way to a lineage's minor ability: its kind, the power (learn), its line, the gifts, and why not (null: it can).</summary>
    public sealed record MinorOption(MinorDeal Kind, string Power, string Label, IReadOnlyList<AccordTerm> Terms, string Refusal);

    /// <summary>A lineage's minor abilities as the clan knows them, how many it does not, and the ways to learn one.</summary>
    public sealed record MinorAbilityLine(string Lineage, string Name, IReadOnlyList<string> Known, int Unknown, IReadOnlyList<MinorOption> Options);

    /// <summary>
    /// The minor abilities of former True Monarchs as the player sees them (G6, 2026-10-03): the lineages where the clan knows
    /// some, could deduce them, or knows a power holding the lineage; for each, the mirror's deduction, a holder's teaching in
    /// kind — never stones. No theft: an ability is no thing to steal (2026-10-03).
    /// </summary>
    public static class MinorAbilityView
    {
        public static IReadOnlyList<MinorAbilityLine> Lines(GameSession s)
        {
            var minors = s.Minors;
            var settings = s.Context.Content.Balance.MinorAbilities;
            var lines = new List<MinorAbilityLine>();
            foreach (var f in s.Context.Content.Fruitions.Where(f => f.Abilities.Any(a => a.Substitute)))
            {
                var known = minors.Known(f.Id);
                var holders = minors.HoldersOf(f.Id);
                string deduce = minors.DeduceRefusal(f.Id);
                if (known.Count == 0 && holders.Count == 0 && deduce != null) continue;
                int unknown = minors.UnknownCount(f.Id);
                var options = new List<MinorOption>();
                if (unknown > 0)
                {
                    options.Add(new MinorOption(MinorDeal.Deduce, null, $"Le miroir déduit ses capacités mineures ({settings.MirrorCost})", null, deduce));
                    foreach (var p in holders)
                    {
                        var gifts = AccordView.Bundle(AccordView.Offerings(s.Accords, s.Clan, s.Techniques, s.Resources, s.SecretBook, s.Artifacts,
                            p.Name, settings.Worth, precious: true), settings.Worth);
                        string refusal = p.RelationWithPlayer < settings.BuyRelation ? $"{p.Name} ne livre pas ce savoir au clan"
                            : gifts == null ? "le clan n'a rien qui vaille ce savoir (les pierres n'y comptent pas)" : null;
                        options.Add(new MinorOption(MinorDeal.Learn, p.Name,
                            $"Apprendre de {p.Name}, en nature" + (gifts == null ? "" : $" : {string.Join(", ", gifts.Select(g => g.Label))}"),
                            gifts?.Select(g => g.Term).ToList(), refusal));
                    }
                }
                lines.Add(new MinorAbilityLine(f.Id, f.Name, known.Select(k => AbilityName(s, k)).ToList(), unknown, options));
            }
            return lines;
        }

        private static string AbilityName(GameSession s, string reference)
        {
            var (lineage, id) = FoundationRef.Parse(reference);
            return s.Context.Content.Fruitions.FirstOrDefault(f => f.Id == lineage)?.Abilities.FirstOrDefault(a => a.Id == id)?.Name ?? reference;
        }

        /// <summary>Does it: null when done; else what came of it (French).</summary>
        public static string Perform(GameSession s, string lineage, MinorOption o) =>
            o.Kind == MinorDeal.Deduce ? s.Minors.Deduce(lineage) : s.Minors.Buy(lineage, o.Power, o.Terms);
    }
}
