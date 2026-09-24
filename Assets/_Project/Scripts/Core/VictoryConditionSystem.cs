using UnityEngine;
using MirrorChronicles.Clan;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Core
{
    /// <summary>
    /// Checks for game-ending conditions: victory (10 generations + 1 ascension)
    /// or defeat (clan extinction).
    /// </summary>
    public class VictoryConditionSystem : MonoBehaviour
    {
        public static VictoryConditionSystem Instance { get; private set; }

        public bool GameWon { get; private set; }
        public bool GameLost { get; private set; }

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
            GameEvents.OnYearStarted += CheckVictory;
            GameEvents.OnCharacterDied += CheckDefeat;
        }

        private void OnDisable()
        {
            GameEvents.OnYearStarted -= CheckVictory;
            GameEvents.OnCharacterDied -= CheckDefeat;
        }

        private void CheckVictory(int year)
        {
            if (GameWon || GameLost) return;

            bool hasGenerations = ClanKarmaSystem.Instance != null &&
                                   ClanKarmaSystem.Instance.GenerationCount >= 10;
            bool hasAscension = AscensionSystem.Instance != null &&
                                 AscensionSystem.Instance.AscendedAncestorsCount >= 1;

            if (hasGenerations && hasAscension)
            {
                GameWon = true;
                Debug.Log("[VictoryCondition] VICTORY! 10 generations survived and an ancestor ascended!");

                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameState.GameOver);
            }
        }

        private void CheckDefeat(CharacterData character, DeathCause cause)
        {
            if (GameWon || GameLost) return;

            if (ClanManager.Instance != null && ClanManager.Instance.LivingMembers.Count == 0)
            {
                GameLost = true;
                Debug.LogWarning("[VictoryCondition] DEFEAT — The clan has been extinguished.");

                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameState.GameOver);
            }
        }
    }
}
