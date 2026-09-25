using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// Clan buildings (eight types, five levels). Passive bonuses land at the start of each year;
    /// the Forge, Library, Council Room and Protective Formation modify tasks and events.
    /// </summary>
    public sealed class BuildingSystem
    {
        public const int MaxLevel = 5;
        public const int TrainingHallXpPerLevel = 2;
        public const int MineStonesPerLevel = 20;
        public const double ForgeYieldBonusPerLevel = 0.05;
        public const double LibraryDiscoveryBonusPerLevel = 0.03;
        public const int CouncilRelationBonusPerLevel = 2;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;
        private readonly CultivationSystem cultivation;
        private readonly List<BuildingData> buildings;

        public IReadOnlyList<BuildingData> Buildings => buildings;

        public int ForgeLevel => GetBuilding(BuildingType.Forge).Level;
        public int LibraryLevel => GetBuilding(BuildingType.Library).Level;
        public int CouncilLevel => GetBuilding(BuildingType.CouncilRoom).Level;
        public int FormationLevel => GetBuilding(BuildingType.ProtectiveFormation).Level;

        public BuildingSystem(GameContext ctx, ClanManager clan, ResourceManager resources,
            MentalStabilitySystem stability, CultivationSystem cultivation)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.stability = stability;
            this.cultivation = cultivation;
            buildings = Enum.GetValues(typeof(BuildingType)).Cast<BuildingType>().Select(t => new BuildingData(t)).ToList();
        }

        public BuildingData GetBuilding(BuildingType type) => buildings.Find(b => b.Type == type);

        public bool CanUpgrade(BuildingType type)
        {
            var building = GetBuilding(type);
            if (building.Level >= MaxLevel) return false;
            if (resources.SpiritStones < building.UpgradeCost) return false;

            var requiredRealm = BuildingData.GetRequiredRealm(type);
            return requiredRealm == CultivationRealm.Embryonic || clan.LivingMembers.Any(m => m.Realm >= requiredRealm);
        }

        public bool Upgrade(BuildingType type)
        {
            if (!CanUpgrade(type)) return false;

            var building = GetBuilding(type);
            if (!resources.ConsumeSpiritStones(building.UpgradeCost)) return false;

            building.Level++;
            ctx.Log.Info($"[Buildings] {type} rises to level {building.Level}.");
            return true;
        }

        /// <summary>Yearly bonuses: training, restful gardens, the mine, the meditation pagoda.</summary>
        public void ApplyPassiveBonuses()
        {
            var members = clan.LivingMembers.ToList();
            foreach (var building in buildings.Where(b => b.Level > 0))
            {
                switch (building.Type)
                {
                    case BuildingType.TrainingHall:
                        foreach (var m in members.Where(m => m.CurrentTask == TaskType.Cultivation))
                            cultivation.GrantXp(m, building.Level * TrainingHallXpPerLevel);
                        break;
                    case BuildingType.HerbGarden:
                        foreach (var m in members.Where(m => m.CurrentTask == TaskType.Rest))
                            stability.ApplyModifier(m, building.Level);
                        break;
                    case BuildingType.Mine:
                        resources.AddSpiritStones(building.Level * MineStonesPerLevel);
                        break;
                    case BuildingType.MeditationPagoda:
                        foreach (var m in members)
                            stability.ApplyModifier(m, building.Level);
                        break;
                }
            }
        }

        /// <summary>Restores levels from a save; unknown or missing types stay at their current level.</summary>
        public void Restore(IEnumerable<BuildingData> saved)
        {
            foreach (var entry in saved)
            {
                var building = GetBuilding(entry.Type);
                if (building != null)
                    building.Level = Math.Clamp(entry.Level, 0, MaxLevel);
            }
        }
    }
}
