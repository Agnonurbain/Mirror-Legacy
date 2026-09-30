using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The ink portraits (Shuimo): five ages by the share of life lived (a cultivator of a hundred years out of three
    /// hundred still looks a grown adult), man or woman, the robe tinted by the member's element, an aura by realm for
    /// those who cultivate, the dead and the departed faded, and a variation of its own that never changes.
    /// </summary>
    [TestFixture]
    public class PortraitViewTests
    {
        private static CharacterData Member(int age, int lifespan, bool male = true, CultivationRealm realm = CultivationRealm.QiRefinement)
        {
            var m = Fixtures.Cultivator(isMale: male, age: age, realm: realm);
            m.MaxLifespan = lifespan;
            m.ID = $"p-{age}-{lifespan}";
            return m;
        }

        [TestCase(8, 70, AgeBracket.Child)]
        [TestCase(17, 70, AgeBracket.Youth)]
        [TestCase(28, 70, AgeBracket.Adult)]
        [TestCase(45, 70, AgeBracket.Mature)]
        [TestCase(62, 70, AgeBracket.Elder)]
        [TestCase(100, 300, AgeBracket.Adult)]
        public void TheAge_IsTheShareOfLifeLived(int age, int lifespan, AgeBracket expected)
        {
            Assert.AreEqual(expected, PortraitView.Of(Member(age, lifespan)).Age);
        }

        [Test]
        public void APortrait_ShowsTheElement_TheAura_AndStaysTheSame()
        {
            var member = Member(30, 120, male: false, realm: CultivationRealm.Foundation);
            member.Affinity = Element.Water;
            var portrait = PortraitView.Of(member);
            Assert.IsFalse(portrait.IsMale);
            Assert.AreEqual(Element.Water, portrait.Element);
            Assert.AreEqual((int)CultivationRealm.Foundation + 1, portrait.Aura);
            Assert.AreEqual(portrait, PortraitView.Of(member), "the same portrait every time");

            var mortal = Fixtures.Mortal();
            mortal.ID = "mortal";
            Assert.AreEqual(0, PortraitView.Of(mortal).Aura, "no aura without cultivation");
        }

        [Test]
        public void TheDead_AreFaded()
        {
            var member = Member(30, 120);
            member.IsAlive = false;
            Assert.IsTrue(PortraitView.Of(member).Faded);
        }

        [Test]
        public void ASessionFindsAPortrait_ByTheMembersId()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var patriarch = s.Clan.GetPatriarch();
            Assert.AreEqual(PortraitView.Of(patriarch), PortraitView.For(s, patriarch.ID));
            Assert.IsNull(PortraitView.For(s, "nobody"));
        }
    }
}
