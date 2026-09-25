using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The mirror's deduction: 2 to 5 fragments become a graded technique, for 10 power per fragment.
    /// Clashing elements make it riskier, feeding ones safer.
    /// </summary>
    public sealed class DeductionEngine
    {
        public const int MinFragments = 2;
        public const int MaxFragments = 5;
        public const int PowerPerFragment = 10;
        private const int BaseRisk = 5;
        private const int ConflictRisk = 30;
        private const int SynergyRelief = 10;

        /// <summary>What a deduction can yield; immortal arts wait for alchemy and artifacts.</summary>
        private static readonly TechniqueKind[] DeducibleKinds =
            { TechniqueKind.Cultivation, TechniqueKind.Spell, TechniqueKind.Movement, TechniqueKind.Weapon };

        private readonly GameContext ctx;
        private readonly MirrorSystem mirror;
        private readonly TechniqueLibrary library;
        private readonly List<FragmentData> fragments = new List<FragmentData>();

        public IReadOnlyList<FragmentData> Fragments => fragments;

        /// <param name="library">Where deduced techniques join the clan's knowledge.</param>
        public DeductionEngine(GameContext ctx, MirrorSystem mirror, TechniqueLibrary library)
        {
            this.ctx = ctx;
            this.mirror = mirror;
            this.library = library;
        }

        public void AddFragment(Element element, int quality, string name = "Unknown Fragment")
        {
            fragments.Add(new FragmentData { ID = ctx.Rng.NextId(), Element = element, Quality = quality, Name = name });
        }

        /// <summary>Returns the new technique, or null when the fragments or the power are lacking.</summary>
        public TechniqueData AttemptDeduction(IReadOnlyList<FragmentData> inputs)
        {
            if (inputs == null || inputs.Count < MinFragments || inputs.Count > MaxFragments)
            {
                ctx.Log.Warning($"[Deduction] A deduction needs {MinFragments} to {MaxFragments} fragments.");
                return null;
            }
            if (!mirror.ConsumePower(inputs.Count * PowerPerFragment)) return null;

            foreach (var fragment in inputs)
                fragments.Remove(fragment);

            var technique = GenerateTechnique(inputs);
            library.AddDeduced(technique);
            ctx.Log.Info($"[Deduction] The mirror deduces [{technique.Name}] ({technique.DominantElement}, risk {technique.RiskFactor}%).");
            return technique;
        }

        public void Restore(IEnumerable<FragmentData> savedFragments)
        {
            fragments.Clear();
            fragments.AddRange(savedFragments);
        }

        /// <summary>
        /// A secret technique (LORE.md §2.3: rebuilt from pieces) graded by its fragments (§2.2). A method is
        /// built on a harvestable Qi of its dominant element; an art opens at the realm of its grade.
        /// </summary>
        private TechniqueData GenerateTechnique(IReadOnlyList<FragmentData> inputs)
        {
            int totalQuality = inputs.Sum(f => f.Quality);
            int grade = TechniqueRules.DeductionGrade(inputs.Select(f => f.Quality).ToList());
            var counts = inputs.GroupBy(f => f.Element).ToDictionary(g => g.Key, g => g.Count());
            var dominant = counts.OrderByDescending(kv => kv.Value).First().Key;
            var kinds = HarvestableQi().Any() ? DeducibleKinds : DeducibleKinds.Where(k => k != TechniqueKind.Cultivation).ToArray();
            var kind = kinds[ctx.Rng.Next(0, kinds.Length)];
            var effect = kind switch
            {
                TechniqueKind.Weapon => TechniqueEffect.Strike,
                TechniqueKind.Spell => ctx.Rng.Chance(0.5) ? TechniqueEffect.Strike : TechniqueEffect.Heal,
                _ => TechniqueEffect.None
            };
            bool isMethod = kind == TechniqueKind.Cultivation;

            return new TechniqueData
            {
                ID = ctx.Rng.NextId(),
                Name = Name(kind, grade, dominant),
                Kind = kind,
                Effect = effect,
                Grade = grade,
                Category = TechniqueCategory.Secret,
                DominantElement = dominant,
                RequiredRealm = isMethod ? CultivationRealm.QiRefinement : TechniqueRules.ArtRequiredRealm(grade),
                RequiredQiId = isMethod ? QiFor(dominant)?.Id : null,
                PowerModifier = totalQuality * 5,
                QiCost = totalQuality * 2,
                Range = effect switch
                {
                    TechniqueEffect.Strike => Math.Clamp(1 + totalQuality / 8, 1, 3), // melee to short range
                    TechniqueEffect.Heal => 2,
                    _ => 0                                                             // methods and movement: self
                },
                RiskFactor = ElementalRisk(counts)
            };
        }

        /// <summary>Qi the clan could harvest; without any, the mirror deduces no method (it could lead nowhere).</summary>
        private IEnumerable<QiDefinition> HarvestableQi() => ctx.Content.Qi.Where(q => !q.Vanished && !q.Ubiquitous);

        /// <summary>A Qi the clan can harvest for a deduced method: of its element when one exists, any otherwise.</summary>
        private QiDefinition QiFor(Element element)
        {
            var harvestable = HarvestableQi().ToList();
            var ofElement = harvestable.Where(q => q.Element == element).ToList();
            var candidates = ofElement.Count > 0 ? ofElement : harvestable;
            return candidates.Count == 0 ? null : candidates[ctx.Rng.Next(0, candidates.Count)];
        }

        /// <summary>The name the data gives: the kind's noun, the grade's word and the element's phrase.</summary>
        private string Name(TechniqueKind kind, int grade, Element element)
        {
            var words = ctx.Content.DeductionNames;
            return words.Template
                .Replace("{kind}", words.Kinds.TryGetValue(kind, out var noun) ? noun : kind.ToString())
                .Replace("{grade}", words.GradeWords[Math.Clamp(grade, TechniqueRules.MinGrade, TechniqueRules.MaxGrade) - 1])
                .Replace("{element}", words.Elements.TryGetValue(element, out var phrase) ? phrase : "")
                .Trim();
        }

        private static int ElementalRisk(Dictionary<Element, int> counts)
        {
            int risk = BaseRisk;
            if (counts.ContainsKey(Element.Water) && counts.ContainsKey(Element.Fire)) risk += ConflictRisk;
            if (counts.ContainsKey(Element.Metal) && counts.ContainsKey(Element.Wood)) risk += ConflictRisk;
            if (counts.ContainsKey(Element.Wood) && counts.ContainsKey(Element.Fire)) risk = Math.Max(0, risk - SynergyRelief);
            return risk;
        }
    }
}
