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
        int Absorptions = 0, int AbsorptionSteps = 0, string SpouseId = null);

    /// <summary>A power as the diplomacy screen shows it: its public allies and its suzerain — never what it hides (D7).</summary>
    public sealed record PowerLine(string Name, string Kind, string HighestRealm, int Relation, IReadOnlyList<TreatyLine> Treaties,
        IReadOnlyList<string> Allies = null, string Suzerain = null, string ClanWatch = null);

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
                    session.Treaties.With(f.Name).Select(t => new TreatyLine(t.Id, KindLabel(t.Kind), t.Secret, t.Sealed,
                        t.EndYear.HasValue ? t.EndYear.Value - session.Clock.Year : null, t.ClanIsSuzerain, t.Grip,
                        t.Absorptions, session.Context.Content.Balance.Politics.ClanAbsorptionSteps, t.SpouseId)).ToList(),
                    PublicAllies(session, f.Name), PublicSuzerain(session, f.Name),
                    World.ClanWatch.Sign(session.Suspicion.ClanDistrust(f.Name), session.Context.Content.Balance.ClanWatch)))
                .ToList();

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
