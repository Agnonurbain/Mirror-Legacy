using System;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>
    /// A power's Protective Formation (AUDIT_LORE.md §2.3, world parity — 🔎 interpretations): as high as its realm knows, it
    /// guards the power as the clan's guards the clan — each level takes the same share off a theft's or a sabotage's odds.
    /// </summary>
    public static class PowerFormation
    {
        /// <summary>The highest formation the power's masters know how to set (balance.json « arts.formation.powerCapByRealm »).</summary>
        public static int Cap(FactionData power, GameContent content)
        {
            var caps = content.Balance.Arts.Formation.PowerCapByRealm;
            var known = caps.Keys.Where(r => r <= power.HighestRealm).ToList();
            return known.Count == 0 ? 0 : caps[known.Max()];
        }

        /// <summary>The formation a power holds when the world begins: one level below what it knows.</summary>
        public static int Start(FactionData power, GameContent content) => Math.Max(0, Cap(power, content) - 1);

        /// <summary>What remains of a blow's odds against the power's formation (1: none).</summary>
        public static double Guard(FactionData power, GameContent content) =>
            power == null ? 1.0 : Math.Max(0, 1 - power.FormationLevel * content.Balance.DaoHunts.FormationGuard);
    }
}
