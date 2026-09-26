using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The hunt as an operation (L2c.3; LORE.md D7 « everything is a plot »): a plan — target, team and roles,
    /// timing, cover story, diversion, false trail, the mirror's help — then three rolls (approach, capture,
    /// retreat). What it leaves behind feeds a hidden suspicion; a false trail that takes turns it into distrust
    /// between the powers.
    /// </summary>
    [TestFixture]
    public class HuntOperationTests
    {
        private const double Pass = 0.0;
        private const double Fail = 0.995;
        private const string Ruan = "Famille Ruan";

        private static HuntSettings Settings => Fixtures.Content.Balance.Hunt;

        /// <summary>A world in the hunt's window, the powers placed, one scouted beast of the Ruan in their prefecture.</summary>
        private static (TestWorld w, WorldBeast beast) World(System.Random rng, string owner = Ruan,
            CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 2)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Talismans.RestoreCalendar(w.Ctx.Clock.Year);
            var beast = new WorldBeast("iron-boar-heshan-1", "iron-boar", "heshan", realm, stage, owner);
            w.Bestiary.Restore(new[] { beast });
            w.Knowledge.Reveal(FactKind.Beast, beast.Id, KnowledgeSource.Studied);
            return (w, beast);
        }

        private static CharacterData Hunter(TestWorld w, int stage = 7) =>
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: stage));

        private static HuntPlan Plan(WorldBeast beast, params (CharacterData Member, HuntRole Role)[] team) => new HuntPlan
        {
            TargetBeastId = beast.Id,
            Team = team.ToDictionary(t => t.Member.ID, t => t.Role),
            Timing = HuntTiming.Dawn,
            Cover = CoverStory.None,
            Aid = MirrorAid.None
        };

        // ---- A plan that cannot be carried out ----

        [Test]
        public void Validate_Refuses_OutsideTheHuntsWindow()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            w.Talismans.RestoreCalendar(w.Ctx.Clock.Year + 10);
            StringAssert.Contains("window", w.Hunts.Validate(Plan(beast, (Hunter(w), HuntRole.Striker))));
        }

        [Test]
        public void Validate_Refuses_ABeastTheClanHasNotScouted()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var unknown = beast with { Id = "unknown-1" };
            w.Bestiary.Restore(new[] { beast, unknown });
            Assert.IsNotNull(w.Hunts.Validate(Plan(unknown, (Hunter(w), HuntRole.Striker))));
        }

        [Test]
        public void Validate_Refuses_ATeamWithoutAStriker()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            Assert.IsNotNull(w.Hunts.Validate(Plan(beast, (Hunter(w), HuntRole.Lookout))));
        }

        [Test]
        public void Validate_Refuses_AMemberBelowTheQiCultivation()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var child = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 3));
            Assert.IsNotNull(w.Hunts.Validate(Plan(beast, (Hunter(w), HuntRole.Striker), (child, HuntRole.Lure))));
        }

        [Test]
        public void Validate_Refuses_ADiversionFromTheTeam_OrOnTheTargetsPlace()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var striker = Hunter(w);
            var inTeam = Plan(beast, (striker, HuntRole.Striker)) with { DiversionMemberId = striker.ID, DiversionRegionId = "wuyang" };
            Assert.IsNotNull(w.Hunts.Validate(inTeam));

            var decoy = Hunter(w);
            var samePlace = Plan(beast, (striker, HuntRole.Striker)) with { DiversionMemberId = decoy.ID, DiversionRegionId = beast.RegionId };
            Assert.IsNotNull(w.Hunts.Validate(samePlace));
        }

        [Test]
        public void Validate_Refuses_FramingTheOwnerItself()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            Assert.IsNotNull(w.Hunts.Validate(Plan(beast, (Hunter(w), HuntRole.Striker)) with { FramedFaction = Ruan }));
        }

        [Test]
        public void Validate_Refuses_AMirrorAidThePowerCannotPay()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            while (w.Mirror.ConsumePower(1)) { }
            Assert.IsNotNull(w.Hunts.Validate(Plan(beast, (Hunter(w), HuntRole.Striker)) with { Aid = MirrorAid.Illusion }));
        }

        [Test]
        public void Validate_Accepts_AWellFormedPlan()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            Assert.IsNull(w.Hunts.Validate(Plan(beast, (Hunter(w), HuntRole.Striker), (Hunter(w), HuntRole.Lookout))));
        }

        // ---- The odds (pure rules) ----

        [Test]
        public void ApproachChance_RisesWithLookoutsNightDiversionAndIllusion()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var striker = Hunter(w);
            var bare = Plan(beast, (striker, HuntRole.Striker));
            int plain = HuntRules.ApproachChance(bare, beast, w.Factions, Fixtures.Content);

            Assert.Greater(HuntRules.ApproachChance(Plan(beast, (striker, HuntRole.Striker), (Hunter(w), HuntRole.Lookout)), beast, w.Factions, Fixtures.Content), plain);
            Assert.Greater(HuntRules.ApproachChance(bare with { Timing = HuntTiming.Night }, beast, w.Factions, Fixtures.Content), plain);
            Assert.Greater(HuntRules.ApproachChance(bare with { DiversionMemberId = "someone", DiversionRegionId = "wuyang" }, beast, w.Factions, Fixtures.Content), plain);
            Assert.Greater(HuntRules.ApproachChance(bare with { Aid = MirrorAid.Illusion }, beast, w.Factions, Fixtures.Content), plain);
        }

        [Test]
        public void ApproachChance_FallsWithTheStrengthOfTheOwnersGuard()
        {
            var (w, ruanBeast) = World(new FixedRandom(Pass));
            var peakBeast = ruanBeast with { OwnerFaction = "Secte du Pic des Nuées" }; // backed by a True Monarch
            var plan = Plan(ruanBeast, (Hunter(w), HuntRole.Striker));
            Assert.Less(HuntRules.ApproachChance(plan, peakBeast, w.Factions, Fixtures.Content),
                HuntRules.ApproachChance(plan, ruanBeast, w.Factions, Fixtures.Content));
        }

        [Test]
        public void CaptureChance_FollowsTheStrikersAgainstTheBeast_AndTheLure()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var weak = Hunter(w, stage: 1);
            var strong = Hunter(w, stage: 9);
            int low = HuntRules.CaptureChance(Plan(beast, (weak, HuntRole.Striker)), beast, w.Clan, Fixtures.Content);
            int high = HuntRules.CaptureChance(Plan(beast, (strong, HuntRole.Striker)), beast, w.Clan, Fixtures.Content);
            int lured = HuntRules.CaptureChance(Plan(beast, (weak, HuntRole.Striker), (Hunter(w), HuntRole.Lure)), beast, w.Clan, Fixtures.Content);
            Assert.Greater(high, low);
            Assert.Greater(lured, low);
        }

        [Test]
        public void Exposure_FallsWithACoverStory_AndStolenMemories()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var plan = Plan(beast, (Hunter(w), HuntRole.Striker));
            int bare = HuntRules.CleanExposure(plan, Fixtures.Content);
            Assert.Less(HuntRules.CleanExposure(plan with { Cover = CoverStory.Pilgrimage }, Fixtures.Content), bare);
            Assert.Less(HuntRules.CleanExposure(plan with { Aid = MirrorAid.MemoryTheft }, Fixtures.Content), bare);
        }

        // ---- Carrying it out ----

        [Test]
        public void Execute_ACleanHunt_TakesTheBeast_AndKeepsTheTeamBusy()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var striker = Hunter(w);
            w.Resources.AddSpiritStones(Settings.CoverStones[(int)CoverStory.Trade]);
            int stones = w.Resources.SpiritStones;

            var outcome = w.Hunts.Execute(Plan(beast, (striker, HuntRole.Striker)) with { Cover = CoverStory.Trade });

            Assert.IsTrue(outcome.Captured);
            Assert.AreEqual(new CapturedBeast(beast.Id, beast.Realm, beast.Stage, Ruan), w.Resources.Beasts.Single());
            Assert.IsFalse(w.Bestiary.Beasts.Contains(beast));
            Assert.AreEqual(TaskType.HuntBeast, striker.CurrentTask);
            Assert.AreEqual(stones - Settings.CoverStones[(int)CoverStory.Trade], w.Resources.SpiritStones);
        }

        [Test]
        public void Execute_ASeenApproach_TakesNothing_AndTheOwnerSuspects()
        {
            var (w, beast) = World(new FixedRandom(Fail));

            var outcome = w.Hunts.Execute(Plan(beast, (Hunter(w), HuntRole.Striker)));

            Assert.IsFalse(outcome.Approached || outcome.Captured);
            Assert.IsTrue(w.Bestiary.Beasts.Contains(beast));
            Assert.AreEqual(Settings.SeenExposure, w.Suspicion.OfClan(Ruan));
        }

        [Test]
        public void Execute_AFailedCapture_WoundsTheStrikers()
        {
            var (w, beast) = World(new SequenceRandom(Pass, Fail, Fail, Fail, Fail));
            var striker = Hunter(w);
            int stability = striker.MentalStability;

            var outcome = w.Hunts.Execute(Plan(beast, (striker, HuntRole.Striker)));

            Assert.IsTrue(outcome.Approached && !outcome.Captured);
            Assert.IsTrue(w.Bestiary.Beasts.Contains(beast), "the beast escapes");
            Assert.Less(striker.MentalStability, stability);
        }

        [Test]
        public void Execute_AFailedCapture_AgainstAFarStrongerBeast_MayKillTheStriker()
        {
            // approach passes, capture fails, the death roll strikes
            var (w, beast) = World(new SequenceRandom(Pass, Fail, Pass), realm: CultivationRealm.PurpleMansion, stage: 1); // power 31 against 11
            var striker = Hunter(w, stage: 1);

            var outcome = w.Hunts.Execute(Plan(beast, (striker, HuntRole.Striker)));

            CollectionAssert.AreEqual(new[] { striker.ID }, outcome.Casualties);
            Assert.IsFalse(striker.IsAlive);
            Assert.AreEqual(DeathCause.Combat, striker.CauseOfDeath);
        }

        [Test]
        public void Execute_TheDiversion_IsSeenElsewhere_NotHunting()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var decoy = Hunter(w);
            w.Hunts.Execute(Plan(beast, (Hunter(w), HuntRole.Striker)) with { DiversionMemberId = decoy.ID, DiversionRegionId = "wuyang" });
            Assert.AreEqual(TaskType.Diversion, decoy.CurrentTask);
        }

        [Test]
        public void Execute_ASolitaryBeast_MakesNoPowerSuspicious()
        {
            var (w, beast) = World(new FixedRandom(Fail), owner: null);
            w.Hunts.Execute(Plan(beast, (Hunter(w), HuntRole.Striker)));
            Assert.IsTrue(w.Factions.Factions.All(f => w.Suspicion.OfClan(f.Name) == 0));
        }

        [Test]
        public void Execute_AFalseTrailThatTakes_TurnsTheOwnersEyesOnAnother()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var outcome = w.Hunts.Execute(Plan(beast, (Hunter(w), HuntRole.Striker)) with { FramedFaction = "Famille Lou" });

            Assert.AreEqual("Famille Lou", outcome.Blamed);
            Assert.AreEqual(0, w.Suspicion.OfClan(Ruan));
            Assert.Greater(w.Suspicion.Distrust(Ruan, "Famille Lou"), 0);
        }

        [Test]
        public void Execute_AFalseTrailThatFails_MakesThingsWorse()
        {
            // captured cleanly, but the framing is seen through
            var (w, beast) = World(new SequenceRandom(Pass, Pass, Fail));
            var plan = Plan(beast, (Hunter(w), HuntRole.Striker));
            int clean = HuntRules.CleanExposure(plan, Fixtures.Content);

            w.Hunts.Execute(plan with { FramedFaction = "Famille Lou" });

            Assert.AreEqual(clean + Settings.FailedFrameBacklash, w.Suspicion.OfClan(Ruan));
        }

        [Test]
        public void Execute_Refuses_AnInvalidPlan()
        {
            var (w, beast) = World(new FixedRandom(Pass));
            var outcome = w.Hunts.Execute(Plan(beast, (Hunter(w), HuntRole.Lookout)));
            Assert.IsFalse(outcome.Approached || outcome.Captured);
            Assert.IsNotNull(outcome.Refusal);
        }

        // ---- Saves ----

        [Test]
        public void RoundTrip_KeepsTheHiddenSuspicionAndDistrust()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Suspicion.AddToClan(Ruan, 25);
            s.Suspicion.AddDistrust(Ruan, "Famille Lou", 10);

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.AreEqual(25, reloaded.Suspicion.OfClan(Ruan));
            Assert.AreEqual(10, reloaded.Suspicion.Distrust(Ruan, "Famille Lou"));
        }
    }
}
