using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// A reborn True Monarch bends lesser minds (audit §1.8, the user's decision 2026-10-03; LORE.md §5.4: a Purple Mansion
    /// resists it). A power's returned True Monarch bends a member below the Purple Mansion into its unwitting spy; the clan
    /// sees an absent gaze, the mirror sounds and breaks the spell; the Purple Mansion frees itself. The clan's returned
    /// ancestor bends a power's lesser elder in turn.
    /// </summary>
    [TestFixture]
    public class EnthrallmentTests
    {
        private static GameSession Session(double yearly = 1.0)
        {
            var b = Fixtures.QuietContent.Balance;
            var content = Fixtures.QuietContent with { Balance = b with { Enthrallment = b.Enthrallment with { YearlyChance = yearly } } };
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
        }

        private static FactionData WithAReturnedMonarch(GameSession s)
        {
            var power = s.Factions.Factions.First();
            power.Elders.Add(new FactionElder { Id = "r", Name = "revenu", Realm = CultivationRealm.GoldenCore, Stage = 1, MaxLifespan = 1000, Reborn = true });
            power.HighestRealm = CultivationRealm.GoldenCore;
            return power;
        }

        [Test]
        public void AReturnedTrueMonarch_BendsALesserMember_IntoItsSpy()
        {
            var s = Session();
            var power = WithAReturnedMonarch(s);
            s.Enthrallment.ProcessYear();
            var bent = s.Clan.LivingMembers.SingleOrDefault(m => m.Enthralled);
            Assert.IsNotNull(bent);
            Assert.AreEqual(power.Name, bent.SpyFor);
            Assert.IsTrue(OperationsView.Spouses(s).Any(l => l.Id == bent.ID && l.From == null), "the clan sees an absent gaze");
        }

        [Test]
        public void APurpleMansion_ResistsTheSpell()
        {
            var s = Session();
            WithAReturnedMonarch(s);
            foreach (var m in s.Clan.LivingMembers) m.Realm = CultivationRealm.PurpleMansion;
            s.Enthrallment.ProcessYear();
            Assert.IsFalse(s.Clan.LivingMembers.Any(m => m.Enthralled));
        }

        [Test]
        public void WithoutAReturnedMonarch_NoMindIsBent()
        {
            var s = Session();
            s.Enthrallment.ProcessYear();
            Assert.IsFalse(s.Clan.LivingMembers.Any(m => m.Enthralled));
        }

        [Test]
        public void TheMirror_SoundsAndBreaksTheSpell()
        {
            var s = Session();
            WithAReturnedMonarch(s);
            s.Enthrallment.ProcessYear();
            var bent = s.Clan.LivingMembers.Single(m => m.Enthralled);
            StringAssert.Contains("aucun envoûtement", s.Enthrallment.Break(bent.ID), "it must be sounded first");
            s.Mirror.AddPower(1000);
            s.Intrigues.Unmask(bent.ID);
            Assert.IsTrue(OperationsView.Spouses(s).Single(l => l.Id == bent.ID).Enthralled);
            Assert.IsNull(s.Enthrallment.Break(bent.ID));
            Assert.IsFalse(bent.Enthralled);
            Assert.IsNull(bent.SpyFor);
        }

        [Test]
        public void RisingToThePurpleMansion_ShakesTheSpellOff()
        {
            var s = Session();
            WithAReturnedMonarch(s);
            s.Enthrallment.ProcessYear();
            var bent = s.Clan.LivingMembers.Single(m => m.Enthralled);
            bent.Realm = CultivationRealm.PurpleMansion;
            s.Factions.Factions.First().Elders.Clear(); // no new spell this year
            s.Enthrallment.ProcessYear();
            Assert.IsFalse(bent.Enthralled);
        }

        [Test]
        public void TheClansReturnedAncestor_BendsAPowersLesserElder()
        {
            var b = Fixtures.QuietContent.Balance;
            var content = Fixtures.QuietContent with { Balance = b with { Enthrallment = b.Enthrallment with { BendBase = 1.0, SeenChance = 0 } } };
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
            var power = s.Factions.Factions.First(f => f.Elders.Any(e => e.Realm < CultivationRealm.PurpleMansion));
            var ordinary = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            s.Clan.AddMember(ordinary);
            StringAssert.Contains("ancêtre revenu", s.Enthrallment.Bend(ordinary.ID, power.Name));
            var ancestor = Fixtures.Cultivator(age: 40, realm: CultivationRealm.GoldenCore);
            ancestor.RebornFrom = "Mo l'Ancien";
            s.Clan.AddMember(ancestor);
            Assert.IsNull(s.Enthrallment.Bend(ancestor.ID, power.Name));
            Assert.IsTrue(power.Elders.Any(e => e.ThrallOfClan));
        }
    }
}
