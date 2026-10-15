using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Lore audit §7, the user's decisions of 2026-10-15 (batch B). 7.9: a lineage's state weighs as the lore says — free,
    /// its Qi is rare and slow to harvest; broken, more abundant, but each breakthrough on it risks a deviation; occupied,
    /// normal. 7.14: a Heart Demon is born of trauma too — mourning, captivity, a grave defeat (struck down in a lost
    /// battle, a failed breakthrough that wounds the Dao), a betrayal (an ally, a spouse unmasked as a spy).
    /// </summary>
    [TestFixture]
    public class AuditDecisionsBTests
    {
        private const string Water = "orthodox-water"; // the Clear Spring's lineage

        private static void Lineage(TestWorld w, FruitionStatus status) =>
            w.Fruitions.Restore(new Dictionary<string, FruitionState> { [Water] = new FruitionState(status, null) }, new System.Random(1));

        // ---- 7.9 The state of a lineage ----

        [Test]
        public void AFreeLineage_GivesARareQi_ABrokenOneAnAbundantQi()
        {
            var w = new TestWorld();
            var spring = Fixtures.Content.Qi.Single(q => q.Id == Fixtures.ClanQi);
            Lineage(w, FruitionStatus.Occupied);
            double ruled = w.Place.Abundance("jingshui-lake", spring);
            Lineage(w, FruitionStatus.Free);
            double free = w.Place.Abundance("jingshui-lake", spring);
            Lineage(w, FruitionStatus.Broken);
            double broken = w.Place.Abundance("jingshui-lake", spring);

            Assert.Less(free, ruled, "free: rare");
            Assert.Greater(broken, ruled, "broken: more abundant");
            Assert.AreEqual(1.0, Fixtures.Content.Balance.RegionalQi.LineageStatusFactors[FruitionStatus.Occupied], "occupied: normal");
        }

        private static CharacterData Harvester(TestWorld w)
        {
            w.Techniques.Learn(Fixtures.ClanMethod);
            var member = w.Join(Fixtures.Cultivator());
            member.CurrentTask = TaskType.GatherQi;
            return member;
        }

        [Test]
        public void AFreeLineagesQi_IsSlowerToHarvest()
        {
            var w = new TestWorld();
            Lineage(w, FruitionStatus.Free);
            Harvester(w);
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(0, w.Resources.QiPortions(Fixtures.ClanQi), "a year is not enough");
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(1, w.Resources.QiPortions(Fixtures.ClanQi));
        }

        [Test]
        public void AnOccupiedLineagesQi_IsHarvestedAtItsPace()
        {
            var w = new TestWorld();
            Lineage(w, FruitionStatus.Occupied);
            Harvester(w);
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(1, w.Resources.QiPortions(Fixtures.ClanQi));
        }

        private static CharacterData AtTheWall(TestWorld w)
        {
            var c = w.Join(Fixtures.Cultivator(age: 30, realm: CultivationRealm.QiRefinement, stage: 9));
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            w.Resources.AddQi(Fixtures.ClanQi, 1);
            return c;
        }

        [Test]
        public void ABreakthrough_OnABrokenLineage_RisksADeviation()
        {
            var w = new TestWorld(new FixedRandom(0.0)); // the trial passes, and the deviation strikes
            Lineage(w, FruitionStatus.Broken);
            var c = AtTheWall(w);
            w.Breakthroughs.AttemptBreakthrough(c);
            Assert.AreEqual(1, c.DaoWounds);
        }

        [Test]
        public void ABreakthrough_OnAnOccupiedLineage_RisksNoDeviation()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            Lineage(w, FruitionStatus.Occupied);
            var c = AtTheWall(w);
            w.Breakthroughs.AttemptBreakthrough(c);
            Assert.AreEqual(0, c.DaoWounds);
        }

        // ---- 7.14 Heart Demons born of trauma ----

        private static TestWorld Fated(bool strikes) => new TestWorld(new FixedRandom(strikes ? 0.0 : 0.999));

        [Test]
        public void Mourning_ASpouse_MayBreedAHeartDemon()
        {
            var w = Fated(true);
            var widow = w.Join(Fixtures.Cultivator(isMale: false));
            var husband = w.Join(Fixtures.Cultivator());
            widow.SpouseID = husband.ID;
            husband.SpouseID = widow.ID;
            w.Clan.Kill(husband, DeathCause.OldAge);
            Assert.Greater(widow.HeartDemonYearsLeft, 0);
        }

        [Test]
        public void Mourning_AChild_MayBreedAHeartDemon_InTheParent()
        {
            var w = Fated(true);
            var mother = w.Join(Fixtures.Cultivator(isMale: false, age: 50));
            var child = w.Join(Fixtures.Cultivator(age: 20));
            child.MotherID = mother.ID;
            w.Clan.Kill(child, DeathCause.Combat);
            Assert.Greater(mother.HeartDemonYearsLeft, 0);
        }

        [Test]
        public void AStranger_IsNotMourned()
        {
            var w = Fated(true);
            var other = w.Join(Fixtures.Cultivator());
            var stranger = w.Join(Fixtures.Cultivator());
            w.Clan.Kill(stranger, DeathCause.OldAge);
            Assert.AreEqual(0, other.HeartDemonYearsLeft);
        }

        [Test]
        public void ATrauma_SparesTheLuckyOnes()
        {
            var w = Fated(false);
            var widow = w.Join(Fixtures.Cultivator(isMale: false));
            var husband = w.Join(Fixtures.Cultivator());
            widow.SpouseID = husband.ID;
            w.Clan.Kill(husband, DeathCause.OldAge);
            Assert.AreEqual(0, widow.HeartDemonYearsLeft);
        }

        [Test]
        public void AMortal_BearsNoHeartDemon()
        {
            var w = Fated(true);
            var widow = w.Join(Fixtures.Mortal(isMale: false));
            var husband = w.Join(Fixtures.Cultivator());
            widow.SpouseID = husband.ID;
            w.Clan.Kill(husband, DeathCause.OldAge);
            Assert.AreEqual(0, widow.HeartDemonYearsLeft);
        }

        [Test]
        public void Captivity_MayBreedAHeartDemon()
        {
            var w = Fated(true);
            var captive = w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerMemberCaptured(captive, "Famille Zang");
            Assert.Greater(captive.HeartDemonYearsLeft, 0);
        }

        [Test]
        public void StruckDown_InALostBattle_MayBreedAHeartDemon()
        {
            var w = Fated(true);
            var fallen = w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerClanBattleLost(new[] { fallen });
            Assert.Greater(fallen.HeartDemonYearsLeft, 0);
        }

        [Test]
        public void AFailedBreakthrough_ThatWoundsTheDao_MayBreedAHeartDemon()
        {
            var w = Fated(true);
            var wounded = w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerTrialWoundedDao(wounded);
            Assert.Greater(wounded.HeartDemonYearsLeft, 0);
        }

        [Test]
        public void AnAllysBetrayal_MayBreedAHeartDemon_InThePatriarch()
        {
            var w = Fated(true);
            var patriarch = Fixtures.Cultivator();
            w.Clan.Restore(new[] { patriarch }, patriarch.ID);
            w.Ctx.Events.TriggerTreatyBetrayed("Famille Zang");
            Assert.Greater(patriarch.HeartDemonYearsLeft, 0);
        }

        [Test]
        public void ASpouseUnmaskedAsASpy_MayBreedAHeartDemon()
        {
            var w = Fated(true);
            var betrayed = w.Join(Fixtures.Cultivator());
            var spy = w.Join(Fixtures.Cultivator(isMale: false));
            spy.SpyFor = "Famille Zang";
            spy.SpouseID = betrayed.ID;
            betrayed.SpouseID = spy.ID;
            w.Mirror.AddPower(100);
            w.Intrigues.Unmask(spy.ID);
            Assert.Greater(betrayed.HeartDemonYearsLeft, 0);
        }

        // ---- Review of batch B ----

        private static int Harvested(FruitionStatus status, int years)
        {
            var w = new TestWorld();
            Lineage(w, status);
            Harvester(w);
            for (int y = 0; y < years; y++) w.Tasks.ProcessYearlyTasks();
            return w.Resources.QiPortions(Fixtures.ClanQi);
        }

        [Test]
        public void TheHarvest_FollowsTheLineagesSway_WithoutRounding()
        {
            Assert.AreEqual(3, Harvested(FruitionStatus.Free, 5), "six tenths of a portion a year");
            Assert.AreEqual(7, Harvested(FruitionStatus.Broken, 5), "seven fifths of a portion a year");
        }

        [Test]
        public void AnOlderSave_KeepsItsHarvestUnderWay()
        {
            var s = MirrorChronicles.Session.GameSession.NewGame(new MirrorChronicles.Session.GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var data = s.ToSaveData();
            data.SaveVersion = "2.56";
            data.QiHarvestProgress = new Dictionary<string, int> { ["upstream-brook-qi"] = 1 }; // a year of a Qi of several
            var loaded = MirrorChronicles.Session.GameSession.FromSaveData(data, new MirrorChronicles.Session.GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual(MirrorChronicles.Economy.ResourceManager.HarvestUnitsPerYear, loaded.Resources.QiHarvestProgress["upstream-brook-qi"]);
        }

        [Test]
        public void AFallenCapturedAfterALostBattle_SuffersOnlyItsCaptivity()
        {
            var w = Fated(true);
            var taken = w.Join(Fixtures.Cultivator());
            taken.CaptorFaction = "Famille Zang";
            w.Ctx.Events.TriggerClanBattleLost(new[] { taken });
            Assert.AreEqual(0, taken.HeartDemonYearsLeft);
        }

        [Test]
        public void ASpyUnmaskedTwice_BetraysItsSpouseOnce()
        {
            var w = Fated(true);
            var betrayed = w.Join(Fixtures.Cultivator());
            var spy = w.Join(Fixtures.Cultivator(isMale: false));
            spy.SpyFor = "Famille Zang";
            spy.SpouseID = betrayed.ID;
            w.Mirror.AddPower(100);
            int told = 0;
            w.Ctx.Events.OnMemberUnmasked += _ => told++;
            w.Intrigues.Unmask(spy.ID);
            w.Intrigues.Unmask(spy.ID);
            Assert.AreEqual(1, told);
        }

        [Test]
        public void ATrauma_IsTold()
        {
            var w = Fated(true);
            var captive = w.Join(Fixtures.Cultivator());
            (CharacterData Member, Trauma Trauma)? told = null;
            w.Ctx.Events.OnTraumaHeartDemon += (m, t) => told = (m, t);
            w.Ctx.Events.TriggerMemberCaptured(captive, "Famille Zang");
            Assert.AreEqual((captive, Trauma.Captivity), told);
        }
    }
}
