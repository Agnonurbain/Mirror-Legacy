using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Executes a technique (MartialArt or SupportArt) that consumes Qi.
    /// Damage/heal scales with PowerModifier and elemental affinity bonus.
    /// </summary>
    public class TechniqueAction : ICombatAction
    {
        public ActionType Type => ActionType.Technique;

        private readonly TechniqueData _technique;

        public TechniqueAction(TechniqueData technique)
        {
            _technique = technique;
        }

        public int GetQiCost() => _technique.QiCost;

        public bool IsValid(CombatUnit user, GridCell targetCell)
        {
            if (user.HasActedThisTurn) return false;
            if (_technique == null) return false;

            // Character must know this technique
            if (user.BaseData.KnownTechniqueIDs == null ||
                !user.BaseData.KnownTechniqueIDs.Contains(_technique.ID))
            {
                return false;
            }

            // Character must meet the realm requirement
            if (user.BaseData.Realm < _technique.RequiredRealm) return false;

            // Must have enough Qi
            if (user.CurrentQi < _technique.QiCost) return false;

            // Range & target validation depends on technique type
            switch (_technique.Type)
            {
                case TechniqueType.MartialArt:
                    // Offensive: needs a valid enemy target in range
                    if (targetCell == null || !targetCell.IsOccupied) return false;
                    if (targetCell.Occupant.IsAlly == user.IsAlly) return false;
                    return GridSystem.Instance.GetDistance(user.CurrentCell, targetCell) <= _technique.Range;

                case TechniqueType.SupportArt:
                    // Supportive: needs a valid ally target in range
                    if (targetCell == null || !targetCell.IsOccupied) return false;
                    if (targetCell.Occupant.IsAlly != user.IsAlly) return false;
                    return GridSystem.Instance.GetDistance(user.CurrentCell, targetCell) <= _technique.Range;

                default:
                    // CultivationMethod: not usable in combat
                    return false;
            }
        }

        public void Execute(CombatUnit user, GridCell targetCell)
        {
            if (!IsValid(user, targetCell))
            {
                Debug.LogWarning($"[TechniqueAction] Invalid technique use by {user.BaseData.FullName}.");
                return;
            }

            user.ConsumeQi(_technique.QiCost);

            CombatUnit target = targetCell.Occupant;

            switch (_technique.Type)
            {
                case TechniqueType.MartialArt:
                    ExecuteOffensive(user, target);
                    break;

                case TechniqueType.SupportArt:
                    ExecuteSupport(user, target);
                    break;
            }

            user.HasActedThisTurn = true;
        }

        private void ExecuteOffensive(CombatUnit user, CombatUnit target)
        {
            // Base damage = PowerModifier × (1 + Realm × 0.2)
            float realmMultiplier = 1f + ((int)user.BaseData.Realm * 0.2f);
            int rawDamage = Mathf.RoundToInt(_technique.PowerModifier * realmMultiplier);

            // Elemental affinity bonus: +25% if user's affinity matches technique element
            if (user.BaseData.Affinity == _technique.DominantElement &&
                _technique.DominantElement != Element.None)
            {
                rawDamage = Mathf.RoundToInt(rawDamage * 1.25f);
            }

            // Terrain element bonus: +20% if Water technique on Water terrain
            if (target.CurrentCell != null &&
                target.CurrentCell.Terrain == TerrainType.Water &&
                _technique.DominantElement == Element.Water)
            {
                rawDamage = Mathf.RoundToInt(rawDamage * 1.2f);
            }

            int finalDamage = Mathf.Max(1, rawDamage - target.Defense);

            Debug.Log($"[TechniqueAction] {user.BaseData.FullName} uses [{_technique.Name}] on {target.BaseData.FullName} for {finalDamage} damage! (Qi: -{_technique.QiCost})");

            target.TakeDamage(finalDamage);
        }

        private void ExecuteSupport(CombatUnit user, CombatUnit target)
        {
            // Heal amount = PowerModifier × (1 + Realm × 0.15)
            float realmMultiplier = 1f + ((int)user.BaseData.Realm * 0.15f);
            int healAmount = Mathf.RoundToInt(_technique.PowerModifier * realmMultiplier);

            // Affinity bonus: +25% if user's affinity matches technique element
            if (user.BaseData.Affinity == _technique.DominantElement &&
                _technique.DominantElement != Element.None)
            {
                healAmount = Mathf.RoundToInt(healAmount * 1.25f);
            }

            target.Heal(healAmount);

            Debug.Log($"[TechniqueAction] {user.BaseData.FullName} uses [{_technique.Name}] on {target.BaseData.FullName}, healing up to {healAmount} HP! (Qi: -{_technique.QiCost})");
        }
    }
}
