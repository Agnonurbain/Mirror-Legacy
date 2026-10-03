using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>An artifact of the clan: its line, and the member it would best serve (null: none).</summary>
    public sealed record ArtifactRow(string Id, string Label, string EntrustTo, string EntrustLabel, bool Owned);

    /// <summary>A work the forge may take: a form, the highest rank its best smith can make, the smith, and why not (null: it can).</summary>
    public sealed record ForgeOffer(string FormId, CultivationRealm Rank, string SmithId, string Label, string Refusal);

    public enum ArtifactDeal { Borrow, Commission, Steal }

    /// <summary>A deal with a power: a loan, a commission, a theft — the team for a theft.</summary>
    public sealed record PowerOffer(ArtifactDeal Kind, string Power, string Label, IReadOnlyList<string> TeamIds,
        CultivationRealm Rank = CultivationRealm.Embryonic, IReadOnlyList<Diplomacy.AccordTerm> Terms = null, string Refusal = null);

    /// <summary>A peak Foundation and a Spiritual Treasure it may bind itself to.</summary>
    public sealed record BindOffer(string MemberId, string ArtifactId, string Label);

    /// <summary>The armoury as the clan sees it (L4f, 2026-10-03): its artifacts and what may be done with them.</summary>
    public static class ArtifactView
    {
        public static string ClassLabel(ArtifactClass cls) => cls switch
        {
            ArtifactClass.DharmaArtifact => "Artefact de Dharma",
            ArtifactClass.SpiritualArtifact => "Artefact Spirituel",
            ArtifactClass.SpiritualTreasure => "Trésor Spirituel",
            _ => "artefact"
        };

        public static string EffectLabel(ArtifactEffect effect) => effect switch
        {
            ArtifactEffect.Combat => "arme",
            ArtifactEffect.Protection => "garde",
            ArtifactEffect.Cultivation => "aide à cultiver",
            _ => ""
        };

        private static string RankLabel(CultivationRealm rank) => RankCatalog.RealmName(rank);

        public static string Describe(ArtifactInstance a) =>
            $"{a.Name} — {ClassLabel(a.Class)}, {RankLabel(a.Rank)}, {EffectLabel(a.Effect)}"
            + (a.LentBy != null ? $" (prêté par {a.LentBy} jusqu'en l'an {a.DueYear})" : "");

        public static IReadOnlyList<ArtifactRow> Rows(GameSession s)
        {
            var rows = new List<ArtifactRow>();
            foreach (var bearer in s.Clan.LivingMembers.Where(m => m.Artifact != null))
                rows.Add(new ArtifactRow(bearer.Artifact.Id, $"{Describe(bearer.Artifact)} — porté par {bearer.FullName}"
                    + (bearer.TreasureBound ? ", lié à vie" : ""), null, null, bearer.Artifact.LentBy == null));
            foreach (var a in s.Artifacts.Armoury)
            {
                var to = s.Clan.LivingMembers.Where(m => m.CaptorFaction == null && !m.TreasureBound && m.Realm >= a.Rank && (m.Artifact == null || m.Artifact.Rank < a.Rank))
                    .OrderByDescending(m => m.Artifact == null ? 1 : 0).ThenByDescending(m => Mirror.HuntRules.Power(m.Realm, m.RealmStage)).FirstOrDefault();
                rows.Add(new ArtifactRow(a.Id, $"{Describe(a)} — dans l'armurerie", to?.ID, to == null ? null : $"Confier à {to.FullName}", a.LentBy == null));
            }
            return rows;
        }

        /// <summary>For each form, the highest rank the clan's best free smith can forge, and why not when it cannot.</summary>
        public static IReadOnlyList<ForgeOffer> ForgeOffers(GameSession s)
        {
            var forging = s.Context.Content.Balance.Artifacts.Forging;
            var smith = s.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Retreat == Retreat.None && m.Realm >= CultivationRealm.QiRefinement)
                .OrderBy(m => m.LastOperationYear == s.Clock.Year ? 1 : 0).ThenByDescending(m => m.Realm).FirstOrDefault();
            if (smith == null) return new List<ForgeOffer>();
            var rank = forging.Keys.Where(r => r <= smith.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if (rank < CultivationRealm.QiRefinement) return new List<ForgeOffer>();
            var cost = forging[rank];
            string refusal = s.Forge.MakeRefusal(rank, smith.ID);
            return s.Context.Content.ArtifactForms.Select(f => new ForgeOffer(f.Id, rank, smith.ID,
                $"Forger : {f.Name} ({ClassLabel(ArtifactArmoury.ClassOf(rank))}, {RankLabel(rank)}, {EffectLabel(f.Effect)}) par {smith.FullName} — {cost.Ores} minerais, {cost.Stones} pierres",
                refusal)).ToList();
        }

        /// <summary>Loans from friendly powers, commissions to the sects, gates and kingdoms that work for the clan, thefts from hostile ones.</summary>
        public static IReadOnlyList<PowerOffer> PowerOffers(GameSession s)
        {
            var t = s.Context.Content.Balance.Artifacts.Trade;
            var offers = new List<PowerOffer>();
            var team = s.Shards.BestTeam(CultivationRealm.QiRefinement, s.Context.Content.Balance.Shards.ExpeditionMaxTeam)
                .Where(id => id != s.Clan.PatriarchID).ToList();
            foreach (var p in s.Factions.Factions.OrderBy(f => f.Name, System.StringComparer.Ordinal))
            {
                if (p.RelationWithPlayer >= t.LoanRelation && p.HighestRealm >= CultivationRealm.QiRefinement)
                    offers.Add(new PowerOffer(ArtifactDeal.Borrow, p.Name, $"Emprunter un artefact à {p.Name} (une faveur due)", new List<string>()));
                if ((p.Kind == FactionKind.Sect || p.Kind == FactionKind.Gate || p.Kind == FactionKind.State) && p.RelationWithPlayer >= t.CommissionRelation
                    && p.HighestRealm >= CultivationRealm.QiRefinement)
                {
                    var rank = p.HighestRealm > CultivationRealm.PurpleMansion ? CultivationRealm.PurpleMansion : p.HighestRealm;
                    int price = s.ArtifactTrade.CommissionPrice(rank);
                    if (!ArtifactTrade.IsPrecious(rank, ArtifactArmoury.ClassOf(rank)))
                        offers.Add(new PowerOffer(ArtifactDeal.Commission, p.Name, $"Commander à {p.Name} un artefact ({RankLabel(rank)}) — {price} pierres", new List<string>(), rank));
                    else
                    {
                        var bundle = AccordView.Bundle(AccordView.Offerings(s.Accords, s.Clan, s.Techniques, s.Resources, s.SecretBook, s.Artifacts, p.Name, price, precious: true), price);
                        offers.Add(new PowerOffer(ArtifactDeal.Commission, p.Name,
                            $"Commander à {p.Name} un artefact ({RankLabel(rank)}) — en nature" + (bundle == null ? "" : $" : {string.Join(", ", bundle.Select(b => b.Label))}"),
                            new List<string>(), rank, bundle?.Select(b => b.Term).ToList(),
                            bundle == null ? "le clan n'a rien qui vaille un artefact du Manoir Pourpre (les pierres n'y comptent pas)" : null));
                    }
                }
                if (p.RelationWithPlayer < 0 && team.Count > 0)
                    offers.Add(new PowerOffer(ArtifactDeal.Steal, p.Name, $"Voler un artefact à {p.Name}", team));
            }
            return offers;
        }

        /// <summary>The peak Foundations and the Spiritual Treasures of the armoury they may bind themselves to.</summary>
        public static IReadOnlyList<BindOffer> BindOffers(GameSession s) =>
            s.Clan.LivingMembers.Where(m => m.CaptorFaction == null && !m.ProgressionSealed && m.Realm == CultivationRealm.Foundation
                    && m.RealmStage >= PowerLadder.StageCount(CultivationRealm.Foundation))
                .SelectMany(m => s.Artifacts.Armoury.Where(a => a.Class == ArtifactClass.SpiritualTreasure)
                    .Select(a => new BindOffer(m.ID, a.Id, $"Lier {m.FullName} à {a.Name} : la puissance d'un Manoir Pourpre, et plus jamais un pas au-delà")))
                .ToList();
    }
}
