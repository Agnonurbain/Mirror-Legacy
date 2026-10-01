using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The pilot rises through the Purple Mansion to the Golden Core (2026-10-01, user decision: every behaviour that
    /// aims at the endings): it builds the garden and the mine, reads the Dao Partners, takes the aligned methods by an
    /// accord or a theft, sends a harvester for their Qi, condenses with resources when no method can be had, then forges
    /// and claims a position when the odds are good.
    /// </summary>
    [TestFixture]
    public class BalancePilotAscentTests
    {
        private const string Sea = "orthodox-water:boundless-sea";
        private const string Ruan = "Famille Ruan";
        private const string FordWatcherMethod = "orthodox-water-ford-watcher-method";
        private const string FordWatcherQi = "orthodox-water-ford-watcher-qi";

        private static readonly string[] FiveOrthodoxWater =
        {
            Sea, "orthodox-water:ford-watcher", "orthodox-water:storm-sky", "orthodox-water:dike-guard", "orthodox-water:river-farewell"
        };

        /// <summary>Five of the Orthodox Water, the Ford Watcher (of Life) condensed last: 60% at an average root.</summary>
        private static readonly string[] LifeLast =
        {
            Sea, "orthodox-water:storm-sky", "orthodox-water:dike-guard", "orthodox-water:river-farewell", "orthodox-water:ford-watcher"
        };

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        private static GameSession Session()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 0; // no accord unless a test allows one
            return s;
        }

        /// <summary>A Purple Mansion of the Orthodox Water holding the given abilities (the Boundless Sea alone by default).</summary>
        private static CharacterData Mansion(GameSession s, int xp = 0, params string[] abilities)
        {
            var m = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: 1);
            m.DivineAbilities = new List<string>(abilities.Length > 0 ? abilities : new[] { Sea });
            m.FoundationId = Sea;
            m.CultivationXP = xp;
            m.RealmStage = PowerLadder.PurpleMansionStageFromAbilities(m.DivineAbilities.Count);
            s.Clan.AddMember(m);
            s.Knowledge.Reveal(FactKind.Ability, Sea, KnowledgeSource.Formed);
            return m;
        }

        /// <summary>No power holds a method aligned on the Orthodox Water: resources are the only way left.</summary>
        private static void NoMethodInTheWorld(GameSession s)
        {
            foreach (var f in s.Factions.Factions)
                f.Techniques.RemoveAll(id => s.Techniques.Find(id)?.RequiredQiId is { } qi && s.Techniques.FindQi(qi)?.Foundation?.StartsWith("orthodox-water:") == true);
        }

        private static void KnowThePartners(GameSession s) => s.Knowledge.Reveal(FactKind.DaoPartners, Sea, KnowledgeSource.Studied);

        [Test]
        public void ThePilot_ReadsTheDaoPartners_OfItsPurpleMansion()
        {
            var s = Session();
            Mansion(s);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Knowledge.Knows(FactKind.DaoPartners, Sea));
        }

        [Test]
        public void ThePilot_TakesAnAlignedMethod_ByAnAccord()
        {
            var s = Session();
            Mansion(s);
            KnowThePartners(s);
            s.Factions.GetFactionByName(Ruan).RelationWithPlayer = 60;
            s.Techniques.Learn("supreme-yin-sutra"); // a grade-5 art Ruan lacks: worth an ascent method
            BalanceRun.Act(s);
            Assert.IsTrue(s.Techniques.Knows("orthodox-water-storm-sky-method"), "the first partner (not of Life: kept for the last) a power holds a method for");
        }

        [Test]
        public void ThePilot_PursuesTheAbility_ItHasAMethodFor_AndSendsAHarvesterForItsQi()
        {
            var s = Session();
            var mansion = Mansion(s);
            KnowThePartners(s);
            s.Techniques.Learn(FordWatcherMethod);
            s.Clan.AddMember(Fixtures.Cultivator(age: 25)); // a free Qi cultivator (the first dredges the lake)
            BalanceRun.Act(s);
            BalanceRun.SetTheIdleToWork(s);
            Assert.AreEqual("orthodox-water:ford-watcher", mansion.PursuedAbility);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.HarvestQiId == FordWatcherQi && m.CurrentTask == TaskType.GatherQi));
        }

        [Test]
        public void ThePilot_CondensesWithResources_WhenNoMethodCanBeHad()
        {
            var s = Session();
            var mansion = Mansion(s, xp: Xp);
            KnowThePartners(s);
            NoMethodInTheWorld(s);
            s.Resources.SetSpiritStones(100_000);
            s.Resources.AddHerbs(500);
            s.Resources.AddOres(500);
            foreach (var m in s.Clan.LivingMembers) m.LastOperationYear = s.Clock.Year; // nobody free to steal a manual
            BalanceRun.Act(s);
            Assert.AreEqual(2, mansion.DivineAbilities.Count);
            Assert.AreEqual(1, mansion.ShallowAbilities.Count, "its foundations stay shallow");
        }

        [Test]
        public void ThePilot_ForgesAGoldenCore_WhenItsGrandPerfectionIsReady()
        {
            var s = Session();
            var master = Mansion(s, Xp, LifeLast);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Knowledge.Knows(FactKind.GoldSeeking, "orthodox-water"), "the mirror deciphers the gold-seeking method");
            Assert.IsTrue(!master.IsAlive || master.Realm == CultivationRealm.GoldenCore, "forged, or the demon was born");
        }

        [Test]
        public void ThePilot_DoesNotForge_AgainstPoorOdds()
        {
            var s = Session();
            var master = Mansion(s, Xp, FiveOrthodoxWater);
            master.ShallowAbilities.AddRange(FiveOrthodoxWater.Skip(1).Take(3)); // three shallow foundations weigh on it
            BalanceRun.Act(s);
            Assert.IsTrue(master.IsAlive && master.Realm == CultivationRealm.PurpleMansion);
        }

        [Test]
        public void ThePilot_DoesNotForge_BelowSixtyPercent()
        {
            var s = Session();
            var master = Mansion(s, Xp, FiveOrthodoxWater); // no Life last: 50% at an average root
            BalanceRun.Act(s);
            Assert.IsTrue(master.IsAlive && master.Realm == CultivationRealm.PurpleMansion, "half a chance of a demon is no chance a patient clan takes");
        }

        [Test]
        public void ThePilot_WaitsForAMethod_RatherThanCondenseAShallowFoundation()
        {
            var s = Session();
            var mansion = Mansion(s, xp: Xp);
            KnowThePartners(s); // the powers hold methods of the Orthodox Water, out of reach this year
            Stocked(s, 100_000);
            BalanceRun.Act(s);
            Assert.AreEqual(1, mansion.DivineAbilities.Count, "it waits for a method rather than condense a shallow foundation");
        }

        [Test]
        public void ThePilot_ClaimsThePosition_OfAForgedEssence()
        {
            var s = Session();
            var essence = Mansion(s, 0, FiveOrthodoxWater);
            essence.Realm = CultivationRealm.GoldenCore;
            essence.RealmStage = 1;
            essence.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            essence.FruitionId = "orthodox-water";
            BalanceRun.Act(s);
            Assert.IsTrue(!essence.IsAlive || essence.GoldenCore == GoldenCoreState.Realization);
        }

        [Test]
        public void ThePilot_ExploresAKnownTomb_WhenItsTeamOutmatchesTheGuardian()
        {
            var s = Session();
            for (int i = 0; i < 3; i++) Mansion(s, 0, FiveOrthodoxWater); // a strong team
            s.Paths.Restore("orthodox-water-ford-watcher-method");
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.LastOperationYear == s.Clock.Year), "an expedition went to the tomb");
        }

        [Test]
        public void ThePilot_TakesAFamilyOfTheLake_AsItsVassal_FirstOfAll()
        {
            var s = Session();
            Mansion(s); // a Purple Mansion: the ascendant over the Foundation families
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 60;
            BalanceRun.Act(s);
            var vassal = s.Treaties.All.Single(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain);
            Assert.AreEqual("jingshui-lake", s.Factions.GetFactionByName(vassal.Faction).RegionId);
        }

        [Test]
        public void ThePilot_SavesForTheSect_InsteadOfBuilding_OnceOnlyItsStonesAreLacking()
        {
            var s = Session();
            Mansion(s);
            int needed = s.Context.Content.Balance.Sect.MinCultivators;
            while (s.Clan.LivingMembers.Count(m => m.Realm >= CultivationRealm.QiRefinement) < needed) s.Clan.AddMember(Fixtures.Cultivator());
            s.Resources.SetSpiritStones(s.Context.Content.Balance.Sect.FoundingStones - 1); // a garden is affordable, the sect not yet
            BalanceRun.Act(s);
            Assert.AreEqual(0, s.Buildings.GetBuilding(BuildingType.HerbGarden).Level, "every stone goes to the peaks");
        }

        private static void Stocked(GameSession s, int stones)
        {
            s.Resources.SetSpiritStones(stones);
            s.Resources.AddHerbs(1000);
            s.Resources.AddOres(1000);
            foreach (var m in s.Clan.LivingMembers) m.LastOperationYear = s.Clock.Year; // nobody free to steal a manual
        }

        [Test]
        public void ThePilot_StillCondenses_WhileSavingForTheSect()
        {
            var s = Session();
            var mansion = Mansion(s, xp: Xp);
            KnowThePartners(s);
            NoMethodInTheWorld(s);
            int needed = s.Context.Content.Balance.Sect.MinCultivators;
            while (s.Clan.LivingMembers.Count(m => m.Realm >= CultivationRealm.QiRefinement) < needed) s.Clan.AddMember(Fixtures.Cultivator());
            Stocked(s, s.Context.Content.Balance.Sect.FoundingStones - 1);
            BalanceRun.Act(s);
            Assert.AreEqual(2, mansion.DivineAbilities.Count, "the road to the Golden Core is no saving to forgo");
        }

        [Test]
        public void ThePilot_KeepsALifeAbility_ForTheLast()
        {
            var s = Session();
            var mansion = Mansion(s, Xp, Sea, "orthodox-water:storm-sky", "orthodox-water:dike-guard", "orthodox-water:river-farewell");
            KnowThePartners(s);
            NoMethodInTheWorld(s);
            Stocked(s, 100_000);
            BalanceRun.Act(s);
            Assert.AreEqual(5, mansion.DivineAbilities.Count);
            Assert.IsTrue(new[] { "orthodox-water:ford-watcher", "orthodox-water:peril-refuge" }.Contains(mansion.DivineAbilities.Last()),
                "a Life ability condensed last helps the forge");
        }

        [Test]
        public void ThePilot_SpendsNoLifeAbility_BeforeTheLast()
        {
            var s = Session();
            var mansion = Mansion(s, xp: Xp);
            KnowThePartners(s);
            NoMethodInTheWorld(s);
            Stocked(s, 100_000);
            BalanceRun.Act(s);
            Assert.AreEqual("orthodox-water:storm-sky", mansion.DivineAbilities.Last(), "the first partner that is not of Life");
        }

        [Test]
        public void ThePilot_BuildsTheHerbGardenAndTheMine_WhenItsTreasuryAllows()
        {
            var s = Session();
            s.Resources.SetSpiritStones(100_000);
            BalanceRun.Act(s);
            Assert.That(s.Buildings.GetBuilding(BuildingType.HerbGarden).Level, Is.GreaterThan(0));
            Assert.That(s.Buildings.GetBuilding(BuildingType.Mine).Level, Is.GreaterThan(0));
        }
    }
}
