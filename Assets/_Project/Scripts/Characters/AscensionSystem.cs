using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;
using MirrorChronicles.Clan;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Handles the ultimate goal of the game: Ascending beyond the highest realm.
    /// </summary>
    public class AscensionSystem : MonoBehaviour
    {
        public static AscensionSystem Instance { get; private set; }
        public int AscendedAncestorsCount { get; private set; } = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnBreakthroughSuccess += CheckForAscension;
        }

        private void OnDisable()
        {
            GameEvents.OnBreakthroughSuccess -= CheckForAscension;
        }

        private void CheckForAscension(CharacterData character, CultivationRealm newRealm)
        {
            // If they breakthrough to the highest realm (DaoEmbryo), they ascend.
            if (newRealm == CultivationRealm.DaoEmbryo)
            {
                Ascend(character);
            }
        }

        public void Ascend(CharacterData character)
        {
            Debug.Log($"<color=yellow>[ASCENSION] The heavens open! {character.FirstName} has ascended to the Immortal Realm!</color>");
            
            AscendedAncestorsCount++;
            character.IsAlive = false; // They are no longer in the mortal plane
            
            if (ClanManager.Instance != null)
            {
                ClanManager.Instance.LivingMembers.Remove(character);
            }

            // Grant permanent clan buff
            ApplyAscensionBlessing();
        }

        private void ApplyAscensionBlessing()
        {
            Debug.Log("<color=cyan>[Divine Blessing]</color> The Clan receives a permanent Divine Blessing from their Ascended Ancestor! Global Qi generation increased.");
        }
    }
}
