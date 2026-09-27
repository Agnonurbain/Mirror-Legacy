using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// The marriage alliance (user decision 2026-09-27, LORE.md D3/D7): the clan offers a member in marriage to a power,
    /// which accepts when it gains — the member's realm is worth something. The spouse it sends joins the clan (a spy,
    /// perhaps). The bond of blood is a treaty: no ambush, no strike without proof, a relation warming each year, a
    /// betrayal far rarer (its blood is the clan's hostage). It ends with a spouse's death; a repudiation breaks it — the
    /// spouse goes home, the power resents it, and the clan's word is worth less.
    /// </summary>
    [TestFixture]
    public class MarriageAllianceTests
    {
        private const string Tao = "Famille Tao";  // relation +20
        private const string Lou = "Famille Lou";  // relation -15
        private static TreatySettings Settings => Fixtures.Content.Balance.Treaties;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 5)));
            return w;
        }

        private static CharacterData Bride(TestWorld w, CultivationRealm realm = CultivationRealm.QiRefinement) =>
            w.Join(Fixtures.Cultivator(isMale: false, age: 20, realm: realm));

        [Test]
        public void AMarriage_BindsTheClanAndThePower()
        {
            var w = World(new FixedRandom(0.999)); // no spy this time
            var bride = Bride(w);

            Assert.IsNull(w.Matches.Propose(Tao, bride.ID));

            var spouse = w.Clan.FindById(bride.SpouseID);
            Assert.AreEqual(Tao, spouse.FromFaction);
            var treaty = w.Treaties.With(Tao).Single(t => t.Kind == TreatyKind.Marriage);
            Assert.AreEqual(spouse.ID, treaty.SpouseId);
        }

        [Test]
        public void AMarriage_IsRefused_ForAnUnfitMember_OrByAColdPower()
        {
            var w = World(new FixedRandom(0.999));
            var child = w.Join(Fixtures.Cultivator(age: 10));
            Assert.IsNotNull(w.Matches.Propose(Tao, child.ID));
            StringAssert.Contains("relation", w.Matches.Propose(Lou, Bride(w).ID));
            StringAssert.Contains("mariage", w.Treaties.Propose(Tao, TreatyKind.Marriage), "a marriage is made by a wedding");
        }

        [Test]
        public void AWorthierMember_MakesAPowerMoreWilling()
        {
            var w = World(new FixedRandom(0.999));
            var tao = w.Factions.GetFactionByName(Tao);
            Assert.Greater(w.Matches.Willingness(tao, Bride(w, CultivationRealm.PurpleMansion)), w.Matches.Willingness(tao, Bride(w)));
        }

        [Test]
        public void TheBondOfBlood_SparesTheClan_AndWarmsEachYear()
        {
            var w = World(new FixedRandom(0.999));
            w.Matches.Propose(Tao, Bride(w).ID);
            int relation = w.Factions.GetFactionByName(Tao).RelationWithPlayer;

            w.Treaties.ProcessYear();

            Assert.IsTrue(w.Treaties.Spares(Tao));
            Assert.AreEqual(relation + Settings.MarriageWarmthPerYear, w.Factions.GetFactionByName(Tao).RelationWithPlayer);
        }

        [Test]
        public void KinBetraysFarLess()
        {
            var power = new FactionData { Personality = FactionPersonality.Manipulative };
            var trade = new Treaty("t", TreatyKind.Trade, "x", 1, null, false, false, false);
            var marriage = new Treaty("m", TreatyKind.Marriage, "x", 1, null, false, false, false);
            Assert.Less(TreatyRules.BetrayalChance(power, marriage, 0, CultivationRealm.Foundation, Settings),
                TreatyRules.BetrayalChance(power, trade, 0, CultivationRealm.Foundation, Settings));
        }

        [Test]
        public void ASpousesDeath_EndsTheAlliance()
        {
            var w = World(new FixedRandom(0.999));
            var bride = Bride(w);
            w.Matches.Propose(Tao, bride.ID);
            w.Clan.Kill(bride, DeathCause.Illness);

            w.Treaties.ProcessYear();

            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Marriage));
        }

        [Test]
        public void ARepudiation_SendsTheSpouseHome_AtAPrice()
        {
            var w = World(new FixedRandom(0.999));
            var bride = Bride(w);
            w.Matches.Propose(Tao, bride.ID);
            var spouse = w.Clan.FindById(bride.SpouseID);
            int relation = w.Factions.GetFactionByName(Tao).RelationWithPlayer;

            Assert.IsNull(w.Matches.Repudiate(spouse.ID));

            Assert.IsFalse(w.Clan.LivingMembers.Contains(spouse), "gone home");
            Assert.IsTrue(spouse.Departed);
            Assert.IsNull(bride.SpouseID);
            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Marriage));
            Assert.AreEqual(relation + Settings.BreakRelation + Settings.RepudiationRelation, w.Factions.GetFactionByName(Tao).RelationWithPlayer);
        }

        [Test]
        public void TheScreens_NameTheAlliance_AndTheMembersToWed()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual("alliance matrimoniale", DiplomacyView.KindLabel(TreatyKind.Marriage));
            Assert.IsTrue(DiplomacyView.MarriageCandidates(s).All(c => s.Clan.FindById(c.Id).SpouseID == null));
        }

        [Test]
        public void RoundTrip_KeepsTheBondOfBlood()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            var single = DiplomacyView.MarriageCandidates(s).First();
            Assert.IsNull(s.Matches.Propose(Tao, single.Id));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(s.Treaties.With(Tao).Single(), reloaded.Treaties.With(Tao).Single());
        }

        // ---- Review ----

        [Test]
        public void ABondOfBlood_IsNotBroken_ButRepudiated()
        {
            var w = World(new FixedRandom(0.999));
            w.Matches.Propose(Tao, Bride(w).ID);
            StringAssert.Contains("répudi", w.Treaties.Break(w.Treaties.With(Tao).Single().Id));
            Assert.IsTrue(w.Treaties.Has(Tao, TreatyKind.Marriage));
        }

        [Test]
        public void TheSpousesDeath_EndsTheAlliance_AtOnce()
        {
            var w = World(new FixedRandom(0.999));
            var bride = Bride(w);
            w.Matches.Propose(Tao, bride.ID);
            var spouse = w.Clan.FindById(bride.SpouseID);

            w.Clan.Kill(spouse, DeathCause.ExecutedAsSpy);

            Assert.IsFalse(w.Treaties.Has(Tao, TreatyKind.Marriage), "no need to wait for the year");
            Assert.IsFalse(w.Treaties.Spares(Tao));
        }

        [Test]
        public void ACaptiveSpouse_CannotBeSentHome()
        {
            var w = World(new FixedRandom(0.999));
            var bride = Bride(w);
            w.Matches.Propose(Tao, bride.ID);
            var spouse = w.Clan.FindById(bride.SpouseID);
            w.Captives.Take(spouse, Lou);
            StringAssert.Contains("captif", w.Matches.Repudiate(spouse.ID));
            Assert.IsFalse(spouse.Departed);
        }

        [Test]
        public void TheTree_TellsTheDeparted_FromTheLivingAndTheDead()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var single = DiplomacyView.MarriageCandidates(s).First();
            s.Matches.Propose(Tao, single.Id);
            var spouseId = s.Clan.FindById(single.Id).SpouseID;
            s.Matches.Repudiate(spouseId);
            Assert.IsTrue(GenealogyView.Tree(s).Single(n => n.Id == spouseId).Departed);
        }
    }
}
