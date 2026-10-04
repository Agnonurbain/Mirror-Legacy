using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Alchemy and the Essence Gathering Pill (AUDIT_LORE.md §2.6-2.7; wiki Li_Chenghui, Li_Jiangxia, Li_Chengliao; the user's
    /// decision 2026-10-04): the Foundation wall may be tried without the pill, at sharply lower odds; the pill must be of the
    /// cultivator's Qi element; a gifted alchemist of the clan refines it from herbs, once a year.
    /// </summary>
    [TestFixture]
    public class AlchemySystemTests
    {
        private static EssencePillSettings P => Fixtures.Content.Balance.Arts.EssencePill;

        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Resources.AddHerbs(1_000);
            s.Resources.AddSpiritStones(1_000);
            return s;
        }

        private static CharacterData AtTheWall(GameSession s)
        {
            var c = Fixtures.Cultivator(age: 30, realm: CultivationRealm.QiRefinement, stage: 9);
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            s.Resources.AddQi(c.QiId, 1);
            s.Clan.AddMember(c);
            return c;
        }

        /// <summary>A Qi Cultivator with no gift for alchemy (identities are tried until one is born so).</summary>
        private static CharacterData Ungifted(GameSession s)
        {
            for (int i = 0; ; i++)
            {
                var m = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
                m.ID = $"plain-{i}";
                if (ImmortalArtRules.Gift(m, ImmortalArt.Alchemy, s.Context.Content.Balance.Arts) != ArtGift.None) continue;
                s.Clan.AddMember(m);
                return m;
            }
        }

        /// <summary>A gifted alchemist, apprenticed, of the clan that holds alchemy's legacy.</summary>
        private static CharacterData Alchemist(GameSession s)
        {
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var a = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            a.TalismanQiId = "holding-profit"; // the hundred arts' talisman Qi: a gift
            a.ArtMastery[ImmortalArt.Alchemy] = P.Mastery;
            s.Clan.AddMember(a);
            return a;
        }

        private static Element ElementOf(GameSession s, CharacterData c) => AlchemySystem.ElementFor(c, s.Context.Content).Value;

        [Test]
        public void WithoutThePill_TheWallIsAGamble()
        {
            var s = Session();
            var c = AtTheWall(s);
            int without = s.Breakthroughs.TrialSuccessRate(c);
            s.Alchemy.GainEssencePills(ElementOf(s, c), 1);
            int with = s.Breakthroughs.TrialSuccessRate(c);
            Assert.AreEqual(System.Math.Max(1, with - P.WithoutPillPenalty), without);
            Assert.Less(without, with);
        }

        [Test]
        public void APillOfAnotherElement_DoesNotServe()
        {
            var s = Session();
            var c = AtTheWall(s);
            int without = s.Breakthroughs.TrialSuccessRate(c);
            var other = System.Enum.GetValues<Element>().First(e => e != ElementOf(s, c));
            s.Alchemy.GainEssencePills(other, 3);
            Assert.AreEqual(without, s.Breakthroughs.TrialSuccessRate(c));
        }

        [Test]
        public void TheAttempt_ConsumesThePill()
        {
            var s = Session();
            var c = AtTheWall(s);
            s.Alchemy.GainEssencePills(ElementOf(s, c), 2);
            Assert.IsNotNull(s.Breakthroughs.AttemptBreakthrough(c));
            Assert.AreEqual(1, s.Alchemy.EssencePillsOf(ElementOf(s, c)));
        }

        [Test]
        public void AGiftedAlchemist_RefinesAPill_OnceAYear_FromHerbs()
        {
            var s = Session();
            var a = Alchemist(s);
            int herbs = s.Resources.MedicinalHerbs;
            Assert.IsNull(s.Alchemy.RefineEssencePill(a.ID, Element.Fire));
            Assert.AreEqual(1, s.Alchemy.EssencePillsOf(Element.Fire));
            Assert.AreEqual(herbs - P.Herbs, s.Resources.MedicinalHerbs);
            StringAssert.Contains("cette année", s.Alchemy.RefineEssencePill(a.ID, Element.Fire));
        }

        [Test]
        public void NoOne_RefinesWithoutTheLegacy_TheGift_OrTheHerbs()
        {
            var s = Session();
            var plain = Ungifted(s);
            StringAssert.Contains("héritage", s.Alchemy.RefineEssencePill(plain.ID, Element.Fire));
            var a = Alchemist(s);
            StringAssert.Contains("don", s.Alchemy.RefineEssencePill(plain.ID, Element.Fire));
            s.Resources.ConsumeHerbs(s.Resources.MedicinalHerbs);
            StringAssert.Contains("herbes", s.Alchemy.RefineEssencePill(a.ID, Element.Fire));
        }

        [Test]
        public void ThePills_AreSaved()
        {
            var s = Session();
            s.Alchemy.GainEssencePills(Element.Water, 2);
            var loaded = GameSession.FromSaveData(s.ToSaveData(), new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual(2, loaded.Alchemy.EssencePillsOf(Element.Water));
        }

        // ---- Poison and sabotage (audit §2.7) ----

        private static FactionData Hostile(GameSession s)
        {
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 0;
            var foe = s.Factions.Factions.First();
            foe.RelationWithPlayer = -80;
            return foe;
        }

        [Test]
        public void AHostilePower_MaySlipAPoisonedPill_IntoTheStore()
        {
            var c = Fixtures.QuietContent;
            var s = GameSession.NewGame(new GameSetup
            {
                Seed = 1,
                Content = c with { Balance = c.Balance with { Arts = c.Balance.Arts with { EssencePill = P with { TaintChance = 1 } } } }
            });
            var foe = Hostile(s);
            s.Alchemy.GainEssencePills(Element.Water, 2);
            s.Alchemy.ProcessYear();
            Assert.AreEqual(1, s.Alchemy.Poisoned.Count);
            Assert.AreEqual(foe.Name, s.Alchemy.Poisoned.Single().Power);
            Assert.AreEqual(2, s.Alchemy.EssencePillsOf(Element.Water), "a poisoned pill looks like any other");
        }

        [Test]
        public void APoisonedPill_FailsTheWall()
        {
            var s = Session();
            var c = AtTheWall(s);
            var e = ElementOf(s, c);
            s.Alchemy.GainEssencePills(e, 1);
            s.Alchemy.Poison(e, Hostile(s).Name);
            Assert.AreEqual(s.Breakthroughs.TrialSuccessRate(c), s.Breakthroughs.TrialSuccessRate(c), "the odds look whole");
            Assert.AreNotEqual(BreakthroughOutcome.Success, s.Breakthroughs.AttemptBreakthrough(c));
            Assert.AreEqual(0, s.Alchemy.EssencePillsOf(e));
            Assert.IsEmpty(s.Alchemy.Poisoned);
        }

        [Test]
        public void AnAdeptAlchemist_FindsThePoison_AndWhoseItIs()
        {
            var s = Session();
            var a = Alchemist(s);
            var foe = Hostile(s);
            s.Alchemy.GainEssencePills(Element.Fire, 2);
            s.Alchemy.Poison(Element.Fire, foe.Name);
            StringAssert.Contains("adepte", s.Alchemy.Examine(a.ID, out _), "an apprentice cannot tell");
            a.ArtMastery[ImmortalArt.Alchemy] = s.Context.Content.Balance.Arts.AdeptAt;
            Assert.IsNull(s.Alchemy.Examine(a.ID, out var culprits));
            CollectionAssert.AreEqual(new[] { foe.Name }, culprits);
            Assert.AreEqual(1, s.Alchemy.EssencePillsOf(Element.Fire), "the poisoned pill is thrown away");
            Assert.IsEmpty(s.Alchemy.Poisoned);
            Assert.Greater(s.Suspicion.ClanDistrust(foe.Name), 0);
        }

        [Test]
        public void ThePoisonedPills_AreSaved()
        {
            var s = Session();
            s.Alchemy.GainEssencePills(Element.Water, 1);
            s.Alchemy.Poison(Element.Water, "X");
            var loaded = GameSession.FromSaveData(s.ToSaveData(), new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual(1, loaded.Alchemy.Poisoned.Count);
        }
    }
}
