using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// What the artifact a member bears gives it (L4f, user decisions 2026-10-03): nothing above its own realm; more to a
    /// bearer of its lineage; a cultivation aid helps only a bearer of its lineage (or one with no lineage of its own).
    /// </summary>
    public static class ArtifactRules
    {
        public static bool Wields(CharacterData member) => member?.Artifact != null && member.Realm >= member.Artifact.Rank;

        public static bool OfItsLineage(CharacterData member) =>
            member?.Artifact?.Lineage != null && FoundationRef.Parse(member.FoundationId).FruitionId == member.Artifact.Lineage;

        private static double Kin(CharacterData member) => OfItsLineage(member) ? member.Artifact.LineageFactor : 1.0;

        public static int Strength(CharacterData member) =>
            Wields(member) ? (int)Math.Round(member.Artifact.Strength * Kin(member)) : 0;

        /// <summary>The chance it foils an ambush or a harvest on its bearer.</summary>
        public static double Protection(CharacterData member) =>
            Wields(member) ? Math.Min(0.95, member.Artifact.Protection * Kin(member)) : 0;

        /// <summary>What an artifact is worth: its rank's forging (ores at their worth in stones), and its class.</summary>
        public static int Worth(ArtifactInstance a, ArtifactSettings s, int oreValue)
        {
            var cost = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.Select(System.Linq.Enumerable.OrderByDescending(
                System.Linq.Enumerable.Where(s.Forging, f => f.Key <= a.Rank), f => f.Key), f => f.Value)) ?? new ArtifactForging();
            double factor = s.ClassFactor.TryGetValue(a.Class, out var f) ? f : 1.0;
            return (int)((cost.Stones + cost.Ores * oreValue) * factor);
        }

        /// <summary>The factor on its bearer's cultivation (1: none).</summary>
        public static double CultivationSpeed(CharacterData member)
        {
            if (!Wields(member) || member.Artifact.Cultivation <= 0) return 1.0;
            bool helps = member.Artifact.Lineage == null || OfItsLineage(member);
            return helps ? 1.0 + member.Artifact.Cultivation * Kin(member) : 1.0;
        }
    }
}
