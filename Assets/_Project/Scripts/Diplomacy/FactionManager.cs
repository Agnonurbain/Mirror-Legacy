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

        /// <summary>
        /// Creates the initial rival families for Phase 3 testing.
        /// </summary>
        private void InitializeWorldFactions()
        {
            Debug.Log("[FactionManager] Initializing world factions...");

            Factions.Add(new FactionData
            {
                Name = "Wang Family",
                Personality = FactionPersonality.Aggressive,
                PowerLevel = 500,
                Wealth = 1000,
                RelationWithPlayer = -20 // Slightly hostile
            });

            Factions.Add(new FactionData
            {
                Name = "Zhao Merchant Guild",
                Personality = FactionPersonality.Merchant,
                PowerLevel = 200,
                Wealth = 5000,
                RelationWithPlayer = 10 // Slightly friendly
            });

            Factions.Add(new FactionData
            {
                Name = "Azure Cloud Sect",
                Personality = FactionPersonality.Isolationist,
                PowerLevel = 5000, // Very powerful
                Wealth = 2000,
                RelationWithPlayer = 0
            });
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

        /// <summary>
        /// Autonomous AI for rival factions. They make decisions based on their personality and relations.
        /// </summary>
        private void ProcessYearlyFactionAI()
        {
            Debug.Log("[FactionManager] --- Processing World Faction AI ---");

            foreach (var faction in Factions)
            {
                // Simple AI logic for prototype
                if (faction.RelationWithPlayer <= -80 && faction.Personality == FactionPersonality.Aggressive)
                {
                    // In a full game, compare PowerLevel with player's clan before attacking
                    Debug.LogWarning($"[FactionManager] {faction.Name} has declared WAR on your clan!");
                    // Trigger combat event
                }
                else if (faction.RelationWithPlayer >= 50 && faction.Personality == FactionPersonality.Merchant)
                {
                    Debug.Log($"[FactionManager] {faction.Name} offers a lucrative trade deal.");
                    // Trigger trade event
                }
                else if (faction.RelationWithPlayer < 0 && faction.Personality == FactionPersonality.Manipulative)
                {
                    Debug.LogWarning($"[FactionManager] {faction.Name} is spreading rumors about your clan. (Prestige loss)");
                    // Apply penalty
                }
            }
        }
    }
}
