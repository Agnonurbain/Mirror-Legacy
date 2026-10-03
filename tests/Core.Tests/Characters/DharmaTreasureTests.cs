using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Dharma Treasures and the Rank Designations (LORE.md §6.9; L4e, user decisions 2026-10-03). A True Monarch of the
    /// clan condenses a treasure of its foundation (ores, a few years): it adds to the clan's war strength and guards its
    /// bearer from ambushes; it is lost with its bearer. A position's holder may mortgage it on its Fruition: a Rank
    /// Designation that guards the domain even after its master's death — fully when its master is loved by the Fruition
    /// (its Dao Heart aligned), by half otherwise. Without a living master of its lineage it turns dangerous, until the
    /// clan unseals it and it returns to the Fruition.
    /// </summary>
    [TestFixture]
    public class DharmaTreasureTests
    {
        private static GameContent With(System.Func<DharmaSettings, DharmaSettings> tweak = null)
        {
            var b = Fixtures.QuietContent.Balance;
            return Fixtures.QuietContent with { Balance = b with { Dharma = tweak == null ? b.Dharma : tweak(b.Dharma) } };
        }

        private static GameSession Session(GameContent content = null)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? With() });
            s.Resources.AddOres(10_000);
            return s;
        }

        private static CharacterData Monarch(GameSession s, GoldenCoreState position = GoldenCoreState.Realization, bool loved = true)
        {
            var lineage = s.Context.Content.Fruitions.First(f => f.Abilities.Count > 0 && f.Temperament != Temperament.None);
            var m = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            m.GoldenCore = position;
            m.FruitionId = lineage.Id;
            m.FoundationId = $"{lineage.Id}:{lineage.Abilities[0].Id}";
            m.Temperament = loved ? lineage.Temperament : System.Enum.GetValues(typeof(Temperament)).Cast<Temperament>()
                .First(t => t != Temperament.None && t != lineage.Temperament);
            s.Clan.AddMember(m);
            return m;
        }

        private static void Years(GameSession s, int years)
        {
            for (int i = 0; i < years; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Dharma.ProcessYear();
            }
        }

        private static CharacterData WithTreasure(GameSession s, GoldenCoreState position = GoldenCoreState.Realization, bool loved = true)
        {
            var m = Monarch(s, position, loved);
            Assert.IsNull(s.Dharma.Condense(m));
            Years(s, s.Context.Content.Balance.Dharma.CondenseYears);
            Assert.IsTrue(m.HasDharmaTreasure);
            return m;
        }

        [Test]
        public void OnlyATrueMonarch_CondensesATreasure()
        {
            var s = Session();
            var mansion = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion);
            s.Clan.AddMember(mansion);
            StringAssert.Contains("Noyau d'Or", s.Dharma.Condense(mansion));
        }

        [Test]
        public void ATreasure_CostsOres_TakesYears_AndAddsToTheClansWarStrength()
        {
            var s = Session();
            var m = Monarch(s);
            double before = s.Wars.ClanWarStrength();
            int ores = s.Resources.SpiritualOres;
            Assert.IsNull(s.Dharma.Condense(m));
            Assert.Less(s.Resources.SpiritualOres, ores);
            Assert.IsFalse(m.HasDharmaTreasure, "it takes years");
            Years(s, s.Context.Content.Balance.Dharma.CondenseYears);
            Assert.IsTrue(m.HasDharmaTreasure);
            Assert.Greater(s.Wars.ClanWarStrength(), before);
            Assert.IsNotNull(s.Dharma.Condense(m), "one treasure each");
        }

        [Test]
        public void ATreasure_GuardsItsBearerFromAmbushes()
        {
            var s = Session();
            var m = WithTreasure(s);
            var other = Fixtures.Cultivator();
            s.Clan.AddMember(other);
            Assert.AreEqual(s.Context.Content.Balance.Dharma.TreasureGuardChance, s.Dharma.GuardChance(m), 1e-9);
            Assert.AreEqual(0, s.Dharma.GuardChance(other), 1e-9);
        }

        [Test]
        public void OnlyAPositionsHolder_MakesARankDesignation()
        {
            var s = Session();
            var m = WithTreasure(s, GoldenCoreState.MetallicEssenceOnly);
            StringAssert.Contains("position", s.Dharma.MakeDesignation(m));
        }

        [Test]
        public void ARankDesignation_GuardsTheDomain_FullyWhenItsMasterIsLoved()
        {
            var loved = Session();
            var a = WithTreasure(loved);
            Assert.IsNull(loved.Dharma.MakeDesignation(a));
            Assert.IsFalse(a.HasDharmaTreasure, "mortgaged on the Fruition");

            var unloved = Session();
            var b = WithTreasure(unloved, loved: false);
            unloved.Dharma.MakeDesignation(b);

            Assert.AreEqual(loved.Dharma.DomainStrength / 2, unloved.Dharma.DomainStrength, 1e-9, "Tan Qing, unloved, wields his only briefly");
            Assert.Greater(loved.Dharma.DomainGuardChance, unloved.Dharma.DomainGuardChance);
            Assert.Greater(loved.Dharma.DomainStrength, 0);
        }

        [Test]
        public void ARankDesignation_StillGuards_AfterItsMastersDeath()
        {
            var s = Session();
            var m = WithTreasure(s);
            s.Dharma.MakeDesignation(m);
            double strength = s.Wars.ClanWarStrength();
            s.Clan.Kill(m, DeathCause.OldAge);
            Assert.Greater(s.Dharma.DomainGuardChance, 0, "Wen Xiang's still guards a Celestial Cave");
            Assert.Greater(s.Dharma.DomainStrength, 0);
        }

        [Test]
        public void WithoutALivingMasterOfItsLineage_ItTurnsDangerous_UntilUnsealed()
        {
            var s = Session(With(d => d with { MasterlessStrikeChance = 1.0 }));
            var m = WithTreasure(s);
            s.Dharma.MakeDesignation(m);
            s.Clan.Kill(m, DeathCause.OldAge);
            s.Clan.AddMember(Fixtures.Cultivator());
            int living = s.Clan.LivingMembers.Count;
            Years(s, 1);
            Assert.Less(s.Clan.LivingMembers.Count, living, "a Designation without master is dangerous");
            var id = s.Dharma.Designations.Single().Id;
            Assert.IsNull(s.Dharma.Unseal(id));
            Assert.IsEmpty(s.Dharma.Designations, "it returns to its Fruition");
        }

        [Test]
        public void AnotherHolderOfItsLineage_BecomesItsMaster()
        {
            var s = Session(With(d => d with { MasterlessStrikeChance = 1.0 }));
            var m = WithTreasure(s);
            s.Dharma.MakeDesignation(m);
            var heir = Monarch(s, GoldenCoreState.Surplus);
            s.Clan.Kill(m, DeathCause.OldAge);
            int living = s.Clan.LivingMembers.Count;
            Years(s, 1);
            Assert.AreEqual(living, s.Clan.LivingMembers.Count, "a master of its lineage holds it");
            Assert.AreEqual(heir.ID, s.Dharma.MasterOf(s.Dharma.Designations.Single())?.ID);
        }

        [Test]
        public void TreasuresAndDesignations_SurviveASave()
        {
            var content = With();
            var s = Session(content);
            var m = WithTreasure(s);
            s.Dharma.MakeDesignation(m);
            WithTreasure(s);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = content });
            Assert.AreEqual(1, reloaded.Dharma.Designations.Count);
            Assert.AreEqual(s.Wars.ClanWarStrength(), reloaded.Wars.ClanWarStrength(), 1e-9);
        }
    }
}
