using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// What a pierced secret is worth (user decision 2026-09-27; D7: profit). The clan may blackmail its holder — a power
    /// that fears the clan and can pay does, once per secret; one that towers over it defies it — expose it to all (every
    /// power learns it and distrusts the holder by its rank; the holder hates the clan), or sell it to a third power (it
    /// pays by the rank; the holder may learn who sold it). The powers expose the clan's secrets they pierced too. The
    /// screens show the known secrets, the clan's own with a sign of who may know, and a probe's odds before it is sent.
    /// </summary>
    [TestFixture]
    public class SecretDealingsTests
    {
        private const string Fang = "Famille Fang";   // Foundation, wealth 300
        private const string Peak = "Secte du Pic des Nuées";
        private const string Tao = "Famille Tao";
        private const string Ruan = "Famille Ruan";
        private static string Clan => SecretBook.ClanHolder;
        private static DealingSettings Settings => Fixtures.Content.Balance.Dealings;

        private static TestWorld World(System.Random rng, CultivationRealm strongest = CultivationRealm.Foundation)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: strongest, stage: 5)));
            return w;
        }

        private static Secret Known(TestWorld w, string holder, string kind)
        {
            var secret = w.SecretBook.Create(kind, holder, null);
            w.SecretBook.Grant(Clan, secret.Id);
            return secret;
        }

        [Test]
        public void TheClan_BlackmailsAPowerThatFearsIt_OncePerSecret()
        {
            var w = World(new FixedRandom(0.999));
            var secret = Known(w, Fang, "internal-feud");
            int stones = w.Resources.SpiritStones;
            int price = Settings.BlackmailPriceByRank[0];

            Assert.IsNull(w.Dealings.Blackmail(secret.Id));

            Assert.AreEqual(stones + price, w.Resources.SpiritStones);
            Assert.AreEqual(Settings.ResentmentByRank[0], w.Suspicion.OfClan(Fang), "it pays, and resents");
            Assert.IsNotNull(w.Dealings.Blackmail(secret.Id), "once per secret");
        }

        [Test]
        public void APowerThatTowersOverTheClan_DefiesItsBlackmail()
        {
            var w = World(new FixedRandom(0.999), CultivationRealm.QiRefinement);
            var secret = Known(w, Peak, "internal-feud");
            int stones = w.Resources.SpiritStones;
            StringAssert.Contains("défie", w.Dealings.Blackmail(secret.Id));
            Assert.AreEqual(stones, w.Resources.SpiritStones);
        }

        [Test]
        public void ASecretTheClanDoesNotKnow_IsWorthNothing()
        {
            var w = World(new FixedRandom(0.999));
            var secret = w.SecretBook.Create("internal-feud", Fang, null);
            Assert.IsNotNull(w.Dealings.Blackmail(secret.Id));
            Assert.IsNotNull(w.Dealings.Expose(secret.Id));
            Assert.IsNotNull(w.Dealings.Sell(secret.Id, Tao));
        }

        [Test]
        public void Exposing_TellsEveryone_AndTheHolderHatesTheClan()
        {
            var w = World(new FixedRandom(0.999));
            var secret = Known(w, Fang, "betrayed-ally"); // rank 3
            int relation = w.Factions.GetFactionByName(Fang).RelationWithPlayer;

            Assert.IsNull(w.Dealings.Expose(secret.Id));

            Assert.IsTrue(w.SecretBook.Knows(Tao, secret.Id));
            Assert.AreEqual(Settings.ExposeDistrustByRank[2], w.Suspicion.Distrust(Tao, Fang));
            Assert.AreEqual(relation + Settings.ExposeRelation, w.Factions.GetFactionByName(Fang).RelationWithPlayer);
        }

        [Test]
        public void Selling_BringsStones_ByTheRank_AndTheBuyerKnows()
        {
            var w = World(new FixedRandom(0.999)); // the holder does not learn who sold it
            var secret = Known(w, Fang, "betrayed-ally");
            int stones = w.Resources.SpiritStones;
            int wealth = w.Factions.GetFactionByName(Peak).Wealth;

            Assert.IsNull(w.Dealings.Sell(secret.Id, Peak));

            Assert.AreEqual(stones + Settings.SellPriceByRank[2], w.Resources.SpiritStones);
            Assert.AreEqual(wealth - Settings.SellPriceByRank[2], w.Factions.GetFactionByName(Peak).Wealth);
            Assert.IsTrue(w.SecretBook.Knows(Peak, secret.Id));
            Assert.AreEqual(0, w.Suspicion.OfClan(Fang));
        }

        [Test]
        public void ASoldSecret_MayReachItsHolder()
        {
            var w = World(new FixedRandom(0.0));
            var secret = Known(w, Fang, "betrayed-ally");
            w.Dealings.Sell(secret.Id, Peak);
            Assert.AreEqual(Settings.SoldResentment, w.Suspicion.OfClan(Fang));
        }

        [Test]
        public void APowerThatPiercedAClanSecret_MayExposeIt()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.ChangeRelation(w.Factions.GetFactionByName(Ruan).ID, -50);
            var deed = w.SecretBook.Create("blood-of-anothers-beast", Clan, Ruan);
            w.SecretBook.Grant(Ruan, deed.Id);

            w.Dealings.ProcessYear();

            Assert.IsTrue(w.SecretBook.Knows(Tao, deed.Id), "everyone knows now");
        }

        // ---- The screens ----

        [Test]
        public void TheKnownSecrets_AndTheClansOwn_AreShown_WithSigns()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var theirs = s.SecretBook.Create("hidden-treasure", Fang, null);
            s.SecretBook.Grant(Clan, theirs.Id);
            var ours = s.SecretBook.Create("planted-false-proof", Clan, Tao);

            var known = SecretsView.Known(s).Single(k => k.Id == theirs.Id);
            Assert.AreEqual((Fang, "Un trésor caché", "vital"), (known.Holder, known.Name, known.Rank));

            Assert.AreEqual("personne ne semble savoir", SecretsView.Clans(s).Single(c => c.Id == ours.Id).Sign);
            s.SecretBook.AddClues(Ruan, Clan, 5);
            Assert.AreEqual("des soupçons rôdent", SecretsView.Clans(s).Single(c => c.Id == ours.Id).Sign);
            s.SecretBook.Grant(Ruan, ours.Id);
            Assert.AreEqual("percé par au moins une puissance", SecretsView.Clans(s).Single(c => c.Id == ours.Id).Sign);
        }

        [Test]
        public void AProbesPreview_GivesItsOdds_OrWhyNot()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var team = new List<string> { s.Clan.GetPatriarch().ID };
            var preview = SecretsView.Preview(s, new ProbePlan(Fang, ProbeApproach.Infiltration, team, new List<string>(), 0));
            Assert.IsNull(preview.Refusal);
            Assert.That(preview.Chance, Is.InRange(1, 99));
            StringAssert.Contains("équipe", SecretsView.Preview(s, new ProbePlan(Fang, ProbeApproach.Infiltration, new List<string>(), new List<string>(), 0)).Refusal);
        }

        [Test]
        public void RoundTrip_KeepsWhatWasAlreadySpent()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            var secret = s.SecretBook.Create("internal-feud", Fang, null);
            s.SecretBook.Grant(Clan, secret.Id);
            s.Dealings.Blackmail(secret.Id);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.IsNotNull(reloaded.Dealings.Blackmail(secret.Id), "already spent");
        }
    }
}
