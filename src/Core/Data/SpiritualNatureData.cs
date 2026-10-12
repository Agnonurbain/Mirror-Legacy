using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Spiritual Nature (balance.json « spiritualNature »; LORE.md §3.1, R10; the user's choices 2026-10-12): one child
    /// with an orifice in a thousand, examined, is elected — its destiny concentrated on a Fruition of its own element (a
    /// free one first), whose virtue dictates its conduct: the tasks it forbids (an interpretation — the Five Virtues, Ze
    /// Wuyan who bowed to no one).
    /// </summary>
    public sealed class SpiritualNatureSettings
    {
        public double ElectChance { get; init; }
        public Dictionary<Element, List<TaskType>> ForbiddenTasks { get; init; } = new Dictionary<Element, List<TaskType>>();
    }
}
