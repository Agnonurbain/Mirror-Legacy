using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Rank names per cultivation path and asymmetric power equivalences (LORE.md §3.7, §5, lexicon §13).
    /// Display names are data; they move to ScriptableObjects with the UI pass.
    /// </summary>
    public static class RankCatalog
    {
        private static readonly string[] RealmNames =
        {
            "Respiration Embryonnaire", "Culture du Qi", "Établissement des Fondations",
            "Manoir Pourpre", "Noyau d'Or", "Embryon du Dao", "Immortel Doré"
        };

        private static readonly string[] ChakraNames =
            { "sans chakra", "Lac Intérieur", "Marée Respirante", "Roue des Méridiens", "Sève d'Émeraude", "Œil du Sommet", "Premier Souffle" };

        private static readonly string[] FoundationStages = { "initial", "intermédiaire", "tardif", "apogée" };
        private static readonly string[] GoldenCoreStages = { "débutant", "intermédiaire", "avancé", "apogée" };
        private static readonly string[] PurpleMansionStages =
            { "début (Maître taoïste)", "milieu (Maître taoïste)", "fin (Grand Maître taoïste)", "Grande Perfection (Grand Maître taoïste)" };

        /// <summary>Human-readable rank, e.g. "Culture du Qi — 4e niveau (milieu)" or "Maître Moine".</summary>
        public static string DisplayName(CharacterData character)
        {
            switch (character.Path)
            {
                case CultivationPath.Buddhist:
                    return BuddhistRank(character.Realm, character.RealmStage);
                case CultivationPath.Devil when character.SubPath == CultivationSubPath.HeavenlyEmbryoDemon:
                    if (character.Realm == CultivationRealm.QiRefinement) return "Manoir Divers";
                    if (character.Realm == CultivationRealm.Foundation) return "Fournaise Unifiée";
                    break;
                case CultivationPath.Divine:
                    if (character.Realm >= CultivationRealm.QiRefinement && character.Realm <= CultivationRealm.PurpleMansion) return "Serviteur Divin";
                    if (character.Realm == CultivationRealm.GoldenCore) return "Noyau Divin";
                    break;
            }
            return ImmortalRank(character.Realm, character.RealmStage);
        }

        /// <summary>
        /// Offset added to the shared power tier: Master Monk slightly above Foundation,
        /// Merciful One slightly below Purple Mansion, Maha between Purple Mansion and Golden Core.
        /// </summary>
        public static float PowerOffset(CharacterData character)
        {
            if (character.Path != CultivationPath.Buddhist) return 0f;
            if (character.Realm == CultivationRealm.Foundation) return 0.1f;
            if (character.Realm == CultivationRealm.PurpleMansion) return IsMaha(character.RealmStage) ? 0.5f : -0.1f;
            return 0f;
        }

        private static string ImmortalRank(CultivationRealm realm, int stage)
        {
            string realmName = RealmNames[(int)realm];
            switch (realm)
            {
                case CultivationRealm.Embryonic:
                    return $"{realmName} — {ChakraNames[Clamp(stage, 0, 6)]}";
                case CultivationRealm.QiRefinement:
                    return $"{realmName} — {Ordinal(stage)} niveau ({PhaseName(PowerLadder.QiPhaseOf(stage))})";
                case CultivationRealm.Foundation:
                    return $"{realmName} — {FoundationStages[Clamp(stage, 1, 4) - 1]}";
                case CultivationRealm.PurpleMansion:
                    return $"{realmName} — {PurpleMansionStages[Clamp(stage, 1, 4) - 1]}";
                case CultivationRealm.GoldenCore:
                    return $"{realmName} — Vrai Monarque {GoldenCoreStages[Clamp(stage, 1, 4) - 1]}";
                case CultivationRealm.DaoEmbryo:
                    return $"{realmName} — Immortel Exalté";
                default:
                    return $"{realmName} — Seigneur Immortel";
            }
        }

        private static string BuddhistRank(CultivationRealm realm, int stage)
        {
            switch (realm)
            {
                case CultivationRealm.Embryonic:
                case CultivationRealm.QiRefinement: return "Moine";
                case CultivationRealm.Foundation: return "Maître Moine";
                case CultivationRealm.PurpleMansion: return IsMaha(stage) ? "Maha" : "Miséricordieux";
                case CultivationRealm.GoldenCore: return "Maître du Dharma";
                default: return "Vénérable";
            }
        }

        private static bool IsMaha(int purpleMansionStage) => purpleMansionStage >= 3;

        private static string PhaseName(QiPhase phase)
        {
            switch (phase)
            {
                case QiPhase.Early: return "début";
                case QiPhase.Middle: return "milieu";
                default: return "fin";
            }
        }

        private static string Ordinal(int n) => n == 1 ? "1er" : $"{n}e";

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
