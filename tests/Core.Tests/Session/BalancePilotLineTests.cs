using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The pilot keeps the orifice in the line (user decisions 2026-10-03, against the bearers' extinction): an heir first —
    /// a childless bearer holds back a deadly trial while its odds are poor, save at the end of its life; bearers are wed to
    /// bearers first; and while the bearers are few, the mirror's Talisman Seed goes to the most gifted mortal child.
    /// </summary>
    [TestFixture]
    public class BalancePilotLineTests
    {
        private static GameSession Session()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            return s;
        }

        private static CharacterData AtTheWall(GameSession s, int age = 40)
        {
            var m = Fixtures.Cultivator(age: age, realm: CultivationRealm.QiRefinement, stage: 9);
            m.SpiritualRoot = 10; // poor odds
            m.MaxLifespan = 120;
            m.SpouseID = null;
            s.Clan.AddMember(m);
            return m;
        }

        [Test]
        public void AChildlessBearer_HoldsADeadlyTrial_WhileItsOddsArePoor()
        {
            var s = Session();
            var m = AtTheWall(s);
            Assume.That(s.Breakthroughs.CalculateSuccessRate(m), Is.LessThan(60));
            BalanceRun.Act(s);
            Assert.IsTrue(m.HoldsTrial, "an heir first");
        }

        [Test]
        public void ABearerWithAChild_DaresItsTrial()
        {
            var s = Session();
            var m = AtTheWall(s);
            var child = Fixtures.Mortal(age: 2);
            child.FatherID = m.ID;
            s.Clan.AddMember(child);
            BalanceRun.Act(s);
            Assert.IsFalse(m.HoldsTrial);
        }

        [Test]
        public void AtTheEndOfItsLife_ABearerDaresAnyway()
        {
            var s = Session();
            var m = AtTheWall(s, age: 115);
            BalanceRun.Act(s);
            Assert.IsFalse(m.HoldsTrial);
        }

        [Test]
        public void BearersAreWedToBearersFirst()
        {
            var s = Session();
            foreach (var x in s.Clan.LivingMembers.Where(x => x.SpouseID == null && x.Age >= 16).ToList()) x.SpouseID = "taken"; // nobody else free
            var seeker = Fixtures.Cultivator(isMale: true, age: 20);
            var seeded = Fixtures.Mortal(isMale: false, age: 20);
            seeded.HasTalismanSeed = true;
            var bearer = Fixtures.Cultivator(isMale: false, age: 20);
            s.Clan.AddMember(seeker);
            s.Clan.AddMember(seeded);
            s.Clan.AddMember(bearer);
            BalanceRun.Act(s);
            Assert.AreEqual(bearer.ID, seeker.SpouseID, "an orifice on both sides: half the children bear it");
        }

        [Test]
        public void WhileTheBearersAreFew_TheSeedGoesToTheMostGiftedChild()
        {
            var s = Session();
            foreach (var x in s.Clan.LivingMembers.Where(x => x.HasSpiritualOrifice).ToList()) x.HasSpiritualOrifice = false;
            var plain = Fixtures.Mortal(age: 11);
            plain.SpiritualRoot = 20;
            var gifted = Fixtures.Mortal(age: 12);
            gifted.SpiritualRoot = 90;
            s.Clan.AddMember(plain);
            s.Clan.AddMember(gifted);
            BalanceRun.Act(s);
            Assert.IsTrue(gifted.HasTalismanSeed);
            Assert.IsFalse(plain.HasTalismanSeed);
        }

        [Test]
        public void WhileTheBearersAreFew_ThePilotRisksNoDeadlyOperation()
        {
            var s = Session();
            var ruins = s.Context.Content.Shards.First(x => x.Source == ShardSource.Ruins).Id;
            s.Shards.RestoreRuins(new[] { ruins });
            foreach (var x in s.Clan.LivingMembers.Where(x => x.HasSpiritualOrifice).Skip(3).ToList()) x.HasSpiritualOrifice = false; // a thin line
            foreach (var x in s.Clan.LivingMembers.Where(x => x.HasSpiritualOrifice)) { x.Realm = CultivationRealm.Foundation; x.RealmStage = 2; }
            BalanceRun.Act(s);
            Assert.IsFalse(s.Clan.LivingMembers.Any(m => m.LastOperationYear == s.Clock.Year), "no expedition, no theft, no hunt: an heir first");
        }
    }
}
