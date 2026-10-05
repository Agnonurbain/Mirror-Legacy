using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// The clan, client of the Cloud Peak (AUDIT_LORE.md §4.1-4.2, §5.1; the user's decisions 2026-10-04; 📚 wiki: the lake's
    /// families are governed by the sect, which takes their prodigies, raises levies for the frontier, and silently wipes out a
    /// defaulter): a small tribute, no absorption; the sect takes the most gifted child born with an orifice as its disciple; it
    /// does not see a seed, but a seeded cultivator in the open gives it clues; breaking free means war. The lake's other
    /// families are its vassals too, and its jurisdiction reaches the lake.
    /// </summary>
    [TestFixture]
    public class ClientageTests
    {
        private const string Peak = "Secte du Pic des Nuées";

        private static ClientageSettings C => Fixtures.Content.Balance.Clientage;

        private static GameSession Session(System.Func<ClientageSettings, ClientageSettings> tweak = null)
        {
            var c = Fixtures.QuietContent;
            var content = tweak == null ? c : c with { Balance = c.Balance with { Clientage = tweak(c.Balance.Clientage) } };
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
        }

        private static Treaty Clientage(GameSession s) => s.Treaties.All.Single(t => t.Client);

        private static void AYear(GameSession s)
        {
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.Treaties.ProcessYear();
        }

        [Test]
        public void TheClan_BeginsAsTheCloudPeaksClient_AsTheLakesFamiliesItsVassals()
        {
            var s = Session();
            var client = Clientage(s);
            Assert.AreEqual(Peak, client.Faction);
            Assert.IsFalse(client.ClanIsSuzerain);
            Assert.AreEqual(Peak, s.Politics.SuzerainOf("Famille Tao"), "the lake's families answer to the sect too");
            Assert.AreEqual("Porte du Fer Ardent", s.Politics.SuzerainOf("Famille Lü"), "📚 save those of the Golden Tang Gate");
        }

        [Test]
        public void TheSectsJurisdiction_ReachesTheLake()
        {
            var s = Session();
            var peak = s.Factions.GetFactionByName(Peak);
            var lakeFamily = s.Factions.Factions.First(f => f.RegionId == "jingshui-lake");
            Assert.IsTrue(s.Factions.AreNeighbours(peak, lakeFamily));
            Assert.IsTrue(s.Factions.AreNeighbours(lakeFamily, peak), "neighbours both ways");
        }

        [Test]
        public void TheTribute_IsSmall_AndTheSectNeverAbsorbsItsClient()
        {
            var s = Session(c => c with { SelectionChance = 0, LevyChance = 0 });
            s.Resources.SetSpiritStones(1000);
            AYear(s);
            Assert.AreEqual(1000 - (int)(1000 * C.TributeShare), s.Resources.SpiritStones);
            for (int i = 0; i < 60; i++) AYear(s);
            Assert.AreEqual(0, Clientage(s).Absorptions, "the sect reaps its clients later, it does not absorb them");
            Assert.IsNotEmpty(s.Clan.LivingMembers);
        }

        [Test]
        public void TheSect_TakesTheMostGiftedChildBornWithAnOrifice_NotASeededOne()
        {
            var s = Session(c => c with { SelectionChance = 1, LevyChance = 0 });
            var seeded = Fixtures.Mortal(age: 10);
            seeded.HasTalismanSeed = true;
            seeded.SpiritualRoot = 99;
            s.Clan.AddMember(seeded);
            var born = Fixtures.Mortal(age: 9);
            born.HasSpiritualOrifice = true;
            born.SpiritualRoot = 60;
            s.Clan.AddMember(born);
            AYear(s);
            Assert.AreEqual(Peak, born.DiscipleOf, "taken as the sect's disciple");
            Assert.IsNull(seeded.DiscipleOf, "the sect does not see the mirror's seed");
        }

        [Test]
        public void ASeededCultivator_InTheOpen_GivesTheSectClues()
        {
            var s = Session(c => c with { SelectionChance = 0, LevyChance = 0, SeededClueChance = 1 });
            var seeded = Fixtures.Mortal(age: 20);
            seeded.HasTalismanSeed = true;
            seeded.RealmStage = 2; // breathing: its cultivation shows
            s.Clan.AddMember(seeded);
            int clues = s.Suspicion.MirrorClues(Peak);
            AYear(s);
            Assert.Greater(s.Suspicion.MirrorClues(Peak), clues);
        }

        [Test]
        public void TheSect_RaisesALevy_ForTheFrontier()
        {
            var s = Session(c => c with { SelectionChance = 0, LevyChance = 1, LevyDeathChance = 0 });
            var soldier = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3);
            s.Clan.AddMember(soldier);
            AYear(s);
            Assert.AreEqual(s.Clock.Year, soldier.LastOperationYear, "away at the frontier this year");
        }

        [Test]
        public void BreakingFree_MeansWar()
        {
            var s = Session();
            Assert.IsNull(s.Treaties.Break(Clientage(s).Id));
            Assert.IsTrue(s.Wars.ClanWars.Any(w => w.Enemy == Peak), "the sect strikes the client who broke away");
        }

        [Test]
        public void TheWorld_OpensWithItsOldEnmities_AndTheNovelsTempers()
        {
            // 📚 audit §4.4, §4.8, §4.9
            var s = Session();
            Assert.Greater(s.Suspicion.Distrust("Famille Lou", "Famille Tao"), 0);
            Assert.Greater(s.Suspicion.Distrust("Famille Bai", "Famille Ruan"), 0);
            Assert.Greater(s.Suspicion.Distrust(Peak, "Secte de la Lune Pâle"), 0);
            var ruan = s.Factions.GetFactionByName("Famille Ruan");
            Assert.AreEqual(FactionPersonality.Manipulative, ruan.Personality);
            Assert.Greater(ruan.RelationWithPlayer, 0, "the clan's (self-interested) protector");
            Assert.Less(s.Factions.GetFactionByName("Famille Lü").RelationWithPlayer, 0, "the clan's first enemy");
            Assert.AreEqual(CultivationRealm.PurpleMansion, s.Factions.GetFactionByName("Porte du Fer Ardent").HighestRealm);
        }

        [Test]
        public void TheBaiAndTheZang_AreFamiliesOfTheSect_OnItsLands()
        {
            // 📚 audit §4.7: not independent powers on the state's border, but the sect's own families
            var s = Session();
            foreach (var name in new[] { "Famille Bai", "Famille Zang" })
            {
                var family = s.Factions.GetFactionByName(name);
                Assert.AreEqual(Peak, s.Politics.SuzerainOf(name));
                Assert.AreEqual("mount-yunfeng", family.RegionId);
                Assert.IsFalse(s.Factions.AreNeighbours(family, s.Factions.GetFactionByName("Empire de Kun")), "no longer on Kun's border");
            }
        }
    }
}
