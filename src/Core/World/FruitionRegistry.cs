using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The state of every Dao lineage in one game (LORE.md §6.8). The lore fixes some statuses (the Mutable
    /// Water is held by Tan Qing, the Nourishing Fire is broken…); the others are drawn from the world's
    /// seed at the start of the game (§11.7), so each game has its own map of free and held positions.
    /// The shared content is never modified.
    /// </summary>
    public sealed class FruitionRegistry
    {
        private readonly GameContext ctx;
        private readonly Dictionary<string, FruitionState> states = new Dictionary<string, FruitionState>();

        public FruitionRegistry(GameContext ctx)
        {
            this.ctx = ctx;
        }

        public IReadOnlyDictionary<string, FruitionState> States => states;

        /// <summary>A lineage's state in this game (the content's status until the world is drawn).</summary>
        public FruitionState State(string fruitionId)
        {
            if (states.TryGetValue(fruitionId, out var state)) return state;
            var fruition = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == fruitionId);
            return fruition == null ? null : new FruitionState(fruition.Status, fruition.Holder);
        }

        /// <summary>
        /// Sets every lineage's state: the lore's where it speaks, a draw where it does not. The world has its
        /// own random source (<see cref="WorldRandom"/>), so it never shifts the game's draws.
        /// </summary>
        public void DrawWorld(Random worldRng) => Restore(null, worldRng);

        /// <summary>Restores the world of a save; lineages it lacks (an older save, new data) are drawn from the world's source.</summary>
        public void Restore(IReadOnlyDictionary<string, FruitionState> saved, Random worldRng)
        {
            states.Clear();
            foreach (var fruition in ctx.Content.Fruitions) // content order: the same seed draws the same world
            {
                var drawn = fruition.Status == FruitionStatus.Unspecified
                    ? Draw(worldRng)
                    : new FruitionState(fruition.Status, fruition.Holder);
                states[fruition.Id] = saved != null && saved.TryGetValue(fruition.Id, out var state) && state != null ? state : drawn;
            }
        }

        /// <summary>The world's own random source for a game seed.</summary>
        public static Random WorldRandom(int seed) => new Random(unchecked(seed * 7919 + 104729));

        /// <summary>The status of a lineage the lore leaves open: free, occupied or broken, by the balance's odds.</summary>
        public static FruitionStatus DrawStatus(Random rng, FruitionOdds odds)
        {
            double roll = rng.NextDouble();
            if (roll < odds.Free) return FruitionStatus.Free;
            if (roll < odds.Free + odds.Occupied) return FruitionStatus.Occupied;
            return FruitionStatus.Broken;
        }

        private FruitionState Draw(Random worldRng)
        {
            var status = DrawStatus(worldRng, ctx.Content.Balance.UnspecifiedFruitionOdds);
            return new FruitionState(status, status == FruitionStatus.Occupied ? ctx.Content.AnonymousHolder : null);
        }
    }
}
