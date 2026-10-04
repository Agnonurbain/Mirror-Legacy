using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    public sealed record MirrorHeader(int Power, int MaxPower, int Seeds, int SeedCapacity);

    /// <summary>A divine intervention: its cost, its effect, and why not now (null when it may be used).</summary>
    public sealed record InterventionLine(string Id, string Name, int Cost, string Effect, string Refusal);

    /// <summary>A member the Talisman Seed could take root in, and why not (null when it could).</summary>
    public sealed record SeedCandidate(string Id, string Name, int Age, string Refusal);

    /// <summary>A member the Judgment could strike; an in-law shows the power that sent them.</summary>
    public sealed record JudgmentTarget(string Id, string Name, string Realm, string Origin)
    {
        public string Power { get; init; }    // a power's elder (null: a member of the clan)
        public string Effect { get; init; }   // what the Light does: « tue », « blesse »
        public string Refusal { get; init; }  // why it cannot strike (null: it can)
    }

    public sealed record FragmentLine(string Id, string Name, string Element, int Quality);

    public sealed record DeductionPreviewLine(int Cost, string Refusal);

    /// <summary>
    /// The mirror's screen (G6, L2b; LORE.md §4, §11.5): its power, the seeds it sustains, its interventions and the
    /// deduction of the fragments. What the clan never examined stays hidden: the seed is offered only to a mortal whose
    /// lack of an orifice is known.
    /// </summary>
    public static class MirrorView
    {
        public const string Shield = "ancestral-shield";
        public const string Judgment = "mirror-judgment";
        public const string Seed = "talisman-seed";
        public const string Pulse = "qi-pulse";

        public static MirrorHeader Header(GameSession session) =>
            new MirrorHeader(session.Mirror.MirrorPower, session.Mirror.Cap, ActiveSeeds(session), session.Mirror.TalismanSeedCapacity);

        private static int ActiveSeeds(GameSession session) => session.Clan.LivingMembers.Count(m => m.HasTalismanSeed);

        public static IReadOnlyList<InterventionLine> Interventions(GameSession session)
        {
            int power = session.Mirror.MirrorPower;
            string Short(int cost) => power < cost ? $"Clair de Lune insuffisant ({power}/{cost})" : null;
            return new List<InterventionLine>
            {
                new InterventionLine(Shield, "Bouclier ancestral", MirrorSystem.AncestralShieldCost, "+30 % à la prochaine percée de Fondation (par une essence métallique)",
                    session.Mirror.ShieldRefusal()),
                new InterventionLine(Judgment, "Jugement du miroir", MirrorSystem.MirrorJudgmentCost, "la Lumière tue ou blesse un traître, ou un ancien ennemi perçu, selon les éclats",
                    Short(MirrorSystem.MirrorJudgmentCost)),
                new InterventionLine(Seed, "Graine de Sceau", MirrorSystem.TalismanSeedCost, "un mortel sans orifice peut cultiver",
                    ActiveSeeds(session) >= session.Mirror.TalismanSeedCapacity ? "le miroir n'en soutient pas davantage" : Short(MirrorSystem.TalismanSeedCost)),
                new InterventionLine(Pulse, "Pulsation de Qi", MirrorSystem.QiPulseCost, "rend du Qi et de la vitalité à un combattant",
                    "seulement en bataille"),
            };
        }

        /// <summary>The living members without a seed who are not known cultivators; the examined mortals first.</summary>
        public static IReadOnlyList<SeedCandidate> SeedCandidates(GameSession session)
        {
            bool full = ActiveSeeds(session) >= session.Mirror.TalismanSeedCapacity;
            bool weak = session.Mirror.MirrorPower < MirrorSystem.TalismanSeedCost;
            return session.Clan.LivingMembers
                .Where(m => !m.HasTalismanSeed && !(m.OrificeKnown && SpiritualOrificeRules.CanCultivate(m)))
                .OrderByDescending(m => m.OrificeKnown).ThenBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new SeedCandidate(m.ID, m.FullName, m.Age,
                    !m.OrificeKnown ? "orifice non examiné"
                    : full ? "le miroir est à sa capacité"
                    : weak ? "puissance insuffisante"
                    : null))
                .ToList();
        }

        /// <summary>
        /// Whom the Light could strike: the clan's members (a traitor) and the powers' elders the mirror perceives
        /// (AUDIT_LORE.md §3.1-3.2), each with what the Light would do, or why it cannot.
        /// </summary>
        public static IReadOnlyList<JudgmentTarget> JudgmentTargets(GameSession session)
        {
            var tier = session.Mirror.Tier;
            var members = session.Clan.LivingMembers
                .OrderBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new JudgmentTarget(m.ID, m.FullName, RankCatalog.DisplayName(m), // an unexamined orifice stays unexamined
                    m.FromFaction == null ? null : $"venu de {m.FromFaction}")
                    { Effect = MirrorTiers.Effect(tier, m.Realm), Refusal = session.Mirror.JudgmentRefusal(m) });
            var elders = session.Light.ElderTargets()
                .OrderBy(t => t.Power, StringComparer.Ordinal).ThenBy(t => t.ElderName, StringComparer.Ordinal)
                .Select(t => new JudgmentTarget(t.ElderId, t.ElderName, RankCatalog.RealmName(t.Realm), $"ancien de {t.Power}")
                    { Power = t.Power, Effect = t.Effect, Refusal = session.Mirror.PayRefusal(MirrorSystem.MirrorJudgmentCost) });
            return members.Concat(elders).ToList();
        }

        public static IReadOnlyList<FragmentLine> Fragments(GameSession session) =>
            session.Deduction.Fragments
                .Select(f => new FragmentLine(f.ID, f.Name, WorldMapView.ElementLabel(f.Element), f.Quality))
                .ToList();

        /// <summary>What deducing these fragments would cost the mirror, or why it cannot.</summary>
        public static DeductionPreviewLine DeductionPreview(GameSession session, IReadOnlyList<string> fragmentIds)
        {
            int count = fragmentIds?.Distinct().Count(id => session.Deduction.Fragments.Any(f => f.ID == id)) ?? 0;
            int cost = count * DeductionEngine.PowerPerFragment;
            if (count < DeductionEngine.MinFragments || count > DeductionEngine.MaxFragments)
                return new DeductionPreviewLine(cost, $"une déduction demande de {DeductionEngine.MinFragments} à {DeductionEngine.MaxFragments} fragments");
            if (session.Mirror.MirrorPower < cost)
                return new DeductionPreviewLine(cost, $"Clair de Lune insuffisant ({session.Mirror.MirrorPower}/{cost})");
            return new DeductionPreviewLine(cost, null);
        }
    }
}
