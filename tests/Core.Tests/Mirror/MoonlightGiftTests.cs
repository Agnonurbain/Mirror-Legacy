using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The Supreme Yin Moonlight given to the clan (📚; LORE.md §11.5; audit §3.7, the user's decision 2026-10-04): a portion
    /// of a Supreme Yin Qi sealed for the clan, or a member's cultivation nourished; a spy in the clan notices so rare a Qi.
    /// </summary>
    [TestFixture]
    public class MoonlightGiftTests
    {
        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Mirror.Restore(100, 0);
            return s;
        }

        [Test]
        public void TheMirror_SealsAPortionOfSupremeYinQi_ForTheClan()
        {
            var s = Session();
            var qi = s.Context.Content.Balance.MirrorTiers.GiftQiId;
            int before = s.Resources.QiPortions(qi), power = s.Mirror.MirrorPower;
            Assert.IsNull(s.Moonlight.SealQi());
            Assert.AreEqual(before + 1, s.Resources.QiPortions(qi));
            Assert.AreEqual(power - s.Context.Content.Balance.MirrorTiers.GiftCost, s.Mirror.MirrorPower);
        }

        [Test]
        public void TheMoonlight_NourishesAMembersCultivation()
        {
            var s = Session();
            var member = s.Clan.LivingMembers.First(m => MirrorChronicles.Characters.SpiritualOrificeRules.CanCultivate(m));
            int xp = member.CultivationXP;
            Assert.IsNull(s.Moonlight.Nourish(member.ID));
            Assert.Greater(member.CultivationXP, xp);
        }

        [Test]
        public void ASpyInTheClan_NoticesSoRareAQi()
        {
            var s = Session();
            var power = s.Factions.Factions.First();
            var spy = Fixtures.Mortal(age: 30);
            spy.SpyFor = power.Name;
            s.Clan.AddMember(spy);
            int clues = s.Suspicion.MirrorClues(power.Name);
            s.Moonlight.SealQi();
            Assert.Greater(s.Suspicion.MirrorClues(power.Name), clues);
        }

        [Test]
        public void WithoutEnoughMoonlight_NothingIsGiven()
        {
            var s = Session();
            s.Mirror.Restore(0, 0);
            StringAssert.Contains("Clair de Lune", s.Moonlight.SealQi());
        }
    }
}
