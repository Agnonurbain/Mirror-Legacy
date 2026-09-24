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
                { AIStrategyType.Defensive, new DefensiveStrategy() },
                { AIStrategyType.Strategic, new StrategicStrategy() },
                { AIStrategyType.Berserker, new BerserkerStrategy() },
                { AIStrategyType.Cautious, new CautiousStrategy() }
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

    /// <summary>
    /// Strategic Strategy: Targets support/healer units first, uses terrain advantages.
    /// </summary>
    public class StrategicStrategy : IAIStrategy
    {
        public void ExecuteTurn(CombatUnit unit, List<CombatUnit> allAllies, List<CombatUnit> allEnemies)
        {
            Debug.Log($"[CombatAI] {unit.BaseData.FullName} is executing Strategic Strategy.");

            // 1. Prioritize support-type enemies (lowest Strength = likely supports)
            CombatUnit target = GetPriorityTarget(unit, allEnemies);
            if (target == null)
            {
                TurnManager.Instance.EndCurrentTurn();
                return;
            }

            // 2. Move towards terrain advantage if possible, otherwise towards target
            if (!unit.HasMovedThisTurn)
            {
                GridCell bestCell = FindBestTerrainNear(unit, target.CurrentCell);
                if (bestCell != null)
                {
                    var move = new MoveAction();
                    if (move.IsValid(unit, bestCell))
                        move.Execute(unit, bestCell);
                }
            }

            // 3. Attack if in range
            int distance = GridSystem.Instance.GetDistance(unit.CurrentCell, target.CurrentCell);
            if (distance == 1 && !unit.HasActedThisTurn)
            {
                var attack = new AttackAction();
                if (attack.IsValid(unit, target.CurrentCell))
                    attack.Execute(unit, target.CurrentCell);
            }

            TurnManager.Instance.EndCurrentTurn();
        }

        private CombatUnit GetPriorityTarget(CombatUnit unit, List<CombatUnit> enemies)
        {
            CombatUnit weakest = null;
            int lowestStrength = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.CurrentVitality <= 0) continue;
                if (enemy.Strength < lowestStrength)
                {
                    lowestStrength = enemy.Strength;
                    weakest = enemy;
                }
            }
            return weakest;
        }

        private GridCell FindBestTerrainNear(CombatUnit unit, GridCell targetCell)
        {
            var neighbors = GridSystem.Instance.GetAdjacentCells(unit.CurrentCell);
            GridCell best = null;
            int bestScore = -1;

            foreach (var cell in neighbors)
            {
                if (cell.IsOccupied) continue;

                int dist = GridSystem.Instance.GetDistance(cell, targetCell);
                int terrainBonus = cell.Terrain switch
                {
                    TerrainType.Forest => 3,
                    TerrainType.Mountain => 2,
                    TerrainType.ConcentratedQi => 1,
                    _ => 0
                };

                int score = terrainBonus * 10 + (10 - dist);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }
            return best;
        }
    }

    /// <summary>
    /// Berserker Strategy: Attacks nearest enemy, ignores defense, spends all Qi on techniques.
    /// </summary>
    public class BerserkerStrategy : IAIStrategy
    {
        public void ExecuteTurn(CombatUnit unit, List<CombatUnit> allAllies, List<CombatUnit> allEnemies)
        {
            Debug.Log($"[CombatAI] {unit.BaseData.FullName} is executing Berserker Strategy.");

            CombatUnit target = GetClosestEnemy(unit, allEnemies);
            if (target == null)
            {
                TurnManager.Instance.EndCurrentTurn();
                return;
            }

            // 1. Move towards nearest enemy
            if (!unit.HasMovedThisTurn)
                MoveTowards(unit, target.CurrentCell);

            int distance = GridSystem.Instance.GetDistance(unit.CurrentCell, target.CurrentCell);

            // 2. Use strongest known technique if Qi available
            if (!unit.HasActedThisTurn && unit.BaseData.KnownTechniqueIDs != null &&
                unit.BaseData.KnownTechniqueIDs.Count > 0 &&
                MirrorChronicles.Mirror.DeductionEngine.Instance != null)
            {
                var engine = MirrorChronicles.Mirror.DeductionEngine.Instance;
                Data.TechniqueData bestTech = null;
                int bestPower = 0;

                foreach (var techId in unit.BaseData.KnownTechniqueIDs)
                {
                    var tech = engine.ClanTechniques.Find(t => t.ID == techId);
                    if (tech == null || tech.Type != Data.TechniqueType.MartialArt) continue;
                    if (unit.CurrentQi < tech.QiCost) continue;
                    if (distance > tech.Range) continue;
                    if (tech.PowerModifier > bestPower)
                    {
                        bestPower = tech.PowerModifier;
                        bestTech = tech;
                    }
                }

                if (bestTech != null)
                {
                    var techAction = new TechniqueAction(bestTech);
                    if (techAction.IsValid(unit, target.CurrentCell))
                    {
                        techAction.Execute(unit, target.CurrentCell);
                        TurnManager.Instance.EndCurrentTurn();
                        return;
                    }
                }
            }

            // 3. Fall back to basic attack
            if (distance == 1 && !unit.HasActedThisTurn)
            {
                var attack = new AttackAction();
                if (attack.IsValid(unit, target.CurrentCell))
                    attack.Execute(unit, target.CurrentCell);
            }

            TurnManager.Instance.EndCurrentTurn();
        }

        private CombatUnit GetClosestEnemy(CombatUnit unit, List<CombatUnit> enemies)
        {
            CombatUnit closest = null;
            int minDist = int.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy.CurrentVitality <= 0) continue;
                int dist = GridSystem.Instance.GetDistance(unit.CurrentCell, enemy.CurrentCell);
                if (dist < minDist) { minDist = dist; closest = enemy; }
            }
            return closest;
        }

        private void MoveTowards(CombatUnit unit, GridCell targetCell)
        {
            var neighbors = GridSystem.Instance.GetAdjacentCells(unit.CurrentCell);
            GridCell bestCell = null;
            int minDist = GridSystem.Instance.GetDistance(unit.CurrentCell, targetCell);
            foreach (var cell in neighbors)
            {
                if (cell.IsOccupied) continue;
                int dist = GridSystem.Instance.GetDistance(cell, targetCell);
                if (dist < minDist) { minDist = dist; bestCell = cell; }
            }
            if (bestCell != null)
            {
                var move = new MoveAction();
                if (move.IsValid(unit, bestCell)) move.Execute(unit, bestCell);
            }
        }
    }

    /// <summary>
    /// Cautious Strategy: Flees if Vitality < 40%, saves Qi, only attacks if safe.
    /// </summary>
    public class CautiousStrategy : IAIStrategy
    {
        public void ExecuteTurn(CombatUnit unit, List<CombatUnit> allAllies, List<CombatUnit> allEnemies)
        {
            Debug.Log($"[CombatAI] {unit.BaseData.FullName} is executing Cautious Strategy.");

            float healthPercent = (float)unit.CurrentVitality / unit.MaxVitality;

            // 1. Flee if Vitality < 40%
            if (healthPercent < 0.4f)
            {
                var flee = new FleeAction();
                if (flee.IsValid(unit, null))
                {
                    flee.Execute(unit, null);
                    TurnManager.Instance.EndCurrentTurn();
                    return;
                }
            }

            // 2. Defend if enemies are adjacent
            CombatUnit nearestEnemy = GetClosestEnemy(unit, allEnemies);
            if (nearestEnemy != null)
            {
                int distance = GridSystem.Instance.GetDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

                if (distance <= 1)
                {
                    var defend = new DefendAction();
                    if (defend.IsValid(unit, unit.CurrentCell))
                    {
                        defend.Execute(unit, unit.CurrentCell);
                        TurnManager.Instance.EndCurrentTurn();
                        return;
                    }
                }

                // 3. Only attack if safe (health > 70% and adjacent)
                if (healthPercent > 0.7f && distance == 1 && !unit.HasActedThisTurn)
                {
                    var attack = new AttackAction();
                    if (attack.IsValid(unit, nearestEnemy.CurrentCell))
                        attack.Execute(unit, nearestEnemy.CurrentCell);
                }
                // 4. Otherwise retreat — move away from nearest enemy
                else if (!unit.HasMovedThisTurn)
                {
                    MoveAwayFrom(unit, nearestEnemy.CurrentCell);
                }
            }

            TurnManager.Instance.EndCurrentTurn();
        }

        private CombatUnit GetClosestEnemy(CombatUnit unit, List<CombatUnit> enemies)
        {
            CombatUnit closest = null;
            int minDist = int.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy.CurrentVitality <= 0) continue;
                int dist = GridSystem.Instance.GetDistance(unit.CurrentCell, enemy.CurrentCell);
                if (dist < minDist) { minDist = dist; closest = enemy; }
            }
            return closest;
        }

        private void MoveAwayFrom(CombatUnit unit, GridCell threatCell)
        {
            var neighbors = GridSystem.Instance.GetAdjacentCells(unit.CurrentCell);
            GridCell bestCell = null;
            int maxDist = GridSystem.Instance.GetDistance(unit.CurrentCell, threatCell);

            foreach (var cell in neighbors)
            {
                if (cell.IsOccupied) continue;
                int dist = GridSystem.Instance.GetDistance(cell, threatCell);
                if (dist > maxDist) { maxDist = dist; bestCell = cell; }
            }

            if (bestCell != null)
            {
                var move = new MoveAction();
                if (move.IsValid(unit, bestCell)) move.Execute(unit, bestCell);
            }
        }
    }
}
