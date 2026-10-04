using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// World parity for the Essence Gathering Pill (AUDIT_LORE.md §2.6-2.7, the user's rule: what befalls the clan befalls the
    /// world): a power's alchemists keep a store of pills; its elder takes one at the Foundation wall, else gambles — and a
    /// failed wall may kill it; a rival may slip a poisoned pill into its store.
    /// </summary>
    [TestFixture]
    public class ElderEssencePillTests
    {
        private const string Peak = "Secte du Pic des Nuées";

        private static GameSession Session(System.Func<EssencePillSettings, EssencePillSettings> tweak)
        {
            var c = Fixtures.QuietContent;
            var content = c with
            {
                Balance = c.Balance with
                {
                    Elders = c.Balance.Elders with { RiseChance = new[] { 0.0, 1.0, 0.0 } }, // every Qi elder at its term tries the wall
                    Arts = c.Balance.Arts with
                    {
                        EssencePill = tweak(c.Balance.Arts.EssencePill with
                        {
                            PowerPillChance = new System.Collections.Generic.Dictionary<FactionKind, double>(), WorldTaintChance = 0
                        })
                    }
                }
            };
            return GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
        }

        /// <summary>A Qi Cultivation elder of the power, long at its term.</summary>
        private static FactionElder AtTheWall(GameSession s, FactionData power)
        {
            var elder = s.Elders.NewElder(power, CultivationRealm.QiRefinement);
            elder.RealmSinceYear = s.Clock.Year - 1_000;
            elder.BornYear = s.Clock.Year - 20;
            power.Elders.Add(elder);
            return elder;
        }

        private static void AYear(GameSession s)
        {
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.Elders.ProcessYear();
        }

        [Test]
        public void WithAPill_TheElderCrossesTheWall_AndThePillIsSpent()
        {
            var s = Session(p => p with { ElderWithoutPillFactor = 0 });
            var power = s.Factions.GetFactionByName(Peak);
            power.EssencePills = 1;
            var elder = AtTheWall(s, power);
            AYear(s);
            Assert.AreEqual(CultivationRealm.Foundation, elder.Realm);
            Assert.AreEqual(0, power.EssencePills);
        }

        [Test]
        public void WithoutAPill_TheWallIsAGamble_ThatMayKill()
        {
            var s = Session(p => p with { ElderWithoutPillFactor = 0, ElderWallDeathChance = 0 });
            var power = s.Factions.GetFactionByName(Peak);
            power.EssencePills = 0;
            var elder = AtTheWall(s, power);
            AYear(s);
            Assert.AreEqual(CultivationRealm.QiRefinement, elder.Realm, "no pill, no luck: it stays");
            Assert.IsTrue(power.Elders.Contains(elder));

            s = Session(p => p with { ElderWithoutPillFactor = 0, ElderWallDeathChance = 1 });
            power = s.Factions.GetFactionByName(Peak);
            power.EssencePills = 0;
            elder = AtTheWall(s, power);
            AYear(s);
            Assert.IsFalse(power.Elders.Contains(elder), "a failed wall may kill");
        }

        [Test]
        public void APoisonedPill_FailsTheWall()
        {
            var s = Session(p => p with { ElderWithoutPillFactor = 1, WorldTaintChance = 1, ElderWallDeathChance = 0 });
            var power = s.Factions.GetFactionByName(Peak);
            power.EssencePills = 1;
            var elder = AtTheWall(s, power);
            AYear(s);
            Assert.AreEqual(CultivationRealm.QiRefinement, elder.Realm, "a rival's poison: the pill betrays it");
            Assert.AreEqual(0, power.EssencePills);
        }

        [Test]
        public void ThePowersAlchemists_RefineTheirPills()
        {
            var s = Session(p => p with
            {
                PowerPillChance = new System.Collections.Generic.Dictionary<FactionKind, double> { [FactionKind.Sect] = 1.0 }, PowerPillCap = 3
            });
            var power = s.Factions.GetFactionByName(Peak);
            power.EssencePills = 0;
            for (int i = 0; i < 5; i++) AYear(s);
            Assert.AreEqual(3, power.EssencePills, "up to its cap");
        }

        [Test]
        public void AnOlderSave_GivesThePowers_TheirFirstStore()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var data = s.ToSaveData();
            data.PoisonedPills = null; // as written before 2.39
            foreach (var f in data.Factions) f.EssencePills = 0;
            var loaded = GameSession.FromSaveData(data, new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.IsTrue(loaded.Factions.Factions.All(f => f.EssencePills == Fixtures.Content.Balance.Arts.EssencePill.PowerStartPills));
        }
    }
}
