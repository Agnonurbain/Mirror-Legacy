using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers' elders (the living world, step A, user decision 2026-10-01): every power counts named and unnamed elders
    /// who age, die, rise; its highest realm follows them. A Purple Mansion at its peak is precious to any faction: it tries
    /// the Golden Core only from good odds or in its last years, and a failure kills it. A dead figure knows nothing more.
    /// </summary>
    [TestFixture]
    public class ElderSystemTests
    {
        private const string Peak = "Secte du Pic des Nuées";
        private const string Gate = "Porte du Roc Obscur"; // a Purple Mansion gate

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Fixtures.QuietContent });

        private static GameContent With(System.Func<ElderSettings, ElderSettings> tweak) => Fixtures.QuietContent with
        {
            Balance = Fixtures.QuietContent.Balance with { Elders = tweak(Fixtures.QuietContent.Balance.Elders) }
        };

        private static void Years(GameSession s, int years)
        {
            for (int i = 0; i < years; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Elders.ProcessYear();
            }
        }

        [Test]
        public void EveryPower_HasElders_ItsStrongestAtItsHighestRealm()
        {
            var s = Session();
            foreach (var power in s.Factions.Factions)
            {
                Assert.IsNotEmpty(power.Elders, power.Name);
                Assert.AreEqual(power.HighestRealm, power.Elders.Max(e => e.Realm), power.Name);
            }
        }

        [Test]
        public void TheNamedFigures_AreTheElders_OfTheirPower()
        {
            var s = Session();
            foreach (var figure in s.Context.Content.Figures)
                Assert.IsTrue(s.Factions.GetFactionByName(figure.FactionName).Elders.Any(e => e.FigureId == figure.Id && e.Name == figure.Name), figure.Name);
        }

        [Test]
        public void AnElder_DiesAtTheEndOfItsLife()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName(Peak);
            var elder = power.Elders.First();
            elder.BornYear = s.Clock.Year - elder.MaxLifespan; // its last year
            Years(s, 1);
            Assert.IsFalse(power.Elders.Contains(elder));
        }

        [Test]
        public void APower_FallsInRealm_WhenItsOnlyTopElderDies()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName(Gate);
            power.Elders.RemoveAll(e => e.Realm != power.HighestRealm); // keep only its tops
            foreach (var e in power.Elders) e.BornYear = s.Clock.Year - e.MaxLifespan;
            power.Elders.Add(new FactionElder { Id = "junior", Name = "Junior", Realm = CultivationRealm.Foundation, Stage = 1,
                BornYear = s.Clock.Year - 50, MaxLifespan = 300, RealmSinceYear = s.Clock.Year });
            Years(s, 1);
            Assert.AreEqual(CultivationRealm.Foundation, power.HighestRealm, "its Purple Mansion gone, the gate falls");
        }

        [Test]
        public void AJunior_RisesInTime()
        {
            var s = Session(With(e => e with { RiseChance = new[] { 1.0, 1.0, 1.0 } }));
            var power = s.Factions.GetFactionByName("Famille Lou");
            var junior = new FactionElder { Id = "j", Name = "J", Realm = CultivationRealm.QiRefinement, Stage = 1,
                BornYear = s.Clock.Year - 40, MaxLifespan = 200, RealmSinceYear = s.Clock.Year - 1000 };
            power.Elders.Add(junior);
            power.EssencePills = 10; // a pill for every elder at the wall (audit §2.6)
            Years(s, 1);
            Assert.AreEqual(CultivationRealm.Foundation, junior.Realm);
        }

        private static FactionElder PeakMansion(GameSession s, FactionData power, double odds, int yearsLeft)
        {
            var e = new FactionElder { Id = "peak", Name = "Peak", Realm = CultivationRealm.PurpleMansion, Stage = 5, MaxLifespan = 500,
                RealmSinceYear = s.Clock.Year - 1000, GoldenCoreOdds = odds, Perfected = true }; // at its Grand Perfection
            e.BornYear = s.Clock.Year + 1 - (e.MaxLifespan - yearsLeft);
            power.Elders.Add(e);
            return e;
        }

        [Test]
        public void AMansionShortOfItsGrandPerfection_NeverTries_EvenAtItsEnd()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName(Gate);
            var elder = PeakMansion(s, power, odds: 1.0, yearsLeft: 5);
            elder.Perfected = false; // five abilities it never gathered
            Years(s, 1);
            Assert.IsTrue(power.Elders.Contains(elder) && elder.Realm == CultivationRealm.PurpleMansion);
        }

        [Test]
        public void APreciousMansion_NeverGambles_OnPoorOdds()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName(Gate);
            var elder = PeakMansion(s, power, odds: 0.4, yearsLeft: 200);
            Years(s, 1);
            Assert.IsTrue(power.Elders.Contains(elder) && elder.Realm == CultivationRealm.PurpleMansion);
        }

        [Test]
        public void APreciousMansion_Rises_FromGoodOdds()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName(Gate);
            var elder = PeakMansion(s, power, odds: 1.0, yearsLeft: 200);
            Years(s, 1);
            Assert.AreEqual(CultivationRealm.GoldenCore, elder.Realm);
            Assert.AreEqual(CultivationRealm.GoldenCore, power.HighestRealm, "the gate rises with its elder");
        }

        [Test]
        public void AMansionNearItsEnd_TriesWhateverTheOdds_AndMayDie()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName(Gate);
            var elder = PeakMansion(s, power, odds: 0.0, yearsLeft: 10); // a certain failure: the demon is born
            Years(s, 1);
            Assert.IsFalse(power.Elders.Contains(elder));
        }

        [Test]
        public void APower_RaisesNewElders_ToKeepItsRanks()
        {
            var s = Session(With(e => e with { NewElderChance = 1.0 }));
            var power = s.Factions.GetFactionByName("Famille Lou");
            power.Elders.Clear();
            Years(s, 1);
            Assert.IsNotEmpty(power.Elders);
        }

        [Test]
        public void ADeadFigure_KnowsNothingMore_OfTheMirror()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName(Peak);
            var names = s.Context.Content.Figures.Where(f => f.FactionName == Peak).Select(f => f.Name).ToList();
            power.Elders.RemoveAll(e => e.FigureId != null); // every figure of it dead
            Assert.That(names, Has.No.Member(s.Lore.KnowerOf(Peak)), "a secret dies with its keeper");
        }

        [Test]
        public void TheElders_SurviveASave()
        {
            var s = Session();
            Years(s, 3);
            var before = s.Factions.GetFactionByName(Peak).Elders.Select(e => (e.Id, e.Realm, e.BornYear)).ToList();
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            CollectionAssert.AreEqual(before, reloaded.Factions.GetFactionByName(Peak).Elders.Select(e => (e.Id, e.Realm, e.BornYear)).ToList());
        }

        [Test]
        public void TheChronicle_TellsTheDeathOfAGreatElder()
        {
            var s = Session();
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            var power = s.Factions.GetFactionByName(Peak);
            var top = power.Elders.OrderByDescending(e => e.Realm).First();
            top.BornYear = s.Clock.Year - top.MaxLifespan;
            Years(s, 1);
            Assert.IsTrue(chronicle.Entries.Any(e => e.Contains(top.Name)), "a Purple Mansion or above is news");
        }

        // ---- Who knows the mirror: the centuries at the summit (user decision 2026-10-01) ----

        [Test]
        public void ANewlyRisenTrueMonarch_KnowsNothingOfTheMirror_TheCenturiesAtTheSummitTeach()
        {
            var s = Session();
            var settings = s.Context.Content.Balance.MirrorLore;
            var risen = new FactionElder { Id = "risen", Realm = CultivationRealm.GoldenCore, BornYear = s.Clock.Year - 450, RealmSinceYear = s.Clock.Year };
            Assert.AreEqual(settings.KnowChance[CultivationRealm.GoldenCore], MirrorChronicles.World.MirrorLoreRules.ElderChance(risen, s.Clock.Year, settings), 1e-9,
                "its old age at the Purple Mansion teaches nothing of the mirror");
            Assert.Greater(MirrorChronicles.World.MirrorLoreRules.ElderChance(risen, s.Clock.Year + 300, settings),
                MirrorChronicles.World.MirrorLoreRules.ElderChance(risen, s.Clock.Year, settings), "three centuries at the summit do");
        }

        [Test]
        public void AnAncientElder_KeepsItsLore()
        {
            var s = Session();
            var settings = s.Context.Content.Balance.MirrorLore;
            var ancient = new FactionElder { Id = "ancient", Realm = CultivationRealm.GoldenCore, Ancient = true, RealmSinceYear = s.Clock.Year };
            Assert.AreEqual(MirrorChronicles.World.MirrorLoreRules.KnowChance(CultivationRealm.GoldenCore, settings.UnknownAge + s.Clock.Year, settings),
                MirrorChronicles.World.MirrorLoreRules.ElderChance(ancient, s.Clock.Year, settings), 1e-9);
        }

        [Test]
        public void AFamily_WhoseElderJustRose_DoesNotKnowTheMirror()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName("Famille Kang");
            power.Elders.Clear();
            power.Elders.Add(new FactionElder { Id = "kang-risen", Name = "Kang Risen", Realm = CultivationRealm.GoldenCore, Stage = 1,
                BornYear = s.Clock.Year - 450, MaxLifespan = 1000, RealmSinceYear = s.Clock.Year });
            power.HighestRealm = CultivationRealm.GoldenCore;
            Assume.That(MirrorChronicles.World.MirrorLoreRules.Draw(1, "kang-risen"), Is.GreaterThan(s.Context.Content.Balance.MirrorLore.KnowChance[CultivationRealm.GoldenCore]));
            Assert.IsFalse(s.Lore.Knows(power.Name));
        }

        // ---- The world stays the world (2026-10-01: within five centuries every power had become a Golden Core) ----

        /// <summary>
        /// A rate over ten worlds, not three fixed seeds: a precious Purple Mansion gambles at the end of its life, so one
        /// world may see a run of luck (2026-10-01: seed 3 drew eight True Monarchs, the mean was one).
        /// </summary>
        [Test]
        public void OverFiveCenturies_TheGoldenCoresStayRare_AndThePurpleMansionsFew()
        {
            double goldenGained = 0, mansionsRatio = 0, greatGained = 0;
            const int Worlds = 10;
            for (int seed = 1; seed <= Worlds; seed++)
            {
                var s = GameSession.NewGame(new GameSetup { Seed = seed, Content = Fixtures.QuietContent });
                int CountRealm(CultivationRealm r) => s.Factions.Factions.SelectMany(f => f.Elders).Count(e => e.Realm == r);
                int Great() => s.Factions.Factions.Count(f => f.HighestRealm >= CultivationRealm.GoldenCore);
                int golden = CountRealm(CultivationRealm.GoldenCore), mansions = CountRealm(CultivationRealm.PurpleMansion), great = Great();
                for (int year = 0; year < 500; year++)
                {
                    s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                    s.Elders.ProcessYear();
                    s.PowerEconomy.ProcessYear();
                }
                goldenGained += CountRealm(CultivationRealm.GoldenCore) - golden;
                mansionsRatio += CountRealm(CultivationRealm.PurpleMansion) / (double)mansions;
                greatGained += Great() - great;
            }
            Assert.That(goldenGained / Worlds, Is.LessThanOrEqualTo(3), "a True Monarch is rarissime");
            Assert.That(mansionsRatio / Worlds, Is.InRange(0.5, 1.5), "the Purple Mansions neither vanish nor swarm");
            Assert.That(greatGained / Worlds, Is.LessThanOrEqualTo(3), "a few great powers, not all");
        }
    }
}
