using UnityEngine;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Handles the Defend action, reducing incoming damage for the round.
    /// </summary>
    public class DefendAction : ICombatAction
    {
        public ActionType Type => ActionType.Defend;

        public int GetQiCost() => 0;

        public bool IsValid(CombatUnit user, GridCell targetCell)
        {
            return !user.HasActedThisTurn;
        }

        public void Execute(CombatUnit user, GridCell targetCell)
        {
            if (!IsValid(user, targetCell)) return;

            user.IsDefending = true;
            user.HasActedThisTurn = true;
            
            Debug.Log($"[DefendAction] {user.BaseData.FullName} is defending. Incoming damage halved.");
        }
    }
}
