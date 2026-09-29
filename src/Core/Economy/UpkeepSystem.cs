using System;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// The clan's upkeep (user decision 2026-09-29, after the long games): once the year's income is in (the end of the
    /// Events phase, before the births) every living member
    /// costs stones — a mortal little, a cultivator more by its realm. A clan that cannot pay spends what it has and is
    /// impoverished for the year: its members are shaken and few children are born; a thin reserve already means fewer.
    /// So the clan grows only as far as it can feed itself.
    /// </summary>
    public sealed class UpkeepSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;

        public UpkeepSystem(GameContext ctx, ClanManager clan, ResourceManager resources, MentalStabilitySystem stability)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.stability = stability;
        }

        private UpkeepSettings Settings => ctx.Content.Balance.Upkeep;

        /// <summary>This year's payment fell short (saved: shown until the next payment).</summary>
        public bool Impoverished { get; private set; }

        /// <summary>
        /// What becomes of this year's chance of a child: all of it with <see cref="UpkeepSettings.ProsperityYears"/> of
        /// upkeep in reserve, less with less (never under a poor year's), a poor year's after a poor year.
        /// </summary>
        public double BirthFactor
        {
            get
            {
                var s = Settings;
                if (Impoverished) return s.PovertyBirthFactor;
                int needed = YearlyUpkeep * s.ProsperityYears;
                return needed <= 0 ? 1.0 : Math.Clamp((double)resources.SpiritStones / needed, s.PovertyBirthFactor, 1.0);
            }
        }

        public int UpkeepOf(CharacterData member) =>
            SpiritualOrificeRules.CanCultivate(member)
                ? Settings.CultivatorStones + (int)member.Realm * Settings.StonesPerRealm
                : Settings.MortalStones;

        /// <summary>What the living members cost this year (a captive is fed by its captor).</summary>
        public int YearlyUpkeep => clan.LivingMembers.Where(m => m.CaptorFaction == null).Sum(UpkeepOf);

        public void PayUpkeep()
        {
            int due = YearlyUpkeep;
            Impoverished = resources.SpiritStones < due;
            if (!Impoverished)
            {
                resources.ConsumeSpiritStones(due);
                return;
            }
            resources.ConsumeSpiritStones(resources.SpiritStones);
            foreach (var member in clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList())
                stability.ApplyModifier(member, Settings.PovertyStability);
            ctx.Log.Warning($"[Upkeep] The clan cannot feed its {clan.LivingMembers.Count(m => m.CaptorFaction == null)} members ({due} stones due): a poor year.");
        }

        public void Restore(bool impoverished) => Impoverished = impoverished;
    }
}
