using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The breakthrough to the Golden Core (LORE.md §5.5.1), in the two steps « most never complete »: a Purple
    /// Mansion at Grand Perfection forges the five abilities into a metal essence with the gold-seeking method
    /// of the lineage aimed at (R7: a True Monarch without position), then asks Heaven for a position there —
    /// Realization, Surplus or Intercalary (R1-R5). A Surplus or an Intercalary of a held lineage needs its
    /// holder's permission. Either failure gives life to a Metal Essence Demon. The gold-seeking methods are
    /// knowledge (P3): the mirror deciphers them; quests and ruins come with L6.
    /// The Left Hand paths reach a Golden Core's power without a position (§6.9): the true one, autonomous,
    /// forged from the Grand Perfection of a lineage that founded one (R18); the false one, a patron's borrowed
    /// power — a holder who agreed — that falls with the patron or the unpaid tribute (R19).
    /// </summary>
    public sealed class GoldenCoreSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FruitionRegistry fruitions;
        private readonly MirrorSystem mirror;
        private readonly KnowledgeBase knowledge;
        private readonly ResourceManager resources;
        private readonly Dictionary<string, string> permissions = new Dictionary<string, string>();

        public GoldenCoreSystem(GameContext ctx, ClanManager clan, FruitionRegistry fruitions, MirrorSystem mirror,
            KnowledgeBase knowledge, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.fruitions = fruitions;
            this.mirror = mirror;
            this.knowledge = knowledge;
            this.resources = resources;
        }

        private GoldenCoreSettings Settings => ctx.Content.Balance.GoldenCore;

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        /// <summary>The permissions holders granted the clan: lineage → the holder who granted it (it lapses with them).</summary>
        public IReadOnlyDictionary<string, string> Permissions => permissions;

        /// <summary>
        /// First step: forges the five abilities into a metal essence aimed at the lineage. Needs the realm's XP,
        /// abilities that lead to a position there, and its gold-seeking method (the specialised one for a
        /// three-two Intercalary). False when it cannot be tried; true once tried, even if the demon is born.
        /// </summary>
        public bool Forge(CharacterData member, string fruitionId)
        {
            if (ForgeRoute(member, fruitionId) == PositionRoute.None)
            {
                ctx.Log.Warning($"[Golden Core] {member?.FullName} cannot forge a metal essence toward \"{fruitionId}\".");
                return false;
            }

            member.CultivationXP -= Xp; // spent in the attempt
            int governing = GovernanceBonus?.Invoke(member) ?? 0;
            int chance = ForgeOdds(member);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                BecomeDemon(member, $"fails to forge the metal essence ({chance}%)");
                return true;
            }

            RiseWithoutPosition(member, GoldenCoreState.MetallicEssenceOnly, fruitionId);
            if (governing > 0) RiseByTheImperialWay(member, fruitionId);
            else ctx.Log.Info($"[Golden Core] {member.FullName} forges a metal essence: a True Monarch without position.");
            return true;
        }

        /// <summary>The Virtues corrupted in the world, whose bridges open (R6; set by the session).</summary>
        public Func<IEnumerable<Element>> CorruptedVirtues { get; set; }

        private IEnumerable<Element> Corrupted => CorruptedVirtues?.Invoke() ?? ctx.Content.Balance.PositionBridges.CorruptedAtStart;

        /// <summary>What governing the clan's kingdom adds to a sovereign's forge (R20; set by the session).</summary>
        public Func<CharacterData, int> GovernanceBonus { get; set; }

        /// <summary>What the home atmosphere gives a forge of its favoured lineage (LORE.md §5.8; set by the session).</summary>
        public Func<CharacterData, int> PlaceBonus { get; set; }

        /// <summary>Chance (%) of forging the metal essence: the rules', a favouring atmosphere, and a reigning sovereign's governing.</summary>
        public int ForgeOdds(CharacterData member) =>
            Math.Min(99, GoldenCoreRules.ForgeChance(member, ctx.Content) + (PlaceBonus?.Invoke(member) ?? 0) + (GovernanceBonus?.Invoke(member) ?? 0));

        /// <summary>
        /// Forged by governing (R20): the sovereign's core takes an imperial Surplus where the lineage leaves one open, else
        /// it is a False Golden Core, as the late emperors' — no position asked of Heaven.
        /// </summary>
        private void RiseByTheImperialWay(CharacterData member, string fruitionId)
        {
            member.ImperialCore = true;
            member.GoldenCore = IsOpen(PositionRoute.Surplus, fruitionId) ? GoldenCoreState.Surplus : GoldenCoreState.FalseGoldenCore;
            ctx.Log.Info($"[Golden Core] {member.FullName} forges by governing: {member.GoldenCore} of {fruitionId}.");
            ctx.Events.TriggerPositionTaken(member, member.GoldenCore, GoldenCoreState.MetallicEssenceOnly);
        }

        /// <summary>
        /// Second step: asks Heaven for the position the abilities lead to in the lineage aimed at. False when it
        /// is out of reach (a held Realization, a broken or hidden lineage, a holder's permission missing): the
        /// essence waits and may try later. True once tried, even if the demon is born.
        /// </summary>
        public bool ClaimPosition(CharacterData member)
        {
            if (member == null || !member.IsAlive || member.Realm != CultivationRealm.GoldenCore
                || member.GoldenCore != GoldenCoreState.MetallicEssenceOnly || member.Retreat != Retreat.None) return false;

            var route = GoldenCoreRules.RouteTo(member.DivineAbilities, member.FruitionId, ctx.Content, Corrupted);
            if (route == PositionRoute.None || !IsOpen(route, member.FruitionId))
            {
                ctx.Log.Warning($"[Golden Core] {member.FullName} cannot ask for a position in \"{member.FruitionId}\".");
                return false;
            }

            var target = ctx.Content.Fruitions.First(f => f.Id == member.FruitionId);
            int chance = GoldenCoreRules.ClaimChance(member, route, target, ctx.Content);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                BecomeDemon(member, $"is refused a position ({chance}%): the residual essence comes alive");
                return true;
            }

            member.GoldenCore = GoldenCoreRules.PositionOf(route);
            if (route == PositionRoute.Realization) fruitions.Claim(member.FruitionId, member.FullName);
            ctx.Log.Info($"[Golden Core] {member.FullName} ascends to the {member.GoldenCore} of {target.Name}.");
            ctx.Events.TriggerPositionTaken(member, member.GoldenCore, GoldenCoreState.MetallicEssenceOnly);
            return true;
        }

        /// <summary>The lineages a Grand Perfection ready to forge may aim at, by the route its abilities open, and whether the clan knows the method.</summary>
        public IReadOnlyList<(string FruitionId, PositionRoute Route, bool KnowsMethod)> ForgeTargets(CharacterData member)
        {
            if (!IsReadyToRise(member, GoldenCoreRules.AbilitiesToForge) || member.ProgressionSealed) return new List<(string, PositionRoute, bool)>();
            return ctx.Content.Fruitions
                .Select(f => (f.Id, Route: GoldenCoreRules.RouteTo(member.DivineAbilities, f.Id, ctx.Content, Corrupted)))
                .Where(x => x.Route != PositionRoute.None)
                .Select(x => (x.Id, x.Route, knowledge.Knows(FactKind.GoldSeeking, x.Route == PositionRoute.IntercalaryThreeTwo ? GoldenCoreRules.SpecialisedMethod(x.Id) : x.Id)))
                .ToList();
        }

        /// <summary>Why this essence cannot ask for its position now (French), or null.</summary>
        public string ClaimRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || member.Realm != CultivationRealm.GoldenCore || member.GoldenCore != GoldenCoreState.MetallicEssenceOnly)
                return "seule une essence métallique sans position demande une position";
            if (member.Retreat != Retreat.None) return "en retraite";
            var route = GoldenCoreRules.RouteTo(member.DivineAbilities, member.FruitionId, ctx.Content, Corrupted);
            if (route == PositionRoute.None) return "ses capacités ne mènent à aucune position de cette lignée";
            if (IsOpen(route, member.FruitionId)) return null;
            var state = fruitions.State(member.FruitionId);
            return state?.Status == FruitionStatus.Occupied && route != PositionRoute.Realization
                ? $"la lignée est tenue : il faut la permission de {state.Holder}" : "la position visée est fermée";
        }

        /// <summary>Chance (%) the essence is granted its position.</summary>
        public int ClaimOdds(CharacterData member)
        {
            var route = GoldenCoreRules.RouteTo(member.DivineAbilities, member.FruitionId, ctx.Content, Corrupted);
            var target = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == member.FruitionId);
            return target == null ? 0 : GoldenCoreRules.ClaimChance(member, route, target, ctx.Content);
        }

        /// <summary>
        /// Offers a tribute to a lineage's holder for leave to take a Surplus or an Intercalary there; the
        /// tribute is spent even if refused. False when refused or when there is no holder to ask.
        /// </summary>
        public bool RequestPermission(string fruitionId)
        {
            var state = fruitions.State(fruitionId);
            if (state?.Status != FruitionStatus.Occupied || state.Holder == null || !resources.ConsumeSpiritStones(Settings.PermissionStones))
                return false;
            if (!ctx.Rng.Chance(Settings.PermissionChance))
            {
                ctx.Log.Info($"[Golden Core] {state.Holder} refuses the clan's tribute.");
                return false;
            }
            permissions[fruitionId] = state.Holder;
            ctx.Log.Info($"[Golden Core] {state.Holder} grants the clan leave to rise in their lineage.");
            return true;
        }

        /// <summary>The mirror deciphers a lineage's gold-seeking method (or its specialised Intercalary method).</summary>
        public bool DecipherGoldSeeking(string fruitionId, bool specialised)
        {
            if (ctx.Content.Fruitions.All(f => f.Id != fruitionId)) return false;
            string subject = specialised ? GoldenCoreRules.SpecialisedMethod(fruitionId) : fruitionId;
            if (knowledge.Knows(FactKind.GoldSeeking, subject)) return false;
            if (!mirror.ConsumePower(specialised ? Settings.SpecialisedMirrorCost : Settings.GoldSeekingMirrorCost)) return false;
            return knowledge.Reveal(FactKind.GoldSeeking, subject, KnowledgeSource.Mirror);
        }

        /// <summary>
        /// The true Left Hand (R18): a Grand Perfection of a lineage that founded one, whose path the clan knows,
        /// forges a Golden Core's power without a position. False when it cannot be tried; true once tried.
        /// </summary>
        public bool ForgeTrueLeftHand(CharacterData member)
        {
            string lineage = GoldenCoreRules.SingleLineage(member?.DivineAbilities);
            bool ready = IsReadyToRise(member, GoldenCoreRules.AbilitiesToForge) && lineage != null
                && ctx.Content.Fruitions.FirstOrDefault(f => f.Id == lineage)?.LeftHand != null
                && knowledge.Knows(FactKind.LeftHand, lineage);
            if (!ready)
            {
                ctx.Log.Warning($"[Golden Core] {member?.FullName} cannot walk a true Left Hand path.");
                return false;
            }

            member.CultivationXP -= Xp;
            int chance = GoldenCoreRules.TrueLeftHandChance(member, ctx.Content);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                BecomeDemon(member, $"fails on the Left Hand path ({chance}%)"); // a lesser demon of the old ways (§6.9)
                return true;
            }

            RiseWithoutPosition(member, GoldenCoreState.TrueLeftHand, lineage);
            ctx.Log.Info($"[Golden Core] {member.FullName} reaches a Golden Core's power by the true Left Hand.");
            return true;
        }

        /// <summary>The mirror deciphers the true Left Hand path a lineage founded.</summary>
        public bool DecipherLeftHand(string fruitionId)
        {
            if (ctx.Content.Fruitions.FirstOrDefault(f => f.Id == fruitionId)?.LeftHand == null
                || knowledge.Knows(FactKind.LeftHand, fruitionId) || !mirror.ConsumePower(Settings.LeftHandMirrorCost)) return false;
            return knowledge.Reveal(FactKind.LeftHand, fruitionId, KnowledgeSource.Mirror);
        }

        /// <summary>
        /// The false Left Hand (R19): a lineage's holder who granted the clan leave lends a Golden Core's power
        /// to a Purple Mansion. A failure spends the XP but forges no essence. False when it cannot be tried.
        /// </summary>
        public bool BindToPatron(CharacterData member, string fruitionId)
        {
            var state = fruitions.State(fruitionId);
            bool patronAgrees = state?.Status == FruitionStatus.Occupied && state.Holder != null
                && permissions.TryGetValue(fruitionId, out var grantor) && grantor == state.Holder;
            if (!patronAgrees || !IsReadyToRise(member, Settings.FalseLeftHandMinAbilities))
            {
                ctx.Log.Warning($"[Golden Core] {member?.FullName} cannot borrow a patron's power in \"{fruitionId}\".");
                return false;
            }

            member.CultivationXP -= Xp;
            int chance = GoldenCoreRules.FalseLeftHandChance(member, ctx.Content);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                ctx.Log.Info($"[Golden Core] {member.FullName} cannot hold {state.Holder}'s borrowed power ({chance}%).");
                return true;
            }

            RiseWithoutPosition(member, GoldenCoreState.FalseLeftHand, fruitionId);
            member.PatronId = state.Holder;
            ctx.Log.Info($"[Golden Core] {member.FullName} becomes a false Left Hand in {state.Holder}'s service.");
            return true;
        }

        /// <summary>Transfer (R8): a Surplus takes its lineage's Realization once it is free. True once tried.</summary>
        public bool Transfer(CharacterData member) => MoveToRealization(member, GoldenCoreState.Surplus, Settings.TransferChance, "Transfer");

        /// <summary>Transformation (R8): an Intercalary seizes its lineage's sovereign position by a deep plan. True once tried.</summary>
        public bool Transform(CharacterData member) => MoveToRealization(member, GoldenCoreState.Intercalary, Settings.TransformationChance, "Transformation");

        private static readonly Element[] Virtues = { Element.Water, Element.Fire, Element.Earth, Element.Metal, Element.Wood };

        /// <summary>Chance (%) of a Transmutation: its base and talent, less the axiom's danger from an orthodox position.</summary>
        public int TransmutationOdds(CharacterData member)
        {
            var s = Settings;
            var origin = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == member?.FruitionId);
            int root = (member.SpiritualRoot - ctx.Content.Balance.TrialModifiers.AverageRoot) / ctx.Content.Balance.TrialModifiers.RootPointsPerPercent;
            return Math.Max(1, Math.Min(99, s.TransmutationChance + root - (origin?.Manifestation == Manifestation.Orthodox ? s.AxiomPenalty : 0)));
        }

        /// <summary>
        /// Transmutation (R8; the user's rule, 2026-10-03): a Realization's holder moves to the free Realization of another
        /// lineage of its Virtue. A failure breaks the branch: its foundation corrupts and a demon is born — a holder's, a
        /// catastrophe. Null when done; else why not, or « la branche rompt » (French).
        /// </summary>
        public string Transmute(CharacterData member, string targetId)
        {
            if (member == null || !member.IsAlive || member.CaptorFaction != null || member.Retreat != Retreat.None
                || member.GoldenCore != GoldenCoreState.Realization || member.FruitionId == null)
                return "seul le détenteur libre d'une Réalisation peut tenter la Transmutation";
            var origin = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == member.FruitionId);
            var target = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == targetId);
            if (origin == null || target == null || target.Id == origin.Id || !Virtues.Contains(origin.Element) || target.Element != origin.Element)
                return "on ne se transmue que vers une autre lignée de sa Vertu";
            if (fruitions.State(targetId)?.Status != FruitionStatus.Free) return "la Réalisation visée n'est pas libre";

            int chance = TransmutationOdds(member);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                BecomeDemon(member, $"fails the Transmutation towards {targetId} ({chance}%): the branch breaks");
                return "la branche rompt";
            }
            string former = member.FruitionId;
            fruitions.Vacate(former);
            fruitions.Claim(targetId, member.FullName);
            member.FruitionId = targetId;
            ctx.Log.Info($"[Golden Core] {member.FullName} transmutes from {former} to the Realization of {targetId}.");
            ctx.Events.TriggerFruitionFreed(former, member.FullName, false);
            ctx.Events.TriggerPositionTaken(member, GoldenCoreState.Realization, GoldenCoreState.Realization);
            return null;
        }

        /// <summary>
        /// A position's holder rises to the Realization of their lineage when it is free; a failure wounds their Dao (a
        /// fifth of their lifespan, LORE.md §5.3) in the war of positions.
        /// </summary>
        private bool MoveToRealization(CharacterData member, GoldenCoreState from, int baseChance, string move)
        {
            if (member == null || !member.IsAlive || member.GoldenCore != from || member.Retreat != Retreat.None
                || fruitions.State(member.FruitionId)?.Status != FruitionStatus.Free)
            {
                ctx.Log.Warning($"[Golden Core] {member?.FullName} cannot attempt the {move}.");
                return false;
            }

            int chance = System.Math.Max(1, System.Math.Min(99, baseChance + (member.SpiritualRoot - ctx.Content.Balance.TrialModifiers.AverageRoot)
                / ctx.Content.Balance.TrialModifiers.RootPointsPerPercent));
            if (ctx.Rng.Next(1, 101) > chance)
            {
                member.DaoWounds++;
                member.MaxLifespan = System.Math.Max(member.Age + 1, PowerLadder.WoundedLifespan(member.MaxLifespan, 1));
                ctx.Log.Info($"[Golden Core] {member.FullName}'s {move} fails ({chance}%): their Dao is wounded.");
                return true;
            }

            fruitions.Claim(member.FruitionId, member.FullName);
            member.GoldenCore = GoldenCoreState.Realization;
            ctx.Log.Info($"[Golden Core] {member.FullName} rises to the Realization by {move}.");
            ctx.Events.TriggerPositionTaken(member, GoldenCoreState.Realization, from);
            return true;
        }

        /// <summary>
        /// Borrowing a Fruition's light (LORE.md §5.4.2): a Foundation at its peak, lent the light of a lineage by its
        /// holder (who granted the clan leave), becomes a « Merciful » Purple Mansion without abilities of its own.
        /// </summary>
        public bool BorrowLight(CharacterData member, string fruitionId)
        {
            var state = fruitions.State(fruitionId);
            bool lent = state?.Status == FruitionStatus.Occupied && state.Holder != null
                && permissions.TryGetValue(fruitionId, out var grantor) && grantor == state.Holder;
            bool ready = member != null && member.IsAlive && member.Retreat == Retreat.None && !member.ProgressionSealed && !member.BorrowedLight
                && member.Realm == CultivationRealm.Foundation && member.RealmStage >= PowerLadder.StageCount(CultivationRealm.Foundation);
            if (!lent || !ready)
            {
                ctx.Log.Warning($"[Golden Core] {member?.FullName} cannot borrow the light of \"{fruitionId}\".");
                return false;
            }

            member.Realm = CultivationRealm.PurpleMansion;
            member.RealmStage = 1;
            member.BorrowedLight = true; // a lent light condenses nothing of its own (DivineAbilitySystem refuses it)
            member.FruitionId = fruitionId;
            member.PatronId = state.Holder;
            member.MaxLifespan = PowerLadder.LifespanAfterAdvance(member);
            ctx.Log.Info($"[Golden Core] {member.FullName} borrows the light of {state.Holder}'s lineage.");
            return true;
        }

        /// <summary>Each Breakthrough phase, every false Left Hand and borrowed light pays its patron — or falls, as it falls with them.</summary>
        public void ProcessBreakthroughPhase()
        {
            foreach (var member in clan.LivingMembers.Where(AffirmsTheImage).ToList())
                AffirmImage(member);
            foreach (var member in clan.LivingMembers.Where(m => m.GoldenCore == GoldenCoreState.Realization).ToList())
                StruggleOfTheFiveFaces(member);
            foreach (var member in clan.LivingMembers.Where(m => m.BorrowedLight).ToList())
            {
                if (fruitions.State(member.FruitionId)?.Holder != member.PatronId) ReturnLight(member, "its lender is gone");
                else if (!resources.ConsumeSpiritStones(Settings.LightBorrowingYearlyStones)) ReturnLight(member, "the tribute went unpaid");
            }

            foreach (var member in clan.LivingMembers.Where(m => m.GoldenCore == GoldenCoreState.FalseLeftHand).ToList())
            {
                if (fruitions.State(member.FruitionId)?.Holder != member.PatronId) Fall(member, "their patron is gone");
                else if (!resources.ConsumeSpiritStones(Settings.FalseLeftHandYearlyStones)) Fall(member, "the tribute went unpaid");
            }
        }

        /// <summary>Restores the permissions of a save.</summary>
        public void Restore(IReadOnlyDictionary<string, string> saved)
        {
            permissions.Clear();
            foreach (var pair in saved ?? new Dictionary<string, string>())
                if (pair.Key != null && pair.Value != null) permissions[pair.Key] = pair.Value;
        }

        private static bool IsReadyToRise(CharacterData member, int abilities) =>
            member != null && member.IsAlive && member.Realm == CultivationRealm.PurpleMansion && member.Retreat == Retreat.None
            && member.DivineAbilities.Count >= abilities && member.CultivationXP >= Xp;

        private PositionRoute ForgeRoute(CharacterData member, string fruitionId)
        {
            if (!IsReadyToRise(member, GoldenCoreRules.AbilitiesToForge)) return PositionRoute.None;

            var route = GoldenCoreRules.RouteTo(member.DivineAbilities, fruitionId, ctx.Content, Corrupted);
            if (route == PositionRoute.None) return route;
            string method = route == PositionRoute.IntercalaryThreeTwo ? GoldenCoreRules.SpecialisedMethod(fruitionId) : fruitionId;
            return knowledge.Knows(FactKind.GoldSeeking, method) ? route : PositionRoute.None;
        }

        /// <summary>
        /// A free lineage is open to every position; a held one only to a Surplus or an Intercalary, with its
        /// holder's leave (a clan member holding it gives it). Broken, hidden or suspected lineages are closed.
        /// </summary>
        private bool IsOpen(PositionRoute route, string fruitionId)
        {
            var state = fruitions.State(fruitionId);
            if (state == null) return false;
            if (state.Status == FruitionStatus.Free) return true;
            if (state.Status != FruitionStatus.Occupied || route == PositionRoute.Realization) return false;

            bool heldByTheClan = clan.LivingMembers.Any(m => m.GoldenCore == GoldenCoreState.Realization && m.FruitionId == fruitionId);
            return heldByTheClan || (permissions.TryGetValue(fruitionId, out var grantor) && grantor == state.Holder);
        }

        private void RiseWithoutPosition(CharacterData member, GoldenCoreState standing, string fruitionId)
        {
            member.Realm = CultivationRealm.GoldenCore;
            member.RealmStage = 1;
            member.GoldenCore = standing;
            member.FruitionId = fruitionId;
            member.PursuedAbility = null;
            member.MaxLifespan = PowerLadder.LifespanAfterAdvance(member);
            ctx.Events.TriggerBreakthroughSuccess(member, member.Realm);
        }

        /// <summary>
        /// The « Struggle of the Five Faces » (§5.5.2): a Fruition remembers its former master and may take back its
        /// holder's soul; the member is lost and the old master holds the lineage again.
        /// </summary>
        private void StruggleOfTheFiveFaces(CharacterData holder)
        {
            var lineage = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == holder.FruitionId);
            if (lineage == null || lineage.FormerHolders.Count == 0) return;
            if (!ctx.Rng.Chance(GoldenCoreRules.ReclaimChance(holder, ctx.Content))) return;

            string master = lineage.FormerHolders[0];
            ctx.Log.Info($"[Golden Core] The {lineage.Name} reclaims {holder.FullName}: {master} returns in their body.");
            clan.Kill(holder, DeathCause.SoulReplaced);
            if (fruitions.State(lineage.Id)?.Status == FruitionStatus.Occupied) fruitions.ChangeHolder(lineage.Id, master);
        }

        /// <summary>A True Monarch with a position or a Left Hand path, cultivating, below the apex.</summary>
        private static bool AffirmsTheImage(CharacterData m) =>
            m.Realm == CultivationRealm.GoldenCore && m.CurrentTask == TaskType.Cultivation && m.RealmStage < PowerLadder.StageCount(CultivationRealm.GoldenCore)
            && (m.GoldenCore == GoldenCoreState.Realization || m.GoldenCore == GoldenCoreState.Surplus || m.GoldenCore == GoldenCoreState.Intercalary
                || m.GoldenCore == GoldenCoreState.TrueLeftHand || m.GoldenCore == GoldenCoreState.FalseLeftHand);

        /// <summary>A year affirming the Fruition image (§5.5.2): points by the Dao Heart's alignment, a stage when enough.</summary>
        private void AffirmImage(CharacterData member)
        {
            var lineage = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == member.FruitionId);
            double heart = FoundationRules.HeartAlignmentSpeed(member.Temperament, lineage, ctx.Content.Balance);
            member.CultivationXP += (int)System.Math.Round(Settings.ImagePointsPerYear * heart);

            int needed = Settings.ImageToNextStage[member.RealmStage - 1];
            if (member.CultivationXP < needed) return;
            member.CultivationXP -= needed;
            member.RealmStage++;
            ctx.Log.Info($"[Golden Core] {member.FullName} affirms their Fruition image: stage {member.RealmStage}.");
            ctx.Events.TriggerBreakthroughSuccess(member, member.Realm);
        }

        /// <summary>A borrowed light goes out: back to the Foundation's peak (a seal from elsewhere stays: it is not the light's).</summary>
        private void ReturnLight(CharacterData member, string why)
        {
            member.Realm = CultivationRealm.Foundation;
            member.RealmStage = PowerLadder.StageCount(CultivationRealm.Foundation);
            member.BorrowedLight = false;
            member.FruitionId = null;
            member.PatronId = null;
            PowerLadder.NormalizeLifespan(member);
            ctx.Log.Info($"[Golden Core] {member.FullName}'s borrowed light goes out: {why}.");
        }

        /// <summary>A false Left Hand loses the borrowed power: back to the Purple Mansion, abilities kept, the borrowed years gone.</summary>
        private void Fall(CharacterData member, string why)
        {
            member.Realm = CultivationRealm.PurpleMansion;
            member.RealmStage = PowerLadder.PurpleMansionStageFromAbilities(member.DivineAbilities.Count);
            member.GoldenCore = GoldenCoreState.None;
            member.FruitionId = null;
            member.PatronId = null;
            PowerLadder.NormalizeLifespan(member);
            ctx.Log.Info($"[Golden Core] {member.FullName} falls back to the Purple Mansion: {why}.");
        }

        private void BecomeDemon(CharacterData member, string how)
        {
            ctx.Log.Info($"[Golden Core] {member.FullName} {how}: a Metal Essence Demon is born.");
            clan.Kill(member, DeathCause.MetalEssenceDemon);
            ctx.Events.TriggerMetalEssenceDemon(member);
        }
    }
}
