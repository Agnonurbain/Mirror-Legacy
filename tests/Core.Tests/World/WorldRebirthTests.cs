using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers' ancestors come back too (the user's rule, 2026-10-03: what befalls the clan befalls the world): a True
    /// Monarch of a power whose essence is intact may be reborn in its power, at the clan's odds; the Chosen may be harvested
    /// while young, before its power can hide it; grown, it stands again at the Golden Core, its Realization its own again if
    /// free or still in its name.
    /// </summary>
    [TestFixture]
    public class WorldRebirthTests
    {
        private static GameContent With(double rebirth, double harvest = 0)
        {
            var b = Fixtures.QuietContent.Balance;
            return Fixtures.QuietContent with
            {
                Balance = b with
                {
                    Ancestors = b.Ancestors with { RebirthChance = rebirth, HarvestChance = harvest },
                    GoldenCore = b.GoldenCore with { TransferChance = 0, TransformationChance = 0 }, // nobody else rises to the lineage meanwhile
                }
            };
        }

        private static GameSession Session(GameContent content) => GameSession.NewGame(new GameSetup { Seed = 1, Content = content });

        private static int YearsToReturn(GameSession s)
        {
            var a = s.Context.Content.Balance.Ancestors;
            return MirrorChronicles.Characters.TaskRules.CultivationAge + a.FoundationYears + a.PurpleMansionYears + a.GoldenCoreYears;
        }

        private static void Years(GameSession s, int years)
        {
            for (int i = 0; i < years; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Rebirths.ProcessYear();
            }
        }

        private static FactionElder Monarch(string name) =>
            new FactionElder { Id = name, Name = name, Realm = CultivationRealm.GoldenCore, Stage = 1, MaxLifespan = 1000 };

        [Test]
        public void APowersTrueMonarch_IsReborn_AndStandsAgainAtTheGoldenCore()
        {
            var s = Session(With(rebirth: 1.0));
            var power = s.Factions.Factions.First();
            s.Events.TriggerElderDied(power, Monarch("ancien"), false);
            Assert.AreEqual(1, s.Rebirths.Pending.Count);
            Years(s, YearsToReturn(s));
            var back = power.Elders.SingleOrDefault(e => e.Name == "ancien");
            Assert.IsNotNull(back, "it stands again in its power");
            Assert.AreEqual(CultivationRealm.GoldenCore, back.Realm);
        }

        [Test]
        public void ADemon_IsNeverReborn()
        {
            var s = Session(With(rebirth: 1.0));
            s.Events.TriggerElderDied(s.Factions.Factions.First(), Monarch("ancien"), true);
            Assert.IsEmpty(s.Rebirths.Pending);
        }

        [Test]
        public void TheChosen_MayBeHarvested_WhileYoung()
        {
            var s = Session(With(rebirth: 1.0, harvest: 1.0));
            var power = s.Factions.Factions.First();
            s.Events.TriggerElderDied(power, Monarch("ancien"), false);
            Years(s, YearsToReturn(s));
            Assert.IsFalse(power.Elders.Any(e => e.Name == "ancien"));
        }

        [Test]
        public void WithoutAnotherPurpleMansionInReach_APowersChosenIsSafe()
        {
            var s = Session(With(rebirth: 1.0, harvest: 1.0));
            var power = s.Factions.Factions.First();
            foreach (var p in s.Factions.Factions.Where(p => p != power)) p.HighestRealm = CultivationRealm.Foundation;
            s.Events.TriggerElderDied(power, Monarch("ancien"), false);
            Years(s, YearsToReturn(s));
            Assert.IsTrue(power.Elders.Any(e => e.Name == "ancien"), "no one could reach its fate");
        }

        [Test]
        public void TheReborn_TakesItsRealizationBack_IfFree()
        {
            var s = Session(With(rebirth: 1.0));
            var power = s.Factions.Factions.First();
            var holder = Monarch("ancien");
            holder.FruitionId = "orthodox-water";
            var state = s.Fruitions.State("orthodox-water");
            if (state.Status == FruitionStatus.Occupied) s.Fruitions.Vacate("orthodox-water");
            Assume.That(s.Fruitions.State("orthodox-water").Status, Is.EqualTo(FruitionStatus.Free));
            s.Fruitions.Claim("orthodox-water", "ancien");
            power.Elders.Add(holder);
            power.Elders.Remove(holder);
            s.Events.TriggerElderDied(power, holder, false);
            Assert.IsNull(s.Fruitions.State("orthodox-water").ReturningHolder, "the rebirth is the ancestor's own, not a second one");
            Years(s, YearsToReturn(s));
            var back = power.Elders.Single(e => e.Name == "ancien");
            Assert.AreEqual("orthodox-water", back.FruitionId);
            Assert.AreEqual("ancien", s.Fruitions.State("orthodox-water").Holder);
        }

        [Test]
        public void ThePendingRebirths_SurviveASave()
        {
            var content = With(rebirth: 1.0);
            var s = Session(content);
            s.Events.TriggerElderDied(s.Factions.Factions.First(), Monarch("ancien"), false);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = content });
            Assert.AreEqual(1, reloaded.Rebirths.Pending.Count);
        }
    }
}
