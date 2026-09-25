using System.Collections.Generic;
using System.Linq;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// The initiative queue: each round, the standing units act by initiative (agility + 10 per realm);
    /// the fallen and the fled lose their turn.
    /// </summary>
    public sealed class TurnOrder
    {
        private readonly List<CombatUnit> units;
        private readonly Queue<CombatUnit> queue = new Queue<CombatUnit>();

        public int Round { get; private set; }
        public CombatUnit Current { get; private set; }

        public TurnOrder(IEnumerable<CombatUnit> units)
        {
            this.units = units.ToList();
        }

        public static int Initiative(CombatUnit unit) => unit.Agility + (int)unit.BaseData.Realm * 10;

        /// <summary>The next unit to act (its turn state refreshed), or null when nobody stands.</summary>
        public CombatUnit Next()
        {
            while (true)
            {
                while (queue.Count > 0 && !queue.Peek().IsActive)
                    queue.Dequeue();

                if (queue.Count > 0)
                {
                    Current = queue.Dequeue();
                    Current.ResetTurnState();
                    return Current;
                }

                var standing = units.Where(u => u.IsActive).OrderByDescending(Initiative).ToList();
                if (standing.Count == 0)
                {
                    Current = null;
                    return null;
                }

                Round++;
                foreach (var unit in standing)
                    queue.Enqueue(unit);
            }
        }
    }
}
