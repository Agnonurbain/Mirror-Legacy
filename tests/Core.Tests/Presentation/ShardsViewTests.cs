using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The shards as the mirror's screen shows them (B3e): how far the mirror is restored, whether it sleeps, and each shard —
    /// found, lying in known ruins, held by a power the clan has pierced, or unknown — with what can be done and why not.
    /// The ending screen tells a dynastic ending; defeats are told by their cause.
    /// </summary>
    [TestFixture]
    public class ShardsViewTests
    {
        private static GameSession Quiet() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });

        [Test]
        public void TheHeader_TellsTheRestoration_AndTheSleep()
        {
            var s = Quiet();
            Assert.AreEqual("Éclats retrouvés : 0/7", ShardsView.Header(s));
            s.Shards.Recover("lake-jade");
            Assert.AreEqual($"Éclats retrouvés : 1/7 · le miroir dort jusqu'à l'an {s.Mirror.AsleepUntil}", ShardsView.Header(s));
        }

        [Test]
        public void EachShard_IsListed_WithWhatTheClanKnows()
        {
            var s = Quiet();
            s.Shards.Recover("lake-jade");
            s.Shards.RestoreRuins(new[] { "sunken-terraces-jade" });
            var held = s.SecretBook.All.Single(x => x.Subject == "jade-buckle");
            s.SecretBook.Grant(SecretBook.ClanHolder, held.Id);

            var lines = ShardsView.Lines(s);

            Assert.AreEqual(7, lines.Count);
            StringAssert.Contains("retrouvé", lines.Single(l => l.Id == "lake-jade").State);
            StringAssert.Contains("ruines", lines.Single(l => l.Id == "sunken-terraces-jade").State);
            StringAssert.Contains(held.Holder, lines.Single(l => l.Id == "jade-buckle").State);
            Assert.AreEqual("inconnu", lines.Single(l => l.Id == "pale-seal-jade").State);
        }

        [Test]
        public void AShardInKnownRuins_OffersAnExpedition_WithTheBestFreeTeam()
        {
            var s = Quiet();
            s.Shards.RestoreRuins(new[] { "sunken-terraces-jade" });
            var action = ShardsView.Lines(s).Single(l => l.Id == "sunken-terraces-jade").Actions.Single();
            Assert.IsTrue(action.Kind == ShardAction.Expedition && action.TeamIds.Count >= 1 && action.Refusal == null);
            StringAssert.Contains("%", action.Label);
        }

        [Test]
        public void AShardHeldByAKnownPower_OffersTheWaysAtHand()
        {
            var s = Quiet();
            s.SecretBook.Grant(SecretBook.ClanHolder, s.SecretBook.All.Single(x => x.Subject == "jade-buckle").Id);
            var kinds = ShardsView.Lines(s).Single(l => l.Id == "jade-buckle").Actions.Select(a => a.Kind).ToList();
            CollectionAssert.AreEquivalent(new[] { ShardAction.Steal, ShardAction.Demand, ShardAction.Trade }, kinds, "war is waged from diplomacy");
        }

        [Test]
        public void AnAction_SaysWhyItCannotBeTakenNow()
        {
            var s = Quiet();
            s.SecretBook.Grant(SecretBook.ClanHolder, s.SecretBook.All.Single(x => x.Subject == "jade-buckle").Id);
            var demand = ShardsView.Lines(s).Single(l => l.Id == "jade-buckle").Actions.Single(a => a.Kind == ShardAction.Demand);
            StringAssert.Contains("vassal", demand.Refusal);
        }

        [Test]
        public void TheEndingScreen_TellsTheEnding_AndWhoReachedIt()
        {
            var ending = Fixtures.VeteranContent.Endings.Single(e => e.Id == "ascension");
            var screen = EndingView.Of(ending, "Mo Jian", 312);
            Assert.AreEqual("L'Ascension", screen.Title);
            StringAssert.StartsWith("An 312 · Mo Jian", screen.Byline);
            Assert.AreEqual(ending.Narrative, screen.Story);
            Assert.AreEqual("Continuer la partie", screen.Continue);
        }

        [Test]
        public void ADefeat_IsToldByItsCause()
        {
            var s = Quiet();
            s.Events.TriggerMirrorSeized("Secte du Pic des Nuées");
            StringAssert.Contains("miroir", EndingView.Defeat(s));
        }

        [Test]
        public void AShardTheMirrorSensed_ShowsItsDirection()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1));
            s.PowerShards.Hide("pale-seal-jade", "Famille Ruan");
            s.ShardSense.Restore(new System.Collections.Generic.Dictionary<string, string> { ["pale-seal-jade"] = "heshan" });
            var line = ShardsView.Lines(s).Single(l => l.Id == "pale-seal-jade");
            StringAssert.Contains("le miroir le sent", line.State);
            StringAssert.Contains(s.Context.Content.Regions.Single(r => r.Id == "heshan").Name, line.State);
        }
    }
}
