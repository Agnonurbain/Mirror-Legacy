using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Manages all rival factions in the world and their autonomous AI during the Event Phase.
    /// </summary>
    public class FactionManager : MonoBehaviour
    {
        public static FactionManager Instance { get; private set; }

        [Header("Faction Templates (optional — uses defaults if empty)")]
        public List<FactionTemplate> Templates = new List<FactionTemplate>();

        public List<FactionData> Factions { get; private set; } = new List<FactionData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeWorldFactions();
        }

        private void OnEnable()
        {
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void InitializeWorldFactions()
        {
            Debug.Log("[FactionManager] Initializing world factions...");

            if (Templates.Count > 0)
            {
                foreach (var t in Templates)
                {
                    if (t == null) continue;
                    Factions.Add(new FactionData
                    {
                        Name = t.FactionName,
                        Personality = t.Personality,
                        PowerLevel = t.PowerLevel,
                        Wealth = t.Wealth,
                        RelationWithPlayer = t.StartingRelation
                    });
                }
            }
            else
            {
                BuildDefaultFactions();
            }

            Debug.Log($"[FactionManager] {Factions.Count} factions initialized.");
        }

        private void BuildDefaultFactions()
        {
            Factions.Add(new FactionData { Name = "Wang Family", Personality = FactionPersonality.Aggressive, PowerLevel = 500, Wealth = 1000, RelationWithPlayer = -20 });
            Factions.Add(new FactionData { Name = "Zhao Merchant Guild", Personality = FactionPersonality.Merchant, PowerLevel = 200, Wealth = 5000, RelationWithPlayer = 10 });
            Factions.Add(new FactionData { Name = "Azure Cloud Sect", Personality = FactionPersonality.Isolationist, PowerLevel = 5000, Wealth = 2000, RelationWithPlayer = 0 });
            Factions.Add(new FactionData { Name = "Iron Fist Hall", Personality = FactionPersonality.Aggressive, PowerLevel = 800, Wealth = 600, RelationWithPlayer = -10 });
            Factions.Add(new FactionData { Name = "Jade Phoenix Pavilion", Personality = FactionPersonality.Merchant, PowerLevel = 300, Wealth = 8000, RelationWithPlayer = 15 });
            Factions.Add(new FactionData { Name = "Shadow Veil Sect", Personality = FactionPersonality.Manipulative, PowerLevel = 1200, Wealth = 1500, RelationWithPlayer = -5 });
            Factions.Add(new FactionData { Name = "Verdant Bamboo Hermitage", Personality = FactionPersonality.Isolationist, PowerLevel = 700, Wealth = 400, RelationWithPlayer = 5 });
            Factions.Add(new FactionData { Name = "Golden Sun Empire", Personality = FactionPersonality.Expansionist, PowerLevel = 10000, Wealth = 20000, RelationWithPlayer = 0 });
        }

        public FactionData GetFactionByID(string id)
        {
            return Factions.Find(f => f.ID == id);
        }

        public void ChangeRelation(string factionId, int amount)
        {
            var faction = GetFactionByID(factionId);
            if (faction != null)
            {
                faction.RelationWithPlayer = Mathf.Clamp(faction.RelationWithPlayer + amount, -100, 100);
                Debug.Log($"[FactionManager] Relation with {faction.Name} changed by {amount}. Current: {faction.RelationWithPlayer}");
            }
        }

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == GamePhase.Events)
            {
                ProcessYearlyFactionAI();
            }
        }

        public void ProcessYearlyFactionAI()
        {
            Debug.Log("[FactionManager] --- Processing World Faction AI ---");

            foreach (var faction in Factions)
            {
                switch (faction.Personality)
                {
                    case FactionPersonality.Aggressive:
                        if (faction.RelationWithPlayer <= -80)
                            Debug.LogWarning($"[FactionManager] {faction.Name} has declared WAR!");
                        else if (faction.RelationWithPlayer < -30)
                            ChangeRelation(faction.ID, -5);
                        break;

                    case FactionPersonality.Merchant:
                        if (faction.RelationWithPlayer >= 50)
                            Debug.Log($"[FactionManager] {faction.Name} offers a trade deal.");
                        else
                            ChangeRelation(faction.ID, +2);
                        break;

                    case FactionPersonality.Manipulative:
                        if (faction.RelationWithPlayer < 0)
                        {
                            Debug.LogWarning($"[FactionManager] {faction.Name} spreads rumors. (Prestige loss)");
                            ChangeRelation(faction.ID, -3);
                        }
                        break;

                    case FactionPersonality.Expansionist:
                        faction.PowerLevel += 100;
                        if (faction.RelationWithPlayer < -50)
                            Debug.LogWarning($"[FactionManager] {faction.Name} eyes your territory.");
                        break;

                    case FactionPersonality.Isolationist:
                        // Slowly drift toward neutral
                        if (faction.RelationWithPlayer > 0)
                            ChangeRelation(faction.ID, -1);
                        else if (faction.RelationWithPlayer < 0)
                            ChangeRelation(faction.ID, +1);
                        break;
                }
            }
        }
    }
}
