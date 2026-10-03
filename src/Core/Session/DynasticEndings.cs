using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// The dynastic endings (LORE.md §11.9, B3b; the user's decisions of 2026-09-30): each ending of endings.json is told
    /// once, the moment all its conditions hold — at the turn of a year, when a member breaks through, when a position is
    /// taken — and kept in the Annals. None ends the game; a lost game reaches none. An ending that awaits a system is
    /// never reached meanwhile.
    /// </summary>
    public sealed class DynasticEndings
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly TreatySystem treaties;
        private readonly FactionManager factions;
        private readonly WarSystem wars;
        private readonly MirrorSystem mirror;
        private readonly ClanAnnals annals;
        private readonly VictoryConditionSystem victory;
        private readonly Clan.SectSystem sect;
        private readonly ClanAbsorption absorption;
        private readonly ImperialWay imperial;
        private readonly Dictionary<string, string> moves = new Dictionary<string, string>(); // « from>to » → who first made it
        private readonly Dictionary<string, int> streaks = new Dictionary<string, int>();     // ending id → years a hegemony held in a row

        public IReadOnlyDictionary<string, string> Moves => moves;
        public IReadOnlyDictionary<string, int> Streaks => streaks;

        public DynasticEndings(GameContext ctx, ClanManager clan, TreatySystem treaties, FactionManager factions, WarSystem wars,
            MirrorSystem mirror, ClanAnnals annals, VictoryConditionSystem victory, Clan.SectSystem sect, ClanAbsorption absorption,
            ImperialWay imperial = null)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.treaties = treaties;
            this.factions = factions;
            this.wars = wars;
            this.mirror = mirror;
            this.annals = annals;
            this.victory = victory;
            this.sect = sect;
            this.absorption = absorption;
            this.imperial = imperial;

            ctx.Events.OnYearStarted += year => { CountHegemonies(); Check(); };
            ctx.Events.OnBreakthroughSuccess += (member, realm) => Check();
            ctx.Events.OnPositionTaken += PositionTaken;
            ctx.Events.OnSectFounded += Check;
        }

        public bool IsReached(string endingId) =>
            annals.Entries.Any(e => e.Kind == AnnalKind.EndingReached && e.Ref == endingId);

        public void Restore(IReadOnlyDictionary<string, string> savedMoves, IReadOnlyDictionary<string, int> savedStreaks)
        {
            moves.Clear();
            foreach (var (move, who) in savedMoves ?? new Dictionary<string, string>()) moves[move] = who;
            streaks.Clear();
            foreach (var (ending, years) in savedStreaks ?? new Dictionary<string, int>()) streaks[ending] = years;
        }

        private void PositionTaken(CharacterData member, GoldenCoreState position, GoldenCoreState from)
        {
            moves.TryAdd($"{from}>{position}", member.FullName);
            Check();
        }

        /// <summary>At the turn of the year, each hegemony held grows by a year; one that slips starts over.</summary>
        private void CountHegemonies()
        {
            foreach (var ending in ctx.Content.Endings.Where(e => e.Awaits == null && !IsReached(e.Id)))
                foreach (var c in ending.Conditions.Where(c => c.Kind == EndingConditionKind.Hegemony))
                    streaks[ending.Id] = Dominates(c) ? streaks.GetValueOrDefault(ending.Id) + 1 : 0;
        }

        private void Check()
        {
            if (victory.IsOver) return;
            foreach (var ending in ctx.Content.Endings.Where(e => e.Awaits == null && !IsReached(e.Id)))
            {
                string subject = null;
                bool holds = ending.Conditions.All(c => Holds(ending, c, ref subject));
                if (!holds) continue;
                annals.RecordEnding(ending.Id, subject);
                ctx.Log.Info($"[Endings] {ending.Id} reached{(subject == null ? "" : " by " + subject)}.");
                ctx.Events.TriggerEndingReached(ending, subject);
            }
        }

        /// <summary>Whether a condition holds; one about a member names them as the ending's subject (the first such wins).</summary>
        private bool Holds(EndingDefinition ending, EndingCondition c, ref string subject)
        {
            string who = null;
            bool holds = c.Kind switch
            {
                EndingConditionKind.MemberRealm => Named(clan.LivingMembers.FirstOrDefault(m => m.Realm >= c.Realm), out who),
                EndingConditionKind.MemberPosition => Named(clan.LivingMembers.FirstOrDefault(m =>
                    m.Realm >= CultivationRealm.GoldenCore && m.GoldenCore == c.Position), out who),
                EndingConditionKind.PositionMove => c.From.Any(from => moves.TryGetValue($"{from}>{c.Position}", out who)),
                EndingConditionKind.TrueMonarchs => clan.LivingMembers.Count(m => m.Realm >= CultivationRealm.GoldenCore) >= c.Count,
                EndingConditionKind.GoldenLine => Named(GoldenLineHeir(c.Count), out who),
                EndingConditionKind.Vassals => VassalsHold(c),
                EndingConditionKind.Hegemony => streaks.GetValueOrDefault(ending.Id) >= c.Years,
                EndingConditionKind.Year => ctx.Clock.Year >= c.Year,
                EndingConditionKind.MirrorShards => mirror.RestoredFragments >= c.Count,
                EndingConditionKind.AnyOf => AnyHolds(ending, c, ref who),
                EndingConditionKind.SectFounded => sect.Founded,
                EndingConditionKind.AncestorReturned => Named(clan.LivingMembers.FirstOrDefault(m =>
                    m.RebornFrom != null && m.Realm >= CultivationRealm.GoldenCore), out who),
                EndingConditionKind.ImperialCore => imperial?.IsKingdom == true && Named(clan.LivingMembers.FirstOrDefault(m =>
                    m.ImperialCore && m.Realm >= CultivationRealm.GoldenCore), out who),
                _ => false // Awaits: a system still to come
            };
            if (holds && subject == null) subject = who;
            return holds;
        }

        private bool AnyHolds(EndingDefinition ending, EndingCondition c, ref string who)
        {
            foreach (var alternative in c.AnyOf)
                if (Holds(ending, alternative, ref who)) return true;
            return false;
        }

        private static bool Named(CharacterData member, out string who)
        {
            who = member?.FullName;
            return member != null;
        }

        /// <summary>
        /// A member at the Golden Core or above whose line counts <paramref name="generations"/> of them in a row, parent to
        /// child (the dead count: the line is the clan's records).
        /// </summary>
        private CharacterData GoldenLineHeir(int generations)
        {
            if (!clan.Registry.Records.Any(r => r.Realm >= CultivationRealm.GoldenCore)) return null; // most years: no True Monarch at all
            var records = clan.Registry.Records.ToDictionary(r => r.ID);
            var depth = new Dictionary<string, int>();
            int Depth(CharacterData m)
            {
                if (m == null || m.Realm < CultivationRealm.GoldenCore) return 0;
                if (depth.TryGetValue(m.ID, out int known)) return known;
                depth[m.ID] = 1; // guards a malformed family tree
                var father = m.FatherID != null && records.TryGetValue(m.FatherID, out var f) ? f : null;
                var mother = m.MotherID != null && records.TryGetValue(m.MotherID, out var mo) ? mo : null;
                return depth[m.ID] = 1 + System.Math.Max(Depth(father), Depth(mother));
            }
            return clan.Registry.Records.FirstOrDefault(r => Depth(r) >= generations);
        }

        private IEnumerable<FactionData> PowersOf(string regionId) =>
            factions.Factions.Where(f => Within(f.RegionId, regionId));

        private bool Within(string place, string regionId)
        {
            for (int hops = 0; place != null && hops < 16; hops++) // a region's parents, up to the state or sea
            {
                if (place == regionId) return true;
                place = ctx.Content.Regions.FirstOrDefault(r => r.Id == place)?.ParentId;
            }
            return false;
        }

        private HashSet<string> ClanVassals() =>
            treaties.All.Where(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain).Select(t => t.Faction).ToHashSet();

        private bool VassalsHold(EndingCondition c)
        {
            var powers = PowersOf(c.RegionId).Where(f => c.FactionKinds.Contains(f.Kind)).ToList();
            var vassals = ClanVassals();
            int absorbed = absorption.Absorbed.Count(a => c.FactionKinds.Contains(a.Kind) && Within(a.RegionId, c.RegionId)); // absorbed: bowed for good
            int bowed = powers.Count(p => vassals.Contains(p.Name)) + absorbed;
            return c.Count == 0 ? powers.Count + absorbed > 0 && bowed == powers.Count + absorbed : bowed >= c.Count;
        }

        /// <summary>The clan, with its vassals, outweighs in war every power of the region, and holds enough vassals.</summary>
        private bool Dominates(EndingCondition c)
        {
            double clanStrength = treaties.SuzerainStrength(); // a hegemon reigns through its vassals (2026-10-01)
            var settings = ctx.Content.Balance.Wars;
            var powers = PowersOf(c.RegionId).ToList();
            return powers.Count > 0 && ClanVassals().Count >= c.Vassals // a place without powers is no one's to dominate
                && powers.All(p => WarRules.Strength(p, settings) < clanStrength);
        }
    }
}
