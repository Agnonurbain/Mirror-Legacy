using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>The pure rules of who knows the mirror exists (user decision, 2026-09-27).</summary>
    public static class MirrorLoreRules
    {
        /// <summary>The chance a being of this realm and age knows: none below the listed realms, more with the centuries.</summary>
        public static double KnowChance(CultivationRealm realm, int age, MirrorLoreSettings s)
        {
            if (s.KnowChance == null || !s.KnowChance.TryGetValue(realm, out var chance)) return 0;
            double perCentury = s.PerCentury != null && s.PerCentury.TryGetValue(realm, out var p) ? p : 0;
            return Math.Clamp(chance + perCentury * Math.Max(0, age) / 100.0, 0, s.MaxChance);
        }

        /// <summary>A named figure's chance this year: nothing before its birth; its age, or the unknown age when the lore gives none.</summary>
        public static double FigureChance(FigureDefinition figure, int year, MirrorLoreSettings s)
        {
            if (figure.BornYear > year) return 0;
            int age = figure.BornYear.HasValue ? year - figure.BornYear.Value : s.UnknownAge + year;
            return KnowChance(figure.Realm, age, s);
        }

        /// <summary>
        /// An elder's chance to know the mirror (user decision 2026-10-01): an ancient being, there when the world began, by
        /// the unknown age of its kind; one risen since, by its centuries at the summit only — a new True Monarch knows nothing.
        /// </summary>
        public static double ElderChance(FactionElder elder, int year, MirrorLoreSettings s) =>
            KnowChance(elder.Realm, elder.Ancient ? s.UnknownAge + year : year - elder.RealmSinceYear, s);

        /// <summary>A stable number in [0, 1) for a world and a being: who knows is fixed by the world, never by the moment.</summary>
        public static double Draw(int worldSeed, string id)
        {
            uint hash = 2166136261;
            foreach (char c in $"{worldSeed}:{id}") hash = (hash ^ c) * 16777619;
            hash ^= hash >> 13; // FNV-1a, then the MurmurHash2 finaliser to spread the bits
            hash *= 0x5bd1e995;
            hash ^= hash >> 15;
            return (hash & 0xFFFFFF) / (double)0x1000000;
        }
    }
}
