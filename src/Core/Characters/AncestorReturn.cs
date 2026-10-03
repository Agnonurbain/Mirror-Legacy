using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Ancestor's Return (LORE.md §5.5.2, R9; user decision 2026-10-01: a swift rebirth, at the risk of a harvest).
    /// A True Monarch of the clan whose essence is intact — neither a demon born of a failed core, nor a soul the Fruition
    /// took back — may be reborn in the clan's next child. That child regains its realms swiftly once of age to cultivate:
    /// the Foundation, then the Purple Mansion with its divine abilities, then the Golden Core and its Realization, if the
    /// lineage is free or still its own. Until the Purple Mansion it is the Chosen of Destiny, whom the powers may harvest;
    /// hidden in seclusion, it is safe.
    /// </summary>
    public sealed class AncestorReturn
    {
        private static readonly DeathCause[] Broken = { DeathCause.MetalEssenceDemon, DeathCause.SoulReplaced, DeathCause.ChosenHarvested };

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FruitionRegistry registry;
        private readonly List<AncestorEssence> pending = new List<AncestorEssence>(); // awaiting a child, the first dead first

        private readonly MetalEssenceDemons demons;

        private readonly Diplomacy.FactionManager factions;

        public AncestorReturn(GameContext ctx, ClanManager clan, FruitionRegistry registry, MetalEssenceDemons demons = null,
            Diplomacy.FactionManager factions = null)
        {
            this.factions = factions;
            this.demons = demons;
            this.ctx = ctx;
            this.clan = clan;
            this.registry = registry;
            ctx.Events.OnCharacterDied += Departed;
            ctx.Events.OnCharacterBorn += Born;
        }

        private AncestorSettings Settings => ctx.Content.Balance.Ancestors;

        public IReadOnlyList<AncestorEssence> Pending => pending;

        public void Restore(IEnumerable<AncestorEssence> saved)
        {
            pending.Clear();
            if (saved != null) pending.AddRange(saved);
        }

        private void Departed(CharacterData dead, DeathCause cause)
        {
            if (dead.Realm < CultivationRealm.GoldenCore || Broken.Contains(cause)) return;
            if (demons?.Grudge > 0) return; // the Underworld keeps the registers of the living, and bears the clan a grudge
            if (!ctx.Rng.Chance(Settings.RebirthChance)) return;
            pending.Add(new AncestorEssence(dead.FullName, (dead.DivineAbilities ?? new List<string>()).ToList(), dead.FruitionId,
                dead.GoldenCore, dead.CultivationMethodId, dead.QiId));
            ctx.Log.Info($"[Ancestors] {dead.FullName}'s essence is intact: it awaits a child of the clan.");
        }

        /// <summary>The clan's next child — not a spouse marrying in — carries the first ancestor awaiting.</summary>
        private void Born(CharacterData child)
        {
            if (pending.Count == 0 || child.RebornFrom != null || !child.IsAlive || child.Age >= TaskRules.WorkingAge || child.FromFaction != null) return;
            var essence = pending[0];
            pending.RemoveAt(0);
            child.RebornFrom = essence.Name;
            child.RebornEssence = essence;
            child.HasSpiritualOrifice = true; // the ancestor's essence opens the way
            ctx.Log.Info($"[Ancestors] {essence.Name} is reborn in {child.FullName}.");
            ctx.Events.TriggerAncestorReborn(child, essence.Name);
        }

        public void ProcessYear()
        {
            foreach (var chosen in clan.LivingMembers.Where(m => m.RebornEssence != null).ToList())
            {
                if (Harvested(chosen)) continue;
                Regain(chosen);
            }
        }

        private bool Harvested(CharacterData chosen)
        {
            if (chosen.Realm >= CultivationRealm.PurpleMansion || chosen.CurrentTask == TaskType.Seclusion
                || chosen.CaptorFaction != null || !AHarvesterReaches(ctx.Content.Clan.HomeRegion, null) || !ctx.Rng.Chance(Settings.HarvestChance)) return false;
            ctx.Log.Warning($"[Ancestors] A power harvests {chosen.FullName}, the Chosen of Destiny.");
            ctx.Events.TriggerChosenHarvested(chosen);
            clan.Kill(chosen, DeathCause.ChosenHarvested);
            return true;
        }

        /// <summary>
        /// Only a Purple Mansion manipulates fate (LORE.md §5.4; the user's decision, 2026-10-03): a power harvests a Chosen
        /// only with one of that realm able to reach it (without the powers' registry, anyone may).
        /// </summary>
        private bool AHarvesterReaches(string region, string exceptPower) =>
            factions == null || factions.Factions.Any(f => f.Name != exceptPower && f.HighestRealm >= CultivationRealm.PurpleMansion
                && World.TravelRules.PowerReaches(f, region, ctx.Content));

        /// <summary>One realm a year at most, each at its age: the ancestor remembers the way.</summary>
        private void Regain(CharacterData chosen)
        {
            var s = Settings;
            var essence = chosen.RebornEssence;
            int foundation = TaskRules.CultivationAge + s.FoundationYears, mansion = foundation + s.PurpleMansionYears, core = mansion + s.GoldenCoreYears;
            if (chosen.Realm < CultivationRealm.Foundation && chosen.Age >= foundation)
            {
                chosen.FoundationId = essence.Abilities.FirstOrDefault();
                Rise(chosen, CultivationRealm.Foundation);
            }
            else if (chosen.Realm == CultivationRealm.Foundation && chosen.Age >= mansion)
            {
                chosen.DivineAbilities = essence.Abilities.ToList();
                chosen.PursuedAbility = null;
                Rise(chosen, CultivationRealm.PurpleMansion, PowerLadder.PurpleMansionStageFromAbilities(chosen.DivineAbilities.Count));
            }
            else if (chosen.Realm == CultivationRealm.PurpleMansion && chosen.Age >= core)
            {
                chosen.CultivationMethodId ??= essence.MethodId;
                chosen.QiId ??= essence.QiId;
                TakeBack(chosen, essence);
                chosen.RebornEssence = null; // it has it all again
                Rise(chosen, CultivationRealm.GoldenCore);
            }
        }

        /// <summary>Its Realization, if free or still held in its former name; else a True Monarch without position.</summary>
        private void TakeBack(CharacterData chosen, AncestorEssence essence)
        {
            var state = essence.FruitionId == null ? null : registry.State(essence.FruitionId);
            bool ours = state != null && (state.Status == FruitionStatus.Free
                || (state.Status == FruitionStatus.Occupied && state.Holder == essence.Name));
            if (ours && essence.Position == GoldenCoreState.Realization)
            {
                if (state.Status == FruitionStatus.Free) registry.Claim(essence.FruitionId, chosen.FullName);
                else registry.ChangeHolder(essence.FruitionId, chosen.FullName);
                chosen.FruitionId = essence.FruitionId;
                chosen.GoldenCore = GoldenCoreState.Realization;
                ctx.Events.TriggerFruitionTaken(essence.FruitionId, chosen.FullName);
                return;
            }
            chosen.FruitionId = null;
            chosen.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
        }

        private void Rise(CharacterData chosen, CultivationRealm realm, int stage = 1)
        {
            chosen.Realm = realm;
            chosen.RealmStage = stage;
            chosen.CultivationXP = 0;
            chosen.OrificeKnown = true;
            chosen.MaxLifespan = System.Math.Max(chosen.MaxLifespan, PowerLadder.MaxLifespan(realm, stage));
            ctx.Log.Info($"[Ancestors] {chosen.FullName}, reborn from {chosen.RebornFrom}, regains {realm}.");
            ctx.Events.TriggerBreakthroughSuccess(chosen, realm);
            if (realm == CultivationRealm.PurpleMansion) ctx.Events.TriggerPurpleMansionAscent(chosen);
        }
    }
}
