using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers plot against the clan for profit (L6a; LORE.md D7 « everything is a plot »): a greedy power ambushes a
    /// member away from the domain and holds them for ransom, interrogating them year after year — a captive who knows
    /// the mirror's secret may talk. The clan pays, rescues, exchanges, or has the mirror blur the captive's memory. An
    /// ambush that fails may leave one of the power's agents in the clan's hands: sold back, released, interrogated,
    /// denounced to the other powers (proof of the scheme), or executed.
    /// </summary>
    [TestFixture]
    public class CaptiveTests
    {
        private const string Ruan = "Famille Ruan"; // aggressive, hostile, a Purple Mansion cultivator
        private const string Lou = "Famille Lou";
        private const string Fang = "Famille Fang";

        private static SchemeSettings Settings => Fixtures.Content.Balance.Schemes;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            return w;
        }

        private static CharacterData Away(TestWorld w, TaskType task = TaskType.Diplomacy,
            CultivationRealm realm = CultivationRealm.QiRefinement)
        {
            var member = w.Join(Fixtures.Cultivator(realm: realm, stage: 3));
            member.CurrentTask = task;
            return member;
        }

        private static CharacterData Captive(TestWorld w, string faction = Ruan)
        {
            var member = Away(w);
            w.Captives.Take(member, faction);
            return member;
        }

        // ---- The scheme ----

        [Test]
        public void SchemeChance_GrowsWithGreed_Hostility_AndTheClansWealth()
        {
            var ruan = new FactionData { Personality = FactionPersonality.Aggressive, RelationWithPlayer = -30 };
            var merchant = new FactionData { Personality = FactionPersonality.Merchant, RelationWithPlayer = -30 };
            var friend = new FactionData { Personality = FactionPersonality.Aggressive, RelationWithPlayer = 60 };

            Assert.Greater(SchemeRules.SchemeChance(ruan, 1000, Settings), SchemeRules.SchemeChance(merchant, 1000, Settings));
            Assert.Greater(SchemeRules.SchemeChance(ruan, 1000, Settings), SchemeRules.SchemeChance(friend, 1000, Settings));
            Assert.Greater(SchemeRules.SchemeChance(ruan, 5000, Settings), SchemeRules.SchemeChance(ruan, 1000, Settings));
            Assert.Greater(SchemeRules.SchemeChance(friend, 1000, Settings), 0, "even an ally schemes, if rarely: profit first");
        }

        [Test]
        public void AnAmbush_TakesAMemberAwayFromTheDomain()
        {
            var w = World(new FixedRandom(0.0));
            var envoy = Away(w);

            Assert.IsTrue(w.Schemes.Ambush(w.Factions.GetFactionByName(Ruan)));

            Assert.AreEqual(Ruan, envoy.CaptorFaction);
            Assert.AreEqual(w.Ctx.Clock.Year, envoy.CapturedYear);
            Assert.AreEqual(envoy, w.Captives.Held.Single());
        }

        [Test]
        public void AnAmbush_FindsNobody_AtHome()
        {
            var w = World(new FixedRandom(0.0));
            var home = Away(w, TaskType.Cultivation);

            Assert.IsFalse(w.Schemes.Ambush(w.Factions.GetFactionByName(Ruan)));
            Assert.IsNull(home.CaptorFaction);
        }

        [Test]
        public void AMemberBackFromAnOperation_IsExposedTheSameYear()
        {
            var w = World(new FixedRandom(0.0));
            var hunter = Away(w, TaskType.Cultivation);
            hunter.LastOperationYear = w.Ctx.Clock.Year;

            Assert.IsTrue(w.Schemes.Ambush(w.Factions.GetFactionByName(Ruan)));
            Assert.AreEqual(Ruan, hunter.CaptorFaction);
        }

        [Test]
        public void CaptureChance_FallsAgainstAStrongerMember()
        {
            var ruan = new FactionData { HighestRealm = CultivationRealm.PurpleMansion };
            var weak = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement);
            var strong = Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion);
            Assert.Greater(SchemeRules.CaptureChance(ruan, weak, Settings), SchemeRules.CaptureChance(ruan, strong, Settings));
        }

        [Test]
        public void AFailedAmbush_MayLeaveTheAgentInTheClansHands()
        {
            // the target picked, the capture failed, the agent taken
            var w = World(new SequenceRandom(0.0, 0.999, 0.0));
            var envoy = Away(w);

            w.Schemes.Ambush(w.Factions.GetFactionByName(Ruan));

            Assert.IsNull(envoy.CaptorFaction);
            Assert.AreEqual(Ruan, w.Captives.Prisoners.Single().Faction);
        }

        [Test]
        public void ProcessYear_LetsTheGreedyScheme()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { w.Factions.GetFactionByName(Ruan) });
            var envoy = Away(w);

            w.Schemes.ProcessYear();

            Assert.AreEqual(Ruan, envoy.CaptorFaction);
        }

        [Test]
        public void ProcessYear_LeavesThePowerThatPiercedTheSecret_ToItsConfrontation()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.Restore(new[] { w.Factions.GetFactionByName(Ruan) });
            w.Secrets.RestoreConfrontation(new Confrontation(Ruan, 1));
            var envoy = Away(w);

            w.Schemes.ProcessYear();

            Assert.IsNull(envoy.CaptorFaction);
        }

        // ---- A member held ----

        [Test]
        public void ACaptive_CanTakeNoTask_NorGoOnAnOperation()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);

            CollectionAssert.AreEqual(new[] { TaskType.None }, TaskRules.AllowedTasks(captive, huntOpen: true));
            Assert.IsFalse(w.Hunts.IsFree(captive));
        }

        [Test]
        public void ACaptiveWhoKnowsTheSecret_MayTalkUnderInterrogation()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            captive.KnowsMirrorSecret = true;

            w.Captives.ProcessYear();

            Assert.AreEqual(Fixtures.Content.Balance.Plots.LeakMirrorClue, w.Suspicion.MirrorClues(Ruan));
            Assert.AreEqual(Fixtures.Content.Balance.Plots.LeakEvidence, w.Suspicion.Evidence(Ruan));
        }

        [Test]
        public void ACaptiveWhoKnowsNothing_GivesNothingAway()
        {
            var w = World(new FixedRandom(0.0));
            Captive(w);
            w.Captives.ProcessYear();
            Assert.AreEqual(0, w.Suspicion.MirrorClues(Ruan));
        }

        [Test]
        public void InterrogationLoosensTongues_MoreThanTheYearlyLeak()
        {
            var keeper = Fixtures.Cultivator();
            Assert.Greater(SchemeRules.InterrogationChance(keeper, sworn: false, Fixtures.Content),
                PlotRules.LeakChance(keeper, sworn: false, Fixtures.Content));
        }

        [Test]
        public void ACaptiveHeldPastTheCaptorsPatience_MayBeExecuted()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            captive.CapturedYear = w.Ctx.Clock.Year - Settings.PatienceYears;

            w.Captives.ProcessYear();

            Assert.IsFalse(captive.IsAlive);
            Assert.AreEqual(DeathCause.Executed, captive.CauseOfDeath);
            Assert.AreEqual(0, w.Captives.Held.Count);
        }

        [Test]
        public void ACaptiveWithinTheCaptorsPatience_Lives()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            captive.CapturedYear = w.Ctx.Clock.Year - Settings.PatienceYears + 1;
            w.Captives.ProcessYear();
            Assert.IsTrue(captive.IsAlive);
        }

        // ---- The clan's answers ----

        [Test]
        public void TheRansom_GrowsWithTheCaptivesRealm()
        {
            Assert.Greater(SchemeRules.Ransom(CultivationRealm.Foundation, Settings), SchemeRules.Ransom(CultivationRealm.QiRefinement, Settings));
        }

        [Test]
        public void PayingTheRansom_FreesTheCaptive()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            int ransom = SchemeRules.Ransom(captive.Realm, Settings);
            w.Resources.ConsumeSpiritStones(w.Resources.SpiritStones - ransom);

            Assert.IsNull(w.Captives.PayRansom(captive.ID));

            Assert.IsNull(captive.CaptorFaction);
            Assert.AreEqual(0, w.Resources.SpiritStones);
        }

        [Test]
        public void TheRansom_CannotBePaid_WithoutTheStones()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            w.Resources.ConsumeSpiritStones(w.Resources.SpiritStones);

            StringAssert.Contains("pierres", w.Captives.PayRansom(captive.ID));
            Assert.AreEqual(Ruan, captive.CaptorFaction);
        }

        [Test]
        public void TheMirror_BlursWhatTheCaptiveKnows()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            captive.KnowsMirrorSecret = true;
            int power = w.Mirror.MirrorPower;

            Assert.IsNull(w.Captives.Silence(captive.ID));

            Assert.IsFalse(captive.KnowsMirrorSecret);
            Assert.AreEqual(power - Settings.SilenceMirrorCost, w.Mirror.MirrorPower);
            w.Captives.ProcessYear();
            Assert.AreEqual(0, w.Suspicion.MirrorClues(Ruan), "nothing left to tell");
        }

        [Test]
        public void TheMirror_HasNothingToBlur_InACaptiveWhoKnowsNothing()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            int power = w.Mirror.MirrorPower;
            Assert.IsNotNull(w.Captives.Silence(captive.ID));
            Assert.AreEqual(power, w.Mirror.MirrorPower);
        }

        [Test]
        public void ARescue_FreesTheCaptive_AndAngersTheCaptor()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            var rescuer = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 5));
            int relation = w.Factions.GetFactionByName(Ruan).RelationWithPlayer;

            Assert.IsNull(w.Captives.Rescue(captive.ID, new[] { rescuer.ID }));

            Assert.IsNull(captive.CaptorFaction);
            Assert.AreEqual(w.Ctx.Clock.Year, rescuer.LastOperationYear, "one operation a year");
            Assert.Less(w.Factions.GetFactionByName(Ruan).RelationWithPlayer, relation);
        }

        [Test]
        public void AFailedRescue_LeavesTheCaptiveHeld_AndTheCaptorSuspicious()
        {
            var w = World(new FixedRandom(0.999));
            var captive = Captive(w);
            var rescuer = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1));

            StringAssert.Contains("échoue", w.Captives.Rescue(captive.ID, new[] { rescuer.ID }));

            Assert.AreEqual(Ruan, captive.CaptorFaction);
            Assert.AreEqual(Settings.FailedRescueSuspicion, w.Suspicion.OfClan(Ruan));
        }

        [Test]
        public void ARescue_NeedsAFreeTeam()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            var busy = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            busy.LastOperationYear = w.Ctx.Clock.Year;

            Assert.IsNotNull(w.Captives.Rescue(captive.ID, new[] { busy.ID }));
            Assert.IsNotNull(w.Captives.Rescue(captive.ID, new string[0]));
            Assert.AreEqual(Ruan, captive.CaptorFaction);
        }

        [Test]
        public void RescueChance_GrowsWithTheTeam_AgainstTheCaptor()
        {
            var ruan = new FactionData { HighestRealm = CultivationRealm.Foundation };
            var weak = new[] { Fixtures.Cultivator(realm: CultivationRealm.QiRefinement) };
            var strong = new[] { Fixtures.Cultivator(realm: CultivationRealm.Foundation), Fixtures.Cultivator(realm: CultivationRealm.Foundation) };
            Assert.Greater(SchemeRules.RescueChance(strong, ruan, Settings), SchemeRules.RescueChance(weak, ruan, Settings));
        }

        // ---- The agents the clan holds ----

        private static Prisoner Agent(TestWorld w, string faction = Ruan)
        {
            var agent = new Prisoner("agent-1", faction, CultivationRealm.QiRefinement, w.Ctx.Clock.Year);
            w.Captives.RestorePrisoners(new[] { agent });
            return agent;
        }

        [Test]
        public void AnExchange_TradesTheirAgentForOurCaptive()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            var agent = Agent(w);

            Assert.IsNull(w.Captives.Exchange(captive.ID, agent.Id));

            Assert.IsNull(captive.CaptorFaction);
            Assert.AreEqual(0, w.Captives.Prisoners.Count);
        }

        [Test]
        public void AnExchange_NeedsAnAgentOfTheSamePower()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w);
            var agent = Agent(w, Lou);
            Assert.IsNotNull(w.Captives.Exchange(captive.ID, agent.Id));
            Assert.AreEqual(Ruan, captive.CaptorFaction);
        }

        [Test]
        public void SellingAnAgentBack_BringsStones_FromItsPower()
        {
            var w = World(new FixedRandom(0.0));
            var agent = Agent(w);
            int stones = w.Resources.SpiritStones;
            int wealth = w.Factions.GetFactionByName(Ruan).Wealth;
            int ransom = SchemeRules.Ransom(agent.Realm, Settings);

            Assert.IsNull(w.Captives.SellBack(agent.Id));

            Assert.AreEqual(stones + ransom, w.Resources.SpiritStones);
            Assert.AreEqual(wealth - ransom, w.Factions.GetFactionByName(Ruan).Wealth);
            Assert.AreEqual(0, w.Captives.Prisoners.Count);
        }

        [Test]
        public void ReleasingAnAgent_WarmsItsPower()
        {
            var w = World(new FixedRandom(0.0));
            var agent = Agent(w);
            int relation = w.Factions.GetFactionByName(Ruan).RelationWithPlayer;

            Assert.IsNull(w.Captives.Release(agent.Id));

            Assert.AreEqual(relation + Settings.ReleaseRelation, w.Factions.GetFactionByName(Ruan).RelationWithPlayer);
            Assert.AreEqual(0, w.Captives.Prisoners.Count);
        }

        [Test]
        public void InterrogatingAnAgent_YieldsFragments_AndWhatItsPowerHolds_Once()
        {
            var w = World(new FixedRandom(0.0));
            var agent = Agent(w);
            w.Suspicion.AddEvidence(Ruan, Fixtures.Content.Balance.Plots.ProofThreshold);
            int fragments = w.Resources.TechniqueFragments;

            string intel = w.Captives.Interrogate(agent.Id);

            StringAssert.Contains("preuve", intel);
            Assert.AreEqual(fragments + Settings.InterrogationFragments, w.Resources.TechniqueFragments);
            Assert.IsTrue(w.Captives.Prisoners.Single().Interrogated);
            Assert.IsNull(w.Captives.Interrogate(agent.Id), "nothing more to tell");
        }

        [Test]
        public void DenouncingAnAgentsPower_MakesTheOthersDistrustIt_Once()
        {
            var w = World(new FixedRandom(0.0));
            var agent = Agent(w);

            Assert.IsNull(w.Captives.Denounce(agent.Id));

            Assert.AreEqual(Settings.DenounceDistrust, w.Suspicion.Distrust(Fang, Ruan));
            Assert.AreEqual(0, w.Suspicion.Distrust(Ruan, Ruan));
            Assert.IsNotNull(w.Captives.Denounce(agent.Id));
            Assert.AreEqual(Settings.DenounceDistrust, w.Suspicion.Distrust(Fang, Ruan));
        }

        [Test]
        public void ExecutingAnAgent_EnragesItsPower_AndChillsTheOthers()
        {
            var w = World(new FixedRandom(0.0));
            var agent = Agent(w);
            int ruan = w.Factions.GetFactionByName(Ruan).RelationWithPlayer;
            int fang = w.Factions.GetFactionByName(Fang).RelationWithPlayer;

            Assert.IsNull(w.Captives.Execute(agent.Id));

            Assert.AreEqual(ruan + Settings.ExecuteRelation, w.Factions.GetFactionByName(Ruan).RelationWithPlayer);
            Assert.AreEqual(fang + Settings.ExecuteWitnessRelation, w.Factions.GetFactionByName(Fang).RelationWithPlayer);
            Assert.AreEqual(0, w.Captives.Prisoners.Count);
        }

        [Test]
        public void AnAgentOfAVanishedPower_IsNoLongerHeld()
        {
            var w = World(new FixedRandom(0.0));
            Agent(w, "Puissance disparue");
            w.Captives.ProcessYear();
            Assert.AreEqual(0, w.Captives.Prisoners.Count);
        }

        [Test]
        public void ACaptiveOfAVanishedPower_WalksFree()
        {
            var w = World(new FixedRandom(0.0));
            var captive = Captive(w, "Puissance disparue");
            w.Captives.ProcessYear();
            Assert.IsNull(captive.CaptorFaction);
        }

        // ---- Saves and the year ----

        [Test]
        public void RoundTrip_KeepsTheCaptivesOnBothSides()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            var member = s.Clan.LivingMembers.Last();
            s.Captives.Take(member, Ruan);
            s.Captives.RestorePrisoners(new[] { new Prisoner("agent-1", Lou, CultivationRealm.Foundation, 1) { Interrogated = true } });

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.AreEqual(Ruan, reloaded.Clan.FindById(member.ID).CaptorFaction);
            Assert.AreEqual(1, reloaded.Clan.FindById(member.ID).CapturedYear);
            Assert.AreEqual(new Prisoner("agent-1", Lou, CultivationRealm.Foundation, 1) { Interrogated = true }, reloaded.Captives.Prisoners.Single());
        }

        [Test]
        public void TheSessionsYear_LetsThePowersScheme_AndHoldsTheirCaptives()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var captive = s.Clan.LivingMembers.Last();
            captive.KnowsMirrorSecret = true;
            s.Captives.Take(captive, Ruan);
            int before = s.Suspicion.MirrorClues(Ruan);

            for (int i = 0; i < 30 && s.Suspicion.MirrorClues(Ruan) == before && captive.CaptorFaction != null; i++) s.AdvanceYear();

            Assert.IsTrue(s.Suspicion.MirrorClues(Ruan) > before || captive.CaptorFaction == null, "the captive is interrogated each year");
        }
    }
}
