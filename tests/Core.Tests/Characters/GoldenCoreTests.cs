using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The breakthrough to the Golden Core (LORE.md §5.5.1, §5.9 R1-R7): the five abilities are forged into a
    /// metal essence with the gold-seeking method of the lineage aimed at, then a position is asked of Heaven
    /// — Realization, Surplus or Intercalary — in two steps most never complete. A failure gives life to a
    /// Metal Essence Demon.
    /// </summary>
    [TestFixture]
    public class GoldenCoreTests
    {
        private const double Pass = 0.0;
        private const double Fail = 0.995;
        private const string OrthodoxWater = "orthodox-water";
        private const string MutableWater = "mutable-water";
        private const string NourishingWater = "nourishing-water";

        private static readonly string[] FiveOrthodoxWater =
        {
            "orthodox-water:boundless-sea", "orthodox-water:ford-watcher", "orthodox-water:storm-sky",
            "orthodox-water:dike-guard", "orthodox-water:river-farewell"
        };

        private static readonly string[] MutableWithSubstitute =
        {
            "mutable-water:buried-spring-song", "mutable-water:mist-veil", "mutable-water:evening-downpour",
            "mutable-water:silhouette-under-the-wave", "mutable-water:drowned-moon-substitute"
        };

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        private static GameContent Content => Fixtures.Content;

        private static FruitionDefinition Lineage(string id) => Content.Fruitions.Single(f => f.Id == id);

        /// <summary>A Purple Mansion cultivator at Grand Perfection holding the given abilities, with the realm's XP.</summary>
        private static CharacterData Master(TestWorld w, params string[] abilities)
        {
            var c = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion, stage: 4);
            c.DivineAbilities = new List<string>(abilities.Length > 0 ? abilities : FiveOrthodoxWater);
            c.FoundationId = c.DivineAbilities[0];
            c.CultivationXP = Xp;
            return w.Join(c);
        }

        private static void KnowGoldSeeking(TestWorld w, string subject) =>
            w.Knowledge.Reveal(FactKind.GoldSeeking, subject, KnowledgeSource.Mirror);

        /// <summary>A Grand Perfection of the Orthodox Water who knows its gold-seeking method.</summary>
        private static CharacterData ReadyToForge(TestWorld w)
        {
            var c = Master(w);
            KnowGoldSeeking(w, OrthodoxWater);
            return c;
        }

        /// <summary>A metal essence already forged toward the lineage, no position asked yet (R7).</summary>
        private static CharacterData Forged(TestWorld w, string fruitionId, params string[] abilities)
        {
            var c = Master(w, abilities);
            c.Realm = CultivationRealm.GoldenCore;
            c.RealmStage = 1;
            c.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            c.FruitionId = fruitionId;
            return c;
        }

        // ---- Which position the five abilities lead to (§5.5.1) ----

        [Test]
        public void RouteTo_FiveOrthodoxAbilities_IsTheRealization()
        {
            Assert.AreEqual(PositionRoute.Realization, GoldenCoreRules.RouteTo(FiveOrthodoxWater, OrthodoxWater, Content.Fruitions));
        }

        [Test]
        public void RouteTo_ASubstituteAmongTheFive_IsTheSurplus()
        {
            Assert.AreEqual(PositionRoute.Surplus, GoldenCoreRules.RouteTo(MutableWithSubstitute, MutableWater, Content.Fruitions));
        }

        [Test]
        public void RouteTo_FourOfOneAndOneOfAnother_IsTheIntercalaryOfTheOther()
        {
            var abilities = FiveOrthodoxWater.Take(4).Append("nourishing-water:winter-drizzle");
            Assert.AreEqual(PositionRoute.IntercalaryFourOne, GoldenCoreRules.RouteTo(abilities, NourishingWater, Content.Fruitions));
            Assert.AreEqual(PositionRoute.None, GoldenCoreRules.RouteTo(abilities, OrthodoxWater, Content.Fruitions));
        }

        [Test]
        public void RouteTo_ThreeOfOneAndTwoOfAnother_IsTheIntercalaryOfTheOther()
        {
            var abilities = FiveOrthodoxWater.Take(3).Concat(new[] { "nourishing-water:winter-drizzle", "nourishing-water:dawn-abyss" });
            Assert.AreEqual(PositionRoute.IntercalaryThreeTwo, GoldenCoreRules.RouteTo(abilities, NourishingWater, Content.Fruitions));
        }

        [Test]
        public void RouteTo_ThreeLineages_LeadsNowhere()
        {
            var abilities = FiveOrthodoxWater.Take(3).Concat(new[] { "nourishing-water:winter-drizzle", "mutable-water:mist-veil" });
            Assert.AreEqual(PositionRoute.None, GoldenCoreRules.RouteTo(abilities, NourishingWater, Content.Fruitions));
        }

        [Test]
        public void RouteTo_FewerThanFive_LeadsNowhere()
        {
            Assert.AreEqual(PositionRoute.None, GoldenCoreRules.RouteTo(FiveOrthodoxWater.Take(4), OrthodoxWater, Content.Fruitions));
        }

        [TestCase(PositionRoute.IntercalaryFourOne, "orthodox-water", true)]  // « an orthodox position knows no Intercalary »
        [TestCase(PositionRoute.Surplus, "gathered-water", true)]             // « a gathered position knows no Surplus »
        [TestCase(PositionRoute.IntercalaryFourOne, "nourishing-water", false)]
        [TestCase(PositionRoute.Realization, "orthodox-water", false)]
        public void BreaksTheAxiom_OfThePositions(PositionRoute route, string fruitionId, bool breaks)
        {
            Assert.AreEqual(breaks, GoldenCoreRules.BreaksTheAxiom(route, Lineage(fruitionId)));
        }

        // ---- First step: forging the metal essence ----

        [Test]
        public void Forge_MakesATrueMonarchWithoutPosition()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = ReadyToForge(w);

            Assert.IsTrue(w.GoldenCore.Forge(c, OrthodoxWater));

            Assert.AreEqual(CultivationRealm.GoldenCore, c.Realm);
            Assert.AreEqual(1, c.RealmStage);
            Assert.AreEqual(GoldenCoreState.MetallicEssenceOnly, c.GoldenCore);
            Assert.AreEqual(OrthodoxWater, c.FruitionId);
            Assert.AreEqual(0, c.CultivationXP);
            Assert.AreEqual(PowerLadder.MaxLifespan(CultivationRealm.GoldenCore, 1), c.MaxLifespan);
        }

        [Test]
        public void Forge_Refuses_WithoutTheGoldSeekingMethodOfTheLineage()
        {
            // Only the True Monarchs of a lineage master its method: a quest, ruins, the mirror (§5.9 E)
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w);
            Assert.IsFalse(w.GoldenCore.Forge(c, OrthodoxWater));
            Assert.AreEqual(CultivationRealm.PurpleMansion, c.Realm);
        }

        [Test]
        public void Forge_Refuses_BeforeTheGrandPerfection()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = ReadyToForge(w);
            c.DivineAbilities.RemoveAt(4);
            Assert.IsFalse(w.GoldenCore.Forge(c, OrthodoxWater));
        }

        [Test]
        public void Forge_Refuses_WithoutTheRealmsExperience()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = ReadyToForge(w);
            c.CultivationXP = Xp - 1;
            Assert.IsFalse(w.GoldenCore.Forge(c, OrthodoxWater));
        }

        [Test]
        public void Forge_Refuses_ALineageTheAbilitiesDoNotLeadTo()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = ReadyToForge(w);
            KnowGoldSeeking(w, NourishingWater);
            Assert.IsFalse(w.GoldenCore.Forge(c, NourishingWater));
        }

        [Test]
        public void Forge_TheThreeTwoIntercalary_NeedsTheSpecialisedMethod()
        {
            // « a specialised gold-seeking method: very difficult but safer » (R5)
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, FiveOrthodoxWater.Take(3).Concat(new[] { "nourishing-water:winter-drizzle", "nourishing-water:dawn-abyss" }).ToArray());
            KnowGoldSeeking(w, NourishingWater);
            Assert.IsFalse(w.GoldenCore.Forge(c, NourishingWater));

            KnowGoldSeeking(w, GoldenCoreRules.SpecialisedMethod(NourishingWater));
            Assert.IsTrue(w.GoldenCore.Forge(c, NourishingWater));
        }

        [Test]
        public void Forge_Failure_GivesLifeToAMetalEssenceDemon()
        {
            var w = new TestWorld(new FixedRandom(Fail));
            var c = ReadyToForge(w);
            CharacterData demon = null;
            w.Ctx.Events.OnMetalEssenceDemon += member => demon = member;

            Assert.IsTrue(w.GoldenCore.Forge(c, OrthodoxWater)); // attempted

            Assert.IsFalse(c.IsAlive);
            Assert.AreEqual(DeathCause.MetalEssenceDemon, c.CauseOfDeath);
            Assert.AreSame(c, demon);
        }

        [Test]
        public void ForgeChance_ShallowAndGraftedAbilitiesWeighOnTheForging()
        {
            // « an inevitably perilous path » (§5.4.3)
            var w = new TestWorld();
            var c = Master(w);
            int deep = GoldenCoreRules.ForgeChance(c, Content);

            c.ShallowAbilities.Add(FiveOrthodoxWater[1]);
            int shallow = GoldenCoreRules.ForgeChance(c, Content);
            c.GraftedAbilities.Add(FiveOrthodoxWater[2]);
            int grafted = GoldenCoreRules.ForgeChance(c, Content);

            Assert.Less(shallow, deep);
            Assert.Less(grafted, shallow);
        }

        [Test]
        public void ForgeChance_TheLifeAbilityCondensedLast_EmbodiesTheLineagesDestiny()
        {
            // §5.4.4: the Life ability, usually acquired last, raises the chances of the Golden Core
            var w = new TestWorld();
            var lifeFirst = Master(w, "orthodox-water:ford-watcher", "orthodox-water:boundless-sea", "orthodox-water:storm-sky",
                "orthodox-water:dike-guard", "orthodox-water:river-farewell");
            var lifeLast = Master(w, "orthodox-water:boundless-sea", "orthodox-water:storm-sky", "orthodox-water:dike-guard",
                "orthodox-water:river-farewell", "orthodox-water:ford-watcher");

            Assert.Greater(GoldenCoreRules.ForgeChance(lifeLast, Content), GoldenCoreRules.ForgeChance(lifeFirst, Content));
        }

        // ---- Second step: asking Heaven for a position ----

        [Test]
        public void ClaimPosition_AFreeRealization_MakesTheMemberItsHolder()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Forged(w, OrthodoxWater);

            Assert.IsTrue(w.GoldenCore.ClaimPosition(c));

            Assert.AreEqual(GoldenCoreState.Realization, c.GoldenCore);
            Assert.AreEqual(new FruitionState(FruitionStatus.Occupied, c.FullName), w.Fruitions.State(OrthodoxWater));
        }

        [Test]
        public void ClaimPosition_Refuses_AnOccupiedRealization_AndTheEssenceWaits()
        {
            // One Realization per lineage; the essence may try again later (R7)
            var w = new TestWorld(new FixedRandom(Pass));
            w.Fruitions.Claim(OrthodoxWater, "Vrai Monarque rival");
            var c = Forged(w, OrthodoxWater);

            Assert.IsFalse(w.GoldenCore.ClaimPosition(c));

            Assert.IsTrue(c.IsAlive);
            Assert.AreEqual(GoldenCoreState.MetallicEssenceOnly, c.GoldenCore);
        }

        [Test]
        public void ClaimPosition_Refuses_ABrokenLineage()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Forged(w, "nourishing-fire", "nourishing-fire:ember-pheasant", "nourishing-fire:heavenly-blaze",
                "nourishing-fire:hearth-god", "nourishing-fire:unrevealed-4", "nourishing-fire:unrevealed-5");
            Assert.IsFalse(w.GoldenCore.ClaimPosition(c));
        }

        [Test]
        public void ClaimPosition_ASurplusOfAnOccupiedLineage_NeedsItsHoldersPermission()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Forged(w, MutableWater, MutableWithSubstitute);
            Assert.IsFalse(w.GoldenCore.ClaimPosition(c));

            w.Resources.AddSpiritStones(Content.Balance.GoldenCore.PermissionStones);
            Assert.IsTrue(w.GoldenCore.RequestPermission(MutableWater));
            Assert.IsTrue(w.GoldenCore.ClaimPosition(c));

            Assert.AreEqual(GoldenCoreState.Surplus, c.GoldenCore);
            Assert.AreEqual(new FruitionState(FruitionStatus.Occupied, "Tan Qing"), w.Fruitions.State(MutableWater));
        }

        [Test]
        public void ClaimPosition_AnIntercalaryOfAFreeLineage_NeedsNoPermission()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Forged(w, NourishingWater, FiveOrthodoxWater.Take(4).Append("nourishing-water:winter-drizzle").ToArray());

            Assert.IsTrue(w.GoldenCore.ClaimPosition(c));

            Assert.AreEqual(GoldenCoreState.Intercalary, c.GoldenCore);
            Assert.AreEqual(FruitionStatus.Free, w.Fruitions.State(NourishingWater).Status); // the Realization stays to take
        }

        [Test]
        public void ClaimPosition_Failure_GivesLifeToAMetalEssenceDemon()
        {
            var w = new TestWorld(new FixedRandom(Fail));
            var c = Forged(w, OrthodoxWater);

            Assert.IsTrue(w.GoldenCore.ClaimPosition(c));

            Assert.IsFalse(c.IsAlive);
            Assert.AreEqual(DeathCause.MetalEssenceDemon, c.CauseOfDeath);
            Assert.AreEqual(FruitionStatus.Free, w.Fruitions.State(OrthodoxWater).Status);
        }

        [Test]
        public void ClaimChance_BreakingTheAxiom_IsDangerous()
        {
            var w = new TestWorld();
            var c = Master(w);
            int safe = GoldenCoreRules.ClaimChance(c, PositionRoute.IntercalaryFourOne, Lineage(NourishingWater), Content);
            int axiom = GoldenCoreRules.ClaimChance(c, PositionRoute.IntercalaryFourOne, Lineage(OrthodoxWater), Content);
            Assert.AreEqual(Content.Balance.GoldenCore.AxiomPenalty, safe - axiom);
        }

        [Test]
        public void ClaimChance_TheSpecialisedIntercalary_IsSaferThanTheOrdinary()
        {
            var w = new TestWorld();
            var c = Master(w);
            Assert.Greater(GoldenCoreRules.ClaimChance(c, PositionRoute.IntercalaryThreeTwo, Lineage(NourishingWater), Content),
                GoldenCoreRules.ClaimChance(c, PositionRoute.IntercalaryFourOne, Lineage(NourishingWater), Content));
        }

        // ---- The holder's permission ----

        [Test]
        public void RequestPermission_CostsATribute_EvenWhenRefused()
        {
            var w = new TestWorld(new FixedRandom(Fail));
            int tribute = Content.Balance.GoldenCore.PermissionStones;
            w.Resources.AddSpiritStones(tribute);
            int before = w.Resources.SpiritStones;

            Assert.IsFalse(w.GoldenCore.RequestPermission(MutableWater));

            Assert.AreEqual(before - tribute, w.Resources.SpiritStones);
            Assert.IsFalse(w.GoldenCore.Permissions.ContainsKey(MutableWater));
        }

        [Test]
        public void RequestPermission_Refuses_ALineageWithoutHolder()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Resources.AddSpiritStones(Content.Balance.GoldenCore.PermissionStones);
            Assert.IsFalse(w.GoldenCore.RequestPermission(OrthodoxWater));
        }

        [Test]
        public void Permission_LapsesWhenTheHolderChanges()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Resources.AddSpiritStones(Content.Balance.GoldenCore.PermissionStones);
            w.GoldenCore.RequestPermission(MutableWater);
            w.Fruitions.Claim(MutableWater, "Nouveau détenteur");

            var c = Forged(w, MutableWater, MutableWithSubstitute);

            Assert.IsFalse(w.GoldenCore.ClaimPosition(c));
        }

        // ---- The gold-seeking methods: deciphered by the mirror (§5.9 E) ----

        [Test]
        public void DecipherGoldSeeking_SpendsTheMirrorsPower_AndRevealsTheMethod()
        {
            var w = new TestWorld();
            int before = w.Mirror.MirrorPower;

            Assert.IsTrue(w.GoldenCore.DecipherGoldSeeking(OrthodoxWater, specialised: false));

            Assert.IsTrue(w.Knowledge.Knows(FactKind.GoldSeeking, OrthodoxWater));
            Assert.AreEqual(before - Content.Balance.GoldenCore.GoldSeekingMirrorCost, w.Mirror.MirrorPower);
        }

        [Test]
        public void DecipherGoldSeeking_TheSpecialisedMethod_RevealsItsOwnFact()
        {
            var w = new TestWorld();
            w.Mirror.AddPower(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower);
            Assert.IsTrue(w.GoldenCore.DecipherGoldSeeking(NourishingWater, specialised: true));
            Assert.IsTrue(w.Knowledge.Knows(FactKind.GoldSeeking, GoldenCoreRules.SpecialisedMethod(NourishingWater)));
            Assert.IsFalse(w.Knowledge.Knows(FactKind.GoldSeeking, NourishingWater));
        }

        [Test]
        public void DecipherGoldSeeking_Refuses_WithoutEnoughPower_OrAnUnknownLineage()
        {
            var w = new TestWorld();
            Assert.IsFalse(w.GoldenCore.DecipherGoldSeeking("no-such-lineage", specialised: false));
            while (w.Mirror.ConsumePower(1)) { }
            Assert.IsFalse(w.GoldenCore.DecipherGoldSeeking(OrthodoxWater, specialised: false));
        }

        // ---- Abilities of another lineage, for the Intercalary ----

        [Test]
        public void Pursue_AnotherLineagesAbility_OnlyAsTheFourthOrFifth()
        {
            var w = new TestWorld();
            var early = Master(w, FiveOrthodoxWater.Take(2).ToArray());
            var late = Master(w, FiveOrthodoxWater.Take(3).ToArray());
            foreach (var m in new[] { early, late }) { m.RealmStage = 1; m.CultivationXP = 0; }
            w.Knowledge.Reveal(FactKind.Ability, "nourishing-water:winter-drizzle", KnowledgeSource.Studied);

            Assert.IsFalse(w.Abilities.Pursue(early, "nourishing-water:winter-drizzle"));
            Assert.IsTrue(w.Abilities.Pursue(late, "nourishing-water:winter-drizzle"));
        }

        [Test]
        public void Pursue_Refuses_AThirdLineage()
        {
            var w = new TestWorld();
            var c = Master(w, FiveOrthodoxWater.Take(3).Append("nourishing-water:winter-drizzle").ToArray());
            w.Knowledge.Reveal(FactKind.Ability, "mutable-water:mist-veil", KnowledgeSource.Studied);
            w.Knowledge.Reveal(FactKind.Ability, "nourishing-water:dawn-abyss", KnowledgeSource.Studied);

            Assert.IsFalse(w.Abilities.Pursue(c, "mutable-water:mist-veil"));
            Assert.IsTrue(w.Abilities.Pursue(c, "nourishing-water:dawn-abyss"));
        }

        // ---- Saves and data ----

        [Test]
        public void RoundTrip_KeepsThePermissionsGranted()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.GoldenCore.Restore(new Dictionary<string, string> { [MutableWater] = "Tan Qing" });

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.AreEqual("Tan Qing", reloaded.GoldenCore.Permissions[MutableWater]);
        }

        [Test]
        public void OlderSave_HasNoPermission()
        {
            var data = GameSession.NewGame(Fixtures.Setup(1)).ToSaveData();
            data.GoldenCorePermissions = null; // saved before L4b
            Assert.AreEqual(0, GameSession.FromSaveData(data, Fixtures.Setup()).GoldenCore.Permissions.Count);
        }

        [Test]
        public void Load_Refuses_ABalanceWithoutTheGoldenCoreSettings()
        {
            var balance = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.BalanceFile));
            balance.Remove("goldenCore");
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.BalanceFile ? balance.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.BalanceFile, error.Message);
        }
    }
}
