using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A member in the family tree: their generation and column, alive or not, what became of them, their kin.</summary>
    public sealed record KinNode(string Id, string Name, int Generation, int Column, bool Alive, bool IsMale, string Status,
        string FatherId, string MotherId, string SpouseId, bool IsPatriarch);

    /// <summary>
    /// The clan's family tree (G6): every member ever recorded. The founders stand at generation 0, a child one below its
    /// parents, a spouse married in beside the member; within a generation the children follow their parents' order
    /// and each couple stands side by side.
    /// </summary>
    public static class GenealogyView
    {
        public static IReadOnlyList<KinNode> Tree(GameSession session)
        {
            var records = session.Clan.Registry.Records.ToList();
            var byId = records.ToDictionary(r => r.ID);
            var index = records.Select((r, i) => (r.ID, i)).ToDictionary(p => p.ID, p => p.i);
            var generations = new Dictionary<string, int>();
            foreach (var r in records) GenerationOf(r, byId, generations);

            var columns = new Dictionary<string, int>();
            foreach (var generation in records.GroupBy(r => generations[r.ID]).OrderBy(g => g.Key))
                PlaceGeneration(generation.ToList(), byId, generations, columns, index);

            return records.Select(r => new KinNode(r.ID, r.FullName, generations[r.ID], columns[r.ID], r.IsAlive, r.IsMale, Status(r),
                    r.FatherID, r.MotherID, r.SpouseID, r.ID == session.Clan.PatriarchID))
                .OrderBy(n => n.Generation).ThenBy(n => n.Column)
                .ToList();
        }

        /// <summary>One below the parents; a spouse married in (no parents here) beside the member; else a founder.</summary>
        private static int GenerationOf(CharacterData r, IReadOnlyDictionary<string, CharacterData> byId, Dictionary<string, int> memo)
        {
            if (memo.TryGetValue(r.ID, out var known)) return known;
            memo[r.ID] = 0; // guards a malformed loop of kin
            var parents = Parents(r, byId).ToList();
            int generation = parents.Count > 0 ? parents.Max(p => GenerationOf(p, byId, memo)) + 1
                : SpouseOf(r, byId) is { } spouse && Parents(spouse, byId).Any() ? GenerationOf(spouse, byId, memo)
                : 0;
            return memo[r.ID] = generation;
        }

        /// <summary>The children in their parents' order, each followed by the spouse married in.</summary>
        private static void PlaceGeneration(List<CharacterData> generation, IReadOnlyDictionary<string, CharacterData> byId,
            IReadOnlyDictionary<string, int> generations, Dictionary<string, int> columns, IReadOnlyDictionary<string, int> index)
        {
            bool FollowsSpouse(CharacterData r) =>
                !Parents(r, byId).Any() && SpouseOf(r, byId) is { } s && generations[s.ID] == generations[r.ID] && Parents(s, byId).Any();

            var leaders = generation.Where(r => !FollowsSpouse(r))
                .OrderBy(r => Parents(r, byId).Select(p => columns.TryGetValue(p.ID, out var c) ? c : int.MaxValue).DefaultIfEmpty(-1).Min())
                .ThenBy(r => index[r.ID]);
            int column = 0;
            foreach (var r in leaders)
            {
                if (columns.ContainsKey(r.ID)) continue;
                columns[r.ID] = column++;
                if (SpouseOf(r, byId) is { } spouse && generations[spouse.ID] == generations[r.ID] && !columns.ContainsKey(spouse.ID))
                    columns[spouse.ID] = column++;
            }
            foreach (var r in generation.Where(r => !columns.ContainsKey(r.ID))) columns[r.ID] = column++; // a spouse whose partner stands elsewhere
        }

        private static IEnumerable<CharacterData> Parents(CharacterData r, IReadOnlyDictionary<string, CharacterData> byId) =>
            new[] { r.FatherID, r.MotherID }.Where(id => id != null && byId.ContainsKey(id)).Select(id => byId[id]);

        private static CharacterData SpouseOf(CharacterData r, IReadOnlyDictionary<string, CharacterData> byId) =>
            r.SpouseID != null && byId.TryGetValue(r.SpouseID, out var spouse) ? spouse : null;

        private static string Status(CharacterData r) =>
            r.Departed ? $"{RankCatalog.DisplayName(r)} — retourné(e) auprès des siens"
            : !r.IsAlive ? $"† {ClanDomainView.DeathLabel(r.CauseOfDeath)}"
            : ClanDomainView.RetreatLabel(r) is { } away ? $"{RankCatalog.DisplayName(r)} — {away}"
            : RankCatalog.DisplayName(r);
    }
}
