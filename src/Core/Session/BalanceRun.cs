using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.World;

namespace MirrorChronicles.Session
{
    /// <summary>What befell the clan and the powers in one automatic game (counts for the whole run, and the final state).</summary>
    public sealed record BalanceReport(
        int Seed, int Years, bool Lost, int Members, int Powers, int Stones, CultivationRealm BestRealm,
        int Strikes, int Captures, int Coalitions, int ClanWars, int PowerWars, int Peaces, int Absorptions,
        int Betrayals, int Blackmails, int Thefts, int ProbesSpotted, int Challenges, int Deaths, int CombatDeaths, int PoorYears,
        int Cultivators, int Devoured, int Extortions, int Foiled, int Hunts, int BeastsTaken, int Shards, int Endings,
        int PurpleMansionYear, int FirstEndingYear); // 0: never

    /// <summary>
    /// Long automatic games (balance, 2026-09-29): a passive clan — no orders given, or only the idle set to work — lives through the years while the
    /// world plots, probes, bands and wars. The run counts what befell it, so the tuning of balance.json can check that
    /// none of it runs away. Engine-free: <c>./Scripts/dev.sh balance</c> prints <see cref="Table"/> over many seeds.
    /// </summary>
    public static partial class BalanceRun
    {
        /// <param name="autopilot">Each year the pilot acts (<see cref="Act"/>) and sets the free members to work (<see cref="SetTheIdleToWork"/>).</param>
        /// <param name="observe">Hooks a diagnostic on the new session before the first year (e.g. where the mirror's clues come from).</param>
        public static BalanceReport Play(GameContent content, int seed, int years, out GameSession session, bool autopilot = false,
            Action<GameSession> observe = null)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = seed, Content = content });
            observe?.Invoke(s);
            var counts = new Dictionary<string, int>();
            void Count(string key) => counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
            var bus = s.Events;
            bus.OnClanStruck += _ => Count("strike");
            bus.OnMemberCaptured += (_, _) => Count("capture");
            bus.OnCoalitionFormed += _ => Count("coalition");
            bus.OnWarBegun += (a, d) => Count(a == SecretBook.ClanHolder || d == SecretBook.ClanHolder ? "clanWar" : "powerWar");
            bus.OnPeace += (_, _) => Count("peace");
            bus.OnPowerAbsorbed += (_, _) => Count("absorption");
            bus.OnTreatyBetrayed += _ => Count("betrayal");
            bus.OnBlackmail += _ => Count("blackmail");
            bus.OnExtortion += _ => Count("extortion");
            bus.OnDaoHuntFoiled += _ => Count("foiled");
            bus.OnHunt += (_, captured) => { Count("hunt"); if (captured) Count("capture-beast"); };
            bus.OnTheft += (_, _) => Count("theft");
            bus.OnAncestorReborn += (_, _) => Count("reborn");
            bus.OnChosenHarvested += _ => Count("chosenHarvested");
            bus.OnProbeSpotted += _ => Count("probe");
            bus.OnChallengeSettled += (_, _) => Count("challenge");
            bus.OnCharacterDied += (_, cause) =>
            {
                Count("death");
                if (cause == DeathCause.Combat) Count("combatDeath");
                if (cause == DeathCause.RipeDaoHarvested) Count("devoured");
            };

            int start = s.Clock.Year;
            while (s.Clock.Year - start < years && !s.Victory.IsOver)
            {
                if (autopilot)
                {
                    Act(s);
                    SetTheIdleToWork(s);
                }
                s.AdvanceYear();
                if (s.Upkeep.Impoverished) Count("poor");
            }

            session = s;
            int Get(string key) => counts.TryGetValue(key, out int n) ? n : 0;
            var living = s.Clan.LivingMembers.ToList();
            return new BalanceReport(seed, s.Clock.Year - start, s.Victory.GameLost, living.Count,
                s.Factions.Factions.Count, s.Resources.SpiritStones,
                living.Count == 0 ? CultivationRealm.Embryonic : living.Max(m => m.Realm),
                Get("strike"), Get("capture"), Get("coalition"), Get("clanWar"), Get("powerWar"), Get("peace"), Get("absorption"),
                Get("betrayal"), Get("blackmail"), Get("theft"), Get("probe"), Get("challenge"), Get("death"), Get("combatDeath"), Get("poor"),
                living.Count(SpiritualOrificeRules.CanCultivate), Get("devoured"), Get("extortion"), Get("foiled"), Get("hunt"), Get("capture-beast"),
                s.Mirror.RestoredFragments, s.Annals.Entries.Count(e => e.Kind == AnnalKind.EndingReached),
                s.Annals.Entries.FirstOrDefault(e => e.Kind == AnnalKind.RealmReached && e.Value == (int)CultivationRealm.PurpleMansion)?.Year ?? 0,
                s.Annals.Entries.FirstOrDefault(e => e.Kind == AnnalKind.EndingReached)?.Year ?? 0);
        }

        private const int ReserveYears = 2;      // the pilot pays a demand only if it keeps two years of upkeep,
        private const int ProtectionReserveYears = 1; // one for the price of protection
        private const int PeaceAfterYears = 2;   // it sues for peace after two years of war
        private const int TreatyEveryYears = 5;  // it seeks a treaty every five years
        private const int MostTreaties = 3;      // and keeps a few, not a web of them
        /// <summary>
        /// The mirror's power the pilot keeps for what it may not forgo: the price of a doubt sown in an investigator — the
        /// blur — whose lack loses the mirror (2026-10-01: an investigator came when the mirror held 36 of the 40 it needed).
        /// </summary>
        private static int MirrorReserve(GameSession session) => session.Context.Content.Balance.Plots.BlurMirrorCost;
        private const double RebuildMargin = 1.5; // a thin reserve is rebuilt, not merely kept
        private const int FewBearers = 8;        // below this many orifice bearers the line thins (2026-10-03)
        private const int EndOfLifeYears = 10;   // with so few years left, a bearer dares its trial anyway
        private const int SeedMinAge = 10;       // a seed for the young: old enough to be examined,
        private const int SeedMaxAge = 30;       // young enough to cultivate long

        /// <summary>
        /// The active pilot (user decision 2026-09-29): what a prudent clan does each year before its tasks — it pays a
        /// demand it can afford and refuses the rest, answers a challenge of its own rank with its best fighters (who
        /// then fight on their own) and flees the others, sues for peace after two years of war, plants a Talisman Seed
        /// in a young examined mortal when the mirror can spare it, hides its ripe Daos in seclusion, weds its cultivators to cultivators (sought abroad when none is free), scouts and
        /// hunts in the window and offers its beast to the mirror, answers for its captives and makes an investigator doubt, and seeks a non-aggression pact now and then (a few at most).
        /// </summary>
        public static void Act(GameSession session)
        {
            // the mirror's power goes first to an investigator: made to doubt, it cannot act (a seed can wait a year)
            if (session.Secrets.Confrontation is { } investigator) session.Secrets.BlurMemories(investigator.Faction);
            SoundTheNewcomers(session); // then to the strangers in the house, before any seed (2026-10-01)
            // then to the method of the ascent, without which the line never rises past the Foundation (2026-10-01: seeds ate it)
            if (AscentMethod(session) == null && session.Deduction.AscentRefusal() == null) session.Deduction.DeduceAscentMethod();
            AnswerForTheCaptives(session); // then its captives, before any stone is spent (2026-10-01: its Foundations were executed)
            SwearTheKeepers(session);      // and those in the mirror's secret swear to keep it
            RiseThroughThePurpleMansion(session); // then to the road to the Golden Core (BalanceRun.Ascent.cs)
            AnswerDemands(session);
            AnswerChallenge(session);
            foreach (var war in session.Wars.ClanWars.Where(w => session.Clock.Year - w.StartYear >= PeaceAfterYears).ToList())
                session.Wars.SuePeace(war.Enemy);
            HoldForAnHeir(session);
            PlantASeed(session);
            WedTheLine(session);
            Hunt(session);
            SeekAMethodToTheAscent(session);
            ProbeAPower(session);
            SeekTheShards(session);
            if (session.Sect.FoundingRefusal() == null) session.Sect.Found(); // the Double House as soon as it can
            if (session.Imperial.FoundingRefusal() == null) session.Imperial.Found(); // then the kingdom: the Imperial Way (R20)
            AnswerTheDemons(session);
            KeepTheTreasures(session);
            ArmTheClan(session);
            OfferToTheMirror(session);
            foreach (var prey in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Retreat == Retreat.None
                && m.CurrentTask != TaskType.Seclusion && FoundationRules.IsPrey(m, session.Context.Content) && session.DaoHunts.IsCoveted(m)).ToList())
                session.Tasks.AssignTask(prey, TaskType.Seclusion); // a ripe Dao someone covets hides
            if (session.Clock.Year % TreatyEveryYears == 0 && session.Treaties.All.Count < MostTreaties) SeekATreaty(session);
        }

        /// <summary>
        /// The mirror's shards (B3c): an expedition to known ruins when the odds are good, the Great Void once it calls, and a
        /// power's shard the clan has pierced — asked of a vassal, bought when the holder sells, else stolen when the odds are good.
        /// </summary>
        private static void SeekTheShards(GameSession session)
        {
            var shards = session.Shards;
            var settings = session.Context.Content.Balance.Shards;
            if (LineIsThin(session)) return; // an heir first: no deadly expedition nor theft while the bearers are few (2026-10-03)
            foreach (var ruins in shards.RevealedRuins.ToList())
            {
                var team = shards.BestTeam(CultivationRealm.QiRefinement, settings.ExpeditionMaxTeam);
                var shard = session.Context.Content.Shards.First(x => x.Id == ruins);
                if (shards.ExpeditionRefusal(ruins, team, out var members) == null && shards.ExpeditionChance(members, shard) >= GoodOdds)
                    shards.Expedition(ruins, team);
            }
            var seeker = shards.BestTeam(CultivationRealm.PurpleMansion, 1);
            if (seeker.Count == 1) shards.VoidSearch(seeker[0]);
            var wary = Wary.GetOrCreateValue(session);
            foreach (var shard in session.PowerShards.KnownToClan().Where(id => !wary.Contains(session.PowerShards.HolderOf(id))))
            {
                if (session.PowerShards.DemandOfVassal(shard) == null) continue;
                string holder0 = session.PowerShards.HolderOf(shard);
                int before = session.Suspicion.MirrorClues(holder0);
                if (session.PowerShards.Trade(shard) == null) continue;
                if (session.Suspicion.MirrorClues(holder0) > before) // it refused, and seemed suddenly very interested in the clan
                {
                    wary.Add(holder0); // never asked again, nor robbed: it knows what the shard is (2026-10-01: a yearly ask lost the mirror)
                    continue;
                }
                var team = shards.BestTeam(CultivationRealm.QiRefinement, settings.ExpeditionMaxTeam);
                var holder = session.Factions.GetFactionByName(session.PowerShards.HolderOf(shard));
                if (team.Count > 0 && session.PowerShards.TheftChance(team.Select(session.Clan.FindById).ToList(), holder) >= GoodOdds)
                    session.PowerShards.Steal(shard, team);
            }
        }

        /// <summary>
        /// A method that leads to the Purple Mansion (its secret, LORE.md §5.3.4; grade 5 and above) is never bought (§11.10):
        /// until the clan knows one, the pilot has the mirror deduce it as soon as the mirror can.
        /// </summary>
        private static void SeekAMethodToTheAscent(GameSession session)
        {
            if (AscentMethod(session) == null && session.Deduction.AscentRefusal() == null) session.Deduction.DeduceAscentMethod();
            AnswerForTheCaptives(session); // then its captives, before any stone is spent (2026-10-01: its Foundations were executed)
            if (session.Sponsorships.Pending != null) session.Sponsorships.Accept(); // a patron's gift, whatever it hides (§11.10)
            foreach (var due in session.Sponsorships.Awaiting.ToList()) // resist a weaker patron, yield to a stronger one
            {
                var patron = session.Factions.GetFactionByName(due.Power);
                if (patron != null && Diplomacy.WarRules.Strength(patron, session.Context.Content.Balance.Wars) < session.Wars.ClanWarStrength())
                    session.Sponsorships.Resist(due.Id);
                else session.Sponsorships.Yield(due.Id);
            }
            foreach (var s in session.Sponsorships.Active.Where(x => !x.Cleansed).ToList()) session.Sponsorships.Cleanse(s.Id); // when the mirror can
            var method = AscentMethod(session);
            if (method == null) return;
            foreach (var member in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.QiId == method.RequiredQiId
                && m.CultivationMethodId != method.ID && TechniqueRules.CanPractise(m, method)).ToList())
                session.Techniques.AssignMethod(member, method.ID); // the cultivators of its Qi take it up
        }

        /// <summary>A method of the Qi Cultivation that holds the ascent's secret and whose Qi can still be gathered.</summary>
        private static bool LeadsToTheAscent(GameSession session, TechniqueData method) =>
            method.Kind == TechniqueKind.Cultivation && method.RequiredQiId != null && TechniqueRules.HasPurpleMansionSecret(method)
            && session.Techniques.FindQi(method.RequiredQiId) is { Vanished: false, Ubiquitous: false };

        private static TechniqueData AscentMethod(GameSession session) =>
            session.Techniques.Known.Where(t => LeadsToTheAscent(session, t)).OrderByDescending(t => t.Grade).FirstOrDefault();

        /// <summary>
        /// While the clan lacks the portions its youth needs to enter its best method (the ascent's, once bought), two breathing
        /// cultivators who perceive Qi gather it (they harvest the best method's Qi, TaskAssignmentSystem.GatherQi): without them,
        /// the youth waits at the sixth chakra for ever (2026-09-30).
        /// </summary>
        private static void GatherTheQiOfTheAscent(GameSession session, bool huntOpen)
        {
            var method = AscentMethod(session) ?? session.Techniques.Known
                .Where(t => t.Kind == TechniqueKind.Cultivation && t.RequiredQiId != null && session.Techniques.FindQi(t.RequiredQiId) is { Vanished: false, Ubiquitous: false })
                .OrderByDescending(t => t.Grade).FirstOrDefault();
            if (method == null || session.Resources.QiPortions(method.RequiredQiId) >= AscentQiReserve) return;
            var harvesters = session.Clan.LivingMembers
                .Where(m => m.CaptorFaction == null && m.Realm == CultivationRealm.Embryonic && TaskRules.IsAllowed(m, TaskType.GatherQi, huntOpen))
                .OrderByDescending(m => m.RealmStage).Take(AscentHarvesters).ToList();
            foreach (var harvester in harvesters) session.Tasks.AssignTask(harvester, TaskType.GatherQi);
        }

        /// <summary>
        /// A probe a year (LORE.md §11.5) by the best free member but the patriarch, when the clan has cultivators to spare:
        /// the way a player pieces the powers' secrets together and finds where the shards lie. A treasure is a secret of the
        /// highest rank, under all the lesser ones: the pilot digs one power until it has nothing left to tell (2026-10-01:
        /// rank-4 treasures were never pierced by probes scattered every third year).
        /// </summary>
        private static void ProbeAPower(GameSession session)
        {
            var plan = NextProbe(session);
            if (plan == null) return;
            session.Probes.Probe(plan);
            Dug(session, plan.Target);
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GameSession, string[]> Digging = new();

        /// <summary>Remembers the power the pilot is digging.</summary>
        public static void Dug(GameSession session, string power)
        {
            Digging.Remove(session);
            Digging.Add(session, new[] { power });
        }

        /// <summary>
        /// The probe the pilot would send this year, or null: the power it is digging while it has something left to tell,
        /// the powers of a region where the mirror sensed a shard first (ShardSense), else a vassal of the clan's, else the power where the odds are best — by the mirror's sight when the mirror can
        /// spare it (it is never seen), else by infiltration or a bribe the reserve allows, from fair odds.
        /// </summary>
        public static ProbePlan NextProbe(GameSession session)
        {
            var cultivators = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm >= CultivationRealm.QiRefinement).ToList();
            if (cultivators.Count < ProbeMinCultivators) return null; // a small clan does not risk its few cultivators outside
            var team = session.Shards.BestTeam(CultivationRealm.QiRefinement, 2).Where(id => id != session.Clan.PatriarchID).Take(1).ToList();
            if (team.Count == 0) return null;
            string digging = Digging.TryGetValue(session, out var dug) ? dug[0] : null;
            var vassals = session.Treaties.All.Where(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain).Select(t => t.Faction).ToHashSet();
            bool canBribe = session.Resources.SpiritStones - ProbeBribe >= session.Upkeep.YearlyUpkeep * ProbeReserveYears;
            var sensed = session.ShardSense.Directions.Where(d => !session.Shards.IsRecovered(d.Key) && !session.PowerShards.KnownByClan(d.Key))
                .Select(d => d.Value).ToHashSet(); // the regions where the mirror sensed a shard still to find
            var best = session.Factions.Factions
                .SelectMany(f => new[]
                {
                    new ProbePlan(f.Name, ProbeApproach.MirrorSight, team, new List<string>(), 0),
                    new ProbePlan(f.Name, ProbeApproach.Infiltration, team, new List<string>(), 0),
                    canBribe ? new ProbePlan(f.Name, ProbeApproach.Bribery, team, new List<string>(), ProbeBribe) : null
                })
                .Where(p => p != null && session.Probes.RefusalOf(p) == null)
                .Where(p => p.Approach != ProbeApproach.MirrorSight || MirrorSpares(session, p.Target))
                .Select(p => (Plan: p, Odds: session.Probes.ChanceAgainst(p)))
                .Where(x => x.Odds >= FairOdds)
                .OrderBy(x => sensed.Contains(session.Factions.GetFactionByName(x.Plan.Target)?.RegionId) ? 0
                    : x.Plan.Target == digging ? 1 : vassals.Contains(x.Plan.Target) ? 2 : 3)
                .ThenByDescending(x => x.Odds).FirstOrDefault();
            return best.Plan;
        }

        /// <summary>
        /// The mirror's sight costs by the rank of the next secret; the pilot keeps its reserve for the rest, and never looks
        /// while a newcomer waits to be sounded (2026-10-01: a yearly sight starved the soundings, and a spy fed a knower).
        /// </summary>
        private static bool MirrorSpares(GameSession session, string target)
        {
            if (UnsoundedNewcomers(session).Any()) return false;
            int rank = session.SecretBook.NextUnknown(SecretBook.ClanHolder, target)?.Rank ?? 1;
            return session.Mirror.MirrorPower >= session.Context.Content.Balance.Secrets.MirrorSightCostPerRank * rank + MirrorReserve(session);
        }

        private const int AscentQiReserve = 2;   // portions kept for the youth entering the ascent's method
        private const int AscentHarvesters = 2;  // breathing cultivators gathering it
        private const double FairOdds = 0.3;     // the pilot probes from these odds
        private const int ProbeBribe = 500;      // the stones of a bribe
        private const int ProbeMinCultivators = 4;
        private const int ProbeReserveYears = 5; // a bribe only from a well-filled treasury

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GameSession, HashSet<string>> Sounded = new();

        /// <summary>The holders that refused a shard's trade with too keen an interest: the pilot leaves them be.</summary>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GameSession, HashSet<string>> Wary = new();

        /// <summary>
        /// The mirror sounds once every member come from a power (a spouse, a defector, a joiner), when its power allows — those of
        /// the strongest powers first, where an elder may know the mirror; an unmasked spy is turned into a double agent (LORE.md
        /// §11.5; 2026-10-01: unsounded spies, waiting their turn for decades, fed the knowers the mirror).
        /// </summary>
        private static IEnumerable<CharacterData> UnsoundedNewcomers(GameSession session)
        {
            var sounded = Sounded.GetOrCreateValue(session);
            return session.Clan.LivingMembers.Where(m => m.FromFaction != null && m.CaptorFaction == null && !sounded.Contains(m.ID));
        }

        private static void SoundTheNewcomers(GameSession session)
        {
            var sounded = Sounded.GetOrCreateValue(session);
            int Rank(CharacterData m) => session.Factions.GetFactionByName(m.FromFaction) is { } f ? (int)f.HighestRealm : -1;
            foreach (var member in UnsoundedNewcomers(session).OrderByDescending(Rank).ToList())
            {
                // no reserve kept: the mirror regains a point a year, and a spy unsounded feeds its master five clues a year (2026-10-01)
                if (session.Mirror.MirrorPower < session.Context.Content.Balance.Intrigues.UnmaskMirrorCost) return;
                if (session.Intrigues.Unmask(member.ID) == null) return;
                sounded.Add(member.ID);
                if (member.SpyUnmasked) session.Intrigues.Turn(member.ID);
            }
        }

        private const double GoodOdds = 0.6; // the pilot risks an expedition or a theft from these odds
        private const int LowTalentRoot = 40; // a cultivator of a lesser root may be sent to the mine; a gifted one never

        private static void AnswerDemands(GameSession session)
        {
            foreach (var demand in session.Intrigues.Demands.ToList())
            {
                // protection costs what a peace would (a share of the hoard), war besides: it is paid down to a year's upkeep (2026-10-01)
                int reserve = demand.Kind == DemandKind.Protection ? ProtectionReserveYears : ReserveYears;
                bool affordable = session.Resources.SpiritStones - demand.Stones >= session.Upkeep.YearlyUpkeep * reserve;
                if (affordable) session.Intrigues.Pay(demand.Faction);
                else session.Intrigues.Refuse(demand.Faction);
            }
        }

        private static void AnswerChallenge(GameSession session)
        {
            var challenge = session.Challenges.Pending;
            if (challenge == null) return;
            int most = session.Context.Content.Balance.Challenges.MaxFighters;
            var fighters = session.Clan.LivingMembers.Where(m => session.Challenges.FighterRefusal(m) == null)
                .OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).Take(most).ToList();
            bool stronger = fighters.Count > 0 && (fighters[0].Realm > challenge.Realm
                || fighters[0].Realm == challenge.Realm && fighters[0].RealmStage > challenge.Stage);
            if (fighters.Count == 0 || fighters[0].Realm < challenge.Realm || challenge.ToTheDeath && !stronger) // a life only for an edge
            {
                session.Challenges.Decline();
                return;
            }
            if (session.Challenges.Accept(fighters.Select(m => m.ID).ToList()) != null)
            {
                session.Challenges.Decline(); // it could not answer: better to flee than to leave it hanging
                return;
            }
            session.Challenges.Current?.AutoPlay();
            session.Challenges.Conclude();
        }

        /// <summary>
        /// An heir first (user decision 2026-10-03, against the bearers' extinction): a childless orifice bearer holds back a
        /// deadly trial — the Foundation's wall, the Purple Mansion's ascent — while its odds are poor, save at its life's end.
        /// </summary>
        /// <summary>The orifice bearers are few: the pilot risks none of them in a deadly operation (2026-10-03, seed 9 lost three in four years).</summary>
        private static bool LineIsThin(GameSession session) => session.Clan.LivingMembers.Count(m => m.HasSpiritualOrifice) < FewBearers;

        /// <summary>A childless orifice bearer while the line is thin: the pilot risks its life in no strike.</summary>
        private static bool Spared(GameSession session, CharacterData m) =>
            LineIsThin(session) && m.HasSpiritualOrifice && !session.Clan.Registry.Records.Any(r => r.FatherID == m.ID || r.MotherID == m.ID);

        private static void HoldForAnHeir(GameSession session)
        {
            foreach (var m in session.Clan.LivingMembers.Where(m => m.HasSpiritualOrifice && m.CaptorFaction == null).ToList())
            {
                var trial = PowerLadder.Next(m.Realm, m.RealmStage).Trial;
                double odds = trial == TrialKind.FoundationWall ? session.Breakthroughs.CalculateSuccessRate(m) / 100.0
                    : trial == TrialKind.PurpleMansionAscension ? session.PurpleMansion.AscentChanceOf(m) / 100.0 : 1.0;
                bool heir = session.Clan.Registry.Records.Any(r => r.FatherID == m.ID || r.MotherID == m.ID);
                bool lastYears = m.MaxLifespan - m.Age <= EndOfLifeYears;
                m.HoldsTrial = !heir && !lastYears && odds < GoodOdds;
            }
        }

        private static void PlantASeed(GameSession session)
        {
            if (session.Mirror.MirrorPower < Mirror.MirrorSystem.TalismanSeedCost + MirrorReserve(session)) return;
            var candidates = session.Clan.LivingMembers
                .Where(m => m.CaptorFaction == null && m.OrificeKnown && !m.HasTalismanSeed && !SpiritualOrificeRules.CanCultivate(m)
                    && m.Age >= SeedMinAge && m.Age <= SeedMaxAge);
            bool few = session.Clan.LivingMembers.Count(m => m.HasSpiritualOrifice) < FewBearers; // the line thins: the most gifted (2026-10-03)
            var mortal = (few ? candidates.OrderByDescending(m => m.SpiritualRoot).ThenBy(m => m.Age) : candidates.OrderBy(m => m.Age)).FirstOrDefault();
            if (mortal != null) session.Mirror.GrantTalismanSeed(mortal);
        }

        /// <summary>Each unwed adult cultivator is wed to a cultivator of the clan, or else one is sought abroad when affordable.</summary>
        private static void WedTheLine(GameSession session)
        {
            var seekers = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.OrificeKnown && m.SpouseID == null
                && SpiritualOrificeRules.CanCultivate(m) && m.Age >= MarriageMatchmaker.MinMarriageAge && m.Age <= MarriageMatchmaker.SeekingAge(m))
                .ToList();
            foreach (var seeker in seekers.Where(m => m.SpouseID == null))
            {
                var partner = session.Clan.LivingMembers.OrderByDescending(p => p.HasSpiritualOrifice) // an orifice on both sides: half the children bear it
                    .FirstOrDefault(p => p.OrificeKnown && SpiritualOrificeRules.CanCultivate(p) && session.Marriages.MarriageRefusal(seeker, p) == null);
                if (partner != null)
                {
                    session.Marriages.Arrange(seeker.ID, partner.ID);
                    continue;
                }
                int cost = session.Context.Content.Balance.Lineage.SeekStones;
                if (session.Resources.SpiritStones - cost >= session.Upkeep.YearlyUpkeep * ReserveYears)
                    session.Marriages.SeekCultivatorSpouse(seeker.ID);
            }
        }

        /// <summary>
        /// In the hunt window: without a known beast, a scout goes out; with one it can take, the best free fighter
        /// strikes (a lookout beside), the solitary beasts first, a cover story when the beast has a master and the clan
        /// can pay. A ripe Dao is never sent out.
        /// </summary>
        private static void Hunt(GameSession session)
        {
            if (!session.Talismans.HuntWindowOpen) return;
            var content = session.Context.Content;
            var hunters = session.Clan.LivingMembers.Where(m => session.Hunts.IsFree(m) && !FoundationRules.IsPrey(m, content))
                .OrderByDescending(m => Mirror.HuntRules.Power(m)).ToList();
            if (hunters.Count == 0) return;
            int strength = Mirror.HuntRules.Power(hunters[0].Realm, hunters[0].RealmStage);
            var known = session.Bestiary.Beasts.Where(b => session.Knowledge.Knows(World.FactKind.Beast, b.Id)).ToList();
            var target = known.Where(b => Mirror.HuntRules.Power(b.Realm, b.Stage) < strength)
                .OrderBy(b => b.OwnerFaction != null).ThenBy(b => Mirror.HuntRules.Power(b.Realm, b.Stage)).FirstOrDefault();
            if (target == null)
            {
                var scout = hunters.LastOrDefault(m => m.CurrentTask != TaskType.ScoutBeasts);
                if (scout != null && !hunters.Any(m => m.CurrentTask == TaskType.ScoutBeasts))
                    session.Tasks.AssignTask(scout, TaskType.ScoutBeasts);
                return;
            }
            var striker = hunters.FirstOrDefault(m => !Spared(session, m) && Mirror.HuntRules.Power(m.Realm, m.RealmStage) > Mirror.HuntRules.Power(target.Realm, target.Stage));
            if (striker == null) return; // an heir first: no childless bearer strikes while the line is thin
            var team = new Dictionary<string, HuntRole> { [striker.ID] = HuntRole.Striker };
            var lookout = hunters.FirstOrDefault(m => m != striker);
            if (lookout != null) team[lookout.ID] = HuntRole.Lookout;
            int coverCost = content.Balance.Hunt.CoverStones[(int)CoverStory.Trade];
            bool cover = target.OwnerFaction != null && session.Resources.SpiritStones - coverCost >= session.Upkeep.YearlyUpkeep * ReserveYears;
            var plan = new HuntPlan { TargetBeastId = target.Id, Team = team, Cover = cover ? CoverStory.Trade : CoverStory.None };
            if (session.Hunts.Validate(plan) == null) session.Hunts.Execute(plan);
        }

        /// <summary>The ritual's year: a beast of rank is offered for the best cultivator without a talisman, who takes the first offered.</summary>
        private static void OfferToTheMirror(GameSession session)
        {
            var talismans = session.Talismans;
            if (talismans.PendingOffer == null)
            {
                var beast = session.Resources.Beasts.Where(b => Mirror.TalismanRules.RankOf(b) != null)
                    .OrderByDescending(b => Mirror.HuntRules.Power(b.Realm, b.Stage)).FirstOrDefault();
                var bearer = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.TalismanQiId == null && SpiritualOrificeRules.CanCultivate(m))
                    .OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).FirstOrDefault();
                if (beast == null || bearer == null || session.Clock.Year != talismans.NextRitualYear) return;
                if (!talismans.PerformRitual(bearer, beast)) return;
            }
            if (talismans.PendingOffer?.Choices.Count > 0) talismans.Choose(talismans.PendingOffer.Choices[0]);
        }

        /// <summary>
        /// Each captive: traded for an agent of its captor the clan holds, else bought back when the reserve allows, else —
        /// when it knows the mirror — silenced by the mirror, so nothing is left to tell.
        /// </summary>
        private static void AnswerForTheCaptives(GameSession session)
        {
            var captives = session.Captives;
            var schemes = session.Context.Content.Balance.Schemes;
            foreach (var captive in captives.Held.OrderByDescending(m => (int)m.Realm).ToList()) // the most precious first
            {
                var agent = captives.Prisoners.FirstOrDefault(p => p.Faction == captive.CaptorFaction);
                if (agent != null && captives.Exchange(captive.ID, agent.Id) == null) continue;
                int ransom = SchemeRules.Ransom(captive.Realm, schemes);
                if (session.Resources.SpiritStones - ransom >= session.Upkeep.YearlyUpkeep * RansomReserveYears(captive)
                    && captives.PayRansom(captive.ID) == null) continue;
                if (Rescue(session, captive)) continue;
                if (captive.KnowsMirrorSecret) captives.Silence(captive.ID);
            }
        }

        /// <summary>
        /// The reserve the clan keeps when it ransoms: none for a Foundation or above — the road to the Purple Mansion, its
        /// rarest — a year for a Qi cultivator, two for the rest (2026-10-01: captured Foundations were executed while the
        /// clan of six hundred kept its two years of upkeep).
        /// </summary>
        private static int RansomReserveYears(CharacterData captive) =>
            captive.Realm >= CultivationRealm.Foundation ? 0 : captive.Realm == CultivationRealm.QiRefinement ? ProtectionReserveYears : ReserveYears;

        /// <summary>A rescue by the best free team, when its odds against the captor are good. True when it brought the captive home.</summary>
        private static bool Rescue(GameSession session, CharacterData captive)
        {
            var captor = session.Factions.GetFactionByName(captive.CaptorFaction);
            if (captor == null) return false;
            var team = session.Shards.BestTeam(CultivationRealm.QiRefinement, session.Context.Content.Balance.Shards.ExpeditionMaxTeam)
                .Where(id => id != session.Clan.PatriarchID).Select(session.Clan.FindById)
                .Where(m => m != null && session.Hunts.IsFree(m)).ToList();
            if (team.Count == 0 || SchemeRules.RescueChance(team, captor, session.Context.Content.Balance.Schemes) < GoodOdds) return false;
            return session.Captives.Rescue(captive.ID, team.Select(m => m.ID).ToList()) == null;
        }

        private const string KeepSecret = "keep-secret"; // oaths.json

        /// <summary>
        /// Each keeper of the mirror's secret not yet sworn swears to keep it, to the patriarch (or, for the patriarch, to
        /// another keeper): sworn, a keeper talks five times less (2026-10-01: unsworn keepers leaked eight hundred clues in
        /// a century and a half, and the rumour reached a knower).
        /// </summary>
        private static void SwearTheKeepers(GameSession session)
        {
            var patriarch = session.Clan.GetPatriarch();
            foreach (var keeper in session.Clan.LivingMembers.Where(m => m.KnowsMirrorSecret && m.CaptorFaction == null
                && session.Oaths.SecrecyPartner(m) == null).ToList())
            {
                var other = keeper != patriarch && patriarch is { CaptorFaction: null } ? patriarch
                    : session.Clan.LivingMembers.FirstOrDefault(m => m != keeper && m.KnowsMirrorSecret && m.CaptorFaction == null);
                if (other != null) session.Oaths.Swear(keeper, other, new[] { KeepSecret });
            }
        }

        private static void SeekATreaty(GameSession session)
        {
            var friend = session.Factions.Factions.Where(f => session.Treaties.With(f.Name).Count == 0)
                .OrderByDescending(f => f.RelationWithPlayer).FirstOrDefault();
            if (friend != null) session.Treaties.Propose(friend.Name, TreatyKind.NonAggression);
        }

        /// <summary>
        /// Every free member is set to work: a cultivator at the Foundation wall without the portion of Qi it absorbs
        /// gathers it (LORE.md §2.5), then cultivates again; the idle cultivate, or else mine; one searches the lake while
        /// its shard lies there (B3c).
        /// </summary>
        public static void SetTheIdleToWork(GameSession session)
        {
            bool huntOpen = session.Talismans.HuntWindowOpen;
            bool lakeOpen = session.Shards.LakeSearchOpen;
            foreach (var member in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList())
            {
                member.HarvestQiId = null; // sent anew each year
                var trial = PowerLadder.Next(member.Realm, member.RealmStage).Trial;
                bool lacksQi = trial == TrialKind.FoundationWall && !session.Cultivation.HasTrialQi(member, trial);
                TaskType wanted = lacksQi && TaskRules.IsAllowed(member, TaskType.GatherQi, huntOpen) ? TaskType.GatherQi
                    : member.CurrentTask == TaskType.GatherQi || member.CurrentTask == TaskType.None
                      || (member.CurrentTask == TaskType.Mine && SpiritualOrificeRules.CanCultivate(member) && member.SpiritualRoot >= LowTalentRoot)
                      || (member.CurrentTask == TaskType.Seclusion && !session.DaoHunts.IsCoveted(member)) ? TaskType.Cultivation // back to cultivation
                    : member.CurrentTask;
                if (!TaskRules.IsAllowed(member, wanted, huntOpen, lakeOpen)) wanted = TaskType.Mine;
                if (wanted != member.CurrentTask && TaskRules.IsAllowed(member, wanted, huntOpen, lakeOpen)) session.Tasks.AssignTask(member, wanted);
            }
            SendASearcherToTheLake(session, huntOpen, lakeOpen);
            GatherTheQiOfTheAscent(session, huntOpen);
            GatherTheQiOfTheAbilities(session, huntOpen);
            SendTheScholars(session, huntOpen);
            SendTheDiplomats(session, huntOpen);
            SendABearerAbroad(session, huntOpen);
            FeedTheClan(session, huntOpen);
            HideTheChosen(session);
        }

        /// <summary>
        /// Each True Monarch condenses its Dharma Treasure; a position's holder mortgages it on its Fruition to guard the
        /// domain; a Designation without master is unsealed before it strikes (L4e, 2026-10-03).
        /// </summary>
        private static void KeepTheTreasures(GameSession session)
        {
            foreach (var monarch in session.Clan.LivingMembers.Where(m => m.Realm >= CultivationRealm.GoldenCore).ToList())
            {
                if (session.Dharma.CondenseRefusal(monarch) == null) session.Dharma.Condense(monarch);
                else if (monarch.HasDharmaTreasure) session.Dharma.MakeDesignation(monarch);
            }
            foreach (var d in session.Dharma.Designations.Where(d => session.Dharma.MasterOf(d) == null).ToList())
                session.Dharma.Unseal(d.Id);
        }

        /// <summary>
        /// A demon born of the clan goes to the Underworld, as the custom wants (the safe choice: no grudge on the clan's
        /// rebirths); one let be is subdued by a Golden Core of the clan when it has one (L4e, 2026-10-03).
        /// </summary>
        private static void AnswerTheDemons(GameSession session)
        {
            foreach (var demon in session.Demons.Pending.ToList()) session.Demons.LeaveToTheUnderworld(demon.Id);
            foreach (var demon in session.Demons.Ravaging.ToList()) session.Demons.Subdue(demon.Id);
        }

        /// <summary>
        /// A reborn ancestor is the Chosen of Destiny (R9, 2026-10-01): until its Purple Mansion the powers may harvest it, so
        /// the clan hides it in seclusion as soon as it may — it regains its realms there all the same.
        /// </summary>
        private static void HideTheChosen(GameSession session)
        {
            foreach (var chosen in session.Clan.LivingMembers.Where(m => m.RebornEssence != null && m.CaptorFaction == null
                && m.Realm < CultivationRealm.PurpleMansion && m.CurrentTask != TaskType.Seclusion
                && TaskRules.IsAllowed(m, TaskType.Seclusion)).ToList())
                session.Tasks.AssignTask(chosen, TaskType.Seclusion);
        }

        private const int MostScholars = 2;     // the gifted minds sent to study for the mirror's deduction
        private const int ScholarRoot = 75;     // a root from which a fragment found is of the third quality (TaskAssignmentSystem.Study)

        /// <summary>
        /// While the clan knows no method to the Purple Mansion and the mirror lacks fragments good enough to deduce one, its
        /// most gifted minds (never the patriarch) study; they come back to their cultivation once the method is known
        /// (2026-10-01: a pilot that never studied never found the fragments, and its line never rose past the Foundation).
        /// </summary>
        private static void SendTheScholars(GameSession session, bool huntOpen)
        {
            var rules = session.Context.Content.Balance.Techniques;
            bool needed = AscentMethod(session) == null
                && session.Deduction.Fragments.Count(f => f.Quality >= rules.AscentDeductionQuality) < rules.AscentDeductionFragments;
            var scholars = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.CurrentTask == TaskType.Study).ToList();
            if (!needed)
            {
                foreach (var scholar in scholars) session.Tasks.AssignTask(scholar, TaskType.Cultivation);
                return;
            }
            foreach (var mind in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.ID != session.Clan.PatriarchID
                    && m.SpiritualRoot >= ScholarRoot && m.CurrentTask == TaskType.Cultivation && TaskRules.IsAllowed(m, TaskType.Study, huntOpen))
                .OrderByDescending(m => m.SpiritualRoot).Take(MostScholars - scholars.Count).ToList())
                session.Tasks.AssignTask(mind, TaskType.Study);
        }

        /// <summary>While the lake holds its shard, the lowest Qi cultivator but the patriarch dredges it.</summary>
        private static void SendASearcherToTheLake(GameSession session, bool huntOpen, bool lakeOpen)
        {
            var free = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            if (!lakeOpen || free.Any(m => m.CurrentTask == TaskType.SearchLake)) return;
            var searcher = free.Where(m => m.ID != session.Clan.PatriarchID && m.Realm == CultivationRealm.QiRefinement
                    && (m.CurrentTask == TaskType.Cultivation || m.CurrentTask == TaskType.Mine)
                    && TaskRules.IsAllowed(m, TaskType.SearchLake, huntOpen, lakeOpen))
                .OrderBy(m => m.RealmStage).FirstOrDefault();
            if (searcher != null) session.Tasks.AssignTask(searcher, TaskType.SearchLake);
        }

        /// <summary>
        /// When the reserve runs thin, the lowest cultivators go down the mine (never a secluded Dao) until the veins'
        /// yield would cover the upkeep and rebuild the reserve — but never past the veins' full slots: beyond them a miner
        /// yields little, and the pilot rather bears the poverty than stop the clan's cultivation (2026-09-30: a clan of three
        /// hundred had every cultivator at the mine, none at the Foundation wall).
        /// </summary>
        private static void FeedTheClan(GameSession session, bool huntOpen)
        {
            if (session.Upkeep.BirthFactor >= 1.0) return;
            if (session.Clan.LivingMembers.Count(m => m.CaptorFaction == null && m.CurrentTask == TaskType.Mine) >= session.Tasks.VeinSlots) return;
            int due = (int)(session.Upkeep.YearlyUpkeep * RebuildMargin);
            var free = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            int Income() => session.Tasks.MiningYield(free.Where(m => m.CurrentTask == TaskType.Mine));
            foreach (var cultivator in free.Where(m => m.CurrentTask == TaskType.Cultivation && m.SpiritualRoot < LowTalentRoot
                    && TaskRules.IsAllowed(m, TaskType.Mine, huntOpen))
                .OrderBy(m => m.SpiritualRoot).ToList()) // the least gifted first; the gifted never (the user's rule, 2026-09-30)
            {
                if (Income() >= due) return;
                session.Tasks.AssignTask(cultivator, TaskType.Mine);
            }
        }

        /// <summary>One line per run, then the means: what to read when tuning.</summary>
        public static string Table(IReadOnlyList<BalanceReport> runs)
        {
            var columns = new (string Name, Func<BalanceReport, double> Value)[]
            {
                ("seed", r => r.Seed), ("years", r => r.Years), ("lost", r => r.Lost ? 1 : 0), ("members", r => r.Members),
                ("powers", r => r.Powers), ("stones", r => r.Stones), ("realm", r => (int)r.BestRealm), ("strikes", r => r.Strikes),
                ("captures", r => r.Captures), ("coalitions", r => r.Coalitions), ("clanWars", r => r.ClanWars),
                ("powerWars", r => r.PowerWars), ("peaces", r => r.Peaces), ("absorbed", r => r.Absorptions),
                ("betrayals", r => r.Betrayals), ("blackmail", r => r.Blackmails), ("thefts", r => r.Thefts),
                ("probes", r => r.ProbesSpotted), ("challenges", r => r.Challenges), ("deaths", r => r.Deaths),
                ("combatDeaths", r => r.CombatDeaths), ("poorYears", r => r.PoorYears),
                ("cultivators", r => r.Cultivators), ("devoured", r => r.Devoured),
                ("extortions", r => r.Extortions), ("foiled", r => r.Foiled),
                ("hunts", r => r.Hunts), ("beasts", r => r.BeastsTaken), ("shards", r => r.Shards), ("endings", r => r.Endings),
                ("pmYear", r => r.PurpleMansionYear), ("endYear", r => r.FirstEndingYear),
            };
            var text = new StringBuilder();
            text.AppendLine(string.Join(" ", columns.Select(c => c.Name.PadLeft(Math.Max(6, c.Name.Length)))));
            foreach (var run in runs)
                text.AppendLine(string.Join(" ", columns.Select(c => c.Value(run).ToString("0").PadLeft(Math.Max(6, c.Name.Length)))));
            if (runs.Count > 0)
                text.AppendLine(string.Join(" ", columns.Select(c => (c.Name == "seed" ? "mean" : runs.Average(c.Value).ToString("0.0"))
                    .PadLeft(Math.Max(6, c.Name.Length)))));
            return text.ToString();
        }
    }
}
