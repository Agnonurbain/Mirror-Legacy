using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Clan;
using MirrorChronicles.Characters;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// Handles the assignment and resolution of yearly tasks for all clan members.
    /// </summary>
    public class TaskAssignmentSystem : MonoBehaviour
    {
        public static TaskAssignmentSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Resolve tasks when transitioning from Management to Events phase
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        public void AssignTask(CharacterData character, TaskType newTask)
        {
            if (!character.IsAlive) return;

            // Basic validation (e.g., Embryonic stage can only Cultivate or Rest)
            if (character.Realm == CultivationRealm.Embryonic && 
                newTask != TaskType.Cultivation && 
                newTask != TaskType.Rest && 
                newTask != TaskType.None)
            {
                Debug.LogWarning($"[TaskAssignmentSystem] {character.FullName} is in Embryonic stage and cannot perform complex tasks.");
                return;
            }

            character.CurrentTask = newTask;
            Debug.Log($"[TaskAssignmentSystem] Assigned {newTask} to {character.FullName}.");
        }

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == GamePhase.Events)
            {
                // The Management phase just ended. Resolve all assigned tasks.
                ProcessYearlyTasks(ClanManager.Instance.LivingMembers);
            }
        }

        private void ProcessYearlyTasks(List<CharacterData> activeMembers)
        {
            Debug.Log("[TaskAssignmentSystem] --- Resolving Yearly Tasks ---");

            int totalMinedStones = 0;
            int patrolCount = 0;

            foreach (var member in activeMembers)
            {
                if (!member.IsAlive) continue;

                switch (member.CurrentTask)
                {
                    case TaskType.Cultivation:
                        CultivationSystem.Instance.ProcessYearlyCultivation(member);
                        break;

                    case TaskType.Mine:
                        // Mining yield depends on Realm
                        int yield = 50 + ((int)member.Realm * 25);
                        totalMinedStones += yield;
                        Debug.Log($"[TaskAssignmentSystem] {member.FullName} mined {yield} Spirit Stones.");
                        break;

                    case TaskType.Patrol:
                        patrolCount++;
                        Debug.Log($"[TaskAssignmentSystem] {member.FullName} is patrolling the domain.");
                        break;

                    case TaskType.Rest:
                        // Recover mental stability faster
                        MentalStabilitySystem.Instance.ApplyModifier(member, +5);
                        Debug.Log($"[TaskAssignmentSystem] {member.FullName} rested (+5 Stability).");
                        break;

                    // Other tasks (Study, Teaching, Diplomacy, Espionage) will be implemented in later phases
                }
            }

            // Apply global task results
            if (totalMinedStones > 0)
            {
                ResourceManager.Instance.AddSpiritStones(totalMinedStones);
            }

            if (patrolCount > 0)
            {
                // In Phase 2, this will reduce the probability of negative random events (e.g., Monster Attacks)
                Debug.Log($"[TaskAssignmentSystem] Domain security increased by {patrolCount * 10}%.");
            }
        }
    }
}
