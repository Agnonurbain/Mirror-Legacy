using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
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

    /// <summary>A member held by a power: since when, their ransom, and whether they could betray the mirror (L6a).</summary>
    public sealed record HeldLine(string Id, string Name, string Rank, string Captor, int Years, int Ransom, bool KnowsSecret);

    /// <summary>An agent of a power the clan holds: its price, and what was already done with it (L6a).</summary>
    public sealed record AgentLine(string Id, string Power, string Strength, int Price, bool Interrogated, bool Denounced);

    /// <summary>A spouse a power sent: whether the mirror sounded them, and — only then — whom they spy for.</summary>
    /// <summary>A member of the clan the unwed cultivator may be wed to.</summary>
    public sealed record PartnerLine(string Id, string Name, string Rank);

    /// <summary>An unwed cultivator: whom the clan may wed them to, and the search abroad (its cost, or why not).</summary>
    public sealed record UnwedLine(string Id, string Name, string Rank, IReadOnlyList<PartnerLine> Partners, int SeekCost, string SeekRefusal);

    public sealed record SpouseLine(string Id, string Name, string From, bool Sounded, string SpyFor, bool DoubleAgent);

    /// <summary>The captives on both sides.</summary>
    public sealed record CaptivesView(IReadOnlyList<HeldLine> Held, IReadOnlyList<AgentLine> Agents);

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
            var beasts = session.Resources.Beasts.Where(b => Mirror.TalismanRules.RankOf(b) != null) // a beast below the Qi Cultivation gives no talisman
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
                : beasts.Count == 0 ? "aucune bête captive de la Culture du Qi ou au-delà à offrir"
                : t.PendingOffer != null ? "une offre attend déjà son choix"
                : bearers.Count == 0 ? "aucun porteur éligible"
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

        /// <summary>The members free to hunt or to be seen elsewhere: fit, and not already on an operation this year.</summary>
        public static IReadOnlyList<MemberChoice> HuntCandidates(GameSession session) =>
            session.Clan.LivingMembers.Where(session.Hunts.IsFree).Select(Choice).ToList();

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
        /// <summary>Why the mirror cannot plant a false proof in a power's hands against another; null when it can.</summary>
        public static string FalseProofRefusal(GameSession session, string faction, string framed) =>
            session.Secrets.FalseProofRefusal(faction, framed);

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

        /// <summary>The living spouses the powers sent: their secret shows only once the mirror sounded them (2026-09-27).</summary>
        /// <summary>The unwed adult cultivators, the highest first, and whom each may wed in the clan (cultivators first).</summary>
        public static IReadOnlyList<UnwedLine> Unwed(GameSession session)
        {
            var living = session.Clan.LivingMembers.ToList();
            int cost = session.Context.Content.Balance.Lineage.SeekStones;
            return living
                .Where(m => m.OrificeKnown && SpiritualOrificeRules.CanCultivate(m) && m.SpouseID == null && m.CaptorFaction == null
                    && m.Age >= MarriageMatchmaker.MinMarriageAge)
                .OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).ThenBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new UnwedLine(m.ID, m.FullName, RankCatalog.DisplayName(m),
                    living.Where(p => session.Marriages.MarriageRefusal(m, p) == null)
                        .OrderByDescending(p => p.OrificeKnown && SpiritualOrificeRules.CanCultivate(p)).ThenBy(p => Math.Abs(p.Age - m.Age))
                        .Select(p => new PartnerLine(p.ID, p.FullName, RankCatalog.DisplayName(p))).ToList(),
                    cost, session.Marriages.SeekRefusal(m)))
                .ToList();
        }

        public static IReadOnlyList<SpouseLine> Spouses(GameSession session) =>
            session.Clan.LivingMembers.Where(m => m.FromFaction != null)
                .Select(m => new SpouseLine(m.ID, m.FullName, m.FromFaction, m.SpyUnmasked, m.SpyUnmasked ? m.SpyFor : null, m.DoubleAgent))
                .ToList();

        /// <summary>Our members held by the powers, and the powers' agents we hold (L6a).</summary>
        public static CaptivesView Captives(GameSession session)
        {
            var s = session.Context.Content.Balance.Schemes;
            var held = session.Captives.Held
                .Select(m => new HeldLine(m.ID, m.FullName, RankCatalog.DisplayName(m), m.CaptorFaction,
                    session.Clock.Year - (m.CapturedYear ?? session.Clock.Year), SchemeRules.Ransom(m.Realm, s), m.KnowsMirrorSecret))
                .ToList();
            var agents = session.Captives.Prisoners
                .Select(p => new AgentLine(p.Id, p.Faction, RankCatalog.RealmName(p.Realm), SchemeRules.Ransom(p.Realm, s), p.Interrogated, p.Denounced))
                .ToList();
            return new CaptivesView(held, agents);
        }

        private static MemberChoice Choice(CharacterData m) => new MemberChoice(m.ID, m.FullName, RankCatalog.DisplayName(m));

        private static string Strength(CultivationRealm realm, int stage) => $"{RankCatalog.RealmName(realm)}, stade {stage}";
    }
}
