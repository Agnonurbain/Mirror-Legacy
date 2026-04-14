using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Manages the active (living) members of the player's clan.
    /// </summary>
    public class ClanManager : MonoBehaviour
    {
        public static ClanManager Instance { get; private set; }

        public string ClanName { get; private set; } = "Li";
        public List<CharacterData> LivingMembers { get; private set; } = new List<CharacterData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeFoundingMembers();
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterDied += HandleCharacterDeath;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterDied -= HandleCharacterDeath;
        }

        /// <summary>
        /// Creates the initial 5 members of the clan for Phase 1 testing.
        /// </summary>
        private void InitializeFoundingMembers()
        {
            Debug.Log("[ClanManager] Initializing founding members...");

            // 1. The Patriarch
            var patriarch = new CharacterData
            {
                FirstName = "Wei",
                LastName = ClanName,
                IsMale = true,
                Age = 45,
                MaxLifespan = 120,
                SpiritualRoot = 65, // Excellent
                Affinity = Element.Fire,
                Realm = CultivationRealm.QiRefinement,
                MentalStability = 80
            };
            AddMember(patriarch);

            // 2. The Matriarch
            var matriarch = new CharacterData
            {
                FirstName = "Xue",
                LastName = ClanName,
                IsMale = false,
                Age = 42,
                MaxLifespan = 120,
                SpiritualRoot = 55, // Average
                Affinity = Element.Water,
                Realm = CultivationRealm.QiRefinement,
                MentalStability = 85,
                SpouseID = patriarch.ID
            };
            patriarch.SpouseID = matriarch.ID;
            AddMember(matriarch);

            // 3. Eldest Son
            var son = new CharacterData
            {
                FirstName = "Jian",
                LastName = ClanName,
                IsMale = true,
                Age = 20,
                MaxLifespan = 80,
                SpiritualRoot = 75, // Genius potential
                Affinity = Element.Lightning,
                Realm = CultivationRealm.Embryonic,
                FatherID = patriarch.ID,
                MotherID = matriarch.ID
            };
            AddMember(son);

            // 4. Daughter
            var daughter = new CharacterData
            {
                FirstName = "Mei",
                LastName = ClanName,
                IsMale = false,
                Age = 16,
                MaxLifespan = 80,
                SpiritualRoot = 45,
                Affinity = Element.Wood,
                Realm = CultivationRealm.Embryonic,
                FatherID = patriarch.ID,
                MotherID = matriarch.ID
            };
            AddMember(daughter);

            // 5. Uncle (Patriarch's brother)
            var uncle = new CharacterData
            {
                FirstName = "Shan",
                LastName = ClanName,
                IsMale = true,
                Age = 40,
                MaxLifespan = 80,
                SpiritualRoot = 25, // Poor
                Affinity = Element.Earth,
                Realm = CultivationRealm.Embryonic,
                MentalStability = 60
            };
            AddMember(uncle);
        }

        public void AddMember(CharacterData newMember)
        {
            LivingMembers.Add(newMember);
            GameEvents.TriggerCharacterBorn(newMember); // Used to notify UI and BloodRegistry
            Debug.Log($"[ClanManager] Added member: {newMember.FullName} (Age: {newMember.Age}, Realm: {newMember.Realm})");
        }

        private void HandleCharacterDeath(CharacterData character, DeathCause cause)
        {
            if (LivingMembers.Contains(character))
            {
                character.IsAlive = false;
                character.CauseOfDeath = cause;
                LivingMembers.Remove(character);
                Debug.Log($"[ClanManager] {character.FullName} has died from {cause}.");
            }
        }
    }
}
