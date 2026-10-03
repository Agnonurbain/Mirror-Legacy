using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>
    /// The Imperial Way (LORE.md §3.6, §5.9 R20; user decisions 2026-10-03): the clan founds its kingdom as the powers do —
    /// its sect founded, a True Monarch living, three vassals, half the greatest power's weight — then its sovereign
    /// cultivates by governing: a Purple Mansion on the throne sees its odds of the Golden Core grow each year with its
    /// vassals and its people. Forged so, its core is an imperial Surplus where one is open, else a False Golden Core; the
    /// ending is reached.
    /// </summary>
    [TestFixture]
    public class ImperialWayTests
    {
        private static readonly string[] FiveOrthodoxWater =
        {
            "orthodox-water:boundless-sea", "orthodox-water:ford-watcher", "orthodox-water:storm-sky",
            "orthodox-water:dike-guard", "orthodox-water:river-farewell"
        };

        private static GameContent With(double powerShare = 0, int forgeBase = -1)
        {
            var b = Fixtures.QuietContent.Balance;
            return Fixtures.QuietContent with
            {
                Balance = b with
                {
                    PowerLifecycle = b.PowerLifecycle with { KingdomPowerShare = powerShare },
                    GoldenCore = forgeBase < 0 ? b.GoldenCore : b.GoldenCore with { ForgeBaseChance = forgeBase },
                }
            };
        }

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? With() });

        private static CharacterData Monarch(GameSession s)
        {
            var m = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            m.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            s.Clan.AddMember(m);
            return m;
        }

        /// <summary>The sect founded, a True Monarch, and the given number of vassals.</summary>
        private static void Ready(GameSession s, int vassals = 3, bool sect = true, bool monarch = true)
        {
            if (sect) s.Sect.Restore(s.Clock.Year);
            if (monarch) Monarch(s);
            var families = s.Factions.Factions.Where(f => f.Kind == FactionKind.Family).Take(vassals).ToList();
            s.Treaties.RestoreTreaties(families.Select((f, i) =>
                new Treaty($"v{i}", TreatyKind.Vassalage, f.Name, s.Clock.Year, null, false, false, true)).ToList());
        }

        /// <summary>A Purple Mansion at Grand Perfection on the throne, ready to forge toward the Orthodox Water.</summary>
        private static CharacterData Sovereign(GameSession s)
        {
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion, stage: 4);
            m.DivineAbilities = new List<string>(FiveOrthodoxWater);
            m.FoundationId = FiveOrthodoxWater[0];
            m.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
            s.Clan.AddMember(m);
            s.Clan.AppointPatriarch(m);
            s.Knowledge.Reveal(FactKind.GoldSeeking, "orthodox-water", KnowledgeSource.Mirror);
            return m;
        }

        [Test]
        public void TheKingdom_AsksTheSectFirst()
        {
            var s = Session();
            Ready(s, sect: false);
            StringAssert.Contains("secte", s.Imperial.FoundingRefusal());
        }

        [Test]
        public void TheKingdom_AsksALivingTrueMonarch()
        {
            var s = Session();
            Ready(s, monarch: false);
            StringAssert.Contains("Vrai Monarque", s.Imperial.FoundingRefusal());
        }

        [Test]
        public void TheKingdom_AsksThreeVassals()
        {
            var s = Session();
            Ready(s, vassals: 2);
            StringAssert.Contains("vassaux", s.Imperial.FoundingRefusal());
        }

        [Test]
        public void TheKingdom_AsksTheWeightOfTheGreatest()
        {
            var s = Session(With(powerShare: 100));
            Ready(s);
            StringAssert.Contains("puissance", s.Imperial.FoundingRefusal());
        }

        [Test]
        public void TheClan_FoundsItsKingdom_WhenAllHolds()
        {
            var s = Session();
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            Ready(s);
            Assert.IsNull(s.Imperial.Found());
            Assert.IsTrue(s.Imperial.IsKingdom);
            Assert.IsNotNull(s.Imperial.FoundingRefusal(), "a kingdom is founded once");
            Assert.IsTrue(chronicle.Entries.Any(e => e.Contains("royaume")));
        }

        [Test]
        public void Governing_RaisesTheSovereignsOdds_YearAfterYear_UpToACap()
        {
            var s = Session();
            Ready(s);
            s.Imperial.Found();
            var sovereign = Sovereign(s);
            int before = s.GoldenCore.ForgeOdds(sovereign);
            s.Imperial.ProcessYear();
            int one = s.GoldenCore.ForgeOdds(sovereign);
            Assert.Greater(one, before, "he cultivates by governing");
            for (int i = 0; i < 200; i++) s.Imperial.ProcessYear();
            Assert.LessOrEqual(s.Imperial.Merit, s.Context.Content.Balance.ImperialWay.MaxMerit);
        }

        [Test]
        public void MoreVassals_GovernMore()
        {
            var few = Session();
            Ready(few, vassals: 3);
            few.Imperial.Found();
            Sovereign(few);
            few.Imperial.ProcessYear();
            var many = Session();
            Ready(many, vassals: 6);
            many.Imperial.Found();
            Sovereign(many);
            many.Imperial.ProcessYear();
            Assert.Greater(many.Imperial.Merit, few.Imperial.Merit);
        }

        [Test]
        public void WithoutAKingdom_NobodyGoverns()
        {
            var s = Session();
            Ready(s);
            var sovereign = Sovereign(s);
            s.Imperial.ProcessYear();
            Assert.AreEqual(GoldenCoreRules.ForgeChance(sovereign, s.Context.Content), s.GoldenCore.ForgeOdds(sovereign));
        }

        [Test]
        public void ANewSovereign_StartsAnew_AndOthersDoNotGovern()
        {
            var s = Session();
            Ready(s);
            s.Imperial.Found();
            var first = Sovereign(s);
            for (int i = 0; i < 5; i++) s.Imperial.ProcessYear();
            var heir = Sovereign(s);
            Assert.AreEqual(GoldenCoreRules.ForgeChance(first, s.Context.Content), s.GoldenCore.ForgeOdds(first), "off the throne, no governing");
            s.Imperial.ProcessYear();
            Assert.Greater(s.GoldenCore.ForgeOdds(heir), GoldenCoreRules.ForgeChance(heir, s.Context.Content));
            Assert.LessOrEqual(s.GoldenCore.ForgeOdds(heir) - GoldenCoreRules.ForgeChance(heir, s.Context.Content),
                s.Imperial.Merit, "the heir starts anew");
        }

        [Test]
        public void ForgedByTheWay_TheCoreIsImperial_AndTheEndingIsReached()
        {
            var s = Session(With(forgeBase: 99));
            Ready(s);
            s.Imperial.Found();
            var sovereign = Sovereign(s);
            s.Imperial.ProcessYear();
            Assert.IsTrue(s.GoldenCore.Forge(sovereign, "orthodox-water"));
            Assert.AreEqual(CultivationRealm.GoldenCore, sovereign.Realm);
            Assert.IsTrue(sovereign.ImperialCore);
            Assert.That(sovereign.GoldenCore, Is.EqualTo(GoldenCoreState.Surplus).Or.EqualTo(GoldenCoreState.FalseGoldenCore));
            Assert.IsTrue(s.Endings.IsReached("imperial-way"));
        }

        [Test]
        public void ForgedByTheWay_OnAHeldLineage_IsAFalseGoldenCore()
        {
            var s = Session(With(forgeBase: 99));
            Ready(s);
            s.Imperial.Found();
            var sovereign = Sovereign(s);
            var state = s.Fruitions.State("orthodox-water");
            if (state.Status == FruitionStatus.Free) s.Fruitions.Claim("orthodox-water", "Un Étranger");
            else if (state.Status == FruitionStatus.Occupied) s.Fruitions.ChangeHolder("orthodox-water", "Un Étranger");
            s.Imperial.ProcessYear();
            s.GoldenCore.Forge(sovereign, "orthodox-water");
            Assert.AreEqual(GoldenCoreState.FalseGoldenCore, sovereign.GoldenCore, "no open Surplus: a False Golden Core, as the late emperors");
        }

        [Test]
        public void ForgedWithoutGoverning_IsNoImperialCore()
        {
            var s = Session(With(forgeBase: 99));
            Ready(s);
            var sovereign = Sovereign(s);
            s.GoldenCore.Forge(sovereign, "orthodox-water");
            Assert.IsFalse(sovereign.ImperialCore);
            Assert.AreEqual(GoldenCoreState.MetallicEssenceOnly, sovereign.GoldenCore);
            Assert.IsFalse(s.Endings.IsReached("imperial-way"));
        }

        [Test]
        public void TheKingdom_AndItsGoverning_SurviveASave()
        {
            var content = With();
            var s = Session(content);
            Ready(s);
            s.Imperial.Found();
            var sovereign = Sovereign(s);
            for (int i = 0; i < 3; i++) s.Imperial.ProcessYear();
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = content });
            Assert.IsTrue(reloaded.Imperial.IsKingdom);
            Assert.AreEqual(s.Imperial.Merit, reloaded.Imperial.Merit);
            Assert.AreEqual(s.GoldenCore.ForgeOdds(sovereign), reloaded.GoldenCore.ForgeOdds(reloaded.Clan.FindById(sovereign.ID)));
        }
    }
}
