using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>Speed of cultivating in Embryonic Breathing without a manual: the common breathing.</summary>
        public const double CommonBreathingSpeed = 1.0;

        /// <summary>
        /// How fast the method cultivates in a realm: its grade's speed, times its flaw in that realm.
        /// Without a method covering the realm, one only breathes (Embryonic) or does not progress at all.
        /// </summary>
        /// <param name="speedByGrade">One speed per grade, 1 to 7 (balance.json).</param>
        public static double CultivationSpeed(TechniqueData method, CultivationRealm realm, IReadOnlyList<double> speedByGrade)
        {
            if (!Covers(method, realm))
                return realm == CultivationRealm.Embryonic ? CommonBreathingSpeed : 0.0;

            double speed = speedByGrade[Math.Clamp(method.Grade, MinGrade, MaxGrade) - 1];
            if (method.Flaws?.SpeedByRealm != null && method.Flaws.SpeedByRealm.TryGetValue(realm, out double factor))
                speed *= factor;
            return speed;
        }

        /// <summary>
        /// The highest realm the method leads to: its supreme realm, the Golden Core for a grade 6 that
        /// « aims » at it, everything for a grade 7+ (LORE.md §2.2).
        /// </summary>
        public static CultivationRealm Ceiling(TechniqueData method)
        {
            if (method.Grade >= MaxGrade) return CultivationRealm.GoldenImmortal;
            var supreme = SupremeRealm(method);
            if (method.Grade == MaxGrade - 1 && supreme < CultivationRealm.GoldenCore) return CultivationRealm.GoldenCore;
            return supreme;
        }

        /// <summary>
        /// True when the method lets its practitioner advance from their realm to the target one: it covers
        /// the current realm (Embryonic Breathing needs no manual) and leads at least as far as the target.
        /// Entering Qi Cultivation has its own rule (<see cref="CanEnterQiCultivation"/>).
        /// </summary>
        public static bool AllowsAdvance(TechniqueData method, CultivationRealm current, CultivationRealm target)
        {
            if (target == current)
                return current == CultivationRealm.Embryonic || Covers(method, current);
            if (target == CultivationRealm.PurpleMansion && !HasPurpleMansionSecret(method))
                return false; // §5.3.4: without the secret technique of ascent, one stops at the Foundation's apogee
            return Covers(method, current) && Ceiling(method) >= target;
        }

        /// <summary>The secret technique of ascent to the Purple Mansion: from grade 5, unless the lore says otherwise (§2.3).</summary>
        public static bool HasPurpleMansionSecret(TechniqueData method) =>
            method.HasPurpleMansionSecret ?? method.Grade >= 5;

        /// <summary>Portions of the Qi absorbed to enter Qi Cultivation: none for a Qi found everywhere.</summary>
        public static int QiPortionsToEnter(QiDefinition qi) => qi.Ubiquitous ? 0 : 1;

        /// <summary>
        /// Entering Qi Cultivation (LORE.md §5.2): absorb a spiritual Qi and cultivate it with the matching
        /// method, so the method must cover Qi Cultivation and the clan must hold enough of its Qi.
        /// </summary>
        public static bool CanEnterQiCultivation(TechniqueData method, QiDefinition qi, int portions) =>
            Covers(method, CultivationRealm.QiRefinement)
            && qi != null
            && qi.Id == method.RequiredQiId
            && portions >= QiPortionsToEnter(qi);

        /// <summary>
        /// Whether a member may take up a cultivation method: a breathing member may practise a breathing
        /// method or prepare a Qi method; a Qi cultivator or beyond needs one covering their realm and built
        /// on the Qi they absorbed (Qi are never interchangeable, §2.5).
        /// </summary>
        public static bool CanPractise(CharacterData member, TechniqueData method)
        {
            if (method == null || method.Kind != TechniqueKind.Cultivation) return false;
            if (!SpiritualOrificeRules.CanCultivate(member)) return false; // a mortal cultivates nothing (§4)
            if (member.Realm == CultivationRealm.Embryonic)
                return Covers(method, CultivationRealm.Embryonic) || Covers(method, CultivationRealm.QiRefinement);
            return Covers(method, member.Realm) && (member.QiId == null || method.RequiredQiId == member.QiId);
        }

        /// <summary>A lifespan shortened by the method's flaw; an unbounded one stays unbounded.</summary>
        public static int LifespanWithMethod(int lifespan, TechniqueData method)
        {
            double factor = method?.Flaws?.LifespanFactor ?? 1.0;
            if (lifespan >= PowerLadder.Unbounded || factor >= 1.0) return lifespan;
            return (int)Math.Floor(lifespan * factor);
        }

        /// <summary>A practitioner of this technique is powerless against one practising the technique it is countered by.</summary>
        public static bool IsPowerlessAgainst(TechniqueData technique, string opponentMethodId) =>
            technique?.Flaws?.CounteredById != null && technique.Flaws.CounteredById == opponentMethodId;

        /// <summary>
        /// The realm from which an art of this grade can be wielded, following the grade table (§2.2): the arts
        /// of a grade 3-4 manual belong to Qi cultivators, those of a grade 5-6 one to the Foundation.
        /// </summary>
        public static CultivationRealm ArtRequiredRealm(int grade)
        {
            if (grade <= 2) return CultivationRealm.Embryonic;
            if (grade <= 4) return CultivationRealm.QiRefinement;
            if (grade <= 6) return CultivationRealm.Foundation;
            return CultivationRealm.PurpleMansion;
        }

        /// <summary>Grade from which a movement art lends two steps instead of one.</summary>
        public const int GreatMovementArtGrade = 5;

        /// <summary>Extra steps a movement art lends in battle: one, two from grade 5.</summary>
        public static int MovementArtSteps(int grade) => grade >= GreatMovementArtGrade ? 2 : 1;

        /// <summary>Fragments from which a deduction completes a technique beyond their average quality.</summary>
        public const int CompleteDeductionFragments = 4;

        /// <summary>
        /// The grade the mirror deduces from fragments of quality 1-5: their average quality, one more when
        /// four or more fragments complete each other, at most 6; a grade 7+ only from five divine fragments
        /// (a single such manual is known, §2.2).
        /// </summary>
        public static int DeductionGrade(IReadOnlyList<int> qualities)
        {
            if (qualities.Count == 5 && qualities.All(q => q >= 5)) return MaxGrade;

            int grade = (int)Math.Floor(qualities.Average());
            if (qualities.Count >= CompleteDeductionFragments) grade++;
            return Math.Clamp(grade, MinGrade, MaxGrade - 1);
        }
    }
}
