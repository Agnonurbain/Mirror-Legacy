using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// The clan's Annals (LORE.md §11.9, B3a): the milestones that tell a game, each dated and kept once — the first
    /// member to reach each realm, the first to take each Golden Core position, each new generation. Unlike the
    /// chronicle, nothing is ever dropped.
    /// </summary>
    public sealed class ClanAnnals
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ClanKarmaSystem karma;
        private readonly List<AnnalEntry> entries = new List<AnnalEntry>();
        private readonly HashSet<CultivationRealm> realmsKnown = new HashSet<CultivationRealm>();
        private int generationKnown = 1;

        public IReadOnlyList<AnnalEntry> Entries => entries;

        public ClanAnnals(GameContext ctx, ClanManager clan, ClanKarmaSystem karma)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.karma = karma;

            ctx.Events.OnBreakthroughSuccess += (member, realm) => RealmReached(member, realm);
            ctx.Events.OnPositionTaken += (member, position, from) => PositionTaken(member, position);
            ctx.Events.OnYearStarted += year => GenerationBegins(); // after the karma, built before: it counts the generation
            ctx.Events.OnSectFounded += () => entries.Add(new AnnalEntry(AnnalKind.SectFounded, ctx.Clock.Year, 0, clan.GetPatriarch()?.FullName));
            ctx.Events.OnShardRecovered += shard =>
                entries.Add(new AnnalEntry(AnnalKind.ShardRecovered, ctx.Clock.Year, entries.Count(e => e.Kind == AnnalKind.ShardRecovered) + 1, null, shard.Id));
        }

        /// <summary>
        /// The saved Annals (none before 2.21), the generation reached, and the realms the clan's members already held
        /// (founders, or members of an older save): those are no milestone of this game.
        /// </summary>
        public void Restore(IEnumerable<AnnalEntry> saved, int generation, IEnumerable<CharacterData> members)
        {
            entries.Clear();
            entries.AddRange(saved ?? Enumerable.Empty<AnnalEntry>());
            generationKnown = System.Math.Max(1, generation);
            realmsKnown.Clear();
            foreach (var m in members ?? Enumerable.Empty<CharacterData>()) realmsKnown.Add(m.Realm);
            foreach (var e in entries.Where(e => e.Kind == AnnalKind.RealmReached)) realmsKnown.Add((CultivationRealm)e.Value);
        }

        /// <summary>A dynastic ending reached (B3b), and who reached it — none when the clan as a whole did.</summary>
        public void RecordEnding(string endingId, string subject) =>
            entries.Add(new AnnalEntry(AnnalKind.EndingReached, ctx.Clock.Year, 0, subject, endingId));

        private void RealmReached(CharacterData member, CultivationRealm realm)
        {
            if (!realmsKnown.Add(realm)) return;
            Record(AnnalKind.RealmReached, (int)realm, member.FullName);
        }

        private void PositionTaken(CharacterData member, GoldenCoreState position)
        {
            if (entries.Any(e => e.Kind == AnnalKind.PositionTaken && e.Value == (int)position)) return;
            Record(AnnalKind.PositionTaken, (int)position, member.FullName);
        }

        private void GenerationBegins()
        {
            if (karma.GenerationCount <= generationKnown) return;
            generationKnown = karma.GenerationCount;
            Record(AnnalKind.Generation, generationKnown, clan.GetPatriarch()?.FullName);
        }

        private void Record(AnnalKind kind, int value, string subject)
        {
            entries.Add(new AnnalEntry(kind, ctx.Clock.Year, value, subject));
            ctx.Log.Info($"[Annals] {kind} {value}: {subject}.");
        }
    }
}
