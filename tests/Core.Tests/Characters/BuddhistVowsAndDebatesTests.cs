using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// A monk's vows and the Dao debates (R13-14b; LORE.md §3.7; the user's choices of 2026-10-12): a vow forbids tasks — a
    /// non-violent monk fights no battle — for a faster nature and a firmer word, and breaking it wounds the mandate. A temple
    /// may challenge the clan, the clan a temple; the odds weigh ranks, mind, a monk's training and a kept vow. The loser's
    /// clan loses face; an immortal may be converted by force or wounded; a monk's Dharma path breaks a realm lower.
    /// </summary>
    [TestFixture]
    public class BuddhistVowsAndDebatesTests
    {
        private const string Modern = "Temple du Lotus";
        private const string Ancient = "Temple de la Merveille Profonde";

        private static BuddhismSettings Settings => Fixtures.Content.Balance.Buddhism;
        private static DebateSettings Debates => Settings.Debates;

        private static TestWorld World(System.Random rng = null)
        {
            var w = rng == null ? new TestWorld(new FixedRandom(0.5)) : new TestWorld(rng);
            w.Factions.InitializeFactions();
            return w;
        }

        private static CharacterData Monk(TestWorld w, string temple = Ancient, CultivationRealm realm = CultivationRealm.Foundation, int stage = 2)
        {
            var c = w.Join(Fixtures.Cultivator(age: 280, realm: CultivationRealm.Foundation, stage: 2));
            c.MaxLifespan = c.Age + 5;
            Assert.IsTrue(w.Buddhism.Convert(c, temple));
            c.Realm = realm;
            c.RealmStage = stage;
            c.MaxLifespan = c.Age + 200;
            return c;
        }

        // ---- Vows ----

        [Test]
        public void AMonk_TakesAVow()
        {
            var w = World();
            var monk = Monk(w);
            Assert.IsNull(w.Buddhism.TakeVow(monk, BuddhistVow.Silence));
            Assert.AreEqual(BuddhistVow.Silence, monk.Vow);
        }

        [Test]
        public void OnlyAMonk_TakesAVow_AndOneAtATime()
        {
            var w = World();
            var immortal = w.Join(Fixtures.Cultivator());
            Assert.IsNotNull(w.Buddhism.TakeVow(immortal, BuddhistVow.Silence));
            var monk = Monk(w);
            w.Buddhism.TakeVow(monk, BuddhistVow.Silence);
            Assert.IsNotNull(w.Buddhism.TakeVow(monk, BuddhistVow.Renunciation));
            Assert.AreEqual(BuddhistVow.Silence, monk.Vow);
        }

        [Test]
        public void AVow_IsNotTaken_AtATaskItForbids()
        {
            var w = World();
            var monk = Monk(w);
            monk.CurrentTask = TaskType.Patrol;
            Assert.IsNotNull(w.Buddhism.TakeVow(monk, BuddhistVow.NonViolence));
            Assert.AreEqual(BuddhistVow.None, monk.Vow);
        }

        [Test]
        public void AMonkAtTheLowestRealm_WhoLoses_IsWounded()
        {
            var w = World(new FixedRandom(0.99));
            var monk = Monk(w, realm: CultivationRealm.Embryonic, stage: 2);
            w.Debates.Challenge(monk, Modern);
            Assert.AreEqual(DebateFate.Wounded, w.Debates.LastFate);
            Assert.AreEqual(1, monk.DaoWounds);
        }

        [Test]
        public void AKeptVow_QuickensTheNature()
        {
            var w = World();
            var vowed = Monk(w);
            var plain = Monk(w);
            w.Buddhism.TakeVow(vowed, BuddhistVow.Silence);
            vowed.CurrentTask = plain.CurrentTask = TaskType.Cultivation;
            vowed.CultivationXP = plain.CultivationXP = 0;
            w.Buddhism.ProcessYear();
            Assert.Greater(vowed.CultivationXP, plain.CultivationXP);
        }

        [Test]
        public void AForbiddenTask_BreaksTheVow_AndWoundsTheMandate()
        {
            var w = World();
            var monk = Monk(w);
            w.Buddhism.TakeVow(monk, BuddhistVow.Silence);
            monk.CurrentTask = TaskType.Diplomacy;
            int lifespan = monk.MaxLifespan;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(BuddhistVow.None, monk.Vow);
            Assert.AreEqual(1, monk.DaoWounds);
            Assert.Less(monk.MaxLifespan, lifespan);
        }

        [Test]
        public void AKeptTask_KeepsTheVow()
        {
            var w = World();
            var monk = Monk(w);
            w.Buddhism.TakeVow(monk, BuddhistVow.Silence);
            monk.CurrentTask = TaskType.Patrol;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(BuddhistVow.Silence, monk.Vow);
            Assert.AreEqual(0, monk.DaoWounds);
        }

        [Test]
        public void ANonViolentMonk_BreaksItsVow_FightingOnTheGrid()
        {
            var w = World();
            var monk = Monk(w);
            w.Buddhism.TakeVow(monk, BuddhistVow.NonViolence);
            w.Ctx.Events.TriggerClanFought(new[] { monk });
            Assert.AreEqual(BuddhistVow.None, monk.Vow);
            Assert.AreEqual(1, monk.DaoWounds);
        }

        [Test]
        public void ASilentMonk_MayFight()
        {
            var w = World();
            var monk = Monk(w);
            w.Buddhism.TakeVow(monk, BuddhistVow.Silence);
            w.Ctx.Events.TriggerClanFought(new[] { monk });
            Assert.AreEqual(BuddhistVow.Silence, monk.Vow);
        }

        // ---- The odds ----

        [Test]
        public void AHigherRealm_DebatesBetter()
        {
            var w = World();
            var low = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3));
            var high = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 3));
            low.MentalStability = high.MentalStability = 70;
            Assert.Greater(w.Debates.Odds(high, Modern), w.Debates.Odds(low, Modern));
        }

        [Test]
        public void AMonk_AndAKeptVow_DebateBetter()
        {
            var w = World();
            var immortal = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            var monk = Monk(w, realm: CultivationRealm.Foundation);
            immortal.MentalStability = monk.MentalStability = 70;
            int monkOdds = w.Debates.Odds(monk, Modern);
            Assert.AreEqual(w.Debates.Odds(immortal, Modern) + Debates.MonkBonus, monkOdds);
            w.Buddhism.TakeVow(monk, BuddhistVow.Silence);
            Assert.AreEqual(monkOdds + Settings.VowDebateBonus, w.Debates.Odds(monk, Modern));
        }

        [Test]
        public void TheOdds_StayWithinTheirBounds()
        {
            var w = World();
            var weak = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1));
            weak.MentalStability = 0;
            Assert.AreEqual(Debates.MinChance, w.Debates.Odds(weak, Modern));
        }

        // ---- The clan's challenge ----

        [Test]
        public void AWonDebate_GivesFaceAndWarmth()
        {
            var w = World(new FixedRandom(0.0));
            var debater = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            var temple = w.Factions.GetFactionByName(Modern);
            int prestige = w.Resources.Prestige, relation = temple.RelationWithPlayer;
            Assert.IsNull(w.Debates.Challenge(debater, Modern));
            Assert.AreEqual(DebateFate.Won, w.Debates.LastFate);
            Assert.AreEqual(prestige + Debates.WinPrestige, w.Resources.Prestige);
            Assert.AreEqual(relation + Debates.WinRelation, temple.RelationWithPlayer);
        }

        [Test]
        public void ALostDebate_CostsFace_AndWoundsAnImmortal()
        {
            var w = World(new FixedRandom(0.99));
            var debater = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            var temple = w.Factions.GetFactionByName(Modern);
            int prestige = w.Resources.Prestige, relation = temple.RelationWithPlayer;
            w.Debates.Challenge(debater, Modern);
            Assert.AreEqual(DebateFate.Wounded, w.Debates.LastFate);
            Assert.AreEqual(1, debater.DaoWounds);
            Assert.AreEqual(CultivationPath.Immortal, debater.Path);
            Assert.AreEqual(prestige - Debates.LossPrestige, w.Resources.Prestige);
            Assert.AreEqual(relation - Debates.LossRelation, temple.RelationWithPlayer);
        }

        [Test]
        public void ALostDebate_MayConvertAnImmortal_ByForce()
        {
            var w = World(new SequenceRandom(0.99, 0.0)); // the debate lost, the conversion drawn
            var debater = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            int lifespan = debater.MaxLifespan;
            w.Debates.Challenge(debater, Modern);
            Assert.AreEqual(DebateFate.Converted, w.Debates.LastFate);
            Assert.AreEqual(CultivationPath.Buddhist, debater.Path);
            Assert.AreEqual(Modern, debater.Temple);
            Assert.AreEqual(CultivationRealm.QiRefinement, debater.Realm);
            Assert.AreEqual(lifespan, debater.MaxLifespan, "not dying: no years gained");
        }

        [Test]
        public void ATrueMonarch_WhoLoses_IsWounded_NotConverted()
        {
            var w = World(new SequenceRandom(0.99, 0.0));
            var monarch = w.Join(Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore, stage: 1));
            w.Debates.Challenge(monarch, Modern);
            Assert.AreEqual(DebateFate.Wounded, w.Debates.LastFate);
            Assert.AreEqual(CultivationPath.Immortal, monarch.Path);
        }

        [Test]
        public void AMonkWhoLoses_FallsARealm()
        {
            var w = World(new FixedRandom(0.99));
            var monk = Monk(w, realm: CultivationRealm.PurpleMansion, stage: 3);
            w.Debates.Challenge(monk, Modern);
            Assert.AreEqual(DebateFate.Fell, w.Debates.LastFate);
            Assert.AreEqual(CultivationRealm.Foundation, monk.Realm);
        }

        [Test]
        public void ATemple_DebatesTheClanOnlyOnceInAWhile()
        {
            var w = World(new FixedRandom(0.0));
            var debater = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            w.Debates.Challenge(debater, Modern);
            Assert.IsNotNull(w.Debates.ChallengeRefusal(debater, Modern));
            Assert.IsNull(w.Debates.ChallengeRefusal(debater, Ancient));
        }

        [Test]
        public void OnlyATemple_IsDebated()
        {
            var w = World();
            var debater = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            var notATemple = w.Factions.Factions.First(f => f.Path != CultivationPath.Buddhist);
            Assert.IsNotNull(w.Debates.ChallengeRefusal(debater, notATemple.Name));
        }

        [Test]
        public void AMortal_AndABoundBeast_DoNotDebate()
        {
            var w = World();
            Assert.IsNotNull(w.Debates.DebaterRefusal(w.Join(Fixtures.Mortal(age: 40))));
            var beast = new CapturedBeast("mist-wolf-region-1", CultivationRealm.Foundation, 2);
            w.Resources.AddBeast(beast);
            Assert.IsNotNull(w.Debates.DebaterRefusal(w.DemonicDao.Bind(beast)));
        }

        // ---- A temple's challenge ----

        [Test]
        public void ATemple_MayChallengeTheClan()
        {
            var w = World(new FixedRandom(0.0));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            w.Debates.ProcessYear();
            Assert.IsNotNull(w.Debates.Pending);
        }

        [Test]
        public void TheClan_AnswersWithItsChampion()
        {
            var w = World(new FixedRandom(0.0));
            var champion = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            w.Debates.ProcessYear();
            Assert.IsNull(w.Debates.Answer(champion));
            Assert.IsNull(w.Debates.Pending);
            Assert.AreEqual(DebateFate.Won, w.Debates.LastFate);
        }

        [Test]
        public void ARefusal_CostsFaceAndWarmth()
        {
            var w = World(new FixedRandom(0.0));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            w.Debates.ProcessYear();
            var temple = w.Factions.GetFactionByName(w.Debates.Pending.Temple);
            int prestige = w.Resources.Prestige, relation = temple.RelationWithPlayer;
            Assert.IsNull(w.Debates.Decline());
            Assert.IsNull(w.Debates.Pending);
            Assert.AreEqual(prestige - Debates.RefusePrestige, w.Resources.Prestige);
            Assert.AreEqual(relation - Debates.RefuseRelation, temple.RelationWithPlayer);
        }

        [Test]
        public void AnUnansweredChallenge_IsARefusal_TheNextYear()
        {
            var w = World(new FixedRandom(0.0));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            w.Debates.ProcessYear();
            int prestige = w.Resources.Prestige;
            w.Ctx.Clock.Restore(w.Ctx.Clock.Year + 1, w.Ctx.Clock.Phase);
            w.Debates.ProcessYear();
            Assert.AreEqual(prestige - Debates.RefusePrestige, w.Resources.Prestige);
        }

        [Test]
        public void NoTemple_ChallengesAClanWithoutADebater()
        {
            var w = World(new FixedRandom(0.0));
            w.Join(Fixtures.Mortal(age: 40));
            w.Debates.ProcessYear();
            Assert.IsNull(w.Debates.Pending);
        }
    }
}
