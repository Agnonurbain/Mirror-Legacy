using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Presentation
{
    /// <summary>A secret of a power the clan knows, and whether it has already served.</summary>
    public sealed record KnownSecretLine(string Id, string Holder, string Name, string Rank, bool Spent);

    /// <summary>A secret of the clan, and a sign of who may know it (never a figure).</summary>
    public sealed record ClanSecretLine(string Id, string Name, string Rank, string Sign);

    /// <summary>A probe's odds (in percent) before it is sent, or why it cannot be.</summary>
    public sealed record ProbePreview(int Chance, string Refusal);

    /// <summary>The secrets screen (2026-09-27): what the clan knows of others, its own secrets, a probe's odds.</summary>
    public static class SecretsView
    {
        public static IReadOnlyList<KnownSecretLine> Known(GameSession session) =>
            session.SecretBook.KnownBy(SecretBook.ClanHolder)
                .OrderByDescending(s => s.Rank).ThenBy(s => s.Holder, System.StringComparer.Ordinal)
                .Select(s => new KnownSecretLine(s.Id, s.Holder, KindName(session, s), RankLabel(s.Rank), session.Dealings.IsSpent(s.Id)))
                .ToList();

        public static IReadOnlyList<ClanSecretLine> Clans(GameSession session)
        {
            var powers = session.Factions.Factions.Select(f => f.Name).ToList();
            return session.SecretBook.Of(SecretBook.ClanHolder)
                .Select(s => new ClanSecretLine(s.Id, KindName(session, s), RankLabel(s.Rank),
                    powers.Any(p => session.SecretBook.Knows(p, s.Id)) ? "percé par au moins une puissance"
                    : powers.Any(p => session.SecretBook.Progress(p, s.Id) > 0) ? "des soupçons rôdent"
                    : "personne ne semble savoir"))
                .ToList();
        }

        public static ProbePreview Preview(GameSession session, ProbePlan plan)
        {
            string refusal = session.Probes.RefusalOf(plan);
            return refusal != null ? new ProbePreview(0, refusal) : new ProbePreview((int)System.Math.Round(session.Probes.ChanceAgainst(plan) * 100), null);
        }

        public static string RankLabel(int rank) => rank switch
        {
            1 => "mineur",
            2 => "sérieux",
            3 => "grave",
            4 => "vital",
            _ => "suprême"
        };

        public static string ApproachLabel(ProbeApproach approach) => approach switch
        {
            ProbeApproach.Provocation => "provocation",
            ProbeApproach.Infiltration => "infiltration",
            ProbeApproach.Bribery => "corruption",
            ProbeApproach.RecordTheft => "vol d'archives",
            ProbeApproach.MirrorSight => "regard du miroir",
            _ => approach.ToString()
        };

        private static string KindName(GameSession session, Secret s) =>
            session.Context.Content.SecretKinds.FirstOrDefault(k => k.Id == s.KindId)?.Name ?? s.KindId;
    }
}
