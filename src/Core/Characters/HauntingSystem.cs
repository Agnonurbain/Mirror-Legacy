using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The haunting of the Divine Dao (L7; LORE.md §3.6; the user's choice of 2026-10-15): a Divine Attendant imprints its
    /// existence so deeply that after its death its memory haunts the clan — for years by the degree of its hold on a
    /// Profundity (borrowing or uniting 1, owning 2, a Divine Core 3). Untended, each year it may drive a member mad (a Dao
    /// wound) or bring a calamity (a share of the stones); honoured, it lends prestige each year, but commemoration multiplies
    /// its dangers; exorcised, it ends, for stones — fewer when a monk of the clan performs it. See <see cref="DivineDaoSettings"/>.
    /// </summary>
    public sealed class HauntingSystem
    {
        /// <summary>The strongest haunting: a Divine Core's.</summary>
        public const int MaxStrength = 3;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly WoundSystem wounds;
        private readonly List<Haunting> hauntings = new List<Haunting>();

        public HauntingSystem(GameContext ctx, ClanManager clan, ResourceManager resources, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.wounds = wounds;
            ctx.Events.OnCharacterDied += Departed;
        }

        private DivineDaoSettings Settings => ctx.Content.Balance.DivineDao;

        /// <summary>The hauntings under way, the oldest first.</summary>
        public IReadOnlyList<Haunting> Hauntings => hauntings;

        /// <summary>Restores the saved hauntings, mended: one an Attendant, its strength and years within bounds, a name.</summary>
        public void Restore(IEnumerable<Haunting> saved)
        {
            hauntings.Clear();
            foreach (var h in saved ?? Enumerable.Empty<Haunting>())
            {
                if (h?.Id == null || h.YearsLeft <= 0 || hauntings.Any(k => k.Id == h.Id)) continue;
                int strength = Math.Clamp(h.Strength, 1, MaxStrength);
                hauntings.Add(new Haunting
                {
                    Id = h.Id, Name = string.IsNullOrWhiteSpace(h.Name) ? "un Serviteur Divin oublié" : h.Name, Strength = strength,
                    YearsLeft = Math.Min(h.YearsLeft, strength * Settings.HauntYearsPerStrength), Honoured = h.Honoured
                });
            }
        }

        /// <summary>A Divine Attendant's degree of hold on a Profundity: a Divine Core 3, an owner 2, the others 1.</summary>
        public static int StrengthOf(CharacterData member) =>
            member.GoldenCore == GoldenCoreState.DivineCore ? MaxStrength
            : member.SubPath == CultivationSubPath.OwnedProfundity ? 2
            : 1;

        private void Departed(CharacterData dead, DeathCause cause)
        {
            if (dead?.Path != CultivationPath.Divine || dead.Departed || hauntings.Any(h => h.Id == dead.ID)) return; // a member gone haunts another house
            int strength = StrengthOf(dead);
            var haunting = new Haunting { Id = dead.ID, Name = dead.FullName, Strength = strength, YearsLeft = strength * Settings.HauntYearsPerStrength };
            hauntings.Add(haunting);
            ctx.Log.Warning($"[Haunting] {dead.FullName}'s memory haunts the clan (strength {strength}, {haunting.YearsLeft} years).");
            ctx.Events.TriggerHauntingBegan(haunting);
        }

        /// <summary>The clan honours the haunting: null when done, else why not (French).</summary>
        public string Honour(string id)
        {
            var haunting = Find(id);
            if (haunting == null) return "aucune hantise de ce nom";
            if (haunting.Honoured) return "sa mémoire est déjà honorée";
            haunting.Honoured = true;
            ctx.Log.Info($"[Haunting] The clan honours {haunting.Name}'s memory.");
            ctx.Events.TriggerHauntingHonoured(haunting);
            return null;
        }

        /// <summary>The stones an exorcism of the haunting costs: fewer when a free monk of the clan performs it.</summary>
        public int ExorcismCost(Haunting haunting)
        {
            int cost = haunting.Strength * Settings.ExorcismStonesPerStrength;
            bool monk = clan.LivingMembers.Any(m => m.Path == CultivationPath.Buddhist && m.CaptorFaction == null);
            return monk ? (int)(cost * Settings.MonkExorcismFactor) : cost;
        }

        /// <summary>The clan exorcises the haunting: null when done, else why not (French).</summary>
        public string Exorcise(string id)
        {
            var haunting = Find(id);
            if (haunting == null) return "aucune hantise de ce nom";
            int cost = ExorcismCost(haunting);
            if (!resources.ConsumeSpiritStones(cost)) return $"il faut {cost} pierres spirituelles";
            hauntings.Remove(haunting);
            ctx.Log.Info($"[Haunting] {haunting.Name}'s haunting is exorcised.");
            ctx.Events.TriggerHauntingExorcised(haunting);
            return null;
        }

        /// <summary>A year of each haunting: honoured, it lends prestige; either way it may madden or strike, then wanes.</summary>
        public void ProcessYear()
        {
            foreach (var haunting in hauntings.ToList())
            {
                double risk = haunting.Strength * (haunting.Honoured ? Settings.HonourRiskFactor : 1);
                if (haunting.Honoured) resources.AddPrestige(haunting.Strength * Settings.HonourPrestigePerStrength);
                if (ctx.Rng.Chance(Settings.MadnessChancePerStrength * risk)) Madden(haunting);
                if (ctx.Rng.Chance(Settings.CalamityChancePerStrength * risk)) Strike(haunting);
                if (--haunting.YearsLeft > 0) continue;
                hauntings.Remove(haunting);
                ctx.Log.Info($"[Haunting] {haunting.Name}'s haunting fades.");
                ctx.Events.TriggerHauntingFaded(haunting);
            }
        }

        private void Madden(Haunting haunting)
        {
            var minds = clan.LivingMembers.Where(m => m.Species == Species.Human && m.CaptorFaction == null).ToList();
            if (minds.Count == 0) return;
            var victim = minds[ctx.Rng.Next(minds.Count)];
            wounds.ApplyDaoWound(victim);
            ctx.Log.Warning($"[Haunting] {haunting.Name}'s haunting drives {victim.FullName} mad.");
            ctx.Events.TriggerHauntingMadness(haunting, victim);
        }

        private void Strike(Haunting haunting)
        {
            int lost = (int)(resources.SpiritStones * Settings.CalamityStoneShare);
            if (lost <= 0) return;
            resources.ConsumeSpiritStones(lost);
            ctx.Log.Warning($"[Haunting] {haunting.Name}'s haunting brings a calamity: {lost} stones lost.");
            ctx.Events.TriggerHauntingCalamity(haunting, lost);
        }

        private Haunting Find(string id) => hauntings.FirstOrDefault(h => h.Id == id);
    }
}
