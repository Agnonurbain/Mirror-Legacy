using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// Who knows the mirror exists (user decision, 2026-09-27): an artefact of the very highest level, known only to a
    /// handful of very high-level beings — not all of them: their realm and their seniority decide, the old knowing more
    /// than the young. A power knows when one of its elders does: a named figure, or an unnamed elder at its highest
    /// realm when no figure of that realm is named. Hidden, fixed by the world's seed; knowledge only grows with the years.
    /// </summary>
    public sealed class MirrorLore
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly int worldSeed;

        public MirrorLore(GameContext ctx, FactionManager factions, int worldSeed)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.worldSeed = worldSeed;
        }

        private MirrorLoreSettings Settings => ctx.Content.Balance.MirrorLore;

        public bool Knows(string faction) => KnowerOf(faction) != null;

        /// <summary>The elder of a power who knows the mirror exists: a figure's name, « un ancien de … », or null.</summary>
        public string KnowerOf(string faction)
        {
            var power = factions.GetFactionByName(faction);
            if (power == null) return null;
            int year = ctx.Clock.Year;
            var figures = ctx.Content.Figures.Where(f => f.FactionName == faction
                && (power.Elders.Count == 0 || power.Elders.Any(e => e.FigureId == f.Id))).ToList(); // a secret dies with its keeper (2026-10-01)
            var named = figures
                .OrderByDescending(f => f.Realm).ThenBy(f => f.BornYear ?? int.MaxValue)
                .FirstOrDefault(f => MirrorLoreRules.Draw(worldSeed, f.Id) < MirrorLoreRules.FigureChance(f, year, Settings));
            if (named != null) return named.Name;
            if (power.Elders.Count > 0) // its living elders, each by its own centuries (2026-10-01)
                return power.Elders.Where(e => e.FigureId == null).OrderByDescending(e => e.Realm)
                    .FirstOrDefault(e => MirrorLoreRules.Draw(worldSeed, e.Id) < MirrorLoreRules.ElderChance(e, year, Settings))?.Name;
            if (figures.Any(f => f.Realm == power.HighestRealm)) return null; // its strongest is named, and does not know

            double elder = MirrorLoreRules.KnowChance(power.HighestRealm, Settings.UnknownAge + year, Settings);
            return MirrorLoreRules.Draw(worldSeed, $"elder:{power.Name}") < elder ? $"un ancien de {power.Name}" : null;
        }
    }
}
