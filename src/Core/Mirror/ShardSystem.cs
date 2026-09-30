using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The mirror's shards (LORE.md §11.5, B3c; the user's decisions of 2026-09-30): seven pieces of jade — one in the lake,
    /// two in ruins, three held by powers, one in the Great Void. Each one recovered restores the mirror, gives back its
    /// memory (a technique, facts, a clue to the mirror's origin), and puts the spirit to sleep while it integrates it.
    /// The lake's shard is dredged up by the clan's searchers (B3c1); the others come with their own operations.
    /// </summary>
    public sealed class ShardSystem
    {
        private readonly GameContext ctx;
        private readonly MirrorSystem mirror;
        private readonly TechniqueLibrary techniques;
        private readonly KnowledgeBase knowledge;
        private readonly List<string> recovered = new List<string>();

        public IReadOnlyList<string> Recovered => recovered;

        public ShardSystem(GameContext ctx, MirrorSystem mirror, TechniqueLibrary techniques, KnowledgeBase knowledge)
        {
            this.ctx = ctx;
            this.mirror = mirror;
            this.techniques = techniques;
            this.knowledge = knowledge;
        }

        private ShardSettings Settings => ctx.Content.Balance.Shards;

        public bool IsRecovered(string shardId) => recovered.Contains(shardId);

        /// <summary>The lake is searched until its shard is found.</summary>
        public bool LakeSearchOpen => LakeShard != null && !IsRecovered(LakeShard.Id);

        private ShardDefinition LakeShard => ctx.Content.Shards.FirstOrDefault(s => s.Source == ShardSource.Lake);

        /// <summary>
        /// A shard comes back to the mirror: it grows, sleeps to integrate it, and gives back its memory. False when the
        /// shard is unknown or already recovered.
        /// </summary>
        public bool Recover(string shardId)
        {
            var shard = ctx.Content.Shards.FirstOrDefault(s => s.Id == shardId);
            if (shard == null || IsRecovered(shardId)) return false;

            recovered.Add(shardId);
            mirror.RestoreShard(shard.SleepYears);
            if (shard.Memory.TechniqueId != null) techniques.Learn(shard.Memory.TechniqueId);
            foreach (var fact in shard.Memory.Facts ?? System.Array.Empty<string>())
                knowledge.Reveal(Fact.Parse(fact), KnowledgeSource.Mirror);

            ctx.Log.Info($"[Shards] The mirror recovers {shard.Name} ({recovered.Count}/{ctx.Content.Shards.Count}); it sleeps {shard.SleepYears} year(s).");
            ctx.Events.TriggerShardRecovered(shard);
            return true;
        }

        /// <summary>A year of dredging the lake: each searcher may find its shard (🔎 balance.json « shards »).</summary>
        public void SearchLake(int searchers)
        {
            if (searchers <= 0 || !LakeSearchOpen) return;
            double chance = 1 - System.Math.Pow(1 - Settings.LakeSearchChance, searchers);
            if (ctx.Rng.Chance(chance)) Recover(LakeShard.Id);
        }

        public void Restore(IEnumerable<string> saved)
        {
            recovered.Clear();
            recovered.AddRange((saved ?? Enumerable.Empty<string>()).Where(id => ctx.Content.Shards.Any(s => s.Id == id)).Distinct());
        }
    }
}
