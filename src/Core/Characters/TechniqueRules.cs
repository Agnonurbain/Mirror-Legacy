using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Pure rules of techniques and their grades (LORE.md §2): which realms a cultivation method covers,
    /// how fast it cultivates, where it stops, and what it needs.
    /// </summary>
    public static class TechniqueRules
    {
        public const int MinGrade = 1;
        public const int MaxGrade = 7; // « 7+ »

        /// <summary>
        /// The last realm a method of this grade covers (LORE.md §2.2): 1-2 Qi Cultivation, 3-4 Foundation,
        /// 5-6 Purple Mansion (a grade 6 also aims at the Golden Core), 7+ everything.
        /// </summary>
        public static CultivationRealm DefaultSupremeRealm(int grade)
        {
            if (grade <= 2) return CultivationRealm.QiRefinement;
            if (grade <= 4) return CultivationRealm.Foundation;
            if (grade <= 6) return CultivationRealm.PurpleMansion;
            return CultivationRealm.GoldenImmortal;
        }

        /// <summary>The method's own supreme realm when the lore gives one (§2.4 exceptions), otherwise its grade's.</summary>
        public static CultivationRealm SupremeRealm(TechniqueData method) =>
            method.SupremeRealm ?? DefaultSupremeRealm(method.Grade);

        /// <summary>True when the cultivation method guides cultivation in this realm.</summary>
        public static bool Covers(TechniqueData method, CultivationRealm realm) =>
            method != null
            && method.Kind == TechniqueKind.Cultivation
            && realm >= method.RequiredRealm
            && realm <= SupremeRealm(method);
    }
}
