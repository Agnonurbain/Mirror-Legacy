using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>
    /// A cultivator's fertility (user decision 2026-10-03, against the orifice bearers' extinction): the motherhood window
    /// lengthens with the mother's realm, but the higher a parent's realm, the rarer a child each year. A member may hold
    /// back its trial — the Foundation's wall, the Purple Mansion's ascent — and does not attempt it meanwhile.
    /// </summary>
    [TestFixture]
    public class FertilityTests
    {
        private static GameSession Session()
        {
            var b = Fixtures.QuietContent.Balance;
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent with { Balance = b with { AnnualBirthChance = 1.0 } } });
        }

        private static (CharacterData Father, CharacterData Mother) Couple(GameSession s, int motherAge, CultivationRealm realm)
        {
            var father = Fixtures.Cultivator(isMale: true, age: motherAge, realm: realm);
            var mother = Fixtures.Cultivator(isMale: false, age: motherAge, realm: realm);
            if (realm == CultivationRealm.Embryonic) { father.HasSpiritualOrifice = mother.HasSpiritualOrifice = false; }
            father.SpouseID = mother.ID;
            mother.SpouseID = father.ID;
            s.Clan.AddMember(father);
            s.Clan.AddMember(mother);
            return (father, mother);
        }

        private static int ChildrenOf(GameSession s, CharacterData mother) => s.Clan.Registry.Records.Count(r => r.MotherID == mother.ID);

        [Test]
        public void AMortalMother_StopsAt45_AFoundationMotherLater()
        {
            var s = Session();
            var (_, mortal) = Couple(s, 50, CultivationRealm.Embryonic);
            var (_, foundation) = Couple(s, 80, CultivationRealm.Foundation);
            for (int i = 0; i < 40; i++) s.Clan.ProcessAnnualBirths();
            Assert.AreEqual(0, ChildrenOf(s, mortal));
            Assert.Greater(ChildrenOf(s, foundation), 0, "the window lengthens with the realm");
        }

        [Test]
        public void TheHigherTheRealm_TheRarerAChild()
        {
            var s = Session();
            var (_, qi) = Couple(s, 30, CultivationRealm.QiRefinement);
            var (_, mansion) = Couple(s, 30, CultivationRealm.PurpleMansion);
            for (int i = 0; i < 30; i++) s.Clan.ProcessAnnualBirths();
            Assert.Greater(ChildrenOf(s, qi), ChildrenOf(s, mansion));
        }

        [Test]
        public void AMemberHoldingItsTrial_DoesNotAttemptIt()
        {
            var s = Session();
            CharacterData Ready(bool holds)
            {
                var m = Fixtures.Cultivator(age: 40, realm: CultivationRealm.QiRefinement, stage: 9); // at the Foundation's wall
                m.CultivationXP = 1_000_000;
                m.HoldsTrial = holds;
                s.Clan.AddMember(m);
                return m;
            }
            s.Resources.AddQi(Fixtures.ClanQi, 10); // the wall's Qi
            var holding = Ready(true);
            Ready(false);
            Assert.AreEqual(1, s.Breakthroughs.ProcessBreakthroughPhase(), "only the one who does not hold back");
            Assert.AreEqual(CultivationRealm.QiRefinement, holding.Realm);
        }

        [Test]
        public void ACultivator_SeeksASpouse_LongerThanAMortal()
        {
            var mortal = Fixtures.Mortal(age: 50);
            var cultivator = Fixtures.Cultivator(age: 60, realm: CultivationRealm.QiRefinement);
            Assert.IsFalse(MirrorChronicles.Clan.MarriageMatchmaker.IsEligible(mortal));
            Assert.IsTrue(MirrorChronicles.Clan.MarriageMatchmaker.IsEligible(cultivator), "a widowed bearer weds again");
        }
    }
}
