using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The Fruitions of the world (the living world, step D, user decisions 2026-10-01). The world's holders pass — death
    /// or the Struggle of the Five Faces — save the eternal; one may come back reborn, its lineage free meanwhile and its
    /// own again only if nobody took it. A power's elder risen to the Golden Core claims a free Realization. A lineage
    /// freed opens a race: the powers with a Grand Perfection dare sooner, and the clan may sabotage a contender.
    /// (The restoration of broken lineages is paused, the user's choice.)
    /// </summary>
    [TestFixture]
    public class WorldFruitionsTests
    {
        private const string Mutable = "mutable-water";   // held by Tan Qing
        private const string FirstJade = "first-jade";    // held by the Venerable Lingxu, an elder of the Pale Moon
        private const string Dawnlight = "dawnlight";     // the Immortal Xiaoyun: she passes only at the end of time

        /// <summary>The content with these world settings, and no Surplus or Intercalary rising (each test of moves asks for them).</summary>
        private static GameContent With(System.Func<WorldFruitionSettings, WorldFruitionSettings> tweak) => Fixtures.QuietContent with
        {
            Balance = Fixtures.QuietContent.Balance with
            {
                WorldFruitions = tweak(Fixtures.QuietContent.Balance.WorldFruitions),
                GoldenCore = Fixtures.QuietContent.Balance.GoldenCore with { TransferChance = 0, TransformationChance = 0 }
            }
        };

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Fixtures.QuietContent });

        private static void Year(GameSession s)
        {
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.WorldFruitions.ProcessYear();
        }

        [Test]
        public void AWorldHolder_Passes_AndItsLineageIsFree()
        {
            var s = Session(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 0 }));
            Year(s);
            Assert.AreEqual(FruitionStatus.Free, s.Fruitions.State(Mutable).Status, "Tan Qing is no more");
        }

        [Test]
        public void TheEternal_NeverPass()
        {
            var s = Session(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 0 }));
            Year(s);
            Assert.AreEqual(FruitionStatus.Occupied, s.Fruitions.State(Dawnlight).Status, "the Immortal Xiaoyun passes only at the end of time");
        }

        [Test]
        public void AClanHolder_IsNotTheWorldsToTake()
        {
            var s = Session(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 0 }));
            var member = Fixtures.Cultivator(realm: CultivationRealm.GoldenCore);
            member.GoldenCore = GoldenCoreState.Realization;
            member.FruitionId = "orthodox-water";
            s.Clan.AddMember(member);
            s.Fruitions.Claim("orthodox-water", member.FullName);
            Year(s);
            Assert.AreEqual(FruitionStatus.Occupied, s.Fruitions.State("orthodox-water").Status, "the clan's own holder lives by the clan's rules");
        }

        [Test]
        public void AnElderHolder_FreesItsLineage_WhenHeDies()
        {
            var s = Session(With(w => w with { ReincarnationChance = 0 }));
            var moon = s.Factions.GetFactionByName("Secte de la Lune Pâle");
            var lingxu = moon.Elders.Single(e => e.Name == "Vénérable Lingxu");
            Assert.AreEqual(FirstJade, lingxu.FruitionId, "the Venerable holds the First Jade");
            lingxu.BornYear = s.Clock.Year - lingxu.MaxLifespan;
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.Elders.ProcessYear();
            Assert.AreEqual(FruitionStatus.Free, s.Fruitions.State(FirstJade).Status);
        }

        [Test]
        public void AHolderReborn_TakesItsLineageBack_IfStillFree()
        {
            var s = Session(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 1.0, ReturnYears = 3 }));
            Year(s);
            Assert.AreEqual(FruitionStatus.Free, s.Fruitions.State(Mutable).Status, "free while he comes back");
            Assert.AreEqual("Tan Qing", s.Fruitions.State(Mutable).ReturningHolder);
            for (int i = 0; i < 3; i++) Year(s);
            Assert.AreEqual(FruitionStatus.Occupied, s.Fruitions.State(Mutable).Status);
            Assert.AreEqual("Tan Qing", s.Fruitions.State(Mutable).Holder, "reborn, he takes it back");
        }

        [Test]
        public void AHolderReborn_FindsHisLineageTaken()
        {
            var s = Session(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 1.0, ReturnYears = 3 }));
            Year(s);
            s.Fruitions.Claim(Mutable, "Usurper"); // an elder of a power: he passes with his elder, not by the world's draw
            s.Factions.GetFactionByName("Porte du Roc Obscur").Elders.Add(new FactionElder { Id = "usurper", Name = "Usurper",
                Realm = CultivationRealm.GoldenCore, Stage = 1, BornYear = s.Clock.Year, MaxLifespan = 1000, FruitionId = Mutable });
            for (int i = 0; i < 3; i++) Year(s);
            Assert.AreEqual("Usurper", s.Fruitions.State(Mutable).Holder, "too late: another holds it now");
        }

        [Test]
        public void AnElderRisenToTheGoldenCore_ClaimsAFreeRealization()
        {
            var s = Session();
            var gate = s.Factions.GetFactionByName("Porte du Roc Obscur");
            var elder = new FactionElder { Id = "risen", Name = "Risen", Realm = CultivationRealm.GoldenCore, Stage = 1, BornYear = 0, MaxLifespan = 1000 };
            gate.Elders.Add(elder);
            s.Events.TriggerElderRose(gate, elder);
            Assert.IsNotNull(elder.FruitionId, "it asked Heaven for a position");
            Assert.AreEqual("Risen", s.Fruitions.State(elder.FruitionId).Holder);
        }

        [Test]
        public void AFreedLineage_OpensARace()
        {
            var s = Session(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 0 }));
            Year(s);
            Assert.IsTrue(s.WorldFruitions.Races.Any(r => r.FruitionId == Mutable));
        }

        private static FactionElder Perfected(GameSession s, FactionData power, double odds)
        {
            var e = new FactionElder { Id = "contender", Name = "Contender", Realm = CultivationRealm.PurpleMansion, Stage = 5, BornYear = s.Clock.Year - 200,
                MaxLifespan = 500, RealmSinceYear = s.Clock.Year - 1000, GoldenCoreOdds = odds, Perfected = true };
            power.Elders.Add(e);
            return e;
        }

        [Test]
        public void InARace_AGrandPerfection_DaresSooner()
        {
            var s = Session(With(w => w with { RaceOdds = 0.4 }));
            var gate = s.Factions.GetFactionByName("Porte du Roc Obscur");
            var contender = Perfected(s, gate, odds: 0.5); // below the patient 60%: outside a race it would wait
            s.Fruitions.Vacate(Mutable);
            s.WorldFruitions.OpenRace(Mutable);
            Year(s);
            Assert.IsTrue(!gate.Elders.Contains(contender) || contender.Realm == CultivationRealm.GoldenCore, "it tried");
        }

        [Test]
        public void TheClan_SabotagesAContender()
        {
            var s = Session();
            var lou = s.Factions.GetFactionByName("Famille Lou"); // a Foundation family: a weak guard
            var contender = Perfected(s, lou, odds: 0.5);
            s.Fruitions.Vacate(Mutable);
            s.WorldFruitions.OpenRace(Mutable);
            var team = Enumerable.Range(0, 3).Select(_ => { var m = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: 3); s.Clan.AddMember(m); return m.ID; }).ToList();
            Assert.Greater(s.WorldFruitions.SabotageChance(lou.Name, team), 0.5);
            string outcome = s.WorldFruitions.Sabotage(lou.Name, team);
            Assert.IsTrue(outcome == null || outcome.Contains("échoue"), outcome);
            if (outcome == null) Assert.Less(contender.GoldenCoreOdds, 0.5, "its preparation is spoilt");
        }

        [Test]
        public void ASabotage_NeedsARace_AndAContender()
        {
            var s = Session();
            var team = new[] { s.Clan.LivingMembers.First().ID };
            Assert.IsNotNull(s.WorldFruitions.Sabotage("Famille Lou", team));
        }

        [Test]
        public void TheRaces_AndTheReturns_SurviveASave()
        {
            var content = With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 1.0 });
            var s = Session(content);
            Year(s);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = content });
            Assert.AreEqual("Tan Qing", reloaded.Fruitions.State(Mutable).ReturningHolder);
            Assert.AreEqual(s.WorldFruitions.Races.Count, reloaded.WorldFruitions.Races.Count);
        }

        [Test]
        public void TheChronicle_TellsALineageFreed()
        {
            var s = Session(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 0 }));
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            Year(s);
            Assert.IsTrue(chronicle.Entries.Any(e => e.Contains("Tan Qing")));
        }

        /// <summary>The Transfer and the Transformation sure: a test of who rises, not of the odds.</summary>
        private static GameContent SureMoves(GameContent c) => c with
        {
            Balance = c.Balance with { GoldenCore = c.Balance.GoldenCore with { TransferChance = 100, TransformationChance = 100 } }
        };

        // ---- The positions move: a Surplus's Transfer, an Intercalary's Transformation (LORE.md §5.5.1) ----

        [Test]
        public void AFreedRealization_IsTakenByItsSurplus_ByTransfer()
        {
            var s = Session(SureMoves(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 0 })));
            Year(s); // Ao Ming passes: the Gathered Water is free, and its four Surplus try
            var state = s.Fruitions.State("gathered-water");
            Assert.AreEqual(FruitionStatus.Occupied, state.Status, "a Surplus rose by Transfer (sure, by the test's roll)");
            Assert.That(new[] { "Ao Yue", "Ao Xi", "Ao Zai", "Ao Lin" }, Has.Member(state.Holder));
        }

        [Test]
        public void AHolderRisenByTransfer_NeverRisesTwice()
        {
            var s = Session(SureMoves(With(w => w with { HolderPassChance = 1.0, ReincarnationChance = 0 })));
            Year(s);
            string first = s.Fruitions.State("gathered-water").Holder;
            Year(s); // he passes in turn: the next Surplus rises, never him again
            Assert.AreNotEqual(first, s.Fruitions.State("gathered-water").Holder);
        }

        // ---- The hidden and the suspected (LORE.md §6.8) ----

        [Test]
        public void AHiddenLineage_HasATrueStatus_TheMirrorReveals()
        {
            var s = Session();
            Assume.That(s.Fruitions.State("violet-qi").Status, Is.EqualTo(FruitionStatus.Hidden));
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            Assert.IsNull(s.WorldFruitions.Reveal("violet-qi"));
            Assert.That(s.Fruitions.State("violet-qi").Status, Is.AnyOf(FruitionStatus.Free, FruitionStatus.Occupied, FruitionStatus.Broken));
            Assert.AreEqual(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower - s.Context.Content.Balance.WorldFruitions.RevealMirrorCost, s.Mirror.MirrorPower);
        }

        [Test]
        public void ASuspectedHolder_IsConfirmedOrCleared_ByTheMirror()
        {
            var s = Session();
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            Assert.IsNull(s.WorldFruitions.Reveal("exiled-qi"));
            var state = s.Fruitions.State("exiled-qi");
            Assert.IsTrue(state.Status == FruitionStatus.Free || (state.Status == FruitionStatus.Occupied && state.Holder == "Marquis de la Nuit"));
        }

        [Test]
        public void TheMirror_RevealsOnlyWhatIsHidden_AndAtItsPrice()
        {
            var s = Session();
            s.Mirror.Restore(0, 0);
            Assert.IsNotNull(s.WorldFruitions.Reveal("violet-qi"), "the mirror lacks the power");
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            Assert.IsNotNull(s.WorldFruitions.Reveal(Mutable), "nothing hidden there");
        }

        [Test]
        public void TheTrueStatus_SurvivesASave()
        {
            var s = Session();
            var hidden = s.Fruitions.State("violet-qi");
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            Assert.AreEqual(hidden.TrueStatus, reloaded.Fruitions.State("violet-qi").TrueStatus);
        }
    }
}
