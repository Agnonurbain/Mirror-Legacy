using System;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>Trials attempted in the Breakthrough phase: chakras and the Foundation wall (LORE.md §5).</summary>
    [TestFixture]
    public class BreakthroughSystemTests
    {
        private const double Success = 0.0;      // roll 1
        private const double Failure = 0.995;    // roll 100
        private const double Minor = 0.495;      // severity 50
        private const double Major = 0.895;      // severity 90
        private const double Deadly = 0.995;     // severity 100

        private GameContext ctx;
        private ClanManager clan;
        private BreakthroughSystem breakthroughs;

        private void Build(Random rng)
        {
            ctx = Fixtures.Context(rng);
            clan = new ClanManager(ctx, "Mo");
            var cultivation = new CultivationSystem(ctx, new ClanKarmaSystem(ctx, clan), new TechniqueLibrary(ctx), new ResourceManager(ctx));
            breakthroughs = new BreakthroughSystem(ctx, clan, cultivation);
        }

        [Test]
        public void AttemptBreakthrough_IsRefused_WhenTheMethodStopsBeforeTheNextRealm()
        {
            // LORE.md §2.2: grades 1-2 lead no further than Qi Cultivation
            Build(new SequenceRandom(Success));
            var capped = FacingTheFoundationWall();
            capped.CultivationMethodId = "common-breath-method";
            capped.QiId = "common-breath-qi";

            Assert.IsNull(breakthroughs.AttemptBreakthrough(capped));
            Assert.AreEqual(CultivationRealm.QiRefinement, capped.Realm);
        }

        /// <summary>A member with exactly the XP for the Inner Lake chakra trial.</summary>
        private CharacterData ReadyForInnerLake()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 0);
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Embryonic);
            clan.AddMember(c);
            return c;
        }

        /// <summary>A member at Qi 9 facing the Foundation wall at 70.</summary>
        private CharacterData FacingTheFoundationWall()
        {
            var c = Fixtures.Cultivator(age: 70, realm: CultivationRealm.QiRefinement, stage: 9);
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            clan.AddMember(c);
            return c;
        }

        [Test]
        public void AttemptBreakthrough_OpensTheChakra_WhenTheRollSucceeds()
        {
            Build(new SequenceRandom(Success));
            var c = ReadyForInnerLake();
            breakthroughs.AttemptBreakthrough(c);
            Assert.IsTrue(c.RealmStage == 1 && c.CultivationXP == 0);
        }

        [Test]
        public void AttemptBreakthrough_RaisesSuccess_WithTheNewRealm()
        {
            Build(new SequenceRandom(Success));
            var c = ReadyForInnerLake();
            CharacterData succeeded = null;
            ctx.Events.OnBreakthroughSuccess += (who, realm) => succeeded = who;
            breakthroughs.AttemptBreakthrough(c);
            Assert.AreSame(c, succeeded);
        }

        [Test]
        public void AttemptBreakthrough_KeepsHalfTheXp_OnAMinorFailure()
        {
            Build(new SequenceRandom(Failure, Minor));
            var c = ReadyForInnerLake();
            int required = c.CultivationXP;
            breakthroughs.AttemptBreakthrough(c);
            Assert.IsTrue(c.RealmStage == 0 && c.CultivationXP == required - required / 2);
        }

        [Test]
        public void AttemptBreakthrough_LosesAllXp_OnAMajorFailure()
        {
            Build(new SequenceRandom(Failure, Major));
            var c = ReadyForInnerLake();
            breakthroughs.AttemptBreakthrough(c);
            Assert.AreEqual(0, c.CultivationXP);
        }

        [Test]
        public void AttemptBreakthrough_RaisesFailure_WhenTheMemberSurvives()
        {
            Build(new SequenceRandom(Failure, Minor));
            var c = ReadyForInnerLake();
            CharacterData failed = null;
            ctx.Events.OnBreakthroughFailed += who => failed = who;
            breakthroughs.AttemptBreakthrough(c);
            Assert.AreSame(c, failed);
        }

        [Test]
        public void AttemptBreakthrough_KillsByQiDeviation_OnTheWorstSeverity()
        {
            Build(new SequenceRandom(Failure, Deadly));
            var c = ReadyForInnerLake();
            bool failureRaised = false;
            ctx.Events.OnBreakthroughFailed += who => failureRaised = true;
            breakthroughs.AttemptBreakthrough(c);
            Assert.IsTrue(!c.IsAlive && c.CauseOfDeath == DeathCause.QiDeviation && !failureRaised);
        }

        [Test]
        public void AttemptBreakthrough_DissolvesTheSpirit_WhenTheFoundationWallBreaksAnOldCultivator()
        {
            Build(new SequenceRandom(Failure, Minor)); // severity 50 ≤ 50% dissolution chance at 70
            var c = FacingTheFoundationWall();
            breakthroughs.AttemptBreakthrough(c);
            Assert.AreEqual(DeathCause.SpiritualDissolution, c.CauseOfDeath);
        }

        [Test]
        public void AttemptBreakthrough_GrantsTheShieldBonusOnce()
        {
            Build(new SequenceRandom(0.855, 0.495, 0.855, 0.495)); // roll 86 twice
            var shielded = ReadyForInnerLake();   // 78% + 30% shield → passes
            var unshielded = ReadyForInnerLake(); // 78% → fails
            breakthroughs.AncestralShieldActive = true;
            breakthroughs.AttemptBreakthrough(shielded);
            breakthroughs.AttemptBreakthrough(unshielded);
            Assert.IsTrue(shielded.RealmStage == 1 && unshielded.RealmStage == 0 && !breakthroughs.AncestralShieldActive);
        }

        [Test]
        public void ProcessBreakthroughPhase_OnlyTriesMembersReadyForATrial()
        {
            Build(new SequenceRandom(Success));
            var ready = ReadyForInnerLake();
            var notReady = Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 0);
            clan.AddMember(notReady);
            breakthroughs.ProcessBreakthroughPhase();
            Assert.IsTrue(ready.RealmStage == 1 && notReady.RealmStage == 0);
        }
    }
}
