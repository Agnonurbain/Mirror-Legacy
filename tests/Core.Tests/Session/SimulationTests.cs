using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// Whole-game runs without an engine: a stand-in player assigns tasks each year and the game must stay
    /// consistent for a century. Catches the kind of bugs WL-012 (no marriages) and WL-013 (no breakthroughs)
    /// describe, which only show up over many years.
    /// </summary>
    [TestFixture]
    public class SimulationTests
    {
        private const int Century = 100;

        /// <summary>Stand-in player: cultivators cultivate, adult mortals mine, children stay home.</summary>
        private static void AssignTasks(GameSession s)
        {
            foreach (var m in s.Clan.LivingMembers.ToList())
            {
                var task = SpiritualOrificeRules.CanCultivate(m) ? TaskType.Cultivation
                    : m.Age >= 16 ? TaskType.Mine
                    : TaskType.None;
                s.Tasks.AssignTask(m, task);
            }
        }

        private static void PlayYears(GameSession s, int years, Action<GameSession> afterEachYear = null)
        {
            for (int y = 0; y < years && !s.Victory.IsOver; y++)
            {
                AssignTasks(s);
                s.AdvanceYear();
                afterEachYear?.Invoke(s);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Century_KeepsEveryInvariant(int seed)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = seed });
            var violations = new List<string>();
            PlayYears(s, Century, played => violations.AddRange(Invariants(played)));
            CollectionAssert.IsEmpty(violations);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Century_BringsMarriagesBirthsAndBreakthroughs(int seed)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = seed });
            int breakthroughs = 0;
            s.Events.OnBreakthroughSuccess += (c, realm) => breakthroughs++;

            PlayYears(s, Century);

            bool someoneMarried = s.Clan.Registry.Records.Any(r => r.SpouseID != null && r.Age > 0);
            Assert.IsTrue(someoneMarried && s.Karma.TotalBirths > 0 && breakthroughs > 0,
                $"married: {someoneMarried}, births: {s.Karma.TotalBirths}, breakthroughs: {breakthroughs}");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Decade_KeepsTheClanAlive(int seed)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = seed });
            PlayYears(s, 10);
            Assert.IsTrue(s.Clock.Year == 11 && s.Clan.LivingMembers.Count > 0);
        }

        [Test]
        public void SameSeed_WritesTheSameHistory()
        {
            string History(int seed)
            {
                var s = GameSession.NewGame(new GameSetup { Seed = seed });
                PlayYears(s, 30);
                return string.Join(",", s.Clan.Registry.Records.Select(r => $"{r.FirstName}{r.Age}{r.Realm}{r.RealmStage}{r.IsAlive}"))
                    + "|" + s.Resources.SpiritStones + "|" + s.Mirror.MirrorPower;
            }

            Assert.AreEqual(History(42), History(42));
        }

        private static IEnumerable<string> Invariants(GameSession s)
        {
            int year = s.Clock.Year;
            foreach (var m in s.Clan.LivingMembers)
            {
                string who = $"year {year}: {m.FullName}";
                if (!m.IsAlive) yield return $"{who} is listed among the living but dead";
                if (s.Clan.Registry.FindById(m.ID) == null) yield return $"{who} is missing from the registry";
                if (m.MaxLifespan <= 0) yield return $"{who} has no lifespan";
                if (m.Age >= PowerLadder.LifespanLimit(m)) yield return $"{who} outlived their lifespan";
                if (m.MentalStability < 0 || m.MentalStability > 100) yield return $"{who} has stability {m.MentalStability}";
                int minStage = m.Realm == CultivationRealm.Embryonic ? 0 : 1;
                if (m.RealmStage < minStage || m.RealmStage > PowerLadder.StageCount(m.Realm)) yield return $"{who} stands at stage {m.RealmStage} of {m.Realm}";
                if ((m.Realm > CultivationRealm.Embryonic || m.RealmStage > 0) && !SpiritualOrificeRules.CanCultivate(m)) yield return $"{who} cultivates without an orifice";
                if (!string.IsNullOrEmpty(m.SpouseID) && s.Clan.FindById(m.SpouseID) == null) yield return $"{who} is married to a stranger";
            }

            if (s.Clan.LivingMembers.Count > 0 && s.Clan.GetPatriarch() == null) yield return $"year {year}: no living patriarch";
            if (s.Resources.SpiritStones < 0) yield return $"year {year}: negative treasury";
            if (s.Mirror.MirrorPower < 0 || s.Mirror.MirrorPower > 100) yield return $"year {year}: mirror power {s.Mirror.MirrorPower}";
            foreach (var f in s.Factions.Factions.Where(f => f.RelationWithPlayer < -100 || f.RelationWithPlayer > 100))
                yield return $"year {year}: relation {f.RelationWithPlayer} with {f.Name}";
        }
    }
}
