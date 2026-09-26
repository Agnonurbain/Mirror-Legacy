using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Presentation
{
    /// <summary>A piece of knowledge told in French (the chronicle, the screens), from the content's names.</summary>
    public static class KnowledgeView
    {
        public static string Describe(Fact fact, GameContent content)
        {
            switch (fact.Kind)
            {
                case FactKind.Technique:
                    return $"la technique {content.Techniques.FirstOrDefault(t => t.ID == fact.Subject)?.Name ?? fact.Subject}";
                case FactKind.Qi:
                    return $"à connaître le {QiName(fact.Subject, content)}";
                case FactKind.FoundationOfQi:
                    return $"la fondation que bâtit le {QiName(fact.Subject, content)}";
                case FactKind.Lineage:
                    return $"l'existence de la lignée {LineageName(fact.Subject, content)}";
                case FactKind.Ability:
                    return $"la capacité {AbilityName(fact.Subject, content)}";
                case FactKind.DaoPartners:
                    return $"les Partenaires Dao de {AbilityName(fact.Subject, content)}";
                case FactKind.Pact:
                    return "l'existence d'un pacte";
                case FactKind.GoldSeeking:
                    var (lineage, specialised) = FoundationRef.Parse(fact.Subject);
                    return specialised == null
                        ? $"la méthode de recherche d'or de la lignée {LineageName(fact.Subject, content)}"
                        : $"la méthode de recherche d'or spécialisée de la lignée {LineageName(lineage, content)}";
                case FactKind.LeftHand:
                    var fruition = content.Fruitions.FirstOrDefault(f => f.Id == fact.Subject);
                    return $"la voie des {fruition?.LeftHand ?? fact.Subject} ({fruition?.Name ?? fact.Subject})";
                default:
                    return fact.Key;
            }
        }

        private static string QiName(string id, GameContent content) => content.Qi.FirstOrDefault(q => q.Id == id)?.Name ?? id;

        private static string LineageName(string id, GameContent content) => content.Fruitions.FirstOrDefault(f => f.Id == id)?.Name ?? id;

        /// <summary>« Ciel d'Orage (Eau Orthodoxe) »; an ability the lore does not name stays « non révélée ».</summary>
        private static string AbilityName(string reference, GameContent content)
        {
            var (lineageId, abilityId) = FoundationRef.Parse(reference);
            var lineage = content.Fruitions.FirstOrDefault(f => f.Id == lineageId);
            string name = lineage?.Abilities.FirstOrDefault(a => a.Id == abilityId)?.Name ?? "non révélée";
            return $"{name} ({lineage?.Name ?? lineageId})";
        }
    }
}
