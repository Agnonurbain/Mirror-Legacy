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
        public void ThePilot_ForgesAnyway_NearTheEndOfItsLife()
        {
            var s = Session();
            var master = Mansion(s, Xp, FiveOrthodoxWater); // 50%: below the patient bar
            master.Age = master.MaxLifespan - 10;            // but nothing left to lose (the user's choice, 2026-10-01)
            BalanceRun.Act(s);
            Assert.IsTrue(!master.IsAlive || master.Realm == CultivationRealm.GoldenCore, "forged, or the demon was born");
        }

        [Test]
        public void ANearlySpentEssence_ClaimsItsPosition_WhateverTheOdds()
        {
            var s = Session();
            var essence = Mansion(s, 0, FiveOrthodoxWater);
            essence.Realm = CultivationRealm.GoldenCore;
            essence.RealmStage = 1;
            essence.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            essence.FruitionId = "orthodox-water";
            essence.SpiritualRoot = 0; // poor odds
            essence.Age = essence.MaxLifespan - 5;
            BalanceRun.Act(s);
            Assert.IsTrue(!essence.IsAlive || essence.GoldenCore == GoldenCoreState.Realization);
        }

        [Test]
        public void ThePilot_WaitsForAMethod_RatherThanCondenseAShallowFoundation()
        {
            var s = Session();
            var mansion = Mansion(s, xp: Xp);
            KnowThePartners(s);
            s.Factions.GetFactionByName(Ruan).RelationWithPlayer = 60; // within reach, though the clan cannot pay it this year
            Stocked(s, 100_000);
            BalanceRun.Act(s);
            Assert.AreEqual(1, mansion.DivineAbilities.Count, "it waits for a method rather than condense a shallow foundation");
        }

        [Test]
        public void ThePilot_CondensesWithResources_WhenTheOnlyHolderIsOutOfReach()
        {
            var s = Session();
            var mansion = Mansion(s, xp: Xp);
            KnowThePartners(s);
            NoMethodInTheWorld(s);
            var kun = s.Factions.GetFactionByName("Empire de Kun"); // a Golden Core empire, hostile: no accord, no theft at good odds
            kun.Techniques.Add("orthodox-water-storm-sky-method");
            kun.RelationWithPlayer = -30;
            Stocked(s, 100_000);
            BalanceRun.Act(s);
            Assert.AreEqual(2, mansion.DivineAbilities.Count, "no holder within reach: waiting would be for ever");
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
            Assert.That(s.Buildings.GetBuilding(BuildingType.Mine).Level, Is.GreaterThan(0), "but the mine feeds the saving");
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
        public void ThePilot_RaisesTheMineAndTheForge_ToTheFourthLevel()
        {
            var s = Session();
            Mansion(s);
            s.Resources.SetSpiritStones(1_000_000);
            for (int year = 0; year < 6; year++) BalanceRun.Act(s);
            Assert.AreEqual(4, s.Buildings.GetBuilding(BuildingType.Mine).Level, "more veins worked fully");
            Assert.AreEqual(4, s.Buildings.GetBuilding(BuildingType.Forge).Level, "a better yield from each");
            Assert.AreEqual(3, s.Buildings.GetBuilding(BuildingType.HerbGarden).Level);
        }

        [Test]
        public void ARichClan_SeeksPeaceWithAStrongerPower_BeforeItCovetsTheHoard()
        {
            var s = Session();
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 30;
            s.Resources.SetSpiritStones(s.Context.Content.Balance.Intrigues.GreedStones); // a hoard that tempts
            BalanceRun.Act(s);
            double clan = s.Wars.ClanWarStrength();
            var settings = s.Context.Content.Balance.Wars;
            Assert.IsTrue(s.Treaties.All.Any(t => t.Kind == TreatyKind.NonAggression
                && MirrorChronicles.Diplomacy.WarRules.Strength(s.Factions.GetFactionByName(t.Faction), settings) > clan),
                "a pact with a power strong enough to covet it");
        }

        [Test]
        public void ThePilot_SendsADiplomat_ToAColdGateItCouldBow()
        {
            var s = Session();
            for (int i = 0; i < 20; i++) Mansion(s); // a host of Purple Mansions: the ascendant over a gate of its realm
            var envoy = Fixtures.Cultivator(age: 40);    // a Qi cultivator of a poor root: the one to send
            envoy.SpiritualRoot = 20;
            s.Clan.AddMember(envoy);
            var gate = s.Factions.GetFactionByName("Porte du Roc Obscur");
            gate.RelationWithPlayer = -20; // too cold to bow
            Assume.That(s.Treaties.HasAscendancyOver(gate));
            BalanceRun.Act(s);
            BalanceRun.SetTheIdleToWork(s);
            var envoys = s.Clan.LivingMembers.Where(m => m.CurrentTask == TaskType.Diplomacy).ToList();
            Assert.IsTrue(envoys.Any(m => s.Factions.GetFactionByName(m.DiplomacyTarget) is { Kind: FactionKind.Gate or FactionKind.Sect }));
            Assert.IsTrue(envoys.All(m => SpiritualOrificeRules.CanCultivate(m)), "a gate receives only a cultivator");
            Assert.IsFalse(s.Clan.LivingMembers.Any(m => m.Realm == CultivationRealm.PurpleMansion && m.CurrentTask == TaskType.Diplomacy),
                "never a Purple Mansion on the road to the Golden Core");
        }

        [Test]
        public void ThePilot_SeeksTheGatesAndSectsOfLinxi_BeforeItsFamilies()
        {
            var s = Session();
            for (int i = 0; i < 20; i++) Mansion(s);
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 60;
            foreach (var family in s.Factions.Factions.Where(f => f.RegionId == "jingshui-lake").ToList()) s.Factions.Restore(s.Factions.Factions.Where(f => f != family).ToList());
            BalanceRun.Act(s);
            var vassal = s.Treaties.All.Single(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain);
            Assert.That(s.Factions.GetFactionByName(vassal.Faction).Kind, Is.AnyOf(FactionKind.Gate, FactionKind.Sect), "the Twelve Gates' heirs");
        }

        // ---- The pilot digs the powers' secrets, where the shards lie (2026-10-01: rank-4 treasures never pierced) ----

        private static GameSession Prober()
        {
            var s = Session();
            for (int i = 0; i < 4; i++) s.Clan.AddMember(Fixtures.Cultivator(age: 30, realm: CultivationRealm.Foundation, stage: 3));
            return s;
        }

        [Test]
        public void ThePilot_ProbesAVassalFirst()
        {
            var s = Prober();
            s.Treaties.Conclude(new Treaty("v-tao", TreatyKind.Vassalage, "Famille Tao", s.Clock.Year, null, false, false, true));
            Assume.That(s.SecretBook.NextUnknown(MirrorChronicles.World.SecretBook.ClanHolder, "Famille Tao"), Is.Not.Null);
            Assert.AreEqual("Famille Tao", BalanceRun.NextProbe(s)?.Target);
        }

        [Test]
        public void ThePilot_KeepsDigging_ThePowerItProbed()
        {
            var s = Prober();
            var first = BalanceRun.NextProbe(s);
            s.Probes.Probe(first);
            BalanceRun.Dug(s, first.Target);
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            Assume.That(s.SecretBook.NextUnknown(MirrorChronicles.World.SecretBook.ClanHolder, first.Target), Is.Not.Null);
            Assert.AreEqual(first.Target, BalanceRun.NextProbe(s)?.Target, "a treasure lies under the lesser secrets: it digs on");
        }

        [Test]
        public void ThePilot_KeepsTheMirror_ForANewcomerToSound_RatherThanItsSight()
        {
            var s = Prober();
            var stranger = Fixtures.Cultivator(age: 25);
            stranger.FromFaction = "Secte de la Lune Pâle";
            s.Clan.AddMember(stranger);
            s.Mirror.Restore(39, 0); // short of a sounding with its reserve (20 + 20), enough for a sight
            Assert.AreNotEqual(ProbeApproach.MirrorSight, BalanceRun.NextProbe(s)?.Approach, "a spy unsounded is the greater danger");
        }

        [Test]
        public void ThePilot_LooksWithTheMirror_WhenItCan()
        {
            var s = Prober(); // the mirror at its full power
            Assert.AreEqual(ProbeApproach.MirrorSight, BalanceRun.NextProbe(s)?.Approach);
        }

        [Test]
        public void ThePilot_SabotagesARival_InTheRaceForItsMastersLineage()
        {
            var s = Session();
            var master = Mansion(s, 0, FiveOrthodoxWater); // a Grand Perfection of the Orthodox Water
            for (int i = 0; i < 3; i++) s.Clan.AddMember(Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: 3));
            var lou = s.Factions.GetFactionByName("Famille Lou");
            var rival = new FactionElder { Id = "rival", Name = "Rival", Realm = CultivationRealm.PurpleMansion, Stage = 5, BornYear = s.Clock.Year - 200,
                MaxLifespan = 500, RealmSinceYear = s.Clock.Year - 1000, GoldenCoreOdds = 0.5, Perfected = true };
            lou.Elders.Add(rival);
            s.WorldFruitions.OpenRace("orthodox-water"); // free from the start
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m != master && m.LastOperationYear == s.Clock.Year), "a team went to spoil the rival's preparation");
        }

        [Test]
        public void ThePilot_BuildsTheHerbGardenAndTheMine_WhenItsTreasuryAllows()
        {
            var s = Session();
            Mansion(s); // the garden's herbs serve a Purple Mansion
            s.Resources.SetSpiritStones(100_000);
            BalanceRun.Act(s);
            Assert.That(s.Buildings.GetBuilding(BuildingType.HerbGarden).Level, Is.GreaterThan(0));
            Assert.That(s.Buildings.GetBuilding(BuildingType.Mine).Level, Is.GreaterThan(0));
        }

        [Test]
        public void ThePilot_HidesTheRebornAncestor_InSeclusion_UntilItsPurpleMansion()
        {
            var s = Session();
            var chosen = Fixtures.Cultivator(age: 9, realm: CultivationRealm.Foundation);
            chosen.RebornFrom = "Mo Ancien";
            chosen.RebornEssence = new AncestorEssence("Mo Ancien", new List<string> { Sea }, null, GoldenCoreState.MetallicEssenceOnly, null, null);
            s.Clan.AddMember(chosen);
            BalanceRun.Act(s);
            BalanceRun.SetTheIdleToWork(s);
            Assert.AreEqual(TaskType.Seclusion, chosen.CurrentTask, "the Chosen of Destiny hides from the harvesters");
        }
    
        [Test]
        public void ThePilot_FoundsTheKingdom_AsSoonAsItMay()
        {
            var s = Session();
            s.Sect.Restore(s.Clock.Year);
            s.Clan.AddMember(Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore));
            var families = s.Factions.Factions.Where(f => f.Kind == FactionKind.Family).Take(3).ToList();
            s.Treaties.RestoreTreaties(families.Select((f, i) =>
                new Treaty($"v{i}", TreatyKind.Vassalage, f.Name, s.Clock.Year, null, false, false, true)).ToList());
            foreach (var f in s.Factions.Factions) f.PowerLevel = 0; // the clan weighs enough
            Assume.That(s.Imperial.FoundingRefusal(), Is.Null);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Imperial.IsKingdom, "the Imperial Way: a kingdom, then cultivating by governing");
        }
    
        [Test]
        public void ThePilot_LeavesADemonToTheUnderworld_AndSubduesOneLetBe()
        {
            var s = Session();
            var failed = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion, stage: 4);
            s.Clan.AddMember(failed);
            s.Clan.Kill(failed, DeathCause.MetalEssenceDemon);
            s.Events.TriggerMetalEssenceDemon(failed);
            s.Events.TriggerMetalEssenceDemon(failed);
            s.Demons.LetItBe(s.Demons.Pending[0].Id);
            s.Clan.AddMember(Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore));
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Demons.Pending, "the custom: safe");
            Assert.IsEmpty(s.Demons.Ravaging, "a Golden Core of the clan subdues it");
            Assert.AreEqual(0, s.Demons.Grudge);
        }
    
        [Test]
        public void ThePilot_CondensesATreasure_MortgagesIt_AndUnsealsAMasterlessDesignation()
        {
            var s = Session();
            s.Resources.AddOres(5_000);
            var monarch = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            monarch.GoldenCore = GoldenCoreState.Realization;
            monarch.FruitionId = "orthodox-water";
            s.Clan.AddMember(monarch);
            BalanceRun.Act(s);
            Assert.IsNotNull(monarch.TreasureReadyYear, "a True Monarch condenses its treasure");
            monarch.HasDharmaTreasure = true;
            BalanceRun.Act(s);
            Assert.AreEqual(1, s.Dharma.Designations.Count, "a holder mortgages it: it guards the domain");
            s.Clan.Kill(monarch, DeathCause.OldAge);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Dharma.Designations, "a masterless Designation is dangerous: it returns to its Fruition");
        }
    }
}
