using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers' treasures and artifacts (the user's rule, 2026-10-03: what befalls the clan befalls the world): their
    /// True Monarchs condense Dharma Treasures, their holders mortgage them into Rank Designations — dangerous without a
    /// master of the lineage, until unsealed —, and they forge artifacts of their craft; all of it weighs in their war
    /// strength.
    /// </summary>
    [TestFixture]
    public class WorldArsenalTests
    {
        private static GameContent With(System.Func<WorldArsenalSettings, WorldArsenalSettings> tweak)
        {
            var b = Fixtures.QuietContent.Balance;
            return Fixtures.QuietContent with { Balance = b with { WorldArsenal = tweak(b.WorldArsenal) } };
        }

        private static GameSession Session(GameContent content) => GameSession.NewGame(new GameSetup { Seed = 1, Content = content });

        private static FactionElder Elder(string id, CultivationRealm realm) =>
            new FactionElder { Id = id, Name = id, Realm = realm, Stage = 1, MaxLifespan = 1000 };

        private static double Strength(GameSession s, FactionData p) => WarRules.Strength(p, s.Context.Content.Balance.Wars);

        [Test]
        public void ATrueMonarch_CondensesATreasure_ThatWeighsInItsPowersWars()
        {
            var s = Session(With(a => a with { ElderCondenseChance = 1.0, ElderDesignationChance = 0, ForgeChance = 0, FamilyForgeChance = 0 }));
            var power = s.Factions.Factions.First();
            var monarch = Elder("monarch", CultivationRealm.GoldenCore);
            power.Elders.Add(monarch);
            double before = Strength(s, power);
            s.Arsenal.ProcessYear();
            Assert.IsTrue(monarch.HasDharmaTreasure);
            Assert.Greater(Strength(s, power), before);
        }

        [Test]
        public void AHolder_MortgagesItsTreasure_IntoADesignationThatOutlivesIt()
        {
            var s = Session(With(a => a with { ElderCondenseChance = 1.0, ElderDesignationChance = 1.0, ForgeChance = 0, FamilyForgeChance = 0, UnsealChance = 0, MasterlessStrikeChance = 0 }));
            var power = s.Factions.Factions.First();
            var holder = Elder("holder", CultivationRealm.GoldenCore);
            holder.FruitionId = "orthodox-water";
            holder.HasDharmaTreasure = true;
            power.Elders.Add(holder);
            s.Arsenal.ProcessYear();
            Assert.AreEqual(1, power.Designations.Count);
            power.Elders.Remove(holder);
            s.Arsenal.ProcessYear();
            Assert.AreEqual(1, power.Designations.Count, "it guards on after its master");
            Assert.Greater(power.DomainStrength, 0);
        }

        [Test]
        public void AMasterlessDesignation_Strikes_UntilUnsealed()
        {
            var s = Session(With(a => a with { ElderCondenseChance = 0, ElderDesignationChance = 0, ForgeChance = 0, FamilyForgeChance = 0, UnsealChance = 0, MasterlessStrikeChance = 1.0 }));
            var power = s.Factions.Factions.First(f => f.Elders.Count(e => e.Realm < CultivationRealm.GoldenCore) > 0);
            power.Designations.Add(new RankDesignation("d", "orthodox-water", "gone", "gone", 0));
            int elders = power.Elders.Count;
            s.Arsenal.ProcessYear();
            Assert.Less(power.Elders.Count, elders);
            var unsealing = Session(With(a => a with { ElderCondenseChance = 0, ElderDesignationChance = 0, ForgeChance = 0, FamilyForgeChance = 0, UnsealChance = 1.0, MasterlessStrikeChance = 0 }));
            var p2 = unsealing.Factions.Factions.First();
            p2.Designations.Add(new RankDesignation("d", "orthodox-water", "gone", "gone", 0));
            unsealing.Arsenal.ProcessYear();
            Assert.IsEmpty(p2.Designations);
        }

        [Test]
        public void APower_ForgesArtifactsOfItsCraft_AsManyAsItsElders()
        {
            var s = Session(With(a => a with { ElderCondenseChance = 0, ElderDesignationChance = 0, ForgeChance = 1.0, FamilyForgeChance = 1.0 }));
            var sect = s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);
            double before = Strength(s, sect);
            for (int i = 0; i < 50; i++) s.Arsenal.ProcessYear();
            Assert.That(sect.Artifacts.Count, Is.InRange(1, sect.Elders.Count));
            Assert.IsTrue(sect.Artifacts.All(a => a.Rank <= CultivationRealm.PurpleMansion && a.Rank <= sect.HighestRealm));
            Assert.Greater(Strength(s, sect), before);
        }

        [Test]
        public void TheArsenal_SurvivesASave()
        {
            var content = With(a => a with { ElderCondenseChance = 1.0, ForgeChance = 1.0, FamilyForgeChance = 1.0 });
            var s = Session(content);
            for (int i = 0; i < 5; i++) s.Arsenal.ProcessYear();
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = content });
            foreach (var p in s.Factions.Factions)
                Assert.AreEqual(Strength(s, p), Strength(reloaded, reloaded.Factions.GetFactionByName(p.Name)), 1e-9, p.Name);
        }
    }
}
