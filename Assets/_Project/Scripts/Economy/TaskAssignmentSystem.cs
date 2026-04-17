using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Clan;
using MirrorChronicles.Characters;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Mirror;

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
                        MentalStabilitySystem.Instance.ApplyModifier(member, +5);
                        Debug.Log($"[TaskAssignmentSystem] {member.FullName} rested (+5 Stability).");
                        break;

                    case TaskType.Study:
                        ProcessStudy(member);
                        break;

                    case TaskType.Teaching:
                        // Teaching is resolved in a second pass (needs a student).
                        break;

                    case TaskType.Diplomacy:
                        ProcessDiplomacy(member);
                        break;

                    case TaskType.Espionage:
                        ProcessEspionage(member);
                        break;
                }
            }

            // Apply global task results
            if (totalMinedStones > 0)
            {
                ResourceManager.Instance.AddSpiritStones(totalMinedStones);
            }

            if (patrolCount > 0)
            {
                Debug.Log($"[TaskAssignmentSystem] Domain security increased by {patrolCount * 10}%.");
            }

            // Second pass: resolve Teaching (mentor buffs a cultivating student)
            ProcessTeaching(activeMembers);
        }

        /// <summary>
        /// Study: increases chance of a Deduction by discovering a random fragment.
        /// Higher SpiritualRoot → better quality fragment.
        /// </summary>
        private void ProcessStudy(CharacterData member)
        {
            float discoveryChance = 0.15f + member.SpiritualRoot * 0.003f; // 15%-45%
            if (Random.value < discoveryChance && DeductionEngine.Instance != null)
            {
                int quality = Mathf.Clamp(member.SpiritualRoot / 25, 1, 4);
                DeductionEngine.Instance.AddFragment(member.Affinity, quality, $"Fragment found by {member.FullName}");
                Debug.Log($"[TaskAssignmentSystem] {member.FullName} discovered a quality-{quality} {member.Affinity} fragment while studying.");
            }
            else
            {
                member.CultivationXP += 10;
                Debug.Log($"[TaskAssignmentSystem] {member.FullName} studied but found nothing special (+10 XP).");
            }
        }

        /// <summary>
        /// Teaching: each teacher buffs one cultivating student (lowest realm first).
        /// The student gains bonus XP equal to 20 + teacher's realm × 10.
        /// </summary>
        private void ProcessTeaching(List<CharacterData> members)
        {
            var teachers = members.Where(m => m.IsAlive && m.CurrentTask == TaskType.Teaching).ToList();
            var students = members
                .Where(m => m.IsAlive && m.CurrentTask == TaskType.Cultivation)
                .OrderBy(m => m.Realm)
                .ToList();

            int studentIdx = 0;
            foreach (var teacher in teachers)
            {
                if (studentIdx >= students.Count) break;

                var student = students[studentIdx++];
                int bonus = 20 + (int)teacher.Realm * 10;
                student.CultivationXP += bonus;
                Debug.Log($"[TaskAssignmentSystem] {teacher.FullName} teaches {student.FullName} (+{bonus} XP).");
            }
        }

        /// <summary>
        /// Diplomacy: improves relation with a random faction by +5.
        /// </summary>
        private void ProcessDiplomacy(CharacterData member)
        {
            if (FactionManager.Instance == null || FactionManager.Instance.Factions.Count == 0) return;

            var factions = FactionManager.Instance.Factions;
            var target = factions[Random.Range(0, factions.Count)];
            FactionManager.Instance.ChangeRelation(target.ID, +5);
            Debug.Log($"[TaskAssignmentSystem] {member.FullName} conducted diplomacy with {target.Name} (+5 relation).");
        }

        /// <summary>
        /// Espionage: chance to steal a technique fragment from a faction.
        /// Success = SpiritualRoot × 0.5 − FactionPower / 100.
        /// Failure = relation −20 with that faction.
        /// </summary>
        private void ProcessEspionage(CharacterData member)
        {
            if (FactionManager.Instance == null || FactionManager.Instance.Factions.Count == 0) return;

            var factions = FactionManager.Instance.Factions;
            var target = factions[Random.Range(0, factions.Count)];

            float successChance = member.SpiritualRoot * 0.005f - target.PowerLevel / 100f;
            successChance = Mathf.Clamp(successChance, 0.05f, 0.60f);

            if (Random.value < successChance)
            {
                if (DeductionEngine.Instance != null)
                {
                    int quality = Mathf.Clamp(target.PowerLevel / 25, 1, 4);
                    DeductionEngine.Instance.AddFragment(
                        member.Affinity, quality, $"Stolen from {target.Name} by {member.FullName}");
                }
                Debug.Log($"[TaskAssignmentSystem] {member.FullName} successfully stole intelligence from {target.Name}!");
            }
            else
            {
                FactionManager.Instance.ChangeRelation(target.ID, -20);
                MentalStabilitySystem.Instance.ApplyModifier(member, -10);
                Debug.Log($"[TaskAssignmentSystem] {member.FullName} was caught spying on {target.Name}! (-20 relation, -10 MS)");
            }
        }
    }
}
