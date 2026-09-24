using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Tracks generational progress and provides cumulative passive bonuses.
    /// A new generation is counted each time a new patriarch is elected.
    /// </summary>
    public class ClanKarmaSystem : MonoBehaviour
    {
        public static ClanKarmaSystem Instance { get; private set; }

        public int GenerationCount { get; private set; } = 1;
        public int TotalDeaths { get; private set; }
        public int TotalBirths { get; private set; }

        private string _lastPatriarchID;

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
            GameEvents.OnCharacterBorn += HandleBirth;
            GameEvents.OnCharacterDied += HandleDeath;
            GameEvents.OnYearStarted += HandleYearStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterBorn -= HandleBirth;
            GameEvents.OnCharacterDied -= HandleDeath;
            GameEvents.OnYearStarted -= HandleYearStarted;
        }

        private void HandleBirth(CharacterData character)
        {
            TotalBirths++;
        }

        private void HandleDeath(CharacterData character, DeathCause cause)
        {
            TotalDeaths++;
        }

        private void HandleYearStarted(int year)
        {
            if (ClanManager.Instance == null) return;

            string currentPatriarch = ClanManager.Instance.PatriarchID;
            if (_lastPatriarchID == null)
            {
                _lastPatriarchID = currentPatriarch;
            }
            else if (currentPatriarch != _lastPatriarchID)
            {
                GenerationCount++;
                _lastPatriarchID = currentPatriarch;
                Debug.Log($"[ClanKarma] New generation! Generation {GenerationCount} begins.");
            }
        }

        /// <summary>
        /// Cultivation speed bonus: +2% per generation, stacking.
        /// </summary>
        public float GetCultivationSpeedBonus()
        {
            return GenerationCount * 0.02f;
        }

        /// <summary>
        /// At 5 generations: +2% cultivation speed.
        /// At 10 generations: +5% cultivation speed + ancestral technique unlock.
        /// </summary>
        public bool HasAncestralTechniqueUnlock()
        {
            return GenerationCount >= 10;
        }

        /// <summary>
        /// Bonus XP from karma applied to all cultivating members.
        /// </summary>
        public int GetBonusXP()
        {
            if (GenerationCount >= 10) return 10;
            if (GenerationCount >= 5) return 5;
            return 0;
        }
    }
}
