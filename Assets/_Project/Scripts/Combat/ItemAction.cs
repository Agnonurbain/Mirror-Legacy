using UnityEngine;
using MirrorChronicles.Data;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Uses a consumable item (healing pill, Qi restoration pill) on self or ally.
    /// </summary>
    public class ItemAction : ICombatAction
    {
        public ActionType Type => ActionType.UseItem;

        private readonly ItemData _item;

        public ItemAction(ItemData item)
        {
            _item = item;
        }

        public int GetQiCost() => 0;

        public bool IsValid(CombatUnit user, GridCell targetCell)
        {
            if (user.HasActedThisTurn) return false;
            if (_item == null || _item.Quantity <= 0) return false;
            if (targetCell == null || !targetCell.IsOccupied) return false;
            if (targetCell.Occupant.IsAlly != user.IsAlly) return false;

            int distance = GridSystem.Instance.GetDistance(user.CurrentCell, targetCell);
            return distance <= 1;
        }

        public void Execute(CombatUnit user, GridCell targetCell)
        {
            if (!IsValid(user, targetCell))
            {
                Debug.LogWarning($"[ItemAction] Invalid item use by {user.BaseData.FullName}.");
                return;
            }

            CombatUnit target = targetCell.Occupant;
            _item.Quantity--;

            switch (_item.Type)
            {
                case ItemType.HealingPill:
                    target.Heal(_item.Power);
                    Debug.Log($"[ItemAction] {user.BaseData.FullName} uses [{_item.Name}] on {target.BaseData.FullName}, healing {_item.Power} HP!");
                    break;

                case ItemType.QiRestorationPill:
                    target.RestoreQi(_item.Power);
                    Debug.Log($"[ItemAction] {user.BaseData.FullName} uses [{_item.Name}] on {target.BaseData.FullName}, restoring {_item.Power} Qi!");
                    break;
            }

            user.HasActedThisTurn = true;
        }
    }
}
