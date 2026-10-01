using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// Long automatic games (balance, 2026-09-29): a passive clan lives through the years while the world plots, probes,
    /// bands and wars; the run counts what befell the clan and the powers, so that none of it runs away.
    /// </summary>
    [TestFixture]
    public class BalanceRunTests
    {
        [Test]
        public void ARun_IsReproducible_AndReflectsTheSession()
        {
            var first = BalanceRun.Play(Fixtures.Content, seed: 3, years: 15, out var session);
            var again = BalanceRun.Play(Fixtures.Content, seed: 3, years: 15, out _);

            Assert.AreEqual(first, again, "the same seed, the same story");
            Assert.AreEqual(session.Clan.LivingMembers.Count, first.Members);
            Assert.AreEqual(session.Factions.Factions.Count, first.Powers);
            Assert.IsTrue(first.Years == 15 || first.Lost, "every year played, unless the line fell");
        }

        [Test]
        public void TheAutopilot_SetsTheIdleToWork()
        {
            var session = GameSession.NewGame(Fixtures.Setup(3));
            BalanceRun.SetTheIdleToWork(session);
            var free = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            Assert.IsTrue(free.Any(m => m.CurrentTask == TaskType.Cultivation), "those who can, cultivate");
            Assert.IsTrue(free.All(m => m.CurrentTask != TaskType.None || !session.Tasks.AssignTask(m, TaskType.Mine)), "nobody idle who could work");
        }

        // ---- An active player's knowledge (2026-09-30): a method to the Purple Mansion, its Qi, the powers' secrets ----

        private static GameSession Rich(int seed = 3)
        {
            var session = GameSession.NewGame(Fixtures.Setup(seed));
            session.Resources.SetSpiritStones(100_000);
            foreach (var f in session.Factions.Factions) f.RelationWithPlayer = 60;
            return session;
        }

        /// <summary>A mirror ready to deduce an ascent method: three shards, good fragments, a lineage known.</summary>
        private static GameSession Knowing(int seed = 3)
        {
            var session = Rich(seed);
            var rules = Fixtures.Content.Balance.Techniques;
            session.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, rules.AscentDeductionShards);
            for (int i = 0; i < rules.AscentDeductionFragments; i++) session.Deduction.AddFragment(Element.Water, rules.AscentDeductionQuality);
            session.Knowledge.Reveal(new Fact(FactKind.Lineage, "orthodox-water"), KnowledgeSource.Mirror);
            return session;
        }

        private static bool KnowsTheAscent(GameSession s) =>
            s.Techniques.Known.Any(t => t.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(t) && t.RequiredQiId != null);

        [Test]
        public void ThePilot_HasTheMirrorDeduceAMethodToThePurpleMansion()
        {
            var session = Knowing();
            Assert.IsFalse(KnowsTheAscent(session));
            BalanceRun.Act(session);
            Assert.IsTrue(KnowsTheAscent(session));
            var method = session.Techniques.Known.First(t => t.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(t) && t.RequiredQiId != null);
            Assert.IsTrue(session.Clan.LivingMembers.Where(m => m.QiId == method.RequiredQiId && m.CaptorFaction == null).All(m => m.CultivationMethodId == method.ID),
                "the cultivators of its Qi take up the ascent method");
        }

        [Test]
        public void ThePilot_GathersTheQiOfItsBestMethod()
        {
            var session = Knowing();
            BalanceRun.Act(session);
            var method = session.Techniques.Known.First(t => t.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(t) && t.RequiredQiId != null);
            session.Resources.ConsumeQi(method.RequiredQiId, session.Resources.QiPortions(method.RequiredQiId)); // the stock is spent
            session.Clan.AddMember(Fixtures.Cultivator(age: 16, realm: CultivationRealm.Embryonic, stage: 5));
            BalanceRun.SetTheIdleToWork(session);
            Assert.IsTrue(session.Clan.LivingMembers.Any(m => m.Realm == CultivationRealm.Embryonic && m.CurrentTask == TaskType.GatherQi),
                "breathing cultivators who perceive Qi gather the ascent method's");
        }

        [Test]
        public void ThePilot_ProbesThePowers()
        {
            var session = Rich();
            while (session.Clan.LivingMembers.Count(m => m.Realm >= CultivationRealm.QiRefinement) < 4) session.Clan.AddMember(Fixtures.Cultivator(stage: 5));
            session.Clock.Restore(3, GamePhase.Management); // a year of probes
            var prober = session.Clan.FindById(session.Shards.BestTeam(CultivationRealm.QiRefinement, 2).First(id => id != session.Clan.PatriarchID));
            BalanceRun.Act(session);
            Assert.AreEqual(session.Clock.Year, prober.LastOperationYear, "the best free member but the patriarch goes out to probe (whatever comes of it)");
        }

        [Test]
        public void ThePilot_SendsOnlyTheLeastGiftedToTheMine_WhenTheCoffersRunLow()
        {
            var session = GameSession.NewGame(Fixtures.Setup(3));
            foreach (var m in session.Clan.LivingMembers.Where(m => !SpiritualOrificeRules.CanCultivate(m)).ToList())
                session.Clan.Kill(m, DeathCause.Illness); // a clan of cultivators only
            var dull = session.Clan.LivingMembers.First(m => m.ID != session.Clan.PatriarchID);
            dull.SpiritualRoot = 20; // a poor root: the mine is for them (the user's rule, 2026-09-30)
            session.Shards.Recover("lake-jade"); // nobody sent to dredge the lake instead
            session.Resources.SetSpiritStones(0);
            BalanceRun.SetTheIdleToWork(session);
            Assert.AreEqual(TaskType.Mine, dull.CurrentTask, "the least gifted feeds the clan");
            Assert.IsTrue(session.Clan.LivingMembers.Where(m => m.SpiritualRoot >= 40).All(m => m.CurrentTask != TaskType.Mine), "the gifted never go down");
        }

        [Test]
        public void TheAutopilot_GathersTheQi_TheFoundationWallAbsorbs()
        {
            var session = GameSession.NewGame(Fixtures.Setup(3));
            var peak = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9);
            session.Clan.AddMember(peak);
            session.Resources.ConsumeQi(peak.QiId, session.Resources.QiPortions(peak.QiId));
            Assume.That(session.Cultivation.HasTrialQi(peak, TrialKind.FoundationWall), Is.False);

            BalanceRun.SetTheIdleToWork(session);

            Assert.AreEqual(TaskType.GatherQi, peak.CurrentTask, "the wall absorbs a portion of their Qi: they gather it");
        }

        // ---- The active pilot ----

        [Test]
        public void ThePilot_PaysADemandItCanAfford_AndRefusesOneItCannot()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Resources.SetSpiritStones(100000);
            s.Intrigues.RestoreDemands(new[] { new Demand("Famille Ruan", 500, s.Clock.Year, DemandKind.Protection) }, null);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Intrigues.Demands);
            Assert.IsFalse(s.Wars.ClanWars.Any(w => w.Enemy == "Famille Ruan"), "paid: no war");

            int short_ = 500 + s.Upkeep.YearlyUpkeep - 1; // paying would leave less than a year of upkeep
            s.Resources.SetSpiritStones(short_);
            s.Intrigues.RestoreDemands(new[] { new Demand("Famille Fang", 500, s.Clock.Year, DemandKind.Protection) }, null);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Intrigues.Demands);
            Assert.IsTrue(s.Wars.ClanWars.Any(w => w.Enemy == "Famille Fang"), "it could not keep its reserve: it refused, and war came");
        }

        [Test]
        public void ThePilot_AnswersAChallenge_AndFightsItOut()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Challenges.Issue(s.Factions.GetFactionByName("Famille Ruan"));
            ChallengeOutcome? outcome = null;
            s.Events.OnChallengeSettled += (_, o) => outcome = o;
            BalanceRun.Act(s);
            Assert.IsNull(s.Challenges.Pending);
            Assert.IsNull(s.Challenges.Current);
            Assert.AreNotEqual(ChallengeOutcome.Declined, outcome, "a challenge of its own rank is fought");
        }

        [Test]
        public void ThePilot_SuesForPeace_InALongWar()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            Assert.IsNull(s.Wars.DeclareOn("Famille Ruan"));
            s.Context.Clock.Restore(s.Clock.Year + 3, s.Clock.Phase);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Wars.ClanWars);
        }

        [Test]
        public void ThePilot_PlantsATalismanSeed_WhenTheMirrorCan()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            var mortal = Fixtures.Mortal(age: 14);
            s.Clan.AddMember(mortal);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.HasTalismanSeed));
        }

        // ---- The pilot sounds its newcomers first (2026-10-01: unsounded spies fed the knowers the mirror) ----

        private static CharacterData Newcomer(GameSession s, string from)
        {
            var newcomer = Fixtures.Cultivator(age: 25);
            newcomer.FromFaction = from;
            s.Clan.AddMember(newcomer);
            return newcomer;
        }

        [Test]
        public void ThePilot_SoundsTheNewcomerOfTheStrongestPower_First()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            var weak = Newcomer(s, "Porte du Fer Ardent");        // a Foundation power
            var strong = Newcomer(s, "Secte du Pic des Nuées");   // a Golden Core one
            strong.SpyFor = "Secte du Pic des Nuées";
            s.Mirror.Restore(s.Context.Content.Balance.Intrigues.UnmaskMirrorCost + 20, 0); // one sounding, its reserve kept
            BalanceRun.Act(s);
            Assert.IsTrue(strong.SpyUnmasked, "the newcomer of the strongest power is sounded first");
            Assert.IsTrue(strong.DoubleAgent, "and turned");
            Assert.IsFalse(weak.SpyUnmasked);
        }

        [Test]
        public void ThePilot_SoundsBeforeItPlantsASeed()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            var spy = Newcomer(s, "Secte du Pic des Nuées");
            spy.SpyFor = "Secte du Pic des Nuées";
            s.Clan.AddMember(Fixtures.Mortal(age: 14)); // a seed candidate
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.TalismanSeedCost + 20, 0); // a seed or a sounding, not both
            BalanceRun.Act(s);
            Assert.IsTrue(spy.SpyUnmasked, "the sounding goes first");
        }

        [Test]
        public void ThePilot_SoundsANewcomer_AsSoonAsTheMirrorCanPay_WithoutAReserve()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            var spy = Newcomer(s, "Secte du Pic des Nuées");
            spy.SpyFor = "Secte du Pic des Nuées";
            s.Mirror.Restore(s.Context.Content.Balance.Intrigues.UnmaskMirrorCost, 0); // just the sounding's price
            BalanceRun.Act(s);
            Assert.IsTrue(spy.SpyUnmasked, "a spy waiting a year feeds a knower: the sounding comes before any reserve");
        }

        [Test]
        public void ThePilot_SoundsOnlyThoseComeFromAPower_NeverTheFounders()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            Assume.That(s.Clan.LivingMembers.Any(m => m.FatherID == null && m.MotherID == null), "founders without parents");
            int power = s.Context.Content.Balance.Intrigues.UnmaskMirrorCost + 20;
            s.Mirror.Restore(power, 0);
            BalanceRun.Act(s);
            Assert.AreEqual(power, s.Mirror.MirrorPower, "nobody came from outside: nothing spent on sounding");
        }

        private const int OtherPerils = 3; // ruins, a tomb, the Great Void, a patron's wrath: deaths « in combat » no counter tracks

        [Test]
        public void ALongGame_WithTheActivePilot_StaysSane([Values(1, 2, 3)] int seed)
        {
            var run = BalanceRun.Play(Fixtures.Content, seed, years: 150, out _, autopilot: true);
            Assert.That(run.Betrayals, Is.LessThanOrEqualTo(24), "a treaty is betrayed for a reason, not as a matter of course (a score of vassals and pacts since 2026-10-01)");
            Assert.That(run.CombatDeaths, Is.LessThanOrEqualTo(run.Challenges + run.ClanWars + run.Hunts + OtherPerils),
                "a challenge by the rules seldom kills (a failed hunt may; so may an expedition, the Great Void, a patron's wrath)");
            Assert.That(run.Devoured, Is.LessThanOrEqualTo(3), "a prudent clan keeps most of its ripe Daos");
            Assert.That(run.Strikes, Is.LessThanOrEqualTo(40), "no chain reaction of blows");
            Assert.That(run.ClanWars, Is.LessThanOrEqualTo(10), "no endless wars against the clan");
            Assert.IsFalse(run.Lost, "a prudent clan answers its threats and endures");
        }

        // ---- The pilot hunts (2026-09-29) ----

        private static GameSession InTheHuntWindow()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Talismans.RestoreCalendar(s.Clock.Year); // the ritual's year: the window is open
            return s;
        }

        [Test]
        public void ThePilot_Scouts_WhenItKnowsNoBeast()
        {
            var s = InTheHuntWindow();
            Assume.That(s.Bestiary.Beasts.Any(b => s.Knowledge.Knows(MirrorChronicles.World.FactKind.Beast, b.Id)), Is.False);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.CurrentTask == TaskType.ScoutBeasts));
        }

        [Test]
        public void ThePilot_HuntsABeastItCanTake()
        {
            var s = InTheHuntWindow();
            int strongest = s.Clan.LivingMembers.Where(s.Hunts.IsFree).Max(m => MirrorChronicles.Mirror.HuntRules.Power(m.Realm, m.RealmStage));
            var prey = s.Bestiary.Beasts.First(b => MirrorChronicles.Mirror.HuntRules.Power(b.Realm, b.Stage) < strongest);
            s.Knowledge.Reveal(MirrorChronicles.World.FactKind.Beast, prey.Id, MirrorChronicles.World.KnowledgeSource.Studied);
            bool? taken = null;
            s.Events.OnHunt += (_, captured) => taken = captured;
            BalanceRun.Act(s);
            Assert.IsNotNull(taken, "a hunt was carried out");
        }

        [Test]
        public void AMemberAwayOnAnOperation_TakesNoOtherTask()
        {
            var s = InTheHuntWindow();
            var hunter = s.Clan.LivingMembers.First(s.Hunts.IsFree);
            hunter.CurrentTask = TaskType.HuntBeast; // sent out this year
            Assert.IsFalse(s.Tasks.AssignTask(hunter, TaskType.Mine), "away for the year");
            Assert.AreEqual(TaskType.HuntBeast, hunter.CurrentTask);
        }

        [Test]
        public void TheChronicle_TellsAHunt()
        {
            var s = InTheHuntWindow();
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            s.Events.TriggerHunt("a-beast", true);
            StringAssert.Contains("bête", chronicle.Entries.Last());
        }

        [Test]
        public void ThePilot_OffersItsBeast_AndTakesATalisman()
        {
            var s = InTheHuntWindow();
            s.Resources.AddPrayers(s.Context.Content.Balance.Talismans.PrayersPerRitual);
            s.Resources.AddBeast(new CapturedBeast("taken", CultivationRealm.QiRefinement, 5, null));
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.TalismanQiId != null), "the ritual performed, a talisman chosen");
        }

        // ---- The pilot answers for its captives and its secret (2026-09-29) ----

        private static (GameSession s, CharacterData captive) Captive(bool knowsTheMirror = false)
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            var captive = s.Clan.LivingMembers.First(m => SpiritualOrificeRules.CanCultivate(m) && m.ID != s.Clan.PatriarchID);
            captive.KnowsMirrorSecret = knowsTheMirror;
            s.Captives.Take(captive, "Famille Ruan");
            return (s, captive);
        }

        [Test]
        public void ThePilot_TradesAnAgent_ForItsCaptive()
        {
            var (s, captive) = Captive();
            s.Captives.Imprison(new Prisoner("agent", "Famille Ruan", CultivationRealm.QiRefinement, s.Clock.Year));
            int stones = s.Resources.SpiritStones;
            BalanceRun.Act(s);
            Assert.IsNull(captive.CaptorFaction);
            Assert.IsEmpty(s.Captives.Prisoners);
        }

        [Test]
        public void ThePilot_BuysBackACaptive_WhenItCan()
        {
            var (s, captive) = Captive();
            s.Resources.SetSpiritStones(100000);
            BalanceRun.Act(s);
            Assert.IsNull(captive.CaptorFaction);
        }

        [Test]
        public void ThePilot_SilencesACaptive_ItCannotBuy()
        {
            var (s, captive) = Captive(knowsTheMirror: true);
            s.Resources.SetSpiritStones(0);
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            BalanceRun.Act(s);
            Assert.IsFalse(captive.KnowsMirrorSecret, "nothing left to tell");
        }

        [Test]
        public void ThePilot_MakesAnInvestigatorDoubt()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            s.Suspicion.AddMirrorClues("Secte du Pic des Nuées", 100);
            s.Secrets.RestoreConfrontation(new Confrontation("Secte du Pic des Nuées", 1));
            s.Mirror.Restore(65, 0); // enough for one blur — or for a seed first
            s.Clan.AddMember(Fixtures.Mortal(age: 14)); // a seed candidate, tempting the mirror's power
            bool seized = false;
            s.Events.OnMirrorSeized += _ => seized = true;
            BalanceRun.Act(s);
            s.Secrets.ProcessYear();
            Assert.IsFalse(seized, "the confrontation is answered before anything else");
            Assert.IsNull(s.Secrets.Confrontation, "the investigator doubts");
        }

        /// <summary>
        /// Where the mirror's clues come from (diagnostic, 2026-10-01): per source, what the powers whose elder knows the
        /// mirror gained over long runs, and who seized it. <c>BALANCE_SEEDS</c>/<c>BALANCE_YEARS</c>, category MirrorTrail.
        /// </summary>
        [Test, Explicit, Category("MirrorTrail")]
        public void MirrorTrail()
        {
            int seeds = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_SEEDS"), out var n) ? n : 10;
            int years = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_YEARS"), out var y) ? y : 500;
            var total = new System.Collections.Generic.Dictionary<string, int>();
            var text = new System.Text.StringBuilder();
            for (int seed = 1; seed <= seeds; seed++)
            {
                var gains = new System.Collections.Generic.Dictionary<string, int>();
                string seizedBy = null;
                int seizedYear = 0;
                BalanceRun.Play(Fixtures.Content, seed, years, out _, autopilot: true, observe: s =>
                {
                    s.Suspicion.OnMirrorClues += (faction, amount, source) =>
                    {
                        if (amount <= 0 || !s.Lore.Knows(faction)) return;
                        gains[source] = gains.TryGetValue(source, out int g) ? g + amount : amount;
                    };
                    s.Events.OnMirrorSeized += f => { seizedBy = f; seizedYear = s.Clock.Year; };
                });
                text.AppendLine($"seed {seed}: seized by {seizedBy ?? "-"} (year {seizedYear}) — "
                    + string.Join(", ", gains.OrderByDescending(p => p.Value).Select(p => $"{p.Key} {p.Value}")));
                foreach (var p in gains) total[p.Key] = total.TryGetValue(p.Key, out int t) ? t + p.Value : p.Value;
            }
            text.AppendLine("total: " + string.Join(", ", total.OrderByDescending(p => p.Value).Select(p => $"{p.Key} {p.Value}")));
            TestContext.Progress.WriteLine(text.ToString());
        }

        /// <summary>
        /// What stands between the clan and its endings (diagnostic, 2026-10-01): per run, the richest treasury, the Purple
        /// Mansions and how close they came to a Golden Core, the cultivators a sect needs, the vassals. Category EndingsTrail.
        /// </summary>
        [Test, Explicit, Category("EndingsTrail")]
        public void EndingsTrail()
        {
            int seeds = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_SEEDS"), out var n) ? n : 10;
            int years = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_YEARS"), out var y) ? y : 500;
            var text = new System.Text.StringBuilder();
            for (int seed = 1; seed <= seeds; seed++)
            {
                int demons = 0, positions = 0, maxStones = 0, pmYears = 0, maxPm = 0, maxAbilities = 0, fullXpYears = 0, readyYears = 0, routeYears = 0, maxCultivators = 0, maxVassals = 0, sectYears = 0;
                BalanceRun.Play(Fixtures.Content, seed, years, out var played, autopilot: true, observe: s =>
                {
                    s.Events.OnMetalEssenceDemon += _ => demons++;
                    s.Events.OnPositionTaken += (_, _, _) => positions++;
                    s.Events.OnYearStarted += _ =>
                    {
                        var free = s.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
                        var pm = free.Where(m => m.Realm == CultivationRealm.PurpleMansion).ToList();
                        maxStones = Math.Max(maxStones, s.Resources.SpiritStones);
                        pmYears += pm.Count;
                        maxPm = Math.Max(maxPm, pm.Count);
                        if (pm.Count > 0) maxAbilities = Math.Max(maxAbilities, pm.Max(m => m.DivineAbilities.Count));
                        int xp = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
                        if (pm.Any(m => m.CultivationXP >= xp)) fullXpYears++;
                        var ready = pm.Where(m => m.CultivationXP >= xp && m.DivineAbilities.Count >= GoldenCoreRules.AbilitiesToForge).ToList();
                        if (ready.Count > 0) readyYears++;
                        if (ready.Any(m => s.Context.Content.Fruitions.Any(f => GoldenCoreRules.RouteTo(m.DivineAbilities, f.Id, s.Context.Content.Fruitions) != PositionRoute.None))) routeYears++;
                        int cultivators = free.Count(m => m.Realm >= CultivationRealm.QiRefinement);
                        maxCultivators = Math.Max(maxCultivators, cultivators);
                        maxVassals = Math.Max(maxVassals, s.Treaties.All.Count(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain));
                        if (pm.Count > 0 && cultivators >= s.Context.Content.Balance.Sect.MinCultivators) sectYears++;
                    };
                });
                text.AppendLine($"seed {seed}: maxStones {maxStones}, pmYears {pmYears} (max {maxPm} at once, best {maxAbilities} abilities), "
                    + $"fullXp {fullXpYears}y, ready {readyYears}y, route {routeYears}y, cultivators max {maxCultivators}, sect-ready-but-stones {sectYears}y, vassals max {maxVassals}, "
                    + $"golden cores {played.Clan.Registry.Records.Count(r => r.Realm >= CultivationRealm.GoldenCore)}, demons {demons}, positions {positions}, endings: "
                    + string.Join(", ", played.Annals.Entries.Where(e => e.Kind == AnnalKind.EndingReached).Select(e => $"{e.Ref} ({e.Year})")));
            }
            TestContext.Progress.WriteLine(text.ToString());
        }

        /// <summary>
        /// The mirror's shards (diagnostic, 2026-10-01): per run, the shards recovered and when, then for each one missing what
        /// stands in the way — ruins never found, a holder unknown or unwilling, the Great Void unopened. Category ShardsTrail.
        /// </summary>
        [Test, Explicit, Category("ShardsTrail")]
        public void ShardsTrail()
        {
            int seeds = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_SEEDS"), out var n) ? n : 10;
            int years = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_YEARS"), out var y) ? y : 500;
            var text = new System.Text.StringBuilder();
            for (int seed = 1; seed <= seeds; seed++)
            {
                var taken = new System.Collections.Generic.List<string>();
                BalanceRun.Play(Fixtures.Content, seed, years, out var s, autopilot: true, observe: x =>
                    x.Events.OnShardRecovered += shard => taken.Add($"{shard.Id} ({x.Clock.Year})"));
                var missing = s.Context.Content.Shards.Where(sh => !s.Shards.IsRecovered(sh.Id)).Select(sh =>
                {
                    switch (sh.Source)
                    {
                        case ShardSource.Ruins: return $"{sh.Id}: ruins {(s.Shards.RevealedRuins.Contains(sh.Id) ? "found" : "never found")}";
                        case ShardSource.Power:
                            string holder = s.PowerShards.HolderOf(sh.Id);
                            var power = s.Factions.GetFactionByName(holder ?? "");
                            bool vassal = s.Treaties.All.Any(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain && t.Faction == holder);
                            return $"{sh.Id}: holder {holder ?? "none"} known={s.PowerShards.KnownByClan(sh.Id)} vassal={vassal} knower={s.Lore.Knows(holder)} realm={power?.HighestRealm} price={(power == null ? 0 : s.PowerShards.TradePrice(sh.Id))}";
                        case ShardSource.GreatVoid: return $"{sh.Id}: void open={s.Shards.CanTraverseVoid}";
                        default: return $"{sh.Id}: {sh.Source}";
                    }
                });
                text.AppendLine($"seed {seed}: {taken.Count} taken — {string.Join(", ", taken)}");
                foreach (var m in missing) text.AppendLine($"    {m}");
            }
            TestContext.Progress.WriteLine(text.ToString());
        }

        /// <summary>The report behind the tuning: <c>./Scripts/dev.sh balance</c> (seeds × years, env BALANCE_SEEDS/BALANCE_YEARS).</summary>
        [Test, Explicit, Category("Balance")]
        public void Report()
        {
            int seeds = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_SEEDS"), out var s) ? s : 10;
            int years = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_YEARS"), out var y) ? y : 150;
            bool autopilot = Environment.GetEnvironmentVariable("BALANCE_AUTOPILOT") != "0";
            var runs = Enumerable.Range(1, seeds).Select(seed => BalanceRun.Play(Fixtures.Content, seed, years, out _, autopilot)).ToList();
            TestContext.Progress.WriteLine(BalanceRun.Table(runs));
        }
    }
}
