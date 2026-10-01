using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.World;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// The pilot's road from the Purple Mansion to the Golden Core (2026-10-01, user decision: every behaviour that aims at
    /// the endings). It builds the Herb Garden and the Mine for the materials, reads its Purple Mansions' Dao Partners, takes
    /// a method aligned on a partner by an accord or a theft (one a year), pursues the ability it has a method for and sends a
    /// harvester for its Qi, condenses with resources when no method can be had, then forges the metal essence and claims a
    /// position — only from good odds, for a failure births a demon. A patient pilot: it waits for a method rather than
    /// condense a shallow foundation, and rises only from 60% (the user's choice 2026-10-01: three demons in three forges).
    /// </summary>
    public static partial class BalanceRun
    {
        private const double RiseOdds = 0.6;        // the pilot forges or claims a position from these odds (patient, the user's choice 2026-10-01)
        private const int MaterialsLevel = 3;       // it builds the Herb Garden and the Mine up to this level
        private const int BuildReserveYears = 5;    // from a treasury that keeps five years of upkeep after the work
        private const int CondenseReserveYears = ReserveYears; // condenses with resources keeping the reserve it keeps for a demand

        private static int PurpleMansionXp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        private static void RiseThroughThePurpleMansion(GameSession session)
        {
            BuildTheMaterials(session);
            ExploreTheTomb(session);
            SeekAVassal(session);
            foreach (var essence in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null
                && m.GoldenCore == GoldenCoreState.MetallicEssenceOnly).ToList())
                ClaimAPosition(session, essence);
            var mansions = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm == CultivationRealm.PurpleMansion
                    && m.Retreat == Retreat.None && !m.ProgressionSealed && !m.BorrowedLight)
                .OrderByDescending(m => m.DivineAbilities.Count).ToList();
            if (mansions.Count == 0) return;
            foreach (var master in mansions.Where(m => m.DivineAbilities.Count >= GoldenCoreRules.AbilitiesToForge).ToList())
                ForgeAMetalEssence(session, master);
            var climbing = mansions.Where(m => m.IsAlive && m.Realm == CultivationRealm.PurpleMansion
                && m.DivineAbilities.Count < GoldenCoreRules.AbilitiesToForge).ToList();
            foreach (var mansion in climbing) ReadThePartners(session, mansion);
            SeekAnAlignedMethod(session, climbing);
            foreach (var mansion in climbing)
            {
                PursueWhatCanBeCondensed(session, mansion);
                CondenseWithResourcesWhenStuck(session, mansion);
            }
        }

        /// <summary>A Purple Mansion's tomb the clan has heard of is explored when the odds against its guardian are good.</summary>
        private static void ExploreTheTomb(GameSession session)
        {
            if (session.Paths.Tomb == null) return;
            var team = session.Shards.BestTeam(CultivationRealm.QiRefinement, session.Context.Content.Balance.Shards.ExpeditionMaxTeam)
                .Select(session.Clan.FindById).Where(m => m != null && m.LastOperationYear != session.Clock.Year && m.DiscipleOf == null).ToList();
            if (team.Count > 0 && session.Paths.TombExpeditionChance(team) >= GoodOdds) session.Paths.ExploreTomb(team.Select(m => m.ID).ToList());
        }

        private const string Lake = "jingshui-lake"; // the Lake's Unity (endings.json)
        private const string Linxi = "linxi";        // the Twelve Gates' Debt and the Hegemony

        /// <summary>
        /// One vassal sought a year (2026-10-01: the endings of the Lake, the Twelve Gates and the Hegemony): the families of
        /// the lake first, then the powers of Linxi, then the rest — any that accepts; a vassal of the lake is absorbed as
        /// soon as the clan's grip allows.
        /// </summary>
        private static void SeekAVassal(GameSession session)
        {
            foreach (var vassal in session.Treaties.All.Where(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain).Select(t => t.Faction).ToList())
                if (session.Factions.GetFactionByName(vassal) is { } power && Within(session, power.RegionId, Lake) && session.Absorption.Refusal(vassal) == null)
                    session.Absorption.Absorb(vassal);
            var candidate = session.Factions.Factions
                .Where(f => session.Treaties.Refusal(f, TreatyKind.Vassalage, clanAsSuzerain: true) == null)
                .OrderBy(f => Within(session, f.RegionId, Lake) ? 0 : Within(session, f.RegionId, Linxi) ? 1 : 2)
                .ThenByDescending(f => f.RelationWithPlayer).FirstOrDefault();
            if (candidate != null) session.Treaties.Propose(candidate.Name, TreatyKind.Vassalage, clanAsSuzerain: true);
        }

        private static bool Within(GameSession session, string place, string regionId)
        {
            for (int hops = 0; place != null && hops < 16; hops++) // a region's parents, up to the state or sea
            {
                if (place == regionId) return true;
                place = session.Context.Content.Regions.FirstOrDefault(r => r.Id == place)?.ParentId;
            }
            return false;
        }

        /// <summary>
        /// Whether the treasury pays this and keeps its reserve — and the sect's price while the clan saves for it, unless
        /// the spending is on the road to the Golden Core, which no saving forgoes.
        /// </summary>
        private static bool Affords(GameSession session, int stones, int reserveYears, bool towardTheGoldenCore = false) =>
            session.Resources.SpiritStones - stones >= session.Upkeep.YearlyUpkeep * reserveYears
                + (!towardTheGoldenCore && SavesForTheSect(session) ? session.Context.Content.Balance.Sect.FoundingStones : 0);

        /// <summary>
        /// The Double House is within reach but for its stones (a Purple Mansion at home, enough cultivators): the clan
        /// saves for it, and spends on nothing it can forgo (2026-10-01).
        /// </summary>
        private static bool SavesForTheSect(GameSession session)
        {
            if (session.Sect.Founded) return false;
            var s = session.Context.Content.Balance.Sect;
            var free = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            return free.Any(m => m.Realm >= s.MinRealm) && free.Count(m => m.Realm >= CultivationRealm.QiRefinement) >= s.MinCultivators;
        }

        /// <summary>The Herb Garden and the Mine, a level each a year, from a full treasury: herbs and ores for the abilities.</summary>
        private static void BuildTheMaterials(GameSession session)
        {
            foreach (var type in new[] { BuildingType.HerbGarden, BuildingType.Mine })
            {
                var building = session.Buildings.GetBuilding(type);
                if (building.Level < MaterialsLevel && Affords(session, building.UpgradeCost, BuildReserveYears))
                    session.Buildings.Upgrade(type);
            }
        }

        private static string FoundationOf(CharacterData m) => m.FoundationId ?? m.DivineAbilities.FirstOrDefault();

        /// <summary>The mirror reads the Dao Partners of the foundation, when the clan does not know them yet.</summary>
        private static void ReadThePartners(GameSession session, CharacterData mansion)
        {
            string foundation = FoundationOf(mansion);
            int cost = session.Context.Content.Balance.KnowledgeTrade.DaoPartnersMirrorCost;
            if (foundation == null || session.Knowledge.Knows(FactKind.DaoPartners, foundation) || session.Mirror.MirrorPower < cost + SeedReserve) return;
            session.Exchange.DecipherDaoPartners(foundation);
        }

        /// <summary>
        /// The partners of the mansion's own lineage it may still condense: named, known to the clan, not yet held — a Life
        /// ability kept for the last, which helps the forge (§5.4.4), the others first.
        /// </summary>
        private static IEnumerable<string> WantedAbilities(GameSession session, CharacterData mansion)
        {
            string lineage = FoundationRef.Parse(FoundationOf(mansion)).FruitionId;
            var fruition = session.Context.Content.Fruitions.FirstOrDefault(f => f.Id == lineage);
            if (fruition == null) return Enumerable.Empty<string>();
            bool last = mansion.DivineAbilities.Count == GoldenCoreRules.AbilitiesToForge - 1;
            return fruition.Abilities.Where(a => a.Name != null && !a.Substitute)
                .Where(a => !mansion.DivineAbilities.Contains($"{lineage}:{a.Id}") && session.Knowledge.Knows(FactKind.Ability, $"{lineage}:{a.Id}"))
                .OrderBy(a => a.Types.Contains(AbilityType.Life) == last ? 0 : 1) // stable: the lineage's order otherwise
                .Select(a => $"{lineage}:{a.Id}");
        }

        /// <summary>The methods whose Qi builds this ability.</summary>
        private static IEnumerable<TechniqueData> AlignedMethods(GameSession session, string ability) =>
            session.Context.Content.Techniques.Where(t => t.Kind == TechniqueKind.Cultivation && t.RequiredQiId != null
                && session.Techniques.FindQi(t.RequiredQiId)?.Foundation == ability);

        private static bool HasAlignedMethod(GameSession session, string ability) =>
            AlignedMethods(session, ability).Any(t => session.Techniques.Knows(t.ID));

        /// <summary>
        /// A power within reach holds a method aligned on the ability: one warm enough for an accord, or one the clan's best
        /// team could rob at good odds — a hostile Golden Core empire is no reason to wait for ever.
        /// </summary>
        private static bool WithinReach(GameSession session, string ability)
        {
            int warm = session.Context.Content.Balance.KnowledgeTrade.AscentAccordMinRelation;
            var team = session.Shards.BestTeam(CultivationRealm.QiRefinement, session.Context.Content.Balance.Shards.ExpeditionMaxTeam)
                .Select(session.Clan.FindById).Where(m => m != null).ToList();
            return AlignedMethods(session, ability).Any(t => session.Factions.Factions.Any(f => f.Techniques.Contains(t.ID)
                && (f.RelationWithPlayer >= warm || session.Paths.ManualTheftChance(f.Name, team) >= GoodOdds)));
        }

        /// <summary>
        /// One aligned method a year, for the first wanted ability the clan has none for: by an accord the power accepts,
        /// else stolen when the odds are good.
        /// </summary>
        private static void SeekAnAlignedMethod(GameSession session, IReadOnlyList<CharacterData> climbing)
        {
            var wanted = climbing.SelectMany(m => WantedAbilities(session, m)).Distinct()
                .Where(a => !HasAlignedMethod(session, a)).ToList();
            foreach (var ability in wanted)
                foreach (var method in AlignedMethods(session, ability))
                    foreach (var power in session.Factions.Factions.Where(f => f.Techniques.Contains(method.ID)).ToList())
                    {
                        var terms = TermsFor(session, power, method);
                        if (terms != null && session.Accords.Conclude(power.Name, method.ID, terms) == null) return;
                        var team = session.Shards.BestTeam(CultivationRealm.QiRefinement, session.Context.Content.Balance.Shards.ExpeditionMaxTeam)
                            .Select(session.Clan.FindById).Where(m => m != null && m.LastOperationYear != session.Clock.Year && m.DiscipleOf == null).ToList();
                        if (team.Count > 0 && session.Paths.ManualTheftChance(power.Name, team) >= GoodOdds)
                        {
                            session.Paths.StealManual(power.Name, method.ID, team.Select(m => m.ID).ToList());
                            return;
                        }
                    }
        }

        /// <summary>
        /// What the clan can offer for a method, up to its worth: the secrets the power lacks, then the arts it lacks (the
        /// most valuable first), then the clan's word as a debt. Null when it does not suffice.
        /// </summary>
        private static List<AccordTerm> TermsFor(GameSession session, FactionData power, TechniqueData method)
        {
            var accords = session.Accords;
            var offers = session.SecretBook.All.Select(x => new AccordTerm(AccordCurrency.Secret, x.Id, 0))
                .Concat(session.Techniques.Known.Where(t => t.ID != method.ID).Select(t => new AccordTerm(AccordCurrency.Technique, t.ID, 0)))
                .Where(t => accords.TermRefusal(power.Name, t) == null)
                .OrderByDescending(t => accords.WorthOf(power.Name, method.ID, t))
                .Append(new AccordTerm(AccordCurrency.Debt, null, 0));
            int price = accords.PriceOf(method), worth = 0;
            var terms = new List<AccordTerm>();
            foreach (var term in offers)
            {
                if (worth >= price) break;
                int value = accords.WorthOf(power.Name, method.ID, term);
                if (value <= 0) continue;
                terms.Add(term);
                worth += value;
            }
            return worth >= price && accords.Refusal(power.Name, method.ID, terms) == null ? terms : null;
        }

        /// <summary>The mansion pursues the first wanted ability the clan has a method for (its Qi is then harvested).</summary>
        private static void PursueWhatCanBeCondensed(GameSession session, CharacterData mansion)
        {
            if (mansion.PursuedAbility != null && HasAlignedMethod(session, mansion.PursuedAbility)) return;
            var ability = WantedAbilities(session, mansion).FirstOrDefault(a => HasAlignedMethod(session, a));
            if (ability != null) session.Abilities.Pursue(mansion, ability);
        }

        /// <summary>
        /// A mansion at its realm's full XP condenses an ability with spiritual objects (half the XP, a shallow foundation
        /// that weighs on the forge) only as a last resort: when no method aligned on any wanted ability lies within reach —
        /// while a power it can deal with or rob holds one, the patient clan waits to take it (the user's choice 2026-10-01).
        /// </summary>
        private static void CondenseWithResourcesWhenStuck(GameSession session, CharacterData mansion)
        {
            if (mansion.CultivationXP < PurpleMansionXp) return;
            var wanted = WantedAbilities(session, mansion).ToList();
            if (wanted.Count == 0 || wanted.Any(a => HasAlignedMethod(session, a) || WithinReach(session, a))) return;
            var s = session.Context.Content.Balance.DivineAbilities;
            if (!Affords(session, s.ResourceStones, CondenseReserveYears, towardTheGoldenCore: true)
                || session.Resources.MedicinalHerbs < s.ResourceHerbs || session.Resources.SpiritualOres < s.ResourceOres) return;
            session.Abilities.CondenseWithResources(mansion, wanted[0]);
        }

        /// <summary>
        /// A Grand Perfection with the realm's XP forges toward the lineage its abilities lead to, its own first: the mirror
        /// deciphers the gold-seeking method, and the forge is tried only from good odds.
        /// </summary>
        private static void ForgeAMetalEssence(GameSession session, CharacterData master)
        {
            if (master.CultivationXP < PurpleMansionXp) return;
            var content = session.Context.Content;
            string own = FoundationRef.Parse(FoundationOf(master)).FruitionId;
            var target = content.Fruitions.OrderBy(f => f.Id == own ? 0 : 1)
                .Select(f => (f.Id, Route: GoldenCoreRules.RouteTo(master.DivineAbilities, f.Id, content.Fruitions)))
                .FirstOrDefault(x => x.Route != PositionRoute.None);
            if (target.Id == null || GoldenCoreRules.ForgeChance(master, content) < RiseOdds * 100) return;
            bool specialised = target.Route == PositionRoute.IntercalaryThreeTwo;
            string method = specialised ? GoldenCoreRules.SpecialisedMethod(target.Id) : target.Id;
            if (!session.Knowledge.Knows(FactKind.GoldSeeking, method) && !session.GoldenCore.DecipherGoldSeeking(target.Id, specialised)) return;
            session.GoldenCore.Forge(master, target.Id);
        }

        /// <summary>A forged essence asks for its position from good odds; a held lineage first asks its holder's leave.</summary>
        private static void ClaimAPosition(GameSession session, CharacterData essence)
        {
            var content = session.Context.Content;
            var target = content.Fruitions.FirstOrDefault(f => f.Id == essence.FruitionId);
            if (target == null || essence.Retreat != Retreat.None) return;
            var route = GoldenCoreRules.RouteTo(essence.DivineAbilities, target.Id, content.Fruitions);
            if (route == PositionRoute.None || GoldenCoreRules.ClaimChance(essence, route, target, content) < RiseOdds * 100) return;
            var state = session.Fruitions.State(target.Id);
            if (route != PositionRoute.Realization && state?.Status == FruitionStatus.Occupied
                && !session.GoldenCore.Permissions.ContainsKey(target.Id)
                && Affords(session, content.Balance.GoldenCore.PermissionStones, CondenseReserveYears, towardTheGoldenCore: true))
                session.GoldenCore.RequestPermission(target.Id);
            session.GoldenCore.ClaimPosition(essence);
        }

        /// <summary>
        /// For each mansion's pursued ability whose method the clan knows but whose Qi it lacks, one free low cultivator
        /// (never the patriarch) is sent to harvest it.
        /// </summary>
        private static void GatherTheQiOfTheAbilities(GameSession session, bool huntOpen)
        {
            int portions = session.Context.Content.Balance.Techniques.AlignedQiPortions;
            foreach (var mansion in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm == CultivationRealm.PurpleMansion
                && m.PursuedAbility != null).ToList())
            {
                var qi = AlignedMethods(session, mansion.PursuedAbility).Where(t => session.Techniques.Knows(t.ID))
                    .Select(t => session.Techniques.FindQi(t.RequiredQiId)).FirstOrDefault(q => q is { Vanished: false, Ubiquitous: false });
                if (qi == null || session.Resources.QiPortions(qi.Id) >= portions) continue;
                if (session.Clan.LivingMembers.Any(m => m.HarvestQiId == qi.Id && m.CurrentTask == TaskType.GatherQi)) continue;
                var harvester = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.ID != session.Clan.PatriarchID
                        && m.Realm <= CultivationRealm.QiRefinement && SpiritualOrificeRules.CanCultivate(m)
                        && (m.CurrentTask == TaskType.Cultivation || m.CurrentTask == TaskType.Mine)
                        && TaskRules.IsAllowed(m, TaskType.GatherQi, huntOpen))
                    .OrderBy(m => (int)m.Realm).ThenBy(m => m.RealmStage).FirstOrDefault();
                if (harvester == null) continue;
                harvester.HarvestQiId = qi.Id;
                session.Tasks.AssignTask(harvester, TaskType.GatherQi);
            }
        }
    }
}
