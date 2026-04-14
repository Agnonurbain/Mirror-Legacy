using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Manages the initiative queue and turn order for all units in combat.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        private List<CombatUnit> _allUnits = new List<CombatUnit>();
        private Queue<CombatUnit> _turnQueue = new Queue<CombatUnit>();

        public CombatUnit CurrentUnit { get; private set; }
        public int CurrentRound { get; private set; } = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(List<CombatUnit> units)
        {
            _allUnits = units;
            CurrentRound = 0;
            StartNewRound();
        }

        /// <summary>
        /// Calculates initiative and builds the queue for the round.
        /// Initiative = Agility + (Realm * 10)
        /// </summary>
        private void StartNewRound()
        {
            CurrentRound++;
            Debug.Log($"[TurnManager] --- Starting Round {CurrentRound} ---");

            // Remove dead units
            _allUnits.RemoveAll(u => u.CurrentVitality <= 0);

            if (_allUnits.Count == 0) return;

            // Sort by initiative (descending)
            var sortedUnits = _allUnits.OrderByDescending(u => CalculateInitiative(u)).ToList();

            _turnQueue.Clear();
            foreach (var unit in sortedUnits)
            {
                _turnQueue.Enqueue(unit);
            }

            NextTurn();
        }

        private int CalculateInitiative(CombatUnit unit)
        {
            int initiative = unit.Agility + ((int)unit.BaseData.Realm * 10);
            
            // Terrain modifiers could be applied here if they affect initiative
            
            return initiative;
        }

        /// <summary>
        /// Advances to the next unit in the queue.
        /// </summary>
        public void NextTurn()
        {
            // Clean up dead units from the queue
            while (_turnQueue.Count > 0 && _turnQueue.Peek().CurrentVitality <= 0)
            {
                _turnQueue.Dequeue();
            }

            if (_turnQueue.Count == 0)
            {
                // Round is over, start a new one
                StartNewRound();
                return;
            }

            CurrentUnit = _turnQueue.Dequeue();
            CurrentUnit.ResetTurnState();

            Debug.Log($"[TurnManager] It is now {CurrentUnit.BaseData.FullName}'s turn. (Ally: {CurrentUnit.IsAlly})");

            // Notify the Combat Manager to change state based on whose turn it is
            if (CurrentUnit.IsAlly)
            {
                TacticalCombatManager.Instance.ChangeState(CombatState.PlayerTurn);
            }
            else
            {
                TacticalCombatManager.Instance.ChangeState(CombatState.EnemyTurn);
            }
        }

        public void EndCurrentTurn()
        {
            if (CurrentUnit != null)
            {
                Debug.Log($"[TurnManager] {CurrentUnit.BaseData.FullName} ended their turn.");
            }
            
            // Check win/loss conditions before passing the turn
            TacticalCombatManager.Instance.CheckCombatEndConditions();
            
            if (TacticalCombatManager.Instance.CurrentState != CombatState.Victory && 
                TacticalCombatManager.Instance.CurrentState != CombatState.Defeat)
            {
                NextTurn();
            }
        }
    }
}
