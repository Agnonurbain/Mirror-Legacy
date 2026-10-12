using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The elect of the Spiritual Nature (R10; LORE.md §3.1; the user's choices of 2026-10-12): among the children just found
    /// to bear an orifice, one in a thousand is elected — its destiny bound to a Fruition of its element, a free one first —
    /// and that Fruition's virtue forbids it some tasks. See <see cref="SpiritualNatureSettings"/>.
    /// </summary>
    public sealed class SpiritualNatureSystem
    {
        private readonly GameContext ctx;
        private readonly FruitionRegistry fruitions;

        public SpiritualNatureSystem(GameContext ctx, FruitionRegistry fruitions)
        {
            this.ctx = ctx;
            this.fruitions = fruitions;
        }

        private SpiritualNatureSettings Settings => ctx.Content.Balance.SpiritualNature;

        /// <summary>The members just found to bear an orifice: each child may be elected, one in a thousand.</summary>
        public void Consider(IEnumerable<CharacterData> examined)
        {
            foreach (var member in examined.ToList())
                if (ctx.Rng.Chance(Settings.ElectChance)) Elect(member);
        }

        /// <summary>
        /// Elects the member; false for an adult (a spouse examined on joining), a member already bound to a lineage or walking
        /// another way, or when no Fruition of its element exists.
        /// </summary>
        public bool Elect(CharacterData member)
        {
            if (member == null || member.Age >= TaskRules.WorkingAge || member.FruitionId != null
                || member.SubPath != CultivationSubPath.PurpleMansionGoldenCore || member.Affinity == Element.None) return false;
            var fitting = ctx.Content.Fruitions.Where(f => f.Element == member.Affinity).ToList();
            if (fitting.Count == 0) return false;
            var free = fitting.Where(f => fruitions.State(f.Id)?.Status == FruitionStatus.Free).ToList();
            var pool = free.Count > 0 ? free : fitting; // a free lineage first: its destiny may one day be fulfilled
            var destiny = pool[ctx.Rng.Next(pool.Count)];

            member.SubPath = CultivationSubPath.SpiritualNature;
            member.FruitionId = destiny.Id;
            member.ForbiddenTasks = Settings.ForbiddenTasks.TryGetValue(destiny.Element, out var forbidden) ? new List<TaskType>(forbidden) : new List<TaskType>();
            if (member.ForbiddenTasks.Contains(member.CurrentTask) && member.CurrentTask != TaskType.HuntBeast && member.CurrentTask != TaskType.Diversion)
                member.CurrentTask = TaskType.None; // its virtue forbids it at once (an operation under way ends with the year)
            ctx.Log.Info($"[SpiritualNature] {member.FullName} is elected: its destiny is bound to {destiny.Id}.");
            ctx.Events.TriggerSpiritualNatureElected(member, destiny.Id);
            return true;
        }
    }
}
