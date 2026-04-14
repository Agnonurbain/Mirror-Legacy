using System.Collections.Generic;
using UnityEngine;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Strategy Pattern interface for Combat AI behaviors.
    /// </summary>
    public interface IAIStrategy
    {
        void ExecuteTurn(CombatUnit unit, List<CombatUnit> allAllies, List<CombatUnit> allEnemies);
    }

    /// <summary>
    /// The main AI controller that assigns and executes strategies for NPC units.
    /// </summary>
    public class CombatAI : MonoBehaviour
    {
        public static CombatAI Instance { get; private set; }

        private Dictionary<AIStrategyType, IAIStrategy> _strategies;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize strategies
            _strategies = new Dictionary<AIStrategyType, IAIStrategy>
            {
                { AIStrategyType.Aggressive, new AggressiveStrategy() },
                { AIStrategyType.Defensive, new DefensiveStrategy() }
                // Other strategies (Strategic, Berserker, Cautious) would be added here
            };
        }

        public void PlayTurn(CombatUnit unit, List<CombatUnit> allAllies, List<CombatUnit> allEnemies)
        {
            // For prototype, default to Aggressive if not specified
            AIStrategyType strategyType = AIStrategyType.Aggressive; 
            
            if (_strategies.TryGetValue(strategyType, out IAIStrategy strategy))
            {
                strategy.ExecuteTurn(unit, allAllies, allEnemies);
            }
            else
            {
                Debug.LogWarning($"[CombatAI] Strategy {strategyType} not found. Ending turn.");
                TurnManager.Instance.EndCurrentTurn();
            }
        }
    }

    /// <summary>
    /// Aggressive Strategy: Moves towards the closest enemy and attacks.
    /// </summary>
    public class AggressiveStrategy : IAIStrategy
    {
        public void ExecuteTurn(CombatUnit unit, List<CombatUnit> allAllies, List<CombatUnit> allEnemies)
        {
            Debug.Log($"[CombatAI] {unit.BaseData.FullName} is executing Aggressive Strategy.");

            // 1. Find closest target
            CombatUnit target = GetClosestEnemy(unit, allEnemies);
            if (target == null)
            {
                TurnManager.Instance.EndCurrentTurn();
                return;
            }

            int distance = GridSystem.Instance.GetDistance(unit.CurrentCell, target.CurrentCell);

            // 2. Move if not adjacent
            if (distance > 1 && !unit.HasMovedThisTurn)
            {
                MoveTowards(unit, target.CurrentCell);
                distance = GridSystem.Instance.GetDistance(unit.CurrentCell, target.CurrentCell);
            }

            // 3. Attack if adjacent
            if (distance == 1 && !unit.HasActedThisTurn)
            {
                var attack = new AttackAction();
                if (attack.IsValid(unit, target.CurrentCell))
                {
                    attack.Execute(unit, target.CurrentCell);
                }
            }

            // 4. End Turn
            TurnManager.Instance.EndCurrentTurn();
        }

        private CombatUnit GetClosestEnemy(CombatUnit unit, List<CombatUnit> enemies)
        {
            CombatUnit closest = null;
            int minDistance = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.CurrentVitality <= 0) continue;

                int dist = GridSystem.Instance.GetDistance(unit.CurrentCell, enemy.CurrentCell);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = enemy;
                }
            }

            return closest;
        }

        private void MoveTowards(CombatUnit unit, GridCell targetCell)
        {
            // Simplified movement: Just find an adjacent cell to the unit that is closer to the target
            var neighbors = GridSystem.Instance.GetAdjacentCells(unit.CurrentCell);
            GridCell bestCell = null;
            int minDistance = GridSystem.Instance.GetDistance(unit.CurrentCell, targetCell);

            foreach (var cell in neighbors)
            {
                if (cell.IsOccupied) continue;

                int dist = GridSystem.Instance.GetDistance(cell, targetCell);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestCell = cell;
                }
            }

            if (bestCell != null)
            {
                var move = new MoveAction();
                if (move.IsValid(unit, bestCell))
                {
                    move.Execute(unit, bestCell);
                }
            }
        }
    }

    /// <summary>
    /// Defensive Strategy: Defends if an enemy is near, otherwise moves away or waits.
    /// </summary>
    public class DefensiveStrategy : IAIStrategy
    {
        public void ExecuteTurn(CombatUnit unit, List<CombatUnit> allAllies, List<CombatUnit> allEnemies)
        {
            Debug.Log($"[CombatAI] {unit.BaseData.FullName} is executing Defensive Strategy.");
            
            // Simplified: Just defend
            var defend = new DefendAction();
            if (defend.IsValid(unit, unit.CurrentCell))
            {
                defend.Execute(unit, unit.CurrentCell);
            }

            TurnManager.Instance.EndCurrentTurn();
        }
    }
}
