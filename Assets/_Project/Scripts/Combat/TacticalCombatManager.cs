using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Clan;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// The core state machine for the tactical combat scene.
    /// Orchestrates the Grid, TurnManager, and Win/Loss conditions.
    /// </summary>
    public class TacticalCombatManager : MonoBehaviour
    {
        public static TacticalCombatManager Instance { get; private set; }

        public CombatState CurrentState { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject combatUnitPrefab; // Placeholder for the actual visual prefab

        private List<CombatUnit> _allies = new List<CombatUnit>();
        private List<CombatUnit> _enemies = new List<CombatUnit>();

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
            // For Phase 2 testing, we auto-start a combat when the scene loads
            ChangeState(CombatState.Initialization);
        }

        public void ChangeState(CombatState newState)
        {
            CurrentState = newState;
            Debug.Log($"[TacticalCombatManager] State changed to: {newState}");

            switch (newState)
            {
                case CombatState.Initialization:
                    InitializeCombat();
                    break;
                case CombatState.Deployment:
                    // In a full game, player places units here. For prototype, we auto-deploy.
                    AutoDeployUnits();
                    ChangeState(CombatState.PlayerTurn); // Start the flow
                    break;
                case CombatState.PlayerTurn:
                    // Wait for player input (UI)
                    break;
                case CombatState.EnemyTurn:
                    // Trigger AI
                    ExecuteEnemyAI();
                    break;
                case CombatState.Victory:
                    HandleVictory();
                    break;
                case CombatState.Defeat:
                    HandleDefeat();
                    break;
            }
        }

        private void InitializeCombat()
        {
            // 1. Init Grid
            GridSystem.Instance.InitializeGrid(10, 10);

            // 2. Load Allies (Grab the first 3 active members from ClanManager for testing)
            _allies.Clear();
            if (ClanManager.Instance != null && ClanManager.Instance.LivingMembers.Count > 0)
            {
                var deploymentTeam = ClanManager.Instance.LivingMembers.Take(3).ToList();
                foreach (var data in deploymentTeam)
                {
                    CreateUnit(data, true);
                }
            }
            else
            {
                Debug.LogError("[TacticalCombatManager] No clan members available for combat!");
                return;
            }

            // 3. Load Enemies (Generate dummy enemies for testing)
            _enemies.Clear();
            for (int i = 0; i < 3; i++)
            {
                var enemyData = new CharacterData 
                { 
                    FirstName = $"Bandit {i+1}", 
                    LastName = "Rogue",
                    Realm = CultivationRealm.Embryonic
                };
                CreateUnit(enemyData, false);
            }

            ChangeState(CombatState.Deployment);
        }

        private void CreateUnit(CharacterData data, bool isAlly)
        {
            // Create a dummy GameObject if prefab is missing (for pure code testing)
            GameObject go = combatUnitPrefab != null ? Instantiate(combatUnitPrefab) : new GameObject($"Unit_{data.FullName}");
            
            CombatUnit unit = go.AddComponent<CombatUnit>();
            unit.Initialize(data, isAlly);

            if (isAlly) _allies.Add(unit);
            else _enemies.Add(unit);
        }

        private void AutoDeployUnits()
        {
            // Place allies on the left side (X = 0, 1)
            for (int i = 0; i < _allies.Count; i++)
            {
                GridCell cell = GridSystem.Instance.GetCellAt(0, i * 2);
                _allies[i].SetCell(cell);
            }

            // Place enemies on the right side (X = 8, 9)
            for (int i = 0; i < _enemies.Count; i++)
            {
                GridCell cell = GridSystem.Instance.GetCellAt(9, i * 2);
                _enemies[i].SetCell(cell);
            }

            // Combine and send to TurnManager
            List<CombatUnit> allUnits = new List<CombatUnit>();
            allUnits.AddRange(_allies);
            allUnits.AddRange(_enemies);
            
            TurnManager.Instance.Initialize(allUnits);
        }

        private void ExecuteEnemyAI()
        {
            CombatUnit currentEnemy = TurnManager.Instance.CurrentUnit;
            Debug.Log($"[TacticalCombatManager] Executing AI for {currentEnemy.BaseData.FullName}...");
            
            // TODO: Implement Strategy Pattern AI here.
            // For now, just end turn immediately.
            TurnManager.Instance.EndCurrentTurn();
        }

        public void CheckCombatEndConditions()
        {
            bool allAlliesDead = _allies.All(u => u.CurrentVitality <= 0);
            bool allEnemiesDead = _enemies.All(u => u.CurrentVitality <= 0);

            if (allAlliesDead)
            {
                ChangeState(CombatState.Defeat);
            }
            else if (allEnemiesDead)
            {
                ChangeState(CombatState.Victory);
            }
        }

        private void HandleVictory()
        {
            Debug.Log("[TacticalCombatManager] VICTORY! The clan prevails.");
            ProcessPostCombat();
            // TODO: Apply rewards, return to Domain scene
        }

        private void HandleDefeat()
        {
            Debug.Log("[TacticalCombatManager] DEFEAT! The clan has suffered a heavy blow.");
            ProcessPostCombat();
            // TODO: return to Domain scene
        }

        private void ProcessPostCombat()
        {
            foreach (var ally in _allies)
            {
                if (ally.CurrentVitality <= 0)
                {
                    // Character died in combat
                    if (MirrorChronicles.Characters.AgingAndDeathSystem.Instance != null)
                    {
                        MirrorChronicles.Characters.AgingAndDeathSystem.Instance.Die(ally.BaseData, DeathCause.Combat);
                    }
                    else
                    {
                        MirrorChronicles.Events.GameEvents.TriggerCharacterDied(ally.BaseData, DeathCause.Combat);
                    }
                }
                else
                {
                    // Character survived, evaluate wounds
                    if (MirrorChronicles.Characters.WoundSystem.Instance != null)
                    {
                        MirrorChronicles.Characters.WoundSystem.Instance.EvaluatePostCombatWounds(ally.BaseData, ally.MaxVitality, ally.CurrentVitality);
                    }
                }
            }
        }
    }
}
