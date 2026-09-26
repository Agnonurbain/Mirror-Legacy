using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>What a fact is about. New kinds of knowledge are added here, with their implications.</summary>
    public enum FactKind
    {
        Technique,      // a technique of the catalog (its manual)
        Qi,             // a spiritual Qi
        Lineage,        // that a Dao lineage exists and what it is
        Ability,        // a foundation / divine ability (« fruition-id:ability-id »)
        DaoPartners,    // the Dao Partners of a foundation (« fruition-id:ability-id »)
        FoundationOfQi, // which foundation a Qi builds (a Qi id)
        Pact            // that a pact exists (a pact id): others' oaths are learned by spying, the mirror…
    }

    /// <summary>Where a piece of knowledge came from.</summary>
    public enum KnowledgeSource { Start, Learned, Formed, Condensed, Studied, Mirror, Event, Trade, Espionage, OlderSave }

    /// <summary>A piece of knowledge: a kind and what it is about, keyed « Kind:Subject » in saves.</summary>
    public readonly record struct Fact(FactKind Kind, string Subject)
    {
        public string Key => $"{Kind}:{Subject}";

        public static Fact Parse(string key) =>
            TryParse(key, out var fact) ? fact : throw new FormatException($"\"{key}\" is not a fact (Kind:Subject).");

        public static bool TryParse(string key, out Fact fact)
        {
            fact = default;
            int colon = key?.IndexOf(':') ?? -1;
            if (colon <= 0 || colon == key.Length - 1 || !Enum.TryParse(key.Substring(0, colon), out FactKind kind)) return false;
            fact = new Fact(kind, key.Substring(colon + 1));
            return true;
        }
    }

    /// <summary>
    /// What someone knows (user request, 2026-09-26; LORE.md §11 P3 « knowledge is a resource »): facts revealed
    /// once, by a source, with pluggable implications (a fact may entail others). The clan has one; factions
    /// can have theirs (P1: the same rules for everyone). Systems ask it before letting one act on a fact.
    /// </summary>
    public sealed class KnowledgeBase
    {
        private readonly HashSet<string> keys = new HashSet<string>();
        private readonly List<Func<Fact, IEnumerable<Fact>>> implications = new List<Func<Fact, IEnumerable<Fact>>>();

        /// <summary>Raised for each fact newly known (the chronicle, the screens may listen).</summary>
        public event Action<Fact, KnowledgeSource> Revealed;

        public bool Knows(Fact fact) => fact.Subject != null && keys.Contains(fact.Key);

        public bool Knows(FactKind kind, string subject) => subject != null && Knows(new Fact(kind, subject));

        /// <summary>Learns a fact and what it entails; false when it was already known.</summary>
        public bool Reveal(Fact fact, KnowledgeSource source)
        {
            if (fact.Subject == null || !keys.Add(fact.Key)) return false;
            Revealed?.Invoke(fact, source);
            foreach (var rule in implications.ToList())
                foreach (var implied in rule(fact) ?? Enumerable.Empty<Fact>())
                    Reveal(implied, source);
            return true;
        }

        public bool Reveal(FactKind kind, string subject, KnowledgeSource source) =>
            subject != null && Reveal(new Fact(kind, subject), source);

        /// <summary>A rule of entailment: when a fact is revealed, the facts it returns are revealed too.</summary>
        public void AddImplication(Func<Fact, IEnumerable<Fact>> rule) => implications.Add(rule);

        /// <summary>The subjects known of a kind, in a stable order.</summary>
        public IReadOnlyList<string> Subjects(FactKind kind)
        {
            string prefix = $"{kind}:";
            return keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .Select(k => k.Substring(prefix.Length))
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Every fact known, « Kind:Subject », in a stable order (for saves).</summary>
        public IReadOnlyList<string> Keys => keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        /// <summary>Restores what a save knew (its implications were saved with it); unreadable keys are dropped.</summary>
        public void Restore(IEnumerable<string> saved)
        {
            keys.Clear();
            foreach (var key in saved ?? Enumerable.Empty<string>())
                if (Fact.TryParse(key, out var fact)) keys.Add(fact.Key);
        }
    }

    /// <summary>The implications of the world's content (a technique's manual, a Qi's foundation, one's partners).</summary>
    public static class WorldKnowledge
    {
        /// <summary>A knowledge base that follows the world's implications.</summary>
        public static KnowledgeBase Create(GameContent content)
        {
            var knowledge = new KnowledgeBase();
            knowledge.AddImplication(fact => Implications(fact, content));
            return knowledge;
        }

        private static IEnumerable<Fact> Implications(Fact fact, GameContent content)
        {
            switch (fact.Kind)
            {
                case FactKind.Technique: // a method's manual names its Qi and the foundation it builds
                    var technique = content.Techniques.FirstOrDefault(t => t.ID == fact.Subject);
                    if (technique?.RequiredQiId != null)
                    {
                        yield return new Fact(FactKind.Qi, technique.RequiredQiId);
                        yield return new Fact(FactKind.FoundationOfQi, technique.RequiredQiId);
                    }
                    break;
                case FactKind.FoundationOfQi:
                    var foundation = content.Qi.FirstOrDefault(q => q.Id == fact.Subject)?.Foundation;
                    if (foundation != null) yield return new Fact(FactKind.Ability, foundation);
                    break;
                case FactKind.Ability:
                    var (lineage, _) = FoundationRef.Parse(fact.Subject);
                    if (lineage != null) yield return new Fact(FactKind.Lineage, lineage);
                    break;
                case FactKind.DaoPartners: // knowing one's partners reveals them
                    var (fruitionId, abilityId) = FoundationRef.Parse(fact.Subject);
                    var fruition = content.Fruitions.FirstOrDefault(f => f.Id == fruitionId);
                    if (fruition == null) break;
                    foreach (var partner in fruition.Abilities.Where(a => a.Id != abilityId))
                        yield return new Fact(FactKind.Ability, $"{fruitionId}:{partner.Id}");
                    break;
            }
        }
    }
}
