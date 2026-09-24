using UnityEngine;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Attempts to flee combat. Success = user Agility roll vs nearest enemy Agility.
    /// On failure the unit loses its turn.
    /// </summary>
    public class FleeAction : ICombatAction
    {
        public ActionType Type => ActionType.Flee;

        public int GetQiCost() => 0;

        public bool IsValid(CombatUnit user, GridCell targetCell)
        {
            if (user.HasActedThisTurn) return false;
            return user.CurrentVitality > 0;
        }

        public void Execute(CombatUnit user, GridCell targetCell)
        {
            if (!IsValid(user, targetCell))
            {
                Debug.LogWarning($"[FleeAction] Invalid flee attempt by {user.BaseData.FullName}.");
                return;
            }

            CombatUnit nearestEnemy = FindNearestEnemy(user);
            int enemyAgility = nearestEnemy != null ? nearestEnemy.Agility : 0;

            // Success chance: userAgility / (userAgility + enemyAgility)
            float successChance = user.Agility / (float)(user.Agility + enemyAgility + 1);
            bool success = Random.value < successChance;

            if (success)
            {
                Debug.Log($"[FleeAction] {user.BaseData.FullName} successfully fled combat!");
                user.SetCell(null);
                user.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log($"[FleeAction] {user.BaseData.FullName} failed to flee! Turn lost.");
            }

            user.HasActedThisTurn = true;
            user.HasMovedThisTurn = true;
        }

        private CombatUnit FindNearestEnemy(CombatUnit user)
        {
            if (GridSystem.Instance == null) return null;

            CombatUnit nearest = null;
            int minDist = int.MaxValue;

            for (int x = 0; x < GridSystem.Instance.Width; x++)
            {
                for (int y = 0; y < GridSystem.Instance.Height; y++)
                {
                    var cell = GridSystem.Instance.GetCellAt(x, y);
                    if (cell == null || !cell.IsOccupied) continue;
                    if (cell.Occupant.IsAlly == user.IsAlly) continue;

                    int dist = GridSystem.Instance.GetDistance(user.CurrentCell, cell);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = cell.Occupant;
                    }
                }
            }

            return nearest;
        }
    }
}
