using MirrorChronicles.Data;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>The pure rules of the powers' own politics.</summary>
    public static class PoliticsRules
    {
        /// <summary>How close two powers are: neighbours, of one Dao, less the distrust between them (both ways).</summary>
        public static int Affinity(FactionData a, FactionData b, bool neighbours, int distrust, PoliticsSettings s) =>
            (neighbours ? s.NeighbourAffinity : 0) + (a.Path == b.Path ? s.SamePathAffinity : 0) - distrust;
    }
}
