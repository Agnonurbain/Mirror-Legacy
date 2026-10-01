using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers are born and fall (the living world, step C, user decisions 2026-10-01). A power without elders, or
    /// ruined, disperses into its strongest neighbour, by the absorption's path. A founding is strict, and harder at each
    /// rank: a Purple Mansion elder who is not his power's strongest may leave with two followers of the Foundation or above
    /// and found a gate; a Golden Core with four, a sect; a kingdom — the hardest — is founded by a power that counts a
    /// Golden Core, three vassals and the size of the greatest. While the world counts fewer powers than at first, a family
    /// rises around a Foundation.
    /// </summary>
    public sealed class PowerLifecycle
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly PowerPoliticsSystem politics;
        private readonly ElderSystem elders;

        public PowerLifecycle(GameContext ctx, FactionManager factions, PowerPoliticsSystem politics, ElderSystem elders)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.politics = politics;
            this.elders = elders;
        }

        private PowerLifecycleSettings Settings => ctx.Content.Balance.PowerLifecycle;

        public void ProcessYear()
        {
            foreach (var power in factions.Factions.ToList()) Fall(power);
            foreach (var power in factions.Factions.ToList()) Secede(power);
            foreach (var power in factions.Factions.ToList()) FoundAKingdom(power);
            RiseAFamily();
        }

        // ---- The fall ----

        private void Fall(FactionData power)
        {
            if (factions.Factions.Count < 2 || (power.Elders.Count > 0 && power.PowerLevel >= Settings.FallPower)) return;
            var heir = factions.Factions.Where(f => f != power && factions.AreNeighbours(f, power)).OrderByDescending(f => f.PowerLevel).FirstOrDefault()
                ?? factions.Factions.Where(f => f != power).OrderByDescending(f => f.PowerLevel).First();
            ctx.Log.Warning($"[Powers] {power.Name} falls apart; {heir.Name} gathers what remains.");
            politics.Disperse(power, heir);
        }

        // ---- A gate or a sect founded by an elder who leaves ----

        private void Secede(FactionData parent)
        {
            if (parent.Elders.Count < 2 || Crowded || !ctx.Rng.Chance(Settings.SecessionChance)) return;
            var strongest = parent.Elders.OrderByDescending(e => e.Realm).First();
            var founder = parent.Elders.Where(e => e != strongest && e.Realm >= CultivationRealm.PurpleMansion)
                .OrderByDescending(e => e.Realm).FirstOrDefault();
            if (founder == null) return;
            bool sect = founder.Realm >= CultivationRealm.GoldenCore;
            int needed = sect ? Settings.SectFollowers : Settings.GateFollowers;
            var followers = parent.Elders.Where(e => e != strongest && e != founder && e.Realm >= CultivationRealm.Foundation)
                .OrderByDescending(e => e.Realm).Take(needed).ToList();
            if (followers.Count < needed) return; // a gate is not founded as easily as a clan (the user's rule)
            string name = FreeName(sect ? ctx.Content.Names.SectNames : ctx.Content.Names.GateNames);
            if (name == null) return;

            int power = (int)(parent.PowerLevel * Settings.SecessionPowerShare), wealth = (int)(Math.Max(0, parent.Wealth) * Settings.SecessionWealthShare);
            var born = new FactionData
            {
                ID = ctx.Rng.NextId(), // the game's own draw: the same seed, the same world
                Name = name,
                Kind = sect ? FactionKind.Sect : FactionKind.Gate,
                RegionId = parent.RegionId,
                Path = parent.Path,
                Personality = Enum.GetValues(typeof(FactionPersonality)).Cast<FactionPersonality>().ElementAt(ctx.Rng.Next(5)),
                PowerLevel = power,
                Wealth = wealth,
                Techniques = new List<string>(parent.Techniques), // they take the arts they learnt
                Provenance = Provenance.Interpretation,
            };
            foreach (var elder in followers.Prepend(founder))
            {
                parent.Elders.Remove(elder);
                born.Elders.Add(elder);
            }
            parent.PowerLevel -= power;
            parent.Wealth -= wealth;
            ElderSystem.Sync(parent);
            ElderSystem.Sync(born);
            factions.AddFaction(born);
            ctx.Log.Warning($"[Powers] {founder.Name} leaves {parent.Name} and founds {born.Name}.");
            ctx.Events.TriggerPowerRose(born, parent.Name);
        }

        // ---- A kingdom: the hardest founding ----

        private void FoundAKingdom(FactionData power)
        {
            if (power.Kind == FactionKind.State || power.HighestRealm < CultivationRealm.GoldenCore) return;
            int vassals = politics.Bonds.Count(b => b.Kind == BondKind.Vassalage && b.A == power.Name);
            int greatest = factions.Factions.Max(f => f.PowerLevel);
            if (vassals < Settings.KingdomVassals || power.PowerLevel < greatest * Settings.KingdomPowerShare) return;
            if (!ctx.Rng.Chance(Settings.KingdomChance)) return;
            power.Kind = FactionKind.State;
            ctx.Log.Warning($"[Powers] {power.Name} founds a kingdom.");
            ctx.Events.TriggerKingdomFounded(power);
        }

        // ---- A family rises ----

        private void RiseAFamily()
        {
            if (factions.Factions.Count >= ctx.Content.Factions.Count || !ctx.Rng.Chance(Settings.RiseChance)) return;
            var used = factions.Factions.Select(f => f.FamilyName).Where(n => n != null).ToHashSet();
            var families = ctx.Content.Names.OutsiderFamilies.Where(n => !used.Contains(n) && factions.GetFactionByName($"Famille {n}") == null).ToList();
            if (families.Count == 0) return;
            string family = families[ctx.Rng.Next(families.Count)];
            var regions = ctx.Content.Factions.Select(f => f.RegionId).Where(r => r != null).Distinct().ToList();
            var born = new FactionData
            {
                ID = ctx.Rng.NextId(),
                Name = $"Famille {family}",
                FamilyName = family,
                Kind = FactionKind.Family,
                RegionId = regions.Count == 0 ? ctx.Content.Clan.HomeRegion : regions[ctx.Rng.Next(regions.Count)],
                Path = CultivationPath.Immortal,
                Personality = Enum.GetValues(typeof(FactionPersonality)).Cast<FactionPersonality>().ElementAt(ctx.Rng.Next(5)),
                PowerLevel = Settings.RisenPower,
                Wealth = Settings.RisenWealth,
                Provenance = Provenance.Interpretation,
            };
            born.Elders.Add(elders.NewElder(born, CultivationRealm.Foundation));
            born.Elders.Add(elders.NewElder(born, CultivationRealm.QiRefinement));
            ElderSystem.Sync(born);
            factions.AddFaction(born);
            ctx.Log.Info($"[Powers] The {family} family rises.");
            ctx.Events.TriggerPowerRose(born, null);
        }

        /// <summary>The world keeps its shape: no founding once it counts a few more powers than at first (the user's choice).</summary>
        private bool Crowded => factions.Factions.Count >= ctx.Content.Factions.Count + Settings.MaxExtraPowers;

        private string FreeName(IReadOnlyList<string> names)
        {
            var free = names.Where(n => factions.GetFactionByName(n) == null && ctx.Content.Factions.All(f => f.Name != n)).ToList();
            return free.Count == 0 ? null : free[ctx.Rng.Next(free.Count)];
        }
    }
}
