using UnityEngine;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Handles a basic physical attack against an adjacent target.
    /// </summary>
    public class AttackAction : ICombatAction
    {
        public ActionType Type => ActionType.PhysicalAttack;

        public int GetQiCost() => 0; // Basic attacks cost no Qi

        public bool IsValid(CombatUnit user, GridCell targetCell)
        {
            if (user.HasActedThisTurn) return false;
            if (targetCell == null || !targetCell.IsOccupied) return false;
            
            // Cannot attack allies
            if (targetCell.Occupant.IsAlly == user.IsAlly) return false;

            // Must be adjacent (range 1)
            int distance = GridSystem.Instance.GetDistance(user.CurrentCell, targetCell);
            return distance == 1;
        }

        public void Execute(CombatUnit user, GridCell targetCell)
        {
            if (!IsValid(user, targetCell))
            {
                Debug.LogWarning($"[AttackAction] Invalid attack for {user.BaseData.FullName}.");
                return;
            }

            CombatUnit target = targetCell.Occupant;

            // Damage Formula: Strength * (1 + Realm * 0.2) - TargetDefense
            float realmMultiplier = 1f + ((int)user.BaseData.Realm * 0.2f);
            int rawDamage = Mathf.RoundToInt(user.Strength * realmMultiplier);
            
            int finalDamage = Mathf.Max(1, rawDamage - target.Defense); // Minimum 1 damage

            Debug.Log($"[AttackAction] {user.BaseData.FullName} attacks {target.BaseData.FullName} for {finalDamage} damage!");
            
            target.TakeDamage(finalDamage);
            user.HasActedThisTurn = true;
        }
    }
}
