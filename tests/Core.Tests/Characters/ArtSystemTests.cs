using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Immortal Arts (AUDIT_LORE.md §2; the user's decisions 2026-10-03/04; Miror.txt: the Purple Mansion practises « any of
    /// the 101 Immortal Arts … without their natural constraints »): below it only the gifted practise, and only with the art's
    /// legacy; an ordinary talent's cultivation slows, a genius's gains; a master teaches at full pace; a legacy is lost with
    /// its last master when no one took it up.
    /// </summary>
    [TestFixture]
    public class ArtSystemTests
    {
        private static ArtSettings S => Fixtures.Content.Balance.Arts;

        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        /// <summary>A Qi Cultivator of the wanted gift for the art (identities are tried until one is born so).</summary>
        private static CharacterData Born(GameSession s, ArtGift gift, ImmortalArt art = ImmortalArt.Alchemy)
        {
            for (int i = 0; ; i++)
            {
                var m = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3);
                m.ID = $"art-{gift}-{i}";
                if (ImmortalArtRules.Born(m, art, S) != gift) continue;
                s.Clan.AddMember(m);
                return m;
            }
        }

        [Test]
        public void TheGift_IsRare_AndStable()
        {
            var people = Enumerable.Range(0, 5000).Select(i => new CharacterData { ID = $"p{i}" }).ToList();
            double gifted = people.Count(p => ImmortalArtRules.Born(p, ImmortalArt.Forge, S) != ArtGift.None) / (double)people.Count;
            double geniuses = people.Count(p => ImmortalArtRules.Born(p, ImmortalArt.Forge, S) == ArtGift.Genius) / (double)people.Count;
            Assert.That(gifted, Is.InRange(0.06, 0.12));
            Assert.That(geniuses, Is.InRange(0.003, 0.02));
            Assert.AreEqual(ImmortalArtRules.Born(people[3], ImmortalArt.Forge, S), ImmortalArtRules.Born(people[3], ImmortalArt.Forge, S));
        }

        [Test]
        public void TheUndergroundForge_MakesAGeniusOfTheForge()
        {
            var smith = new CharacterData { ID = "x", FoundationId = "hidden-metal:underground-forge" };
            Assert.AreEqual(ArtGift.Genius, ImmortalArtRules.Gift(smith, ImmortalArt.Forge, S));
        }

        [Test]
        public void WithoutTheLegacy_OrTheGift_NoOnePractises()
        {
            var s = Session();
            var gifted = Born(s, ArtGift.Ordinary);
            var ungifted = Born(s, ArtGift.None);
            StringAssert.Contains("héritage", s.Arts.Practise(gifted, ImmortalArt.Alchemy));
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            Assert.IsNull(s.Arts.Practise(gifted, ImmortalArt.Alchemy));
            StringAssert.Contains("Manoir Pourpre", s.Arts.Practise(ungifted, ImmortalArt.Alchemy));
            ungifted.Realm = CultivationRealm.PurpleMansion;
            Assert.IsNull(s.Arts.Practise(ungifted, ImmortalArt.Alchemy), "the Purple Mansion practises any art");
        }

        [Test]
        public void AnOrdinaryTalent_CultivatesLess_AGenius_More()
        {
            var s = Session();
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var ordinary = Born(s, ArtGift.Ordinary);
            var genius = Born(s, ArtGift.Genius);
            var plain = Born(s, ArtGift.None);
            plain.CurrentTask = TaskType.Cultivation;
            s.Arts.Practise(ordinary, ImmortalArt.Alchemy);
            s.Arts.Practise(genius, ImmortalArt.Alchemy);
            int o = ordinary.CultivationXP, g = genius.CultivationXP, p = plain.CultivationXP;
            s.Cultivation.ProcessYearlyCultivation(plain);
            s.Arts.ProcessYear();
            Assert.Less(ordinary.CultivationXP - o, plain.CultivationXP - p, "the art takes from an ordinary talent's cultivation");
            Assert.GreaterOrEqual(genius.CultivationXP - g, plain.CultivationXP - p, "and feeds a genius's");
            Assert.Greater(ArtSystem.MasteryOf(genius, ImmortalArt.Alchemy), ArtSystem.MasteryOf(ordinary, ImmortalArt.Alchemy));
        }

        [Test]
        public void AMaster_TeachesAtFullPace()
        {
            var s = Session();
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var pupil = Born(s, ArtGift.Ordinary);
            s.Arts.Practise(pupil, ImmortalArt.Alchemy);
            s.Arts.ProcessYear();
            int alone = ArtSystem.MasteryOf(pupil, ImmortalArt.Alchemy);
            var master = Born(s, ArtGift.Ordinary);
            master.ArtMastery[ImmortalArt.Alchemy] = S.MasterAt;
            s.Arts.Practise(pupil, ImmortalArt.Alchemy);
            s.Arts.ProcessYear();
            Assert.Greater(ArtSystem.MasteryOf(pupil, ImmortalArt.Alchemy) - alone, alone);
        }

        [Test]
        public void ALegacy_DiesWithItsLastMaster_IfNoOneTookItUp()
        {
            var s = Session();
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var master = Born(s, ArtGift.Ordinary);
            master.ArtMastery[ImmortalArt.Alchemy] = S.MasterAt;
            s.Clan.Kill(master, DeathCause.OldAge);
            Assert.IsFalse(s.Arts.HoldsLegacy(ImmortalArt.Alchemy), "📚 Li Quantao failed to inherit the alchemy");

            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var second = Born(s, ArtGift.Ordinary);
            second.ArtMastery[ImmortalArt.Alchemy] = S.MasterAt;
            var heir = Born(s, ArtGift.Ordinary);
            heir.ArtMastery[ImmortalArt.Alchemy] = 10;
            s.Clan.Kill(second, DeathCause.OldAge);
            Assert.IsTrue(s.Arts.HoldsLegacy(ImmortalArt.Alchemy), "an apprentice carries it on");
        }

        [Test]
        public void TheLegacies_AndTheMastery_SurviveASave()
        {
            var s = Session();
            s.Arts.GainLegacy(ImmortalArt.Formations);
            var m = Born(s, ArtGift.Ordinary, ImmortalArt.Formations);
            m.ArtMastery[ImmortalArt.Formations] = 33;
            var back = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = s.Context.Content });
            Assert.IsTrue(back.Arts.HoldsLegacy(ImmortalArt.Formations));
            Assert.AreEqual(33, ArtSystem.MasteryOf(back.Clan.FindById(m.ID), ImmortalArt.Formations));
        }

        // ---- Talismans (audit §2.4: « one of the few ways to earn stones », wiki Li_Xuanxuan) ----

        [Test]
        public void ATalismanDrawer_SellsItsYearsTalismans_ForStones()
        {
            var s = Session();
            s.Arts.GainLegacy(ImmortalArt.Talismans);
            var drawer = Born(s, ArtGift.Ordinary, ImmortalArt.Talismans);
            drawer.ArtMastery[ImmortalArt.Talismans] = 40;
            int expected = ImmortalArtRules.TalismanStones(drawer, S);
            Assert.Greater(expected, 0);
            Assert.IsNull(s.Arts.Practise(drawer, ImmortalArt.Talismans));
            int stones = s.Resources.SpiritStones;
            s.Arts.ProcessYear();
            Assert.AreEqual(stones + expected, s.Resources.SpiritStones);
        }

        [Test]
        public void AMasterOrAGenius_DrawsDearerTalismans()
        {
            var apprentice = new CharacterData { ID = "a", TalismanQiId = "holding-profit" };
            apprentice.ArtMastery[ImmortalArt.Talismans] = 10;
            var master = new CharacterData { ID = "m", TalismanQiId = "holding-profit" };
            master.ArtMastery[ImmortalArt.Talismans] = 80;
            Assert.Greater(ImmortalArtRules.TalismanStones(master, S), ImmortalArtRules.TalismanStones(apprentice, S));
            var s = Session();
            var genius = Born(s, ArtGift.Genius, ImmortalArt.Talismans);
            var ordinary = Born(s, ArtGift.Ordinary, ImmortalArt.Talismans);
            genius.ArtMastery[ImmortalArt.Talismans] = ordinary.ArtMastery[ImmortalArt.Talismans] = 40;
            Assert.Greater(ImmortalArtRules.TalismanStones(genius, S), ImmortalArtRules.TalismanStones(ordinary, S));
        }

        [Test]
        public void TheOtherArts_SellNothing()
        {
            var s = Session();
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var m = Born(s, ArtGift.Ordinary);
            Assert.IsNull(s.Arts.Practise(m, ImmortalArt.Alchemy));
            int stones = s.Resources.SpiritStones;
            s.Arts.ProcessYear();
            Assert.AreEqual(stones, s.Resources.SpiritStones);
        }
    }
}
