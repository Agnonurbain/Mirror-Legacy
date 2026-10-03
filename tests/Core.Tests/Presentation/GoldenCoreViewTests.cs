using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>The Golden Core's actions as the player sees them (L4e, G6): what each member may do, at what odds, or why not.</summary>
    [TestFixture]
    public class GoldenCoreViewTests
    {
        private static readonly string[] Five =
        {
            "orthodox-water:boundless-sea", "orthodox-water:ford-watcher", "orthodox-water:storm-sky",
            "orthodox-water:dike-guard", "orthodox-water:river-farewell"
        };

        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            s.Resources.AddOres(1_000);
            return s;
        }

        private static CharacterData GrandPerfection(GameSession s)
        {
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion, stage: 4);
            m.DivineAbilities = new List<string>(Five);
            m.FoundationId = Five[0];
            m.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
            s.Clan.AddMember(m);
            return m;
        }

        [Test]
        public void AGrandPerfection_IsOfferedToDecipherTheMethod_ThenToForge()
        {
            var s = Session();
            var m = GrandPerfection(s);
            var decipher = GoldenCoreView.Actions(s).Single(a => a.MemberId == m.ID && a.Kind == GoldenCoreAction.Decipher && a.Target == "orthodox-water");
            Assert.IsNull(GoldenCoreView.Perform(s, decipher));
            var forge = GoldenCoreView.Actions(s).Single(a => a.MemberId == m.ID && a.Kind == GoldenCoreAction.Forge && a.Target == "orthodox-water");
            StringAssert.Contains("%", forge.Label);
        }

        [Test]
        public void AnEssence_IsOfferedItsPosition_OrTheHoldersLeave()
        {
            var s = Session();
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore);
            m.DivineAbilities = new List<string>(Five);
            m.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            m.FruitionId = "orthodox-water";
            s.Clan.AddMember(m);
            var claim = GoldenCoreView.Actions(s).Single(a => a.MemberId == m.ID && a.Kind == GoldenCoreAction.Claim);
            Assert.IsTrue(claim.Refusal == null || claim.Refusal.Length > 0);
            StringAssert.Contains("position", claim.Label);
        }

        [Test]
        public void ATrueMonarch_IsOfferedItsDharmaTreasure()
        {
            var s = Session();
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore);
            s.Clan.AddMember(m);
            Assert.IsTrue(GoldenCoreView.Actions(s).Any(a => a.MemberId == m.ID && a.Kind == GoldenCoreAction.Treasure && a.Refusal == null));
        }

        [Test]
        public void AHolder_IsOfferedTheTransmutations_OfItsVirtue()
        {
            var s = Session();
            var m = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            m.GoldenCore = GoldenCoreState.Realization;
            m.FruitionId = "gathered-wood";
            s.Clan.AddMember(m);
            var transmutations = GoldenCoreView.Actions(s).Where(a => a.MemberId == m.ID && a.Kind == GoldenCoreAction.Transmute).ToList();
            Assert.IsTrue(transmutations.All(a => s.Context.Content.Fruitions.Single(f => f.Id == a.Target).Element == Element.Wood));
            Assert.IsTrue(transmutations.All(a => s.Fruitions.State(a.Target).Status == FruitionStatus.Free));
        }
    }
}
