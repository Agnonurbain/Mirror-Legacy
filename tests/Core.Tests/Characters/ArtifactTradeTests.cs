using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The artifacts between the clan and the powers (L4f, user decisions 2026-10-03; 📚 an armour commissioned of a sect, a
    /// map lent by a family, an axe traded for a pill): a sect or a gate forges on commission what its realm allows, dearer
    /// than the clan's forge; the clan sells an artifact; a friendly power lends one for some years, and the clan lends its
    /// own — a power fallen out with it keeps it; the clan steals from a power, and a power steals from the clan's armoury.
    /// </summary>
    [TestFixture]
    public class ArtifactTradeTests
    {
        private static GameContent Content(double theft = -1)
        {
            var b = Fixtures.QuietContent.Balance;
            return theft < 0 ? Fixtures.QuietContent : Fixtures.QuietContent with
            {
                Balance = b with { Shards = b.Shards with { TheftMinChance = theft, TheftMaxChance = theft } }
            };
        }

        private static GameSession Session(GameContent content = null)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Content() });
            s.Resources.AddSpiritStones(100_000);
            return s;
        }

        private static FactionData Sect(GameSession s) => s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);

        private static string Form(GameSession s) => s.Context.Content.ArtifactForms.First().Id;

        [Test]
        public void ASect_ForgesOnCommission_WhatItsRealmAllows_ForStones()
        {
            var s = Session();
            var sect = Sect(s);
            sect.RelationWithPlayer = 50;
            int stones = s.Resources.SpiritStones;
            Assert.IsNull(s.ArtifactTrade.Commission(sect.Name, Form(s), CultivationRealm.Foundation));
            Assert.AreEqual(CultivationRealm.Foundation, s.Artifacts.Armoury.Single().Rank);
            Assert.Less(s.Resources.SpiritStones, stones);
            var family = s.Factions.Factions.First(f => f.Kind == FactionKind.Family);
            family.RelationWithPlayer = 50;
            StringAssert.Contains("secte", s.ArtifactTrade.Commission(family.Name, Form(s), CultivationRealm.QiRefinement));
        }

        [Test]
        public void AHostilePower_TakesNoCommission()
        {
            var s = Session();
            var sect = Sect(s);
            sect.RelationWithPlayer = -50;
            Assert.IsNotNull(s.ArtifactTrade.Commission(sect.Name, Form(s), CultivationRealm.QiRefinement));
        }

        [Test]
        public void TheClan_SellsAnArtifact()
        {
            var s = Session();
            var sect = Sect(s);
            var sword = s.Artifacts.Create(Form(s), CultivationRealm.Foundation, null);
            int stones = s.Resources.SpiritStones;
            Assert.IsNull(s.ArtifactTrade.Sell(sword.Id, sect.Name));
            Assert.Greater(s.Resources.SpiritStones, stones);
            Assert.IsEmpty(s.Artifacts.Armoury);
            Assert.IsTrue(sect.Artifacts.Any(a => a.Id == sword.Id));
        }

        [Test]
        public void AFriendlyPower_LendsAnArtifact_ThatGoesBackInTime()
        {
            var s = Session();
            var sect = Sect(s);
            sect.RelationWithPlayer = 80;
            Assert.IsNull(s.ArtifactTrade.Borrow(sect.Name));
            Assert.AreEqual(80 - s.Context.Content.Balance.Artifacts.Trade.BorrowRelationCost, sect.RelationWithPlayer, "a favour owed");
            var lent = s.Artifacts.Armoury.Single();
            Assert.AreEqual(sect.Name, lent.LentBy);
            var bearer = Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore);
            s.Clan.AddMember(bearer);
            s.Artifacts.Equip(bearer, lent.Id);
            for (int i = 0; i <= s.Context.Content.Balance.Artifacts.Trade.LoanYears; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.ArtifactTrade.ProcessYear();
            }
            Assert.IsNull(bearer.Artifact, "even borne, it goes back");
            Assert.IsEmpty(s.Artifacts.Armoury);
        }

        [Test]
        public void TheClansLoan_ComesBack_UnlessThePowerFellOutWithIt()
        {
            var s = Session();
            var sect = Sect(s);
            sect.RelationWithPlayer = 20;
            var sword = s.Artifacts.Create(Form(s), CultivationRealm.Foundation, null);
            Assert.IsNull(s.ArtifactTrade.Lend(sword.Id, sect.Name));
            Assert.Greater(sect.RelationWithPlayer, 20, "a loan warms a power");
            var shield = s.Artifacts.Create(Form(s), CultivationRealm.Foundation, null);
            var family = s.Factions.Factions.First(f => f.Kind == FactionKind.Family);
            family.RelationWithPlayer = 20;
            s.ArtifactTrade.Lend(shield.Id, family.Name);
            family.RelationWithPlayer = -60;
            for (int i = 0; i <= s.Context.Content.Balance.Artifacts.Trade.LoanYears; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.ArtifactTrade.ProcessYear();
            }
            Assert.IsTrue(s.Artifacts.Armoury.Any(a => a.Id == sword.Id), "it comes back");
            Assert.IsFalse(s.Artifacts.Armoury.Any(a => a.Id == shield.Id), "a power fallen out keeps it");
            Assert.IsTrue(family.Artifacts.Any(a => a.Id == shield.Id));
        }

        [Test]
        public void TheClan_StealsFromAPower()
        {
            var s = Session(Content(theft: 1.0));
            var sect = Sect(s);
            var thief = Fixtures.Cultivator(age: 50, realm: CultivationRealm.Foundation);
            s.Clan.AddMember(thief);
            Assert.IsNull(s.ArtifactTrade.StealFrom(sect.Name, new[] { thief.ID }));
            Assert.AreEqual(1, s.Artifacts.Armoury.Count);
        }

        [Test]
        public void ATheftCaught_LeavesProofAndAGrudge()
        {
            var s = Session(Content(theft: 0.0));
            var sect = Sect(s);
            int relation = sect.RelationWithPlayer;
            var thief = Fixtures.Cultivator(age: 50, realm: CultivationRealm.Foundation);
            s.Clan.AddMember(thief);
            Assert.IsNotNull(s.ArtifactTrade.StealFrom(sect.Name, new[] { thief.ID }));
            Assert.IsEmpty(s.Artifacts.Armoury);
            Assert.Less(sect.RelationWithPlayer, relation);
        }

        [Test]
        public void APower_StealsFromTheClansArmoury()
        {
            var s = Session();
            var sword = s.Artifacts.Create(Form(s), CultivationRealm.Foundation, null);
            var thief = Sect(s);
            Assert.IsNotNull(s.Intrigues.Steal(thief, IntrigueTarget.Artifact));
            Assert.IsEmpty(s.Artifacts.Armoury);
            Assert.IsTrue(thief.Artifacts.Any(a => a.Id == sword.Id));
        }

        [Test]
        public void ThePowersArtifacts_AndTheLoans_SurviveASave()
        {
            var s = Session();
            var sect = Sect(s);
            sect.RelationWithPlayer = 80;
            s.ArtifactTrade.Borrow(sect.Name);
            var sword = s.Artifacts.Create(Form(s), CultivationRealm.Foundation, null);
            s.ArtifactTrade.Sell(sword.Id, sect.Name);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Content() });
            Assert.AreEqual(sect.Name, reloaded.Artifacts.Armoury.Single().LentBy);
            Assert.AreEqual(1, reloaded.Factions.GetFactionByName(sect.Name).Artifacts.Count);
        }
    }
}
