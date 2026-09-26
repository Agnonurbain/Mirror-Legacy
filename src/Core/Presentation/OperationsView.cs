using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Presentation
{
    /// <summary>A captured beast as the ritual shows it (its owner, « solitaire » for none).</summary>
    public sealed record BeastLine(string Id, string Strength, string Owner);

    /// <summary>A talisman Qi the mirror offers.</summary>
    public sealed record TalismanChoice(string Id, string Name, string Notes);

    /// <summary>The talismans awaiting the player's choice, and for whom.</summary>
    public sealed record OfferView(string Bearer, IReadOnlyList<TalismanChoice> Choices);

    /// <summary>A member the player may pick (bearer, hunter, keeper).</summary>
    public sealed record MemberChoice(string Id, string Name, string Rank);

    /// <summary>The mirror's ritual: its year, the hunt's window, the prayers, the beasts, the offer, and why it cannot be done now.</summary>
    public sealed record RitualView(int Year, bool HuntOpen, int Prayers, int PrayersNeeded, IReadOnlyList<BeastLine> Beasts,
        IReadOnlyList<MemberChoice> Bearers, OfferView Offer, string Refusal);

    /// <summary>A scouted beast the clan may hunt.</summary>
    public sealed record HuntTarget(string Id, string Species, string Strength, string Owner, string Place);

    /// <summary>What a plan would give before it is launched: the odds, the traces of a clean hunt, the costs, or why it cannot be.</summary>
    public sealed record HuntPreviewView(int Approach, int Capture, int Exposure, int Stones, int MirrorPower, string Refusal);

    /// <summary>What the mirror perceives of a power: a sign, never a figure (LORE.md D7).</summary>
    public sealed record PowerSign(string Power, string Sign);

    /// <summary>A member in the mirror's secret, and whether they swore to keep it.</summary>
    public sealed record KeeperLine(string Id, string Name, bool Sworn);

    /// <summary>
    /// The secret operations screen (L2c.5): the mirror's ritual, the hunt's planning with its odds before launching it,
    /// and the secret — the signs the mirror perceives of the powers (the ledger stays hidden, D7) and those who carry it.
    /// </summary>
    public static class OperationsView
    {
        public static RitualView Ritual(GameSession session)
        {
            var t = session.Talismans;
            var settings = session.Context.Content.Balance.Talismans;
            var beasts = session.Resources.Beasts
                .Select(b => new BeastLine(b.Id, Strength(b.Realm, b.Stage), b.OwnerFaction ?? "solitaire")).ToList();
            var bearers = session.Clan.LivingMembers
                .Where(m => m.TalismanQiId == null && SpiritualOrificeRules.CanCultivate(m))
                .Select(Choice).ToList();

            OfferView offer = null;
            if (t.PendingOffer != null)
            {
                var talismans = session.Context.Content.Talismans;
                offer = new OfferView(session.Clan.FindById(t.PendingOffer.BeneficiaryId)?.FullName,
                    t.PendingOffer.Choices.Select(id => talismans.FirstOrDefault(x => x.Id == id))
                        .Where(x => x != null).Select(x => new TalismanChoice(x.Id, x.Name, x.Notes)).ToList());
            }

            string refusal = session.Clock.Year != t.NextRitualYear ? $"le rituel a lieu en l'an {t.NextRitualYear}"
                : session.Resources.Prayers < settings.PrayersPerRitual ? "les prières ne suffisent pas"
                : beasts.Count == 0 ? "aucune bête captive à offrir"
                : t.PendingOffer != null ? "une offre attend déjà son choix"
                : null;

            return new RitualView(t.NextRitualYear, t.HuntWindowOpen, session.Resources.Prayers, settings.PrayersPerRitual,
                beasts, bearers, offer, refusal);
        }

        /// <summary>The beasts the clan has scouted, anywhere on the map.</summary>
        public static IReadOnlyList<HuntTarget> HuntTargets(GameSession session)
        {
            var content = session.Context.Content;
            return session.Bestiary.Beasts
                .Where(b => session.Knowledge.Knows(FactKind.Beast, b.Id))
                .Select(b => new HuntTarget(b.Id,
                    content.BeastSpecies.FirstOrDefault(sp => sp.Id == b.SpeciesId)?.Name ?? b.SpeciesId,
                    Strength(b.Realm, b.Stage), b.OwnerFaction ?? "solitaire",
                    content.Regions.FirstOrDefault(r => r.Id == b.RegionId)?.Name ?? b.RegionId))
                .ToList();
        }

        /// <summary>The members fit to hunt or to be seen elsewhere: the Qi Cultivation or beyond, not in retreat.</summary>
        public static IReadOnlyList<MemberChoice> HuntCandidates(GameSession session) =>
            session.Clan.LivingMembers
                .Where(m => m.Realm >= CultivationRealm.QiRefinement && m.Retreat == Retreat.None)
                .Select(Choice).ToList();

        /// <summary>A plan's odds and costs before launching it; the refusal says why it cannot be carried out now.</summary>
        public static HuntPreviewView HuntPreview(GameSession session, HuntPlan plan)
        {
            var content = session.Context.Content;
            var beast = session.Bestiary.Beasts.FirstOrDefault(b => b.Id == plan.TargetBeastId);
            int approach = beast == null ? 0 : HuntRules.ApproachChance(plan, beast, session.Factions, content);
            int capture = beast == null ? 0 : HuntRules.CaptureChance(plan, beast, session.Clan, content);
            var s = content.Balance.Hunt;
            return new HuntPreviewView(approach, capture, HuntRules.CleanExposure(plan, content),
                s.CoverStones[(int)plan.Cover], s.AidMirrorCost[(int)plan.Aid], session.Hunts.Validate(plan));
        }

        /// <summary>What the mirror perceives of each power: an investigator, an inquiry, questions, or calm.</summary>
        public static IReadOnlyList<PowerSign> Signs(GameSession session)
        {
            int inquiry = session.Context.Content.Balance.Plots.InvestigateThreshold;
            return session.Factions.Factions.Select(f =>
            {
                string sign = session.Secrets.Confrontation?.Faction == f.Name ? "un enquêteur est venu"
                    : session.Suspicion.OfClan(f.Name) >= inquiry ? "on enquête sur le clan"
                    : session.Suspicion.OfClan(f.Name) > 0 || session.Suspicion.MirrorClues(f.Name) > 0 ? "des questions circulent"
                    : "calme";
                return new PowerSign(f.Name, sign);
            }).ToList();
        }

        /// <summary>The members in the mirror's secret, and whether they swore to keep it.</summary>
        public static IReadOnlyList<KeeperLine> Keepers(GameSession session) =>
            session.Clan.LivingMembers.Where(m => m.KnowsMirrorSecret)
                .Select(m => new KeeperLine(m.ID, m.FullName, session.Oaths.SecrecyPartner(m) != null)).ToList();

        public static string RoleLabel(HuntRole role) => role switch
        {
            HuntRole.Striker => "Frappeur",
            HuntRole.Lure => "Appât",
            HuntRole.Lookout => "Guetteur",
            HuntRole.Cover => "Couverture",
            _ => role.ToString()
        };

        public static string TimingLabel(HuntTiming timing) => timing switch
        {
            HuntTiming.Dawn => "À l'aube",
            HuntTiming.Night => "De nuit",
            HuntTiming.Festival => "Un jour de fête",
            _ => timing.ToString()
        };

        public static string CoverLabel(CoverStory cover) => cover switch
        {
            CoverStory.None => "Aucune couverture",
            CoverStory.Trade => "Voyage de commerce",
            CoverStory.Pilgrimage => "Pèlerinage",
            CoverStory.Escort => "Escorte",
            _ => cover.ToString()
        };

        public static string AidLabel(MirrorAid aid) => aid switch
        {
            MirrorAid.None => "Sans l'aide du miroir",
            MirrorAid.Illusion => "Illusion du miroir",
            MirrorAid.MemoryTheft => "Vol de souvenirs",
            _ => aid.ToString()
        };

        private static MemberChoice Choice(CharacterData m) => new MemberChoice(m.ID, m.FullName, RankCatalog.DisplayName(m));

        private static string Strength(CultivationRealm realm, int stage) => $"{RankCatalog.RealmName(realm)}, stade {stage}";
    }
}
