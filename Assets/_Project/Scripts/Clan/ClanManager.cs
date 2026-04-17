using System.Collections.Generic;
using System.Linq;
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
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterDied -= HandleCharacterDeath;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Inheritance)
                ProcessAnnualBirths();
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

        private static readonly string[] MaleNames =
            { "Wei", "Jian", "Long", "Feng", "Hao", "Chen", "Ming", "Shan", "Zhi", "Bo", "Tao", "Jun", "Kai" };
        private static readonly string[] FemaleNames =
            { "Xue", "Mei", "Lan", "Ying", "Lin", "Yue", "Hua", "Qing", "Zhen", "Rui", "Shu", "Dan" };

        /// <summary>
        /// Creates a new child from two parents using GeneticSystem for
        /// spiritual root and elemental affinity. The child starts at age 0 in
        /// Embryonic realm and is immediately added to the clan.
        /// </summary>
        public CharacterData GenerateChild(CharacterData father, CharacterData mother)
        {
            bool isMale = Random.value > 0.5f;
            string[] pool = isMale ? MaleNames : FemaleNames;
            string firstName = pool[Random.Range(0, pool.Length)];

            var child = new CharacterData
            {
                FirstName = firstName,
                LastName = ClanName,
                IsMale = isMale,
                Age = 0,
                MaxLifespan = Random.Range(60, 120),
                SpiritualRoot = GeneticSystem.GenerateSpiritualRoot(father, mother),
                Affinity = GeneticSystem.GenerateAffinity(father, mother),
                Realm = CultivationRealm.Embryonic,
                MentalStability = 70,
                FatherID = father?.ID,
                MotherID = mother?.ID
            };

            AddMember(child);
            Debug.Log($"[ClanManager] A child is born: {child.FullName} (Root: {child.SpiritualRoot}, Affinity: {child.Affinity})");
            return child;
        }

        /// <summary>
        /// Called during the Inheritance phase. For each married couple where both
        /// are alive and the mother is between 16-45, roll a chance for a new child.
        /// </summary>
        public void ProcessAnnualBirths()
        {
            var couples = LivingMembers
                .Where(m => !string.IsNullOrEmpty(m.SpouseID) && m.IsMale)
                .ToList();

            foreach (var father in couples)
            {
                var mother = LivingMembers.Find(m => m.ID == father.SpouseID);
                if (mother == null || !mother.IsAlive) continue;
                if (mother.Age < 16 || mother.Age > 45) continue;

                if (Random.value < 0.25f) // 25% chance per year
                    GenerateChild(father, mother);
            }
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
