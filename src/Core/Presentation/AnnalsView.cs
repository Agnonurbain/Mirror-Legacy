using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>The clan's Annals as the player reads them (LORE.md §11.9): one French line per milestone, oldest first.</summary>
    public static class AnnalsView
    {
        public static IReadOnlyList<string> Lines(GameSession session) =>
            session.Annals.Entries.Select(e => $"An {e.Year} : {Describe(e)}").ToList();

        public static string Describe(AnnalEntry entry) => entry.Kind switch
        {
            AnnalKind.RealmReached => $"{entry.Subject} atteint {WithArticle((CultivationRealm)entry.Value)}, une première pour le clan.",
            AnnalKind.PositionTaken => $"{entry.Subject} obtient {FirstPosition((GoldenCoreState)entry.Value)}.",
            AnnalKind.Generation => $"la {entry.Value}e génération commence sous {entry.Subject ?? "un patriarche oublié"}.",
            _ => entry.Subject
        };

        /// <summary>A realm's name with its article: « la Culture du Qi », « l'Embryon du Dao », « le Manoir Pourpre ».</summary>
        private static string WithArticle(CultivationRealm realm)
        {
            string name = RankCatalog.RealmName(realm);
            if ("AEÉIOU".Contains(name[0])) return "l'" + name;
            return realm == CultivationRealm.QiRefinement || realm == CultivationRealm.Embryonic ? "la " + name : "le " + name;
        }

        private static string FirstPosition(GoldenCoreState position) => position switch
        {
            GoldenCoreState.Realization => "la première Réalisation d'une Fruition du clan",
            GoldenCoreState.Surplus => "le premier Surplus d'une Fruition du clan",
            GoldenCoreState.Intercalary => "le premier Intercalaire du clan",
            GoldenCoreState.TrueLeftHand => "la première Main Gauche vraie du clan",
            GoldenCoreState.FalseLeftHand => "la première Main Gauche fausse du clan",
            _ => "une puissance de Noyau d'Or, une première pour le clan"
        };
    }
}
