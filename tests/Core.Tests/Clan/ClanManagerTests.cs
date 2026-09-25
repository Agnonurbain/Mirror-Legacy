using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>
    /// The clan roster: joining, dying (one entry point for every cause), ascending, succession, births.
    /// </summary>
    [TestFixture]
    public class ClanManagerTests
    {
        private GameContext ctx;
        private ClanManager clan;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            clan = new ClanManager(ctx, "Mo");
        }

        [Test]
        public void AddMember_RegistersTheMemberAmongTheLiving()
        {
            var member = Fixtures.Cultivator();
            clan.AddMember(member);
            Assert.IsTrue(clan.LivingMembers.Contains(member) && clan.Registry.FindById(member.ID) == member);
        }

        [Test]
        public void AddMember_RaisesCharacterBorn()
        {
            CharacterData joined = null;
            ctx.Events.OnCharacterBorn += c => joined = c;
            var member = Fixtures.Cultivator();
            clan.AddMember(member);
            Assert.AreSame(member, joined);
        }

        [Test]
        public void Kill_MarksTheMemberDeadWithItsCause()
        {
            var member = Fixtures.Cultivator();
            clan.AddMember(member);
            clan.Kill(member, DeathCause.OldAge);
            Assert.IsTrue(!member.IsAlive && member.CauseOfDeath == DeathCause.OldAge && !clan.LivingMembers.Contains(member));
        }

        [Test]
        public void Kill_RaisesCharacterDied_AfterTheRosterIsUpdated()
        {
            var member = Fixtures.Cultivator();
            clan.AddMember(member);
            bool stillListedWhenNotified = true;
            ctx.Events.OnCharacterDied += (c, cause) => stillListedWhenNotified = clan.LivingMembers.Contains(c);
            clan.Kill(member, DeathCause.Combat);
            Assert.IsFalse(stillListedWhenNotified);
        }

        [Test]
        public void Kill_IgnoresTheAlreadyDead()
        {
            var member = Fixtures.Cultivator();
            clan.AddMember(member);
            int deaths = 0;
            ctx.Events.OnCharacterDied += (c, cause) => deaths++;
            clan.Kill(member, DeathCause.Combat);
            clan.Kill(member, DeathCause.Illness);
            Assert.IsTrue(deaths == 1 && member.CauseOfDeath == DeathCause.Combat);
        }

        [Test]
        public void Kill_ElectsTheHighestRealmThenOldest_WhenThePatriarchDies()
        {
            var patriarch = Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation);
            var youngFoundation = Fixtures.Cultivator(age: 30, realm: CultivationRealm.Foundation);
            var oldFoundation = Fixtures.Cultivator(age: 50, realm: CultivationRealm.Foundation);
            var elderQi = Fixtures.Cultivator(age: 90, realm: CultivationRealm.QiRefinement);
            foreach (var m in new[] { patriarch, youngFoundation, oldFoundation, elderQi }) clan.AddMember(m);
            clan.AppointPatriarch(patriarch);

            clan.Kill(patriarch, DeathCause.OldAge);

            Assert.AreEqual(oldFoundation.ID, clan.PatriarchID);
        }

        [Test]
        public void Kill_PrefersTheHigherStage_WithinTheSameRealm()
        {
            var patriarch = Fixtures.Cultivator(age: 60);
            var early = Fixtures.Cultivator(age: 50, realm: CultivationRealm.QiRefinement, stage: 2);
            var late = Fixtures.Cultivator(age: 30, realm: CultivationRealm.QiRefinement, stage: 7);
            foreach (var m in new[] { patriarch, early, late }) clan.AddMember(m);
            clan.AppointPatriarch(patriarch);
            clan.Kill(patriarch, DeathCause.OldAge);
            Assert.AreEqual(late.ID, clan.PatriarchID);
        }

        [Test]
        public void Kill_PrefersTheStrongerRoot_BetweenEquals()
        {
            var patriarch = Fixtures.Cultivator(age: 60);
            var weak = Fixtures.Cultivator(age: 40);
            var gifted = Fixtures.Cultivator(age: 40);
            weak.SpiritualRoot = 20;
            gifted.SpiritualRoot = 80;
            foreach (var m in new[] { patriarch, weak, gifted }) clan.AddMember(m);
            clan.AppointPatriarch(patriarch);
            clan.Kill(patriarch, DeathCause.OldAge);
            Assert.AreEqual(gifted.ID, clan.PatriarchID);
        }

        [Test]
        public void Ascend_ElectsASuccessor_WhenThePatriarchAscends()
        {
            var patriarch = Fixtures.Cultivator(realm: CultivationRealm.DaoEmbryo);
            var heir = Fixtures.Cultivator();
            clan.AddMember(patriarch);
            clan.AddMember(heir);
            clan.AppointPatriarch(patriarch);
            clan.Ascend(patriarch);
            Assert.AreEqual(heir.ID, clan.PatriarchID);
        }

        [Test]
        public void Kill_LeavesNoPatriarch_WhenTheLastMemberDies()
        {
            var last = Fixtures.Cultivator();
            clan.AddMember(last);
            clan.AppointPatriarch(last);
            clan.Kill(last, DeathCause.OldAge);
            Assert.IsNull(clan.PatriarchID);
        }

        [Test]
        public void Ascend_LeavesTheMortalPlaneWithoutDying()
        {
            var ancestor = Fixtures.Cultivator(realm: CultivationRealm.DaoEmbryo);
            clan.AddMember(ancestor);
            int deaths = 0;
            CharacterData ascended = null;
            ctx.Events.OnCharacterDied += (c, cause) => deaths++;
            ctx.Events.OnAncestorAscended += c => ascended = c;

            clan.Ascend(ancestor);

            Assert.IsTrue(!ancestor.IsAlive && ancestor.CauseOfDeath == DeathCause.None && deaths == 0
                && ascended == ancestor && !clan.LivingMembers.Contains(ancestor));
        }

        [Test]
        public void GenerateChild_IsAnUnexaminedNewbornOfItsParents()
        {
            var father = Fixtures.Cultivator(isMale: true);
            var mother = Fixtures.Cultivator(isMale: false);
            var child = clan.GenerateChild(father, mother);
            Assert.IsTrue(child.Age == 0 && child.FatherID == father.ID && child.MotherID == mother.ID
                && child.LastName == "Mo" && !child.OrificeKnown && clan.LivingMembers.Contains(child));
        }

        [Test]
        public void GenerateChild_AvoidsTheFirstNameOfALivingMember()
        {
            // Every draw is 0.99: the child is a boy and a raw pick would land on the last name, already taken
            var sons = new ClanManager(Fixtures.Context(new FixedRandom(0.99)), "Mo");
            var male = Fixtures.Content.Names.Male;
            foreach (var name in male.Skip(1))
            {
                var elder = Fixtures.Cultivator(age: 40);
                elder.FirstName = name;
                sons.AddMember(elder);
            }

            var child = sons.GenerateChild(null, null);

            Assert.AreEqual(male.First(), child.FirstName);
        }

        [Test]
        public void GenerateChild_HasAMortalLifespan()
        {
            var child = clan.GenerateChild(Fixtures.Cultivator(), Fixtures.Cultivator(isMale: false));
            Assert.That(child.MaxLifespan, Is.InRange(60, 80));
        }

        [Test]
        public void GenerateChild_InheritsTheOrifice_WhenTheRollSucceeds()
        {
            var lucky = new ClanManager(Fixtures.Context(new FixedRandom(0.0)), "Mo");
            var child = lucky.GenerateChild(Fixtures.Cultivator(), Fixtures.Cultivator(isMale: false));
            Assert.IsTrue(child.HasSpiritualOrifice);
        }

        [Test]
        public void GenerateChild_IsMortal_WhenTheRollFails()
        {
            var unlucky = new ClanManager(Fixtures.Context(new FixedRandom(0.99)), "Mo");
            var child = unlucky.GenerateChild(Fixtures.Cultivator(), Fixtures.Cultivator(isMale: false));
            Assert.IsFalse(child.HasSpiritualOrifice);
        }

        [Test]
        public void ProcessAnnualBirths_GivesAChild_WhenTheCouplesRollSucceeds()
        {
            var fertile = new ClanManager(Fixtures.Context(new FixedRandom(0.0)), "Mo");
            Couple(fertile, motherAge: 25);
            Assert.AreEqual(1, fertile.ProcessAnnualBirths());
        }

        [TestCase(15)]
        [TestCase(46)]
        public void ProcessAnnualBirths_SkipsMothersOutsideSixteenToFortyFive(int motherAge)
        {
            var fertile = new ClanManager(Fixtures.Context(new FixedRandom(0.0)), "Mo");
            Couple(fertile, motherAge);
            Assert.AreEqual(0, fertile.ProcessAnnualBirths());
        }

        [Test]
        public void ExamineOrifices_RevealsNewborns_WhenASummitEyeCultivatorLives()
        {
            clan.AddMember(Fixtures.Cultivator());
            var newborn = new CharacterData { HasSpiritualOrifice = true };
            clan.AddMember(newborn);
            clan.ExamineOrifices();
            Assert.IsTrue(newborn.OrificeKnown);
        }

        [Test]
        public void Restore_RebuildsTheLivingFromTheRecords()
        {
            var alive = Fixtures.Cultivator();
            var dead = Fixtures.Cultivator();
            dead.IsAlive = false;
            clan.Restore(new List<CharacterData> { alive, dead }, alive.ID);
            Assert.IsTrue(clan.LivingMembers.Count == 1 && clan.Registry.Records.Count == 2 && clan.PatriarchID == alive.ID);
        }

        private static void Couple(ClanManager target, int motherAge)
        {
            var father = Fixtures.Cultivator(isMale: true, age: 30);
            var mother = Fixtures.Cultivator(isMale: false, age: motherAge);
            father.SpouseID = mother.ID;
            mother.SpouseID = father.ID;
            target.AddMember(father);
            target.AddMember(mother);
        }
    }
}
