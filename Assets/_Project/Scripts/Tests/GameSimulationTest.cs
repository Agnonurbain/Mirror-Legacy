using UnityEngine;
using System.Collections;
using MirrorChronicles.Core;
using MirrorChronicles.Clan;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests
{
    /// <summary>
    /// A test script to simulate 10 years of gameplay automatically.
    /// Attach this to a GameObject in the scene to watch the console output.
    /// </summary>
    public class GameSimulationTest : MonoBehaviour
    {
        [Header("Simulation Settings")]
        public int YearsToSimulate = 10;
        public float DelayBetweenYears = 1.0f;

        private void Start()
        {
            StartCoroutine(RunSimulation());
        }

        private IEnumerator RunSimulation()
        {
            Debug.Log("<color=cyan>=== STARTING 10-YEAR MIRROR CHRONICLES SIMULATION ===</color>");

            // Wait a moment for all Singletons to initialize
            yield return new WaitForSeconds(1f);

            for (int year = 1; year <= YearsToSimulate; year++)
            {
                Debug.Log($"\n<color=yellow>--- YEAR {year} BEGINS ---</color>");

                // ---------------------------------------------------------
                // PHASE 1: CLAN MANAGEMENT & ECONOMY
                // ---------------------------------------------------------
                Debug.Log("<color=green>[Phase 1] Clan Management & Resource Generation</color>");
                
                // Simulate income
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddSpiritStones(150);
                    Debug.Log($"Spirit Stones: {ResourceManager.Instance.SpiritStones}");
                }

                // Simulate Cultivation for all living members
                if (ClanManager.Instance != null)
                {
                    foreach (var member in ClanManager.Instance.LivingMembers)
                    {
                        // Give them some fake XP
                        member.CultivationXP += 50;
                        Debug.Log($"{member.FirstName} cultivates. XP: {member.CultivationXP}");
                        
                        // Randomly simulate a breakthrough attempt if they are close
                        if (UnityEngine.Random.value > 0.8f)
                        {
                            Debug.Log($"{member.FirstName} attempts a breakthrough!");
                            // In a real scenario, we'd call BreakthroughSystem.Instance.AttemptBreakthrough(member)
                        }
                    }
                }

                // ---------------------------------------------------------
                // PHASE 2: EVENTS & COMBAT
                // ---------------------------------------------------------
                Debug.Log("<color=orange>[Phase 2] Random Events & Encounters</color>");
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.GenerateYearlyEvent();
                }
                else
                {
                    Debug.Log("A wandering merchant visits the clan.");
                }

                // ---------------------------------------------------------
                // PHASE 3: WORLD & DIPLOMACY
                // ---------------------------------------------------------
                Debug.Log("<color=magenta>[Phase 3] World & Diplomacy</color>");
                if (FactionManager.Instance != null)
                {
                    FactionManager.Instance.ProcessYearlyFactionAI();
                }

                // In Year 3, let's simulate the Deduction Engine
                if (year == 3 && DeductionEngine.Instance != null)
                {
                    Debug.Log("<color=pink>--- Mirror Deduction Triggered! ---</color>");
                    DeductionEngine.Instance.AddFragment(Element.Fire, 3, "Scorched Dragon Scale");
                    DeductionEngine.Instance.AddFragment(Element.Wood, 2, "Ancient Treant Bark");
                    
                    var inputs = new System.Collections.Generic.List<FragmentData>(DeductionEngine.Instance.FragmentInventory);
                    DeductionEngine.Instance.AttemptDeduction(inputs);
                }

                // In Year 5, let's simulate a marriage
                if (year == 5 && ClanManager.Instance != null && ClanManager.Instance.LivingMembers.Count > 0)
                {
                    Debug.Log("<color=pink>--- Political Marriage Triggered! ---</color>");
                    var member = ClanManager.Instance.LivingMembers[0];
                    // Assuming MarriageSystem exists and has a method, or just log it
                    Debug.Log($"Arranging political marriage for {member.FirstName} to secure an alliance.");
                }

                // ---------------------------------------------------------
                // PHASE 4: RESOLUTION
                // ---------------------------------------------------------
                Debug.Log("<color=lightblue>[Phase 4] Resolution & Aging</color>");
                
                // Simulate an Ascension in Year 7
                if (year == 7 && ClanManager.Instance != null && ClanManager.Instance.LivingMembers.Count > 1)
                {
                    var elder = ClanManager.Instance.LivingMembers[1];
                    elder.Realm = CultivationRealm.DaoEmbryo;
                    GameEvents.TriggerBreakthroughSuccess(elder, CultivationRealm.DaoEmbryo);
                }

                // Simulate a Death in Year 9
                if (year == 9 && ClanManager.Instance != null && ClanManager.Instance.LivingMembers.Count > 0)
                {
                    var victim = ClanManager.Instance.LivingMembers[0];
                    if (MirrorChronicles.Characters.AgingAndDeathSystem.Instance != null)
                    {
                        MirrorChronicles.Characters.AgingAndDeathSystem.Instance.Die(victim, DeathCause.Combat);
                    }
                    else
                    {
                        GameEvents.TriggerCharacterDied(victim, DeathCause.Combat);
                    }
                }

                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.AdvancePhase(); // Assuming it loops back to Management and triggers YearStarted
                }
                else
                {
                    // Fallback if TimeManager isn't in the scene
                    GameEvents.TriggerYearStarted(year + 1);
                }

                // Wait before next year
                yield return new WaitForSeconds(DelayBetweenYears);
            }

            Debug.Log("\n<color=cyan>=== SIMULATION COMPLETE ===</color>");
            if (ClanManager.Instance != null)
                Debug.Log($"Final Clan Size: {ClanManager.Instance.LivingMembers.Count}");
            if (ResourceManager.Instance != null)
                Debug.Log($"Final Spirit Stones: {ResourceManager.Instance.SpiritStones}");
        }
    }
}
