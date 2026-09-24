using System;
using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// One marriage decided for this year. The spouse is an outsider when it is not yet a clan member.
    /// </summary>
    public readonly struct MarriagePlan
    {
        public CharacterData Member { get; }
        public CharacterData Spouse { get; }
        public bool SpouseIsOutsider { get; }

        public MarriagePlan(CharacterData member, CharacterData spouse, bool spouseIsOutsider)
        {
            Member = member;
            Spouse = spouse;
            SpouseIsOutsider = spouseIsOutsider;
        }
    }

    /// <summary>
    /// Pure matchmaking rules for the annual marriages that keep the lineage alive.
    /// </summary>
    public static class MarriageMatchmaker
    {
        public const int MinMarriageAge = 18;
        public const int MaxSeekingAge = 40;
        public const float AnnualMarriageChance = 0.3f;

        public static bool IsEligible(CharacterData character) => false; // TDD stub

        public static CharacterData FindClanPartner(CharacterData seeker, IEnumerable<CharacterData> candidates, Func<string, CharacterData> findById) => null; // TDD stub

        public static CharacterData CreateOutsiderSpouse(CharacterData member, string lastName, Random rng) => new CharacterData(); // TDD stub

        public static List<MarriagePlan> PlanAnnualMarriages(IReadOnlyList<CharacterData> members, Func<string, CharacterData> findById, float chance, Random rng) => new List<MarriagePlan>(); // TDD stub
    }
}
