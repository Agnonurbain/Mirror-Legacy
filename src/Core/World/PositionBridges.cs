using System.Collections.Generic;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The Virtues corrupted in the world (R6; user decisions 2026-10-03): while a Virtue is corrupted, its bridges between
    /// lineages open the Intercalary 4A+1B=C. The Water's is from the start (the lore's one case); the others wait for an
    /// event to corrupt theirs.
    /// </summary>
    public sealed class PositionBridges
    {
        private readonly GameContext ctx;
        private readonly HashSet<Element> corrupted = new HashSet<Element>();

        public PositionBridges(GameContext ctx)
        {
            this.ctx = ctx;
            Restore(null);
        }

        public IReadOnlyCollection<Element> Corrupted => corrupted;

        /// <summary>The saved Virtues (null: a save from before 2.32, or a new game: those of the start).</summary>
        public void Restore(IEnumerable<Element> saved)
        {
            corrupted.Clear();
            foreach (var virtue in saved ?? ctx.Content.Balance.PositionBridges.CorruptedAtStart) corrupted.Add(virtue);
        }

        public void Corrupt(Element virtue)
        {
            if (!corrupted.Add(virtue)) return;
            ctx.Log.Warning($"[Bridges] The Virtue of {virtue} is corrupted: its bridges open.");
        }
    }
}
