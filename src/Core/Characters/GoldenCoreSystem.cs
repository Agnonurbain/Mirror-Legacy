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
            int chance = GoldenCoreRules.ForgeChance(member, ctx.Content);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                BecomeDemon(member, $"fails to forge the metal essence ({chance}%)");
                return true;
            }

            member.Realm = CultivationRealm.GoldenCore;
            member.RealmStage = 1;
            member.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            member.FruitionId = fruitionId;
            member.PursuedAbility = null;
            member.MaxLifespan = PowerLadder.LifespanAfterAdvance(member);
            ctx.Log.Info($"[Golden Core] {member.FullName} forges a metal essence: a True Monarch without position.");
            ctx.Events.TriggerBreakthroughSuccess(member, member.Realm);
            return true;
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

            var route = GoldenCoreRules.RouteTo(member.DivineAbilities, member.FruitionId, ctx.Content.Fruitions);
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
            return true;
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

        /// <summary>Restores the permissions of a save.</summary>
        public void Restore(IReadOnlyDictionary<string, string> saved)
        {
            permissions.Clear();
            foreach (var pair in saved ?? new Dictionary<string, string>())
                if (pair.Key != null && pair.Value != null) permissions[pair.Key] = pair.Value;
        }

        private PositionRoute ForgeRoute(CharacterData member, string fruitionId)
        {
            if (member == null || !member.IsAlive || member.Realm != CultivationRealm.PurpleMansion || member.Retreat != Retreat.None
                || member.CultivationXP < Xp) return PositionRoute.None;

            var route = GoldenCoreRules.RouteTo(member.DivineAbilities, fruitionId, ctx.Content.Fruitions);
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

        private void BecomeDemon(CharacterData member, string how)
        {
            ctx.Log.Info($"[Golden Core] {member.FullName} {how}: a Metal Essence Demon is born.");
            clan.Kill(member, DeathCause.MetalEssenceDemon);
            ctx.Events.TriggerMetalEssenceDemon(member);
        }
    }
}
