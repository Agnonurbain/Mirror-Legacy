using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Clan;
using MirrorChronicles.Events;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// Manages clan buildings (8 types × 5 levels).
    /// Buildings provide passive bonuses processed each year.
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        public static BuildingSystem Instance { get; private set; }

        public List<BuildingData> Buildings { get; private set; } = new List<BuildingData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeBuildings();
        }

        private void OnEnable()
        {
            GameEvents.OnYearStarted += HandleYearStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnYearStarted -= HandleYearStarted;
        }

        private void InitializeBuildings()
        {
            Buildings.Clear();
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
            {
                Buildings.Add(new BuildingData(type));
            }
        }

        public BuildingData GetBuilding(BuildingType type)
        {
            return Buildings.Find(b => b.Type == type);
        }

        public bool CanUpgrade(BuildingType type)
        {
            var building = GetBuilding(type);
            if (building == null || building.Level >= 5) return false;

            if (ResourceManager.Instance == null) return false;
            if (ResourceManager.Instance.SpiritStones < building.UpgradeCost) return false;

            var requiredRealm = BuildingData.GetRequiredRealm(type);
            if (requiredRealm > CultivationRealm.Embryonic && ClanManager.Instance != null)
            {
                bool meetsRealm = ClanManager.Instance.LivingMembers.Any(m => m.Realm >= requiredRealm);
                if (!meetsRealm) return false;
            }

            return true;
        }

        public bool Upgrade(BuildingType type)
        {
            if (!CanUpgrade(type)) return false;

            var building = GetBuilding(type);
            if (!ResourceManager.Instance.ConsumeSpiritStones(building.UpgradeCost)) return false;

            building.Level++;
            Debug.Log($"[BuildingSystem] {type} upgraded to level {building.Level}! Cost: {BuildingData.GetUpgradeCost(building.Level)}");
            return true;
        }

        private void HandleYearStarted(int year)
        {
            ApplyPassiveBonuses();
        }

        private void ApplyPassiveBonuses()
        {
            foreach (var building in Buildings)
            {
                if (building.Level <= 0) continue;

                switch (building.Type)
                {
                    case BuildingType.TrainingHall:
                        // +2 XP per level to all cultivating members
                        if (ClanManager.Instance != null)
                        {
                            foreach (var m in ClanManager.Instance.LivingMembers)
                            {
                                if (m.CurrentTask == TaskType.Cultivation)
                                    m.CultivationXP += building.Level * 2;
                            }
                        }
                        break;

                    case BuildingType.Forge:
                        // +5% mine yield per level (handled via modifier)
                        break;

                    case BuildingType.Library:
                        // +3% study discovery chance per level (handled in TaskAssignmentSystem)
                        break;

                    case BuildingType.HerbGarden:
                        // +1 MS per level to all resting members
                        if (ClanManager.Instance != null)
                        {
                            foreach (var m in ClanManager.Instance.LivingMembers)
                            {
                                if (m.CurrentTask == TaskType.Rest)
                                    Characters.MentalStabilitySystem.Instance?.ApplyModifier(m, building.Level);
                            }
                        }
                        break;

                    case BuildingType.Mine:
                        // +20 stones per level per year
                        ResourceManager.Instance?.AddSpiritStones(building.Level * 20);
                        break;

                    case BuildingType.CouncilRoom:
                        // +2 diplomacy bonus per level (handled in TaskAssignmentSystem)
                        break;

                    case BuildingType.MeditationPagoda:
                        // +1 MS to all members per level
                        if (ClanManager.Instance != null)
                        {
                            foreach (var m in ClanManager.Instance.LivingMembers)
                                Characters.MentalStabilitySystem.Instance?.ApplyModifier(m, building.Level);
                        }
                        break;

                    case BuildingType.ProtectiveFormation:
                        // Reduces event damage — checked by EventManager
                        break;
                }
            }
        }

        public int GetForgeBonus() => GetBuilding(BuildingType.Forge)?.Level ?? 0;
        public int GetLibraryBonus() => GetBuilding(BuildingType.Library)?.Level ?? 0;
        public int GetCouncilBonus() => GetBuilding(BuildingType.CouncilRoom)?.Level ?? 0;
        public int GetFormationLevel() => GetBuilding(BuildingType.ProtectiveFormation)?.Level ?? 0;
    }
}
