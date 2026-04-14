using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Characters;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Handles marriages within the clan (Love) or with other factions (Arranged).
    /// </summary>
    public class MarriageSystem : MonoBehaviour
    {
        public static MarriageSystem Instance { get; private set; }

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
        /// Validates if two characters can marry (Age > 18, not already married, not direct siblings).
        /// </summary>
        public bool CanMarry(CharacterData personA, CharacterData personB)
        {
            if (personA == null || personB == null) return false;
            if (!personA.IsAlive || !personB.IsAlive) return false;
            if (personA.Age < 18 || personB.Age < 18) return false;
            if (!string.IsNullOrEmpty(personA.SpouseID) || !string.IsNullOrEmpty(personB.SpouseID)) return false;

            // Basic incest check (siblings)
            bool shareFather = !string.IsNullOrEmpty(personA.FatherID) && personA.FatherID == personB.FatherID;
            bool shareMother = !string.IsNullOrEmpty(personA.MotherID) && personA.MotherID == personB.MotherID;
            
            if (shareFather || shareMother)
            {
                Debug.LogWarning("[MarriageSystem] Cannot marry direct siblings.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// A marriage for love within the clan or with a wandering rogue cultivator.
        /// Boosts mental stability but gives no political power.
        /// </summary>
        public bool HandleLoveMarriage(CharacterData member, CharacterData spouse)
        {
            if (!CanMarry(member, spouse)) return false;

            member.SpouseID = spouse.ID;
            spouse.SpouseID = member.ID;

            // Big mental stability boost
            MentalStabilitySystem.Instance.ApplyModifier(member, +10);
            MentalStabilitySystem.Instance.ApplyModifier(spouse, +10);

            Debug.Log($"[MarriageSystem] LOVE MARRIAGE: {member.FullName} and {spouse.FullName} are now married. (+10 Stability)");
            return true;
        }

        /// <summary>
        /// A political marriage with a rival faction.
        /// Boosts relations significantly, but may harm the member's mental stability if forced.
        /// </summary>
        public bool HandleArrangedMarriage(CharacterData member, string targetFactionId, bool isForced)
        {
            if (member == null || !member.IsAlive || member.Age < 18 || !string.IsNullOrEmpty(member.SpouseID)) 
                return false;

            var faction = FactionManager.Instance.GetFactionByID(targetFactionId);
            if (faction == null) return false;

            // Create a dummy spouse from the faction
            CharacterData politicalSpouse = new CharacterData
            {
                FirstName = "Spouse",
                LastName = faction.Name.Split(' ')[0], // e.g., "Wang"
                Age = member.Age + UnityEngine.Random.Range(-5, 5),
                SpiritualRoot = 30, // Average
                Realm = CultivationRealm.QiRefinement
            };

            member.SpouseID = politicalSpouse.ID;
            politicalSpouse.SpouseID = member.ID;

            // Political consequences
            int relationBoost = 25;
            FactionManager.Instance.ChangeRelation(targetFactionId, relationBoost);

            // Personal consequences
            if (isForced)
            {
                MentalStabilitySystem.Instance.ApplyModifier(member, -15);
                Debug.Log($"[MarriageSystem] ARRANGED MARRIAGE (Forced): {member.FullName} married into {faction.Name}. Relations +{relationBoost}, Stability -15.");
            }
            else
            {
                MentalStabilitySystem.Instance.ApplyModifier(member, +5);
                Debug.Log($"[MarriageSystem] ARRANGED MARRIAGE (Willing): {member.FullName} married into {faction.Name}. Relations +{relationBoost}, Stability +5.");
            }

            return true;
        }
    }
}
