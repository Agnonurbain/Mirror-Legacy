using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A treaty as the diplomacy screen shows it (the years left: null for an open treaty).</summary>
    public sealed record TreatyLine(string Id, string Kind, bool Secret, bool Sealed, int? YearsLeft, bool ClanIsSuzerain, int Grip,
        int Absorptions = 0, int AbsorptionSteps = 0, string SpouseId = null)
    {
        public bool Client { get; init; } // the clan, a sect's client (audit §4.1): breaking free means war
    }

    /// <summary>A power as the diplomacy screen shows it: its public allies and its suzerain — never what it hides (D7).</summary>
    public sealed record PowerLine(string Name, string Kind, string HighestRealm, int Relation, IReadOnlyList<TreatyLine> Treaties,
        IReadOnlyList<string> Allies = null, string Suzerain = null, string ClanWatch = null);

    /// <summary>A temple a dying cultivator may convert at (R13-R14): its school, and why not now (null: it may).</summary>
    public sealed record TempleChoice(string Name, string School, string Refusal);

    /// <summary>A dying cultivator of the clan, the years it has left, and the temples it may convert at.</summary>
    public sealed record ConversionLine(string MemberId, string Name, string Rank, int YearsLeft, IReadOnlyList<TempleChoice> Temples);

    /// <summary>A monk of the clan, its vow (null: none), and the vows it may take with what each forbids.</summary>
    public sealed record MonkLine(string MemberId, string Name, string Rank, string Vow, IReadOnlyList<VowChoice> Choices);

    /// <summary>A vow a monk may take: its label and what it forbids.</summary>
    public sealed record VowChoice(BuddhistVow Vow, string Label, string Forbids);

    /// <summary>A member who may debate, with its odds against the temple in question.</summary>
    public sealed record DebaterLine(string MemberId, string Name, string Rank, int Odds);

    /// <summary>A temple the clan may debate: its debaters and their odds, or why not now.</summary>
    public sealed record DebateTarget(string Temple, string School, string Refusal, IReadOnlyList<DebaterLine> Debaters);

    /// <summary>A member who may bind itself to a Profundity (R15), and the great partners it may borrow from.</summary>
    public sealed record AttendantLine(string MemberId, string Name, string Rank, IReadOnlyList<PatronChoice> Lenders);

    /// <summary>A great partner bound to the clan by a pact, to borrow from.</summary>
    public sealed record PatronChoice(string Id, string Name);

    /// <summary>A Divine Attendant who owns a Profundity: the button's text and why it cannot condense its core now.</summary>
    public sealed record DivineCoreLine(string MemberId, string Label, string Refusal);

    /// <summary>A great partner: what it gives, its tribute, why it would not hear the clan now, and — once bound — a sign of its favour.</summary>
    public sealed record PatronLine(string Id, string Name, string Kind, string Boon, int Tribute, string Refusal, bool Bound, string Favor,
        string EndWarning = null);

    /// <summary>A war, as the screen tells it; for the clan's, its enemy and the tribute peace would cost.</summary>
    public sealed record WarLine(string Description, bool ClansWar, string Enemy, int PeaceCost);

    /// <summary>Powers banded against the clan, for the years left.</summary>
    public sealed record CoalitionLine(IReadOnlyList<string> Members, int YearsLeft);

    /// <summary>A power's blackmail awaiting the clan's answer.</summary>
    /// <summary>A demand awaiting an answer: what it is for, and what a refusal brings.</summary>
    public sealed record DemandLine(string Faction, int Stones, string Reason, string Refusal);

    /// <summary>An ally of the clan attacked, calling it to arms, and what answering costs.</summary>
    public sealed record CallLine(string Ally, string Attacker, int Cost);

    /// <summary>The diplomacy screen (G6; D7): the powers, the treaties that bind them to the clan, why a proposal fails.</summary>
    public static class DiplomacyView
    {
        /// <summary>Every power, the friendliest first.</summary>
        public static IReadOnlyList<PowerLine> Powers(GameSession session) =>
            session.Factions.Factions
                .OrderByDescending(f => f.RelationWithPlayer).ThenBy(f => f.Name, StringComparer.Ordinal)
                .Select(f => new PowerLine(f.Name, WorldMapView.KindLabel(f.Kind), RankCatalog.RealmName(f.HighestRealm), f.RelationWithPlayer,
                    session.Treaties.With(f.Name).Select(t => new TreatyLine(t.Id, t.Client ? ClientLabel(session) : KindLabel(t.Kind), t.Secret, t.Sealed,
                        t.EndYear.HasValue ? t.EndYear.Value - session.Clock.Year : null, t.ClanIsSuzerain, t.Grip,
                        t.Absorptions, session.Context.Content.Balance.Politics.ClanAbsorptionSteps, t.SpouseId) { Client = t.Client }).ToList(),
                    PublicAllies(session, f.Name), PublicSuzerain(session, f.Name),
                    World.ClanWatch.Sign(session.Suspicion.ClanDistrust(f.Name), session.Context.Content.Balance.ClanWatch)))
                .ToList();

        /// <summary>
        /// A power's elders as the world knows them (the living world, 2026-10-01): name, realm, a Grand Perfection, the
        /// Realization held — never its odds of the Golden Core, which stay its own.
        /// </summary>
        public static IReadOnlyList<string> Elders(GameSession session, string power) =>
            session.Factions.GetFactionByName(power)?.Elders.OrderByDescending(e => e.Realm).ThenBy(e => e.Name, StringComparer.Ordinal)
                .Select(e => $"{e.Name} — {RankCatalog.RealmName(e.Realm)}"
                    + (e.Realm == CultivationRealm.PurpleMansion && e.Perfected ? ", à la Grande Perfection" : "")
                    + (e.FruitionId != null ? $", tient la Réalisation de la lignée {session.Context.Content.Fruitions.FirstOrDefault(f => f.Id == e.FruitionId)?.Name ?? e.FruitionId}" : ""))
                .ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();

        public static IReadOnlyList<DemandLine> Demands(GameSession session) =>
            session.Intrigues.Demands.Select(d => d.Kind == DemandKind.Protection
                ? new DemandLine(d.Faction, d.Stones, "pour sa protection", "elle prendra par la guerre")
                : new DemandLine(d.Faction, d.Stones, "pour son silence", "elle répand ses preuves")).ToList();

        public static CoalitionLine Coalition(GameSession session) =>
            session.Politics.Coalition is { } c ? new CoalitionLine(c.Members.ToList(), c.YearsLeft) : null;

        public static CallLine Call(GameSession session) =>
            session.Politics.PendingCall is { } call
                ? new CallLine(call.Ally, call.Attacker, session.Context.Content.Balance.Politics.CallStonesCost)
                : null;

        private static IReadOnlyList<string> PublicAllies(GameSession session, string power) =>
            session.Politics.Bonds.Where(b => b.Kind == BondKind.Alliance && !b.Secret && (b.A == power || b.B == power))
                .Select(b => b.A == power ? b.B : b.A).OrderBy(n => n, StringComparer.Ordinal).ToList();

        private static string PublicSuzerain(GameSession session, string power) =>
            session.Politics.Bonds.FirstOrDefault(b => b.Kind == BondKind.Vassalage && !b.Secret && b.B == power)?.A;

        /// <summary>Why a power would refuse this treaty now; null when it would accept.</summary>
        public static string ProposalRefusal(GameSession session, string faction, TreatyKind kind, bool clanAsSuzerain, bool sealedByOath) =>
            session.Treaties.Refusal(session.Factions.GetFactionByName(faction), kind, sealedByOath, clanAsSuzerain);

        /// <summary>The wars under way: the clan's first, with the price of peace; then the powers' (each side with its allies).</summary>
        public static IReadOnlyList<WarLine> Wars(GameSession session)
        {
            int peace = (int)(session.Resources.SpiritStones * session.Context.Content.Balance.Wars.PeaceTributeShare);
            return session.Wars.ClanWars
                .Select(w => new WarLine($"Le clan en guerre contre {w.Enemy} depuis l'an {w.StartYear}", true, w.Enemy, peace))
                .Concat(session.Wars.Wars.Select(w => new WarLine(
                    $"{string.Join(", ", w.SideA)} contre {string.Join(", ", w.SideB)} depuis l'an {w.StartYear}", false, null, 0)))
                .ToList();
        }

        /// <summary>The clan's dying cultivators who may convert, each with the world's temples (R13-R14; none without a temple).</summary>
        public static IReadOnlyList<ConversionLine> Conversions(GameSession session)
        {
            var temples = session.Factions.Factions.Where(f => f.Path == CultivationPath.Buddhist && f.School != null).ToList();
            if (temples.Count == 0) return Array.Empty<ConversionLine>();
            return session.Clan.LivingMembers.Where(session.Buddhism.MayConvert)
                .OrderBy(m => PowerLadder.LifespanLimit(m) - m.Age).ThenBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new ConversionLine(m.ID, m.FullName, RankCatalog.DisplayName(m), Math.Max(0, PowerLadder.LifespanLimit(m) - m.Age),
                    temples.Select(t => new TempleChoice(t.Name, SchoolLabel(t.School.Value), session.Buddhism.ConversionRefusal(m, t.Name))).ToList()))
                .ToList();
        }

        /// <summary>« la Transformation », « la Dissimulation », « le Sacrifice ».</summary>
        public static string ShamanMethodLabel(ShamanMethod method) => method switch
        {
            ShamanMethod.Transformation => "la Transformation",
            ShamanMethod.Concealment => "la Dissimulation",
            ShamanMethod.Sacrifice => "le Sacrifice",
            _ => "aucune méthode"
        };

        /// <summary>The clan's members of the Grand Shaman's foundation who may pass to the shamanic Dao (R17).</summary>
        public static IReadOnlyList<MemberChoice> ShamanCandidates(GameSession session) =>
            session.Clan.LivingMembers.Where(m => session.Shamans.ConversionRefusal(m) == null)
                .OrderBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new MemberChoice(m.ID, m.FullName, RankCatalog.DisplayName(m))).ToList();

        /// <summary>The clan's shamans and their rite toward a shamanic Golden Core (R17).</summary>
        public static IReadOnlyList<DivineCoreLine> ShamanCores(GameSession session)
        {
            var s = session.Context.Content.Balance.Shamanism;
            return session.Clan.LivingMembers.Where(m => m.Path == CultivationPath.Shamanic && m.Realm == CultivationRealm.PurpleMansion)
                .Select(m => new DivineCoreLine(m.ID, $"{m.FullName} ({RankCatalog.DisplayName(m)}) : rite de sang du Noyau d'Or chamanique ({s.CoreBlood} essences, {(int)Math.Round(s.CoreChance * 100)} %)",
                    session.Shamans.CoreRefusal(m))).ToList();
        }

        /// <summary>The clan's bound beasts at their apogee, and the offering that feeds the awakening of their core (L7).</summary>
        public static IReadOnlyList<DivineCoreLine> DemonicCores(GameSession session)
        {
            var s = session.Context.Content.Balance.DemonicDao;
            return session.Clan.LivingMembers.Where(m => DemonicDaoSystem.IsDemonic(m) && m.Realm == CultivationRealm.PurpleMansion
                    && m.RealmStage >= PowerLadder.StageCount(CultivationRealm.PurpleMansion))
                .OrderBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new DivineCoreLine(m.ID, $"{m.FullName} ({RankCatalog.DisplayName(m)}) : nourrir l'éveil de son noyau ({s.OfferingStones} pierres, {(int)Math.Round((s.OfferingCoreChance + (DemonicDaoSystem.IsDragon(m) ? s.DragonCoreBonus : 0)) * 100)} % ; l'échec blesse son Dao)",
                    session.DemonicDao.CoreRefusal(m))).ToList();
        }

        /// <summary>The clan's cultivators who may become Divine Attendants, and the great partners bound by a pact (R15).</summary>
        public static IReadOnlyList<AttendantLine> Attendants(GameSession session)
        {
            var lenders = session.Patrons.Pacts.Select(p => session.Context.Content.Patrons.FirstOrDefault(d => d.Id == p.PatronId))
                .Where(d => d != null).Select(d => new PatronChoice(d.Id, d.Name)).ToList();
            return session.Clan.LivingMembers.Where(m => session.Divine.AttendantRefusal(m) == null)
                .OrderByDescending(m => (int)m.Realm).ThenBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new AttendantLine(m.ID, m.FullName, RankCatalog.DisplayName(m), lenders)).ToList();
        }

        /// <summary>The owners of a Profundity and their Divine Core (R15).</summary>
        public static IReadOnlyList<DivineCoreLine> DivineCores(GameSession session)
        {
            var s = session.Context.Content.Balance.DivineDao;
            return session.Clan.LivingMembers.Where(m => m.Path == CultivationPath.Divine && m.SubPath == CultivationSubPath.OwnedProfundity)
                .Select(m => new DivineCoreLine(m.ID, $"{m.FullName} ({RankCatalog.DisplayName(m)}) : condenser un Noyau Divin ({s.CorePrestige} prestige, {(int)Math.Round(s.CoreChance * 100)} %)",
                    session.Divine.CoreRefusal(m))).ToList();
        }

        /// <summary>« le vœu de silence ».</summary>
        public static string VowLabel(BuddhistVow vow) => vow switch
        {
            BuddhistVow.NonViolence => "le vœu de non-violence",
            BuddhistVow.Silence => "le vœu de silence",
            BuddhistVow.Renunciation => "le vœu de dépouillement",
            _ => "aucun vœu"
        };

        /// <summary>The clan's monks and the vows open to them (R13-14b).</summary>
        public static IReadOnlyList<MonkLine> Monks(GameSession session)
        {
            var vows = new[] { BuddhistVow.NonViolence, BuddhistVow.Silence, BuddhistVow.Renunciation };
            return session.Clan.LivingMembers.Where(m => m.Path == CultivationPath.Buddhist && m.CaptorFaction == null)
                .OrderBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new MonkLine(m.ID, m.FullName, RankCatalog.DisplayName(m), m.Vow == BuddhistVow.None ? null : VowLabel(m.Vow),
                    m.Vow != BuddhistVow.None ? Array.Empty<VowChoice>()
                        : vows.Select(v => new VowChoice(v, VowLabel(v), Forbids(session, v))).ToList()))
                .ToList();
        }

        private static string Forbids(GameSession session, BuddhistVow vow)
        {
            var tasks = session.Buddhism.ForbiddenBy(vow).Select(ClanDomainView.TaskLabel).ToList();
            if (vow == BuddhistVow.NonViolence) tasks.Add("tout combat");
            return $"interdit : {string.Join(", ", tasks)} ; la cultivation plus rapide et +{session.Context.Content.Balance.Buddhism.VowDebateBonus} aux débats tant qu'il est tenu";
        }

        /// <summary>The members who may debate a temple, with their odds, the best first.</summary>
        public static IReadOnlyList<DebaterLine> Debaters(GameSession session, string temple) =>
            session.Clan.LivingMembers.Where(m => session.Debates.DebaterRefusal(m) == null)
                .Select(m => new DebaterLine(m.ID, m.FullName, RankCatalog.DisplayName(m), session.Debates.Odds(m, temple)))
                .OrderByDescending(d => d.Odds).ThenBy(d => d.Name, StringComparer.Ordinal).ToList();

        /// <summary>The temples the clan may challenge, each with its debaters, or why not now.</summary>
        public static IReadOnlyList<DebateTarget> DebateTargets(GameSession session) =>
            session.Debates.Temples.OrderBy(t => t.Name, StringComparer.Ordinal).Select(t =>
            {
                var debaters = Debaters(session, t.Name);
                string refusal = debaters.Count == 0 ? "le clan n'a personne pour débattre"
                    : session.Debates.ChallengeRefusal(session.Clan.FindById(debaters[0].MemberId), t.Name);
                return new DebateTarget(t.Name, SchoolLabel(t.School.Value), refusal, debaters);
            }).ToList();

        /// <summary>« école ancienne » or « école moderne ».</summary>
        public static string SchoolLabel(CultivationSubPath school) =>
            school == CultivationSubPath.ModernBuddhism ? "école moderne : plus rapide, l'âme gardée par le temple" : "école ancienne : lente et stable";

        /// <summary>The great partners of the very high level (2026-09-27).</summary>
        public static IReadOnlyList<PatronLine> Patrons(GameSession session)
        {
            var s = session.Context.Content.Balance.Patrons;
            return session.Context.Content.Patrons.Select(p =>
            {
                var pact = session.Patrons.Pacts.FirstOrDefault(x => x.PatronId == p.Id);
                string favor = pact == null ? null
                    : pact.Favor < s.SafeEndFavor ? "sa colère gronde" : pact.Favor >= s.MaxFavor ? "elle vous tient en haute estime" : "elle vous est favorable";
                return new PatronLine(p.Id, p.Name, p.Kind == PatronKind.Beast ? "grande bête" : "figure solitaire",
                    p.Boon switch { PatronBoon.Protection => "protection", PatronBoon.Insight => "savoir", _ => "regard" },
                    p.Tribute, pact == null ? session.Patrons.Refusal(p.Id) : null, pact != null, favor,
                    pact != null && pact.Favor < s.SafeEndFavor ? "partir maintenant attirerait sa colère : un membre en mourrait" : null);
            }).ToList();
        }

        /// <summary>The members the clan may offer in marriage.</summary>
        public static IReadOnlyList<MemberChoice> MarriageCandidates(GameSession session) =>
            session.Clan.LivingMembers.Where(Clan.MarriageMatchmaker.IsEligible)
                .Select(m => new MemberChoice(m.ID, m.FullName, RankCatalog.DisplayName(m))).ToList();

        /// <summary>What the clientage costs, as the screen says it (audit §4.1, §5.1).</summary>
        public static string ClientLabel(GameSession s) =>
            $"clientèle : le clan lui doit {(int)System.Math.Round(s.Context.Content.Balance.Clientage.TributeShare * 100)} % de ses pierres, "
            + "ses enfants les plus doués et des levées pour la frontière";

        public static string KindLabel(TreatyKind kind) => kind switch
        {
            TreatyKind.NonAggression => "non-agression",
            TreatyKind.Trade => "commerce",
            TreatyKind.Defence => "défense mutuelle",
            TreatyKind.Vassalage => "vassalité",
            TreatyKind.Marriage => "alliance matrimoniale",
            _ => kind.ToString()
        };
    }
}
