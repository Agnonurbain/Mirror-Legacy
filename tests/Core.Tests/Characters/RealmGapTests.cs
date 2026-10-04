using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The gap between realms (LORE.md §5.4, §5.5; AUDIT_LORE.md §1, user decision 2026-10-03): a higher realm is of
    /// another nature, not a larger number. A Purple Mansion is invisible to the realms below and slips away at 5,000 m;
    /// two realms below, one cannot even touch its foe. Those out of reach add nothing to a team, and a power cannot
    /// ambush whom it cannot reach.
    /// </summary>
    [TestFixture]
    public class RealmGapTests
    {
        private static readonly RealmGapSettings Gap = new RealmGapSettings();

        private static CharacterData Member(CultivationRealm realm, int stage = 1) => Fixtures.Cultivator(realm: realm, stage: stage);

        [Test]
        public void APurpleMansion_IsOutOfReach_OfEveryRealmBelow()
        {
            Assert.IsTrue(RealmGap.OutOfReach(CultivationRealm.Foundation, CultivationRealm.PurpleMansion, Gap));
            Assert.IsTrue(RealmGap.OutOfReach(CultivationRealm.PurpleMansion, CultivationRealm.GoldenCore, Gap));
            Assert.IsFalse(RealmGap.OutOfReach(CultivationRealm.PurpleMansion, CultivationRealm.PurpleMansion, Gap));
        }

        [Test]
        public void BelowThePurpleMansion_OneRealmBehind_StillReaches_TwoDoNot()
        {
            Assert.IsFalse(RealmGap.OutOfReach(CultivationRealm.QiRefinement, CultivationRealm.Foundation, Gap), "traps, formations, numbers");
            Assert.IsTrue(RealmGap.OutOfReach(CultivationRealm.Embryonic, CultivationRealm.Foundation, Gap));
            Assert.IsFalse(RealmGap.OutOfReach(CultivationRealm.Foundation, CultivationRealm.QiRefinement, Gap), "the higher always reaches the lower");
        }

        [Test]
        public void ATeam_CountsOnlyThoseWhoReachTheFoe()
        {
            var three = new List<CharacterData> { Member(CultivationRealm.Foundation, 4), Member(CultivationRealm.Foundation, 4), Member(CultivationRealm.Foundation, 4) };
            Assert.AreEqual(0, RealmGap.TeamStrength(three, CultivationRealm.GoldenCore, 1.0, Gap), "three Foundations are nothing to a Golden Core");
            Assert.Greater(RealmGap.TeamStrength(three, CultivationRealm.Foundation, 1.0, Gap), 0);
            var mixed = new List<CharacterData> { Member(CultivationRealm.PurpleMansion), Member(CultivationRealm.Foundation, 4) };
            Assert.AreEqual(MirrorChronicles.Mirror.HuntRules.Power(mixed[0]), RealmGap.TeamStrength(mixed, CultivationRealm.PurpleMansion, 1.0, Gap), 1e-9);
        }

        [Test]
        public void APowerBelow_CannotAmbushAPurpleMansion()
        {
            var s = Fixtures.QuietContent.Balance.Schemes;
            var mansion = Member(CultivationRealm.PurpleMansion);
            var lesser = new FactionData { Name = "lesser", HighestRealm = CultivationRealm.Foundation };
            var peer = new FactionData { Name = "peer", HighestRealm = CultivationRealm.PurpleMansion };
            Assert.AreEqual(0, SchemeRules.CaptureChance(lesser, mansion, s, Gap), 1e-9);
            Assert.Greater(SchemeRules.CaptureChance(peer, mansion, s, Gap), 0);
        }

        private static MirrorChronicles.Session.GameSession Session() =>
            MirrorChronicles.Session.GameSession.NewGame(new MirrorChronicles.Session.GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static List<CharacterData> ThreeFoundations(MirrorChronicles.Session.GameSession s)
        {
            var team = new List<CharacterData> { Member(CultivationRealm.Foundation, 4), Member(CultivationRealm.Foundation, 4), Member(CultivationRealm.Foundation, 4) };
            foreach (var m in team) s.Clan.AddMember(m);
            return team;
        }

        [Test]
        public void NoExpedition_ToRuinsGuardedBeyondReach()
        {
            var s = Session();
            var team = ThreeFoundations(s);
            Assert.AreEqual(0, s.Shards.ExpeditionChance(team, new ShardDefinition { Id = "x", GuardRealm = CultivationRealm.GoldenCore }), 1e-9);
            Assert.Greater(s.Shards.ExpeditionChance(team, new ShardDefinition { Id = "x", GuardRealm = CultivationRealm.Foundation }), 0);
        }

        [Test]
        public void NoShardTheft_NoSabotage_FromAPowerBeyondReach()
        {
            var s = Session();
            var team = ThreeFoundations(s);
            var power = s.Factions.Factions[0];
            power.HighestRealm = CultivationRealm.PurpleMansion;
            Assert.AreEqual(0, s.PowerShards.TheftChance(team, power), 1e-9);
            Assert.AreEqual(0, s.WorldFruitions.SabotageChance(power.Name, team.ConvertAll(m => m.ID)), 1e-9);
            power.HighestRealm = CultivationRealm.Foundation;
            Assert.Greater(s.PowerShards.TheftChance(team, power), 0);
            Assert.Greater(s.WorldFruitions.SabotageChance(power.Name, team.ConvertAll(m => m.ID)), 0);
        }

        [Test]
        public void NoStrikerCaptures_ABeastBeyondReach()
        {
            var s = Session();
            var team = ThreeFoundations(s);
            var plan = new HuntPlan { Team = new Dictionary<string, HuntRole>() };
            foreach (var m in team) plan.Team[m.ID] = HuntRole.Striker;
            var mansionBeast = new WorldBeast("b", "x", "r", CultivationRealm.PurpleMansion, 1, null);
            var peerBeast = mansionBeast with { Realm = CultivationRealm.Foundation };
            Assert.AreEqual(0, MirrorChronicles.Mirror.HuntRules.CaptureChance(plan, mansionBeast, s.Clan, s.Context.Content));
            Assert.Greater(MirrorChronicles.Mirror.HuntRules.CaptureChance(plan, peerBeast, s.Clan, s.Context.Content), 0);
        }

        [Test]
        public void InBattle_ABlowFromBeyondReach_DoesNotLand()
        {
            var field = new MirrorChronicles.Combat.BattleField(new MirrorChronicles.Combat.CombatGrid(10, 10), new System.Random(1), new MirrorChronicles.Session.RecordingGameLog());
            var foundation = new MirrorChronicles.Combat.CombatUnit(Member(CultivationRealm.Foundation, 4), isAlly: true);
            var mansion = new MirrorChronicles.Combat.CombatUnit(Member(CultivationRealm.PurpleMansion), isAlly: false);
            var qi = new MirrorChronicles.Combat.CombatUnit(Member(CultivationRealm.QiRefinement, 9), isAlly: true);
            var peer = new MirrorChronicles.Combat.CombatUnit(Member(CultivationRealm.Foundation, 1), isAlly: false);
            Assert.IsTrue(field.IsPowerless(foundation, mansion), "a Purple Mansion is untouchable from below");
            Assert.IsFalse(field.IsPowerless(mansion, foundation));
            Assert.IsFalse(field.IsPowerless(qi, peer), "one realm behind still lands");
        }

        [Test]
        public void AGoldenCoresEssence_LeavesThoseOfItsFoundation_Powerless()
        {
            var monarch = Member(CultivationRealm.GoldenCore);
            monarch.FruitionId = "orthodox-water";
            var kin = Member(CultivationRealm.GoldenCore);
            kin.FoundationId = "orthodox-water:boundless-sea";
            var other = Member(CultivationRealm.GoldenCore);
            other.FoundationId = "bright-yang:x";
            Assert.IsTrue(RealmGap.Suppressed(kin, monarch), "LORE.md §5.5.2: of the same foundation, wholly powerless");
            Assert.IsFalse(RealmGap.Suppressed(other, monarch));
            Assert.IsFalse(RealmGap.Suppressed(monarch, kin), "the essence suppresses, it is not suppressed");
            var field = new MirrorChronicles.Combat.BattleField(new MirrorChronicles.Combat.CombatGrid(10, 10), new System.Random(1), new MirrorChronicles.Session.RecordingGameLog());
            Assert.IsTrue(field.IsPowerless(new MirrorChronicles.Combat.CombatUnit(kin, true), new MirrorChronicles.Combat.CombatUnit(monarch, false)));
        }

        [Test]
        public void APowersGoldenCore_LeavesThoseOfItsFoundation_Powerless_InItsSchemes()
        {
            var s = Fixtures.QuietContent.Balance.Schemes;
            var power = new FactionData { Name = "p", HighestRealm = CultivationRealm.GoldenCore };
            power.Elders.Add(new FactionElder { Id = "m", Name = "m", Realm = CultivationRealm.GoldenCore, Stage = 1, MaxLifespan = 1000, FruitionId = "orthodox-water" });
            var kin = Member(CultivationRealm.GoldenCore, 2);
            kin.FoundationId = "orthodox-water:boundless-sea";
            var other = Member(CultivationRealm.GoldenCore, 2);
            other.FoundationId = "bright-yang:x";
            Assert.IsTrue(RealmGap.SuppressedBy(kin, power));
            Assert.Greater(SchemeRules.CaptureChance(power, kin, s, Gap), SchemeRules.CaptureChance(power, other, s, Gap), "it cannot resist the essence");
            Assert.AreEqual(0, SchemeRules.RescueChance(new List<CharacterData> { kin }, power, s, Gap), 1e-9, "nor raise a hand against it");
            Assert.Greater(SchemeRules.RescueChance(new List<CharacterData> { other }, power, s, Gap), 0);
        }

        [Test]
        public void InWar_ASideOutOfReach_CannotWin()
        {
            Assert.AreEqual(1.0, MirrorChronicles.Diplomacy.WarRules.WinChance(10, CultivationRealm.PurpleMansion, 1000, CultivationRealm.Foundation, Gap), 1e-9,
                "a host of Foundations cannot touch a Purple Mansion");
            Assert.AreEqual(0.0, MirrorChronicles.Diplomacy.WarRules.WinChance(1000, CultivationRealm.Foundation, 10, CultivationRealm.PurpleMansion, Gap), 1e-9);
            Assert.AreEqual(0.5, MirrorChronicles.Diplomacy.WarRules.WinChance(50, CultivationRealm.Foundation, 50, CultivationRealm.QiRefinement, Gap), 1e-9,
                "within reach, strength decides");
        }

        [Test]
        public void AProbeByHand_CannotTouchAPowerBeyondReach_ButABribeStillMay()
        {
            var s = Session();
            var team = ThreeFoundations(s);
            var power = s.Factions.Factions[0];
            power.HighestRealm = CultivationRealm.PurpleMansion;
            var ids = team.ConvertAll(m => m.ID);
            Assert.AreEqual(0, s.Probes.ChanceAgainst(new ProbePlan(power.Name, ProbeApproach.Infiltration, ids, new List<string>(), 0)), 1e-9);
            if (s.SecretBook.NextUnknown(SecretBook.ClanHolder, power.Name) != null)
                StringAssert.Contains("n'atteint", s.Probes.RefusalOf(new ProbePlan(power.Name, ProbeApproach.Infiltration, ids, new List<string>(), 0)));
            Assert.AreEqual(0, s.Probes.ChanceAgainst(new ProbePlan(power.Name, ProbeApproach.RecordTheft, ids, new List<string>(), 0)), 1e-9);
            Assert.Greater(s.Probes.ChanceAgainst(new ProbePlan(power.Name, ProbeApproach.Bribery, ids, new List<string>(), 500)), 0, "gold has no realm");
            power.HighestRealm = CultivationRealm.Foundation;
            Assert.Greater(s.Probes.ChanceAgainst(new ProbePlan(power.Name, ProbeApproach.Infiltration, ids, new List<string>(), 0)), 0);
        }

        [Test]
        public void APurpleMansionProbing_ALesserTarget_IsNotSeen()
        {
            var s = Session();
            var mansion = Member(CultivationRealm.PurpleMansion);
            s.Clan.AddMember(mansion);
            var power = s.Factions.Factions[0];
            power.HighestRealm = CultivationRealm.Foundation;
            var unseen = new ProbePlan(power.Name, ProbeApproach.Infiltration, new List<string> { mansion.ID }, new List<string>(), 0);
            Assert.AreEqual(0, s.Probes.DetectChanceAgainst(unseen), 1e-9, "invisible to the realms below");
            power.HighestRealm = CultivationRealm.PurpleMansion;
            Assert.Greater(s.Probes.DetectChanceAgainst(unseen), 0);
        }

        [Test]
        public void ALesserPower_CannotProbeAClanGuardedBeyondItsReach()
        {
            var s = Session();
            foreach (var m in s.Clan.LivingMembers) m.Realm = CultivationRealm.PurpleMansion;
            var lesser = s.Factions.Factions[0];
            lesser.HighestRealm = CultivationRealm.Foundation;
            Assert.AreEqual(0, s.Probes.PowerChanceAgainst(lesser, SecretBook.ClanHolder, ProbeApproach.Infiltration), 1e-9);
        }

        [Test]
        public void NoRescue_WhenNoRescuerReachesTheCaptor()
        {
            var s = Fixtures.QuietContent.Balance.Schemes;
            var captor = new FactionData { Name = "captor", HighestRealm = CultivationRealm.GoldenCore };
            var three = new List<CharacterData> { Member(CultivationRealm.Foundation, 4), Member(CultivationRealm.Foundation, 4), Member(CultivationRealm.Foundation, 4) };
            Assert.AreEqual(0, SchemeRules.RescueChance(three, captor, s, Gap), 1e-9);
            var peers = new List<CharacterData> { Member(CultivationRealm.GoldenCore, 2) };
            Assert.Greater(SchemeRules.RescueChance(peers, captor, s, Gap), 0);
        }
    }
}
