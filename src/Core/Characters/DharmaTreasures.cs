using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Dharma Treasures and the Rank Designations (LORE.md §6.9; L4e, user decisions 2026-10-03). A True Monarch of the
    /// clan condenses a treasure of its foundation and its essence (ores, a few years): it weighs in the clan's wars and
    /// guards its bearer from ambushes; it is lost with its bearer. A position's holder may mortgage it on its Fruition — a
    /// Rank Designation that guards the domain even after its master's death, as Wen Xiang's still guards a Celestial Cave:
    /// fully when its master is loved by the Fruition (its Dao Heart aligned), by half otherwise or without master. Without a
    /// living holder of its lineage it is dangerous, until the clan unseals it and it returns to the Fruition.
    /// </summary>
    public sealed class DharmaTreasures
    {
        private static readonly GoldenCoreState[] Positions = { GoldenCoreState.Realization, GoldenCoreState.Surplus, GoldenCoreState.Intercalary };

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly List<RankDesignation> designations = new List<RankDesignation>();

        public DharmaTreasures(GameContext ctx, ClanManager clan, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
        }

        private DharmaSettings Settings => ctx.Content.Balance.Dharma;

        public IReadOnlyList<RankDesignation> Designations => designations;

        public void Restore(IEnumerable<RankDesignation> saved)
        {
            designations.Clear();
            if (saved != null) designations.AddRange(saved);
        }

        private static bool Free(CharacterData m) => m != null && m.IsAlive && m.CaptorFaction == null;

        /// <summary>Why this member cannot condense a treasure (French), or null.</summary>
        public string CondenseRefusal(CharacterData member)
        {
            if (!Free(member) || member.Realm < CultivationRealm.GoldenCore) return "seul un Vrai Monarque libre, au Noyau d'Or, condense un Trésor de Dharma";
            if (member.HasDharmaTreasure || member.TreasureReadyYear != null) return "il a déjà son Trésor de Dharma";
            if (resources.SpiritualOres < Settings.CondenseOres) return $"il faut {Settings.CondenseOres} minerais spirituels";
            return null;
        }

        /// <summary>A True Monarch begins to condense its treasure. Null when begun, else why not (French).</summary>
        public string Condense(CharacterData member)
        {
            if (CondenseRefusal(member) is { } why) return why;
            resources.ConsumeOres(Settings.CondenseOres);
            member.TreasureReadyYear = ctx.Clock.Year + Settings.CondenseYears;
            ctx.Log.Info($"[Dharma] {member.FullName} begins to condense a Dharma Treasure.");
            return null;
        }

        /// <summary>A position's holder mortgages its treasure on its Fruition. Null when done, else why not (French).</summary>
        public string MakeDesignation(CharacterData member)
        {
            if (!Free(member) || !member.HasDharmaTreasure) return "il lui faut d'abord un Trésor de Dharma";
            if (!Positions.Contains(member.GoldenCore) || member.FruitionId == null) return "seul le détenteur d'une position peut l'hypothéquer sur sa Fruition";
            member.HasDharmaTreasure = false;
            var designation = new RankDesignation(ctx.Rng.NextId(), member.FruitionId, member.ID, member.FullName, ctx.Clock.Year);
            designations.Add(designation);
            ctx.Log.Info($"[Dharma] {member.FullName}'s treasure becomes a Rank Designation of {member.FruitionId}.");
            ctx.Events.TriggerRankDesignation(designation);
            return null;
        }

        /// <summary>The clan unseals a Designation: it returns to its Fruition. Null when done, else why not (French).</summary>
        public string Unseal(string id)
        {
            var designation = designations.FirstOrDefault(d => d.Id == id);
            if (designation == null) return "aucune Désignation de Rang de ce nom";
            designations.Remove(designation);
            ctx.Log.Info($"[Dharma] {designation.MasterName}'s Rank Designation returns to its Fruition.");
            return null;
        }

        /// <summary>Its master: its first, while alive and free; else a living holder of its lineage's positions; else none.</summary>
        public CharacterData MasterOf(RankDesignation d)
        {
            var first = clan.FindById(d.MasterId);
            if (Free(first)) return first;
            return clan.LivingMembers.FirstOrDefault(m => Free(m) && m.FruitionId == d.Lineage && Positions.Contains(m.GoldenCore));
        }

        private bool Loved(CharacterData master, string lineage)
        {
            var fruition = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == lineage);
            return fruition == null || fruition.Temperament == Temperament.None || master.Temperament == fruition.Temperament;
        }

        /// <summary>How fully a Designation is wielded: 1 by a loved master, ½ by another or without master.</summary>
        private double Wield(RankDesignation d)
        {
            var master = MasterOf(d);
            return master != null && Loved(master, d.Lineage) ? 1.0 : 0.5;
        }

        /// <summary>What the treasures and the Designations add to the clan's war strength.</summary>
        public double DomainStrength => World.WorldArsenal.Weigh( // the greatest whole, the others as the clan's other fighters
            clan.LivingMembers.Where(m => Free(m) && m.HasDharmaTreasure).Select(_ => Settings.TreasureStrength)
                .Concat(designations.Select(d => Wield(d) * Settings.DesignationStrength)),
            ctx.Content.Balance.Wars.ClanStrengthPerMember);

        /// <summary>The chance the Designations foil an ambush on any member of the clan.</summary>
        public double DomainGuardChance =>
            1 - designations.Aggregate(1.0, (miss, d) => miss * (1 - Wield(d) * Settings.DesignationGuardChance));

        /// <summary>The chance this member's own treasure foils an ambush on it.</summary>
        public double GuardChance(CharacterData member) => Free(member) && member.HasDharmaTreasure ? Settings.TreasureGuardChance : 0;

        public void ProcessYear()
        {
            int year = ctx.Clock.Year;
            foreach (var m in clan.LivingMembers.Where(m => m.TreasureReadyYear != null && m.TreasureReadyYear <= year && !m.HasDharmaTreasure).ToList())
            {
                m.HasDharmaTreasure = true;
                ctx.Log.Info($"[Dharma] {m.FullName} condenses its Dharma Treasure.");
                ctx.Events.TriggerDharmaTreasure(m);
            }
            foreach (var d in designations.Where(d => MasterOf(d) == null).ToList())
            {
                if (!ctx.Rng.Chance(Settings.MasterlessStrikeChance)) continue;
                var prey = clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm < CultivationRealm.GoldenCore).ToList();
                if (prey.Count == 0) continue;
                var struck = prey[ctx.Rng.Next(prey.Count)];
                ctx.Log.Warning($"[Dharma] {d.MasterName}'s masterless Designation strikes {struck.FullName}.");
                clan.Kill(struck, DeathCause.DesignationStruck);
            }
        }
    }
}
