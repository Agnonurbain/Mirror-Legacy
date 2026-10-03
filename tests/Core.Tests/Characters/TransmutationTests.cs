using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Transmutation (LORE.md §5.5.1, R8; L4e, user decision 2026-10-03): a Realization's holder tries to move to the
    /// free Realization of another lineage of the same Virtue. It fails, and the branch breaks — its foundation corrupts, it
    /// becomes a Metal Essence Demon (an Orthodox Wood patriarch, locked by his Life ability, tried towards the Gathered
    /// Wood). From an orthodox position the attempt is the more perilous.
    /// </summary>
    [TestFixture]
    public class TransmutationTests
    {
        private static GameSession Session(int chance)
        {
            var b = Fixtures.QuietContent.Balance;
            var content = Fixtures.QuietContent with { Balance = b with { GoldenCore = b.GoldenCore with { TransmutationChance = chance, AxiomPenalty = 0 } } };
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
        }

        private static void Free(GameSession s, string id)
        {
            var state = s.Fruitions.State(id);
            if (state.Status == FruitionStatus.Occupied) s.Fruitions.Vacate(id);
            Assume.That(s.Fruitions.State(id).Status, Is.EqualTo(FruitionStatus.Free));
        }

        private static CharacterData Holder(GameSession s, string lineage)
        {
            var m = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            m.SpiritualRoot = 50;
            m.GoldenCore = GoldenCoreState.Realization;
            m.FruitionId = lineage;
            s.Clan.AddMember(m);
            Free(s, lineage);
            s.Fruitions.Claim(lineage, m.FullName);
            return m;
        }

        [Test]
        public void AHolder_MovesToAnotherRealizationOfItsVirtue()
        {
            var s = Session(chance: 99);
            var m = Holder(s, "gathered-wood");
            Free(s, "nourishing-wood");
            Assert.IsNull(s.GoldenCore.Transmute(m, "nourishing-wood"));
            Assert.AreEqual("nourishing-wood", m.FruitionId);
            Assert.AreEqual(GoldenCoreState.Realization, m.GoldenCore);
            Assert.AreEqual(m.FullName, s.Fruitions.State("nourishing-wood").Holder);
            Assert.AreEqual(FruitionStatus.Free, s.Fruitions.State("gathered-wood").Status, "its former lineage is free");
        }

        [Test]
        public void OnlyWithinItsVirtue_AndToAFreeRealization()
        {
            var s = Session(chance: 99);
            var m = Holder(s, "gathered-wood");
            StringAssert.Contains("Vertu", s.GoldenCore.Transmute(m, "orthodox-water"));
            var state = s.Fruitions.State("nourishing-wood");
            if (state.Status == FruitionStatus.Free) s.Fruitions.Claim("nourishing-wood", "Un Étranger");
            StringAssert.Contains("libre", s.GoldenCore.Transmute(m, "nourishing-wood"));
        }

        [Test]
        public void OnlyARealizationsHolder_Transmutes()
        {
            var s = Session(chance: 99);
            var m = Holder(s, "gathered-wood");
            m.GoldenCore = GoldenCoreState.Surplus;
            Free(s, "nourishing-wood");
            StringAssert.Contains("Réalisation", s.GoldenCore.Transmute(m, "nourishing-wood"));
        }

        [Test]
        public void AFailure_BreaksTheBranch_AndADemonIsBorn()
        {
            var s = Session(chance: 1);
            var m = Holder(s, "gathered-wood");
            Free(s, "nourishing-wood");
            Assert.AreEqual("la branche rompt", s.GoldenCore.Transmute(m, "nourishing-wood"));
            Assert.IsFalse(m.IsAlive);
            Assert.AreEqual(DeathCause.MetalEssenceDemon, m.CauseOfDeath);
            Assert.AreEqual(DemonTier.Realization, s.Demons.Pending.Single().Tier, "a holder's demon: a catastrophe");
        }

        [Test]
        public void FromAnOrthodoxPosition_ItIsTheMorePerilous()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var orthodox = Holder(s, "orthodox-wood");
            var gathered = Holder(s, "gathered-wood");
            Assert.Less(s.GoldenCore.TransmutationOdds(orthodox), s.GoldenCore.TransmutationOdds(gathered),
                "an orthodox position knows no Intercalary: locked by its Life ability");
        }
    }
}
