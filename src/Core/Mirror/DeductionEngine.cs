using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The mirror's deduction: 2 to 5 fragments become a technique, for 10 power per fragment.
    /// Clashing elements make it riskier, feeding ones safer. (Technique grades arrive with phase L3.)
    /// </summary>
    public sealed class DeductionEngine
    {
        public const int MinFragments = 2;
        public const int MaxFragments = 5;
        public const int PowerPerFragment = 10;
        private const int BaseRisk = 5;
        private const int ConflictRisk = 30;
        private const int SynergyRelief = 10;

        private static readonly int HighestRealm = Enum.GetValues(typeof(CultivationRealm)).Length - 1;

        /// <summary>What a deduction can yield until it follows the fragments (phase L3d).</summary>
        private static readonly (TechniqueKind, TechniqueEffect)[] DeducibleForms =
        {
            (TechniqueKind.Cultivation, TechniqueEffect.None),
            (TechniqueKind.Weapon, TechniqueEffect.Strike),
            (TechniqueKind.Spell, TechniqueEffect.Heal)
        };

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

        private TechniqueData GenerateTechnique(IReadOnlyList<FragmentData> inputs)
        {
            int totalQuality = inputs.Sum(f => f.Quality);
            var counts = inputs.GroupBy(f => f.Element).ToDictionary(g => g.Key, g => g.Count());
            var dominant = counts.OrderByDescending(kv => kv.Value).First().Key;
            var (kind, effect) = DeducibleForms[ctx.Rng.Next(0, DeducibleForms.Length)];

            return new TechniqueData
            {
                ID = ctx.Rng.NextId(),
                Name = ProceduralName(dominant, kind, totalQuality),
                Kind = kind,
                Effect = effect,
                DominantElement = dominant,
                RequiredRealm = (CultivationRealm)Math.Clamp(totalQuality / 3, 0, HighestRealm),
                PowerModifier = totalQuality * 5,
                QiCost = totalQuality * 2,
                Range = effect switch
                {
                    TechniqueEffect.Strike => Math.Clamp(1 + totalQuality / 8, 1, 3), // melee to short range
                    TechniqueEffect.Heal => 2,
                    _ => 0                                                             // cultivation method: self
                },
                RiskFactor = ElementalRisk(counts)
            };
        }

        private static int ElementalRisk(Dictionary<Element, int> counts)
        {
            int risk = BaseRisk;
            if (counts.ContainsKey(Element.Water) && counts.ContainsKey(Element.Fire)) risk += ConflictRisk;
            if (counts.ContainsKey(Element.Metal) && counts.ContainsKey(Element.Wood)) risk += ConflictRisk;
            if (counts.ContainsKey(Element.Wood) && counts.ContainsKey(Element.Fire)) risk = Math.Max(0, risk - SynergyRelief);
            return risk;
        }

        /// <summary>Placeholder names until technique grades and their names move to data (phase L3).</summary>
        private static string ProceduralName(Element element, TechniqueKind kind, int quality)
        {
            string prefix = element switch
            {
                Element.Fire => "Blazing",
                Element.Water => "Flowing",
                Element.Wood => "Verdant",
                Element.Metal => "Piercing",
                Element.Earth => "Unshakable",
                Element.Lightning => "Heavenly",
                Element.Darkness => "Shadow",
                Element.Light => "Radiant",
                _ => "Mystic"
            };
            string suffix = kind switch
            {
                TechniqueKind.Cultivation => "Mantra",
                TechniqueKind.Weapon => "Fist",
                _ => "Aura"
            };
            string adjective = quality > 10 ? "Divine " : quality > 5 ? "Profound " : "";
            return $"{adjective}{prefix} {suffix}";
        }
    }
}
