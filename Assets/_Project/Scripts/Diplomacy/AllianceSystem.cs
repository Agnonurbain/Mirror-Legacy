using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Handles direct diplomatic actions initiated by the player towards other factions.
    /// </summary>
    public class AllianceSystem : MonoBehaviour
    {
        public static AllianceSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Offers Spirit Stones to improve relations.
        /// </summary>
        public bool OfferTribute(string factionId, int spiritStonesAmount)
        {
            var faction = FactionManager.Instance.GetFactionByID(factionId);
            if (faction == null) return false;

            if (ResourceManager.Instance.ConsumeSpiritStones(spiritStonesAmount))
            {
                // Base +5 relation, plus 1 for every 100 stones
                int relationBoost = 5 + (spiritStonesAmount / 100);
                
                // Merchants like money more
                if (faction.Personality == FactionPersonality.Merchant)
                {
                    relationBoost = Mathf.RoundToInt(relationBoost * 1.5f);
                }

                FactionManager.Instance.ChangeRelation(factionId, relationBoost);
                Debug.Log($"[AllianceSystem] Offered {spiritStonesAmount} stones to {faction.Name}. Relation improved by {relationBoost}.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Proposes a non-aggression pact. Requires neutral or better relations.
        /// </summary>
        public bool ProposeNonAggression(string factionId)
        {
            var faction = FactionManager.Instance.GetFactionByID(factionId);
            if (faction == null) return false;

            if (faction.RelationWithPlayer >= 0)
            {
                FactionManager.Instance.ChangeRelation(factionId, 10);
                Debug.Log($"[AllianceSystem] {faction.Name} accepted the Non-Aggression Pact.");
                return true;
            }
            else
            {
                Debug.LogWarning($"[AllianceSystem] {faction.Name} rejected the Non-Aggression Pact. Relations are too low ({faction.RelationWithPlayer}).");
                return false;
            }
        }

        public void DeclareWar(string factionId)
        {
            var faction = FactionManager.Instance.GetFactionByID(factionId);
            if (faction != null)
            {
                FactionManager.Instance.ChangeRelation(factionId, -100); // Instant hostility
                Debug.LogWarning($"[AllianceSystem] You have declared WAR on {faction.Name}!");
            }
        }
    }
}
