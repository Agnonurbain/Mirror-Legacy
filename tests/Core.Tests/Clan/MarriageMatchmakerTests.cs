using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>
    /// Annual marriages keep the lineage alive: without them only the founding
    /// couple ever has children and the clan dies out after one generation.
    /// </summary>
    [TestFixture]
    public class MarriageMatchmakerTests
    {
        private Dictionary<string, CharacterData> registry;
        private Random rng;
        private static NamePools Names => Fixtures.Content.Names;
        private static OrificeOdds Odds => Fixtures.Content.Balance.OrificeOdds;

        [SetUp]
        public void SetUp()
        {
            registry = new Dictionary<string, CharacterData>();
            rng = new Random(42);
        }

        private CharacterData Person(bool isMale, int age, CharacterData father = null, CharacterData mother = null)
        {
            var person = new CharacterData { IsMale = isMale, Age = age, FatherID = father?.ID, MotherID = mother?.ID };
            registry[person.ID] = person;
            return person;
        }

        private CharacterData FindById(string id)
        {
            return registry.TryGetValue(id, out var person) ? person : null;
        }

        [Test]
        public void IsEligible_ReturnsTrue_WhenUnmarriedAdult()
        {
            Assert.IsTrue(MarriageMatchmaker.IsEligible(Person(true, 20)));
        }

        [Test]
        public void IsEligible_ReturnsFalse_WhenMinor()
        {
            Assert.IsFalse(MarriageMatchmaker.IsEligible(Person(true, 17)));
        }

        [Test]
        public void IsEligible_ReturnsFalse_WhenPastSeekingAge()
        {
            Assert.IsFalse(MarriageMatchmaker.IsEligible(Person(false, MarriageMatchmaker.MaxSeekingAge + 1)));
        }

        [Test]
        public void IsEligible_ReturnsFalse_WhenAlreadyMarried()
        {
            var person = Person(true, 25);
            person.SpouseID = Guid.NewGuid().ToString();
            Assert.IsFalse(MarriageMatchmaker.IsEligible(person));
        }

        [Test]
        public void IsEligible_ReturnsFalse_WhenDead()
        {
            var person = Person(true, 25);
            person.IsAlive = false;
            Assert.IsFalse(MarriageMatchmaker.IsEligible(person));
        }

        [Test]
        public void FindClanPartner_ReturnsUnrelatedOppositeSexAdult()
        {
            var seeker = Person(true, 22);
            var partner = Person(false, 21);
            var result = MarriageMatchmaker.FindClanPartner(seeker, new[] { seeker, partner }, FindById);
            Assert.AreSame(partner, result);
        }

        [Test]
        public void FindClanPartner_ReturnsNull_WhenOnlyCloseKin()
        {
            var father = Person(true, 50);
            var brother = Person(true, 22, father);
            var sister = Person(false, 20, father);
            Assert.IsNull(MarriageMatchmaker.FindClanPartner(brother, new[] { brother, sister }, FindById));
        }

        [Test]
        public void FindClanPartner_ReturnsNull_WhenOnlySameSex()
        {
            var seeker = Person(true, 22);
            Assert.IsNull(MarriageMatchmaker.FindClanPartner(seeker, new[] { seeker, Person(true, 23) }, FindById));
        }

        [Test]
        public void FindClanPartner_PrefersClosestAge()
        {
            var seeker = Person(true, 25);
            var older = Person(false, 38);
            var closest = Person(false, 24);
            var result = MarriageMatchmaker.FindClanPartner(seeker, new[] { seeker, older, closest }, FindById);
            Assert.AreSame(closest, result);
        }

        [Test]
        public void CreateOutsiderSpouse_HasOppositeSex()
        {
            var member = Person(false, 20);
            Assert.IsTrue(MarriageMatchmaker.CreateOutsiderSpouse(member, "Wang", Names, Odds, rng).IsMale);
        }

        [Test]
        public void CreateOutsiderSpouse_IsLivingAdultWithLifespan()
        {
            var spouse = MarriageMatchmaker.CreateOutsiderSpouse(Person(true, 18), "Wang", Names, Odds, rng);
            Assert.IsTrue(spouse.IsAlive && spouse.Age >= MarriageMatchmaker.MinMarriageAge && spouse.MaxLifespan > 0);
        }

        [Test]
        public void CreateOutsiderSpouse_UsesGivenFamilyNameAndHasNoClanParents()
        {
            var spouse = MarriageMatchmaker.CreateOutsiderSpouse(Person(true, 30), "Zhao", Names, Odds, rng);
            Assert.IsTrue(spouse.LastName == "Zhao" && spouse.FatherID == null && spouse.MotherID == null);
        }

        /// <summary>Every draw returns the same sample, so the orifice roll is predictable.</summary>
        private sealed class FixedRandom : Random
        {
            private readonly double sample;
            public FixedRandom(double sample) { this.sample = sample; }
            protected override double Sample() => sample;
        }

        [Test]
        public void CreateOutsiderSpouse_HasOrifice_WhenRollBeatsCommonerOdds()
        {
            var spouse = MarriageMatchmaker.CreateOutsiderSpouse(Person(true, 25), "Zhao", Names, Odds, new FixedRandom(0.0));
            Assert.IsTrue(spouse.HasSpiritualOrifice);
        }

        [Test]
        public void CreateOutsiderSpouse_IsMortalWithMortalLifespan_WhenRollFails()
        {
            var spouse = MarriageMatchmaker.CreateOutsiderSpouse(Person(true, 25), "Zhao", Names, Odds, new FixedRandom(0.99));
            Assert.IsTrue(!spouse.HasSpiritualOrifice && spouse.MaxLifespan >= 60 && spouse.MaxLifespan <= 80);
        }

        [Test]
        public void PlanAnnualMarriages_MarriesSiblingsToOutsiders_WhenChanceIsCertain()
        {
            var father = Person(true, 50);
            var brother = Person(true, 22, father);
            var sister = Person(false, 20, father);

            var plans = MarriageMatchmaker.PlanAnnualMarriages(new[] { brother, sister }, FindById, 1f, Names, Odds, rng);

            Assert.IsTrue(plans.Count == 2 && plans.All(p => p.SpouseIsOutsider && p.Spouse.IsMale != p.Member.IsMale));
        }

        [Test]
        public void PlanAnnualMarriages_PairsUnrelatedClanMembersOnce()
        {
            var man = Person(true, 24);
            var woman = Person(false, 23);

            var plans = MarriageMatchmaker.PlanAnnualMarriages(new[] { man, woman }, FindById, 1f, Names, Odds, rng);

            Assert.IsTrue(plans.Count == 1 && !plans[0].SpouseIsOutsider);
        }

        [Test]
        public void PlanAnnualMarriages_ReturnsEmpty_WhenChanceIsZero()
        {
            var plans = MarriageMatchmaker.PlanAnnualMarriages(new[] { Person(true, 24), Person(false, 23) }, FindById, 0f, Names, Odds, rng);
            Assert.IsEmpty(plans);
        }

        [Test]
        public void PlanAnnualMarriages_SkipsIneligibleMembers()
        {
            var married = Person(true, 30);
            married.SpouseID = Guid.NewGuid().ToString();

            var plans = MarriageMatchmaker.PlanAnnualMarriages(new[] { Person(false, 15), married }, FindById, 1f, Names, Odds, rng);

            Assert.IsEmpty(plans);
        }
    }
}
