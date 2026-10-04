using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
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

        /// <summary>The clan's Immortal Arts and the powers (set by the session): the Protective Formation asks a formation master (audit §2.3).</summary>
        public ArtSystem Arts { get; set; }
        public FactionManager Factions { get; set; }

        private FormationSettings Formation => ctx.Content.Balance.Arts.Formation;

        /// <summary>The mastery of the formations the formation's next level asks (🔎): apprentice, then adept, then master.</summary>
        private int FormationMasteryFor(int level) =>
            level >= Formation.MasterFromLevel ? ctx.Content.Balance.Arts.MasterAt
            : level >= Formation.AdeptFromLevel ? ctx.Content.Balance.Arts.AdeptAt : 1;

        /// <summary>The clan's free formation master able to raise the formation's next level, or null.</summary>
        public CharacterData FormationMaster()
        {
            if (Arts == null || !Arts.HoldsLegacy(ImmortalArt.Formations)) return null;
            int needed = FormationMasteryFor(FormationLevel + 1);
            return clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Retreat == Retreat.None && m.LastOperationYear != ctx.Clock.Year
                    && ImmortalArtRules.MayPractise(m, ImmortalArt.Formations, ctx.Content.Balance.Arts) && ArtSystem.MasteryOf(m, ImmortalArt.Formations) >= needed)
                .OrderBy(m => ArtSystem.MasteryOf(m, ImmortalArt.Formations)).FirstOrDefault(); // the least master that suffices
        }

        private string FormationRefusal()
        {
            if (FormationMaster() != null) return null;
            string rank = ImmortalArtRules.Rank(FormationMasteryFor(FormationLevel + 1), ctx.Content.Balance.Arts);
            return Arts.HoldsLegacy(ImmortalArt.Formations)
                ? $"il faut un {rank} des formations libre cette année ; ou louer celui d'une puissance amie"
                : "le clan ne tient pas l'héritage des formations ; on peut louer le maître d'une puissance amie";
        }

        /// <summary>The stones a power's formation master asks for the next level: the building's own, and its fee.</summary>
        public int HireCost()
        {
            var building = GetBuilding(BuildingType.ProtectiveFormation);
            return (int)(building.UpgradeCost * (1 + Formation.HireFeeShare));
        }

        /// <summary>Why this power cannot lend its formation master for the next level (French), or null.</summary>
        public string HireRefusal(string powerName)
        {
            var building = GetBuilding(BuildingType.ProtectiveFormation);
            if (building.Level >= MaxLevel) return "niveau maximal";
            var power = Factions?.GetFactionByName(powerName);
            if (power == null) return "puissance inconnue";
            if (power.RelationWithPlayer < Formation.HireRelation) return $"il faut une relation de {Formation.HireRelation} au moins";
            if (power.FormationLevel == 0) return "cette puissance n'a pas de maître des formations";
            if (power.FormationLevel < building.Level + 1) // its master sets no higher formation than its own (parity)
                return $"le maître de {power.Name} ne dresse qu'une formation de niveau {power.FormationLevel}";
            if (resources.SpiritStones < HireCost()) return $"pierres insuffisantes ({resources.SpiritStones}/{HireCost()})";
            return null;
        }

        /// <summary>A friendly power's formation master raises the formation by one level, for its fee. Null when done, else why not.</summary>
        public string HireFormation(string powerName)
        {
            if (HireRefusal(powerName) is { } why) return why;
            resources.ConsumeSpiritStones(HireCost());
            GetBuilding(BuildingType.ProtectiveFormation).Level++;
            ctx.Log.Info($"[Buildings] {powerName}'s formation master raises the Protective Formation to level {FormationLevel}.");
            return null;
        }

        public bool CanUpgrade(BuildingType type) => UpgradeRefusal(type) == null;

        /// <summary>Why the building cannot rise now, or null when it can: its height, a master at home to raise it, the stones.</summary>
        public string UpgradeRefusal(BuildingType type)
        {
            var building = GetBuilding(type);
            if (building.Level >= MaxLevel) return "niveau maximal";

            if (type == BuildingType.ProtectiveFormation && Arts != null) // an Immortal Art, not a realm (audit §2.3)
            {
                if (FormationRefusal() is { } noMaster) return noMaster;
            }
            else
            {
                var requiredRealm = BuildingData.GetRequiredRealm(type);
                if (requiredRealm != CultivationRealm.Embryonic
                    && !clan.LivingMembers.Any(m => m.CaptorFaction == null && m.Realm >= requiredRealm)) // a captive raises nothing at home
                    return $"demande au domaine un cultivateur de rang {RankCatalog.RealmName(requiredRealm)} ou plus";
            }
            if (resources.SpiritStones < building.UpgradeCost) return $"pierres insuffisantes ({resources.SpiritStones}/{building.UpgradeCost})";
            return null;
        }

        public bool Upgrade(BuildingType type)
        {
            if (!CanUpgrade(type)) return false;

            var building = GetBuilding(type);
            var master = type == BuildingType.ProtectiveFormation && Arts != null ? FormationMaster() : null;
            if (!resources.ConsumeSpiritStones(building.UpgradeCost)) return false;
            if (master != null) master.LastOperationYear = ctx.Clock.Year; // its year's work

            building.Level++;
            ctx.Log.Info($"[Buildings] {type} rises to level {building.Level}.");
            return true;
        }

        private UpkeepSettings Materials => ctx.Content.Balance.Upkeep;

        /// <summary>Yearly bonuses: training, restful gardens and their herbs, the mine and its ores, the meditation pagoda.</summary>
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
                        resources.AddHerbs(building.Level * Materials.HerbsPerGardenLevel); // its herbs (2026-10-01)
                        break;
                    case BuildingType.Mine:
                        resources.AddSpiritStones(building.Level * MineStonesPerLevel);
                        resources.AddOres(building.Level * Materials.OresPerMineLevel);
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
