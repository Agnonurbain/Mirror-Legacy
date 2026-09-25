using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>Mental stability 0-100: modifiers, grief and breakthrough reactions.</summary>
    [TestFixture]
    public class MentalStabilitySystemTests
    {
        private GameContext ctx;
        private ClanManager clan;
        private MentalStabilitySystem stability;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            clan = new ClanManager(ctx, "Mo");
            stability = new MentalStabilitySystem(ctx, clan);
        }

        private static CharacterData WithStability(int value) => new CharacterData { MentalStability = value };

        [TestCase(50, 20, 70)]
        [TestCase(50, -30, 20)]
        [TestCase(10, -50, 0)]
        [TestCase(90, 50, 100)]
        [TestCase(70, 0, 70)]
        public void ApplyModifier_AddsAndClampsToZeroHundred(int start, int amount, int expected)
        {
            var c = WithStability(start);
            stability.ApplyModifier(c, amount);
            Assert.AreEqual(expected, c.MentalStability);
        }

        [Test]
        public void ApplyModifier_IgnoresTheDead()
        {
            var c = WithStability(50);
            c.IsAlive = false;
            stability.ApplyModifier(c, 20);
            Assert.AreEqual(50, c.MentalStability);
        }

        [Test]
        public void Death_GrievesTheCloseRelatives()
        {
            var father = Fixtures.Cultivator();
            var son = Fixtures.Cultivator();
            son.FatherID = father.ID;
            clan.AddMember(father);
            clan.AddMember(son);
            int before = son.MentalStability;

            clan.Kill(father, DeathCause.OldAge);

            Assert.AreEqual(before - MentalStabilitySystem.GriefPenalty, son.MentalStability);
        }

        [Test]
        public void Death_LeavesUnrelatedMembersUntouched()
        {
            var deceased = Fixtures.Cultivator();
            var stranger = Fixtures.Cultivator();
            clan.AddMember(deceased);
            clan.AddMember(stranger);
            int before = stranger.MentalStability;
            clan.Kill(deceased, DeathCause.OldAge);
            Assert.AreEqual(before, stranger.MentalStability);
        }

        [Test]
        public void BreakthroughSuccess_Steadies()
        {
            var c = WithStability(50);
            ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.Foundation);
            Assert.AreEqual(60, c.MentalStability);
        }

        [Test]
        public void BreakthroughFailure_Shakes()
        {
            var c = WithStability(50);
            ctx.Events.TriggerBreakthroughFailed(c);
            Assert.AreEqual(40, c.MentalStability);
        }
    }
}
