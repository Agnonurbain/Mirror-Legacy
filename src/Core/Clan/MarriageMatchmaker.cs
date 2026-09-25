using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

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

        private const int OutsiderAgeSpread = 5;
        private const int OutsiderMinSpiritualRoot = 10;
        private const int OutsiderMaxSpiritualRoot = 50;

        /// <summary>
        /// A living, unmarried member between <see cref="MinMarriageAge"/> and <see cref="MaxSeekingAge"/>.
        /// </summary>
        public static bool IsEligible(CharacterData character)
        {
            return character != null
                && character.IsAlive
                && string.IsNullOrEmpty(character.SpouseID)
                && character.Age >= MinMarriageAge
                && character.Age <= MaxSeekingAge;
        }

        /// <summary>
        /// The eligible opposite-sex candidate closest in age who is not close kin, or null.
        /// </summary>
        public static CharacterData FindClanPartner(CharacterData seeker, IEnumerable<CharacterData> candidates, Func<string, CharacterData> findById)
        {
            return candidates
                .Where(c => c != seeker
                    && c.IsMale != seeker.IsMale
                    && IsEligible(c)
                    && !KinshipRules.AreCloseKin(seeker, c, KinshipRules.MarriageForbiddenGenerations, findById))
                .OrderBy(c => Math.Abs(c.Age - seeker.Age))
                .FirstOrDefault();
        }

        /// <summary>
        /// A wandering cultivator of the opposite sex, adult, with no parents in the clan; a commoner,
        /// so the orifice comes with the commoner odds.
        /// </summary>
        public static CharacterData CreateOutsiderSpouse(CharacterData member, string lastName, NamePools names, OrificeOdds odds, Random rng)
        {
            bool isMale = !member.IsMale;
            var firstNames = isMale ? names.Male : names.Female;
            int elementCount = Enum.GetValues(typeof(Element)).Length;

            return new CharacterData
            {
                FirstName = firstNames[rng.Next(firstNames.Count)],
                LastName = lastName,
                IsMale = isMale,
                Age = Math.Max(MinMarriageAge, member.Age + rng.Next(-OutsiderAgeSpread, OutsiderAgeSpread + 1)),
                MaxLifespan = SpiritualOrificeRules.MortalLifespan(rng.NextDouble()), // has never cultivated
                SpiritualRoot = rng.Next(OutsiderMinSpiritualRoot, OutsiderMaxSpiritualRoot + 1),
                Affinity = (Element)rng.Next(1, elementCount), // skip Element.None
                HasSpiritualOrifice = SpiritualOrificeRules.HasOrificeAtBirth(0, rng.NextDouble(), odds), // commoner odds
                Temperament = FoundationRules.RandomTemperament(rng),
                ID = rng.NextId() // seeded, for reproducible games
            };
        }

        /// <summary>
        /// Rolls <paramref name="chance"/> for each eligible member; a match is found inside the clan
        /// when possible, otherwise an outsider is created. Nobody is planned twice.
        /// </summary>
        public static List<MarriagePlan> PlanAnnualMarriages(IReadOnlyList<CharacterData> members, Func<string, CharacterData> findById,
            double chance, NamePools names, OrificeOdds odds, Random rng)
        {
            var plans = new List<MarriagePlan>();
            var matched = new HashSet<string>();

            foreach (var member in members)
            {
                if (matched.Contains(member.ID) || !IsEligible(member)) continue;
                if (rng.NextDouble() >= chance) continue;

                var partner = FindClanPartner(member, members.Where(c => !matched.Contains(c.ID)), findById);
                var spouse = partner ?? CreateOutsiderSpouse(member, PickFamilyName(names, rng), names, odds, rng);

                matched.Add(member.ID);
                matched.Add(spouse.ID);
                plans.Add(new MarriagePlan(member, spouse, partner == null));
            }

            return plans;
        }

        /// <summary>The surname of a wandering cultivator's family.</summary>
        public static string PickFamilyName(NamePools names, Random rng)
        {
            return names.OutsiderFamilies[rng.Next(names.OutsiderFamilies.Count)];
        }
    }
}
