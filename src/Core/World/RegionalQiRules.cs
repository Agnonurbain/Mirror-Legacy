using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>The pure rules of the regional atmospheres (L5b; LORE.md §5.8).</summary>
    public static class RegionalQiRules
    {
        /// <summary>Whether an atmosphere favours a cultivator of this lineage, element or path.</summary>
        public static bool Favours(AtmosphereDefinition atmosphere, string fruitionId, Element element, CultivationPath path) =>
            atmosphere != null
            && ((fruitionId != null && atmosphere.FavouredFruitions.Contains(fruitionId))
                || atmosphere.FavouredElements.Contains(element)
                || atmosphere.FavouredPaths.Contains(path));

        /// <summary>An atmosphere's weight on cultivation: 1 + its general speed, + its favoured speed for those it favours.</summary>
        public static double AtmosphereSpeed(AtmosphereDefinition atmosphere, string fruitionId, Element element, CultivationPath path) =>
            atmosphere == null ? 1.0
                : 1.0 + atmosphere.GeneralSpeed + (Favours(atmosphere, fruitionId, element, path) ? atmosphere.FavouredSpeed : 0);

        /// <summary>The points of breakthrough chance an atmosphere gives those it favours.</summary>
        public static int AtmosphereBreakthrough(AtmosphereDefinition atmosphere, string fruitionId, Element element, CultivationPath path) =>
            Favours(atmosphere, fruitionId, element, path) ? atmosphere.FavouredBreakthrough : 0;
    }
}
