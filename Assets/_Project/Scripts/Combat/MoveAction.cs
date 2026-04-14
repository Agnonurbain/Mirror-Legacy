using UnityEngine;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Handles moving a unit from its current cell to a target cell.
    /// </summary>
    public class MoveAction : ICombatAction
    {
        public ActionType Type => ActionType.Move;

        public int GetQiCost() => 0; // Movement costs no Qi

        public bool IsValid(CombatUnit user, GridCell targetCell)
        {
            if (user.HasMovedThisTurn) return false;
            if (targetCell == null || targetCell.IsOccupied) return false;

            int distance = GridSystem.Instance.GetDistance(user.CurrentCell, targetCell);
            int maxMovement = Mathf.Max(1, user.Agility / 10); // Minimum 1 tile

            // Check if within range and not blocked by terrain cost (simplified for prototype)
            // A real implementation would use A* pathfinding to check actual movement cost
            return distance <= maxMovement;
        }

        public void Execute(CombatUnit user, GridCell targetCell)
        {
            if (!IsValid(user, targetCell))
            {
                Debug.LogWarning($"[MoveAction] Invalid move for {user.BaseData.FullName}.");
                return;
            }

            Debug.Log($"[MoveAction] {user.BaseData.FullName} moved to ({targetCell.X}, {targetCell.Y}).");
            
            user.SetCell(targetCell);
            user.HasMovedThisTurn = true;
        }
    }
}
