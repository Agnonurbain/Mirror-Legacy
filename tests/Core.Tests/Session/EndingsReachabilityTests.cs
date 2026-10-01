using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// Can the endings beyond the Purple Mansion be reached at all (the user's question, 2026-10-01)? Each test starts from a
    /// prepared state — a Grand Perfection at its realm's peak, its lineage's gold-seeking method known — and plays the real
    /// rules (the forge, the claim, the Transfer) over many seeds, counting the successes: a single one proves the road open.
    /// The rates printed are the rules' own odds, without a pilot's choices.
    /// </summary>
    [TestFixture]
    public class EndingsReachabilityTests
    {
        private const int Seeds = 60;
        private const string OrthodoxWater = "orthodox-water";
        private const string Celadon = "celadon-proclamation"; // a free lineage with a substitute: room for a Surplus

        /// <summary>Five of the Orthodox Water, the Ford Watcher (of Life) condensed last.</summary>
        private static readonly string[] FiveOrthodoxWater =
        {
            "orthodox-water:boundless-sea", "orthodox-water:storm-sky", "orthodox-water:dike-guard",
            "orthodox-water:river-farewell", "orthodox-water:ford-watcher"
        };

        /// <summary>Five of the Celadon Proclamation, its substitute among them: the Surplus (the Mutable Water is held: no Transfer there).</summary>
        private static readonly string[] CeladonWithSubstitute =
        {
            "celadon-proclamation:abyss-ram", "celadon-proclamation:celadon-emblem", "celadon-proclamation:mountain-chain",
            "celadon-proclamation:sovereign-rock", "celadon-proclamation:lesser-yang-essence"
        };

        private static GameSession Session(int seed) => GameSession.NewGame(new GameSetup { Seed = seed, Content = Fixtures.QuietContent });

        /// <summary>A Grand Perfection at the Purple Mansion's peak, holding these abilities, its lineage's gold-seeking known.</summary>
        private static CharacterData Master(GameSession s, string[] abilities, string father = null)
        {
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion, stage: 4);
            m.DivineAbilities = new List<string>(abilities);
            m.FoundationId = abilities[0];
            m.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
            m.FatherID = father;
            s.Clan.AddMember(m);
            s.Knowledge.Reveal(FactKind.GoldSeeking, FoundationRef.Parse(abilities[0]).FruitionId, KnowledgeSource.Mirror);
            return m;
        }

        private static void NextYear(GameSession s) => s.Events.TriggerYearStarted(s.Clock.Year);

        /// <summary>Plays the scenario over many seeds; returns how many reached the ending.</summary>
        private static int Reached(string ending, Action<GameSession> scenario)
        {
            int reached = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var s = Session(seed);
                scenario(s);
                NextYear(s);
                if (s.Endings.IsReached(ending)) reached++;
            }
            TestContext.Progress.WriteLine($"{ending}: {reached}/{Seeds}");
            return reached;
        }

        [Test]
        public void TheFruitionThrone_IsReachable_ByTheForgeAndTheClaim()
        {
            int reached = Reached("fruition-throne", s =>
            {
                var master = Master(s, FiveOrthodoxWater);
                if (s.GoldenCore.Forge(master, OrthodoxWater) && master.IsAlive) s.GoldenCore.ClaimPosition(master);
            });
            Assert.That(reached, Is.GreaterThan(0), "a Realization can be won");
        }

        [Test]
        public void TheRiverBed_IsReachable_ByASurplusTransfer()
        {
            int reached = Reached("river-bed", s =>
            {
                var master = Master(s, CeladonWithSubstitute);
                if (!s.GoldenCore.Forge(master, Celadon) || !master.IsAlive) return;
                s.GoldenCore.ClaimPosition(master);
                if (master.IsAlive && master.GoldenCore == GoldenCoreState.Surplus) s.GoldenCore.Transfer(master);
            });
            Assert.That(reached, Is.GreaterThan(0), "a Surplus may rise to its free lineage's Realization");
        }

        [Test]
        public void TheHouseOfTrueMonarchs_IsReachable_ByThreeForges()
        {
            int reached = Reached("house-of-monarchs", s =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var master = Master(s, FiveOrthodoxWater);
                    s.GoldenCore.Forge(master, OrthodoxWater);
                }
            });
            Assert.That(reached, Is.GreaterThan(0), "three True Monarchs under one roof");
        }

        [Test]
        public void TheGoldenLine_IsReachable_ByThreeGenerationsOfForges()
        {
            int reached = Reached("golden-line", s =>
            {
                var grandfather = Master(s, FiveOrthodoxWater);
                var father = Master(s, FiveOrthodoxWater, grandfather.ID);
                var son = Master(s, FiveOrthodoxWater, father.ID);
                foreach (var m in new[] { grandfather, father, son }) s.GoldenCore.Forge(m, OrthodoxWater);
            });
            Assert.That(reached, Is.GreaterThan(0), "parent to child, three True Monarchs");
        }

        [Test]
        public void TheMirrorAwakened_IsReachable_ByTheSevenShardsAndARealization()
        {
            int reached = Reached("mirror-awakened", s =>
            {
                foreach (var shard in s.Context.Content.Shards) s.Shards.Recover(shard.Id);
                var master = Master(s, FiveOrthodoxWater);
                if (s.GoldenCore.Forge(master, OrthodoxWater) && master.IsAlive) s.GoldenCore.ClaimPosition(master);
            });
            Assert.That(reached, Is.GreaterThan(0), "the mirror whole, and a son of the lake in Heaven");
        }
    }
}
