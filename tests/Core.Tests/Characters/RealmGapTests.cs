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
